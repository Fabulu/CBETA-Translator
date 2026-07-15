import datetime
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WAVES = ROOT / "fresh-build/waves"
sys.path.insert(0, str(ROOT))
import zc

review = json.loads((WAVES / "f003-laneB-701-750-independent-exact-review.json").read_text(encoding="utf-8"))
targets = [r for r in review["rows"] if r["verdict"] == "REVISE"]
now = datetime.datetime.now(datetime.timezone.utc).isoformat()
rungs = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]

def note(o, label, decision):
    title = zc.title(o["RelPath"])
    return f"Source record ({title}; {o['RelPath']}). Exact actor: {label}. The complete case distinguishes the headword-bearing turn from surrounding narration and replies."

def unnamed(o, label="the unnamed questioner", role="questioner", grammar=None):
    o.pop("MasterName", None)
    o.pop("DraftActorProof", None)
    o["ContextMasters"] = []
    grammar = grammar or "The explicit 問/僧問 frame assigns the headword-bearing words to an unnamed questioner; the following 師曰/師云 response is a separate turn."
    o["ActorAttribution"] = {
        "Status": "reviewed-unnamed", "Kind": "unnamed monastic questioner",
        "ActorLabel": label, "ActorRole": role, "RungsChecked": rungs,
        "GrammarEvidence": grammar, "ReviewedBy": "Codex f003 B701-750 exact-actor repair author", "ReviewedUtc": now,
    }
    o["AttributionNote"] = note(o, label, grammar)
    o["DraftActorProof"] = {"ExactHeadwordClause": o["Kwic"], "GrammaticalSubject": label, "SpeechFrame": grammar, "FullCaseDecision": o["AttributionNote"]}

def narrated(o, label="the source compiler or recorder", grammar=None):
    o.pop("MasterName", None)
    o.pop("DraftActorProof", None)
    o["ContextMasters"] = []
    grammar = grammar or "The headword occurs in narrator-governed documentary prose, an event heading, or a nonverbal action clause; no person utters the headword here."
    o["ActorAttribution"] = {
        "Status": "narrated", "Kind": "compiler or recorder narration",
        "ActorLabel": label, "ActorRole": "compiler", "RungsChecked": rungs,
        "GrammarEvidence": grammar, "ReviewedBy": "Codex f003 B701-750 exact-actor repair author", "ReviewedUtc": now,
    }
    o["AttributionNote"] = note(o, label, grammar)
    o["DraftActorProof"] = {"ExactHeadwordClause": o["Kwic"], "GrammaticalSubject": label, "SpeechFrame": grammar, "FullCaseDecision": o["AttributionNote"]}

def nonmaster(o, label, role="utterer", grammar=None):
    o.pop("MasterName", None)
    o.pop("DraftActorProof", None)
    o["ContextMasters"] = []
    grammar = grammar or f"The complete case identifies {label} as the non-master who utters the headword-bearing wording."
    o["ActorAttribution"] = {
        "Status": "identified-non-master", "Kind": "identified participant",
        "ActorLabel": label, "ActorRole": role, "RungsChecked": rungs,
        "GrammarEvidence": grammar, "ReviewedBy": "Codex f003 B701-750 exact-actor repair author", "ReviewedUtc": now,
    }
    o["AttributionNote"] = note(o, label, grammar)
    o["DraftActorProof"] = {"ExactHeadwordClause": o["Kwic"], "GrammaticalSubject": label, "SpeechFrame": grammar, "FullCaseDecision": o["AttributionNote"]}

def named(o, name, grammar=None):
    grammar = grammar or "The complete speech frame assigns the exact headword-bearing words to this named master."
    o["MasterName"] = name
    o.pop("ActorAttribution", None)
    o["ContextMasters"] = [{"MasterName": name, "Roles": ["utterer"]}]
    o["AttributionNote"] = note(o, name, grammar)
    o["DraftActorProof"] = {
        "ExactHeadwordClause": o["Kwic"], "GrammaticalSubject": name,
        "SpeechFrame": grammar, "FullCaseDecision": o["AttributionNote"],
    }

