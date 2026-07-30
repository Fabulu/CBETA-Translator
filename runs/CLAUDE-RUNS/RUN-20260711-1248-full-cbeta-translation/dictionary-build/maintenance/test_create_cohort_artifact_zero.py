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
                "--floors", "4", "8", "8", "--case-load", "20",
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
            self.assertEqual(20, gate["adjudicatedCaseLoad"])
            self.assertEqual(420, gate["deadlinesSeconds"]["adjudicatedConfig"])
            self.assertEqual(960, gate["deadlinesSeconds"]["publication"])
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

    def test_explicit_replacement_case_load_and_low_rejection(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as directory:
            omitted = Path(directory) / "missing-load.json"
            result = subprocess.run([
                sys.executable, str(HELPER), "--output", str(omitted),
                "--cohort", "TEST", "--floors", "4", "8", "8",
            ], capture_output=True, text=True)
            self.assertNotEqual(0, result.returncode)
            self.assertFalse(omitted.exists())
            output = Path(directory) / "expanded.json"
            result = subprocess.run([
                sys.executable, str(HELPER), "--output", str(output),
                "--cohort", "TEST", "--floors", "8", "4", "7", "--case-load", "23",
            ], capture_output=True, text=True)
            self.assertEqual(0, result.returncode, result.stderr)
            gate = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual(23, gate["adjudicatedCaseLoad"])
            self.assertEqual(456, gate["deadlinesSeconds"]["adjudicatedConfig"])
            low = Path(directory) / "low.json"
            result = subprocess.run([
                sys.executable, str(HELPER), "--output", str(low),
                "--cohort", "TEST", "--floors", "8", "4", "7", "--case-load", "18",
            ], capture_output=True, text=True)
            self.assertNotEqual(0, result.returncode)
            self.assertFalse(low.exists())


if __name__ == "__main__":
    unittest.main()
