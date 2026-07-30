#!/usr/bin/env python3
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import unittest

ROOT = Path(__file__).resolve().parent.parent
HELPER = ROOT / "maintenance/create_cohort_artifact_zero.py"


class ArtifactZeroTests(unittest.TestCase):
    def test_real_mounted_workspace_creation_and_verification(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as directory:
            output = Path(directory) / "gate.json"
            before = time.time()
            result = subprocess.run([
                sys.executable, str(HELPER), "--output", str(output),
                "--cohort", "TEST", "--continuation-of", "R48",
                "--floors", "4", "8", "8",
            ], capture_output=True, text=True)
            after = time.time()
            self.assertEqual(0, result.returncode, result.stderr)
            report = json.loads(result.stdout)
            gate = json.loads(output.read_text(encoding="utf-8"))
            self.assertTrue(report["hardPass"])
            self.assertLessEqual(report["mtimeDeltaSeconds"], 1)
            self.assertGreaterEqual(output.stat().st_mtime, before - 1)
            self.assertLessEqual(output.stat().st_mtime, after)
            self.assertEqual(20, gate["admittedRequiredOccurrences"])
            self.assertEqual(300, gate["deadlinesSeconds"]["adjudicatedConfig"])
            self.assertEqual(780, gate["deadlinesSeconds"]["publication"])
            self.assertNotIn("os.utime", HELPER.read_text(encoding="utf-8"))

    def test_existing_receipt_is_not_overwritten(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as directory:
            output = Path(directory) / "gate.json"
            output.write_text("sentinel", encoding="utf-8")
            result = subprocess.run([
                sys.executable, str(HELPER), "--output", str(output),
                "--cohort", "TEST", "--floors", "4", "8", "8",
            ], capture_output=True, text=True)
            self.assertNotEqual(0, result.returncode)
            self.assertEqual("sentinel", output.read_text(encoding="utf-8"))


if __name__ == "__main__":
    unittest.main()
