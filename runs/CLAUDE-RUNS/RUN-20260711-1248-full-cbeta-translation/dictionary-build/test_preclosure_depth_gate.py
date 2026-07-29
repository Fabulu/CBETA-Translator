#!/usr/bin/env python3
import unittest

from preclosure_depth_gate import floor_failure

class PreclosureDepthGateTests(unittest.TestCase):
    def test_twenty_hits_two_occurrences_fails(self):
        self.assertTrue(floor_failure(20, 2))

    def test_hundred_hits_four_occurrences_fails(self):
        self.assertTrue(floor_failure(100, 4))

    def test_floor_values_pass(self):
        self.assertFalse(floor_failure(20, 4))
        self.assertFalse(floor_failure(100, 6))

if __name__ == "__main__":
    unittest.main()
