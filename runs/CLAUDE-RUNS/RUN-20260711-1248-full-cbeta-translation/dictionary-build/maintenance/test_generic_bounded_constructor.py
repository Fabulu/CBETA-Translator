#!/usr/bin/env python3
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

from atomic_write import atomic_write_json
from maintenance.generic_bounded_constructor import (
    ActorClosureError, CompilerPrewriteError, enforce_governed_deadline, run,
    verify_actor_closure, verify_output_collision_policy,
    verify_late_research_continuation, verify_whole_config_preclosure,
)

ROOT = Path(__file__).resolve().parent.parent
WATCHDOG = ROOT / "maintenance/construction_start_watchdog.py"
ENGINE = ROOT / "maintenance/generic_bounded_constructor.py"
FIXTURE_IDS = ["t_1c2e34e1abb7", "t_1c3869bb802d", "t_1c7d25824f85"]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


class GenericBoundedConstructorIntegrationTest(unittest.TestCase):
    def test_authorized_late_research_accepts_exact_scope_and_hashes(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            base = Path(raw).resolve()
            extraction = base / "extraction.json"
            failed = base / "failed.json"
            matrix = base / "matrix.json"
            for path, value in (
                (extraction, {"rows": []}),
                (failed, {"hardPass": False}),
                (matrix, {"rows": []}),
            ):
                atomic_write_json(path, value)
            receipt = base / "late.json"
            ids = ["a", "b", "c"]
            atomic_write_json(receipt, {
                "schemaVersion": "r89-late-research-continuation.v1",
                "cohort": "R89", "hardPass": False,
                "lateContinuationAuthorized": True,
                "scopeExpansionForbidden": True, "ids": ids,
                "extractionPath": str(extraction), "extractionSha256": sha(extraction),
                "failClosedCheckpointPath": str(failed),
                "failClosedCheckpointSha256": sha(failed),
                "acceptedMatrixPath": str(matrix),
                "acceptedMatrixSha256": sha(matrix),
            })
            research = {
                "governedExtractionSha256": sha(extraction),
                "rows": [{"retainedReviewSha256": sha(matrix)} for _ in ids],
            }
            verify_late_research_continuation(
                receipt, allowed_root=base, cohort="R89",
                entry_ids=ids, research=research)

    def test_authorized_late_research_rejects_scope_and_hash_drift(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            base = Path(raw).resolve()
            files = [base / name for name in ("extraction.json", "failed.json", "matrix.json")]
            for path in files:
                atomic_write_json(path, {"name": path.name})
            extraction, failed, matrix = files
            receipt = base / "late.json"
            atomic_write_json(receipt, {
                "schemaVersion": "r89-late-research-continuation.v1",
                "cohort": "R89", "hardPass": False,
                "lateContinuationAuthorized": True,
                "scopeExpansionForbidden": True, "ids": ["a", "b", "c"],
                "extractionPath": str(extraction), "extractionSha256": sha(extraction),
                "failClosedCheckpointPath": str(failed),
                "failClosedCheckpointSha256": sha(failed),
                "acceptedMatrixPath": str(matrix),
                "acceptedMatrixSha256": sha(matrix),
            })
            research = {
                "governedExtractionSha256": sha(extraction),
                "rows": [{"retainedReviewSha256": sha(matrix)}] * 3,
            }
            with self.assertRaisesRegex(ValueError, "scope or decision drift"):
                verify_late_research_continuation(
                    receipt, allowed_root=base, cohort="R89",
                    entry_ids=["a", "b", "drift"], research=research)
            research["governedExtractionSha256"] = "0" * 64
            with self.assertRaisesRegex(ValueError, "extraction/research binding drift"):
                verify_late_research_continuation(
                    receipt, allowed_root=base, cohort="R89",
                    entry_ids=["a", "b", "c"], research=research)

    def test_actor_prewrite_rejects_roster_alias_and_unlinked_name(self):
        identity = FIXTURE_IDS[0]
        source = ROOT / "fresh-build/entries" / identity
        worksheet = json.loads((source / "evidence.draft.json").read_text())
        dossier = json.loads((source / "source-dossier.json").read_text())
        config = {"entries": [{
            "id": identity,
            "term": worksheet["Entry"]["SourceTerm"],
            "sourceDossier": dossier,
            "evidenceDraft": worksheet,
        }]}
        occurrence = worksheet["Entry"]["Senses"][0]["Occurrences"][0]
        occurrence["MasterName"] = "石霜楚圓"
        with self.assertRaises(ActorClosureError) as alias_failure:
            verify_actor_closure(config)
        self.assertIn(
            "use canonical names[0] 'Shishuang Chuyuan'",
            str(alias_failure.exception),
        )

        occurrence["MasterName"] = "Juefan Huihong"
        with self.assertRaises(ActorClosureError) as unlinked_failure:
            verify_actor_closure(config)
        self.assertIn(
            "structured identified-unlinked-master ActorAttribution",
            str(unlinked_failure.exception),
        )

    def test_preexisting_per_id_output_requires_bound_replacement_authority(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            base = Path(raw).resolve()
            output = base / "output"
            identity = "t_collision_fixture"
            (output / identity).mkdir(parents=True)
            config = {
                "cohort": "TEST",
                "entries": [{"id": identity}],
            }
            with self.assertRaisesRegex(
                ValueError, "preexisting per-ID output directories rejected"
            ):
                verify_output_collision_policy(config, output, base, time.time())
            self.assertTrue(
                (output / identity).is_dir(),
                "collision preflight must not mutate the preexisting directory",
            )

    def test_prewrite_rejects_stale_cohort_boilerplate_in_duplicate_ruling(self):
        identity = FIXTURE_IDS[0]
        source = ROOT / "fresh-build/entries" / identity
        worksheet = json.loads((source / "evidence.draft.json").read_text())
        dossier = json.loads((source / "source-dossier.json").read_text())
        worksheet["Admission"]["DuplicateCheck"]["NearDuplicateRuling"] = (
            "No collision occurs in R01-R10 or the current R11 selection."
        )
        entry = {
            "id": identity,
            "term": worksheet["Entry"]["SourceTerm"],
            "sourceDossier": dossier,
            "evidenceDraft": worksheet,
        }
        with self.assertRaisesRegex(
            ValueError, "NearDuplicateRuling contains stale cohort-number boilerplate"
        ):
            verify_whole_config_preclosure({"entries": [entry]})

    def test_fully_adjudicated_evidence_may_exceed_minimum_floor(self):
        identity = FIXTURE_IDS[0]
        source = ROOT / "fresh-build/entries" / identity
        worksheet = json.loads((source / "evidence.draft.json").read_text())
        dossier = json.loads((source / "source-dossier.json").read_text())
        floor = dossier["requiredFloor"]
        self.assertEqual(floor, len(dossier["retainedCompleteCases"]))
        self.assertEqual(
            floor,
            sum(len(sense["Occurrences"]) for sense in worksheet["Entry"]["Senses"]),
        )
        dossier["retainedCompleteCases"].append(
            json.loads(json.dumps(dossier["retainedCompleteCases"][-1]))
        )
        worksheet["Entry"]["Senses"][0]["Occurrences"].append(
            json.loads(json.dumps(worksheet["Entry"]["Senses"][0]["Occurrences"][-1]))
        )
        entry = {
            "id": identity,
            "term": worksheet["Entry"]["SourceTerm"],
            "sourceDossier": dossier,
            "evidenceDraft": worksheet,
        }
        verify_whole_config_preclosure({"entries": [entry]})

    def test_finalized_evidence_below_minimum_floor_is_rejected(self):
        identity = FIXTURE_IDS[0]
        source = ROOT / "fresh-build/entries" / identity
        worksheet = json.loads((source / "evidence.draft.json").read_text())
        dossier = json.loads((source / "source-dossier.json").read_text())
        floor = dossier["requiredFloor"]
        dossier["retainedCompleteCases"] = dossier["retainedCompleteCases"][:floor - 1]
        entry = {
            "id": identity,
            "term": worksheet["Entry"]["SourceTerm"],
            "sourceDossier": dossier,
            "evidenceDraft": worksheet,
        }
        with self.assertRaisesRegex(
            ValueError,
            rf"retained semantic evidence {floor - 1} is below requiredFloor {floor}",
        ):
            verify_whole_config_preclosure({"entries": [entry]})

    def test_injected_now_governed_deadline_boundaries(self):
        deadlines={"firstProduct":518,"construction":578}
        enforce_governed_deadline(300,deadlines,"firstProduct")
        enforce_governed_deadline(400,deadlines,"construction")
        with self.assertRaisesRegex(TimeoutError,r"firstProduct late: 519\.000s > 518s"):
            enforce_governed_deadline(519,deadlines,"firstProduct")
        with self.assertRaisesRegex(TimeoutError,r"construction late: 579\.000s > 578s"):
            enforce_governed_deadline(579,deadlines,"construction")

    def test_r58_config_only_payload_closes_before_authority(self):
        entries = []
        for identity in FIXTURE_IDS:
            source = ROOT / "fresh-build/entries" / identity
            worksheet = json.loads((source / "evidence.draft.json").read_text())
            dossier = json.loads((source / "source-dossier.json").read_text())
            worksheet["Entry"]["CreatedBy"] = "R58 source-hierarchy repair"
            worksheet["FamilyHarvest"]["Scope"] = (
                "R58 source-hierarchy repair exact source-first family harvest"
            )
            entries.append({
                "id": identity,
                "term": worksheet["Entry"]["SourceTerm"],
                "sourceDossier": dossier,
                "evidenceDraft": worksheet,
            })
        verify_whole_config_preclosure({"entries": entries})

    def test_preclosure_omission_and_stale_scope_fail_before_any_write(self):
        fixture_ids = FIXTURE_IDS[:2]
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            base = Path(raw).resolve()
            output = base / "output"
            config_path = base / "config.json"
            entries = []
            for identity in fixture_ids:
                source = ROOT / "fresh-build/entries" / identity
                worksheet = json.loads((source / "evidence.draft.json").read_text())
                dossier = json.loads((source / "source-dossier.json").read_text())
                entries.append({
                    "id": identity,
                    "term": worksheet["Entry"]["SourceTerm"],
                    "sourceDossier": dossier,
                    "evidenceDraft": worksheet,
                })
            entries[1]["sourceDossier"].pop("requiredFloor", None)
            entries[1]["evidenceDraft"]["Entry"]["CreatedBy"] = "R57 fixture"
            entries[1]["evidenceDraft"]["FamilyHarvest"]["Scope"] = "R56 stale scope"
            atomic_write_json(config_path, {"entries": entries})
            with patch(
                "maintenance.generic_bounded_constructor.verify_authority",
                return_value={"outputRoot": output},
            ):
                with self.assertRaisesRegex(
                    ValueError,
                    "whole-config prewrite preclosure failed.*requiredFloor.*FamilyHarvest.Scope",
                ):
                    run(config_path, base)
            self.assertFalse(
                output.exists(),
                "later-entry metadata closure failure must create no product root",
            )

    def test_actor_closure_reports_all_coordinates_before_any_product_write(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            base = Path(raw).resolve()
            output = base / "output"
            config_path = base / "config.json"

            def bad_occurrence(*, utterer=False, empty_grammar=False):
                actor = {
                    "Status": "narrated",
                    "Kind": "identified non-master",
                    "ActorLabel": "reviewed actor",
                    "GrammarEvidence": (
                        "" if empty_grammar
                        else "The complete clause explicitly assigns this action."
                    ),
                }
                return {
                    "RelPath": "X/fixture.xml",
                    "FromLb": "0001a01",
                    "ToLb": "0001a02",
                    "Kwic": "甲云測試",
                    "MasterName": None,
                    "ActorAttribution": actor,
                    "ContextMasters": (
                        [{"MasterName": "Fixture Master", "Roles": ["utterer"]}]
                        if utterer else []
                    ),
                    "AttributionNote": "Source record (X/fixture.xml). Fixture.",
                    "DraftActorProof": {
                        "GrammaticalSubject": "reviewed actor",
                        "FullCaseDecision": "The complete case assigns the clause.",
                    },
                }

            entries = []
            for index, occurrence in enumerate((
                bad_occurrence(empty_grammar=True),
                bad_occurrence(utterer=True),
            )):
                identity = f"t_fixture_{index}"
                entries.append({
                    "id": identity,
                    "term": f"fixture-{index}",
                    "sourceDossier": {},
                    "evidenceDraft": {
                        "Entry": {
                            "Id": identity,
                            "SourceTerm": f"fixture-{index}",
                            "Senses": [{"Occurrences": [occurrence]}],
                        }
                    },
                })
            atomic_write_json(config_path, {"entries": entries})
            governed_paths = {"outputRoot": output}
            with patch(
                "maintenance.generic_bounded_constructor.verify_authority",
                return_value=governed_paths,
            ):
                with self.assertRaises(ActorClosureError) as raised:
                    run(config_path, base)

            errors = raised.exception.errors
            self.assertTrue(any("entries[0](t_fixture_0)" in row for row in errors))
            self.assertTrue(any("GrammarEvidence" in row for row in errors))
            self.assertTrue(any("entries[1](t_fixture_1)" in row for row in errors))
            self.assertTrue(any("utterer role contradicts null MasterName" in row for row in errors))
            self.assertFalse(output.exists(), "actor preflight must create no product root")

    def test_canonical_compiler_collects_later_entry_defects_before_any_write(self):
        """A valid first entry cannot escape before unrelated later defects."""
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            base = Path(raw).resolve()
            output = base / "output"
            config_path = base / "config.json"
            entries = []
            for identity in FIXTURE_IDS:
                source = ROOT / "fresh-build/entries" / identity
                worksheet = json.loads((source / "evidence.draft.json").read_text())
                dossier = json.loads((source / "source-dossier.json").read_text())
                entries.append({
                    "id": identity,
                    "term": worksheet["Entry"]["SourceTerm"],
                    "sourceDossier": dossier,
                    "evidenceDraft": worksheet,
                })
            # The R62 defect occurs only in the second entry.
            entries[1]["evidenceDraft"]["Entry"]["Senses"][0]["DraftEvidence"][
                "FamilyControls"] = []
            # A different canonical compiler-schema defect occurs in the third.
            entries[2]["evidenceDraft"]["Admission"]["Decision"] = "reject"
            atomic_write_json(config_path, {"entries": entries})
            with patch(
                "maintenance.generic_bounded_constructor.verify_authority",
                return_value={"outputRoot": output},
            ):
                with self.assertRaises(CompilerPrewriteError) as raised:
                    run(config_path, base)
            errors = raised.exception.errors
            self.assertTrue(any(
                f"entries[1]({FIXTURE_IDS[1]})" in row and "FamilyControls" in row
                for row in errors
            ))
            self.assertTrue(any(
                f"entries[2]({FIXTURE_IDS[2]})" in row and "Admission.Decision" in row
                for row in errors
            ))
            self.assertFalse(
                output.exists(),
                "all-entry canonical dry compile must precede output-root creation",
            )

    def test_watchdog_cli_real_compile_three_entries(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            base = Path(raw).resolve()
            started = time.time()
            timegate = base / "timegate.json"
            receipt = base / "start.json"
            selection = base / "selection.json"
            research = base / "research.json"
            count = base / "count.json"
            union = base / "union.json"
            preflight = base / "preflight.json"
            audit = base / "commands.json"
            config_path = base / "config.json"
            wrapper = base / "constructor.py"
            atomic_write_json(timegate, {
                "cohort": "FIXTURE", "startedEpoch": started,
                "deadlinesSeconds": {"firstProduct": 518, "construction": 578},
            })
            entries = []
            selection_rows = []
            research_rows = []
            for identity in FIXTURE_IDS:
                source = ROOT / "fresh-build/entries" / identity
                worksheet = json.loads((source / "evidence.draft.json").read_text(encoding="utf-8"))
                dossier = json.loads((source / "source-dossier.json").read_text(encoding="utf-8"))
                term = worksheet["Entry"]["SourceTerm"]
                entries.append({
                    "id": identity, "term": term,
                    "sourceDossier": dossier, "evidenceDraft": worksheet,
                })
                selection_rows.append({"identityId": identity, "term": term})
                research_rows.append({"id": identity, "term": term})
            atomic_write_json(selection, {"rows": selection_rows})
            atomic_write_json(research, {"rows": research_rows})
            atomic_write_json(count, {"results": []})
            atomic_write_json(union, {"uniqueIds": []})
            atomic_write_json(preflight, {"hardPass": True})
            atomic_write_json(audit, {
                "complete": True,
                "commands": [{"epoch": time.time(), "command": "fixture receipt-first setup"}],
            })
            paths = {
                "selection": str(selection), "research": str(research),
                "outputRoot": str(base / "output"),
                "firstProductReceipt": str(base / "first.json"),
                "preclosure": str(base / "preclosure.json"),
                "manifest": str(base / "manifest.json"),
                "closure": str(base / "closure.json"),
            }
            atomic_write_json(config_path, {
                "schemaVersion": "generic-bounded-constructor-config.v2",
                "cohort": "FIXTURE", "startedEpoch": started,
                "timegatePath": str(timegate),
                "watchdogReceiptPath": str(receipt),
                "commandAuditPath": str(audit),
                "engineSha256": sha(ENGINE),
                "paths": paths, "entries": entries,
            })
            wrapper.write_text(
                "import sys\n"
                f"sys.path.insert(0,{str(ROOT)!r})\n"
                "from maintenance.generic_bounded_constructor import main\n"
                "raise SystemExit(main())\n",
                encoding="utf-8",
            )
            os.utime(wrapper, None)
            command = [
                sys.executable, str(WATCHDOG), "invoke",
                "--timegate", str(timegate), "--receipt", str(receipt),
                "--constructor", str(wrapper), "--preflight-receipt", str(preflight),
                "--command-audit", str(audit),
            ]
            for kind, path in {
                "union": union, "selection": selection, "count": count,
                "preflight": preflight, "research": research, "config": config_path,
                "command-audit": audit,
            }.items():
                command += ["--cohort-artifact", f"{kind}={path}"]
            command += [
                "--ids", *FIXTURE_IDS, "--", sys.executable, str(wrapper),
                "--config", str(config_path), "--allowed-build-root", str(base),
            ]
            completed = subprocess.run(command, cwd=ROOT, text=True, capture_output=True)
            self.assertEqual(0, completed.returncode, completed.stderr + completed.stdout)
            self.assertTrue((base / "first.json").is_file())
            self.assertTrue((base / "preclosure.json").is_file())
            self.assertTrue((base / "manifest.json").is_file())
            self.assertTrue((base / "closure.json").is_file())
            self.assertTrue(json.loads((base / "preclosure.json").read_text())["hardPass"])
            self.assertEqual(
                518, json.loads((base / "first.json").read_text())["deadlineSeconds"])
            self.assertEqual(
                578, json.loads((base / "manifest.json").read_text())["deadlineSeconds"])
            self.assertEqual(
                578, json.loads((base / "closure.json").read_text())["deadlineSeconds"])
            for identity in FIXTURE_IDS:
                self.assertTrue((base / "output" / identity / "entry.v2.json").is_file())


if __name__ == "__main__":
    unittest.main()
