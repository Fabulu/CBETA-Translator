#!/usr/bin/env python3
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[1]
HERE = ROOT / "maintenance"
ENV = HERE / "dictionary_python_env.py"
START = HERE / "start_assigned_cohort.py"
LAUNCH = HERE / "launch_assigned_cohort.py"
SELECTOR = HERE / "last1500-public-depth/final-scope/full-regeneration-selector.json"
PRIOR = HERE / "non-iriya-v7-depth-regeneration-r64-prior-union-b.json"
ENTRIES = [
    ("t_17c1d8b4f105", "劫外", 7),
    ("t_1820fe9e6a50", "瓦解冰消", 6),
    ("t_1901868691a8", "呈漆器", 4),
]
RESERVES = [
    "t_16fa94f9bd79", "t_1707269fc3fc", "t_1793c3514a69",
    "t_17b2631fd3ed",
]


class CanonicalAssignedStarterTests(unittest.TestCase):
    def command(self, gate, output):
        command = [
            sys.executable, str(ENV), "--script", str(START), "--",
            "--cohort", "TEST-R65", "--timegate", str(gate),
            "--prior-union", str(PRIOR), "--selector", str(SELECTOR),
            "--case-load", "17", "--output-dir", str(output),
        ]
        for identity, term, floor in ENTRIES:
            command += ["--entry", identity, term, str(floor)]
        for identity in RESERVES:
            command += ["--reserve-id", identity]
        return command

    def test_mounted_workspace_artifact_zero_precedes_green_viability(self):
        with tempfile.TemporaryDirectory(dir=HERE) as raw:
            output = Path(raw)
            gate = output / "gate.json"
            result = subprocess.run(
                self.command(gate, output), cwd=ROOT, capture_output=True, text=True)
            self.assertEqual(0, result.returncode, result.stderr)
            doc = json.loads(gate.read_text(encoding="utf-8"))
            self.assertEqual("bounded-dictionary-timegate.v2", doc["schemaVersion"])
            self.assertTrue(doc["artifactZero"])
            self.assertEqual([7, 6, 4], doc["requiredFloors"])
            self.assertEqual(17, doc["adjudicatedCaseLoad"])
            self.assertEqual(
                [row[0] for row in ENTRIES],
                [row["id"] for row in doc["assignedLaunch"]["entries"]])
            prefix = "non-iriya-v7-depth-regeneration-test-r65"
            paths = [
                output / f"{prefix}-prior-union-b.json",
                output / f"{prefix}-selection-b.json",
                output / f"{prefix}-count-b.json",
                output / f"{prefix}-viability-checkpoint-b.json",
            ]
            self.assertTrue(all(path.is_file() for path in paths))
            self.assertTrue(all(gate.stat().st_mtime <= path.stat().st_mtime for path in paths))
            receipt = json.loads(paths[-1].read_text(encoding="utf-8"))
            self.assertTrue(receipt["hardPass"])
            before = {path: hashlib.sha256(path.read_bytes()).hexdigest()
                      for path in [gate, *paths]}
            again = subprocess.run(
                self.command(gate, output), cwd=ROOT, capture_output=True, text=True)
            self.assertNotEqual(0, again.returncode)
            self.assertIn("refusing to overwrite artifact zero", again.stderr)
            self.assertEqual(before, {
                path: hashlib.sha256(path.read_bytes()).hexdigest()
                for path in [gate, *paths]
            })

    def test_legacy_gate_cannot_launch_or_create_post_gate_artifacts(self):
        with tempfile.TemporaryDirectory(dir=HERE) as raw:
            output = Path(raw)
            gate = output / "legacy.json"
            gate.write_text(json.dumps({
                "schemaVersion": "bounded-dictionary-timegate.v1",
                "cohort": "TEST-R65", "startedEpoch": 1,
            }), encoding="utf-8")
            command = [
                sys.executable, str(ENV), "--script", str(LAUNCH), "--",
                "--cohort", "TEST-R65", "--timegate", str(gate),
                "--prior-union", str(PRIOR), "--selector", str(SELECTOR),
                "--output-dir", str(output),
            ]
            for identity, term, floor in ENTRIES:
                command += ["--entry", identity, term, str(floor)]
            for identity in RESERVES:
                command += ["--reserve-id", identity]
            result = subprocess.run(command, cwd=ROOT, capture_output=True, text=True)
            self.assertNotEqual(0, result.returncode)
            self.assertEqual([gate], list(output.iterdir()))


if __name__ == "__main__":
    unittest.main()
