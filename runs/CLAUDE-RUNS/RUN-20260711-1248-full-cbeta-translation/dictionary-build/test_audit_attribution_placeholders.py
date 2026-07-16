#!/usr/bin/env python3

import unittest
from collections import Counter
import re

from audit_attribution import (
    ANONYMOUS_MONK_QUESTION,
    CLOSED_ROLES,
    DUPLICATED_NOTE_PREFIX_RE,
    DUPLICATED_SOURCE_PREFIX_RE,
    EXPLICIT_MASTER_ACTION,
    EXPLICIT_MASTER_TURN,
    PLACEHOLDER_ACTOR_RE,
    RAISED_OLD_SAYING,
    anonymous_actor_collapse_failure,
    explicit_master_turns_before_headword,
    has_evidence_bound_later_quoter,
    has_exact_actor_context,
    uniform_actor_placeholder_failure,
    attribution_note_hygiene_failures,
)


class PlaceholderActorTest(unittest.TestCase):
    def test_rejects_reader_visible_attribution_scaffolding(self):
        rel = "X/X80/X80n1565.xml"
        self.assertEqual([], attribution_note_hygiene_failures(
            f"Source record ({rel}). Compiler narration: The recorder reports the action.", rel
        ))
        bad = attribution_note_hygiene_failures(
            f"Compiler narration: Source record ({rel}). The source does not name an unnamed monk.", rel
        )
        self.assertIn("noncanonical-source-opening", bad)
        self.assertIn("malformed-unnamed-actor", bad)
        recursive = "Source record (%s). the question says (the question says (the question says (問曰)))" % rel
        self.assertIn("recursive-translation-expansion", attribution_note_hygiene_failures(recursive, rel))

    def test_executable_role_vocabulary_matches_actor_audit_law(self):
        self.assertIn("person-described", CLOSED_ROLES)
        self.assertIn("case-figure", CLOSED_ROLES)
        for forbidden in ("action-performer", "case-teacher", "named-unrostered"):
            self.assertNotIn(forbidden, CLOSED_ROLES)

    def test_detects_anonymous_monk_questions_and_raised_precedents(self):
        self.assertIsNotNone(ANONYMOUS_MONK_QUESTION.search("僧問如何是和尚家風"))
        self.assertIsNotNone(ANONYMOUS_MONK_QUESTION.search("僧進問鼻孔遼天"))
        for value in ("古人云", "古德曰", "先德有言"):
            self.assertIsNotNone(RAISED_OLD_SAYING.search(value))

    def test_detects_explicit_master_turns_in_headword_clause(self):
        for value in ("師云", "師曰", "師乃云", "師復問"):
            with self.subTest(value=value):
                self.assertIsNotNone(EXPLICIT_MASTER_TURN.search(value))

    def test_accepts_named_verse_author_as_exact_headword_actor(self):
        contexts = [{"MasterName": "Tianyin Yuanxiu", "Roles": ["verse-author"]}]
        self.assertTrue(has_exact_actor_context("Tianyin Yuanxiu", contexts))
        self.assertFalse(has_exact_actor_context("Tianyin Yuanxiu", [
            {"MasterName": "Tianyin Yuanxiu", "Roles": ["later-quoter"]}
        ]))

    def test_following_master_said_is_not_assigned_backward_to_question(self):
        self.assertEqual([], explicit_master_turns_before_headword(
            "喝下", "問德山棒臨濟喝如何是一喝下事師云我不作這活計"
        ))

    def test_cue_shaped_headword_does_not_trigger_master_turn(self):
        self.assertEqual([], explicit_master_turns_before_headword(
            "答話", "謝師答話畢僧便禮拜"
        ))
        self.assertEqual([], explicit_master_turns_before_headword(
            "師答", "僧問如何是道師答話畢"
        ))

    def test_old_saying_requires_evidence_bound_quoter_not_role_string(self):
        bare = {"ActorRole": "later-quoter"}
        self.assertFalse(has_evidence_bound_later_quoter(bare))
        complete = {
            "Status": "identified-non-master",
            "Kind": "monastic officer",
            "ActorLabel": "Qi, the chief cook (栖典座)",
            "ActorRole": "later-quoter",
            "RungsChecked": [
                "line", "expanded-context", "section-header", "book-title",
                "tei-header", "parallel-passage",
            ],
            "GrammarEvidence": "栖典座問 assigns the quoted headword wording to Qi's turn.",
        }
        self.assertTrue(has_evidence_bound_later_quoter(complete))
        incomplete = dict(complete, GrammarEvidence="quoted precedent")
        self.assertFalse(has_evidence_bound_later_quoter(incomplete))

    def test_separates_narrated_master_actions_from_speech(self):
        for value in ("師拈", "師下座", "師歸方丈", "師乃卓"):
            with self.subTest(value=value):
                self.assertIsNone(EXPLICIT_MASTER_TURN.search(value))
                self.assertIsNotNone(EXPLICIT_MASTER_ACTION.search(value))

    def test_identified_non_master_labels_must_name_someone(self):
        generic = re.compile(r"^(?:the|an?|one|some)\b", re.IGNORECASE)
        for value in (
            "the Chan verse-anthology author",
            "an imperial memorial author",
            "the named local-gentry invitation authors",
        ):
            with self.subTest(value=value):
                self.assertIsNotNone(generic.match(value))
        for value in ("Pei Xiu", "Puming", "Zhang Shangying"):
            with self.subTest(value=value):
                self.assertIsNone(generic.match(value))

    def test_speaking_record_owner_language_is_not_an_anonymous_actor(self):
        value = "The complete unit places the token inside the current record owner's address."
        self.assertIsNotNone(re.search(
            r"speaking\s+record[- ]owner|current\s+record[- ]owner(?:['’]s)?\s+address", value,
            re.IGNORECASE,
        ))

    def test_reviewed_unnamed_labels_must_be_reader_explicit(self):
        explicit = re.compile(r"\bunnamed\b|does not name", re.IGNORECASE)
        self.assertIsNotNone(explicit.search("the unnamed questioning monk"))
        self.assertIsNone(explicit.search("speaking record owner resolved from the complete section"))

    def test_rejects_duplicated_actor_note_prefix(self):
        self.assertIsNotNone(DUPLICATED_NOTE_PREFIX_RE.search(
            "Foyan Qingyuan: Foyan Qingyuan: Record of Foyan says..."
        ))
        self.assertIsNotNone(DUPLICATED_NOTE_PREFIX_RE.search(
            "Source text (古尊宿語錄). Xuansha Shibei: Xuansha Shibei: addresses the assembly."
        ))
        self.assertIsNone(DUPLICATED_NOTE_PREFIX_RE.search(
            "Foyan Qingyuan: Record of Foyan says..."
        ))
        self.assertIsNotNone(DUPLICATED_SOURCE_PREFIX_RE.search(
            "Source record (X/X80/X80n1565.xml). Source record (五燈會元). Exact actor: Zhaozhou Congshen."
        ))
        self.assertIsNone(DUPLICATED_SOURCE_PREFIX_RE.search(
            "Source record (X/X80/X80n1565.xml). Exact actor: Zhaozhou Congshen."
        ))

    def test_rejects_generated_actor_placeholders(self):
        for value in (
            "the fully reviewed source voice",
            "reviewed source voice",
            "the reviewed compilation voice",
            "the named section speaker or quoted case voice",
            "the verse or address invoking Li Guang",
            "the record’s named-book discussion",
            "the cited voice",
            "the cited participant",
            "the identified master",
            "a cited figure",
            "the presiding speaker",
            "verse voice",
            "the case or verse narrator",
            "an unresolved quoted speaker",
            "the generic case narrator",
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

    def test_requires_a_meaningful_named_share_or_a_small_cohort(self):
        mostly_narrated = Counter({("narrated",): 40, ("reviewed-unnamed",): 5})
        self.assertIsNotNone(anonymous_actor_collapse_failure(10, 1, mostly_narrated))
        self.assertIsNone(anonymous_actor_collapse_failure(10, 10, mostly_narrated))
        mixed = Counter({("narrated",): 20, ("reviewed-unnamed",): 20})
        self.assertIsNotNone(anonymous_actor_collapse_failure(10, 0, mixed))
        self.assertIsNone(anonymous_actor_collapse_failure(5, 0, mixed))


if __name__ == "__main__":
    unittest.main()