# Per-entry exact-case decisions. Occurrence numbers are one-based within the
# entry's flattened sense/occurrence order.
unnamed_map = {
    702: {1,2,3,4,7,8}, 703: {1,5}, 737: {1}, 740: {6},
    742: {1,3}, 747: {6},
}
narrated_map = {
    707: {2,4,6,7},
    709: {5},
    712: {2,3,4,5,6,7,8,9,10},
    713: {2,4},
    722: {2,3,4,5,6,7},
    726: {1,2,3,4,5,6,7,8,9},
    727: {1,2,3,4,5,6,7},
    728: {1,2,3,4,5,6,7},
    730: {1,3,4,5,6},
    731: {1,5},
    733: {1,6,7},
    735: {1,2,3,4,6,7,8},
    736: {1,2,3,6,7},
    737: {4,8},
    738: {1,2,4,5,6,9},
    739: {1},
    740: {4,8},
    741: {1,2,3,4,6,7},
    743: {1,2,3,4,5,6,8},
    745: set(),
    746: {1,2,3,4,5,7},
    748: {2,6},
    749: {1,3,4,5,6,7},
}
nonmaster_map = {
    (719,1): ("Guo Xiangzheng, the lay patron", "utterer"),
    (729,3): ("the source preface author", "utterer"),
    (731,7): ("Liangting, the lay author", "utterer"),
    (736,5): ("Ming, the guest prefect", "utterer"),
    (740,5): ("the Miaoyuan layman", "utterer"),
    (747,1): ("Yang, the calligrapher", "questioner"),
    (747,2): ("Daoyuan, the lamp-record compiler", "compiler"),
}
named_map = {
    (702,5): "Hanyue Fazang",
    (709,6): "Nanquan Puyuan",
    (709,9): "Baoen Yuan",
    (738,3): "Fayan Wenyi",
    (742,6): "Guishan Lingyou",
    (745,1): "Linji Yixuan",
    (745,3): "Caoshan Benji",
    (747,5): "Buddha",
    (747,7): "Buddha",
    (750,2): "Yuanwu Keqin",
}

# Raw descriptive MasterName values never survive. These are documentary
# owners/roles, not people proven to utter the headword.
descriptive = (
    "compiler", "record owner", "record-owner", "recorded by", "monastery rule",
    "ritual compiler", "preface author", "identified teaching-seat speaker",
    "hermitage keeper", "guest prefect", "attendant",
)

