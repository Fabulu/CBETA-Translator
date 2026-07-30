#!/usr/bin/env python3

import copy
import unittest

from compile_evidence_draft import compile_draft


def valid_payload():
    return {
        "SchemaVersion": 1,
        "Entry": {
            "Id": "t_test",
            "SourceTerm": "試",
            "CorpusBaselineSha256": "abc",
            "CreatedBy": "test",
            "WrittenUtc": None,
            "Senses": [{
                "SenseKey": None,
                "MasterName": None,
                "PreferredTarget": "test word",
                "AlternateTargets": [],
                "SearchAliases": ["test word", "test expression"],
                "Status": "preferred",
                "ExplanationParts": {
                    "CorpusEarnedOpening": "The records use this word as a direct public test.",
                    "EvidenceBody": ["The stored exchange supplies the exact wording and named turn."],
                },
                "Validation": "provisional",
                "Note": "One-work evidence remains provisional.",
                "Occurrences": [{
                    "RelPath": "T/test.xml", "FromLb": "0001a01", "ToLb": "0001a01",
                    "Kwic": "師云試", "MasterName": "Test Master", "Curated": True,
                    "ContextMasters": [{"MasterName": "Test Master", "Roles": ["utterer"]}],
                    "AttributionNote": "Source record (T/test.xml). Test Master utters the headword.",
                    "DraftActorProof": {
                        "ExactHeadwordClause": "師云試", "SpeechFrame": "師云 governs 試.",
                        "FullCaseDecision": "The complete exchange contains one turn and names its speaker.",
                    },
                }],
                "ClaimAnchors": [], "SourceTexts": ["T/test.xml"],
                "RelatedMasters": ["Test Master"], "RelatedTerms": [],
                "DraftEvidence": {
                    "OpeningClaimEvidenceKeys": ["o1"],
                    "ZenBend": "An ordinary test word is used in a public encounter.",
                    "CounterexampleOrLimit": "No second referent appears in the stored concordance.",
                    "DifferentThingTest": {"Decision": "one-thing", "ComparedThings": ["word"],
                                           "Reason": "Every witness denotes the same lexical item."},
                    "AliasRationale": "Both aliases preserve the tested-word referent.",
                    "ModifierControls": [{"Status": "not-applicable", "Reason": "No modifier graph."}],
                    "FamilyControls": [{"Status": "checked", "Reason": "Longer compounds were excluded."}],
                    "IndependentWorkIds": ["work:test"],
                },
            }],
        },
    }


