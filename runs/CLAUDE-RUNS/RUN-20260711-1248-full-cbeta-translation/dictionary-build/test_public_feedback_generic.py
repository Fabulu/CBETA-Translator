import json
import shutil
import tempfile
import unittest
from pathlib import Path

from audit_public_feedback import LEDGER_KEYS, audit


class PublicFeedbackGenericTests(unittest.TestCase):
    def write_entry(self, explanation: str) -> Path:
        build_root = Path(__file__).resolve().parent
        temporary = Path(tempfile.mkdtemp(dir=build_root))
        self.addCleanup(shutil.rmtree, temporary)
        root = temporary / "t_test"
        root.mkdir()
        (root / "entry.v2.json").write_text(json.dumps({
            "Id": "t_test", "SourceTerm": "試語", "Senses": [{
                "PreferredTarget": "test expression",
                "Explanation": explanation,
                "SearchAliases": ["test expression"],
                "Occurrences": [],
            }],
        }), encoding="utf-8")
        (root / "WORK.md").write_text("\n".join(f"{key} checked" for key in LEDGER_KEYS), encoding="utf-8")
        return root / "entry.v2.json"

    def test_generic_figure_prose_fails_cohort_audit(self):
        path = self.write_entry(
            "Manjusri is the figure the records place inside Zen cases, quotations, and public questions."
        )
        result = audit(path)
        self.assertFalse(result["passes"])
        self.assertTrue(any(flag["kind"] == "generic-template-prose" for flag in result["flags"]))

    def test_review_process_sentence_fails_reader_prose(self):
        path = self.write_entry(
            "The phrase names family disgrace in the school. Complete-unit reading separates direct speech, "
            "quoted verse, authored exposition, invitation or memorial prose, action narration, and duplicate recensions."
        )
        result = audit(path)
        self.assertFalse(result["passes"])
        self.assertTrue(any(flag["kind"] == "generic-template-prose" for flag in result["flags"]))

    def test_specific_prose_is_not_flagged_as_generic(self):
        path = self.write_entry(
            "The bird course pictures flight through open sky: a passage can be made, but no track remains for a follower."
        )
        result = audit(path)
        self.assertTrue(result["passes"], result["flags"])

    def test_relative_entry_path_runs_the_same_gate(self):
        path = self.write_entry(
            "A test expression is the plain-English referent tested by the selected Chan records."
        )
        relative = path.relative_to(Path(__file__).resolve().parent)
        result = audit(relative)
        self.assertFalse(result["passes"])
        self.assertTrue(any(flag["kind"] == "generic-template-prose" for flag in result["flags"]))

    def test_consecutive_duplicate_opening_fails(self):
        path = self.write_entry(
            "The staff is held crosswise in the case. The staff is held crosswise in the case. A monk then answers."
        )
        result = audit(path)
        self.assertFalse(result["passes"])
        self.assertTrue(any(flag["kind"] == "duplicated-explanation-opening" for flag in result["flags"]))

    def test_later_recurrence_is_not_mistaken_for_opening_duplication(self):
        path = self.write_entry(
            "The staff is held crosswise in the case. A monk answers, and the staff is held crosswise in the case."
        )
        result = audit(path)
        self.assertTrue(result["passes"], result["flags"])


if __name__ == "__main__":
    unittest.main()
