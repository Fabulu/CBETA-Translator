#!/usr/bin/env python3
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

def load(entry_id):
    path = ROOT / "fresh-build" / "entries" / entry_id / "evidence.draft.json"
    return path, json.loads(path.read_text(encoding="utf-8"))

path, data = load("t_21926ca0b92e")
sense = data["Entry"]["Senses"][0]
row = next(o for o in sense["Occurrences"] if o["RelPath"] == "J/J34/J34nB300.xml")
row["AttributionNote"] = "Source text Record of Master Chaozong (朝宗禪師語錄): the unnamed monk is the exact speaker who says the furnace has opened the crown-gate eye. Chaozong Tongren immediately responds by asking what that eye is, then strikes after the monk bows and says it has been knocked blind."
row["ActorAttribution"]["GrammarEvidence"] = "The explicit turn marker 進云 introduces the unnamed monk’s claim 恁麼則豁開頂門眼; the following 師云 separately introduces Chaozong Tongren’s response 如何是頂門眼 and later action."
row["ContextMasters"] = [{"MasterName":"Chaozong Tongren","Roles":["respondent","interlocutor","record-owner"]}]
row["DraftActorProof"]["FullCaseDecision"] = "The 進云／師云 sequence assigns the opening assertion to the unnamed monk and the question, strike, and comment to Chaozong Tongren; the two turns are not collapsed."
path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")

path, data = load("t_c327d2a1fc8c")
aliases=data["Entry"]["Senses"][0]["SearchAliases"]
seen=set(); data["Entry"]["Senses"][0]["SearchAliases"]=[a for a in aliases if not (a in seen or seen.add(a))]
path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")

path, data = load("t_07d808115439")
sense=data["Entry"]["Senses"][0]
row=next(o for o in sense["Occurrences"] if o["RelPath"]=="X/X68/X68n1318.xml" and o["FromLb"]=="0409a02")
row.pop("MasterName",None)
row["AttributionNote"] = "Source text Continued Essential Sayings of Ancient Venerable Masters (續古尊宿語要): the document transmits the positive formula before the explicit marker ‘Baoning says’ and therefore leaves its voice unattributed. Baoning Renyong is the commentator who owns only the following counter-formulation."
row["ActorAttribution"]={"Status":"impersonal","Kind":"transmitted formula","ActorLabel":"the unattributed positive formula","ActorRole":"compiler","GrammarEvidence":"The positive wording ends before the explicit speech marker 保寧道; that marker opens Baoning Renyong’s following counter-formulation and cannot retroactively assign the preceding clause to him.","ReviewedBy":"Codex post-independent-review exact-turn repair","ReviewedUtc":"2026-07-15T00:00:00Z"}
row["ContextMasters"]=[{"MasterName":"Baoning Renyong","Roles":["commentator","later-raiser","section-subject"]}]
row["DraftActorProof"]={"GrammaticalSubject":"the unattributed transmitted formula","FullCaseDecision":"The formula precedes 保寧道 and has no named speaker in its own turn; Baoning Renyong owns the marked counter-formulation that follows, not this positive formula."}
body=sense["ExplanationParts"]["EvidenceBody"]
sense["ExplanationParts"]["EvidenceBody"]=[p.replace("Baoning Renyong first says that it cannot be known by wisdom or recognized by consciousness and pairs the headword with mental activity extinguished; his next line reverses both predicates, saying", "An unattributed transmitted formula says that it cannot be known by wisdom or recognized by consciousness and pairs the headword with mental activity extinguished; after the explicit marker ‘Baoning says,’ Baoning Renyong reverses the predicates, saying") for p in body]
path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
