import unittest

from apply_attribution_decisions import RUNGS, validate_actor


class DecisionValidationTests(unittest.TestCase):
    def test_named_actor_xor(self):
        self.assertEqual([], validate_actor({"MasterName": "Zhaozhou Congshen", "AttributionNote": "Record of Zhaozhou: Zhaozhou Congshen answers."}))
        self.assertTrue(validate_actor({"MasterName": "Zhaozhou Congshen", "ActorAttribution": {"Status": "impersonal"}, "AttributionNote": "x"}))

    def test_reviewed_unnamed_master_is_forbidden(self):
        decision = {"ActorAttribution": {"Status": "reviewed-unnamed", "Kind": "master", "ActorLabel": "unnamed master", "ActorRole": "speaker", "ReviewedBy": "reviewer", "ReviewedUtc": "now", "RungsChecked": RUNGS}, "AttributionNote": "source: unnamed master speaks"}
        self.assertIn("an unnamed master is forbidden", validate_actor(decision))

    def test_reviewed_non_master_requires_six_rungs(self):
        decision = {"ActorAttribution": {"Status": "reviewed-unnamed", "Kind": "monk", "ActorLabel": "unnamed monk", "ActorRole": "questioner", "ReviewedBy": "reviewer", "ReviewedUtc": "now", "RungsChecked": RUNGS}, "AttributionNote": "source: unnamed monk asks"}
        self.assertEqual([], validate_actor(decision))

    def test_note_must_repeat_exact_actor_and_expected_source(self):
        decision = {"MasterName": "Zhaozhou Congshen", "AttributionNote": "Zhaozhou Congshen answers."}
        self.assertIn("AttributionNote must contain ExpectedSourceTitle", validate_actor(decision, "趙州錄"))
        self.assertIn("AttributionNote must contain the exact MasterName", validate_actor({"MasterName": "Zhaozhou Congshen", "AttributionNote": "The master answers."}))


if __name__ == "__main__":
    unittest.main()
