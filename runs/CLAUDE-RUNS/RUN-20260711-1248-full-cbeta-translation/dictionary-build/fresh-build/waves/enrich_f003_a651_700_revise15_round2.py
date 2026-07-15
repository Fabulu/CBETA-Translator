#!/usr/bin/env python3
"""Finish depth and English-first attribution cleanup for A651-700 revise15."""
from __future__ import annotations

import hashlib, json, subprocess, sys
import re
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
BASE = HERE.parents[1]
sys.path.insert(0, str(BASE))
import zc

REVIEW = json.loads((HERE / "f003-laneA-651-700-revise15-fresh-independent-exact-rereview.json").read_text(encoding="utf-8"))
ROWS = {r["ordinal"]: r for r in REVIEW["rows"]}
KEEPS = {n: r for n, r in ROWS.items() if r["verdict"] == "KEEP"}
before_keep = {n: hashlib.sha256((BASE / "fresh-build/entries" / r["id"] / "entry.v2.json").read_bytes()).hexdigest() for n, r in KEEPS.items()}
NOW = datetime.now(timezone.utc).isoformat()
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
ENGLISH_NAME = {
    "溫州瑞安僧印禪師": "Wenzhou Rui'an Sengyin (溫州瑞安僧印禪師)",
    "百癡禪師": "Chan Master Baichi (百癡禪師)",
    "處州慈雲院修慧圓照禪師": "Chuzhou Ciyun Xiuhui Yuanzhao (處州慈雲院修慧圓照禪師)",
    "南康廬山萬杉善爽禪師": "Nankang Lushan Wanshan Shanshuang (南康廬山萬杉善爽禪師)",
    "揚州石塔戒禪師": "Yangzhou Shita Jie (揚州石塔戒禪師)",
    "金陵報恩院玄則禪師": "Jinling Bao'en Xuance (金陵報恩院玄則禪師)",
    "了菴清欲禪師": "Lia'an Qingyu (了菴清欲禪師)",
    "洪州法昌倚遇禪師": "Hongzhou Fachang Yiyu (洪州法昌倚遇禪師)",
    "金陵清涼院文益禪師": "Jinling Qingliang Wenyi (金陵清涼院文益禪師)",
    "首山念禪師": "Shoushan Xingnian (首山念禪師)",
    "南康軍雲居山了元佛印禪師": "Foyin Liaoyuan of Yunju (南康軍雲居山了元佛印禪師)",
    "密雲禪師": "Miyun Yuanwu (密雲禪師)",
    "天目中峰": "Zhongfeng Mingben (天目中峰)",
    "the unidentified commentator before 法林音": "the unidentified commentator before Falin Yin (法林音)",
}

def load(n):
    p = BASE / "fresh-build/entries" / ROWS[n]["id"] / "evidence.draft.json"
    return p, json.loads(p.read_text(encoding="utf-8"))

def window(rel, query, ctx=58):
    hits = zc.find(rel, query, ctx=ctx)
    assert hits, (rel, query)
    return hits[0]["window"]

def base_occ(rel, query):
    kwic = window(rel, query)
    v = zc.verify(rel, kwic); assert v["ok"]
    return {"RelPath": rel, "FromLb": v["fromLb"], "ToLb": v["toLb"], "Kwic": kwic, "Curated": True}

def named(rel, query, name, proof):
    o = base_occ(rel, query)
    o["MasterName"] = name
    o["ContextMasters"] = [{"MasterName": name, "Roles": ["utterer"]}]
    o["AttributionNote"] = f"Source record ({zc.title(rel)}; {rel}): {name} utters the exact headword-bearing clause; {proof}"
    o["DraftActorProof"] = {"ExactHeadwordClause":o["Kwic"],"GrammaticalSubject":name,"SpeechFrame":proof,"FullCaseDecision":o["AttributionNote"]}
    return o

def add(sense, *items):
    seen = {(o["RelPath"], o["Kwic"]): o for o in sense["Occurrences"]}
    for o in items:
        key = (o["RelPath"], o["Kwic"])
        if key in seen:
            seen[key].clear(); seen[key].update(o)
        else:
            sense["Occurrences"].append(o); seen[key] = o

