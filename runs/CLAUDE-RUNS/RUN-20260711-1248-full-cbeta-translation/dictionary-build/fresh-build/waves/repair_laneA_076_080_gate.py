import json
from pathlib import Path

R = Path(__file__).resolve().parents[2]

def load(eid):
    p = R / "fresh-build" / "entries" / eid / "evidence.draft.json"
    return p, json.loads(p.read_text(encoding="utf-8"))

def save(p, d):
    p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

# 活人劍: reader-visible notes, exact nested speaker, and one additional exact work.
p, d = load("t_e6eb14b6c1ca")
s = d["Entry"]["Senses"][0]
s["ExplanationParts"]["CorpusEarnedOpening"] = s["ExplanationParts"]["CorpusEarnedOpening"].replace("whether a teacher possesses", "whether a named teacher possesses")
o = s["Occurrences"]
o[0]["AttributionNote"] = "Miyun Yuanwu, in the Recorded Sayings of Chan Master Miyun (密雲禪師語錄), answers the monk's life-giving-sword question with a blow and tells him to take it away."
o[1]["AttributionNote"] = "Guting Shanjian, in the Abridged Recorded Sayings of Chan Master Guting (古庭禪師語錄輯略), raises Luopu's final instruction and pairs the killing knife with the life-giving sword."
o[2]["AttributionNote"] = "Tian'an Sheng, in the Recorded Sayings of Chan Master Tian'an Sheng (天岸昇禪師語錄), gives the four-turn formulation in a precept-giving hall address."
o[4]["AttributionNote"] = "Yulin Tongxiu, in the Recorded Sayings of National Teacher Puji Yulin (普濟玉琳國師語錄), speaks about his teacher Huanyou's killing knife and life-giving sword during a hall address."
o[5]["AttributionNote"] = "Dahui Zonggao, quoted by Tianyin Yuanxiu in the Recorded Sayings of Master Tianyin (天隱和尚語錄), says that Huangbo has only the killing sword and lacks the life-giving sword."
o[5]["ContextMasters"].insert(0, {"MasterName":"Dahui Zonggao","Roles":["utterer"]})
o.append({"RelPath":"J/J38/J38nB415.xml","FromLb":"0452c29","ToLb":"0452c30","Kwic":"殺人須是殺人刀，活人須是活人劍。","MasterName":"Miaoyun Xiong","Curated":True,"AttributionNote":"Miaoyun Xiong, in the Recorded Sayings of Chan Master Dabei Miaoyun (大悲玅雲禪師語錄), comments after raising Deshan's words that the killing knife must kill and the life-giving sword must give life.","ContextMasters":[{"MasterName":"Miaoyun Xiong","Roles":["utterer","commentator","record-owner"]}],"DraftActorProof":{"ExactHeadwordClause":"殺人須是殺人刀，活人須是活人劍。","SpeechFrame":"The sentence follows Miaoyun's own comment and precedes his next raised case.","FullCaseDecision":"Miaoyun Xiong owns the exact headword-bearing comment; Deshan is the person discussed in the preceding quotation."}})
s["SourceTexts"].append("J/J38/J38nB415.xml")
s["DraftEvidence"]["IndependentWorkIds"].append("work:J38nB415")
s["DraftEvidence"]["OpeningClaimEvidenceKeys"].append("o7")
save(p,d)

# 大機大用: component-only parallels remain family controls, not occurrences.
p, d = load("t_d03aa9267f79")
s = d["Entry"]["Senses"][0]
s["Occurrences"] = [x for x in s["Occurrences"] if s.get("SourceTerm", "大機大用") in x["Kwic"] or "大機大用" in x["Kwic"]]
s["SourceTexts"] = list(dict.fromkeys(x["RelPath"] for x in s["Occurrences"]))
s["DraftEvidence"]["IndependentWorkIds"] = list(dict.fromkeys("work:" + Path(x).stem for x in s["SourceTexts"]))
s["DraftEvidence"]["OpeningClaimEvidenceKeys"] = [f"o{i}" for i in range(1, len(s["Occurrences"])+1)]
save(p,d)

# 呵佛罵祖: add an exact fourth work; variant witnesses remain explicitly governed.
p, d = load("t_1da939bf1267")
s = d["Entry"]["Senses"][0]
s["Occurrences"].append({"RelPath":"T/T48/T48n2003.xml","FromLb":"0143b19","ToLb":"0143b20","Kwic":"此子已後。向孤峯頂上。盤結草庵。呵佛罵祖去在","MasterName":"Guishan Lingyou","Curated":True,"AttributionNote":"Guishan Lingyou, quoted in Yuanwu Keqin's Blue Cliff Record (佛果圜悟禪師碧巖錄), predicts that Deshan will build a grass hut on a solitary peak and revile buddhas and curse patriarchs.","ContextMasters":[{"MasterName":"Guishan Lingyou","Roles":["utterer","case-figure"]},{"MasterName":"Yuanwu Keqin","Roles":["later-quoter","commentator","record-owner"]}],"DraftActorProof":{"ExactHeadwordClause":"呵佛罵祖去在","SpeechFrame":"Guishan's marked reply predicts Deshan's later conduct; Yuanwu reproduces it within the case.","FullCaseDecision":"Guishan Lingyou owns the exact prediction; Yuanwu Keqin is the later quoter and commentator."}})
s["SourceTexts"].append("T/T48/T48n2003.xml")
s["DraftEvidence"]["IndependentWorkIds"].append("work:T48n2003")
s["DraftEvidence"]["OpeningClaimEvidenceKeys"].append("o11")
save(p,d)
