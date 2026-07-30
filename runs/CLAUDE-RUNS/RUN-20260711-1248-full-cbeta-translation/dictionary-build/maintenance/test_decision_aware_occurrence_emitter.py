from __future__ import annotations

import copy
import unittest
from unittest.mock import patch

import construct_r11_clean_regeneration_c as authoring
from maintenance.generic_bounded_constructor import ActorClosureError, verify_actor_closure


RUNGS = [
    "line", "expanded-context", "section-header",
    "book-title", "tei-header", "parallel-passage",
]


def decision(key: str, kind: str) -> dict:
    grammar = {
        "named": "師云 assigns the complete headword-bearing statement to Miyun Yuanwu.",
        "anonymous": "僧問 assigns the headword-bearing question to an unnamed monk before 師云 opens the reply.",
        "identified": "李公問 explicitly assigns the headword-bearing question to the named lay official Li.",
        "narrated": "The compiler's finite narrative verb assigns the headword-bearing action to the described subject.",
        "impersonal": "The editorial heading contains the headword as an event label and has no human utterer.",
    }[kind]
    base = {
        "evidenceKey": key,
        "masterName": "Miyun Yuanwu" if kind == "named" else None,
        "actorAttribution": None,
        "contextMasters": (
            [{"MasterName": "Miyun Yuanwu", "Roles": ["utterer"]}]
            if kind == "named"
            else [{"MasterName": "Miyun Yuanwu", "Roles": [
                "respondent" if kind in {"anonymous", "identified"} else "person-described"
            ]}]
        ),
        "contextActors": [],
        "exactHeadwordClause": "主人公",
        "grammarEvidence": grammar,
        "voice": {
            "named": "Direct speech marked by 師云.",
            "anonymous": "An anonymous monastic question followed by the named master's reply.",
            "identified": "A named lay questioner followed by the named master's reply.",
            "narrated": "Third-person compiler narration, not quoted speech.",
            "impersonal": "An editorial event heading, not human speech.",
        }[kind],
        "fullCaseDecision": {
            "named": "Miyun Yuanwu is the exact headword utterer.",
            "anonymous": "The unnamed monk owns the question; Miyun Yuanwu owns only the answer.",
            "identified": "The named lay official Li owns the question; Miyun Yuanwu owns only the answer.",
            "narrated": "The compiler narrates the action about Miyun Yuanwu; Miyun does not utter the headword.",
            "impersonal": "The headword occurs in an impersonal editorial heading.",
        }[kind],
        "action": {
            "named": "utters the term in his own answer",
            "anonymous": "asks the headword-bearing question",
            "identified": "asks the headword-bearing question",
            "narrated": "narrates the headword-bearing action",
            "impersonal": "labels the editorial event",
        }[kind],
        "attributionNote": {
            "named": "Miyun Yuanwu utters the retained headword.",
            "anonymous": "An unnamed monk asks the retained question; Miyun Yuanwu answers.",
            "identified": "The named lay official Li asks the retained question; Miyun Yuanwu answers.",
            "narrated": "The compiler narrates the action about Miyun Yuanwu.",
            "impersonal": "An editorial heading carries the retained term.",
        }[kind],
    }
    if kind == "anonymous":
        base["actorAttribution"] = {
            "Status": "reviewed-unnamed",
            "Kind": "monastic questioner",
            "ActorLabel": "an unnamed monastic questioner",
            "ActorRole": "questioner",
            "RungsChecked": RUNGS,
            "GrammarEvidence": grammar,
            "ReviewedBy": "decision-aware emitter test",
            "ReviewedUtc": "2026-07-30T00:00:00Z",
        }
    elif kind == "identified":
        base["actorAttribution"] = {
            "Status": "identified-non-master",
            "Kind": "named lay official",
            "ActorLabel": "Li",
            "ActorRole": "questioner",
            "GrammarEvidence": grammar,
            "ReviewedBy": "decision-aware emitter test",
            "ReviewedUtc": "2026-07-30T00:00:00Z",
        }
    elif kind == "narrated":
        base["actorAttribution"] = {
            "Status": "narrated",
            "Kind": "compiler narrative",
            "ActorLabel": "the source compiler",
            "ActorRole": "compiler",
            "GrammarEvidence": grammar,
            "AuthoredVoiceRiskReviewed": True,
            "ReviewedBy": "decision-aware emitter test",
            "ReviewedUtc": "2026-07-30T00:00:00Z",
        }
    elif kind == "impersonal":
        base["contextMasters"] = []
        base["actorAttribution"] = {
            "Status": "impersonal",
            "Kind": "editorial heading",
            "ActorLabel": "an impersonal editorial heading",
            "ActorRole": "compiler",
            "GrammarEvidence": grammar,
            "ReviewedBy": "decision-aware emitter test",
            "ReviewedUtc": "2026-07-30T00:00:00Z",
        }
    return base


