import unittest

import attribution_triage


class ReviewedExceptionTests(unittest.TestCase):
    def test_reviewed_unnamed_non_master_uses_status(self):
        occurrence = {"ActorAttribution": {"Status": "reviewed-unnamed", "Kind": "monk", "RungsChecked": ["line"]}}
        self.assertTrue(attribution_triage.reviewed_exception(occurrence))

    def test_unnamed_master_never_counts_complete(self):
        occurrence = {"ActorAttribution": {"Status": "reviewed-unnamed", "Kind": "master", "RungsChecked": ["line"]}}
        self.assertFalse(attribution_triage.reviewed_exception(occurrence))

    def test_impersonal_requires_grammar_evidence(self):
        self.assertFalse(attribution_triage.reviewed_exception({"ActorAttribution": {"Status": "impersonal"}}))
        self.assertTrue(attribution_triage.reviewed_exception({"ActorAttribution": {"Status": "impersonal", "GrammarEvidence": "The sentence is governed by the text says."}}))


if __name__ == "__main__":
    unittest.main()
