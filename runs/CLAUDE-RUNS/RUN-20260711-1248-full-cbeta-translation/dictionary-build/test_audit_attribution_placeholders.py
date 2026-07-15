#!/usr/bin/env python3

import unittest
from collections import Counter

from audit_attribution import (
    DUPLICATED_NOTE_PREFIX_RE,
    PLACEHOLDER_ACTOR_RE,
    anonymous_actor_collapse_failure,
    uniform_actor_placeholder_failure,
)


class PlaceholderActorTest(unittest.TestCase):
    def test_rejects_duplicated_actor_note_prefix(self):
        self.assertIsNotNone(DUPLICATED_NOTE_PREFIX_RE.search(
            "Foyan Qingyuan: Foyan Qingyuan: Record of Foyan says..."
        ))
        self.assertIsNone(DUPLICATED_NOTE_PREFIX_RE.search(
            "Foyan Qingyuan: Record of Foyan says..."
        ))

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

    def test_rejects_a906_930_style_uniform_actor_cohort(self):
        signature = (
            "narrated", "documentary or narrator voice",
            "documentary or narrator voice after complete-unit review", "compiler",
        )
        failure = uniform_actor_placeholder_failure(25, 0, Counter({signature: 168}))
        self.assertIsNotNone(failure)
        self.assertEqual("batch-uniform-actor-placeholder", failure["kind"])

    def test_does_not_claim_that_a_mixed_cohort_is_invalid(self):
        signatures = Counter({("narrated", "compiler", "compiler", "compiler"): 4,
                              ("impersonal", "no-human", "no human actor", "none"): 1})
        self.assertIsNone(uniform_actor_placeholder_failure(5, 0, signatures))
        self.assertIsNone(uniform_actor_placeholder_failure(5, 1, Counter({("x",): 5})))

    def test_rejects_varied_labels_that_still_collapse_to_narration(self):
        signatures = Counter({
            ("narrated", "compiler", "compiler A", "compiler"): 25,
            ("narrated", "documentary", "documentary B", "compiler"): 22,
            ("reviewed-unnamed", "monk", "unnamed monk", "questioner"): 11,
        })
        failure = anonymous_actor_collapse_failure(10, 0, signatures)
        self.assertIsNotNone(failure)
        self.assertEqual("batch-anonymous-actor-collapse", failure["kind"])

    def test_allows_a_cohort_only_when_it_has_named_utterers_or_is_small(self):
        mostly_narrated = Counter({("narrated",): 40, ("reviewed-unnamed",): 5})
        self.assertIsNone(anonymous_actor_collapse_failure(10, 1, mostly_narrated))
        mixed = Counter({("narrated",): 20, ("reviewed-unnamed",): 20})
        self.assertIsNotNone(anonymous_actor_collapse_failure(10, 0, mixed))
        self.assertIsNone(anonymous_actor_collapse_failure(5, 0, mixed))


if __name__ == "__main__":
    unittest.main()
