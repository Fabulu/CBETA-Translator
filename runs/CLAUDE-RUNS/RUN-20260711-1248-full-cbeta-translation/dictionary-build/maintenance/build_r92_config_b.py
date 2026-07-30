#!/usr/bin/env python3
"""R92 reviewed source-ranked bounded config builder."""
from pathlib import Path
import json
from maintenance.adjudicated_actor_adapter import adapt_actor, builder_use, verify_builder_uses

template=Path(__file__).with_name("build_r84_config_b.py").read_text(encoding="utf-8")
start=template.index("specs=[")
end=template.index("\n\ntitles={",start)
meta={"reviewedBy":"R92 frozen source-first adjudication","reviewedUtc":"2026-07-30T14:12:00Z"}
specs=[
 {"id":"t_219099a33daa","term":"疾入於涅槃","floor":4,
  "target":"quickly enter final extinction","also":["hasten into final extinction"],"aliases":["quickly enter nirvana"],
  "opening":"A fixed clause from the Buddha's reported deliberation: to quickly enter final extinction rather than teach.",
  "body":"Fushi Tongxian, Shengke Deyu, Dahui Zonggao, and Yuanwu Keqin independently quote, explain, contrast, or alter the clause.",
  "note":"The clause reports a counterfactual deliberation before the Buddha decides to teach; it is not a general injunction to die.",
  "review":"REVA",
  "uses":[
   builder_use(adapt_actor(kind="quoted-nonroster-master",label="Shakyamuni",role="quoted-speaker",context_masters=[{"MasterName":"Fushi Tongxian","Roles":["later-quoter","commentator","record-owner"]}]),"J/J26/J26nB185.xml","nirvana:Fushi-quotation",2,meta),
   builder_use(adapt_actor(kind="quoted-nonroster-master",label="Shakyamuni",role="quoted-speaker",context_masters=[{"MasterName":"Shengke Deyu","Roles":["later-quoter","commentator","record-owner"]}]),"J/J35/J35nB342.xml","nirvana:Shengke-contrast",2,meta),
   builder_use(adapt_actor(kind="quoted-nonroster-master",label="Shakyamuni",role="quoted-speaker",context_masters=[{"MasterName":"Dahui Zonggao","Roles":["later-quoter","commentator","record-owner"]}]),"M/M59/M59n1540.xml","nirvana:Dahui-explanation",2,meta),
   builder_use(adapt_actor(kind="quoted-nonroster-master",label="Shakyamuni",role="quoted-speaker",context_masters=[{"MasterName":"Yuanwu Keqin","Roles":["later-quoter","commentator"]}]),"T/T48/T48n2003.xml","nirvana:Yuanwu-commentary",2,meta)]},
 {"id":"t_21a3463bc0db","term":"隨處","floor":7,
  "target":"wherever","also":["everywhere","in any place"],"aliases":["wherever one is","wherever it occurs"],
  "opening":"At whatever place or situation is in view, or throughout all such places.",
  "body":"Seven independent authored works use the expression for displaying a house style, meeting reactions, seeing Buddha, following conditions, teaching, revealing mind-light, or becoming attached.",
  "note":"These different predicates preserve one locative-distributive sense rather than establishing separate lexical things.",
  "review":"REVC",
  "uses":[
   builder_use(adapt_actor(kind="roster-master",label="Xuefeng Huikong",role="verse-author"),"D/D50/D50n8945.xml","wherever:xuefeng-house-style",1,meta),
   builder_use(adapt_actor(kind="unlinked-master",label="Micang Daokai",role="utterer"),"J/J23/J23nB118.xml","wherever:micang-reaction",1,meta),
   builder_use(adapt_actor(kind="unlinked-master",label="Fushan Benzhi",role="verse-author"),"J/J25/J25nB166.xml","wherever:fushan-see-buddha",1,meta),
   builder_use(adapt_actor(kind="roster-master",label="Yongjia Xuanjue",role="utterer"),"T/T48/T48n2013.xml","wherever:yongjia-conditions",1,meta),
   builder_use(adapt_actor(kind="roster-master",label="Guifeng Zongmi",role="utterer"),"T/T48/T48n2015.xml","wherever:zongmi-teachers",1,meta),
   builder_use(adapt_actor(kind="roster-master",label="Yongming Yanshou",role="verse-author"),"T/T48/T48n2018.xml","wherever:yanshou-mind-light",1,meta),
   builder_use(adapt_actor(kind="unlinked-master",label="Zhiche",role="utterer"),"T/T48/T48n2021.xml","wherever:zhiche-attachment",1,meta)]},
 {"id":"t_21b44f051c7a","term":"財法二施","floor":4,
  "target":"the two givings: material support and teaching","also":["material and teaching gifts"],"aliases":["material and dharma gifts"],
  "opening":"The paired givings of material support supplied by donors and teaching supplied in return or alongside it.",
  "body":"Ruibai Mingxue, an unnamed monk questioning Tianyin Yuanxiu, Huangbo Xiyun, and Feiyin Tongrong deploy the paired expression in four independent recorded-sayings families.",
  "note":"Equality, completion, or joint fulfillment is supplied by surrounding predicates and is not part of the headword itself.",
  "review":"REVA",
  "uses":[
   builder_use(adapt_actor(kind="roster-master",label="Ruibai Mingxue",role="utterer",context_masters=[{"MasterName":"Ruibai Mingxue","Roles":["utterer","record-owner"]}]),"J/J26/J26nB188.xml","two-givings:ruibai-hall",2,meta),
   builder_use(adapt_actor(kind="unnamed-questioner",label="the unnamed monk questioning Tianyin Yuanxiu",role="questioner",context_masters=[{"MasterName":"Tianyin Yuanxiu","Roles":["respondent","record-owner"]}]),"J/J25/J25nB171.xml","two-givings:tianyin-question",2,meta),
   builder_use(adapt_actor(kind="roster-master",label="Huangbo Xiyun",role="utterer",context_masters=[{"MasterName":"Juelang Daosheng","Roles":["later-quoter","commentator","record-owner"]}]),"J/J25/J25nB174.xml","two-givings:ganzi-huangbo",2,meta),
   builder_use(adapt_actor(kind="roster-master",label="Feiyin Tongrong",role="utterer",context_masters=[{"MasterName":"Feiyin Tongrong","Roles":["utterer","record-owner"]}]),"J/J26/J26nB178.xml","two-givings:feiyin-hall",2,meta)]}
]
verify_builder_uses(specs)
registry_path=Path(__file__).resolve().parents[5]/"Assets/Data/zen-source-authority.json"
registry={r["RelPath"]:int(r["Tier"]) for r in json.loads(registry_path.read_text(encoding="utf-8"))["entries"]}
for spec in specs:
    spec["uses"]=[tuple(list(u[:3])+[registry[u[0]]]+list(u[4:])) for u in spec["uses"]]
source=template[:start]+"specs="+repr(specs)+template[end:]
source=source.replace("R84","R92").replace("r84","r92")
source=source.replace("non-iriya-v7-depth-regeneration-r92-review-first-two-a.json","non-iriya-v7-depth-regeneration-r92-adjudication-a.json")
source=source.replace("non-iriya-v7-depth-regeneration-r92-review-third-b.json","non-iriya-v7-depth-regeneration-r92-adjudication-c.json")
source=source.replace('OLD=M/"non-iriya-v7-depth-regeneration-r92-constructor-config-b.json"','OLD=M/"non-iriya-v7-depth-regeneration-r91-constructor-config-b.json"')
source=source.replace("'review': 'REVA'","'review': REV1").replace("'review': 'REVC'","'review': REV2")
template_path=Path(__file__).with_name("build_r84_config_b.py")
exec(compile(source,str(template_path),"exec"),{"__name__":"__main__","__file__":str(template_path)})
