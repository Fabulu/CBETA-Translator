import unittest

from audit_iriya_queue import Automaton, parse_rows


class IriyaAuditTests(unittest.TestCase):
    def test_parses_all_attested_and_component_only_rows(self):
        rows = parse_rows()
        self.assertEqual(2008, len(rows))
        self.assertEqual(163, sum(not row["query"] for row in rows))
        self.assertEqual(2008, len({row["id"] for row in rows}))

    def test_aho_scan_counts_overlapping_and_nested_patterns(self):
        automaton = Automaton(["入地獄如箭", "入地獄如箭射", "哈哈"])
        found = automaton.scan("入地獄如箭射哈哈哈")
        self.assertEqual(1, found[0])
        self.assertEqual(1, found[1])
        self.assertEqual(2, found[2])


if __name__ == "__main__":
    unittest.main()
