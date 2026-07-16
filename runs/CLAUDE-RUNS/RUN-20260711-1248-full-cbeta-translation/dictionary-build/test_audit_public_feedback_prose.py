#!/usr/bin/env python3

import unittest

from audit_public_feedback import oversized_kwics, repeated_sentence


class RepeatedProseTest(unittest.TestCase):
    def test_oversized_kwic_requires_reader_facing_recut(self):
        sense = {"Occurrences": [{"Kwic": "短句"}, {"Kwic": "字" * 801}]}
        self.assertEqual([{"occurrence": 1, "length": 801}], oversized_kwics(sense))

    def test_rejects_repeated_count_sentence(self):
        sentence = "Frozen-corpus concordance: 137 exact hits in 87 files representing 86 works."
        self.assertEqual(sentence, repeated_sentence(f"{sentence} {sentence}"))

    def test_allows_distinct_substantive_sentences(self):
        self.assertIsNone(repeated_sentence(
            "The first witness states the formula directly. The second witness raises it as an inherited case."
        ))


if __name__ == "__main__":
    unittest.main()
