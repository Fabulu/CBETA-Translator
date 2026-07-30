#!/usr/bin/env python3
import unittest

from maintenance.r80_jiufeng_grammar_spec import USES as JIUFENG
from maintenance.r80_direct_family_spec import USES as JIXIANG
from maintenance.actor_note_format import format_actor_note


class ExactGrammarFrameTest(unittest.TestCase):
    def test_note_format_contains_exact_canonical_master_name(self):
        for rel, master, _family, grammar in [*JIUFENG, *JIXIANG]:
            note = format_actor_note(rel, "Recorded Sayings of Test Master", master, grammar)
            self.assertIn(f"Exact actor: {master}.", note)
            self.assertIn("Recorded Sayings of Test Master", note)

    def test_jiufeng_frames_bind_exact_speech_markers(self):
        joined = "\n".join(row[3] for row in JIUFENG)
        for marker in ("師拈云", "拈曰", "視左右，云", "師云"):
            self.assertIn(marker, joined)
        self.assertNotIn("complete retained turn assigns", joined)

    def test_huanxi_title_and_frame_are_not_generic(self):
        huanxi = JIXIANG[-1]
        self.assertEqual("X/X70/X70n1388.xml", huanxi[0])
        self.assertIn("Huanxi", huanxi[1])
        self.assertIn("除夜", huanxi[3])
        self.assertIn("明年明日來，朝朝雞向五更啼", huanxi[3])


if __name__ == "__main__":
    unittest.main()