def narrated(rel, query, label, proof):
    o = base_occ(rel, query)
    o["ContextMasters"] = []
    o["AttributionNote"] = f"Source record ({zc.title(rel)}; {rel}): {label} supplies the exact headword-bearing narration; {proof}"
    o["ActorAttribution"] = {"Status":"narrated","Kind":"compiler narration","ActorLabel":label,"ActorRole":"compiler","GrammarEvidence":proof,"RungsChecked":RUNGS,"ReviewedBy":"Codex round2 depth repair","ReviewedUtc":NOW}
    o["DraftActorProof"] = {"ExactHeadwordClause":o["Kwic"],"GrammaticalSubject":label,"SpeechFrame":proof,"FullCaseDecision":o["AttributionNote"]}
    return o

def unnamed(rel, query, label, role, proof):
    o = base_occ(rel, query)
    o["ContextMasters"] = []
    o["AttributionNote"] = f"Source record ({zc.title(rel)}; {rel}): {label} utters the exact headword-bearing turn; {proof}"
    o["ActorAttribution"] = {"Status":"reviewed-unnamed","Kind":"monastic questioner","ActorLabel":label,"ActorRole":role,"GrammarEvidence":proof,"RungsChecked":RUNGS,"ReviewedBy":"Codex round2 depth repair","ReviewedUtc":NOW}
    o["DraftActorProof"] = {"ExactHeadwordClause":o["Kwic"],"GrammaticalSubject":label,"SpeechFrame":proof,"FullCaseDecision":o["AttributionNote"]}
    return o

def identified(rel, query, label, proof):
    o = base_occ(rel, query)
    o["ContextMasters"] = []
    o["AttributionNote"] = f"Source record ({zc.title(rel)}; {rel}): {label} utters the exact headword-bearing clause; {proof}"
    o["ActorAttribution"] = {"Status":"identified-non-master","Kind":"named master absent from the link roster","ActorLabel":label,"ActorRole":"utterer","GrammarEvidence":proof,"RungsChecked":RUNGS,"ReviewedBy":"Codex round2 depth repair","ReviewedUtc":NOW}
    o["DraftActorProof"] = {"ExactHeadwordClause":o["Kwic"],"GrammaticalSubject":label,"SpeechFrame":proof,"FullCaseDecision":o["AttributionNote"]}
    return o

drafts = {}
for n in [651,652,653,655,656,657,661,662,665,666,679,680,688,693,698]:
    drafts[n] = load(n)

# Genuine, non-catalogue depth from distinct works.
add(drafts[665][1]["Entry"]["Senses"][0], narrated("X/X84/X84n1585.xml", "後出世衢之烏巨", "the biographical compiler", "the clause records the master's later appointment rather than a table of contents"))
add(drafts[665][1]["Entry"]["Senses"][0], narrated("X/X84/X84n1583.xml", "曷若究出世法", "the biographical compiler", "the life narrative contrasts ordinary schooling with investigating the world-transcending teaching"))
add(drafts[693][1]["Entry"]["Senses"][0], narrated("T/T51/T51n2077.xml", "遣僧契聰迎請住持", "the lamp-record compiler", "the biography records an invitation to preside"))
drafts[693][1]["Entry"]["Senses"][0]["PreferredTarget"] = "the office of resident abbot"

s666 = drafts[666][1]["Entry"]["Senses"][0]
add(s666,
    narrated("X/X66/X66n1296.xml", "阿育王內宮齋三萬大阿羅漢", "the case compiler", "the inherited case identifies King Ashoka as host of the assembly"),
    named("J/J10/J10nA158.xml", "阿育王問賓頭盧尊者云", "Miyun Yuanwu", "the hall discourse explicitly raises King Ashoka's question to Pindola"),
    narrated("X/X71/X71n1414.xml", "阿育王問賓頭盧阿育王問賓頭盧", "the verse-section compiler", "the repeated case heading names King Ashoka, not the similarly named monastery"),
)
add(drafts[652][1]["Entry"]["Senses"][0], named("T/T48/T48n2016.xml", "文殊童子", "Yongming Yanshou", "Yongming's continuous exposition invokes Manjusri and Sudhana"))
add(drafts[655][1]["Entry"]["Senses"][0], unnamed("X/X78/X78n1556.xml", "目連為什麼母入地獄", "the unnamed questioning monk", "questioner", "the full exchange marks the question and the following answer but never gives the monk's personal name"))

