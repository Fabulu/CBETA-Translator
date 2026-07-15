#!/usr/bin/env python3
"""Close the mechanical/depth failures left by the A651-700 round-3 repair."""

from __future__ import annotations

import json
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

HERE = Path(__file__).resolve().parent
BASE = HERE.parents[1]
sys.path.insert(0, str(BASE))
import zc  # noqa: E402

NOW = datetime.now(timezone.utc).isoformat()
AUTHOR = "Codex A651-700 round3 gate-fix repair author"
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
IDS = {
    651: "t_c06edf8c18f1", 652: "t_6e4234dfd60f", 655: "t_c193d5b26854",
    656: "t_cbf868f557e2", 657: "t_cb44465faa59", 661: "t_f74516e0ba71",
    662: "t_279cf2b97244", 666: "t_612cae8da268", 679: "t_4da199fae933",
    680: "t_2d567d37256b", 688: "t_cc8c8a1cb550", 693: "t_453e3c35baa6",
    698: "t_ff91d085da8d",
}


def path(n: int) -> Path:
    return BASE / "fresh-build/entries" / IDS[n] / "evidence.draft.json"


def load(n: int) -> dict:
    return json.loads(path(n).read_text(encoding="utf-8"))


def occs(d: dict) -> list[dict]:
    return [o for s in d["Entry"]["Senses"] for o in s["Occurrences"]]


def find(d: dict, needle: str) -> dict:
    got = [o for o in occs(d) if needle in o["Kwic"]]
    assert len(got) == 1, (d["Entry"]["SourceTerm"], needle, len(got))
    return got[0]


def named(o: dict, name: str, proof: str, extras: list[dict] | None = None) -> None:
    o["MasterName"] = name
    o.pop("ActorAttribution", None)
    o["ContextMasters"] = [{"MasterName": name, "Roles": ["utterer"]}] + (extras or [])
    note = f"Source record ({zc.title(o['RelPath'])}; {o['RelPath']}): {name} utters the headword. {proof}"
    o["AttributionNote"] = note
    o["DraftActorProof"] = {
        "ExactHeadwordClause": o["Kwic"], "GrammaticalSubject": name,
        "SpeechFrame": proof, "FullCaseDecision": note,
    }


def exception(o: dict, status: str, kind: str, label: str, role: str, proof: str,
              extras: list[dict] | None = None) -> None:
    o.pop("MasterName", None)
    o["ContextMasters"] = extras or []
    note = f"Source record ({zc.title(o['RelPath'])}; {o['RelPath']}): {label} supplies the headword. {proof}"
    o["AttributionNote"] = note
    o["DraftActorProof"] = {
        "ExactHeadwordClause": o["Kwic"], "GrammaticalSubject": label,
        "SpeechFrame": proof, "FullCaseDecision": note,
    }
    o["ActorAttribution"] = {
        "Status": status, "Kind": kind, "ActorLabel": label, "ActorRole": role,
        "RungsChecked": RUNGS, "GrammarEvidence": proof,
        "ReviewedBy": AUTHOR, "ReviewedUtc": NOW,
    }


def window(rel: str, query: str, ctx: int = 120) -> dict:
    hits = zc.find(rel, query, ctx=ctx, limit=20)
    hit = next(h for h in hits if query in h["window"])
    v = zc.verify(rel, hit["window"])
    assert v["ok"]
    return {"RelPath": rel, "FromLb": v["fromLb"], "ToLb": v["toLb"],
            "Kwic": hit["window"], "Curated": True}


def add_named(s: dict, rel: str, query: str, name: str, proof: str,
              extras: list[dict] | None = None) -> None:
    o = window(rel, query); named(o, name, proof, extras); s["Occurrences"].append(o)


def add_exception(s: dict, rel: str, query: str, status: str, kind: str,
                  label: str, role: str, proof: str,
                  extras: list[dict] | None = None) -> None:
    o = window(rel, query); exception(o, status, kind, label, role, proof, extras); s["Occurrences"].append(o)


drafts = {n: load(n) for n in IDS}

# Rewrite all previously flagged notes in English-first prose while preserving exact source titles.
named(find(drafts[651], "莫錯會好"), "Tiantai Deshao",
      "The section header names Tiantai Deshao, and the continuous master-said frame governs this warning.")
named(find(drafts[651], "不錯謬乎"), "Mahakasyapa",
      "The explicit question frame assigns the sentence to Mahakasyapa before the assembly answers.")
