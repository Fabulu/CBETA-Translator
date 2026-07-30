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

from atomic_write import atomic_write_json

ROOT = Path(__file__).resolve().parent.parent
WATCHDOG = ROOT / "maintenance/construction_start_watchdog.py"
ENGINE = ROOT / "maintenance/generic_bounded_constructor.py"
FIXTURE_IDS = ["t_0f8df3105c35", "t_0f97bfab265c", "t_0fb97dffe2bc"]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


class GenericBoundedConstructorIntegrationTest(unittest.TestCase):
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
            atomic_write_json(timegate, {"cohort": "FIXTURE", "startedEpoch": started})
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
            for identity in FIXTURE_IDS:
                self.assertTrue((base / "output" / identity / "entry.v2.json").is_file())


if __name__ == "__main__":
    unittest.main()
