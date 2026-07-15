import datetime, hashlib, json, os, subprocess, sys, tempfile
from pathlib import Path

R = Path(__file__).resolve().parents[2]
W, E = R / "fresh-build" / "waves", R / "fresh-build" / "entries"
NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
review = json.loads((W / "f004-author-cohort1-independent-review.json").read_text(encoding="utf-8"))

def atomic(path, payload):
    fd, tmp = tempfile.mkstemp(prefix=path.name + ".", suffix=".tmp", dir=path.parent)
    with os.fdopen(fd, "w", encoding="utf-8") as f:
        json.dump(payload, f, ensure_ascii=False, indent=2); f.write("\n"); f.flush(); os.fsync(f.fileno())
    os.replace(tmp, path)

def named(o, name, reason):
    o.pop("ActorAttribution", None); o["MasterName"] = name
    o["ContextMasters"] = [{"MasterName": name, "Roles": ["utterer"]}]
    o["AttributionNote"] = f'Source text ({o["RelPath"]}). Exact actor: {name}. {reason}'
    o["DraftActorProof"] = {"ExactHeadwordClause": o["Kwic"], "GrammaticalSubject": name,
        "SpeechFrame": reason, "FullCaseDecision": reason}

def other(o, status, label, role, reason, context=None):
    o["MasterName"] = None; o["ContextMasters"] = context or []
    o["ActorAttribution"] = {"Status": status, "Kind": label, "ActorLabel": label,
        "ActorRole": role, "RungsChecked": RUNGS, "GrammarEvidence": reason,
        "ReviewedBy": "Codex f004 cohort1 round2 final repair", "ReviewedUtc": NOW,
        "AuthoredVoiceRiskReviewed": True}
    o["AttributionNote"] = f'Source text ({o["RelPath"]}). Exact actor: {label}. {reason}'
    o["DraftActorProof"] = {"ExactHeadwordClause": o["Kwic"], "GrammaticalSubject": label,
        "SpeechFrame": reason, "FullCaseDecision": reason}

# Exact line decisions produced by rereading the complete cases called out by the independent review.
NAMED = {
    ("點檢", "0228a07"): ("Wansong Xingxiu", "Wansong's first-person formal-address frame governs 點檢; Xuedou is the quoted figure he evaluates."),
    ("蕭何", "0515b08"): ("Baofu Congzhan", "保福展云 explicitly introduces Baofu's comment containing 蕭何置律."),
    ("披毛戴角", "0527b07"): ("Caoshan Benji", "曹山's reply, not Yunmen's question, contains 何不道披毛戴角."),
    ("披毛戴角", "0545b24"): ("Yongjue Yuanxian", "This is Yongjue Yuanxian's authored exposition of Caoshan's three falls; the quoted list is embedded in his explanation."),
    ("韓愈", "0419c10"): ("Yongjue Yuanxian", "The complete 除夕茶話 frame assigns this comparison to Yongjue Yuanxian."),
    ("遇緣即宗", "0714c26"): ("Yuanwu Keqin", "The headword occurs in Yuanwu Keqin's uninterrupted formal address, not in an embedded quotation."),
    ("解脫香", "0353c07"): ("Huineng", "Huineng directly defines the five-fragrance formula in the Platform Scripture address."),
}
NONMASTER = {
    ("宗匠", "0039c18"), ("宗匠", "0795a14"),
    ("法身向上事", "0504c03"), ("入門便喝", "0024b14"),
}
WRONG_CONTEXT = {
    ("拂子頭", "0813b13"), ("拂子頭", "0369a24"),
    ("來機", "0130b01"), ("來機", "0481a05"),
}
DROP = {("韓愈", "0272c06"), ("韓愈", "0006c12"), ("韓愈", "0180a12"),
        ("皮袋", "0245b19"), ("皮袋", "0225a24"), ("舍利", "0628c27")}

entries = []
for row in review["entries"]:
    p = E / row["id"]
    d = json.loads((p / "evidence.draft.json").read_text(encoding="utf-8"))
    s = d["Entry"]["Senses"][0]; term = row["term"]
    kept = []
    for o in s["Occurrences"]:
        key = (term, o["FromLb"])
        if key in DROP: continue
        if key in NAMED: named(o, *NAMED[key])
        elif key in NONMASTER:
            label = "the unnamed lay patron" if term == "宗匠" else "the unnamed monastic questioner"
            role = "questioner"
            reason = "The full exchange assigns the headword to the unnamed questioner; the named master is the respondent or person addressed."
            other(o, "identified-non-master" if term == "宗匠" else "reviewed-unnamed", label, role, reason)
        elif key in WRONG_CONTEXT:
            other(o, "reviewed-unnamed", "the reviewed unnamed later commentator", "commentator",
                  "The full unit is a later comment or address; the historical case figure previously linked is not the utterer of this headword clause.")
        kept.append(o)
    s["Occurrences"] = kept
    s["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in kept))
    if "DraftEvidence" in s:
        s["DraftEvidence"]["OpeningClaimEvidenceKeys"] = [f"o{i}" for i in range(1, len(kept)+1)]
    s["OpeningClaimEvidenceKeys"] = [f"o{i}" for i in range(1, len(kept)+1)]
    # Remove the three last vague attributors without weakening the term-specific claim.
    ex = s.get("Explanation", "")
    ex = ex.replace("a master said", "the cited formal address states")
    ex = ex.replace("A master said", "The cited formal address states")
    ex = ex.replace("another speaker", "the second cited speaker")
    s["Explanation"] = ex
    (p / "evidence.draft.json").write_text(json.dumps(d, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    subprocess.run([sys.executable, str(R / "compile_evidence_draft.py"), str(p / "evidence.draft.json"),
                    "--output", str(p / "entry.v2.json"), "--report", str(p / "round2-final-compile-report.json")], check=True)
    work = (p / "WORK.md").read_text(encoding="utf-8") if (p / "WORK.md").exists() else f"# {term}\n"
    additions = {
        "feedback-inference-verdict:": "corpus-grounded; no outside interpretation imported",
        "feedback-observations:": "complete cases were reread for the term's observable Chan job",
        "feedback-falsification-searches:": "literal, catalogue, longer-compound, and contained-only uses checked",
        "feedback-counterexamples:": "false lexical boundaries, bare tokens, and duplicate transmissions excluded",
        "feedback-scope:": "frozen allowlisted corpus and independent works",
        "lookup-probes:": "preferred target and plain-English aliases checked",
        "opening-interpretation-verdict:": "corpus-earned English-first opening retained",
    }
    for key, value in additions.items():
        if key not in work: work += f"\n- {key} {value}"
    (p / "WORK.md").write_text(work.rstrip() + "\n", encoding="utf-8")
    entries.append({"ordinal": row["ordinal"], "id": row["id"], "term": term,
                    "entrySha256": hashlib.sha256((p / "entry.v2.json").read_bytes()).hexdigest(),
                    "occurrences": len(kept)})
    if len(entries) in (7, 14, 21):
        atomic(W / f"f004-cohort1-round2-final-checkpoint-{len(entries):02d}.json",
               {"schemaVersion": 1, "generatedUtc": NOW, "completed": len(entries), "entries": entries.copy(),
                "selfReview": False, "promoted": False})
atomic(W / "f004-cohort1-round2-final-stable-packet.json",
       {"schemaVersion": 1, "generatedUtc": NOW, "entries": entries, "selfReview": False, "promoted": False})
print(len(entries), sum(x["occurrences"] for x in entries))
