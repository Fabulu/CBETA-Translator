#!/usr/bin/env python3
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parent.parent
WRAPPER = ROOT / "maintenance/dictionary_python_env.py"


class DictionaryPythonEnvironmentTests(unittest.TestCase):
    def test_clean_external_cwd_resolves_dictionary_modules(self):
        with tempfile.TemporaryDirectory(dir=ROOT / "maintenance") as raw:
            fixture = Path(raw) / "fixture.py"
            output = Path(raw) / "result.json"
            fixture.write_text(
                "import json,os,sys\n"
                "import zc\n"
                "import atomic_write\n"
                f"open({str(output)!r},'w',encoding='utf-8').write("
                "json.dumps({'cwd':os.getcwd(),'zc':zc.__file__,"
                "'atomic':atomic_write.__file__,'path0':sys.path[0]}))\n",
                encoding="utf-8",
            )
            environment = os.environ.copy()
            environment.pop("PYTHONPATH", None)
            completed = subprocess.run(
                [sys.executable, str(WRAPPER), "--script", str(fixture)],
                cwd="/tmp",
                env=environment,
                capture_output=True,
                text=True,
            )
            self.assertEqual(0, completed.returncode, completed.stderr)
            result = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual(str(ROOT), result["cwd"])
            self.assertEqual(str(ROOT / "zc.py"), str(Path(result["zc"]).resolve()))
            self.assertEqual(str(ROOT / "atomic_write.py"), str(Path(result["atomic"]).resolve()))

    def test_script_outside_dictionary_root_is_rejected(self):
        with tempfile.TemporaryDirectory() as raw:
            fixture = Path(raw) / "outside.py"
            fixture.write_text("raise SystemExit(0)\n", encoding="utf-8")
            completed = subprocess.run(
                [sys.executable, str(WRAPPER), "--script", str(fixture)],
                capture_output=True,
                text=True,
            )
            self.assertNotEqual(0, completed.returncode)
            self.assertIn("escapes active dictionary root", completed.stderr)


if __name__ == "__main__":
    unittest.main()