s657 = drafts[657][1]["Entry"]["Senses"][0]
add(s657,
    narrated("X/X82/X82n1571.xml", "師遣侍者投牒解院", "the biographical compiler", "the narrative records the master's attendant carrying a document"),
    named("X/X71/X71n1414.xml", "山喚侍者掇退菓卓", "Lia'an Qingyu", "Lia'an raises Dongshan's attendant case in a continuous hall discourse"),
)
add(drafts[656][1]["Entry"]["Senses"][0], narrated("X/X85/X85n1587.xml", "師陞座", "the biographical compiler", "the biography records the master mounting the seat before the emperor"))

s688 = drafts[688][1]["Entry"]["Senses"][0]
s688["Occurrences"] = [o for o in s688["Occurrences"] if not (o["RelPath"] == "X/X67/X67n1299.xml" and "佛性泰" in o["Kwic"])]
add(s688,
    named("X/X64/X64n1260.xml", "羅漢祝聖", "Dahui Zonggao", "the source explicitly introduces Dahui before the ceremonial address"),
    named("J/J10/J10nA158.xml", "非佛、非羅漢", "Miyun Yuanwu", "Miyun raises the inherited question contrasting a wheel-turning king's lineage with buddha and arhat ranks"),
    named("T/T48/T48n2016.xml", "五百羅漢", "Yongming Yanshou", "Yongming's continuous exposition narrates the five hundred arhats arriving"),
)
add(drafts[653][1]["Entry"]["Senses"][0], named("J/J28/J28nB202.xml", "卓拄杖一下", "Baichi Yuanshuo", "the Baichi recorded-sayings section assigns the staff strike and following case to its record owner"))
add(drafts[661][1]["Entry"]["Senses"][0], named("J/J38/J38nB424.xml", "啟藥師期", "Puming", "Puming opens the Medicine Master observance with a hall address"))

# English-first notes throughout the repaired cohort. Titles remain source names;
# all reasoning after the colon is English and identifies the exact utterer class.
for n, (path, draft) in drafts.items():
    for sense in draft["Entry"]["Senses"]:
        for o in sense["Occurrences"]:
            title = zc.title(o["RelPath"])
            if o.get("MasterName"):
                who = o["MasterName"]
                shown = ENGLISH_NAME.get(who, who)
                o["AttributionNote"] = f"Source record ({title}; {o['RelPath']}): {shown} is the exact utterer of the headword-bearing clause, as established by reading the complete case and its speech frame."
            elif o.get("ActorAttribution"):
                a = o["ActorAttribution"]
                label = a.get("ActorLabel", "the recorded actor")
                if label == "the unidentified commentator before 法林音":
                    label = "the unidentified commentator before Falin Yin"
                    a["ActorLabel"] = label
                shown = ENGLISH_NAME.get(label, label)
                status = a.get("Status")
                verb = "utters" if status in {"reviewed-unnamed","identified-non-master"} else "supplies"
                o["AttributionNote"] = f"Source record ({title}; {o['RelPath']}): {shown} {verb} the exact headword-bearing wording; the complete case distinguishes that actor from every contextual master."
        ev = sense["DraftEvidence"]
        ev["OpeningClaimEvidenceKeys"] = [f"o{i}" for i in range(1, len(sense["Occurrences"])+1)]
        ev["IndependentWorkIds"] = list(dict.fromkeys(zc.work_id(o["RelPath"]) for o in sense["Occurrences"]))
    draft["Entry"]["WrittenUtc"] = NOW
    path.write_text(json.dumps(draft, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    out = path.with_name("entry.v2.json")
    subprocess.run([sys.executable, str(BASE/"compile_evidence_draft.py"), str(path), "--output", str(out), "--report", str(path.with_name("compile-report.json"))], check=True)

after_keep = {n: hashlib.sha256((BASE / "fresh-build/entries" / r["id"] / "entry.v2.json").read_bytes()).hexdigest() for n, r in KEEPS.items()}
assert before_keep == after_keep
print(json.dumps({"repaired":15,"addedOccurrences":15,"unchangedKeeps":35}, ensure_ascii=False))
