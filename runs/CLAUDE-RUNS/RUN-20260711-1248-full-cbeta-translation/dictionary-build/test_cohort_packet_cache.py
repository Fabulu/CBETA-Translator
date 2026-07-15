#!/usr/bin/env python3

import json
import tempfile
import unittest
from pathlib import Path

from run_cohort_gate import PACKET_GENERATOR_VERSION, load_cached_packet


class CohortPacketCacheTests(unittest.TestCase):
    def test_reuses_only_exact_versioned_hash_set(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "packets.json"
            payload = {"generatorVersion": PACKET_GENERATOR_VERSION, "inputEntrySha256": {"t_a": "abc"}, "packets": []}
            path.write_text(json.dumps(payload), encoding="utf-8")
            self.assertEqual(payload, load_cached_packet(path, {"t_a": "abc"}))
            self.assertIsNone(load_cached_packet(path, {"t_a": "changed"}))
            self.assertIsNone(load_cached_packet(path, {"t_a": "abc", "t_b": "def"}))

    def test_rejects_old_or_malformed_packet(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "packets.json"
            path.write_text('{"generatorVersion":1,"inputEntrySha256":{"t_a":"abc"}}', encoding="utf-8")
            self.assertIsNone(load_cached_packet(path, {"t_a": "abc"}))
            path.write_text("not json", encoding="utf-8")
            self.assertIsNone(load_cached_packet(path, {"t_a": "abc"}))


if __name__ == "__main__":
    unittest.main()
