#!/usr/bin/env python3
"""R90 specialization of the reviewed source-ranked bounded config builder."""
from pathlib import Path

template=Path(__file__).with_name("build_r84_config_b.py").read_text(encoding="utf-8")
start=template.index("specs=[")
end=template.index("\n\ntitles={",start)
review_meta={"reviewedBy":"R90 frozen source-first adjudication","reviewedUtc":"2026-07-30T13:03:00Z"}
specs=[
 {"id":"t_207efae5f6bd","term":"死句","floor":6,
  "target":"dead phrase","also":["dead saying"],"aliases":["dead sentence"],
  "opening":"A phrase treated through verbal formulation, explanation, thought, or discrimination, conventionally contrasted with a living phrase.",
  "body":"Four authored sources and two recorded-sayings sources define, contrast, or apply the dead phrase. They also warn that use depends on the person: a dead phrase can be used alive and a living phrase can be used dead.",
  "note":"The contrast with a living phrase is stable, but the label is not an intrinsic property of a string of words. These are deployments of one contrastive sense, not separate senses.",
  "review":"REVA",
  "uses":[
   ("X/X63/X63n1255.xml","Hyujeong","dead-phrase:hyujeong-authored",1,"utterer"),
   ("X/X69/X69n1357.xml","Yuanwu Keqin","dead-phrase:yuanwu-authored",1,"utterer"),
   ("X/X73/X73n1457.xml",None,"dead-phrase:mailang-definition",1,"utterer",None,"identified-unlinked-master",
    {"actorLabel":"Mailang Huai","contextMasters":[],**review_meta}),
   ("X/X87/X87n1624.xml","Dongshan Shouchu","dead-phrase:dongshan-quoted-definition",1,"utterer",None,"linked",
    {"contextMasters":[{"MasterName":"Dongshan Shouchu","Roles":["utterer","case-figure"]}],
     "contextActors":[{"Status":"identified-unlinked-master","ActorLabel":"Huihong","Roles":["later-quoter","record-owner"],"GrammarEvidence":"予 introduces Huihong's narration; 大略曰 presents Dongshan Shouchu's quoted definition."}],
     "noteContext":"Huihong is the identified later quoter and record owner."}),
   ("B/B27/B27n0152.xml","Yulin Tongxiu","dead-phrase:yulin-appraisal",2,"utterer"),
   ("C/C077/C077n1710.xml","Foyan Qingyuan","dead-phrase:foyan-instruction",2,"utterer")]},
 {"id":"t_20d13943f1a6","term":"兔子喫牛嬭","floor":4,
  "target":"a rabbit drinks cow's milk","also":["a rabbit drinking cow's milk"],"aliases":["rabbit drinks cow's milk"],
  "opening":"A deliberately incongruous answer-image: a rabbit drinking cow's milk.",
  "body":"Yangqi Fanghui, Wuzhun Shifan, Yuejiang Zhengyin, and Liao'an Qingyu independently deploy the image as an answer or verdict in recorded sayings.",
  "note":"Preserve the animal-and-milk incongruity; the records do not give it one single abstract paraphrase.",
  "review":"REVC",
  "uses":[
   ("T/T47/T47n1994A.xml","Yangqi Fanghui","rabbit-milk:yangqi-direct",2,"utterer"),
   ("X/X70/X70n1382.xml","Wuzhun Shifan","rabbit-milk:wuzhun-direct",2,"utterer"),
   ("X/X71/X71n1409.xml","Yuejiang Zhengyin","rabbit-milk:yuejiang-direct",2,"utterer"),
   ("X/X71/X71n1414.xml","Liao'an Qingyu","rabbit-milk:liaoan-direct",2,"utterer")]},
 {"id":"t_20ff8118754b","term":"赤骨律","floor":4,
  "target":"stark naked","also":["bare to the bone","completely exposed"],"aliases":["utterly bare"],
  "opening":"An intensifying image of being utterly bare, exposed, or without covering or contrivance.",
  "body":"Dawei Jinglun uses it in an authored discourse; Sanyi Mingyu, Yinyuan Longqi, and Zhangxue Tongzui use it in direct hall discourse or verse.",
  "note":"Its surrounding syntax may make the phrase adjectival or adverbial, but the frozen evidence does not establish separate lexical senses.",
  "review":"REVC",
  "uses":[
   ("J/J25/J25nB165.xml",None,"stark-naked:dawei-authored",1,"utterer",None,"identified-unlinked-master",
    {"actorLabel":"Dawei Jinglun","contextMasters":[],**review_meta}),
   ("J/J27/J27nB189.xml","Sanyi Mingyu","stark-naked:sanyi-hall",2,"utterer"),
   ("J/J27/J27nB193.xml","Yinyuan Longqi","stark-naked:yinyuan-lineage-verse",2,"verse-author",None,"linked",
    {"contextMasters":[{"MasterName":"Yinyuan Longqi","Roles":["verse-author","record-owner"]},{"MasterName":"Nanyue Huairang","Roles":["case-figure"]}]}),
   ("J/J27/J27nB194.xml","Zhangxue Tongzui","stark-naked:zhangxue-hall",2,"utterer")]}
]
if not all(spec.get("review") for spec in specs):
    raise SystemExit("every R90 spec must bind an accepted frozen adjudication")
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
source=source.replace("R84","R90").replace("r84","r90")
source=source.replace(
    'RCP=M/"non-iriya-v7-depth-regeneration-r90-research-checkpoint-b.json"',
    'RCP=M/"non-iriya-v7-depth-regeneration-r90-late-research-continuation-root.json"')
