import json
import unittest
from pathlib import Path

from quick_ownership import partition_rows, quick_rows, validate_partition


class QuickOwnershipTests(unittest.TestCase):
    def test_occurrence_class_overrides_mixed_cluster_class(self):
        payload = {"sources": [{"RelPath": "X.xml", "clusters": [{
            "reviewClass": "full-ladder-or-parallel-needed",
            "occurrences": [
                {"entryId": "quick-inside-full", "RelPath": "X.xml", "FromLb": "1", "Kwic": "a", "reviewClass": "co-located-reviewed-candidate"},
                {"entryId": "full", "RelPath": "X.xml", "FromLb": "2", "Kwic": "b", "reviewClass": "full-ladder-or-parallel-needed"},
            ],
        }, {
            "reviewClass": "anthology-header-candidate",
            "occurrences": [
                {"entryId": "full-inside-quick", "RelPath": "X.xml", "FromLb": "3", "Kwic": "c", "reviewClass": "full-ladder-or-parallel-needed"},
                {"entryId": "quick", "RelPath": "X.xml", "FromLb": "4", "Kwic": "d", "reviewClass": "anthology-header-candidate"},
            ],
        }]}]}
        self.assertEqual({row["entryId"] for row in quick_rows(payload, {"X.xml"})}, {"quick-inside-full", "quick"})

    def test_partition_is_complete_unique_and_entry_exclusive(self):
        rows = [
            {"entryId": "a", "RelPath": "X", "FromLb": "1", "Kwic": "a1"},
            {"entryId": "a", "RelPath": "Y", "FromLb": "2", "Kwic": "a2"},
            {"entryId": "b", "RelPath": "X", "FromLb": "3", "Kwic": "b"},
            {"entryId": "c", "RelPath": "X", "FromLb": "4", "Kwic": "c"},
        ]
        workers = partition_rows(rows, 2)
        validate_partition(rows, workers)
        owners = [{row["entryId"] for row in worker} for worker in workers]
        self.assertFalse(owners[0] & owners[1])
        self.assertEqual(sum(map(len, workers)), len(rows))

    def test_current_triage_is_consistent_with_closed_attribution_repair(self):
        payload = json.loads(Path("maintenance/attribution-triage-all.json").read_text(encoding="utf-8"))
        rows = quick_rows(payload, {"X/X80/X80n1565.xml", "X/X82/X82n1571.xml", "T/T51/T51n2076.xml"})
        # This durable artifact is regenerated from the current termbase.  The
        # legacy attribution repair is now closed, so historical row IDs and
        # counts are deliberately absent.  Keep the test tied to the artifact's
        # declared state instead of pinning a pre-closure snapshot forever.
        metrics = payload["metrics"]
        self.assertEqual(metrics["allUnresolvedOccurrences"], 0)
        self.assertEqual(metrics["selectedOccurrences"], 0)
        self.assertEqual(payload["sources"], [])
        self.assertEqual(rows, [])


if __name__ == "__main__":
    unittest.main()
