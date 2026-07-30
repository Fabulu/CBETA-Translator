#!/usr/bin/env python3
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import launch_assigned_cohort as launcher


class AssignedCohortLauncherTest(unittest.TestCase):
    def test_exact_next_unreserved_and_single_batch_count(self):
        with tempfile.TemporaryDirectory() as raw:
            root = Path(raw)
            maintenance = root / "maintenance"
            maintenance.mkdir()
            chunk = maintenance / "chunk.json"
            chunk.write_text(json.dumps({"rows": [
                {"id": "used", "term": "舊", "requiredFloor": 4, "hardFail": True},
                {"id": "reserved", "term": "留", "requiredFloor": 7, "hardFail": False},
                {"id": "a", "term": "甲", "requiredFloor": 8, "hardFail": False},
                {"id": "b", "term": "乙", "requiredFloor": 4, "hardFail": True},
                {"id": "c", "term": "丙", "requiredFloor": 7, "hardFail": False},
            ]}), encoding="utf-8")
            selector = maintenance / "selector.json"
            selector.write_text(json.dumps({"chunks": [{"path": "maintenance/chunk.json"}]}), encoding="utf-8")
            union = maintenance / "union.json"
            union.write_text(json.dumps({"ids": ["used"]}), encoding="utf-8")
            timegate = maintenance / "timegate.json"
            timegate.write_text(json.dumps({"cohort": "R99", "artifactZero": True}), encoding="utf-8")
            calls = []

            def fake_count(terms):
                calls.append(list(terms))
                return {term: {"hits": 10, "files": 9, "works": 8, "per_file": []} for term in terms}

            with patch.object(launcher, "ROOT", root):
                paths = launcher.prepare(
                    cohort="R99", timegate=timegate, prior_union=union, selector=selector,
                    entries=[("a", "甲", 8), ("b", "乙", 4), ("c", "丙", 7)],
                    reserve_ids=["reserved"], output_dir=maintenance, count_fn=fake_count,
                )
            self.assertEqual(calls, [["甲", "乙", "丙"]])
            self.assertTrue(all(path.exists() for key, path in paths.items() if key != "receipt"))
            selection = json.loads(paths["selection"].read_text())
            self.assertEqual([row["identityId"] for row in selection["rows"]], ["a", "b", "c"])
            self.assertTrue(selection["collisionCheck"]["hardPass"])


if __name__ == "__main__":
    unittest.main()
