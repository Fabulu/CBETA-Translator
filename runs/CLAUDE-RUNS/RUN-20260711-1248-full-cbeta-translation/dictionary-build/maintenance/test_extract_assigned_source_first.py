#!/usr/bin/env python3
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

from extract_assigned_source_first import build_documents, extract_rows

HERE = Path(__file__).resolve().parent
ENV = HERE / "dictionary_python_env.py"
EXTRACTOR = HERE / "extract_assigned_source_first.py"

class AssignedSourceFirstTests(unittest.TestCase):
    def fixtures(self, floor, paths, tiers, duplicate=None):
        selection = [{"identityId": "id", "term": "詞", "requiredFloor": floor}]
        counts = {"id": {"per_file": [[path, 1] for path in paths]}}
        works = {path: (duplicate if duplicate and path.endswith("dup") else f"work:{path}")
                 for path in paths}
        def find(path, term, **_):
            return [{"window": f"前{term}後:{path}", "fromLb": "0001a01"}]
        return selection, counts, tiers, works, find

    def test_higher_tiers_fill_exact_floor_without_lamps(self):
        fx = self.fixtures(3, ["z2", "a1", "b2", "lamp3"],
                           {"z2": 2, "a1": 1, "b2": 2, "lamp3": 3})
        rows = extract_rows(
            fx[0], fx[1], tiers=fx[2], find_fn=fx[4],
            work_id_fn=lambda path: fx[3][path])
        self.assertEqual(["a1", "b2", "z2"],
                         [c["relPath"] for c in rows[0]["sourceCandidates"]])
        self.assertEqual(0, rows[0]["lampFallbackCount"])
        self.assertFalse(rows[0]["tier3Consulted"])
        self.assertTrue(all(c["familyAdjudicationRequired"]
                            for c in rows[0]["sourceCandidates"]))
        self.assertTrue(all("provisionalWorkKey" in c and
                            "deploymentFamilyId" not in c
                            for c in rows[0]["sourceCandidates"]))

    def test_minimal_lamp_fallback_after_higher_tier_exhaustion(self):
        fx = self.fixtures(3, ["b2", "lamp_b", "lamp_a", "lamp_c"],
                           {"b2": 2, "lamp_b": 3, "lamp_a": 3, "lamp_c": 3})
        rows = extract_rows(
            fx[0], fx[1], tiers=fx[2], find_fn=fx[4],
            work_id_fn=lambda path: fx[3][path])
        self.assertEqual(["b2", "lamp_a", "lamp_b"],
                         [c["relPath"] for c in rows[0]["sourceCandidates"]])
        self.assertEqual(2, rows[0]["lampFallbackCount"])
        self.assertIn("last-resort", rows[0]["lampPolicy"])

    def test_distinct_work_exact_order_and_deterministic_hashes(self):
        selection = [{"identityId": "id", "term": "詞", "requiredFloor": 2}]
        counts = {"id": {"per_file": [["a", 1], ["a_dup", 1], ["b", 1]]}}
        tiers = {"a": 1, "a_dup": 1, "b": 2}
        works = {"a": "work:a", "a_dup": "work:a", "b": "work:b"}
        def find(path, term, **_):
            return [{"window": f"{term}:{path}", "fromLb": "1"}]
        rows1 = extract_rows(selection, counts, tiers=tiers, find_fn=find,
                             work_id_fn=lambda path: works[path])
        rows2 = extract_rows(selection, counts, tiers=tiers, find_fn=find,
                             work_id_fn=lambda path: works[path])
        self.assertEqual(["a", "b"], [c["relPath"] for c in rows1[0]["sourceCandidates"]])
        self.assertEqual(rows1, rows2)
        out1 = build_documents("TEST", rows1)
        out2 = build_documents("TEST", rows2)
        rendered1 = json.dumps(out1, ensure_ascii=False, sort_keys=True).encode()
        rendered2 = json.dumps(out2, ensure_ascii=False, sort_keys=True).encode()
        self.assertEqual(hashlib.sha256(rendered1).hexdigest(),
                         hashlib.sha256(rendered2).hexdigest())

    def test_real_cli_rejects_stale_floor_and_tampered_count(self):
        with tempfile.TemporaryDirectory(dir=HERE) as raw:
            root = Path(raw)
            prefix = "non-iriya-v7-depth-regeneration-r99"
            selection = root / f"{prefix}-selection-b.json"
            count = root / f"{prefix}-count-b.json"
            gate = root / f"{prefix}-timegate-b.json"
            viability = root / f"{prefix}-viability-checkpoint-b.json"
            output = root / f"{prefix}-extraction-output-b.json"
            skeleton = root / f"{prefix}-research-skeleton-b.json"
            rows = [{"identityId": "id", "term": "詞", "requiredFloor": 2}]
            selection.write_text(json.dumps({"rows": rows}), encoding="utf-8")
            count.write_text(json.dumps({"results": [{
                "id": "id", "term": "詞", "per_file": [["a", 1], ["b", 1]]
            }]}), encoding="utf-8")
            gate.write_text(json.dumps({
                "schemaVersion": "bounded-dictionary-timegate.v2",
                "requiredFloors": [2]}), encoding="utf-8")
            viability.write_text(json.dumps({
                "hardPass": True, "ids": ["id"], "terms": ["詞"],
                "requiredFloors": [2],
                "selectionSha256": hashlib.sha256(selection.read_bytes()).hexdigest(),
                "countSha256": hashlib.sha256(count.read_bytes()).hexdigest(),
            }), encoding="utf-8")
            command = [
                sys.executable, str(ENV), "--script", str(EXTRACTOR), "--",
                "--extraction-output", str(output), "--research-skeleton", str(skeleton),
                "--timegate", str(gate), "--selection", str(selection),
                "--count", str(count), "--viability-receipt", str(viability),
            ]
            rows[0]["requiredFloor"] = 3
            selection.write_text(json.dumps({"rows": rows}), encoding="utf-8")
            stale_floor = subprocess.run(command, capture_output=True, text=True)
            self.assertNotEqual(0, stale_floor.returncode)
            self.assertIn("stale/tampered", stale_floor.stderr)
            self.assertFalse(output.exists())
            rows[0]["requiredFloor"] = 2
            selection.write_text(json.dumps({"rows": rows}), encoding="utf-8")
            viability_data = json.loads(viability.read_text())
            viability_data["selectionSha256"] = hashlib.sha256(
                selection.read_bytes()).hexdigest()
            viability.write_text(json.dumps(viability_data), encoding="utf-8")
            count.write_text(count.read_text() + " ", encoding="utf-8")
            stale_count = subprocess.run(command, capture_output=True, text=True)
            self.assertNotEqual(0, stale_count.returncode)
            self.assertIn("stale/tampered", stale_count.stderr)
            self.assertFalse(output.exists())


if __name__ == "__main__":
    unittest.main()
