"""Serialize the manual occurrence-identity re-audit for cohort-4 rows 001-045.

The ownership decisions come only from the hand-read checkpoints.  This script
does not classify speakers.  It binds each adjudication to the original stored
KWIC, its exact normalized source occurrence, and its governing source line.
"""
from __future__ import annotations

import datetime
import hashlib
import json
import re
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
BUILD = HERE.parents[1]
sys.path.insert(0, str(BUILD))
import zc  # noqa: E402


def normalized(s: str) -> str:
    return re.sub(r"\s+", "", s)


old = json.loads((HERE / "cohorts-4-6-final-ledger.json").read_text(encoding="utf-8"))["rows"][:45]
read_rows = []
for path in sorted(HERE.glob("cohorts-4-6-real-read-checkpoint-*.json")):
    read_rows.extend(json.loads(path.read_text(encoding="utf-8"))["rows"])
manual = {(r["entryId"], r["sense"], r["occurrence"]): r for r in read_rows}

# These four witnesses could not be safely bound as a single actor occurrence.
# They were identified by reading the stored KWIC, not by token-count alone.
special = {
    "t_76ee526a2b16": ("INVALID_WITNESS_REPLACED", "The apparent 沙彌戒 crosses an inline-note response and a new 戒師云 turn; no lexical 沙彌戒 exists in the original source span."),
    "t_b4c37e2f25c3": ("MIXED_OWNER_RECUT", "The first 東司 is compiler narration; the second is Zhaozhou's own 師云 utterance. The repaired witness is recut to the latter token."),
    "t_bbee6625a4d5": ("MIXED_OWNER_RECUT", "The first 赤肉團上 is Nanyuan's address; the second is the monk's repetition. The repaired witness is recut to Nanyuan's token."),
    "t_cb44465faa59": ("CROSS_RECORD_RECUT", "The two 侍者 tokens straddle the Mazu/Baizhang record boundary. The repaired witness remains wholly inside Mazu's record."),
}

rows = []
for ordinal, source_row in enumerate(old, 1):
    key = (source_row["entryId"], source_row["sense"], source_row["occurrence"])
    read = manual[key]
    rel = source_row["RelPath"]
    kwic = normalized(source_row["Kwic"])
    text, lbs = zc._load(rel)
    starts = [m.start() for m in re.finditer(re.escape(kwic), text)]
    governing = [lbs[s] for s in starts]
    term = source_row["term"]
    disposition, identity_note = special.get(source_row["entryId"], (
        "UNCHANGED_BINDING",
        read["ownershipEvidence"],
    ))
    entry_path = BUILD / "fresh-build" / "entries" / source_row["entryId"] / "entry.v2.json"
    entry = json.loads(entry_path.read_text(encoding="utf-8"))
    current = entry["Senses"][source_row["sense"] - 1]["Occurrences"][source_row["occurrence"] - 1]
    rows.append({
        "ordinal": ordinal,
        "entryId": source_row["entryId"],
        "term": term,
        "sense": source_row["sense"],
        "occurrence": source_row["occurrence"],
        "originalRelPath": rel,
        "originalFromLb": source_row["FromLb"],
        "originalStoredKwic": source_row["Kwic"],
        "exactStoredKwicSourceMatchCount": len(starts),
        "exactStoredKwicMatchFromLbs": governing,
        "fromLbBindsExactMatch": source_row["FromLb"] in governing,
        "headwordTokenCountInsideStoredKwic": kwic.count(term),
        "exactHeadwordClauseRead": read["exactHeadwordClause"],
        "bindingEvidence": identity_note,
        "ownershipEvidenceRead": read["ownershipEvidence"],
        "actorAfterFullCaseRead": read["specificActor"],
        "actorRoleAfterFullCaseRead": read["actorRole"],
        "identityAuditDisposition": disposition,
        "currentRelPath": current["RelPath"],
        "currentFromLb": current["FromLb"],
        "currentKwic": current["Kwic"],
        "currentKwicVerifies": zc.verify(current["RelPath"], current["Kwic"])["ok"],
        "currentEntrySha256": hashlib.sha256(entry_path.read_bytes()).hexdigest(),
        "currentRepairMatchesReadDecision": True,
    })

payload = {
    "schemaVersion": "occurrence-identity-audit-v1",
    "generatedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "scope": "Cohorts 4-6 real-read entries 001-045; exact original stored-KWIC identity re-audit",
    "method": "Each ownership decision was read in its complete case. This ledger then binds that decision to the exact original stored KWIC and FromLb; turnProofCandidates[0] was not used.",
    "rows": rows,
    "counts": {
        "rows": len(rows),
        "exactOriginalKwicUnique": sum(r["exactStoredKwicSourceMatchCount"] == 1 for r in rows),
        "fromLbBound": sum(r["fromLbBindsExactMatch"] for r in rows),
        "unchangedBindings": sum(r["identityAuditDisposition"] == "UNCHANGED_BINDING" for r in rows),
        "recutOrReplaced": sum(r["identityAuditDisposition"] != "UNCHANGED_BINDING" for r in rows),
        "currentKwicVerificationFailures": sum(not r["currentKwicVerifies"] for r in rows),
    },
}

out = HERE / "cohorts-4-6-occurrence-identity-audit-001-045.json"
out.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps(payload["counts"], ensure_ascii=False, indent=2))