source=source.replace(
    'non-iriya-v7-depth-regeneration-r90-review-first-two-a.json',
    'non-iriya-v7-depth-regeneration-r90-adjudication-a.json')
source=source.replace(
    'non-iriya-v7-depth-regeneration-r90-review-third-b.json',
    'non-iriya-v7-depth-regeneration-r90-adjudication-c.json')
source=source.replace(
    'OLD=M/"non-iriya-v7-depth-regeneration-r90-constructor-config-b.json"',
    'OLD=M/"non-iriya-v7-depth-regeneration-r89-constructor-config-b.json"')
source=source.replace("'review': 'REVA'","'review': REV1")
source=source.replace("'review': 'REVC'","'review': REV2")
insertion=r'''
# Mailang Huai's defining use belongs only to the answer introduced by 答曰.
for _entry in entries:
    if _entry["id"] != "t_207efae5f6bd":
        continue
    _kwic="答曰：古人雖有死句、活句之分"
    _verified=zc.verify("X/X73/X73n1457.xml",_kwic)
    if not _verified.get("ok"):
        raise RuntimeError("R90 Mailang answer-only KWIC did not verify")
    _occ=_entry["evidenceDraft"]["Entry"]["Senses"][0]["Occurrences"][2]
    _occ["Kwic"]=_kwic
    _occ["FromLb"]=_verified["fromLb"]
    _occ["ToLb"]=_verified["toLb"]
    _grammar="答曰 introduces Mailang Huai's answer after the anonymous question; the headword occurs in that answer."
    _occ["ActorAttribution"]["Kind"]="Chan master"
    _occ["ActorAttribution"]["GrammarEvidence"]=_grammar
    _occ["DraftActorProof"]["FullCaseDecision"]=_grammar
    _occ["DraftActorProof"]["SpeechFrame"]=_grammar
    _span=_entry["sourceDossier"]["retainedCompleteCases"][2]["sourceSpanIdentity"]
    _span["boundedKwic"]=_kwic
    _span["boundedFromLb"]=_verified["fromLb"]
    _span["boundedToLb"]=_verified["toLb"]
    _span["boundaryEvidence"]="zc.verify binds Mailang Huai's answer without the anonymous question."
'''
source=source.replace("\nconfig=copy.deepcopy(old)",insertion+"\nconfig=copy.deepcopy(old)")
template_path=Path(__file__).with_name("build_r84_config_b.py")
exec(compile(source,str(template_path),"exec"),{"__name__":"__main__","__file__":str(template_path)})
