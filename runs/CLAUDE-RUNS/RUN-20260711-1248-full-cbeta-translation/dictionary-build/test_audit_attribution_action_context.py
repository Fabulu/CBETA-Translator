import unittest

from audit_attribution import (
    EXPLICIT_MASTER_ACTION,
    EXPLICIT_MASTER_TURN,
    NARRATOR_REPORT_NOTE,
    RAISED_RECORD_NOTE,
    has_governed_action_performer_context,
    narrated_action_performer_missing,
)


class GovernedActionPerformerContextTests(unittest.TestCase):
    def test_master_instructs_assembly_saying_is_a_speech_turn(self):
        self.assertIsNotNone(EXPLICIT_MASTER_TURN.search("師示眾云：望州亭與汝相見了也"))

    def test_identified_unlinked_master_with_closed_role_and_proof_passes(self):
        actor = {
            "Status": "identified-unlinked-master",
            "ActorLabel": "Tianzhu Chonghui",
            "Roles": ["person-described", "action-performer"],
            "GrammarEvidence": "The complete section identifies this master as the action performer.",
        }
        self.assertTrue(has_governed_action_performer_context([], [actor]))

    def test_unproved_or_nonmaster_context_does_not_pass(self):
        actor = {
            "Status": "identified-non-master",
            "ActorLabel": "an unnamed monk",
            "Roles": ["person-described"],
            "GrammarEvidence": "too short",
        }
        self.assertFalse(has_governed_action_performer_context([], [actor]))

    def test_current500_prefix_patterns_fail_when_only_recorder_is_recorded(self):
        actor = {
            "Status": "narrated",
            "ActorLabel": "the encounter or address recorder",
            "GrammarEvidence": "The reviewed literal boundary '上堂' fixes the recorder as the exact headword voice.",
        }
        for term in ("提起坐具", "驀豎拂子", "豎拄杖"):
            with self.subTest(term=term):
                self.assertTrue(narrated_action_performer_missing(term, actor, [], []))

    def test_context_master_performer_passes(self):
        actor = {"Status": "narrated", "GrammarEvidence": "The record narrates the gesture."}
        contexts = [{"MasterName": "Yinyuan Longqi", "Roles": ["performer"]}]
        self.assertFalse(narrated_action_performer_missing("豎拄杖", actor, contexts, []))

    def test_person_described_without_action_performer_fails(self):
        actor = {"Status": "narrated", "GrammarEvidence": "The record narrates the seated action."}
        contexts = [{"MasterName": "Zhaozhou Congshen", "Roles": ["person-described"]}]
        self.assertTrue(narrated_action_performer_missing("端坐", actor, contexts, []))

    def test_explicit_action_performer_role_passes(self):
        actor = {"Status": "narrated", "GrammarEvidence": "The record narrates the seated action."}
        contexts = [{
            "MasterName": "Zhaozhou Congshen",
            "Roles": ["person-described", "action-performer"],
        }]
        self.assertFalse(narrated_action_performer_missing("端坐", actor, contexts, []))

    def test_identified_nonmaster_context_performer_passes(self):
        actor = {"Status": "narrated", "GrammarEvidence": "The record narrates the gesture."}
        contexts = [{"Status": "identified-non-master", "ActorLabel": "the Jiangling monk",
                     "Roles": ["performer"],
                     "GrammarEvidence": "The subject 江陵僧 immediately governs 提起坐具 in the complete turn."}]
        self.assertFalse(narrated_action_performer_missing("提起坐具", actor, [], contexts))

    def test_reviewed_unnamed_case_figure_with_action_proof_passes(self):
        actor = {"Status": "narrated", "GrammarEvidence": "The record narrates the gesture."}
        contexts = [{
            "Status": "reviewed-unnamed",
            "ActorLabel": "the unnamed monk who comes forward",
            "Roles": ["case-figure"],
            "GrammarEvidence": "有僧出 identifies the unnamed monk as the person whose circle-drawing action the narrator describes.",
        }]
        self.assertFalse(narrated_action_performer_missing("打一圓相", actor, [], contexts))

    def test_closed_inline_performer_proof_passes(self):
        actor = {"Status": "narrated", "GrammarEvidence":
                 "The record supplies the narration voice. Action performer: Feiyin Tongrong (identified-unlinked-master)."}
        self.assertFalse(narrated_action_performer_missing("驀豎拂子", actor, [], []))

    def test_seated_actions_are_not_mistaken_for_utterances(self):
        for literal in ("師乃趺坐", "師正身端坐", "師坐", "師立"):
            with self.subTest(literal=literal):
                self.assertIsNotNone(EXPLICIT_MASTER_ACTION.search(literal))

    def test_reader_note_cannot_assign_narrated_action_to_mastername(self):
        self.assertIsNotNone(
            NARRATOR_REPORT_NOTE.search(
                "The record narrator reports Zhaozhou sitting upright."
            )
        )
        self.assertIsNotNone(
            NARRATOR_REPORT_NOTE.search(
                "The biographical section reports Yongjue sitting and dying."
            )
        )
        self.assertIsNotNone(
            RAISED_RECORD_NOTE.search(
                "Dongshan raises the record of his teacher sitting upright."
            )
        )


if __name__ == "__main__":
    unittest.main()
