#!/usr/bin/env python3
"""Apply the six hash-bound metadata repairs from independent residual review."""
from __future__ import annotations

import hashlib
import json
import os
from datetime import datetime, timezone
from pathlib import Path

BASE = Path(__file__).resolve().parents[1]
ENTRIES = BASE / "fresh-build" / "entries"
WAVES = BASE / "fresh-build" / "waves"
NOW = datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")

REVISE = {
    "t_ff50c6974a36": ("五位", "e8a9f81e20ec4b4473abd6071550363f589e6297113497a23fa49f6f0aba897c"),
    "t_9199b9a31645": ("盡大地", "3c03f0d5b9a885144301312be7b32b882f41f8577acf7e711530fd0ecc2b5bfa"),
    "t_1274824e797b": ("普請", "64611ab9cd84c6f0f42e4f30bfa9a446555465981a58c48012c8296dcc44cc44"),
    "t_00e8627f3a48": ("歷歷", "12188ab68b7cc6b648ff0ad6f1fa747ac0f4149bf9ed5d129d1445eab2b599d0"),
    "t_c968268a64d1": ("心印", "6706fd2820e44acb36ac17a712a356f3ccaa86006d6a2013bef4d39cea406d8e"),
    "t_395ae8fd7f32": ("無住", "5173aeeae9c50e3ea00154ebbb53c0efe3c92f5946d4127a62384017a30d59d4"),
}
LOCKED_KEEP = {
    "t_7887dc8d449f": "3d8fb7a2f1833f80328ad7de9f1997a5bcbe0d17f530559aea16493f077f7437",
    "t_3972185a2e25": "1751c322ef55aa844051299260f282895da5537ff078800fbe764cccfe26a1ff",
    "t_d926adb80feb": "a6ad81444a8b6bfae136a356dc0d1da7d4d4c78e9dd3d9945154a68e44141998",
    "t_b191c4fa2e9f": "5f881d5ba0d070cb536dd77e8aac1c4d087bc6293c32281672f1fd8d5929a337",
    "t_52fdda90e9ab": "c78cd8b56f0edd1c57514c2a975b9f16323406e30320700d6e60ca0d663b63df",
}


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def atomic_json(path: Path, value: object) -> None:
    raw = (json.dumps(value, ensure_ascii=False, indent=2) + "\n").encode("utf-8")
    tmp = path.with_suffix(path.suffix + ".tmp-round2")
    tmp.write_bytes(raw)
    os.replace(tmp, path)


for entry_id, expected in LOCKED_KEEP.items():
    actual = sha(ENTRIES / entry_id / "entry.v2.json")
    assert actual == expected, f"locked KEEP changed before repair: {entry_id} {actual}"

entries = {}
for entry_id, (term, reviewed_hash) in REVISE.items():
    path = ENTRIES / entry_id / "entry.v2.json"
    assert sha(path) == reviewed_hash, f"review hash drift: {term}"
    entry = json.loads(path.read_text(encoding="utf-8"))
    assert entry["Id"] == entry_id and entry["SourceTerm"] == term
    entries[term] = entry


def occurrence_sources(sense):
    return list(dict.fromkeys(o["RelPath"] for o in sense.get("Occurrences", [])))


# 五位: synchronize the displayed inventory with retained evidence and count
# distinct works, not multiple rows/files from the same work.
s1, s2 = entries["五位"]["Senses"]
s1["SourceTexts"] = occurrence_sources(s1)
s2["SourceTexts"] = occurrence_sources(s2)
s2["Validation"] = "multi-source"
s2["Note"] = (
    "This sense denotes five-member sequences other than the named Caodong Five Ranks. "
    "Its three exact anchors represent two independent works: the Patriarchs' Hall Collection "
    "(work:B25n0144) and Yongming Yanshou's Source-Mirror Record (work:T48n2016); the two Yanshou rows count once."
)

