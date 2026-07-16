import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import zc


class BridgedSearchTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        root = Path(self.temporary.name)
        (root / "X").mkdir()
        (root / "X" / "one.xml").write_text(
            '<TEI><text><body><lb ed="X" n="1a01"/>生、老、病、死'
            '<lb ed="X" n="1a02"/>甲<lb ed="X" n="1a03"/>乙。丙'
            '<note>生老病死</note><app><rdg>生老病死</rdg></app>'
            '甲。乙甲甲甲甲</body></text></TEI>',
            encoding="utf-8",
        )
        self.allow = root / "allow.json"
        self.allow.write_text(json.dumps({
            "texts": ["X/one.xml"],
            "work_ids": {"X/one.xml": "work-one"},
        }), encoding="utf-8")
        self.patches = [
            patch.object(zc, "CORPUS", str(root)),
            patch.object(zc, "ALLOW", str(self.allow)),
            patch.object(zc, "_DISK_CACHE_ENABLED", False),
        ]
        for active_patch in self.patches:
            active_patch.start()
        zc._cache.clear()
        zc._raw_position_index_for_rel.cache_clear()

    def tearDown(self):
        zc._cache.clear()
        zc._raw_position_index_for_rel.cache_clear()
        for active_patch in reversed(self.patches):
            active_patch.stop()
        self.temporary.cleanup()

    def test_punctuation_bridges_for_discovery_but_exact_mode_is_unchanged(self):
        self.assertEqual(zc.count("生老病死")["hits"], 0)
        self.assertEqual(zc.find("X/one.xml", "生老病死"), [])
        self.assertFalse(zc.verify("X/one.xml", "生老病死")["ok"])
        self.assertEqual(zc.bridged_count("生老病死")["hits"], 1)
        hit = zc.bridged_find("X/one.xml", "生老病死")[0]
        self.assertEqual(hit["bridged"], "生、老、病、死")

    def test_tags_and_line_breaks_bridge_in_both_modes(self):
        self.assertEqual(zc.count("甲乙")["hits"], 1)
        self.assertTrue(zc.verify("X/one.xml", "甲乙")["ok"])
        self.assertEqual(zc.bridged_count("甲乙")["hits"], 2)

    def test_bridged_window_exposes_punctuation_false_positive(self):
        hits = zc.bridged_find("X/one.xml", "乙丙")
        self.assertEqual(len(hits), 1)
        self.assertEqual(hits[0]["bridged"], "乙。丙")
        self.assertIn("。", hits[0]["window"])
        self.assertFalse(zc.verify("X/one.xml", "乙丙")["ok"])

    def test_notes_and_apparatus_remain_excluded_from_dictionary_discovery(self):
        # The only visible form is punctuation-split; exact note/app strings
        # would inflate this above one if dictionary extraction included them.
        self.assertEqual(zc.bridged_count("生老病死")["hits"], 1)

    def test_strip_ranges_grams_and_nonoverlapping_count(self):
        self.assertEqual(
            zc.bridged_grams("㐀、㐁䷀一豈"),
            ["㐀㐁", "一豈"],
        )
        # str.count semantics are greedy/non-overlapping for self-pairs.
        self.assertEqual(zc.bridged_count("甲甲")["hits"], 2)


if __name__ == "__main__":
    unittest.main()
