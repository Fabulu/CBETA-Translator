#!/usr/bin/env python3
"""R91 specialization of the reviewed source-ranked bounded config builder."""
from pathlib import Path

template=Path(__file__).with_name("build_r84_config_b.py").read_text(encoding="utf-8")
start=template.index("specs=[")
end=template.index("\n\ntitles={",start)
review_meta={"reviewedBy":"R91 frozen source-first adjudication","reviewedUtc":"2026-07-30T16:10:00Z"}
specs=[
 {"id":"t_21170b1b9a8d","term":"須彌頂上浪滔天","floor":4,
  "target":"waves flood the sky atop Mount Sumeru",
  "also":["on Mount Sumeru's summit, waves reach the sky"],
  "aliases":["waves on top of Mount Sumeru"],
  "opening":"A deliberately impossible image of waves flooding the sky from the summit of Mount Sumeru.",
  "body":"Lanshi Ling, Danya Chaoyuan, Huizhou Hao, and Yangqi Fanghui deploy the image in direct recorded sayings, pairing it with other physical impossibilities.",
  "note":"The verse, declaration, and answer settings are deployments of one image, not separate lexical senses or one universal abstract paraphrase.",
  "review":"REVA",
  "uses":[
   ("J/J28/J28nB218.xml","Lanshi Ling","sumeru-waves:lanshi-verse",2,"verse-author"),
   ("J/J39/J39nB456.xml","Danya Chaoyuan","sumeru-waves:danya-hall",2,"utterer"),
   ("J/J39/J39nB460.xml","Huizhou Hao","sumeru-waves:huizhou-hall",2,"utterer"),
   ("T/T47/T47n1994A.xml","Yangqi Fanghui","sumeru-waves:yangqi-verse",2,"verse-author")]},
 {"id":"t_211c871daa1f","term":"大庾嶺頭提不起","floor":4,
  "target":"could not lift it at Dayu Ridge","also":["unliftable at Dayu Ridge"],
  "aliases":["could not lift the robe at Dayu Ridge"],
  "opening":"A fixed allusion to the robe in the Huineng–Ming encounter that could not be lifted at Dayu Ridge.",
  "body":"Poshan Haiming and Tian'an Sheng deploy the allusion directly; unnamed monks invoke it in questions to Feiyin Tongrong and Wanru Tongwei.",
  "note":"Later applications to presented or transmitted robes remain deployments of the same allusion rather than separate senses.",
  "review":"REVA",
  "uses":[
   ("J/J26/J26nB177.xml","Poshan Haiming","dayu-unliftable:poshan-hall",2,"utterer"),
   ("J/J26/J26nB178.xml",None,"dayu-unliftable:feiyin-question",2,"questioner",
    None,"reviewed-unnamed",
    {"actorLabel":"the unnamed monk questioning Feiyin Tongrong",
     "contextMasters":[{"MasterName":"Feiyin Tongrong","Roles":["respondent","record-owner"]}],**review_meta}),
   ("J/J26/J26nB182.xml",None,"dayu-unliftable:wanru-question",2,"questioner",
    None,"reviewed-unnamed",
    {"actorLabel":"the unnamed monk questioning Wanru Tongwei",
     "contextMasters":[{"MasterName":"Wanru Tongwei","Roles":["respondent","record-owner"]}],**review_meta}),
   ("J/J26/J26nB187.xml","Tian'an Sheng","dayu-unliftable:tianan-hall",2,"utterer")]},
 {"id":"t_218e4815d84a","term":"勘破","floor":6,
  "target":"see through","also":["expose upon examination","find out completely"],
  "aliases":["see through completely"],
  "opening":"To carry an examination through until a person, claim, or situation is exposed or decisively known.",
  "body":"Four authored sources and two recorded-sayings sources use the term for seeing through or being exposed. Active and passive constructions describe the same event from different sides.",
  "note":"The term does not itself guarantee enlightenment or final truth; each complete case decides whether a claimed examination succeeded. Six independent families meet an authorized frozen-supply exception: the remaining included witnesses duplicate the Zhaozhou or Huanglong families, while the final reserve falls outside the Chinese-Chan corpus boundary.",
  "review":"REVC",
  "uses":[
   ("D/D50/D50n8945.xml","Xuefeng Huikong","see-through:xuefeng-verse",1,"verse-author",None,"linked",
    {"actorNote":"Source record (D/D50/D50n8945.xml). Outer Collection of Monk Xuefeng Kong. Xuefeng Huikong authors the verse; Zhaozhou Congshen is the case figure and the old woman on the Mount Wutai road is the person discussed.",
     "contextMasters":[{"MasterName":"Xuefeng Huikong","Roles":["verse-author"]},{"MasterName":"Zhaozhou Congshen","Roles":["case-figure"]}],
     "contextActors":[{"Status":"reviewed-unnamed","ActorLabel":"the old woman on the Mount Wutai road","Roles":["person-discussed"],"GrammarEvidence":"The verse presents her as the person Zhaozhou examines."}]}),
   ("J/J39/J39nB458.xml",None,"see-through:runguang-exhortation",1,"utterer",None,"identified-unlinked-master",
    {"actorLabel":"Runguang Ze","contextMasters":[],**review_meta}),
   ("X/X69/X69n1357.xml","Yuanwu Keqin","see-through:yuanwu-authored",1,"utterer",None,"linked",
    {"contextMasters":[{"MasterName":"Yuanwu Keqin","Roles":["utterer"]},{"MasterName":"Zhaozhou Congshen","Roles":["person-discussed"]}]}),
   ("X/X87/X87n1624.xml","Huanglong Huinan","see-through:huanglong-verse",1,"verse-author",None,"linked",
    {"actorNote":"Source record (X/X87/X87n1624.xml). Records from the Groves of Chan. Huanglong Huinan authors the verse about Zhaozhou Congshen and the old woman on the Mount Wutai road; Shishuang Chuyuan corrects it, and Huihong is the later quoter and record owner.",
     "contextMasters":[{"MasterName":"Huanglong Huinan","Roles":["verse-author"]},{"MasterName":"Shishuang Chuyuan","Roles":["teacher"]},{"MasterName":"Zhaozhou Congshen","Roles":["case-figure"]}],
     "contextActors":[{"Status":"reviewed-unnamed","ActorLabel":"the old woman on the Mount Wutai road","Roles":["person-discussed"],"GrammarEvidence":"Huanglong's verse names the old woman as the examined person."},{"Status":"identified-unlinked-master","ActorLabel":"Huihong","Roles":["later-quoter","record-owner"],"GrammarEvidence":"Huihong narrates and preserves Huanglong's verse."}]}),
   ("B/B27/B27n0152.xml",None,"see-through:tianzhen-quoted",2,"utterer",None,"identified-unlinked-master",
    {"actorLabel":"Tianzhen","contextMasters":[{"MasterName":"Yulin Tongxiu","Roles":["later-quoter","record-owner"]}],**review_meta}),
   ("C/C077/C077n1710.xml","Huanglong Huinan","see-through:huanglong-substitute",2,"utterer",None,"linked",
    {"actorNote":"Source record (C/C077/C077n1710.xml). Recorded Sayings of Ancient Worthies. Huanglong Huinan supplies the substitute line after recounting Wang the judicial commissioner questioning Lian Sansheng.",
     "contextMasters":[{"MasterName":"Huanglong Huinan","Roles":["utterer"]}],
     "contextActors":[{"Status":"identified-unlinked-master","ActorLabel":"Wang the judicial commissioner","Roles":["person-discussed"],"GrammarEvidence":"Huanglong recounts Wang's question."},{"Status":"identified-unlinked-master","ActorLabel":"Lian Sansheng","Roles":["person-discussed"],"GrammarEvidence":"Huanglong recounts Lian's failure to answer before supplying the substitute line."}]})]}
]
if not all(spec.get("review") for spec in specs):
    raise SystemExit("every R91 spec must bind an accepted frozen adjudication")
