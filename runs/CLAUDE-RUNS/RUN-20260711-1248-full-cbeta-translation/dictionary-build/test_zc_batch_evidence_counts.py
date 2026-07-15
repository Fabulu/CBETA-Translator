import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from zc_batch import verify_entries


class EvidenceCountTests(unittest.TestCase):
    def test_occurrences_and_claim_anchors_are_reported_separately(self):
        entry = {
            "Id": "t_test",
            "SourceTerm": "話",
            "Senses": [{
                "Occurrences": [
                    {"RelPath": "X.xml", "FromLb": "1", "ToLb": "1", "Kwic": "甲"},
                    {"RelPath": "X.xml", "FromLb": "2", "ToLb": "2", "Kwic": "乙"},
                ],
                "ClaimAnchors": [
                    {"RelPath": "X.xml", "FromLb": "3", "ToLb": "3", "Kwic": "丙"},
                ],
            }],
        }
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "entry.v2.json"
            path.write_text(json.dumps(entry), encoding="utf-8")
            with patch("zc_batch.zc.verify", side_effect=[
                {"ok": True, "fromLb": "1", "toLb": "1"},
                {"ok": True, "fromLb": "2", "toLb": "2"},
                {"ok": True, "fromLb": "3", "toLb": "3"},
            ]):
                result = verify_entries([path])
        self.assertEqual(result["verified"], 3)
        self.assertEqual(result["occurrenceVerified"], 2)
        self.assertEqual(result["claimAnchorVerified"], 1)
        self.assertEqual(result["results"][0]["occurrenceVerified"], 2)
        self.assertEqual(result["results"][0]["claimAnchorVerified"], 1)


if __name__ == "__main__":
    unittest.main()