named(find(drafts[655], "此目連之孝也"), "Xinghua Shaoqing",
      "Parallel lamp witnesses place the complete memorial-day hall address under Xinghua Shaoqing.")
named(find(drafts[656], "法昌今日開爐"), "Fachang Yiyu",
      "The section header names Fachang Yiyu as the presider who mounts the seat and begins the address.")
named(find(drafts[656], "少頃陞座，僧問"), "Fayan Wenyi",
      "The section header names Fayan Wenyi as the master mounting the seat; the monk asks afterward.")
named(find(drafts[657], "鑑乃顧侍者云"), "Nengren Jian",
      "The preceding marked speech names Nengren Jian, whose turn continues into this address to the attendant.")
named(find(drafts[657], "洞山冬夜喫菓子次"), "Dongshan Liangjie",
      "The embedded case assigns the call to Dongshan Liangjie; Lia'an Qingyu quotes and comments afterward.",
      [{"MasterName": "Lia'an Qingyu", "Roles": ["later-quoter", "commentator"]}])
exception(find(drafts[661], "啟藥師期，上堂"), "narrated", "ceremony and hall-event narration",
          "the source recorder", "compiler",
          "The recorder announces the Medicine Master observance and hall event; Puming supplies the following address.",
          [{"MasterName": "Puming", "Roles": ["record-owner", "respondent"]}])
o661_mid = next(o for o in occs(drafts[661]) if o["RelPath"] == "B/B25/B25n0145.xml")
named(o661_mid, "Zhongfeng Mingben",
      "Zhongfeng Mingben's Extended Record (天目中峰廣錄) assigns this formula to him in continuous speech.")
named(find(drafts[662], "法座前拈疏"), "Baichi Yuanshuo",
      "Baichi Yuanshuo's record (百癡禪師語錄) assigns the marked address to him.")
exception(find(drafts[662], "序金粟費大師語錄序佛祖之道"), "identified-non-master",
          "signed preface author", "Tang Shiji", "compiler",
          "The signed colophon names Tang Shiji as the author of this preface.")
named(find(drafts[679], "自後不知消息"), "Yanguan Qi'an",
      "The explicit speech marker assigns this report about the absent monk to Yanguan Qi'an.")
named(find(drafts[680], "佛謂富樓那曰"), "Shakyamuni Buddha",
      "The explicit Buddha-addresses-Purna frame assigns the embedded turn to Shakyamuni Buddha; Dahui quotes it later.",
      [{"MasterName": "Dahui Zonggao", "Roles": ["later-quoter", "record-owner"]}])
named(find(drafts[688], "羅漢有一句"), "Luohan Qin",
      "The section and hall-speech frame assign this first-person statement to Luohan Qin.")
exception(find(drafts[693], "住持僧忠智奏"), "identified-non-master", "signed memorial author",
          "monk Zhongzhi", "compiler", "The memorial heading names monk Zhongzhi as its documentary author.")
exception(find(drafts[693], "住持婁東行悅述"), "identified-non-master", "signed preface author",
          "Loudong Xingyue", "compiler", "The author line names Loudong Xingyue as the writer of the preface.")
named(find(drafts[698], "居士若道得即請坐"), "Foyin Liaoyuan",
      "The section header and master-said frame assign this challenge to Foyin Liaoyuan.")

# Restore honest depth with distinct, context-read deployments rather than overlapping windows.
s = drafts[662]["Entry"]["Senses"][0]
add_exception(s, "M/M59/M59n1540.xml", "僧問馬祖如何是佛祖云即心是佛",
              "reviewed-unnamed", "unnamed monk questioner", "an unnamed monk", "questioner",
              "The monk utters the headword in his question; Mazu's answer begins after the speech marker.",
              [{"MasterName": "Mazu Daoyi", "Roles": ["respondent", "case-figure"]},
               {"MasterName": "Dahui Zonggao", "Roles": ["later-raiser", "record-owner"]}])
add_exception(s, "B/B27/B27n0152.xml", "僧問白雲祖師如何是佛祖云鑊湯無冷處",
              "reviewed-unnamed", "unnamed monk questioner", "an unnamed monk", "questioner",
              "The monk utters the headword in his question; Baiyun's answer follows after the speech marker.",
              [{"MasterName": "Baiyun Shouduan", "Roles": ["respondent", "case-figure"]},
               {"MasterName": "Yulin Tongxiu", "Roles": ["later-raiser", "record-owner"]}])

