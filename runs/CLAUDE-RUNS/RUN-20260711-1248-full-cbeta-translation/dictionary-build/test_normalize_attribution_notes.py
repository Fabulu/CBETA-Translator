#!/usr/bin/env python3

import unittest

from normalize_attribution_notes import actor_prefix


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


if __name__ == "__main__":
    unittest.main()
