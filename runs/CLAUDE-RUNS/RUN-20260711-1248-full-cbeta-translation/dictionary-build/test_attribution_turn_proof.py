import unittest

from attribution_packet import turn_proof_candidates


class TurnProofCandidateTests(unittest.TestCase):
    def test_question_headword_is_not_assigned_to_following_response(self):
        rows = turn_proof_candidates("僧問如何是宗乘。師云庭前柏樹子。", "宗乘")
        self.assertEqual(len(rows), 1)
        self.assertEqual(rows[0]["headwordClause"], "僧問如何是宗乘。")
        self.assertIn("僧問", rows[0]["nearestPrecedingCue"]["text"])
        self.assertIn("師云", rows[0]["nearestFollowingCue"]["text"])

    def test_response_headword_uses_response_cue(self):
        rows = turn_proof_candidates("僧問如何是佛。師云麻三斤。", "麻三斤")
        self.assertEqual(rows[0]["headwordClause"], "師云麻三斤。")
        self.assertIn("師云", rows[0]["nearestPrecedingCue"]["text"])

    def test_absent_headword_has_no_proof(self):
        self.assertEqual(turn_proof_candidates("師云庭前柏樹子。", "宗乘"), [])


if __name__ == "__main__":
    unittest.main()
