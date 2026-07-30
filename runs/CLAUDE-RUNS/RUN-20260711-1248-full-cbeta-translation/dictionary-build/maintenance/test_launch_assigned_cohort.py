#!/usr/bin/env python3
import json
import os
import subprocess
import sys
import tempfile
import time
import unittest
from datetime import datetime, timezone
from pathlib import Path
from unittest.mock import patch

import launch_assigned_cohort as launcher
from cohort_checkpoint_watchdog import evidence_schedule


class AssignedCohortLauncherTest(unittest.TestCase):
    def test_cli_imports_root_zc_without_external_pythonpath(self):
        script = Path(__file__).with_name("launch_assigned_cohort.py").resolve()
        environment = os.environ.copy()
        environment.pop("PYTHONPATH", None)
        with tempfile.TemporaryDirectory() as raw:
            result = subprocess.run(
                [sys.executable, str(script), "--help"],
                cwd=raw,
                env=environment,
                text=True,
                capture_output=True,
            )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertNotIn("ModuleNotFoundError", result.stderr)

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
            started = time.time()
            timegate.write_text(json.dumps({
                "schemaVersion": "bounded-dictionary-timegate.v3",
                "cohort": "R99", "artifactZero": True,
                "startedEpoch": started,
                "createdUtc": datetime.fromtimestamp(
                    started, timezone.utc).isoformat().replace("+00:00", "Z"),
                "requiredFloors": [8, 4, 7],
                "admittedRequiredOccurrences": 19,
                "adjudicatedCaseLoad": 19,
                "researchCandidateReserve": 3,
                "deadlinesSeconds": evidence_schedule([8, 4, 7], 19)[1],
                "assignedLaunch": {
                    "selector": str(selector.resolve()),
                    "priorUnion": str(union.resolve()),
                    "entries": [
                        {"id": "a", "term": "甲", "requiredFloor": 8},
                        {"id": "b", "term": "乙", "requiredFloor": 4},
                        {"id": "c", "term": "丙", "requiredFloor": 7},
                    ],
                    "reserveIds": ["reserved"],
                    "researchCandidateReserve": 3,
                },
            }), encoding="utf-8")
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
            audit = json.loads(paths["researchAudit"].read_text())
            argv = audit["commands"][0]["argv"]
            self.assertEqual(Path(argv[1]), (root / "maintenance/dictionary_python_env.py").resolve())
            self.assertEqual(Path(argv[3]), (root / "maintenance/extract_assigned_source_first.py").resolve())
            self.assertEqual(Path(argv[10]), timegate.resolve())
            self.assertEqual(Path(argv[12]), paths["selection"].resolve())
            self.assertEqual(Path(argv[14]), paths["count"].resolve())
            self.assertEqual(Path(argv[16]), paths["receipt"].resolve())


if __name__ == "__main__":
    unittest.main()
