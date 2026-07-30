#!/usr/bin/env python3
import unittest
from pathlib import Path

from maintenance.adjudicated_actor_adapter import adapt_actor, builder_use, merge_context_masters, verify_builder_uses


class ActorAdapterTests(unittest.TestCase):
    def test_roster_master(self):
        row=adapt_actor(kind="roster-master",label="Huangbo Xiyun",role="utterer")
        self.assertEqual("Huangbo Xiyun",row["master"])

    def test_quoted_nonroster_figure_with_later_quoter(self):
        row=adapt_actor(kind="quoted-nonroster-master",label="Shakyamuni",role="quoted-speaker",
          context_masters=[{"MasterName":"Dahui Zonggao","Roles":["later-quoter","commentator","record-owner"]}])
        self.assertEqual("identified-unlinked-master",row["status"])
        self.assertEqual("utterer",row["role"])
        self.assertEqual("Dahui Zonggao",row["contextMasters"][0]["MasterName"])

    def test_named_nonmaster_author(self):
        row=adapt_actor(kind="named-nonmaster-author",label="Wang Kentang",role="author")
        self.assertEqual("identified-non-master",row["status"])
        self.assertEqual("utterer",row["role"])

    def test_unnamed_questioner(self):
        row=adapt_actor(kind="unnamed-questioner",
          label="the unnamed monk questioning Tianyin Yuanxiu",role="questioner",
          context_masters=[{"MasterName":"Tianyin Yuanxiu","Roles":["respondent","record-owner"]}])
        self.assertEqual("reviewed-unnamed",row["status"])

    def test_early_builder_use_closure_with_commentator(self):
        actor=adapt_actor(kind="roster-master",label="Huangbo Xiyun",role="utterer",
          context_masters=[{"MasterName":"Juelang Daosheng","Roles":["later-quoter","commentator","record-owner"]}])
        spec={"id":"fixture","uses":[builder_use(actor,"J/J25/J25nB174.xml","family",2)]}
        verify_builder_uses([spec])

    def test_rejects_noncanonical_exact_role(self):
        with self.assertRaisesRegex(ValueError,"exact actor role"):
            adapt_actor(kind="roster-master",label="Huangbo Xiyun",role="author")

    def test_verify_builder_use_rejects_handwritten_role(self):
        with self.assertRaisesRegex(ValueError,"invalid exact actor role"):
            verify_builder_uses([{"id":"x","uses":[
                ("p","Huangbo Xiyun","f",2,"quoted-speaker",None,"linked",{"contextMasters":[]})
            ]}])

    def test_named_nonmaster_rejects_roster_canonical_and_alias(self):
        with self.assertRaises(ValueError):
            adapt_actor(kind="named-nonmaster-author",label="Huangbo Xiyun",role="utterer")
        with self.assertRaises(ValueError):
            adapt_actor(kind="named-nonmaster-author",label="黃檗希運",role="utterer")

    def test_r92_builder_uses_adapter_as_sole_actor_path(self):
        source=Path(__file__).with_name("build_r92_config_b.py").read_text(encoding="utf-8")
        self.assertEqual(15,source.count("builder_use(adapt_actor("))
        self.assertIn("verify_builder_uses(specs)",source)

    def test_context_master_merge_retains_actor_and_dedupes_roles(self):
        rows=merge_context_masters(
          [{"MasterName":"Feiyin Tongrong","Roles":["utterer"]}],
          [{"MasterName":"Feiyin Tongrong","Roles":["utterer","record-owner"]}])
        self.assertEqual([{"MasterName":"Feiyin Tongrong","Roles":["utterer","record-owner"]}],rows)


if __name__=="__main__":
    unittest.main()
