#!/usr/bin/env python3
import sys
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT))
from construct_r11_clean_regeneration_c import semantic_projection


class SemanticProjectionTest(unittest.TestCase):
    def test_alternate_targets_do_not_become_search_aliases(self):
        alternate, aliases=semantic_projection({
            "also":["trace a circle"],
            "aliases":["make a circular figure"],
        })
        self.assertEqual(["trace a circle"],alternate)
        self.assertEqual(["make a circular figure"],aliases)

    def test_missing_optional_lists_are_empty(self):
        self.assertEqual(([],[]),semantic_projection({}))


if __name__=="__main__":
    unittest.main()
