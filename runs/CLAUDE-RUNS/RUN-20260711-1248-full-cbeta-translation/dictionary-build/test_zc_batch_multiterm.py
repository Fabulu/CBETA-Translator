#!/usr/bin/env python3

import unittest
from unittest import mock

import zc_batch


class MultiTermBatchCountTest(unittest.TestCase):
    def test_cli_count_uses_one_batch_and_preserves_result_shape(self):
        counted = {
            "甲": {"hits": 3, "files": 2, "works": 2, "per_file": [("a.xml", 2), ("b.xml", 1)]},
            "乙": {"hits": 1, "files": 1, "works": 1, "per_file": [("c.xml", 1)]},
        }
        with mock.patch.object(zc_batch.zc, "batch_count", return_value=counted) as batch:
            result = zc_batch.count_jobs(["甲", "乙"], per_file=True, top_files=1)
        batch.assert_called_once_with(["甲", "乙"])
        self.assertEqual(
            [
                {"op": "count", "term": "甲", "hits": 3, "files": 2, "works": 2, "per_file": [("a.xml", 2)]},
                {"op": "count", "term": "乙", "hits": 1, "files": 1, "works": 1, "per_file": [("c.xml", 1)]},
            ],
            result,
        )

    def test_cli_count_omits_histogram_by_default(self):
        counted = {
            "甲": {"hits": 3, "files": 2, "works": 2, "per_file": [("a.xml", 2), ("b.xml", 1)]},
        }
        with mock.patch.object(zc_batch.zc, "batch_count", return_value=counted):
            result = zc_batch.count_jobs(["甲"])
        self.assertNotIn("per_file", result[0])


if __name__ == "__main__":
    unittest.main()
