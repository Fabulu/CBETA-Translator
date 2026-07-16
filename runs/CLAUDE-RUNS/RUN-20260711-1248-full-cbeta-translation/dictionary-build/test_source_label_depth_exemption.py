#!/usr/bin/env python3

import unittest

from audit_depth_sense import without_authoritative_source_label


class SourceLabelDepthExemptionTest(unittest.TestCase):
    def test_only_exact_authoritative_title_is_removed(self):
        rel = "X/X66/X66n1297.xml"
        label = "Mirror of the Lineage Dharma Grove (宗鑑法林)"
        note = f"Source record ({rel}). {label}: Caotang Shanqing speaks."
        self.assertEqual(
            "Caotang Shanqing speaks.",
            without_authoritative_source_label(note, {rel: label}),
        )
        self.assertIn(
            "Dharma",
            without_authoritative_source_label(
                f"Source record ({rel}). Wrong title: Dharma is explanatory prose.", {rel: label}
            ),
        )


if __name__ == "__main__":
    unittest.main()
