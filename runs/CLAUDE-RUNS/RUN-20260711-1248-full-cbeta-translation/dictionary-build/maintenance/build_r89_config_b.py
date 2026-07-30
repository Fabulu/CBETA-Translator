#!/usr/bin/env python3
"""R89 specialization of the reviewed source-ranked bounded config builder."""
from pathlib import Path

template=Path(__file__).with_name("build_r84_config_b.py").read_text(encoding="utf-8")
start=template.index("specs=[")
end=template.index("\n\ntitles={",start)
review_meta={"reviewedBy":"R89 root source-first adjudication","reviewedUtc":"2026-07-30T12:23:00Z"}
specs=[
 {"id":"t_1e41b014d80e","term":"向上一路","floor":7,
  "target":"the road beyond","also":["the one road upward"],"aliases":["the further road"],
 "opening":"A further road or coordinate raised beyond entry, awakening, inherited teaching, or ordinary explanation.",
  "body":"Two authored works and five recorded-sayings sources actively question, develop, or interpret the road beyond, including the recurrent statement that even a thousand sages do not transmit it.",
  "note":"Formula, direct question, and beyond-awakening uses are deployments of one referent. Passive inherited quotations do not become new origins.",
  "review":"REV1",
  "uses":[
   ("X/X63/X63n1257.xml","Wuyi Yuanlai","road-beyond:wuyi-direct",1,"utterer"),
   ("X/X63/X63n1259.xml","Huishan Jiexian","road-beyond:huishan-authored",1,"utterer"),
   ("X/X64/X64n1276.xml",None,"road-beyond:tianze-authored",1,"utterer",None,"identified-unlinked-master",
    {"actorLabel":"Tianze, the identified author of the headword-bearing clause","contextMasters":[],**review_meta}),
   ("X/X69/X69n1357.xml","Yuanwu Keqin","road-beyond:yuanwu-instruction",1,"utterer"),
   ("B/B27/B27n0152.xml","Yulin Tongxiu","road-beyond:yulin-beyond-awakening",2,"utterer"),
   ("C/C077/C077n1710.xml",None,"road-beyond:muzhou-question",2,"questioner",None,"reviewed-unnamed",
    {"actorLabel":"the unnamed monk questioning Muzhou Daoming",
     "contextMasters":[{"MasterName":"Muzhou Daoming","Roles":["respondent","section-subject","record-owner"]}],**review_meta}),
   ("J/J10/J10nA158.xml","Miyun Yuanwu","road-beyond:miyun-formula",2,"utterer")]},
 {"id":"t_1f3653f30389","term":"抱子弄孫","floor":4,
  "target":"hold one's children and amuse one's grandchildren",
  "also":["family life with children and grandchildren"],"aliases":["tend children and grandchildren"],
  "opening":"Ordinary family life: holding one's children and amusing or tending one's grandchildren.",
  "body":"Four recorded masters place this household activity among receiving guests, serving a household, and moving through daily life, making ordinary lay activity the immediate road or Buddha-work rather than an obstruction outside it.",
  "note":"The phrase keeps its ordinary family sense; its Zen job comes from where the masters place that activity.",
  "review":"REV1",
  "uses":[
   ("J/J26/J26nB188.xml","Ruibai Mingxue","family-life:ruibai-letter",2,"utterer"),
   ("J/J27/J27nB190.xml","Shiyu Mingfang","family-life:shiyu-hall",2,"utterer"),
   ("J/J27/J27nB196.xml","Yuanhu Miaoyong","family-life:yuanhu-letter",2,"utterer"),
   ("J/J28/J28nB211.xml","Jizong Che","family-life:jizong-letter",2,"utterer")]},
 {"id":"t_1fe4eac13d6e","term":"入門便喝","floor":6,
  "target":"shout as soon as someone enters","also":["shout on entry"],"aliases":["an entry shout"],
  "opening":"The inherited formula for Linji shouting as soon as a visitor enters.",
  "body":"One authored work and five recorded-sayings sources actively treat Linji's entry shout beside Deshan's entry blow; two additional sayings records extend the phrase to asking how one should answer an entry shout and to entry shouting as a general operation.",
  "note":"Six witnesses treat one inherited Linji action formula, not six historical origins. Two actor-clear operational extensions demonstrate that the lexical unit is the full entry-shout clause rather than the adjacent name Linji.",
  "review":"REV1",
  "uses":[
   ("X/X69/X69n1357.xml","Yuanwu Keqin","entry-shout:yuanwu-comment",1,"utterer",None,"linked",
    {"contextMasters":[{"MasterName":"Yuanwu Keqin","Roles":["utterer","commentator","record-owner"]},{"MasterName":"Linji Yixuan","Roles":["case-figure"]},{"MasterName":"Deshan Xuanjian","Roles":["case-figure"]}]}),
   ("X/X78/X78n1554.xml",None,"entry-shout:dongshan-question",1,"questioner",None,"reviewed-unnamed",
    {"actorLabel":"the unnamed monk questioning Dongshan Xiaocong",
     "contextMasters":[{"MasterName":"Dongshan Xiaocong","Roles":["respondent","section-subject"]},{"MasterName":"Linji Yixuan","Roles":["case-figure"]},{"MasterName":"Deshan Xuanjian","Roles":["case-figure"]}],**review_meta}),
   ("B/B25/B25n0145.xml","Zhongfeng Mingben","entry-shout:zhongfeng-discourse",2,"utterer",None,"linked",
    {"contextMasters":[{"MasterName":"Zhongfeng Mingben","Roles":["utterer","record-owner"]},{"MasterName":"Linji Yixuan","Roles":["case-figure"]},{"MasterName":"Deshan Xuanjian","Roles":["case-figure"]}]}),
   ("C/C077/C077n1710.xml","Fenyang Shanzhao","entry-shout:fenyang-critique",2,"utterer",None,"linked",
    {"contextMasters":[{"MasterName":"Fenyang Shanzhao","Roles":["utterer","commentator","record-owner"]},{"MasterName":"Linji Yixuan","Roles":["case-figure"]},{"MasterName":"Deshan Xuanjian","Roles":["case-figure"]}]}),
   ("J/J10/J10nA158.xml","Miyun Yuanwu","entry-shout:miyun-preface",2,"utterer",None,"linked",
    {"contextMasters":[{"MasterName":"Miyun Yuanwu","Roles":["utterer","commentator","record-owner"]},{"MasterName":"Linji Yixuan","Roles":["case-figure"]},{"MasterName":"Deshan Xuanjian","Roles":["case-figure"]}]}),
   ("J/J25/J25nB163.xml","Guting Shanjian","entry-shout:guting-discourse",2,"utterer",None,"linked",
    {"contextMasters":[{"MasterName":"Guting Shanjian","Roles":["utterer","commentator","record-owner"]},{"MasterName":"Linji Yixuan","Roles":["case-figure"]},{"MasterName":"Deshan Xuanjian","Roles":["case-figure"]}]}),
   ("B/B27/B27n0152.xml","Yulin Tongxiu","entry-shout:yulin-operational-extension",2,"utterer"),
   ("J/J25/J25nB171.xml",None,"entry-shout:linxuan-response-question",2,"questioner",None,"identified-non-master",
    {"actorLabel":"Lin Xuan, the named non-master questioner",
     "contextMasters":[{"MasterName":"Tianyin Yuanxiu","Roles":["respondent","record-owner"]}],**review_meta})]}
]
if not all(spec.get("review") for spec in specs):
    raise SystemExit("every R89 spec must bind an accepted review matrix before config construction")
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
    if any(use[3] != registry[use[0]] for use in spec["uses"]):
        raise SystemExit(f"{spec['id']}: registry-tier normalization failed")
