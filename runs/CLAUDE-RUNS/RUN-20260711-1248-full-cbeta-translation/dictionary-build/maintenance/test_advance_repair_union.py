import json
import tempfile
import unittest
from unittest.mock import patch
from pathlib import Path

from maintenance.advance_repair_union import advance
from maintenance.advance_repair_union import sha256


class AdvanceRepairUnionTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)

    def tearDown(self):
        self.tmp.cleanup()

    def write(self, name, value):
        path = self.root / name
        path.write_text(json.dumps(value), encoding="utf-8")
        return path

    def prior(self, ids=("a", "b")):
        return {"schemaVersion": "receipt-first-prior-union.v1",
                "ids": list(ids), "uniqueIdCount": len(ids), "hardPass": True}

    def ledger(self, ids=("c", "d")):
        return {
            "schemaVersion": "authoritative-catastrophe-ledger-advancement.v1",
            "cohort": "RTEST", "sealed": True, "arithmeticHardPass": True,
            "windowsGitPush": True, "windowsNodeMerge": True,
            "publicIntegrity": {"hardPass": True, "entryCount": 7,
                "aggregateCount": 7, "legacyCount": 7, "indexCount": 7,
                "shardCount": 7, "exactProductParity": f"{len(ids)}/{len(ids)}"},
            "publicCommit": "a" * 40,
            "publishedIds": list(ids), "authoritativeRemainderBefore": 10,
            "resolvedThisAdvancement": len(ids),
            "authoritativeRemainderAfter": 10 - len(ids),
        }

    def test_duplicate_published_ids_fail_without_output(self):
        prior = self.write("prior.json", self.prior())
        ledger = self.write("ledger.json", self.ledger(("c", "c")))
        output = self.root / "out.json"
        with self.assertRaisesRegex(ValueError, "duplicate"):
            advance(prior, ledger, output, sha256(ledger))
        self.assertFalse(output.exists())

    def test_overlap_fails_without_output(self):
        prior = self.write("prior.json", self.prior())
        ledger = self.write("ledger.json", self.ledger(("b", "c")))
        output = self.root / "out.json"
        with self.assertRaisesRegex(ValueError, "already reserved"):
            advance(prior, ledger, output, sha256(ledger))
        self.assertFalse(output.exists())

    def test_tampered_arithmetic_fails_without_output(self):
        prior = self.write("prior.json", self.prior())
        value = self.ledger()
        value["authoritativeRemainderAfter"] = 9
        ledger = self.write("ledger.json", value)
        output = self.root / "out.json"
        with self.assertRaisesRegex(ValueError, "arithmetic"):
            advance(prior, ledger, output, sha256(ledger))
        self.assertFalse(output.exists())

    def test_success_preserves_prior_order_and_binds_inputs(self):
        prior = self.write("prior.json", self.prior())
        ledger = self.write("ledger.json", self.ledger())
        output = self.root / "out.json"
        result = advance(prior, ledger, output, sha256(ledger))
        self.assertEqual(result["ids"], ["a", "b", "c", "d"])
        self.assertEqual(result["countArithmetic"], {
            "prior": 2, "added": 2, "result": 4, "hardPass": True})
        self.assertEqual(json.loads(output.read_text()), result)

    def test_forged_self_sealed_wrong_schema_and_hash_fail(self):
        prior = self.write("prior.json", self.prior())
        value = self.ledger()
        value["schemaVersion"] = "forged-authority.v1"
        ledger = self.write("ledger.json", value)
        output = self.root / "out.json"
        with self.assertRaisesRegex(ValueError, "binding mismatch"):
            advance(prior, ledger, output, "0" * 64)
        with self.assertRaisesRegex(ValueError, "schemaVersion"):
            advance(prior, ledger, output, sha256(ledger))
        self.assertFalse(output.exists())

    def test_prior_count_tamper_fails_without_output(self):
        value = self.prior()
        value["uniqueIdCount"] = 99
        prior = self.write("prior.json", value)
        ledger = self.write("ledger.json", self.ledger())
        output = self.root / "out.json"
        with self.assertRaisesRegex(ValueError, "uniqueIdCount"):
            advance(prior, ledger, output, sha256(ledger))
        self.assertFalse(output.exists())

    def test_ledger_is_hashed_and_parsed_from_one_byte_read(self):
        prior = self.write("prior.json", self.prior())
        ledger = self.write("ledger.json", self.ledger())
        output = self.root / "out.json"
        ledger_bytes = ledger.read_bytes()
        original = type(ledger).read_bytes
        calls = []

        def guarded_read(path):
            if path == ledger:
                calls.append(path)
                if len(calls) > 1:
                    raise AssertionError("second ledger read could substitute bytes")
                return ledger_bytes
            return original(path)

        with patch.object(type(ledger), "read_bytes", guarded_read):
            result = advance(prior, ledger, output,
                             __import__("hashlib").sha256(ledger_bytes).hexdigest())
        self.assertTrue(result["hardPass"])
        self.assertEqual(len(calls), 1)


if __name__ == "__main__":
    unittest.main()
