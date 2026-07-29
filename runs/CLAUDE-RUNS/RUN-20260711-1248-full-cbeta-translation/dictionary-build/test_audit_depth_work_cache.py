import tempfile
import unittest
from pathlib import Path

import audit_depth_sense as depth


class DepthWorkCacheTests(unittest.TestCase):
    def test_work_ledger_change_invalidates_entry_cache_key(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            entry = root / "entry.v2.json"
            work = root / "WORK.md"
            entry.write_text('{"Id":"t_fixture"}\n', encoding="utf-8")
            work.write_text("first ledger\n", encoding="utf-8")
            first = depth.audit_cache_key(entry, {"hits": 1, "files": 1}, "a" * 64)
            work.write_text("second ledger\n", encoding="utf-8")
            second = depth.audit_cache_key(entry, {"hits": 1, "files": 1}, "a" * 64)
            self.assertNotEqual(first, second)

    def test_pairwise_same_class_rulings_collapse_transitively(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            entry = root / "entry.v2.json"
            entry.write_text("{}\n", encoding="utf-8")
            (root / "WORK.md").write_text(
                "deployment-duplication-ruling: s1:o1,o2=same-class; depth-count=1; reason=fixture.\n"
                "deployment-duplication-ruling: s1:o2,o3=same-class; depth-count=1; reason=fixture.\n",
                encoding="utf-8",
            )
            senses = [{"Occurrences": [
                {"Kwic": "甲詞", "EvidenceRole": "headword"},
                {"Kwic": "甲詞", "EvidenceRole": "headword"},
                {"Kwic": "甲詞", "EvidenceRole": "headword"},
                {"Kwic": "甲詞", "EvidenceRole": "headword"},
            ]}]
            result = depth.effective_deployment_classes(entry, senses, "甲詞")
            self.assertEqual(result[0]["count"], 2)
            self.assertEqual(result[0]["classes"], [[1, 2, 3], [4]])

    def test_distinct_class_ruling_does_not_collapse(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            entry = root / "entry.v2.json"
            entry.write_text("{}\n", encoding="utf-8")
            (root / "WORK.md").write_text(
                "deployment-duplication-ruling: s1:o1,o2=distinct-class; depth-count=2; reason=fixture.\n",
                encoding="utf-8",
            )
            senses = [{"Occurrences": [{"Kwic": "甲詞"}, {"Kwic": "甲詞"}]}]
            result = depth.effective_deployment_classes(entry, senses, "甲詞")
            self.assertEqual(result[0]["count"], 2)


if __name__ == "__main__":
    unittest.main()
