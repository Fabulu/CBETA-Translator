#!/usr/bin/env python3
"""R93 reviewed source-ranked bounded config builder."""
from pathlib import Path
import json
from maintenance.adjudicated_actor_adapter import adapt_actor, builder_use, verify_builder_uses

template=Path(__file__).with_name("build_r84_config_b.py").read_text(encoding="utf-8")
start=template.index("specs=["); end=template.index("\n\ntitles={",start)
meta={"reviewedBy":"R93 frozen source-first adjudication","reviewedUtc":"2026-07-30T14:48:00Z"}
def A(label,role="utterer",contexts=None):
    return adapt_actor(kind="roster-master",label=label,role=role,context_masters=contexts or [])
def U(actor,rel,family,tier,deployment=None):
    review=dict(meta)
    if deployment: review["deploymentRole"]=deployment
    return builder_use(actor,rel,family,tier,review)
specs=[
 {"id":"t_2202e37854d4","term":"王老師","floor":7,"target":"Old Teacher Wang",
  "also":["Teacher Wang"],"aliases":["Nanquan","Old Teacher Wang"],
  "opening":"A familiar personal designation for Nanquan Puyuan, whose lay surname was Wang.",
  "body":"Nanquan uses the designation of himself in the third person, while Xuefeng Huikong, Yuanwu Keqin, Xisou Shaotan, Zhongfeng Mingben, Yulin Tongxiu, and Miyun Yuanwu quote or redeploy it as Nanquan's familiar name.",
  "note":"It does not identify an otherwise unknown teacher surnamed Wang; the surrounding buffalo, spirit, self-sale, and examination stories belong to their individual passages.",
  "review":Path(__file__).with_name("non-iriya-v7-depth-regeneration-r93-adjudication-wang-a.json"),
  "uses":[
   U(A("Xuefeng Huikong","verse-author",[{"MasterName":"Nanquan Puyuan","Roles":["case-figure","person-discussed"]}]),"D/D50/D50n8945.xml","wang:xuefeng-self-sale",1),
   U(A("Yuanwu Keqin","utterer",[{"MasterName":"Nanquan Puyuan","Roles":["person-discussed"]}]),"X/X69/X69n1357.xml","wang:yuanwu-jiazhong",1),
   U(A("Nanquan Puyuan","utterer",[{"MasterName":"Xisou Shaotan","Roles":["compiler","later-quoter","verse-author"]}]),"X/X78/X78n1554.xml","wang:xisou-portrait",1,"active-quotation"),
   U(A("Nanquan Puyuan","utterer",[{"MasterName":"Zhongfeng Mingben","Roles":["later-quoter","commentator","record-owner"]}]),"B/B25/B25n0145.xml","wang:zhongfeng-colophon",2,"active-quotation"),
   U(A("Nanquan Puyuan","utterer",[{"MasterName":"Yulin Tongxiu","Roles":["later-raiser","commentator","record-owner"]}]),"B/B27/B27n0152.xml","wang:yulin-spirit-case",2,"active-quotation"),
   U(A("Nanquan Puyuan","utterer",[{"MasterName":"Huangbo Xiyun","Roles":["respondent","student","section-subject"]}]),"C/C077/C077n1710.xml","wang:nanquan-huangbo",2),
   U(A("Nanquan Puyuan","utterer",[{"MasterName":"Miyun Yuanwu","Roles":["later-quoter","commentator","record-owner"]}]),"J/J10/J10nA158.xml","wang:miyun-buffalo",2,"active-quotation")]},
 {"id":"t_2229af16905a","term":"威音王","floor":7,"target":"King Majestic Sound",
  "also":["Majestic Sound King","the King Majestic Sound Buddha"],
  "aliases":["before King Majestic Sound","on the far side of King Majestic Sound"],
  "opening":"The proper name King Majestic Sound, often used in a temporal construction reaching before even that named Buddha.",
  "body":"Yuanwu, Gulin, Weilin, Xisou, Zhongfeng, Huangbo, and Miyun use the name in seven independent authored or recorded-sayings deployments.",
  "note":"Direct reference or seeing and the temporal constructions retain one named referent; 'before' and 'on the far side' do not create another lexical thing.",
  "review":Path(__file__).with_name("non-iriya-v7-depth-regeneration-r93-adjudication-weiyin-c.json"),
  "uses":[
   U(A("Yuanwu Keqin"),"X/X69/X69n1357.xml","weiyin:yuanwu-empty-age",1),
   U(A("Gulin Qingmao","verse-author"),"X/X71/X71n1413.xml","weiyin:gulin-verse",1),
   U(A("Weilin Daopei"),"X/X72/X72n1442.xml","weiyin:weilin-before",1),
   U(A("Xisou Shaotan","verse-author"),"X/X78/X78n1554.xml","weiyin:xisou-praise",1),
   U(A("Zhongfeng Mingben"),"B/B25/B25n0145.xml","weiyin:zhongfeng-glance",2),
   U(A("Huangbo Xiyun",contexts=[{"MasterName":"Nanquan Puyuan","Roles":["questioner","teacher"]}]),"C/C077/C077n1710.xml","weiyin:huangbo-nanquan",2),
   U(A("Miyun Yuanwu"),"J/J10/J10nA158.xml","weiyin:miyun-before",2)]},
 {"id":"t_222d636a08a9","term":"竪窮三際","floor":4,"target":"vertically exhaust the three times",
  "also":["vertically reach through the three times"],"aliases":["through past present and future"],
  "opening":"The temporal half of a paired scope formula: vertically reaching without limit through past, present, and future.",
  "body":"Yongming Yanshou, Zhongfeng Mingben, Dahui Zonggao, and Wansong Xingxiu use or explicitly gloss the expression in four independent families.",
  "note":"The vertical axis marks temporal scope, not posture; preserve its horizontal ten-direction partner where the source supplies it.",
  "review":Path(__file__).with_name("non-iriya-v7-depth-regeneration-r93-adjudication-three-times-b.json"),
  "uses":[
   U(A("Yongming Yanshou",contexts=[{"MasterName":"Yongming Yanshou","Roles":["utterer","record-owner"]}]),"T/T48/T48n2016.xml","three-times:yongming-sudhana",1),
   U(A("Zhongfeng Mingben",contexts=[{"MasterName":"Zhongfeng Mingben","Roles":["utterer","record-owner"]}]),"B/B25/B25n0145.xml","three-times:zhongfeng-mind-seal",2),
   U(A("Dahui Zonggao",contexts=[{"MasterName":"Dahui Zonggao","Roles":["utterer","record-owner"]}]),"M/M59/M59n1540.xml","three-times:dahui-great-matter",2),
   U(A("Wansong Xingxiu","commentator",[{"MasterName":"Wansong Xingxiu","Roles":["commentator","record-owner"]}]),"T/T48/T48n2004.xml","three-times:wansong-gloss",2)]}
]
verify_builder_uses(specs)
registry_path=Path(__file__).resolve().parents[5]/"Assets/Data/zen-source-authority.json"
registry={r["RelPath"]:int(r["Tier"]) for r in json.loads(registry_path.read_text(encoding="utf-8"))["entries"]}
for spec in specs:
    spec["uses"]=[tuple(list(u[:3])+[registry[u[0]]]+list(u[4:])) for u in spec["uses"]]
