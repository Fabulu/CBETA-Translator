#!/usr/bin/env python3
"""R88 specialization of the reviewed source-ranked bounded config builder."""
from pathlib import Path

template=Path(__file__).with_name("build_r84_config_b.py").read_text(encoding="utf-8")
start=template.index("specs=[")
end=template.index("\n\ntitles={",start)
specs=[
 {"id":"t_1e38e6b91833","term":"宜作舟航","floor":4,
  "target":"you should serve as a ferryboat",
  "also":["you should become a ferryboat"],"aliases":["serve as a boat that carries others across"],
  "opening":"Mazu tells Yaoshan to become a conveyance that carries others across rather than treating realization as a private stopping place.",
  "body":"Tianyin Yuanxiu, Poshan Haiming, Miyin Zhenchuan, and Guantao Daqi actively raise and interpret the same inherited Mazu–Yaoshan instruction.",
  "note":"The four records are active later treatments of one inherited case, not four independent origins. Mazu owns the quoted imperative; the record masters own their outer comments.",
  "review":"REV1",
  "uses":[
   ("J/J25/J25nB171.xml","Mazu Daoyi","ferryboat:tianyin-active-raising",2,"utterer",None,"linked",
    {"contextMasters":[{"MasterName":"Mazu Daoyi","Roles":["utterer","case-figure"]},{"MasterName":"Yaoshan Weiyan","Roles":["addressee","case-figure"]},{"MasterName":"Tianyin Yuanxiu","Roles":["later-raiser","commentator","record-owner"]}]}),
   ("J/J26/J26nB177.xml","Mazu Daoyi","ferryboat:poshan-active-disagreement",2,"utterer",None,"linked",
    {"contextMasters":[{"MasterName":"Mazu Daoyi","Roles":["utterer","case-figure"]},{"MasterName":"Yaoshan Weiyan","Roles":["addressee","case-figure"]},{"MasterName":"Poshan Haiming","Roles":["later-raiser","commentator","record-owner"]}]}),
   ("J/J35/J35nB343.xml","Mazu Daoyi","ferryboat:miyin-active-comment",2,"utterer",None,"linked",
    {"contextMasters":[{"MasterName":"Mazu Daoyi","Roles":["utterer","case-figure"]},{"MasterName":"Yaoshan Weiyan","Roles":["addressee","case-figure"]},{"MasterName":"Miyin Zhenchuan","Roles":["later-raiser","commentator","record-owner"]}]}),
   ("J/J36/J36nB362.xml","Mazu Daoyi","ferryboat:guantao-active-criticism",2,"utterer",None,"linked",
    {"contextMasters":[{"MasterName":"Mazu Daoyi","Roles":["utterer","case-figure"]},{"MasterName":"Yaoshan Weiyan","Roles":["addressee","case-figure"]},{"MasterName":"Guantao Daqi","Roles":["later-raiser","commentator","record-owner"]}]})]},
 {"id":"t_1e3d3a5173a6","term":"回互","floor":7,
  "target":"interrelation or interweaving",
  "also":["mutual interrelation"],"aliases":["interweave"],
  "opening":"A context-sensitive term for elements turning through one another, for the absence of any turn or room for maneuver, and for responsive accommodation.",
  "body":"Four authored works and three sayings records distinguish the interrelation, no-turning, and tactful-accommodation uses.",
  "note":"The three contextual jobs must not be flattened into a single universal gloss.",
  "review":"REV1",
  "uses":[
   ("X/X87/X87n1624.xml","Juefan Huihong","huihu:interrelation-huihong",1,"utterer",None,"identified-unlinked-master",
    {"contextMasters":[],"reviewedBy":"R88 root source-first review and corrected matrix","reviewedUtc":"2026-07-30T11:53:00Z"}),
   ("C/C077/C077n1710.xml","Yunmen Wenyan","huihu:contrast-yunmen",2,"utterer"),
   ("X/X71/X71n1413.xml","Gulin Qingmao","huihu:no-turn-gulin",1,"verse-author"),
   ("X/X72/X72n1442.xml","Weilin Daopei","huihu:no-turn-weilin",1,"verse-author"),
   ("B/B25/B25n0145.xml","Zhongfeng Mingben","huihu:no-turn-zhongfeng",2,"utterer"),
   ("J/J10/J10nA158.xml","Miyun Yuanwu","huihu:no-turn-miyun",2,"utterer"),
   ("X/X69/X69n1357.xml","Yuanwu Keqin","huihu:accommodation-yuanwu",1,"utterer")]},
 {"id":"t_1e3e02536ca2","term":"疑團","floor":6,
  "target":"a mass of doubt",
  "also":["a knot of doubt"],"aliases":["doubt-mass"],
  "opening":"A compacted mass or knot of doubt, conventionally broken, smashed, pulverized, or made to burst.",
  "body":"Three authored works and three sayings records deploy the image in verse, direct instruction, critique, and narrated breakthrough.",
  "note":"Instruction, verse, and narration are deployment types of one referent. A narrator describing another person's breakthrough does not become that represented experiencer.",
  "review":"REV2",
  "uses":[
   ("J/J25/J25nB166.xml","Fushan Benzhi","doubt-mass:fushan-verse",1,"verse-author",None,"identified-unlinked-master",
    {"contextMasters":[],"reviewedBy":"R88 root source-first review and corrected matrix","reviewedUtc":"2026-07-30T11:53:00Z"}),
   ("X/X63/X63n1257.xml","Wuyi Yuanlai","doubt-mass:boshan-instruction",1,"utterer"),
   ("X/X63/X63n1259.xml","Huishan Jiexian","doubt-mass:huishan-instruction",1,"utterer"),
   ("B/B25/B25n0145.xml","Zhongfeng Mingben","doubt-mass:zhongfeng-verse",2,"verse-author"),
   ("B/B27/B27n0152.xml",None,"doubt-mass:yulin-narration",2,"person-described",None,"narrated",
    {"actorLabel":"Gaofeng Yuanmiao's narrated doubt-mass breakthrough",
     "contextMasters":[{"MasterName":"Yulin Tongxiu","Roles":["commentator","record-owner"]},{"MasterName":"Gaofeng Yuanmiao","Roles":["person-described"]}],
     "reviewedBy":"R88 root source-first review and corrected matrix","reviewedUtc":"2026-07-30T11:53:00Z",
     "actorNote":"Source record (B/B27/B27n0152.xml). Recorded Sayings of National Master Puji Yulin. Yulin Tongxiu narrates Gaofeng Yuanmiao's doubt-mass breakthrough; Gaofeng is the represented experiencer, not the utterer of a quoted line."}),
   ("J/J20/J20nB098.xml","Wunian You","doubt-mass:wunian-instruction",2,"utterer")]}
]
replacement="specs="+repr(specs)
source=template[:start]+replacement+template[end:]
source=source.replace("R84","R88").replace("r84","r88")
source=source.replace('non-iriya-v7-depth-regeneration-r88-review-first-two-a.json','non-iriya-v7-depth-regeneration-r88-corrected-source-matrix-b.json')
source=source.replace('non-iriya-v7-depth-regeneration-r88-review-third-b.json','non-iriya-v7-depth-regeneration-r88-corrected-source-matrix-b.json')
source=source.replace(
    'OLD=M/"non-iriya-v7-depth-regeneration-r88-constructor-config-b.json"',
    'OLD=M/"non-iriya-v7-depth-regeneration-r87-constructor-config-b.json"')