import json
registry_path=Path(__file__).resolve().parents[5]/"Assets/Data/zen-source-authority.json"
registry={row["RelPath"]:int(row["Tier"]) for row in json.loads(registry_path.read_text(encoding="utf-8"))["entries"]}
for spec in specs:
    normalized=[]
    for use in spec["uses"]:
        parts=list(use)
        if parts[0] not in registry:
            raise SystemExit(f"{spec['id']}: retained source absent from authority registry: {parts[0]}")
        parts[3]=registry[parts[0]]
        normalized.append(tuple(parts))
    spec["uses"]=normalized
replacement="specs="+repr(specs)
source=template[:start]+replacement+template[end:]
source=source.replace("R84","R91").replace("r84","r91")
source=source.replace(
    'RCP=M/"non-iriya-v7-depth-regeneration-r91-research-checkpoint-b.json"',
    'RCP=M/"non-iriya-v7-depth-regeneration-r91-late-research-continuation-root.json"')
source=source.replace(
    'non-iriya-v7-depth-regeneration-r91-review-first-two-a.json',
    'non-iriya-v7-depth-regeneration-r91-adjudication-a.json')
source=source.replace(
    'non-iriya-v7-depth-regeneration-r91-review-third-b.json',
    'non-iriya-v7-depth-regeneration-r91-adjudication-c.json')
source=source.replace(
    'OLD=M/"non-iriya-v7-depth-regeneration-r91-constructor-config-b.json"',
    'OLD=M/"non-iriya-v7-depth-regeneration-r90-constructor-config-b.json"')
source=source.replace("'review': 'REVA'","'review': REV1")
source=source.replace("'review': 'REVC'","'review': REV2")
insertion=r'''
for _entry in entries:
    if _entry["id"] != "t_218e4815d84a":
        continue
    _exception={
      "Decision":"FROZEN_CANDIDATE_EXHAUSTION",
      "AuthorizedBy":"root",
      "FrozenCandidateExhausted":True,
      "RetainedIndependentFamilies":6,
      "GuideFloor":7,
      "ExcludedReserve":[
        "X/X78/X78n1554.xml duplicates the retained Zhaozhou family",
        "B/B25/B25n0145.xml duplicates the retained Huanglong family",
        "D/D51/D51n8948.xml is Japanese and excluded from the Chinese-Chan corpus boundary"
      ]}
    _entry["evidenceDraft"]["Entry"]["Senses"][0]["DraftEvidence"]["DepthHarvestReceipt"]["AuthorizedFloorException"]=_exception
    _entry["sourceDossier"]["researchNotes"]["depthReceipt"]["AuthorizedFloorException"]=_exception
'''
source=source.replace("\nconfig=copy.deepcopy(old)",insertion+"\nconfig=copy.deepcopy(old)")
template_path=Path(__file__).with_name("build_r84_config_b.py")
exec(compile(source,str(template_path),"exec"),{"__name__":"__main__","__file__":str(template_path)})
