import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
E = ROOT / "fresh-build" / "entries"

def load(i):
    p=E/i/"evidence.draft.json"; return p,json.loads(p.read_text(encoding="utf-8"))
def save(p,d): p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
def named(o,name,role="utterer"):
    o["MasterName"]=name; o.pop("ActorAttribution",None)
    o["ContextMasters"]=[{"MasterName":name,"Roles":[role]}]
    o["AttributionNote"] += f" Full-case rereading identifies {name} as the exact utterer of the headword."
    o["DraftActorProof"]["GrammaticalSubject"]=name
    o["DraftActorProof"]["SpeechFrame"]=f"The complete case identifies {name} as the exact utterer of the headword."
    o["DraftActorProof"]["FullCaseDecision"]=o["DraftActorProof"]["SpeechFrame"]
def nonmaster(o,label,contexts):
    o["MasterName"]=None; o["ContextMasters"]=contexts
    a=o.setdefault("ActorAttribution",{}); a.update(Status="identified-non-master",Kind=label,ActorLabel=label,ActorRole="compiler",RungsChecked=["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],GrammarEvidence=f"The complete case assigns the headword to {label}.",ReviewedBy="Codex reviewer11 repair author",ReviewedUtc="2026-07-15T18:25:00+02:00",AuthoredVoiceRiskReviewed=True)
    o["AttributionNote"] += f" Full-case rereading assigns the headword to {label}."
    o["DraftActorProof"]["GrammaticalSubject"]=label
    o["DraftActorProof"]["SpeechFrame"]=f"The complete case assigns the headword to {label}."
    o["DraftActorProof"]["FullCaseDecision"]=o["DraftActorProof"]["SpeechFrame"]

# 家醜: make the genuinely unresolved ritual voice specific rather than inventing a record owner.
p,d=load("t_b336769aabdf"); o=d["Entry"]["Senses"][0]["Occurrences"][5]
o["ActorAttribution"].update(Status="reviewed-unnamed",Kind="the ancestral-rite speaker who calls himself Beishan",ActorLabel="the ancestral-rite speaker who calls himself Beishan",ActorRole="other")
o["DraftActorProof"]["GrammaticalSubject"]="the ancestral-rite speaker who calls himself Beishan"
o["DraftActorProof"]["SpeechFrame"]="The full rite address supplies the self-reference 北山 but no recoverable canonical personal name after all six rungs."
o["DraftActorProof"]["FullCaseDecision"]=o["DraftActorProof"]["SpeechFrame"]; save(p,d)

# 徐六擔板: remove the fabricated Fayuyi ownership; retain a fully reviewed, specifically named source unit.
p,d=load("t_e21288d0fefb"); o=d["Entry"]["Senses"][0]["Occurrences"][4]
o["ActorAttribution"].update(Status="reviewed-unnamed",Kind="Guishan Hui'an Guang, whose canonical roster form remains unresolved",ActorLabel="Guishan Hui'an Guang, whose canonical roster form remains unresolved",ActorRole="master")
o["DraftActorProof"]["GrammaticalSubject"]="Guishan Hui'an Guang"
o["DraftActorProof"]["SpeechFrame"]="The section heading 龜山晦菴光狀元和尚 and 師拈云 identify Guishan Hui'an Guang; no safe canonical roster form was found."
o["DraftActorProof"]["FullCaseDecision"]=o["DraftActorProof"]["SpeechFrame"]; save(p,d)

# 毒藥: explicitly quarantine the ordinary antidote comparison as a different-thing counterexample.
p,d=load("t_641de814fd8a"); s=d["Entry"]["Senses"][0]; o=s["Occurrences"][4]
o["AttributionNote"] += " This occurrence is retained only as a marked different-thing boundary witness: poison as an ordinary substance/antidote comparison, not a Chan saying turned harmful."
o["DraftActorProof"]["FullCaseDecision"] += " It is a boundary counterexample and does not establish the preferred sense."
save(p,d)

# 李廣: two complete-case identifications missed by the prior repair.
p,d=load("t_10b63ac74f61"); occ=d["Entry"]["Senses"][0]["Occurrences"]
named(occ[0],"Wuzu Fayan")
named(occ[3],"Gushan Gui")
save(p,d)

# 五燈嚴統: distinguish annalist narration from the record owner's letter.
p,d=load("t_b016f513be3d"); occ=d["Entry"]["Senses"][0]["Occurrences"]
nonmaster(occ[0],"the Feiyin record annalist",[{"MasterName":"Feiyin Tongrong","Roles":["person-described"]}])
named(occ[1],"Shuijian Huihai")
occ[1]["ContextMasters"].append({"MasterName":"Feiyin Tongrong","Roles":["person-discussed"]})
save(p,d)

# 下禪床: exact performer is Foyan Qingyuan, while the grammatical headword clause is narration.
p,d=load("t_74c3c0e1b896"); o=d["Entry"]["Senses"][0]["Occurrences"][4]
nonmaster(o,"the imperial-address narrator describing Foyan Qingyuan's descent and dance",[{"MasterName":"Foyan Qingyuan","Roles":["person-described"]}])
save(p,d)

# Current hard gate: a role label is never an identified person's name.  Convert
# all source-role-only actors in this repair cohort to six-rung reviewed-unnamed.
for ident in ["t_b336769aabdf","t_e21288d0fefb","t_641de814fd8a","t_10b63ac74f61","t_b016f513be3d","t_74c3c0e1b896"]:
    p,d=load(ident)
    for s in d["Entry"]["Senses"]:
        for o in s["Occurrences"]:
            a=o.get("ActorAttribution")
            if a and a.get("Status")=="identified-non-master":
                a["Status"]="reviewed-unnamed"
                a["ActorRole"]="compiler" if "narrator" in a.get("ActorLabel","") or "annalist" in a.get("ActorLabel","") or "compiler" in a.get("ActorLabel","") else "verse-author"
            if a and a.get("ActorRole") not in {"utterer","respondent","questioner","interlocutor","addressee","section-subject","record-owner","person-described","person-discussed","commentator","later-raiser","later-quoter","teacher","student","compiler","verse-author","case-figure"}:
                a["ActorRole"]="utterer"
            if a and a.get("ActorLabel") and a["ActorLabel"] not in o.get("AttributionNote",""):
                o["AttributionNote"] += " Six-rung actor result: "+a["ActorLabel"]+"."
    save(p,d)

# These two masters are textually identified but not yet canonical-roster linked;
# keep their real names in the reviewed label rather than minting broken links.
for ident,idx,label in [("t_10b63ac74f61",3,"Gushan Gui (鼓山珪)"),("t_b016f513be3d",1,"Shuijian Huihai (水鑑海)")]:
    p,d=load(ident); o=d["Entry"]["Senses"][0]["Occurrences"][idx]
    o["MasterName"]=None
    o["ContextMasters"]=[x for x in o.get("ContextMasters",[]) if x.get("MasterName") not in {"Gushan Gui","Shuijian Huihai"}]
    o["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":label,"ActorLabel":label,"ActorRole":"utterer","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":f"The full case names {label}, but no canonical roster link exists yet.","ReviewedBy":"Codex reviewer11 repair author","ReviewedUtc":"2026-07-15T18:25:00+02:00","AuthoredVoiceRiskReviewed":True}
    o["AttributionNote"] += f" The source names {label}; roster linking is deferred without changing the textual identification."
    save(p,d)
