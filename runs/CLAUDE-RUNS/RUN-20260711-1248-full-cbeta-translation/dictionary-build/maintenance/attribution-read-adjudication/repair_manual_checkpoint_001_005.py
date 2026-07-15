#!/usr/bin/env python3
"""Apply only the eight human-adjudicated repairs from checkpoint 001--005."""
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
UTC = "2026-07-15T20:30:00Z"
OUT = ROOT / "maintenance/attribution-read-adjudication/cohorts-7-9-repair-001-005.json"


def load(eid):
    path = ROOT / f"fresh-build/entries/{eid}/entry.v2.json"
    return path, json.loads(path.read_text(encoding="utf-8"))


def occ(entry, sense, number):
    return entry["Senses"][sense - 1]["Occurrences"][number - 1]


def context(master, roles):
    return {"MasterName": master, "Roles": roles}


def narrated_action(o, actor, evidence, contexts):
    o.pop("MasterName", None)
    o["ActorAttribution"] = {
        "Status": "narrated",
        "Kind": "narrated physical action",
        "ActorLabel": actor,
        "ActorRole": "action-performer",
        "ReviewedBy": "Codex manual full-case cohorts 7-9",
        "ReviewedUtc": UTC,
        "GrammarEvidence": evidence,
    }
    o["ContextMasters"] = contexts


def named_master(o, master, note, roles=None):
    o["MasterName"] = master
    o.pop("ActorAttribution", None)
    o["ContextMasters"] = [context(master, roles or ["utterer"])]
    o["AttributionNote"] = note


def named_nonmaster(o, actor, role, note, evidence, contexts=None):
    o.pop("MasterName", None)
    o["ActorAttribution"] = {
        "Status": "identified-non-master",
        "Kind": "identified written or spoken voice",
        "ActorLabel": actor,
        "ActorRole": role,
        "RungsChecked": ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"],
        "ReviewedBy": "Codex manual full-case cohorts 7-9",
        "ReviewedUtc": UTC,
        "GrammarEvidence": evidence,
    }
    o["ContextMasters"] = contexts or []
    o["AttributionNote"] = note


def main():
    changed = []

    path, e = load("t_6bc71cc88c2f")
    o = occ(e, 1, 1)
    narrated_action(o, "Huineng", "盧見師奔至 carries 盧 into 即擲衣鉢於磐石曰: Huineng performs 擲; 曰 introduces his following words.", [context("Huineng", ["action-performer"]), context("Huiming", ["student"])])
    o["AttributionNote"] = "Compendium of the Five Lamps (五燈會元): Huineng is the narrated action-performer who throws the robe and bowl onto the rock; his speech begins afterward with 此衣表信."
    o = occ(e, 2, 3)
    narrated_action(o, "the unnamed Caoqi robe-and-bowl attendant", "曹溪守衣鉢侍者…乃提起衣曰 assigns 提起 to the unnamed attendant; his quoted words begin 者是大庾嶺頭.", [context("Huineng", ["case-figure"])])
    o["AttributionNote"] = "Collected Old Cases Raised in the Lineage (宗門拈古彙集): an unnamed Caoqi robe-and-bowl attendant performs 提起; the record does not name him in any of the six attribution rungs."
    o = occ(e, 2, 4)
    named_master(o, "Feiyin Tongrong", "Five Lamps Strict Lineage: Resolving Doubts (五燈嚴統解惑編), letter to the Hangzhou gentry: Feiyin Tongrong is the written speaker who reports the custody and transmission of the robe and bowl; 先師 and 予 identify his first-person letter voice.")
    path.write_text(json.dumps(e, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    changed.append(path)

    path, e = load("t_6f47a97d45b0")
    o = occ(e, 1, 2)
    named_nonmaster(o, "Lin Zhifan", "utterer", "Preface to the Extensive Record of Heshang Yongjue (永覺和尚廣錄序): Lin Zhifan is the named preface writer; 命之蕃序, 蕃自顧, and 謹序曰 identify his written voice.", "命之蕃序其卷首 is followed by 蕃自顧鈍根 and 謹序曰, naming Lin Zhifan rather than an impersonal heading.")
    o = occ(e, 1, 6)
    named_master(o, "Linquan Conglun", "General Preface to Linquan's Commentary (林泉老人評唱投子丹霞頌古總序): Linquan Conglun is the written speaker; the prose says 竊窺 and closes 林泉老衲…說.", ["utterer", "commentator"])
    o = occ(e, 1, 7)
    named_master(o, "Weilin Daopei", "Preface to the Late Gushan Record (鼓山晚錄序): Weilin Daopei is the exact written speaker, identified by 小子霈 and the signature 嗣法弟子道霈焚香稽首題.")
    e["Senses"][0]["Explanation"] = "The headword names a preface: prose placed before a record, collection, or scripture to introduce its compilation, transmission, author, or publication. The corpus supplies repeated title headings, named preface writers, first-person prefaces, and requests that someone compose a preface. Xu Fu, Lin Zhifan, Xiong Kaiyuan, Yang Yi, Yunqi Zhuhong, Linquan Conglun, Weilin Daopei, and Sanfeng Hanyue Fazang are established passage by passage as writers or speakers. A record owner named in a heading is contextual and is not thereby the preface voice."
    path.write_text(json.dumps(e, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    changed.append(path)

    path, e = load("t_77774b8724f1")
    o = occ(e, 1, 1)
    named_nonmaster(o, "Layman Shi Zhicheng", "questioner", "Patriarchs' Hall Collection (祖堂集), Reply to Layman Shi's Ten Questions: Layman Shi Zhicheng is the exact questioner using 付法 in his ninth question; Guifeng Zongmi's answer follows outside this stored turn.", "第九問曰 governs 又佛滅後付法於迦葉; the wider title and colophon identify the lay questioner as 史制誠.", [context("Guifeng Zongmi", ["respondent"])])
    e["Senses"][0]["Explanation"] = "The term names the act or formula of entrusting the teaching from a teacher to a successor: literally ‘entrust’ (付) the ‘teaching’ (法), attested 608 times in 143 allowlisted texts. Lamp records use it for a predecessor handing the teaching to a successor. Layman Shi Zhicheng's ninth question summarizes the lineage claim as ‘after the Buddha's extinction, the teaching was entrusted to Kasyapa and mind was transmitted by mind’ (佛滅後付法於迦葉，以心傳心). Narrative closure frequently says ‘after the patriarch had entrusted the teaching’ (祖付法已), followed by his departure or death. The compound ‘verse on entrusting the teaching’ (付法偈) names verses spoken or recorded at that handover."
    path.write_text(json.dumps(e, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    changed.append(path)

    path, e = load("t_7887dc8d449f")
    o = occ(e, 1, 8)
    named_nonmaster(o, "Yan Cheng", "utterer", "Vow Verse for Printing the Record of Pointing at the Moon (刻指月錄發願偈): Yan Cheng is the signed prose author who discusses the old established seniors.", "The complete document signs the passage 萬曆辛丑歲八月初三日，吳郡嚴澂和南書.")
    path.write_text(json.dumps(e, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    changed.append(path)

    rows = []
    for p in changed:
        rows.append({"entry": p.parent.name, "path": str(p.relative_to(ROOT)), "sha256": hashlib.sha256(p.read_bytes()).hexdigest()})
    OUT.write_text(json.dumps({"generatedUtc": datetime.now(timezone.utc).isoformat(), "checkpoint": [1, 5], "repairs": 8, "entries": rows}, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(rows, indent=2))


if __name__ == "__main__":
    main()
