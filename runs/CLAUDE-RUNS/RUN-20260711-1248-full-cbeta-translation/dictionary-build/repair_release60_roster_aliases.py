#!/usr/bin/env python3
"""Promote the nine release-60 utterers newly resolved by the exact roster gate."""
from __future__ import annotations

import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

from compile_evidence_draft import compile_draft

ROOT = Path(__file__).resolve().parent
FRESH = ROOT / "fresh-build" / "entries"
OUT = ROOT / "maintenance" / "iriya-release60-nine-alias-promotion.json"

# entry, sense (1-based), occurrence (1-based), old label, canonical names[0]
CHANGES = [
    ("t_19559c6691da", 1, 5, "Xuzhou Pudu", "Xuzhou Pudu"),
    ("t_1f2952865d30", 1, 5, "Shending Yikui", "Yikui Yuankui"),
    ("t_6a36bf01ea70", 1, 4, "Yongjue Yuanxian", "Yongjue Yuanxian"),
    ("t_6a36bf01ea70", 1, 5, "Feiyin Tongrong", "Feiyin Tongrong"),
    ("t_744799df0748", 1, 5, "Shenfeng Xian", "Shenfeng Xian"),
    ("t_7b5328124660", 1, 5, "Wuyi Yuanlai", "Wuyi Yuanlai"),
    ("t_95ed005ba0c5", 1, 4, "Touzi Yiqing", "Touzi Yiqing"),
    ("t_a68f9a04d31b", 1, 1, "Ying'an Tanhua", "Ying'an Tanhua"),
    ("t_ee00dd9e7b4d", 1, 5, "Jinfeng Congzhi 金峰從志", "Jinfeng Congzhi"),
]


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_json(path: Path, value) -> None:
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main() -> int:
    touched: dict[str, dict] = {}
    rows = []
    for entry_id, sense_no, occurrence_no, old, canonical in CHANGES:
        draft_path = FRESH / entry_id / "evidence.draft.json"
        draft = touched.setdefault(entry_id, json.loads(draft_path.read_text(encoding="utf-8-sig")))
        occurrence = draft["Entry"]["Senses"][sense_no - 1]["Occurrences"][occurrence_no - 1]
        actor = occurrence.get("ActorAttribution") or {}
        if occurrence.get("MasterName") is not None:
            raise SystemExit(f"{entry_id} s{sense_no} o{occurrence_no}: MasterName is not null")
        if actor.get("Status") != "identified-unlinked-master" or actor.get("ActorLabel") != old:
            raise SystemExit(f"{entry_id} s{sense_no} o{occurrence_no}: expected unlinked {old!r}")
        occurrence["MasterName"] = canonical
        occurrence.pop("ActorAttribution", None)
        links = occurrence.setdefault("ContextMasters", [])
        links[:] = [row for row in links if row.get("MasterName") not in {old, canonical}]
        links.append({"MasterName": canonical, "Roles": ["utterer"]})
        occurrence["AttributionNote"] = str(occurrence.get("AttributionNote") or "").replace(old, canonical)
        proof = occurrence.get("DraftActorProof") or {}
        for key in ("GrammaticalSubject", "SpeechFrame", "FullCaseDecision"):
            if isinstance(proof.get(key), str):
                proof[key] = proof[key].replace(old, canonical)
        rows.append({"id": entry_id, "sense": sense_no, "occurrence": occurrence_no,
                     "oldActorLabel": old, "canonicalMasterName": canonical})

    outputs = []
    for entry_id, draft in touched.items():
        draft_path = FRESH / entry_id / "evidence.draft.json"
        entry_path = FRESH / entry_id / "entry.v2.json"
        write_json(draft_path, draft)
        compiled, errors = compile_draft(draft)
        if errors:
            raise SystemExit(f"{entry_id}: compiler errors: {errors}")
        write_json(entry_path, compiled)
        outputs.append({"id": entry_id, "worksheetSha256": sha(draft_path), "entrySha256": sha(entry_path)})

    receipt = {
        "schemaVersion": "release60-roster-alias-promotion-v1",
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "authorizedScope": "nine exact occurrences reported by the strengthened release-60 attribution gate",
        "promotions": rows,
        "promotionCount": len(rows),
        "entryCount": len(outputs),
        "outputs": outputs,
        "lineageRosterMutation": False,
        "installPerformed": False,
    }
    write_json(OUT, receipt)
    print(json.dumps({"hardPass": True, "promotions": len(rows), "entries": len(outputs),
                      "receipt": str(OUT.relative_to(ROOT)), "receiptSha256": sha(OUT)}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
