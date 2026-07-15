#!/usr/bin/env python3

import unittest

from run_cohort_gate import public_feedback_hard_pass


class PublicFeedbackCohortGateTests(unittest.TestCase):
    def test_requires_zero_flags(self):
        self.assertTrue(public_feedback_hard_pass({
            "exitCode": 0, "payload": {"entries": 5, "passing": 5, "flagged": 0}
        }))
        self.assertFalse(public_feedback_hard_pass({
            "exitCode": 0, "payload": {"entries": 5, "passing": 4, "flagged": 1}
        }))

    def test_rejects_missing_or_failed_audit(self):
        self.assertFalse(public_feedback_hard_pass({"exitCode": 0, "payload": None}))
        self.assertFalse(public_feedback_hard_pass({
            "exitCode": 1, "payload": {"flagged": 0}
        }))


if __name__ == "__main__":
    unittest.main()
