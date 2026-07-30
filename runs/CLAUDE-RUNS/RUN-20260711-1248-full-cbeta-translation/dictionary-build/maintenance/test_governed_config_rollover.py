#!/usr/bin/env python3
import copy
import json
import subprocess
import sys
from pathlib import Path
import tempfile
import unittest

from maintenance.cohort_checkpoint_watchdog import audit_commands
from maintenance.governed_config_rollover import (
    assert_authority, file_sha, rebind)

ROOT=Path(__file__).resolve().parents[1]
CONFIG=ROOT/"maintenance/non-iriya-v7-depth-regeneration-r61-constructor-config-b.json"
WRAPPER=ROOT/"maintenance/dictionary_python_env.py"


class GovernedConfigRolloverTests(unittest.TestCase):
    def test_changed_engine_sha_is_rebound_with_wrapper_authority(self):
        old=json.loads(CONFIG.read_text())
        with tempfile.TemporaryDirectory(dir=ROOT/"maintenance") as raw:
            engine=Path(raw)/"engine.py"; engine.write_text("print('new')\n")
            config,audit=rebind(
                old,cohort="R99",started_epoch=99.0,
                old_path_token="r61-",new_path_token="r99-",
                engine=engine,wrapper=WRAPPER,
                config_path=Path(raw)/"config.json",allowed_root=Path(raw),
                audit_epoch=100.0)
            self.assertNotEqual(old["engineSha256"],config["engineSha256"])
            self.assertEqual(file_sha(engine),config["engineSha256"])
            self.assertEqual(file_sha(WRAPPER),
                audit["authorizedToolBindings"]["wrapperSha256"])
            self.assertEqual(100.0,audit["commands"][0]["epoch"])
            self.assertEqual(str((Path(raw)/"config.json").resolve()),
                audit["commands"][0]["argv"][-3])
            self.assertEqual(str(Path(raw).resolve()),
                audit["commands"][0]["argv"][-1])

    def test_stale_tool_sha_and_stale_path_fail_exhaustively(self):
        old=json.loads(CONFIG.read_text())
        with tempfile.TemporaryDirectory(dir=ROOT/"maintenance") as raw:
            engine=Path(raw)/"engine.py"; engine.write_text("print('new')\n")
            config,audit=rebind(
                old,cohort="R99",started_epoch=99.0,
                old_path_token="r61-",new_path_token="r99-",
                engine=engine,wrapper=WRAPPER,
                config_path=Path(raw)/"config.json",allowed_root=Path(raw),
                audit_epoch=100.0)
            stale=copy.deepcopy(config); stale["paths"]["research"]="maintenance/r61-stale.json"
            with self.assertRaisesRegex(ValueError,"stale governed binding"):
                assert_authority(stale,audit,cohort="R99",new_path_token="r99-",
                    engine=engine,wrapper=WRAPPER)
            stale=copy.deepcopy(config); stale["engineSha256"]="0"*64
            with self.assertRaisesRegex(ValueError,"tool path/SHA authority mismatch"):
                assert_authority(stale,audit,cohort="R99",new_path_token="r99-",
                    engine=engine,wrapper=WRAPPER)

    def test_absent_and_prestart_audit_epochs_fail(self):
        old=json.loads(CONFIG.read_text())
        with tempfile.TemporaryDirectory(dir=ROOT/"maintenance") as raw:
            engine=Path(raw)/"engine.py"; engine.write_text("pass\n")
            common=dict(
                old_config=old,cohort="R99",started_epoch=99.0,
                old_path_token="r61-",new_path_token="r99-",
                engine=engine,wrapper=WRAPPER,
                config_path=Path(raw)/"config.json",allowed_root=Path(raw))
            with self.assertRaisesRegex(ValueError,">= startedEpoch"):
                rebind(**common,audit_epoch=None)
            with self.assertRaisesRegex(ValueError,">= startedEpoch"):
                rebind(**common,audit_epoch=98.9)

    def test_real_rollover_audit_reaches_wrapper_engine_contract(self):
        old=json.loads(CONFIG.read_text())
        with tempfile.TemporaryDirectory(dir=ROOT/"maintenance") as raw:
            base=Path(raw); marker=base/"marker.json"; config_path=base/"config.json"
            engine=base/"engine.py"
            engine.write_text(
                "import json,sys\n"
                f"open({str(marker)!r},'w').write(json.dumps(sys.argv[1:]))\n")
            config,audit=rebind(
                old,cohort="R99",started_epoch=99.0,
                old_path_token="r61-",new_path_token="r99-",
                engine=engine,wrapper=WRAPPER,config_path=config_path,
                allowed_root=base,audit_epoch=100.0)
            config_path.write_text(json.dumps(config))
            audit_path=base/"audit.json"; audit_path.write_text(json.dumps(audit))
            command=audit["commands"][0]["argv"]
            audit_commands(audit_path,99.0,command)
            completed=subprocess.run(command,cwd=ROOT,capture_output=True,text=True)
            self.assertEqual(0,completed.returncode,completed.stderr)
            self.assertEqual(
                ["--config",str(config_path.resolve()),
                 "--allowed-build-root",str(base.resolve())],
                json.loads(marker.read_text()))


if __name__=="__main__":
 unittest.main()
