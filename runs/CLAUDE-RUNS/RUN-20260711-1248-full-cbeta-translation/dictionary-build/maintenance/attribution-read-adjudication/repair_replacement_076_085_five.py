#!/usr/bin/env python3
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TERMS = ROOT / "terms"
FRESH = ROOT / "fresh-build" / "entries"
OUT = Path(__file__).with_name("independent-review-1-3-076-085-replacement-author-repair-ledger.json")


def path_for(tid):
    p = TERMS / tid / "entry.v2.json"
    return p if p.exists() else FRESH / tid / "entry.v2.json"


def digest(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()


def main():
    changes = []

    # 香合: the complete case says Qingliang lifts the box; encode him with the
    # closed contextual role and leave the finer narrated action in prose.
    p = path_for("t_916ec389a07d"); d = json.loads(p.read_text())
    before = digest(p)
    o = d["Senses"][0]["Occurrences"][3]
    o["ContextMasters"] = [{"MasterName": "Qingliang Taiqin", "Roles": ["person-described", "section-subject"]}]
    p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n")
    changes.append({"id": d["Id"], "term": d["SourceTerm"], "path": str(p), "beforeSha256": before,
                    "afterSha256": digest(p), "repair": "Qingliang's narrated lifting of the incense box retained; forbidden action-performer role replaced by closed person-described role."})

    # 言前: the complete transmission unit is compiler narration about Fengxue's
    # matter being brought to Shoukuo, not an actorless label.
    p = path_for("t_961b548d6462"); d = json.loads(p.read_text()); before = digest(p)
    o = d["Senses"][0]["Occurrences"][3]
    o["ActorAttribution"] = {
        "Status": "narrated", "Kind": "compiler narration",
        "ActorLabel": "lamp-record compiler", "ActorRole": "compiler",
        "GrammarEvidence": "The complete transmission unit narrates in the third person that the matter from Fengxue was presented to Shoukuo; 言前 is in that compiler-owned narration, not in a quoted turn.",
        "ReviewedBy": "Codex literal full-case repair", "ReviewedUtc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")}
    p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n")
    changes.append({"id": d["Id"], "term": d["SourceTerm"], "path": str(p), "beforeSha256": before,
                    "afterSha256": digest(p), "repair": "Reclassified third-person transmission sentence from impersonal to compiler-narrated; Fengxue and Shoukuo context preserved."})

    # 臘八: Feiyin directly utters the token; only remove the duplicated role.
    p = path_for("t_a2c5b2af7b10"); d = json.loads(p.read_text()); before = digest(p)
    o = d["Senses"][0]["Occurrences"][6]
    for cm in o.get("ContextMasters", []):
        cm["Roles"] = list(dict.fromkeys(cm.get("Roles", [])))
    p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n")
    changes.append({"id": d["Id"], "term": d["SourceTerm"], "path": str(p), "beforeSha256": before,
                    "afterSha256": digest(p), "repair": "Deduplicated Feiyin Tongrong's utterer role after confirming the complete hall exchange."})

    # 茫然: the heading immediately governing the complete 機緣 case is the
    # attached record of 明道正覺䒢溪森 = roster-canonical Maoxi Xingsen.
    p = path_for("t_bb3cdb68e388"); d = json.loads(p.read_text()); before = digest(p)
    o = d["Senses"][0]["Occurrences"][3]
    o["ContextMasters"] = [{"MasterName": "Maoxi Xingsen", "Roles": ["respondent", "section-subject"]}]
    p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n")
    changes.append({"id": d["Id"], "term": d["SourceTerm"], "path": str(p), "beforeSha256": before,
                    "afterSha256": digest(p), "repair": "Resolved 師 in the complete 機緣 case from the governing 御選明道正覺䒢溪森禪師語錄附 heading to roster-canonical Maoxi Xingsen; the monk remains the headword actor."})

    # 佛手: grammar-only correction; evidence-bound interpretation unchanged.
    p = path_for("t_bf467ac18ec0"); d = json.loads(p.read_text()); before = digest(p)
    s = d["Senses"][0]
    old = "Huanglong Huinan's recurring question, 'How is my hand like the Buddha's hand?', one of his Three Barriers."
    new = "Huanglong Huinan's recurring question, 'How is my hand like the Buddha's hand?', is one of his Three Barriers."
    if old not in s["Explanation"]:
        raise RuntimeError("expected 佛手 sentence not found")
    s["Explanation"] = s["Explanation"].replace(old, new)
    p.write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n")
    changes.append({"id": d["Id"], "term": d["SourceTerm"], "path": str(p), "beforeSha256": before,
                    "afterSha256": digest(p), "repair": "Completed the opening English sentence; no semantic or evidence change."})

    OUT.write_text(json.dumps({"selfReview": False, "method": "author repair after independent full-unit review; each cited complete case re-read before editing", "changes": changes}, ensure_ascii=False, indent=2) + "\n")


if __name__ == "__main__":
    main()
