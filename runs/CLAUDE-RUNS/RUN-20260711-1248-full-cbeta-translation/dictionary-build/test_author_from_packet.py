import copy
import hashlib
import json
import tempfile
import unittest
from pathlib import Path

import author_from_packet as afp
import audit_attribution
import audit_batch_semantic_templates
import audit_depth_sense
from compile_evidence_draft import compile_draft


class AuthorFromPacketTests(unittest.TestCase):
    def setUp(self):
        self.packet = {"rows": [{"id": "t_fixture", "term": "西天四七", "candidateCases": [{
            "caseIndex": 3, "workId": "work:one", "relPath": "X/X01/X01n0001.xml",
            "fromLb": "0001a01", "toLb": "0002b02", "englishTitle": "Fixture Record",
            "chineseTitle": "試錄"}]}]}
        self.decision = {"schemaVersion": afp.SCHEMA, "entries": [{
            "id": "t_fixture", "createdBy": "Fixture human reviewer", "writtenUtc": "2026-07-18T00:00:00Z",
            "senses": [{"senseKey": None, "masterName": None, "preferredTarget": "the twenty-eight western ancestors",
                "alternateTargets": [], "searchAliases": ["twenty-eight western ancestors"], "status": "preferred",
                "validation": "provisional", "note": "The tally is limited to the cited lineage formula.",
                "explanationParts": {"corpusEarnedOpening": "The records use this tally for the twenty-eight western ancestors.",
                                     "evidenceBody": ["The fixture address pairs the tally with the eastern succession."]},
                "relatedMasters": [], "relatedTerms": ["東土二三"],
                "draftEvidence": {"OpeningClaimEvidenceKeys": ["o1"], "ZenBend": "The paired lineage tally fixes the local use.",
                    "CounterexampleOrLimit": "No numerical use is claimed outside this formula.",
                    "DifferentThingTest": {"Decision": "one-thing", "ComparedThings": [], "Reason": "The retained witness names one lineage tally."},
                    "AliasRationale": "The English alias exposes the translated tally.",
                    "ModifierControls": ["No modifier changes the referent in the retained witness."],
                    "FamilyControls": ["Compared with the paired eastern tally." ]},
                "occurrences": [{"caseIndex": 3, "fromLb": "0001a03", "toLb": "0001a04",
                    "kwic": "上堂云西天四七東土二三", "exactHeadwordClause": "西天四七",
                    "grammaticalProof": "The explicit hall-address frame governs this paired lineage clause before the address ends.",
                    "actor": {"type": "named-master", "masterName": "Fixture Master"},
                    "contextMasters": []}]}]}]}

    def build(self, decision=None):
        return afp.build(self.packet, decision or self.decision, "a" * 64, {"Fixture Master", "Other Master"})

    def test_deterministic_output_and_compiler_schema_parity(self):
        first = self.build()[0]
        second = self.build()[0]
        self.assertEqual(afp._render(first[1]), afp._render(second[1]))
        self.assertEqual(afp._render(first[2]), afp._render(second[2]))
        compiled, errors = compile_draft(first[1])
        self.assertEqual([], errors)
        self.assertEqual(first[2], compiled)
        occurrence = compiled["Senses"][0]["Occurrences"][0]
        self.assertEqual("X/X01/X01n0001.xml", occurrence["RelPath"])
        self.assertNotIn("workId", occurrence)
        self.assertNotIn("DraftActorProof", occurrence)

    def reject(self, mutate, phrase):
        value = copy.deepcopy(self.decision)
        mutate(value)
        with self.assertRaisesRegex(afp.DecisionError, phrase):
            self.build(value)

    def test_rejections(self):
        occurrence = lambda d: d["entries"][0]["senses"][0]["occurrences"][0]
        self.reject(lambda d: occurrence(d)["actor"].update(type="unresolved"), "unresolved or invalid")
        self.reject(lambda d: occurrence(d)["actor"].update(masterName="Invented Master"), "canonical roster")
        self.reject(lambda d: occurrence(d).update(grammaticalProof="Fixture Master utters the headword."), "case-specific")
        self.reject(lambda d: occurrence(d).update(fromLb="line one"), "malformed")
        self.reject(lambda d: occurrence(d).update(exactHeadwordClause="東土二三"), "must contain")
        self.reject(lambda d: occurrence(d).update(caseIndex=99), "packet mismatch")
        self.reject(lambda d: occurrence(d).update(attributionContext="僧問"), "English prose")
        self.reject(lambda d: d["entries"][0]["senses"][0].update(note="僧問"), "non-English prose")
        self.reject(lambda d: occurrence(d).update(relPath="X/wrong.xml"), "immutable packet fields")
        def bad_role(d):
            occurrence(d)["contextMasters"] = [{"masterName": "Other Master", "roles": ["invented-role"]}]
        self.reject(bad_role, "closed non-utterer")

    def test_null_actor_expands_six_rungs(self):
        value = copy.deepcopy(self.decision)
        occurrence = value["entries"][0]["senses"][0]["occurrences"][0]
        occurrence["actor"] = {"type": "reviewed-unnamed", "kind": "question turn",
            "label": "the unnamed questioning monk", "role": "questioner",
            "subject": "The unnamed questioning monk", "reviewedBy": "Fixture human reviewer"}
        worksheet = self.build(value)[0][1]
        actor = worksheet["Entry"]["Senses"][0]["Occurrences"][0]["ActorAttribution"]
        self.assertEqual(afp.RUNGS, actor["RungsChecked"])

    def test_unlinked_proof_repeats_identity(self):
        value = copy.deepcopy(self.decision)
        occurrence = value["entries"][0]["senses"][0]["occurrences"][0]
        occurrence["actor"] = {"type": "identified-unlinked-master", "kind": "full-case speech turn",
            "label": "Named Unlinked 名未錄", "role": "utterer", "subject": "Named Unlinked 名未錄",
            "reviewedBy": "Fixture human reviewer"}
        compiled = self.build(value)[0][2]
        actor = compiled["Senses"][0]["Occurrences"][0]["ActorAttribution"]
        self.assertIn(actor["ActorLabel"], actor["GrammarEvidence"])

    def test_unlinked_context_actor_is_named_but_not_linked(self):
        value = copy.deepcopy(self.decision)
        occurrence = value["entries"][0]["senses"][0]["occurrences"][0]
        occurrence["contextActors"] = [{
            "type": "identified-unlinked-master",
            "label": "Named Context Figure",
            "roles": ["person-discussed"],
            "grammaticalProof": "The retained clause explicitly discusses Named Context Figure inside the governed turn.",
        }]
        occurrence["attributionContext"] = "The turn explicitly discusses Named Context Figure."
        compiled = self.build(value)[0][2]
        emitted = compiled["Senses"][0]["Occurrences"][0]
        self.assertEqual("Named Context Figure", emitted["ContextActors"][0]["ActorLabel"])
        self.assertNotIn("Named Context Figure", [row["MasterName"] for row in emitted["ContextMasters"]])
        self.assertIn("Named Context Figure", emitted["AttributionNote"])

    def test_roster_identity_cannot_hide_in_unlinked_context_actor(self):
        def mutate(value):
            occurrence = value["entries"][0]["senses"][0]["occurrences"][0]
            occurrence["contextActors"] = [{
                "type": "identified-unlinked-master",
                "label": "Other Master",
                "roles": ["person-discussed"],
                "grammaticalProof": "The retained clause explicitly discusses Other Master inside the governed turn.",
            }]
            occurrence["attributionContext"] = "The turn explicitly discusses Other Master."
        self.reject(mutate, "belongs in contextMasters")

    def test_position_81_requires_voice_layer_and_quoted_context(self):
        self.packet["rows"][0]["position"] = 81
        self.reject(lambda d: None, "voiceLayer")
        value = copy.deepcopy(self.decision)
        occurrence = value["entries"][0]["senses"][0]["occurrences"][0]
        occurrence["voiceLayer"] = "quoted-original"
        with self.assertRaisesRegex(afp.DecisionError, "outer raiser"):
            self.build(value)
        occurrence["contextMasters"] = [{"masterName": "Other Master", "roles": ["later-raiser"]}]
        self.assertEqual(1, len(self.build(value)))

    def test_compiler_narration_cannot_silently_cross_speech_marker(self):
        self.packet["rows"][0]["position"] = 81
        value = copy.deepcopy(self.decision)
        occurrence = value["entries"][0]["senses"][0]["occurrences"][0]
        occurrence["voiceLayer"] = "compiler-narration"
        occurrence["actor"] = {"type": "narrated", "kind": "compiler narrative",
            "label": "the source compiler or narrator", "role": "compiler",
            "subject": "the source compiler or narrator", "reviewedBy": "Fixture human reviewer"}
        with self.assertRaisesRegex(afp.DecisionError, "speechMarkerReviewed"):
            self.build(value)
        occurrence["speechMarkerReviewed"] = True
        self.assertEqual(1, len(self.build(value)))

    def test_narrow_kwic_does_not_hide_full_case_speech_marker(self):
        self.packet["rows"][0]["position"] = 81
        self.packet["rows"][0]["candidateCases"][0]["speechFrame"] = "僧問如何是祖師西來意師曰庭前柏樹子編者記西天四七"
        value = copy.deepcopy(self.decision)
        occurrence = value["entries"][0]["senses"][0]["occurrences"][0]
        occurrence["kwic"] = "編者記西天四七"
        occurrence["exactHeadwordClause"] = "西天四七"
        occurrence["voiceLayer"] = "compiler-narration"
        occurrence["actor"] = {"type": "narrated", "kind": "compiler narrative",
            "label": "the source compiler or narrator", "role": "compiler",
            "subject": "the source compiler or narrator", "reviewedBy": "Fixture human reviewer"}
        with self.assertRaisesRegex(afp.DecisionError, "speechMarkerReviewed"):
            self.build(value)

    def test_governed_punctuation_and_graphic_variants(self):
        cases = [
            ("天上天下、唯我獨尊", "天上天下，唯我獨尊", "editorial-punctuation"),
            ("料掉沒交渉", "料掉沒交涉", "governed-graphic"),
        ]
        for display, query, kind in cases:
            packet = copy.deepcopy(self.packet); decision = copy.deepcopy(self.decision)
            packet["rows"][0]["term"] = display; packet["rows"][0]["searchTerm"] = query
            occurrence = decision["entries"][0]["senses"][0]["occurrences"][0]
            occurrence["kwic"] = f"上堂云{query}而後下座"; occurrence["exactHeadwordClause"] = query
            product = afp.build(packet, decision, "a" * 64, {"Fixture Master"})[0][2]
            emitted = product["Senses"][0]["Occurrences"][0]
            self.assertEqual(("variant", query, kind),
                             (emitted["EvidenceRole"], emitted["VariantForm"], emitted["VariantKind"]))
            self.assertTrue(audit_attribution.valid_governed_variant(emitted))
            self.assertTrue(audit_depth_sense.is_depth_headword_occurrence(emitted, display))

    def test_string_control_arrays_are_auditable(self):
        worksheet = self.build()[0][1]["Entry"]
        values = list(audit_batch_semantic_templates.semantic_strings(worksheet))
        self.assertTrue(any(field == "ModifierControls" for field, _, _ in values))
        self.assertTrue(any(field == "FamilyControls" for field, _, _ in values))

    def test_depth_count_failure_is_fail_closed(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "entry.v2.json"
            entry = self.build()[0][2]
            entry["Senses"][0]["Occurrences"][0]["RelPath"] = "X/X82/X82n1571.xml"
            path.write_text(json.dumps(entry, ensure_ascii=False), encoding="utf-8")
            (path.parent / "WORK.md").write_text("fixture", encoding="utf-8")
            result = audit_depth_sense.audit_entry(path, {"hits": 0, "files": 0, "works": 0,
                                                          "countError": "missing exact-path count"})
            self.assertFalse(result["hardPass"])
            self.assertIn("corpus-count-unavailable", {row["kind"] for row in result["hardFlags"]})

    def test_no_roster_write_and_collision_refusal(self):
        roster = Path("../../../../Assets/Data/lineage-masters.json").resolve()
        before = hashlib.sha256(roster.read_bytes()).hexdigest()
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            packet = root / "packet.json"; decisions = root / "decisions.json"; baseline = root / "baseline.json"; fixture_roster = root / "roster.json"
            packet.write_text(json.dumps(self.packet), encoding="utf-8")
            decisions.write_text(json.dumps(self.decision), encoding="utf-8")
            baseline.write_text(json.dumps({"manifestSha256": "a" * 64}), encoding="utf-8")
            fixture_roster.write_text(json.dumps([{"names": ["Fixture Master"]}]), encoding="utf-8")
            common = [str(packet), str(decisions), "--output-root", str(root / "out"), "--baseline", str(baseline), "--roster", str(fixture_roster)]
            self.assertEqual(0, afp.main(common))
            changed = copy.deepcopy(self.decision); changed["entries"][0]["senses"][0]["note"] = "A materially revised human note."
            decisions.write_text(json.dumps(changed), encoding="utf-8")
            self.assertEqual(2, afp.main(common))
        self.assertEqual(before, hashlib.sha256(roster.read_bytes()).hexdigest())


if __name__ == "__main__":
    unittest.main()