# 盡大地: add the two retained occurrence sources and state the work-identity
# basis honestly; the Yuanwu witnesses do not carry the validation by themselves.
s = entries["盡大地"]["Senses"][0]
s["SourceTexts"] = occurrence_sources(s)
s["Validation"] = "multi-source"
s["Note"] = (
    "The frozen corpus has 2,318 exact hits in 341 files representing 336 works. Eight anchors come from seven "
    "explicit work identities: work:T48n2003, work:X79n1557, work:wudeng-quanshu, work:J27nB193, work:X64n1260, "
    "work:B27n0152, and work:T47n1997. The two Yuanwu witnesses repeat a related formula and are not needed to "
    "manufacture independence: Xuefeng, Shangfeng Huihe, Yinyuan Longqi, Yulin Tongxiu, and Yuanwu deployments "
    "already preserve cross-work spread. Occurrences only nested inside a longer landscape phrase were excluded."
)

# 普請 is one corpus-wide institutional sense, never a custom/master key.
entries["普請"]["Senses"][0]["SenseKey"] = None

# 歷歷's repaired packet contains eight, not seven, exact witnesses.
entries["歷歷"]["Senses"][0]["Note"] = (
    "The frozen corpus has 1,664 exact hits in 338 files representing 331 works. Eight anchors cover a listener, "
    "surroundings, verse, instruction, named clarity formulas, and critical use across independent works."
)

# The bestowed title is attested in one work and therefore provisional.
entries["心印"]["Senses"][1]["Validation"] = "provisional"

# Baotang Wuzhu's name is a corpus-wide proper-name collision, not a custom
# master-specific sense key. Two distinct work IDs carry parallel biography
# wording, so the semantic support remains conservatively provisional.
s = entries["無住"]["Senses"][1]
s["SenseKey"] = None
s["Validation"] = "provisional"
s["SourceTexts"] = occurrence_sources(s)
s["Note"] = (
    "This person-name sense prevents Baotang Wuzhu from being folded into the lexical phrase. The headings occur "
    "in two distinct manifest works, work:wudeng-yantong and work:X80n1565, but they preserve the same biography "
    "formula in closely related lamp compilations; the sense therefore remains provisional rather than treating "
    "storage or work count alone as independent semantic confirmation."
)

rows = []
for entry_id, (term, reviewed_hash) in REVISE.items():
    path = ENTRIES / entry_id / "entry.v2.json"
    atomic_json(path, entries[term])
    current = sha(path)
    with (ENTRIES / entry_id / "WORK.md").open("a", encoding="utf-8") as fh:
        fh.write(
            f"\n## Independent residual repair round 2 — {NOW}\n"
            "- Synchronized source inventory, work identity, validation labels, sense keys, and stated evidence depth exactly as independently reviewed.\n"
            f"- Reviewed SHA-256: `{reviewed_hash}`\n"
            f"- Repaired SHA-256: `{current}`\n"
        )
    rows.append({
        "id": entry_id,
        "term": term,
        "reviewedSha256": reviewed_hash,
        "entrySha256": current,
        "path": f"fresh-build/entries/{entry_id}/entry.v2.json",
        "status": "repair-ready-for-independent-rereview",
    })

for entry_id, expected in LOCKED_KEEP.items():
    actual = sha(ENTRIES / entry_id / "entry.v2.json")
    assert actual == expected, f"locked KEEP changed during repair: {entry_id} {actual}"

atomic_json(WAVES / "f001-laneC-legacy-residual-repairs-round2.json", {
    "schemaVersion": 1,
    "wave": "f001",
    "lane": "C",
    "writtenUtc": NOW,
    "policy": "Six reviewed REVISE hashes only; five independent KEEP hashes locked byte-for-byte; no promotion.",
    "entries": rows,
    "lockedKeepHashes": LOCKED_KEEP,
})
print(json.dumps({"repaired": len(rows), "lockedKeepUnchanged": len(LOCKED_KEEP)}, ensure_ascii=False))
