import unittest

from remove_public_entry_atomic import authorized_removal


class RemovalAuthorityTests(unittest.TestCase):
    def test_exact_recoverable_removal_authority(self):
        authority = {
            "rows": [
                {
                    "id": "t_target",
                    "term": "target",
                    "decision": "AUTHORIZE_RECOVERABLE_REMOVAL",
                }
            ]
        }
        self.assertTrue(authorized_removal(authority, "t_target", "target"))
        self.assertFalse(authorized_removal(authority, "t_other", "target"))
        self.assertFalse(authorized_removal(authority, "t_target", "other"))

    def test_hold_is_not_removal_authority(self):
        authority = {
            "rows": [
                {"id": "t_target", "term": "target", "decision": "HOLD"}
            ]
        }
        self.assertFalse(authorized_removal(authority, "t_target", "target"))


if __name__ == "__main__":
    unittest.main()
