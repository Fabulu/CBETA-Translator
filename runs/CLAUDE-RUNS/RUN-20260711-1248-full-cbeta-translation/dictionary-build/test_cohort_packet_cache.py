#!/usr/bin/env python3

import json
import tempfile
import unittest
from pathlib import Path

from attribution_packet import packet_input_sha256
from run_cohort_gate import PACKET_GENERATOR_VERSION, load_cached_packet


class CohortPacketCacheTests(unittest.TestCase):
    def test_reuses_only_exact_versioned_hash_set(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "packets.json"
            payload = {"generatorVersion": PACKET_GENERATOR_VERSION, "inputPacketSha256": {"t_a": "abc"}, "packets": []}
            path.write_text(json.dumps(payload), encoding="utf-8")
            self.assertEqual(payload, load_cached_packet(path, {"t_a": "abc"}))
            self.assertIsNone(load_cached_packet(path, {"t_a": "changed"}))
            self.assertIsNone(load_cached_packet(path, {"t_a": "abc", "t_b": "def"}))

    def test_prose_only_change_reuses_but_evidence_change_invalidates(self):
        entry = {
            "Id": "t_a", "SourceTerm": "話", "Senses": [{
                "Explanation": "first prose", "Occurrences": [{
                    "RelPath": "X/X01.xml", "FromLb": "0001a01",
                    "Kwic": "師云話", "MasterName": "Zhaozhou Congshen",
                }],
            }],
        }
        before = packet_input_sha256(entry)
        entry["Senses"][0]["Explanation"] = "repaired prose"
        self.assertEqual(before, packet_input_sha256(entry))
        occurrence = entry["Senses"][0]["Occurrences"].pop()
        entry["Senses"].append({"Explanation": "second sense", "Occurrences": [occurrence]})
        self.assertNotEqual(before, packet_input_sha256(entry))
        entry["Senses"].pop()
        entry["Senses"][0]["Occurrences"].append(occurrence)
        entry["Senses"][0]["Occurrences"][0]["MasterName"] = "Mazu Daoyi"
        self.assertNotEqual(before, packet_input_sha256(entry))

    def test_rejects_old_or_malformed_packet(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "packets.json"
            path.write_text('{"generatorVersion":1,"inputEntrySha256":{"t_a":"abc"}}', encoding="utf-8")
            self.assertIsNone(load_cached_packet(path, {"t_a": "abc"}))
            path.write_text("not json", encoding="utf-8")
            self.assertIsNone(load_cached_packet(path, {"t_a": "abc"}))


if __name__ == "__main__":
    unittest.main()
