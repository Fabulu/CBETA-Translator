#!/usr/bin/env python3

import unittest

from normalize_attribution_notes import actor_prefix, normalize


class ActorPrefixTest(unittest.TestCase):
    def test_named_master(self):
        self.assertEqual("Zhaozhou Congshen", actor_prefix({"MasterName": "Zhaozhou Congshen"}))

    def test_named_non_master(self):
        self.assertEqual("Pei Xiu", actor_prefix({"ActorAttribution": {
            "Status": "identified-non-master", "ActorLabel": "Pei Xiu"
        }}))

    def test_generic_non_master_is_not_resolved(self):
        self.assertIsNone(actor_prefix({"ActorAttribution": {
            "Status": "identified-non-master", "ActorLabel": "the preface author"
        }}))

    def test_reviewed_unnamed_is_explicit(self):
        self.assertEqual("The source does not name unnamed questioning monk", actor_prefix({
            "ActorAttribution": {"Status": "reviewed-unnamed", "ActorLabel": "the unnamed questioning monk"}
        }))

    def test_record_owner_cannot_hide_in_reviewed_unnamed(self):
        self.assertIsNone(actor_prefix({"ActorAttribution": {
            "Status": "reviewed-unnamed", "ActorLabel": "speaking record owner resolved from the complete section"
        }}))

    def test_source_prefix_is_canonical_and_idempotent(self):
        row = {
            "RelPath": "X/X80/X80n1565.xml",
            "MasterName": "Zhaozhou Congshen",
            "AttributionNote": (
                "Source record (X/X80/X80n1565.xml). Source record (五燈會元). "
                "Exact actor: Zhaozhou Congshen."
            ),
        }
        note, changes = normalize(row)
        self.assertEqual(
            "Source record (X/X80/X80n1565.xml). Exact actor: Zhaozhou Congshen.",
            note,
        )
        self.assertIn("source-canonicalized", changes)
        row["AttributionNote"] = note
        again, changes = normalize(row)
        self.assertEqual(note, again)
        self.assertEqual([], changes)


if __name__ == "__main__":
    unittest.main()
