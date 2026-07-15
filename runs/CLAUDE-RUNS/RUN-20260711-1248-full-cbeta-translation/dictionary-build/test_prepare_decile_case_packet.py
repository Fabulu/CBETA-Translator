import unittest

from prepare_decile_case_packet import build_source_groups


class SourceGroupPacketTests(unittest.TestCase):
    def test_groups_cases_by_source_and_line_without_losing_entry_refs(self):
        entries = [
            {"ordinal": 2, "id": "t_b", "term": "乙", "cases": [
                {"RelPath": "X/a.xml", "sourceTitle": "A", "workId": "work:a",
                 "sense": 1, "kind": "occurrence", "index": 1, "FromLb": "002b", "ToLb": "002c"},
            ]},
            {"ordinal": 1, "id": "t_a", "term": "甲", "cases": [
                {"RelPath": "X/a.xml", "sourceTitle": "A", "workId": "work:a",
                 "sense": 1, "kind": "occurrence", "index": 1, "FromLb": "001a", "ToLb": "001b"},
                {"RelPath": "J/b.xml", "sourceTitle": "B", "workId": "work:b",
                 "sense": 1, "kind": "occurrence", "index": 2, "FromLb": "003a", "ToLb": "003b"},
            ]},
        ]
        groups = build_source_groups(entries)
        self.assertEqual(["J/b.xml", "X/a.xml"], [group["RelPath"] for group in groups])
        self.assertEqual([1, 2], [row["ordinal"] for row in groups[1]["caseRefs"]])
        self.assertEqual(3, sum(len(group["caseRefs"]) for group in groups))


if __name__ == "__main__":
    unittest.main()