source=source.replace("'review': 'REV1'","'review': REV1").replace("'review': 'REV2'","'review': REV2")
insertion=r'''
# Split 回互 into the three source-adjudicated contextual senses before closure.
for _entry in entries:
    if _entry["id"] != "t_1e3d3a5173a6":
        continue
    import copy as _copy
    _base=_entry["evidenceDraft"]["Entry"]["Senses"][0]
    _groups=[
      ("interrelation or interweaving",["mutual interrelation"],[0,1],
       "Distinct positions or aspects interrelate without collapsing into identity."),
      ("turning, mediation, or responsive accommodation",["room for maneuver","tactful accommodation"],[2,3,4,5,6],
       "The term marks turning or mediation: negative constructions deny deviation or room for maneuver, while a positive construction praises tactful responsiveness.")
    ]
    _senses=[]
    for _target,_alts,_idxs,_explanation in _groups:
        _sense=_copy.deepcopy(_base)
        _sense["PreferredTarget"]=_target
        _sense["AlternateTargets"]=_alts
        _sense["Explanation"]=_explanation
        _sense["ExplanationParts"]["CorpusEarnedOpening"]=_explanation
        _sense["ExplanationParts"]["EvidenceBody"]=[
          "The retained source frames establish this contextual job without making it a universal gloss."
        ]
        _sense["Occurrences"]=[_copy.deepcopy(_base["Occurrences"][_i]) for _i in _idxs]
        _sense["SourceTexts"]=[_base["SourceTexts"][_i] for _i in _idxs]
        _sense["RelatedMasters"]=list(dict.fromkeys(
          _name for _i in _idxs for _name in
          ([_base["Occurrences"][_i].get("MasterName")] +
           [_cm["MasterName"] for _cm in _base["Occurrences"][_i].get("ContextMasters",[])])
          if _name))
        _sense["DraftAcceptedDerivedFields"]={"SourceTexts":_sense["SourceTexts"],"RelatedMasters":_sense["RelatedMasters"]}
        _sense["DraftEvidence"]["IndependentWorkIds"]=[_entry["sourceDossier"]["retainedCompleteCases"][_i]["workId"] for _i in _idxs]
        _authority=[_copy.deepcopy(_entry["sourceDossier"]["sourceAuthorityManifest"]["rows"][_i]) for _i in _idxs]
        for _n,_row in enumerate(_authority,1): _row["EvidenceKey"]=f"o{_n}"
        _sense["DraftEvidence"]["SourceAuthorityRows"]=_authority
        _keys=[f"o{_n}" for _n in range(1,len(_idxs)+1)]
        _sense["DraftEvidence"]["OpeningClaimEvidenceKeys"]=_keys
        _sense["DraftEvidence"]["EvidenceBodyClaimKeys"]=[_keys]
        _sense["Validation"]="multi-source" if len(_idxs)>1 else "provisional"
        _senses.append(_sense)
    _entry["evidenceDraft"]["Entry"]["Senses"]=_senses
    _entry["evidenceDraft"]["EvidenceTransport"]["DepthFloorScope"]="entry"
'''
source=source.replace("\nconfig=copy.deepcopy(old)",insertion+"\nconfig=copy.deepcopy(old)")
template_path=Path(__file__).with_name("build_r84_config_b.py")
exec(compile(source,str(template_path),"exec"),{"__name__":"__main__","__file__":str(template_path)})
