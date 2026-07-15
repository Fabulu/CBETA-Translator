"""Validate and register one completed wave in STATUS files and MANIFEST.jsonl."""

import argparse
import hashlib
import json
import re
from pathlib import Path


BUILD = Path(__file__).resolve().parent

# Read-only preflight reports occasionally contain navigational family labels or
# a spelling variant that the wave plan explicitly says belongs in an existing
# entry.  Keep these exclusions explicit rather than manufacturing duplicate or
# zero-hit dictionary headwords merely to satisfy the cached report.
PREFLIGHT_EXCLUSIONS = {
    "b035": {
        "t_b986851dcdd8": "父母未生已前 is already an attested variant in 父母未生前",
    },
    "b036": {
        "t_073191901a6f": "Caodong Five Ranks is a family heading, not a corpus headword",
        "t_e84705b10d72": "心-prefixed is a navigation note, not a corpus headword",
    },
}


def depth_gate_error(entry_id: str, entry_path: Path) -> str | None:
    gate_path = BUILD / "maintenance" / "depth-sense-gate.json"
    if not gate_path.exists():
        return f"depth/sense gate has not been run: {entry_id}"
    result = json.loads(gate_path.read_text(encoding="utf-8")).get("results", {}).get(entry_id)
    digest = hashlib.sha256(entry_path.read_bytes()).hexdigest()
    if not result or result.get("entrySha256") != digest:
        return f"depth/sense gate is missing or stale: {entry_id}"
    if not result.get("hardPass"):
        return f"depth/sense gate failed: {entry_id} {result.get('hardFlags')}"
    return None


def assignment_agents(batch_id: str) -> dict[str, int]:
    path = BUILD / f"{batch_id.upper()}_ASSIGNMENTS.md"
    text = path.read_text(encoding="utf-8")
    current = None
    mapping = {}
    for line in text.splitlines():
        match = re.fullmatch(r"## Batch ([ABC])", line)
        if match:
            current = ord(match.group(1)) - ord("A") + 1
            continue
        term = re.match(r"- `(t_[0-9a-f]{12})`\s+", line)
        if term and current is not None:
            mapping[term.group(1)] = current
    return mapping


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("batch_id")
    parser.add_argument("--commit", action="store_true")
    args = parser.parse_args()
    batch_id = args.batch_id.lower()
    if not re.fullmatch(r"b\d{3}", batch_id):
        raise SystemExit("batch_id must match bNNN")

    report = json.loads(
        (BUILD / "maintenance" / f"{batch_id}-preflight.json").read_text(encoding="utf-8")
    )
    agents = assignment_agents(batch_id)
    manifest_path = BUILD / "MANIFEST.jsonl"
    manifest_rows = [
        json.loads(line)
        for line in manifest_path.read_text(encoding="utf-8").splitlines()
        if line.strip()
    ]
    existing_ids = {row["termId"] for row in manifest_rows}
    existing_terms = {row["sourceTerm"] for row in manifest_rows}
    additions = []
    errors = []

    exclusions = PREFLIGHT_EXCLUSIONS.get(batch_id, {})
    for planned in report["terms"]:
        if planned["Id"] in exclusions:
            print(
                f"excluded preflight item: {planned['Id']} "
                f"{planned['SourceTerm']} — {exclusions[planned['Id']]}"
            )
            continue
        entry_path = BUILD / "terms" / planned["Id"] / "entry.v2.json"
        if not entry_path.exists():
            errors.append(f"missing entry: {planned['Id']} {planned['SourceTerm']}")
            continue
        entry = json.loads(entry_path.read_text(encoding="utf-8"))
        if entry.get("Id") != planned["Id"] or entry.get("SourceTerm") != planned["SourceTerm"]:
            errors.append(f"identity mismatch: {entry_path}")
        gate_error = depth_gate_error(planned["Id"], entry_path)
        if gate_error:
            errors.append(gate_error)
        if planned["Id"] not in agents:
            errors.append(f"missing assignment: {planned['Id']}")
        if planned["Id"] in existing_ids or planned["SourceTerm"] in existing_terms:
            errors.append(f"already in manifest: {planned['Id']} {planned['SourceTerm']}")
        additions.append(
            {
                "termId": planned["Id"],
                "sourceTerm": planned["SourceTerm"],
                "status": "done",
                "batchId": batch_id,
                "agent": agents.get(planned["Id"]),
            }
        )

    if errors:
        raise SystemExit("\n".join(errors))
    print(f"validated {len(additions)} entries for {batch_id}; commit={args.commit}")
    if not args.commit:
        return

    for row in additions:
        status_path = BUILD / "terms" / row["termId"] / "STATUS"
        status_path.write_text("done\n", encoding="utf-8")
    with manifest_path.open("a", encoding="utf-8", newline="") as handle:
        for row in additions:
            handle.write(json.dumps(row, ensure_ascii=False, separators=(",", ":")) + "\n")
    print(f"registered {len(additions)} entries for {batch_id}")


if __name__ == "__main__":
    main()