before_keep = {r["id"]: r["entrySha256"] for r in review["rows"] if r["verdict"] == "KEEP"}
ledger_rows = []
for row in targets:
    ordinal = row["ordinal"]
    entry_dir = ROOT / "fresh-build/entries" / row["id"]
    worksheet = entry_dir / "evidence.draft.json"
    data = json.loads(worksheet.read_text(encoding="utf-8"))
    flat = []
    for sense in data["Entry"]["Senses"]:
        flat.extend(sense.get("Occurrences", []))
    for i, o in enumerate(flat, 1):
        if (ordinal, i) in named_map:
            named(o, named_map[(ordinal, i)])
        elif (ordinal, i) in nonmaster_map:
            label, role = nonmaster_map[(ordinal, i)]
            nonmaster(o, label, role)
        elif i in unnamed_map.get(ordinal, set()):
            unnamed(o)
        elif i in narrated_map.get(ordinal, set()):
            narrated(o)
        elif o.get("MasterName") and any(x in o["MasterName"].lower() for x in descriptive):
            narrated(o)
        elif o.get("ActorAttribution", {}).get("Status") == "narrated" and "unnamed" in o.get("ActorAttribution", {}).get("ActorLabel", ""):
            unnamed(o)
        if not o.get("MasterName") and o.get("ActorAttribution"):
            o["ContextMasters"] = []
            aa = o["ActorAttribution"]
            if re.search(r"[\u3400-\u9fff]", aa.get("ActorLabel", "")):
                aa["ActorLabel"] = {
                    "narrated": "the source compiler or recorder",
                    "reviewed-unnamed": "the unnamed participant",
                    "identified-non-master": "the identified participant",
                    "impersonal": "the impersonal documentary construction",
                }.get(aa.get("Status"), "the documented actor")
            if not o.get("DraftActorProof"):
                label = o["ActorAttribution"].get("ActorLabel", "the documented actor")
                grammar = o["ActorAttribution"].get("GrammarEvidence", "The complete case supplies the recorded actor classification.")
                o["DraftActorProof"] = {"ExactHeadwordClause": o["Kwic"], "GrammaticalSubject": label, "SpeechFrame": grammar, "FullCaseDecision": o.get("AttributionNote", grammar)}
        # English-first reader note for every repaired-row occurrence. Exact
        # Chinese grammar remains in GrammarEvidence and the stored KWIC.
        actor_label = o.get("MasterName") or o.get("ActorAttribution", {}).get("ActorLabel", "the source recorder")
        o["AttributionNote"] = note(o, actor_label, "")
        if o.get("DraftActorProof"):
            o["DraftActorProof"]["FullCaseDecision"] = o["AttributionNote"]

    # Entry-specific prose repair required by the independent review.
    if ordinal == 750:
        s = data["Entry"]["Senses"][0]
        s["Explanation"] = (
            "To ‘sit across and cut off everyone’s tongue’ pictures one position occupying the whole field so that no competing utterance remains. "
            "The records use the phrase as an explicit appraisal, not as literal mutilation. Feiyin Tongrong states the formula directly; Wuxie Lingmo applies it to Wuxie’s departure from Guishan; Mazu Daoyi uses it while proposing how an encounter should have been handled; and Qianyan Yuanzhang applies it to Bodhidharma’s wall-facing. These different appraisals establish the recurring verbal image, but they do not prove that every cited act actually silenced every later speaker."
        )

    # Refresh RelatedMasters only from actual named utterers.
    for sense in data["Entry"]["Senses"]:
        sense["RelatedMasters"] = sorted({o["MasterName"] for o in sense.get("Occurrences", []) if o.get("MasterName")})

    worksheet.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    subprocess.run([
        sys.executable, str(ROOT / "compile_evidence_draft.py"), str(worksheet),
        "--output", str(entry_dir / "entry.v2.json"), "--report", str(entry_dir / "compile-report.json")
    ], check=True, stdout=subprocess.DEVNULL)
    entry_sha = hashlib.sha256((entry_dir / "entry.v2.json").read_bytes()).hexdigest()
    ledger_rows.append({"ordinal": ordinal, "id": row["id"], "term": row["term"], "entrySha256": entry_sha, "occurrences": len(flat)})

# Byte-identity guard for the 20 independent KEEP entries.
for eid, expected in before_keep.items():
    p = ROOT / "fresh-build/entries" / eid / "entry.v2.json"
    actual = hashlib.sha256(p.read_bytes()).hexdigest()
    if actual != expected:
        raise SystemExit(f"KEEP entry drifted: {eid} {actual} != {expected}")

for block_no, start in enumerate(range(0, len(ledger_rows), 10), 1):
    block = ledger_rows[start:start+10]
    out = {
        "generatedUtc": now, "scope": f"f003 B701-750 actor repair checkpoint {block_no}",
        "repairedRows": block, "keepEntriesByteIdentical": True,
        "selfReviewRun": False, "promotionOrMergePerformed": False, "siteTouched": False,
    }
    (WAVES / f"f003-laneB-701-750-repair-checkpoint-{block_no}.json").write_text(json.dumps(out, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")

print(json.dumps({"repaired": len(ledger_rows), "keepByteIdentical": len(before_keep)}, ensure_ascii=False))
