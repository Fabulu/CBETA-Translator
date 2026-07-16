import importlib.util
import hashlib
import json
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
SPEC = importlib.util.spec_from_file_location("closure", HERE / "audit_repair_reason_closure.py")
closure = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(closure)


class RepairReasonClosureTest(unittest.TestCase):
    def test_observed_forbidden_term_is_rejected(self):
        entry = {"SourceTerm":"甲", "Senses":[{"Explanation":"inherited Buddhist vocabulary"}]}
        self.assertIn("forbidden-reader-term", {x["code"] for x in closure.detect_entry_defects(entry)})

    def test_observed_copied_sense_explanation_is_rejected(self):
        entry = {"SourceTerm":"知客", "Senses":[
            {"Explanation":"The guest prefect receives visitors."},
            {"Explanation":"The guest prefect receives visitors."},
        ]}
        self.assertIn("copied-sense-explanation", {x["code"] for x in closure.detect_entry_defects(entry)})

    def test_observed_malformed_notes_are_rejected(self):
        entry = {"SourceTerm":"單提", "Senses":[{"Occurrences":[{
            "Kwic":"單提", "AttributionNote":"Source record (x). Title: In the,; Yuanwu owns it."
        }]}]}
        self.assertIn("malformed-attribution-note", {x["code"] for x in closure.detect_entry_defects(entry)})

    def test_observed_question_turn_is_rejected(self):
        entry = {"SourceTerm":"接引", "Senses":[{"Occurrences":[{
            "Kwic":"問：和尚以那一門接引後學？師云：棒喝兩途。", "MasterName":"Konggu Daocheng"
        }]}]}
        self.assertIn("unresolved-question-turn", {x["code"] for x in closure.detect_entry_defects(entry)})

    def test_rejected_fragments_are_extracted(self):
        reason = "Remove the broken clause 'occurs 140 times in .' before review."
        self.assertEqual(closure.quoted_rejection_fragments(reason), ["occurs 140 times in ."])

    def test_missing_closure_and_unchanged_rejected_prose_fail_closed(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            entries = root / "entries" / "t_x"
            entries.mkdir(parents=True)
            entry = {"SourceTerm":"甲", "Senses":[{"Explanation":"occurs 140 times in ."}]}
            entry_path = entries / "entry.v2.json"
            entry_path.write_text(json.dumps(entry), encoding="utf-8")
            current_sha = hashlib.sha256(entry_path.read_bytes()).hexdigest()
            review = root / "review.json"
            review.write_text(json.dumps({"reviseRows":[{
                "id":"t_x", "term":"甲", "entrySha256":"old",
                "reason":"Remove 'occurs 140 times in .' before review."
            }]}), encoding="utf-8")
            repair = root / "repair.json"
            repair.write_text(json.dumps({"rows":[{
                "id":"t_x", "afterSha256":current_sha
            }]}), encoding="utf-8")
            result = closure.audit(review, repair, root / "entries")
            codes = {finding["code"] for finding in result["rows"][0]["findings"]}
            self.assertFalse(result["hardPass"])
            self.assertIn("missing-explicit-reason-closure", codes)
            self.assertIn("rejected-prose-substantially-unchanged", codes)


if __name__ == "__main__":
    unittest.main()
