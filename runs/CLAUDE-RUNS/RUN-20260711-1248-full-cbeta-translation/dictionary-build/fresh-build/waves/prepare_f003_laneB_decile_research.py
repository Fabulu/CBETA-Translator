import argparse
import datetime
import hashlib
import json
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
sys.path.insert(0, ROOT)
import zc

ap = argparse.ArgumentParser()
ap.add_argument("start", type=int)
a = ap.parse_args()
assert 701 <= a.start <= 791 and (a.start - 701) % 10 == 0

PREFLIGHT = "fresh-build/waves/f003-laneB-701-800-preflight.json"
packet = json.load(open(os.path.join(ROOT, PREFLIGHT), encoding="utf-8"))
offset = a.start - 701
rows = []

for ordinal, item in zip(range(a.start, a.start + 10), packet["entries"][offset:offset + 10]):
    chosen = []
    works = set()
    for candidate in item["candidateWorks"]:
        if candidate["workId"] in works:
            continue
        lead = next(
            (window for window in candidate.get("windows", []) if item["term"] in window["window"]),
            None,
        )
        if not lead:
            continue
        found = zc.find(candidate["RelPath"], item["term"], ctx=180)
        match = next(
            (hit for hit in found if hit["fromLb"] == lead.get("fromLb")),
            found[0] if found else None,
        )
        if not match:
            continue
        verification = zc.verify(candidate["RelPath"], match["window"])
        if not verification.get("ok"):
            continue
        works.add(candidate["workId"])
        chosen.append({
            "workId": candidate["workId"],
            "RelPath": candidate["RelPath"],
            "title": zc.title(candidate["RelPath"]),
            "fromLb": verification["fromLb"],
            "toLb": verification["toLb"],
            "expandedWindow": match["window"],
            "zcVerifyOk": True,
            "headingContext": zc.head(candidate["RelPath"], verification["fromLb"]),
        })
        if len(chosen) >= max(item["evidenceFloor"], 4):
            break
    rows.append({
        "ordinal": ordinal,
        "id": item["id"],
        "term": item["term"],
        "hits": item["hits"],
        "files": item["files"],
        "works": item["works"],
        "evidenceFloor": item["evidenceFloor"],
        "selectedDistinctWorks": len(chosen),
        "workIdUnique": len(works) == len(chosen),
        "allExpandedWindowsVerified": all(x["zcVerifyOk"] for x in chosen),
        "witnesses": chosen,
    })

out = {
    "generatedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "wave": "f003",
    "lane": "B",
    "ordinals": [a.start, a.start + 9],
    "corpusBaselineSha256": packet["corpusBaselineSha256"],
    "sourcePreflight": PREFLIGHT,
    "formalGateRun": False,
    "siteTouched": False,
    "state": "verified-research-ready-for-full-turn-attribution",
    "entries": rows,
}

target = os.path.join(
    ROOT,
    f"fresh-build/waves/f003-laneB-{a.start:03d}-{a.start + 9:03d}-research-ledger.json",
)
with open(target, "w", encoding="utf-8") as handle:
    json.dump(out, handle, ensure_ascii=False, indent=2)
    handle.write("\n")

print(json.dumps({
    "output": os.path.relpath(target, ROOT),
    "entries": len(rows),
    "witnesses": sum(len(x["witnesses"]) for x in rows),
    "underFloor": [x["ordinal"] for x in rows if x["selectedDistinctWorks"] < x["evidenceFloor"]],
    "sha256": hashlib.sha256(open(target, "rb").read()).hexdigest(),
}))
