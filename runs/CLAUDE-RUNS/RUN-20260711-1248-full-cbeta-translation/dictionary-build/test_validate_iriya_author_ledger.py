#!/usr/bin/env python3
import copy
import unittest
from unittest.mock import patch

import validate_iriya_author_ledger as validator


AUTHORITY = {
    "t_test": {
        "queueNumber": 1,
        "canonicalIndex": 0,
        "id": "t_test",
        "term": "甲乙",
        "query": "甲乙",
    }
}


def valid_ledger():
    return {
        "reviewedCount": 1,
        "offset": 0,
        "decisions": [{
            "batchOrdinal": 1,
            **AUTHORITY["t_test"],
            "disposition": "KEEP (component)",
            "zcExact": {"hits": 1, "files": 1, "distinctWorks": 1},
            "evidence": [{
                "source": "X/test.xml",
                "workId": "work:test",
                "title": "Test",
                "hitFromLb": "0001a01",
                "hitToLb": "0001a01",
                "kwic": "前甲乙後",
            }],
        }],
    }


class ValidatorTests(unittest.TestCase):
    def test_good_cheap(self):
        self.assertEqual(validator.validate_ledger(valid_ledger(), AUTHORITY), [])

    def test_empty_and_count_mismatch_fail(self):
        self.assertTrue(validator.validate_ledger({"reviewedCount": 0, "decisions": []}, AUTHORITY))
        ledger = valid_ledger()
        ledger["reviewedCount"] = 2
        self.assertTrue(any("reviewedCount" in failure for failure in validator.validate_ledger(ledger, AUTHORITY)))

    def test_ordinal_and_offset_fail(self):
        ledger = valid_ledger()
        ledger["decisions"][0]["batchOrdinal"] = 2
        ledger["offset"] = 1
        failures = validator.validate_ledger(ledger, AUTHORITY)
        self.assertTrue(any("batchOrdinal" in failure for failure in failures))
        self.assertTrue(any("modulo" in failure for failure in failures))

    def test_query_resolution_requires_contained_attested_form(self):
        ledger = valid_ledger()
        witness = ledger["decisions"][0]["evidence"][0]
        witness["kwic"] = "前甲，乙後"
        witness["queryResolution"] = "punctuation variant"
        self.assertTrue(any("attestedForm" in failure for failure in validator.validate_ledger(ledger, AUTHORITY)))
        witness["queryResolution"] = {"attestedForm": "甲，乙", "reason": "editorial punctuation"}
        self.assertEqual(validator.validate_ledger(ledger, AUTHORITY), [])

    def test_known_wrong_row_payload_pattern_fails(self):
        ledger = valid_ledger()
        ledger["decisions"][0]["query"] = "丙丁"
        ledger["decisions"][0]["evidence"][0]["kwic"] = "前丙丁後"
        failures = validator.validate_ledger(ledger, AUTHORITY)
        self.assertTrue(any("query=" in failure for failure in failures))

    def test_full_missing_source_and_kwic_fail_closed(self):
        ledger = valid_ledger()
        witness = ledger["decisions"][0]["evidence"][0]
        witness["source"] = ""
        witness["kwic"] = ""
        with patch.object(validator.zc, "batch_count", return_value={"甲乙": {"hits": 1, "files": 1, "works": 1}}):
            failures = validator.validate_ledger(ledger, AUTHORITY, full=True)
        self.assertTrue(any("missing source" in failure for failure in failures))
        self.assertTrue(any("missing KWIC" in failure for failure in failures))

    def test_full_missing_query_is_structured_failure_not_exception(self):
        ledger = valid_ledger()
        del ledger["decisions"][0]["query"]
        with (
            patch.object(validator.zc, "batch_count", return_value={}),
            patch.object(validator.zc, "verify", return_value={"ok": True, "fromLb": "0001a01", "toLb": "0001a01"}),
            patch.object(validator.zc, "work_id", return_value="work:test"),
            patch.object(validator.zc, "title", return_value="Test"),
        ):
            failures = validator.validate_ledger(ledger, AUTHORITY, full=True)
        self.assertTrue(any("query=" in failure for failure in failures))

    def test_full_checks_zc_fields(self):
        ledger = valid_ledger()
        with (
            patch.object(validator.zc, "verify", return_value={"ok": True, "fromLb": "0001a01", "toLb": "0001a01"}),
            patch.object(validator.zc, "work_id", return_value="work:test"),
            patch.object(validator.zc, "title", return_value="Test"),
            patch.object(validator.zc, "batch_count", return_value={"甲乙": {"hits": 1, "files": 1, "works": 1}}),
        ):
            self.assertEqual(validator.validate_ledger(ledger, AUTHORITY, full=True), [])


if __name__ == "__main__":
    unittest.main()
