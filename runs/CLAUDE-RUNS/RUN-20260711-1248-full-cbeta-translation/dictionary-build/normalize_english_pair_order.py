"""Conservatively reorder explicit Chinese→English prose pairs for #0c.

Only patterns with an explicit quoted English gloss are changed. KWIC fields,
structural fields, and unpaired Chinese are untouched. A ZIP backup is written.
"""

from __future__ import annotations

import json
import re
import zipfile
from datetime import datetime, timezone
from pathlib import Path

BUILD = Path(__file__).resolve().parent
TERMS = BUILD / "terms"
MAINT = BUILD / "maintenance"
CN = r"[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff，。、／：；？！、·]+"

PATTERNS = [
    re.compile(rf"(?P<cn>{CN})\s*\(\s*\"(?P<en>[^\"]+)\"(?P<tail>[^)]*)\)"),
    re.compile(rf"(?P<cn>{CN})\s*\(\s*“(?P<en>[^”]+)”(?P<tail>[^)]*)\)"),
    re.compile(rf"(?P<cn>{CN})\s*\"(?P<en>[^\"]+)\""),
    re.compile(rf"(?P<cn>{CN})\s*‘(?P<en>[^’]+)’"),
]


def reorder(text: str) -> tuple[str, int]:
    total = 0
    for index, pattern in enumerate(PATTERNS):
        def replace(match: re.Match) -> str:
            nonlocal total
            total += 1
            cn = match.group("cn")
            en = match.group("en")
            tail = match.groupdict().get("tail") or ""
            quote_open, quote_close = ('"', '"') if index in (0, 2) else ('‘', '’')
            return f"{quote_open}{en}{quote_close} ({cn}{tail})"
        text = pattern.sub(replace, text)
    return text, total


def main() -> None:
    paths = sorted(TERMS.glob("*/entry.v2.json"))
    MAINT.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    backup = MAINT / f"english-pair-order-backup-{stamp}.zip"
    with zipfile.ZipFile(backup, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for path in paths:
            archive.write(path, path.relative_to(BUILD))

    files_changed = 0
    pairs_changed = 0
    for path in paths:
        entry = json.loads(path.read_text(encoding="utf-8"))
        count = 0
        for sense in entry.get("Senses") or []:
            for field in ("PreferredTarget", "Explanation", "Note"):
                value = sense.get(field)
                if isinstance(value, str):
                    sense[field], n = reorder(value)
                    count += n
            alternates = []
            for value in sense.get("AlternateTargets") or []:
                value, n = reorder(value)
                alternates.append(value)
                count += n
            sense["AlternateTargets"] = alternates
            for occurrence in sense.get("Occurrences") or []:
                value = occurrence.get("AttributionNote")
                if isinstance(value, str):
                    occurrence["AttributionNote"], n = reorder(value)
                    count += n
        if count:
            path.write_text(json.dumps(entry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
            files_changed += 1
            pairs_changed += count

    print(json.dumps({"filesChanged": files_changed, "pairsChanged": pairs_changed, "backup": str(backup)}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
