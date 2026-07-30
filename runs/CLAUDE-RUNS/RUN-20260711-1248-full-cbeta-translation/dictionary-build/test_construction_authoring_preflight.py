#!/usr/bin/env python3
from __future__ import annotations

import importlib.util
import json
import subprocess
import sys
import tempfile
import unittest
from copy import deepcopy
from pathlib import Path

HERE = Path(__file__).resolve().parent
SPEC = importlib.util.spec_from_file_location("construction_preflight", HERE / "construction_authoring_preflight.py")
preflight = importlib.util.module_from_spec(SPEC)
assert SPEC.loader
SPEC.loader.exec_module(preflight)
COMPILER_SPEC = importlib.util.spec_from_file_location("canonical_compiler", HERE / "compile_evidence_draft.py")
compiler = importlib.util.module_from_spec(COMPILER_SPEC)
assert COMPILER_SPEC.loader
COMPILER_SPEC.loader.exec_module(compiler)

REFERENCE = HERE / "fresh-build/entries/t_fb9ab5bac0bf/evidence.draft.json"


class ConstructionPreflightTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="construction-preflight-test-")
        self.root = Path(self.temp.name)

    def tearDown(self):
        self.temp.cleanup()

    def fixture(self, mutate=None, corrupt_product=False):
        directory = self.root / "t_fixture000001"
        directory.mkdir()
        draft = deepcopy(json.loads(REFERENCE.read_text(encoding="utf-8")))
        draft["Entry"]["Id"] = directory.name
        if mutate:
            mutate(draft["Entry"])
        worksheet = directory / "evidence.draft.json"
        worksheet.write_text(json.dumps(draft, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        product = directory / "entry.v2.json"
        report = directory / "compile.json"
        subprocess.run(
            [sys.executable, str(HERE / "compile_evidence_draft.py"), str(worksheet), "--output", str(product), "--report", str(report)],
            check=False, capture_output=True, text=True,
        )
        if corrupt_product:
            product.write_text(product.read_text(encoding="utf-8") + " ", encoding="utf-8")
        return directory

    def kinds(self, directory, expected_term="草鞋"):
        expected = {directory.name: {"id": directory.name, "term": expected_term,
                    "worksheetSha256": preflight.sha(directory / "evidence.draft.json"),
                    "productSha256": preflight.sha(directory / "entry.v2.json") if (directory / "entry.v2.json").exists() else "missing"}}
        row = preflight.inspect(directory, expected, preflight.DEFAULT_PENDING)
        return {failure["kind"] for failure in row["failures"]}

    def manifest(self, directories):
        rows=[]
        for directory in directories:
            draft=json.loads((directory/"evidence.draft.json").read_text())["Entry"]
            rows.append({"id":directory.name,"term":draft["SourceTerm"],
                         "worksheetSha256":preflight.sha(directory/"evidence.draft.json"),
                         "productSha256":preflight.sha(directory/"entry.v2.json")})
        path=self.root/"manifest.json"
        source=self.root/"source.json"; source.write_text("{}\n")
        payload={"entryCount":len(rows),"source":str(source),"sourceSha256":preflight.sha(source),
                 "corpusManifestSha256":preflight.sha(preflight.CORPUS_MANIFEST),
                 "rosterSha256":preflight.sha(preflight.DEFAULT_ROSTER),
                 "pendingRosterSha256":preflight.sha(preflight.DEFAULT_PENDING),"rows":rows}
        path.write_text(json.dumps(payload))
        return path

    def test_headword_placeholder_and_forbidden_framing_fail_early(self):
        def mutate(entry):
            entry["SourceTerm"] = "straw sandals"
            entry["Senses"][0]["Note"] = "TODO: Buddhism meditation placeholder"
        kinds = self.kinds(self.fixture(mutate))
        self.assertIn("expected-source-term-mismatch", kinds)
        self.assertIn("english-corrupted-headword", kinds)
        self.assertIn("unresolved-placeholder", kinds)
        self.assertIn("forbidden-framing", kinds)

    def test_invalid_actor_and_source_note_are_delegated_to_canonical_auditor(self):
        def mutate(entry):
            occurrence = entry["Senses"][0]["Occurrences"][0]
            occurrence["MasterName"] = None
            occurrence["ActorAttribution"] = {
                "Status": "invented-status", "Kind": "named actor", "ActorLabel": "Named Person",
                "ActorRole": "author", "ReviewedBy": "fixture", "ReviewedUtc": "2026-07-17T00:00:00Z"
            }
            occurrence["AttributionNote"] = "Broken source opening with no English label."
        kinds = self.kinds(self.fixture(mutate))
        self.assertTrue({"invalid_actor_status", "invalid_actor_role"} & kinds)
        self.assertTrue({"attribution-note-source-opening-defect", "note_missing_english_source_label"} & kinds)

    def test_source_ledger_and_exact_kwic_fail_before_review(self):
        def mutate(entry):
            sense = entry["Senses"][0]
            occurrence = sense["Occurrences"][0]
            sense["SourceTexts"] = [path for path in sense["SourceTexts"] if path != occurrence["RelPath"]]
            occurrence["Kwic"] += "不存在"
        kinds = self.kinds(self.fixture(mutate))
        self.assertIn("worksheet-occurrence-relpath-missing-from-sense-sourcetexts", kinds)
        self.assertIn("exact-kwic-failure", kinds)

    def test_compile_drift_is_fatal(self):
        self.assertIn("worksheet-entry-compile-drift", self.kinds(self.fixture(corrupt_product=True)))

    def test_known_redundant_opening_definitions_fail(self):
        known_failures = [
            ("瓦解冰消", "瓦解冰消 means collapse and melt away. 瓦解冰消 means to collapse and melt away completely."),
            ("三界唯心", "三界唯心 means the three realms are mind alone. 三界唯心 is the inherited formula that the three realms are mind alone."),
        ]
        for term, explanation in known_failures:
            with self.subTest(term=term):
                self.assertTrue(preflight.redundant_consecutive_opening_definition(term, explanation))
                self.assertTrue(compiler.redundant_consecutive_opening_definition(term, explanation))

    def test_later_contextual_headword_recurrence_is_permitted(self):
        explanation = (
            "三界唯心 is the inherited formula that the three realms are mind alone. "
            "Huangbo qualifies it rather than leaving it as doctrine. "
            "三界唯心 then becomes the premise of Tianyin's question about entering and leaving."
        )
        self.assertFalse(preflight.redundant_consecutive_opening_definition("三界唯心", explanation))
        self.assertFalse(compiler.redundant_consecutive_opening_definition("三界唯心", explanation))

    def test_unbound_or_unlisted_entry_fails(self):
        directory=self.fixture()
        row=preflight.inspect(directory, {}, preflight.DEFAULT_PENDING, run_attribution_check=False)
        self.assertIn("entry-not-in-manifest", {x["kind"] for x in row["failures"]})

    def test_duplicate_manifest_ids_fail(self):
        directory=self.fixture(); path=self.manifest([directory])
        data=json.loads(path.read_text()); data["rows"].append(deepcopy(data["rows"][0])); data["entryCount"]=2; path.write_text(json.dumps(data))
        _, failures, _=preflight.manifest_rows(path)
        self.assertIn("duplicate-manifest-entry-id", {x["kind"] for x in failures})

    def test_stale_paired_content_fails_manifest_binding(self):
        directory=self.fixture(); expected={directory.name:{"id":directory.name,"term":"草鞋","worksheetSha256":"0"*64,"productSha256":"0"*64}}
        row=preflight.inspect(directory,expected,preflight.DEFAULT_PENDING,run_attribution_check=False)
        kinds={x["kind"] for x in row["failures"]}
        self.assertIn("manifest-worksheet-hash-mismatch",kinds); self.assertIn("manifest-product-hash-mismatch",kinds)

    def test_current_authority_is_965_roster(self):
        roster=json.loads(preflight.DEFAULT_ROSTER.read_text())
        self.assertEqual(965,len(roster))

    def test_missing_handoff_source_binding_fails(self):
        directory=self.fixture(); path=self.manifest([directory]); data=json.loads(path.read_text())
        data.pop("source"); data.pop("sourceSha256"); path.write_text(json.dumps(data))
        _, failures, _=preflight.manifest_rows(path)
        self.assertIn("manifest-handoff-source-binding-missing", {x["kind"] for x in failures})

    def test_partial_limit_is_not_certification(self):
        one=self.fixture(); two=self.root/"t_fixture000002"; two.mkdir()
        for name in ("evidence.draft.json","entry.v2.json","compile.json"):
            (two/name).write_bytes((one/name).read_bytes())
        draft=json.loads((two/"evidence.draft.json").read_text()); draft["Entry"]["Id"]=two.name
        (two/"evidence.draft.json").write_text(json.dumps(draft));
        subprocess.run([sys.executable,str(HERE/"compile_evidence_draft.py"),str(two/"evidence.draft.json"),"--output",str(two/"entry.v2.json"),"--report",str(two/"compile.json")],capture_output=True)
        manifest=self.manifest([one,two]); out=self.root/"out.json"
        process=subprocess.run([sys.executable,str(HERE/"construction_authoring_preflight.py"),"--manifest",str(manifest),"--limit","1","--output",str(out)],capture_output=True,text=True)
        self.assertNotEqual(0,process.returncode)
        self.assertIn("scope-not-fully-checked", {x["kind"] for x in json.loads(out.read_text())["manifestFailures"]})


if __name__ == "__main__":
    unittest.main()
