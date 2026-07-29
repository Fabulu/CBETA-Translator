#!/usr/bin/env python3

import json
import tempfile
import unittest
from pathlib import Path
from unittest import mock

from atomic_write import atomic_write_json, atomic_write_text


class AtomicWriteTests(unittest.TestCase):
    def test_interruption_before_replace_preserves_prior_target_bytes(self):
        with tempfile.TemporaryDirectory() as temporary:
            target = Path(temporary) / "checkpoint.json"
            prior = b'{"generation":"prior"}\\n'
            target.write_bytes(prior)

            with mock.patch("atomic_write.os.replace", side_effect=KeyboardInterrupt):
                with self.assertRaises(KeyboardInterrupt):
                    atomic_write_text(target, '{"generation":"new"}\n')

            self.assertEqual(prior, target.read_bytes())
            self.assertEqual([], list(Path(temporary).glob(".checkpoint.json.*.tmp")))

    def test_json_write_replaces_complete_document(self):
        with tempfile.TemporaryDirectory() as temporary:
            target = Path(temporary) / "manifest.json"
            atomic_write_json(target, {"rows": [1, 2], "complete": True})
            self.assertEqual(
                {"rows": [1, 2], "complete": True},
                json.loads(target.read_text(encoding="utf-8")),
            )
            self.assertTrue(target.read_bytes().endswith(b"\n"))


if __name__ == "__main__":
    unittest.main()