def spec(key: str, kind: str) -> dict:
    return {
        "evidenceKey": key,
        "relPath": "J/J10/J10nA158.xml",
        "occurrenceIndex": 0,
        "actorDecision": decision(key, kind),
    }


def emitted(key: str, kind: str) -> dict:
    with patch.object(
        authoring, "concise_kwic",
        return_value=("僧問主人公在何處師云看腳下", "0001a01"),
    ):
        return authoring.make_occurrence(
            {"J/J10/J10nA158.xml": "Recorded Sayings of Miyun"},
            "主人公", spec(key, kind), key,
        )


class DecisionAwareOccurrenceEmitterTests(unittest.TestCase):
    def test_explicit_actor_variants_pass_whole_config_guard(self):
        for kind in ("named", "anonymous", "identified", "narrated", "impersonal"):
            with self.subTest(kind=kind):
                occurrence = emitted("o1", kind)
                config = {
                    "entries": [{
                        "id": f"test-{kind}",
                        "evidenceDraft": {
                            "Entry": {"Senses": [{"Occurrences": [occurrence]}]}
                        },
                    }]
                }
                verify_actor_closure(config)

    def test_missing_decision_fails_closed(self):
        with self.assertRaisesRegex(
            ValueError, "explicit keyed actor/action decision"
        ):
            authoring.validate_occurrence_spec(
                ("J/J10/J10nA158.xml", 0, "Miyun Yuanwu", []), "o1"
            )

    def test_occurrence_and_decision_identity_are_both_bound(self):
        wrong_spec = spec("o2", "named")
        with self.assertRaisesRegex(ValueError, "occurrence evidence key mismatch"):
            authoring.validate_occurrence_spec(wrong_spec, "o1")
        wrong_decision = spec("o1", "named")
        wrong_decision["actorDecision"]["evidenceKey"] = "o2"
        with self.assertRaisesRegex(ValueError, "actor decision is keyed"):
            authoring.validate_occurrence_spec(wrong_decision, "o1")

    def test_whole_config_guard_rejects_later_missing_actor_before_writes(self):
        good = emitted("o1", "named")
        bad = copy.deepcopy(emitted("o1", "anonymous"))
        bad.pop("ActorAttribution")
        config = {
            "entries": [
                {
                    "id": "first",
                    "evidenceDraft": {
                        "Entry": {"Senses": [{"Occurrences": [good]}]}
                    },
                },
                {
                    "id": "later",
                    "evidenceDraft": {
                        "Entry": {"Senses": [{"Occurrences": [bad]}]}
                    },
                },
            ]
        }
        with self.assertRaisesRegex(
            ActorClosureError, r"entries\[1\]\(later\)"
        ):
            verify_actor_closure(config)

    def test_actual_emitter_preflight_is_exhaustive_and_zero_write(self):
        import tempfile
        from pathlib import Path

        configs = [
            {"id": "first", "occurrences": [spec("o1", "named")]},
            {"id": "later", "occurrences": [spec("o2", "anonymous")]},
        ]
        with tempfile.TemporaryDirectory() as raw:
            original = authoring.FRESH
            authoring.FRESH = Path(raw) / "must-not-exist"
            try:
                with self.assertRaisesRegex(
                    authoring.OccurrenceDecisionClosureError,
                    "later.*missing occurrence keys.*occurrence evidence key mismatch",
                ):
                    authoring.compile_all(
                        configs, {}, {}, {}, expected_ids=["first", "later"]
                    )
                self.assertFalse(authoring.FRESH.exists())
            finally:
                authoring.FRESH = original

    def test_config_id_and_occurrence_key_inventory_is_exhaustive(self):
        configs = [
            {"id": "duplicate", "occurrences": [spec("o1", "named")]},
            {"id": "duplicate", "occurrences": [
                spec("o1", "anonymous"), spec("o1", "impersonal")
            ]},
            {"id": "surplus", "occurrences": [spec("o1", "narrated")]},
        ]
        with self.assertRaises(authoring.OccurrenceDecisionClosureError) as caught:
            authoring.preflight_config_occurrence_decisions(
                configs, expected_ids=["missing", "duplicate"]
            )
        message = str(caught.exception)
        for fragment in (
            "duplicate config ids", "missing config ids", "surplus config ids",
            "duplicate occurrence keys", "missing occurrence keys",
        ):
            self.assertIn(fragment, message)


if __name__ == "__main__":
    unittest.main()