s = drafts[693]["Entry"]["Senses"][0]
add_exception(s, "T/T47/T47n1987A.xml", "有信士王若一。捨何王觀。請師住持",
              "narrated", "biographical narration", "the biographer", "compiler",
              "The biographer reports that the patron Wang Ruoyi donates the site and asks Caoshan Benji to serve as abbot.",
              [{"MasterName": "Caoshan Benji", "Roles": ["person-described", "section-subject"]}])

s = drafts[666]["Entry"]["Senses"][0]
add_named(s, "X/X69/X69n1366.xml", "昔阿育王造佛塔，其數滿八萬四千", "Wuchu Daguan",
          "Wuchu Daguan invokes King Ashoka's construction of eighty-four thousand stupas in his authored verse.")
add_exception(s, "X/X78/X78n1553.xml", "居士又再問：阿育王造八萬四千寶塔",
              "reviewed-unnamed", "unnamed lay questioner", "an unnamed layman", "questioner",
              "The layman utters the headword in a public question about King Ashoka's eighty-four thousand stupas.")

s = drafts[652]["Entry"]["Senses"][0]
add_named(s, "T/T47/T47n1998A.xml", "文殊曰。弗也舍利弗", "Manjusri",
          "The embedded dialogue explicitly assigns the answer to Manjusri; Dahui raises it in his address.",
          [{"MasterName": "Dahui Zonggao", "Roles": ["later-raiser", "record-owner"]}])
add_named(s, "M/M59/M59n1540.xml", "文殊曰是藥者採將來", "Manjusri",
          "The inherited medicine case explicitly assigns the instruction to Manjusri; Dahui comments afterward.",
          [{"MasterName": "Dahui Zonggao", "Roles": ["later-raiser", "commentator"]}])

s = drafts[657]["Entry"]["Senses"][0]
for rel, query, proof, extras in [
    ("X/X84/X84n1580.xml", "侍者問：和尚適來莫是成褫伊麼", "The attendant directly asks the headword-bearing question after the monk withdraws.", []),
    ("X/X80/X80n1565.xml", "侍者問曰。殻在這裏。蟬向甚麼處去也", "The attendant directly asks where the cicada has gone and then awakens at the master's response.", []),
    ("J/J37/J37nB386.xml", "至晚，侍者問：和尚被者僧不肯了便休", "The attendant directly questions Baizhang about the earlier exchange.", [{"MasterName": "Baizhang Huaihai", "Roles": ["respondent", "case-figure"]}]),
    ("B/B25/B25n0144.xml", "當風颺殼時如何", "The attendant directly asks the second of Qinshan's relayed questions.", []),
]:
    add_exception(s, rel, query, "reviewed-unnamed", "unnamed attendant questioner",
                  "an unnamed attendant", "questioner", proof, extras)

for n, draft in drafts.items():
    for sense in draft["Entry"]["Senses"]:
        # Exact duplicate passage anchors are never allowed to count as depth.
        seen = set()
        for o in sense["Occurrences"]:
            v = zc.verify(o["RelPath"], o["Kwic"]); assert v["ok"], (n, o["RelPath"])
            o["FromLb"], o["ToLb"] = v["fromLb"], v["toLb"]
            key = (o["RelPath"], o["FromLb"], o["ToLb"], "".join(o["Kwic"].split()))
            assert key not in seen, (n, key)
            seen.add(key)
        ev = sense["DraftEvidence"]
        ev["OpeningClaimEvidenceKeys"] = [f"o{i}" for i in range(1, len(sense["Occurrences"]) + 1)]
        ev["IndependentWorkIds"] = list(dict.fromkeys(zc.work_id(o["RelPath"]) for o in sense["Occurrences"]))
    draft["Entry"]["WrittenUtc"] = NOW
    path(n).write_text(json.dumps(draft, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    out = BASE / "fresh-build/entries" / IDS[n] / "entry.v2.json"
    report = out.with_name("compile-report.json")
    subprocess.run([sys.executable, str(BASE / "compile_evidence_draft.py"), str(path(n)),
                    "--output", str(out), "--report", str(report)], check=True)

print(json.dumps({"compiled": len(drafts), "terms": [drafts[n]["Entry"]["SourceTerm"] for n in drafts]}, ensure_ascii=False))