replacement="specs="+repr(specs)
source=template[:start]+replacement+template[end:]
source=source.replace("R84","R89").replace("r84","r89")
source=source.replace(
    'RCP=M/"non-iriya-v7-depth-regeneration-r89-research-checkpoint-b.json"',
    'RCP=M/"non-iriya-v7-depth-regeneration-r89-late-research-continuation-b.json"')
source=source.replace(
    'non-iriya-v7-depth-regeneration-r89-review-first-two-a.json',
    'non-iriya-v7-depth-regeneration-r89-corrected-source-matrix-b.json')
source=source.replace(
    'non-iriya-v7-depth-regeneration-r89-review-third-b.json',
    'non-iriya-v7-depth-regeneration-r89-corrected-source-matrix-b.json')
source=source.replace(
    'OLD=M/"non-iriya-v7-depth-regeneration-r89-constructor-config-b.json"',
    'OLD=M/"non-iriya-v7-depth-regeneration-r88-constructor-config-b.json"')
source=source.replace("'review': 'REV1'","'review': REV1")
insertion=r'''
# Keep the anonymous monk's headword-bearing question separate from Muzhou's
# immediately following 師云 answer, so the public KWIC cannot reverse actors.
for _entry in entries:
    if _entry["id"] != "t_1e41b014d80e":
        continue
    _kwic="問如何是向上一路"
    _verified=zc.verify("C/C077/C077n1710.xml",_kwic)
    if not _verified.get("ok"):
        raise RuntimeError("R89 Muzhou question-only KWIC did not verify")
    _occ=_entry["evidenceDraft"]["Entry"]["Senses"][0]["Occurrences"][5]
    _occ["Kwic"]=_kwic
    _occ["FromLb"]=_verified["fromLb"]
    _occ["ToLb"]=_verified["toLb"]
    _grammar="僧問 introduces the headword-bearing question; the following 師云 introduces Muzhou's response."
    _occ["ActorAttribution"]["GrammarEvidence"]=_grammar
    _occ["DraftActorProof"]["FullCaseDecision"]=_grammar
    _occ["DraftActorProof"]["SpeechFrame"]=_grammar
    _span=_entry["sourceDossier"]["retainedCompleteCases"][5]["sourceSpanIdentity"]
    _span["boundedKwic"]=_kwic
    _span["boundedFromLb"]=_verified["fromLb"]
    _span["boundedToLb"]=_verified["toLb"]
    _span["boundaryEvidence"]="zc.verify binds the anonymous monk's complete headword-bearing question without Muzhou's following answer."
# Lin Xuan is named at the opening of the same uninterrupted question sequence.
# Keep him unlinked and structured as a non-master questioner; Tianyin remains
# only the respondent/record owner.
for _entry in entries:
    if _entry["id"] != "t_1fe4eac13d6e":
        continue
    _occ=_entry["evidenceDraft"]["Entry"]["Senses"][0]["Occurrences"][7]
    _grammar="林玹出問 names Lin Xuan as the questioner; 進云 introduces his headword-bearing question, and the following 師云 introduces Tianyin Yuanxiu's response."
    _occ["ActorAttribution"]["Kind"]="identified non-master"
    _occ["ActorAttribution"]["GrammarEvidence"]=_grammar
    _context_actor={"Status":"identified-non-master","ActorLabel":"Lin Xuan","Roles":["questioner"],"GrammarEvidence":_grammar}
    _occ["ContextActors"]=[dict(_context_actor)]
    _occ["DraftActorProof"]["FullCaseDecision"]=_grammar
    _occ["DraftActorProof"]["SpeechFrame"]=_grammar
    _case=_entry["sourceDossier"]["retainedCompleteCases"][7]
    _case["actorDecision"]["actorAttribution"]["Kind"]="identified non-master"
    _case["actorDecision"]["actorAttribution"]["GrammarEvidence"]=_grammar
    _case["actorDecision"]["contextActors"]=[dict(_context_actor)]
'''
source=source.replace("\nconfig=copy.deepcopy(old)",insertion+"\nconfig=copy.deepcopy(old)")
template_path=Path(__file__).with_name("build_r84_config_b.py")
exec(compile(source,str(template_path),"exec"),{"__name__":"__main__","__file__":str(template_path)})
