"""Canonicalize dictionary link fields against master-dates.json.

Chinese/alternate names are mapped through the roster's names arrays. Values
not represented in the roster are removed from RelatedMasters or set to null in
scalar link fields; the surrounding English attribution note remains intact.
"""

from __future__ import annotations

import json
import zipfile
from datetime import datetime, timezone
from pathlib import Path

BUILD = Path(__file__).resolve().parent
TERMS = BUILD / "terms"
ROSTER = Path(r"C:\programmieren\MergeWorkCbeta\CBETA-Translator\Assets\Data\master-dates.json")
MAINT = BUILD / "maintenance"


def main() -> None:
    roster = json.loads(ROSTER.read_text(encoding="utf-8"))
    aliases = {
        name: master["names"][0]
        for master in roster["masters"]
        for name in master.get("names", [])
    }
    # A short form found in older entries; the roster has the fuller canonical name.
    aliases["Da'an"] = "Changqing Da'an"
    canonical = {master["names"][0] for master in roster["masters"]}

    paths = sorted(TERMS.glob("*/entry.v2.json"))
    MAINT.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    backup = MAINT / f"master-link-backup-{stamp}.zip"
    with zipfile.ZipFile(backup, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for path in paths:
            archive.write(path, path.relative_to(BUILD))

    changes = []
    for path in paths:
        entry = json.loads(path.read_text(encoding="utf-8"))
        touched = False
        for si, sense in enumerate(entry.get("Senses") or []):
            for field in ("SenseKey", "MasterName"):
                old = sense.get(field)
                if not old or old in canonical:
                    continue
                new = aliases.get(old)
                sense[field] = new
                changes.append({"entryId": entry["Id"], "field": f"Senses[{si}].{field}", "old": old, "new": new})
                touched = True

            old_related = sense.get("RelatedMasters") or []
            new_related = []
            for old in old_related:
                new = old if old in canonical else aliases.get(old)
                if new and new not in new_related:
                    new_related.append(new)
                if new != old:
                    changes.append({"entryId": entry["Id"], "field": f"Senses[{si}].RelatedMasters", "old": old, "new": new})
                    touched = True
            sense["RelatedMasters"] = new_related

            for oi, occurrence in enumerate(sense.get("Occurrences") or []):
                old = occurrence.get("MasterName")
                if not old or old in canonical:
                    continue
                new = aliases.get(old)
                occurrence["MasterName"] = new
                changes.append({"entryId": entry["Id"], "field": f"Senses[{si}].Occurrences[{oi}].MasterName", "old": old, "new": new})
                touched = True

        if touched:
            path.write_text(json.dumps(entry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    report = {
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "backup": str(backup),
        "filesScanned": len(paths),
        "fieldsChanged": len(changes),
        "changes": changes,
    }
    report_path = MAINT / f"master-link-normalization-{stamp}.json"
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({k: report[k] for k in ("filesScanned", "fieldsChanged", "backup")}, ensure_ascii=False, indent=2))
    print(f"report: {report_path}")


if __name__ == "__main__":
    main()
