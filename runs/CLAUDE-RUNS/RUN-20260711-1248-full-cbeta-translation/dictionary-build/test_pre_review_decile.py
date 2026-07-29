import sys
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory
from unittest.mock import patch

import pre_review_decile


class PreReviewTimegateTests(unittest.TestCase):
    def test_pre_review_checks_review_phase(self):
        with TemporaryDirectory() as tmp:
            output = Path(tmp) / "review.json"
            receipt = Path(tmp) / "timegate.json"
            argv = [
                "pre_review_decile.py",
                "--output",
                str(output),
                "--timegate",
                str(receipt),
                "entry",
            ]
            with (
                patch.object(sys, "argv", argv),
                patch.object(pre_review_decile.subprocess, "run") as run,
            ):
                run.return_value.returncode = 124
                self.assertEqual(pre_review_decile.main(), 124)
            command = run.call_args.args[0]
            self.assertEqual(command[command.index("--phase") + 1], "review")


if __name__ == "__main__":
    unittest.main()