source=template[:start]+"specs="+repr(specs)+template[end:]
source=source.replace("R84","R93").replace("r84","r93")
source=source.replace("non-iriya-v7-depth-regeneration-r93-review-first-two-a.json","non-iriya-v7-depth-regeneration-r93-adjudication-wang-a.json")
source=source.replace("non-iriya-v7-depth-regeneration-r93-review-third-b.json","non-iriya-v7-depth-regeneration-r93-adjudication-three-times-b.json")
source=source.replace('OLD=M/"non-iriya-v7-depth-regeneration-r93-constructor-config-b.json"','OLD=M/"non-iriya-v7-depth-regeneration-r92-retry2-constructor-config-b.json"')
source=source.replace("'review': PosixPath('"+str(Path(__file__).with_name("non-iriya-v7-depth-regeneration-r93-adjudication-wang-a.json"))+"')","'review': REV1")
source=source.replace("'review': PosixPath('"+str(Path(__file__).with_name("non-iriya-v7-depth-regeneration-r93-adjudication-weiyin-c.json"))+"')","'review': M/'non-iriya-v7-depth-regeneration-r93-adjudication-weiyin-c.json'")
source=source.replace("'review': PosixPath('"+str(Path(__file__).with_name("non-iriya-v7-depth-regeneration-r93-adjudication-three-times-b.json"))+"')","'review': REV2")
template_path=Path(__file__).with_name("build_r84_config_b.py")
exec(compile(source,str(template_path),"exec"),{"__name__":"__main__","__file__":str(template_path)})
