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
    verify_whole_config_preclosure,
)

ROOT = Path(__file__).resolve().parent.parent
WATCHDOG = ROOT / "maintenance/construction_start_watchdog.py"
ENGINE = ROOT / "maintenance/generic_bounded_constructor.py"
FIXTURE_IDS = ["t_0f8df3105c35", "t_0f97bfab265c", "t_0fb97dffe2bc"]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


class GenericBoundedConstructorIntegrationTest(unittest.TestCase):
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
