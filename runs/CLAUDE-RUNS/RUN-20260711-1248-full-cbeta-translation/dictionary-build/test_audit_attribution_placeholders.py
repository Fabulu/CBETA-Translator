#!/usr/bin/env python3

import unittest

from audit_attribution import PLACEHOLDER_ACTOR_RE


class PlaceholderActorTest(unittest.TestCase):
    def test_rejects_generated_actor_placeholders(self):
        for value in (
            "the fully reviewed source voice",
            "reviewed source voice",
            "the cited voice",
            "a cited figure",
            "the presiding speaker",
            "verse voice",
        ):
            with self.subTest(value=value):
                self.assertIsNotNone(PLACEHOLDER_ACTOR_RE.search(value))

    def test_keeps_specific_or_honestly_unnamed_labels(self):
        for value in (
            "the unnamed monk who asks the question",
            "compiler narration about Xiuyun Wei",
            "Huanglong Huinan",
        ):
            with self.subTest(value=value):
                self.assertIsNone(PLACEHOLDER_ACTOR_RE.search(value))


if __name__ == "__main__":
    unittest.main()
