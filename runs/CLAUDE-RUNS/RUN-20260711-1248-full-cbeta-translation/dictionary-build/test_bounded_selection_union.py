import json
import re
import tempfile
import unittest
from pathlib import Path

import bounded_selection_union
from bounded_selection_union import PriorUnionError, build_union


class BoundedSelectionUnionTest(unittest.TestCase):
    def test_glob_mismatched_prior_manifest_fails_closed(self):
        with tempfile.TemporaryDirectory() as directory:
            maintenance = Path(directory)
            path = maintenance / "non-iriya-v7-depth-regeneration-r14-selection-a.json"
            path.write_text(
                json.dumps({"rows": [{"identityId": "t_existing"}]}),
                encoding="utf-8",
            )
            original = bounded_selection_union.SELECTION_RE
            try:
                bounded_selection_union.SELECTION_RE = re.compile(
                    r"non-iriya-v7-depth-regeneration-r(\\d+)-selection-a\.json"
                )
                with self.assertRaisesRegex(PriorUnionError, "fail-closed"):
                    build_union(maintenance, max_cohort=33)
            finally:
                bounded_selection_union.SELECTION_RE = original

    def test_valid_prior_manifest_builds_union(self):
        with tempfile.TemporaryDirectory() as directory:
            maintenance = Path(directory)
            path = maintenance / "non-iriya-v7-depth-regeneration-r14-selection-a.json"
            path.write_text(
                json.dumps({"rows": [{"identityId": "t_existing"}]}),
                encoding="utf-8",
            )
            union = build_union(maintenance, max_cohort=33)
            self.assertTrue(union["hardPass"])
            self.assertEqual(union["ids"], ["t_existing"])
            self.assertEqual(union["selectionManifestCount"], 1)


if __name__ == "__main__":
    unittest.main()
