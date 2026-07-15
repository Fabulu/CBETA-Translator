import json
import unittest
from pathlib import Path


class IriyaSemanticPacketTests(unittest.TestCase):
    def test_packets_cover_sha_bound_audit_exactly_once(self):
        root = Path("fresh-build/iriya-admission")
        ledger = json.loads((root / "ledger.json").read_text(encoding="utf-8"))
        audit = json.loads(Path("IRIYA_PREBUILD_AUDIT.json").read_text(encoding="utf-8"))
        baseline = json.loads(Path("fresh-build/corpus-baseline.json").read_text(encoding="utf-8"))
        rows = []
        for packet_row in ledger["packets"]:
            packet = json.loads((root / packet_row["path"]).read_text(encoding="utf-8"))
            self.assertEqual(packet["corpusManifestSha256"], baseline["manifestSha256"])
            self.assertEqual(packet["candidateCount"], len(packet["rows"]))
            rows.extend(packet["rows"])
        self.assertEqual(ledger["corpusManifestSha256"], baseline["manifestSha256"])
        self.assertEqual(len(rows), len(audit["rows"]))
        self.assertEqual([row["id"] for row in rows], [row["id"] for row in audit["rows"]])
        self.assertEqual(len({row["id"] for row in rows}), 2008)
        self.assertTrue(all(row["disposition"] is None for row in rows))


if __name__ == "__main__":
    unittest.main()
