"""Generate balanced, durable three-agent assignments from wave preflight reports."""

import argparse
import json
import math
from pathlib import Path


BUILD = Path(__file__).resolve().parent


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("start", type=int)
    parser.add_argument("end", type=int)
    args = parser.parse_args()

    for number in range(args.start, args.end + 1):
        batch_id = f"b{number:03d}"
        report_path = BUILD / "maintenance" / f"{batch_id}-preflight.json"
        report = json.loads(report_path.read_text(encoding="utf-8"))
        rows = report["terms"]
        capacity = math.ceil(len(rows) / 3)
        groups = [{"rows": [], "hits": 0} for _ in range(3)]
        for row in sorted(rows, key=lambda item: (-item["Hits"], item["SourceTerm"])):
            choices = [group for group in groups if len(group["rows"]) < capacity]
            group = min(choices, key=lambda item: (item["hits"], len(item["rows"])))
            group["rows"].append(row)
            group["hits"] += row["Hits"]

        lines = [
            f"# {batch_id} agent assignments",
            "",
            f"Prepared from current corpus counts in `maintenance/{batch_id}-preflight.json`. Do not seed or edit these term directories until earlier waves pass root integration.",
            "",
        ]
        for label, group in zip("ABC", groups):
            lines.extend([f"## Batch {label}", ""])
            for row in group["rows"]:
                lines.append(f"- `{row['Id']}` {row['SourceTerm']}")
            lines.append("")
        lines.extend(
            [
                "Each agent must read the guide and checkpoint in full, build rich entries from `zc.py` research, harvest self-definitions/contrasts/variants/morphology, verify exact contiguous evidence and governing heads, and touch only its assigned term directories plus its report. Planning glosses are search leads only, never authority for a definition. Root owns manifest/status/termbase integration.",
                "",
            ]
        )
        out = BUILD / f"{batch_id.upper()}_ASSIGNMENTS.md"
        if out.exists():
            raise SystemExit(f"refusing to overwrite existing assignment: {out}")
        out.write_text("\n".join(lines), encoding="utf-8")
        print(f"{batch_id}: {len(rows)} terms -> {[len(group['rows']) for group in groups]}")


if __name__ == "__main__":
    main()
