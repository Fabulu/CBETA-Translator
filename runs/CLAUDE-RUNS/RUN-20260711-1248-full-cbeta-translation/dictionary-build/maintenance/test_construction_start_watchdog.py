#!/usr/bin/env python3
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

HERE = Path(__file__).resolve().parent
WATCHDOG = HERE / "construction_start_watchdog.py"


class ConstructionStartWatchdogTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.receipt = self.root / "start.json"
        self.timegate = self.root / "timegate.json"
        self.timegate.write_text(
            json.dumps({"cohort": "TEST", "startedEpoch": 1000.0}), encoding="utf-8"
        )
        self.preflight = self.root / "preflight.json"
        self.preflight.write_text(json.dumps({"hardPass": True}), encoding="utf-8")
        self.constructor = self.root / "constructor.py"
        self.constructor.write_text("#!/usr/bin/env python3\npass\n", encoding="utf-8")
        self.artifacts = {}
        for kind in ("union", "selection", "count", "research"):
            path = self.root / f"{kind}.json"
            path.write_text(json.dumps({"kind": kind}), encoding="utf-8")
            self.artifacts[kind] = path
        self.command_audit = self.root / "commands.json"
        self.write_command_audit(1001.0)

    def tearDown(self):
        self.temp.cleanup()

    def run_watchdog(self, *args):
        return subprocess.run(
            [sys.executable, str(WATCHDOG), *map(str, args)],
            capture_output=True,
            text=True,
        )

    def write_command_audit(self, epoch):
        self.command_audit.write_text(
            json.dumps(
                {
                    "complete": True,
                    "commands": [{"epoch": epoch, "command": "bounded cohort setup"}],
                }
            ),
            encoding="utf-8",
        )

    def invoke_args(self, now="1120"):
        values = [
            "invoke",
            "--timegate", self.timegate,
            "--receipt", self.receipt,
            "--constructor", self.constructor,
            "--preflight-receipt", self.preflight,
            "--command-audit", self.command_audit,
        ]
        for kind, path in self.artifacts.items():
            values.extend(["--cohort-artifact", f"{kind}={path}"])
        values.extend(
            [
                "--cohort-artifact", f"preflight={self.preflight}",
                "--ids", "t_one", "t_two",
                "--now-epoch", now,
                "--", sys.executable, self.constructor,
            ]
        )
        return values

    def test_missing_marker_fails_closed(self):
        result = self.run_watchdog("check", "--receipt", self.receipt)
        self.assertEqual(124, result.returncode)
        self.assertTrue(Path(str(self.receipt) + ".fail-closed.json").exists())

    def test_unexecuted_draft_is_not_a_start(self):
        self.assertTrue(self.constructor.exists())
        result = self.run_watchdog("check", "--receipt", self.receipt)
        self.assertEqual(124, result.returncode)
        self.assertIn("receipt is missing", result.stderr)

    def test_late_marker_fails_before_invocation(self):
        result = self.run_watchdog(*self.invoke_args(now="1120.001"))
        self.assertEqual(124, result.returncode)
        self.assertFalse(self.receipt.exists())

    def test_valid_timely_invocation_writes_verifiable_receipt(self):
        result = self.run_watchdog(*self.invoke_args())
        self.assertEqual(0, result.returncode, result.stderr)
        data = json.loads(self.receipt.read_text(encoding="utf-8"))
        self.assertEqual(True, data["invocationAttempted"])
        self.assertEqual(True, data["receiptFirstVerified"])
        self.assertEqual("completed", data["processState"])
        self.assertEqual(["t_one", "t_two"], data["ids"])
        checked = self.run_watchdog("check", "--receipt", self.receipt)
        self.assertEqual(0, checked.returncode, checked.stderr)

    def test_pre_receipt_artifact_is_rejected(self):
        os.utime(self.artifacts["union"], (999.0, 999.0))
        result = self.run_watchdog(*self.invoke_args())
        self.assertEqual(124, result.returncode)
        self.assertIn("pre-receipt artifact rejected", result.stderr)

    def test_pre_receipt_command_is_rejected(self):
        self.write_command_audit(999.0)
        result = self.run_watchdog(*self.invoke_args())
        self.assertEqual(124, result.returncode)
        self.assertIn("pre-receipt command rejected", result.stderr)


if __name__ == "__main__":
    unittest.main()
