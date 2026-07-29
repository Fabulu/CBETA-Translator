#!/usr/bin/env python3

import json
import tempfile
import unittest
from pathlib import Path
from unittest import mock

import construct_non_iriya_v6_batch44_ab as constructor
from compile_evidence_draft import (
    required_depth_floor,
    validate_new_entry_depth,
)


def occurrence(number, work=None):
    return {
        "RelPath": f"T/source-{number}.xml",
        "FromLb": f"0001a{number:02d}",
        "ToLb": f"0001a{number:02d}",
        "Kwic": f"師云測試{number}",
        "MasterName": "Test Master",
    }, {
        "EvidenceKey": f"o{number}",
        "RelPath": f"T/source-{number}.xml",
        "WorkId": work or f"work:{number}",
        "Tier": 2,
        "SourceClass": "recorded-sayings",
        "AuthorityReason": "direct test witness",
        "WitnessFamilyId": f"family:{number}",
        "DeploymentRole": "original-use",
    }


def sense_with(count, works=None):
    pairs = [
        occurrence(index, (works or [None] * count)[index - 1])
        for index in range(1, count + 1)
    ]
    return {
        "Occurrences": [pair[0] for pair in pairs],
        "DraftEvidence": {
            "SourceAuthorityRows": [pair[1] for pair in pairs],
            "DepthHarvestReceipt": {
                "Complete": True,
                "SearchedDeploymentClasses": ["answers", "questions", "appraisals"],
                "OmissionAudit": ["All distinct retained and excluded classes adjudicated."],
                "ReviewedExactHitCount": 68,
                "AvailableSourceFiles": count,
            },
        },
    }


class NewEntryDepthGuardTests(unittest.TestCase):
    def test_guide_floor_boundaries(self):
        expected = {
            0: 0, 1: 1, 2: 2, 3: 3, 19: 3, 20: 4, 99: 4,
            100: 6, 499: 6, 500: 7, 1999: 7, 2000: 8,
            9999: 8, 10000: 10,
        }
        self.assertEqual(
            expected,
            {hits: required_depth_floor(hits) for hits in expected},
        )

    def test_rejects_duplicate_witness_and_under_floor(self):
        sense = sense_with(2)
        sense["Occurrences"][1] = dict(sense["Occurrences"][0])
        errors = []
        validate_new_entry_depth({"ExactCount": 68}, sense, 1, errors)
        self.assertTrue(any("duplicate retained witness" in error for error in errors))
        self.assertTrue(any("guide depth floor is 4" in error for error in errors))

    def test_accepts_floor_only_with_completed_harvest_receipt(self):
        sense = sense_with(4)
        errors = []
        validate_new_entry_depth({"ExactCount": 68}, sense, 1, errors)
        self.assertEqual([], errors)

        del sense["DraftEvidence"]["DepthHarvestReceipt"]
        errors = []
        validate_new_entry_depth({"ExactCount": 68}, sense, 1, errors)
        self.assertTrue(any("completed deployment-harvest receipt required" in error for error in errors))

    def test_hundred_plus_hits_require_four_works_when_available(self):
        sense = sense_with(6, ["work:a", "work:a", "work:b", "work:b", "work:c", "work:c"])
        receipt = sense["DraftEvidence"]["DepthHarvestReceipt"]
        receipt["ReviewedExactHitCount"] = 117
        receipt["AvailableSourceFiles"] = 8
        errors = []
        validate_new_entry_depth({"ExactCount": 117}, sense, 1, errors)
        self.assertTrue(any("require four source works" in error for error in errors))

    def test_shared_constructor_rejects_manufactured_second_source(self):
        row = ("T/test.xml", "Test Master", "direct test", "original-use")
        config = (
            "t_test", "測試", "test", "a test deployment", "bounded",
            [row, row], ["test"],
        )
        with self.assertRaisesRegex(ValueError, "duplicate retained source rows"):
            constructor.build(config)

    def test_shared_constructor_supports_one_source_without_copying_it(self):
        row = ("T/test.xml", "Test Master", "direct test", "original-use")
        config = (
            "t_test", "測試", "test", "a test deployment", "bounded",
            [row], ["test"],
        )
        occ, authority = occurrence(1)
        with tempfile.TemporaryDirectory() as temp:
            old_out = constructor.OUT
            constructor.OUT = Path(temp)
            constructor.FAMILY["測試"] = (
                "reject", "", "測試", "semantic-neighbor", "not a duplicate"
            )
            constructor.SEMANTIC_CONTROLS["測試"] = (
                "alias bounded", "modifier bounded", "family bounded"
            )
            try:
                with mock.patch.object(
                    constructor.zc, "count",
                    return_value={"hits": 1, "files": 1, "works": 1, "per_file": []},
                ), mock.patch.object(
                    constructor, "occurrence", return_value=(occ, authority)
                ):
                    constructor.build(config)
                payload = json.loads(
                    (Path(temp) / "t_test" / "evidence.draft.json").read_text()
                )
            finally:
                constructor.OUT = old_out
        sense = payload["Entry"]["Senses"][0]
        self.assertEqual(1, len(sense["Occurrences"]))
        self.assertEqual(1, len(sense["ExplanationParts"]["EvidenceBody"]))
        self.assertEqual("provisional", sense["Validation"])


if __name__ == "__main__":
    unittest.main()