class EvidenceDraftTests(unittest.TestCase):
    def test_compiles_to_existing_schema_and_strips_research_fields(self):
        entry, errors = compile_draft(valid_payload())
        self.assertEqual([], errors)
        sense = entry["Senses"][0]
        self.assertNotIn("ExplanationParts", sense)
        self.assertNotIn("DraftEvidence", sense)
        self.assertNotIn("DraftActorProof", sense["Occurrences"][0])
        self.assertEqual(
            "The records use this word as a direct public test. "
            "The stored exchange supplies the exact wording and named turn.",
            sense["Explanation"],
        )

    def test_derives_link_inventories_from_structured_evidence(self):
        payload = valid_payload()
        sense = payload["Entry"]["Senses"][0]
        sense["RelatedMasters"] = []
        sense["SourceTexts"] = ["stale.xml"]
        sense["Occurrences"][0]["ContextMasters"].append(
            {"MasterName": "Context Master", "Roles": ["respondent"]}
        )
        entry, errors = compile_draft(payload)
        self.assertEqual([], errors)
        compiled = entry["Senses"][0]
        self.assertEqual(["Test Master", "Context Master"], compiled["RelatedMasters"])
        self.assertEqual(["T/test.xml"], compiled["SourceTexts"])

    def test_preserves_reviewed_unnamed_context_actor(self):
        payload = valid_payload()
        occurrence = payload["Entry"]["Senses"][0]["Occurrences"][0]
        occurrence["ContextActors"] = [{
            "Status": "reviewed-unnamed",
            "ActorLabel": "the unnamed monk described in the narration",
            "Roles": ["case-figure"],
            "GrammarEvidence": (
                "有僧 identifies an unnamed monk as the person whose narrated physical action is described."
            ),
        }]
        entry, errors = compile_draft(payload)
        self.assertEqual([], errors)
        context = entry["Senses"][0]["Occurrences"][0]["ContextActors"][0]
        self.assertEqual("reviewed-unnamed", context["Status"])
        self.assertEqual(["case-figure"], context["Roles"])

    def test_rejects_and_strips_null_related_master(self):
        payload = valid_payload()
        payload["Entry"]["Senses"][0]["RelatedMasters"] = ["Test Master", None]
        entry, errors = compile_draft(payload)
        self.assertTrue(any("RelatedMasters" in error for error in errors))
        self.assertEqual(["Test Master"], entry["Senses"][0]["RelatedMasters"])

    def test_rejects_calque_first_opening(self):
        payload = valid_payload()
        payload["Entry"]["Senses"][0]["ExplanationParts"]["CorpusEarnedOpening"] = "Literally, test."
        _, errors = compile_draft(payload)
        self.assertTrue(any("calque" in error for error in errors))

    def test_rejects_polished_database_process_boilerplate(self):
        payload = valid_payload()
        payload["Entry"]["Senses"][0]["ExplanationParts"]["CorpusEarnedOpening"] = (
            "In the selected records, the headword is rendered as ‘test word’; "
            "its stored turns define the scope of this sense."
        )
        _, errors = compile_draft(payload)
        self.assertTrue(any("generic template filler" in error for error in errors))

    def test_rejects_opening_duplicated_as_evidence_body(self):
        payload = valid_payload()
        opening = payload["Entry"]["Senses"][0]["ExplanationParts"]["CorpusEarnedOpening"]
        payload["Entry"]["Senses"][0]["ExplanationParts"]["EvidenceBody"] = [opening]
        _, errors = compile_draft(payload)
        self.assertTrue(any("duplicates the corpus-earned opening" in error for error in errors))

    def test_rejects_generic_deployment_inventory_prose(self):
        payload = valid_payload()
        payload["Entry"]["Senses"][0]["ExplanationParts"]["EvidenceBody"] = [
            "The expression ‘test word’ occurs in the cited questions, answers, actions, narration, or verse.",
            "This sense remains limited to those deployments and the explicit contrasts stated in the opening.",
        ]
        _, errors = compile_draft(payload)
        self.assertGreaterEqual(sum("generic template filler" in error for error in errors), 2)

    def test_rejects_action_image_judgment_template(self):
        payload = valid_payload()
        payload["Entry"]["Senses"][0]["ExplanationParts"]["CorpusEarnedOpening"] = (
            "A Snowflake On A Red-Hot Furnace is the corpus expression for the action, image, "
            "or judgment described by the stored cases."
        )
        payload["Entry"]["Senses"][0]["ExplanationParts"]["EvidenceBody"] = [
            "The witnesses place it in direct answers, challenges, verses, appraisals, and "
            "narrative controls, so the entry follows those predicates rather than an outside interpretation."
        ]
        _, errors = compile_draft(payload)
        self.assertGreaterEqual(sum("generic template filler" in error for error in errors), 2)

    def test_rejects_generic_figure_template(self):
        payload = valid_payload()
        payload["Entry"]["Senses"][0]["ExplanationParts"]["CorpusEarnedOpening"] = (
            "Manjusri is the figure the records place inside Zen cases, quotations, and public questions."
        )
        payload["Entry"]["Senses"][0]["ExplanationParts"]["EvidenceBody"] = [
            "The selected witnesses define this figure by what masters ask, quote, praise, rebuke, or reenact."
        ]
        _, errors = compile_draft(payload)
        self.assertGreaterEqual(sum("generic template filler" in error for error in errors), 2)

    def test_rejects_generic_institution_template(self):
        payload = valid_payload()
        payload["Entry"]["Senses"][0]["ExplanationParts"]["CorpusEarnedOpening"] = (
            "Attendant names a concrete implement, office, rite, or communal act in the public life of a Zen monastery."
        )
        payload["Entry"]["Senses"][0]["ExplanationParts"]["EvidenceBody"] = [
            "The selected witnesses show who performs it, where it enters the hall sequence, and how masters bring it into encounters."
        ]
        _, errors = compile_draft(payload)
        self.assertGreaterEqual(sum("generic template filler" in error for error in errors), 2)

    def test_rejects_exact_a906_930_polished_template(self):
        payload = valid_payload()
        sense = payload["Entry"]["Senses"][0]
        sense["ExplanationParts"]["CorpusEarnedOpening"] = (
            "The mechanism beyond the presented terms is the plain-English referent "
            "tested by the selected Chan records."
        )
        sense["ExplanationParts"]["EvidenceBody"] = [
            "The selected cases place the mechanism beyond the presented terms inside "
            "lineage records, public addresses, institutional narration, or inherited cases; "
            "the exact surrounding predicates delimit how the records use it rather than "
            "importing an external definition."
        ]
        _, errors = compile_draft(payload)
        self.assertGreaterEqual(sum("generic template filler" in error for error in errors), 2)

    def test_strips_all_draft_prefixed_fields(self):
        payload = valid_payload()
        occurrence = payload["Entry"]["Senses"][0]["Occurrences"][0]
        occurrence["DraftTemporaryNote"] = "research only"
        payload["Entry"]["Senses"][0]["DraftTemporaryNote"] = "research only"
        entry, errors = compile_draft(payload)
        self.assertEqual([], errors)
        self.assertNotIn("DraftTemporaryNote", entry["Senses"][0])
        self.assertNotIn("DraftTemporaryNote", entry["Senses"][0]["Occurrences"][0])

    def test_rejects_generic_actor_filler(self):
        payload = valid_payload()
        occurrence = payload["Entry"]["Senses"][0]["Occurrences"][0]
        occurrence.pop("MasterName")
        occurrence["ActorAttribution"] = {
            "Status": "narrated", "Kind": "compiler narrative", "ActorLabel": "the compiler",
            "ActorRole": "compiler", "GrammarEvidence": "Expanded context establishes the actor.",
        }
        occurrence["ContextMasters"] = []
        occurrence["DraftActorProof"] = {
            "GrammaticalSubject": "the compiler", "FullCaseDecision": "The full case is narration."
        }
        _, errors = compile_draft(payload)
        self.assertTrue(any("generic template filler" in error for error in errors))

    def test_rejects_speech_frame_as_master_name(self):
        for bad_name in ("師乃", "師以杖指法座", "示眾", "作投機偈", "謂弟子"):
            with self.subTest(bad_name=bad_name):
                payload = valid_payload()
                payload["Entry"]["Senses"][0]["Occurrences"][0]["MasterName"] = bad_name
                _, errors = compile_draft(payload)
                self.assertTrue(any("not a person's canonical roster name" in error for error in errors))

    def test_rejects_actor_attribution_beside_populated_master_name(self):
        payload = valid_payload()
        occurrence = payload["Entry"]["Senses"][0]["Occurrences"][0]
        occurrence["ActorAttribution"] = {
            "Status": "identified-unlinked-master",
            "Kind": "full-case exact-turn adjudication",
            "ActorLabel": "Different Actor",
            "ActorRole": "utterer",
            "GrammarEvidence": "The stale decision assigns the headword-bearing action to another actor.",
        }
        _, errors = compile_draft(payload)
        self.assertTrue(any("contradicts populated MasterName" in error for error in errors))

        occurrence["ActorAttribution"]["ActorLabel"] = "Test Master"
        _, errors = compile_draft(payload)
        self.assertTrue(any("exact actor ownership must use one representation" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
