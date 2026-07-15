"""Validate and register an explicit curated term batch outside WAVE_PLAN."""

import argparse
import hashlib
import json
import re
from pathlib import Path


BUILD = Path(__file__).resolve().parent


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


def term_id(term: str) -> str:
    return "t_" + hashlib.sha256(term.strip().encode("utf-8")).hexdigest()[:12]


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("batch_id")
    parser.add_argument("terms", nargs="+")
    parser.add_argument("--commit", action="store_true")
    args = parser.parse_args()
    if not re.fullmatch(r"[a-z0-9][a-z0-9_-]*", args.batch_id):
        raise SystemExit("batch_id must use lowercase letters, digits, underscores, or hyphens")

    manifest_path = BUILD / "MANIFEST.jsonl"
    rows = [json.loads(line) for line in manifest_path.read_text(encoding="utf-8").splitlines() if line.strip()]
    existing_ids = {row["termId"] for row in rows}
    existing_terms = {row["sourceTerm"] for row in rows}
    additions = []
    errors = []
    for index, term in enumerate(args.terms):
        entry_id = term_id(term)
        entry_path = BUILD / "terms" / entry_id / "entry.v2.json"
        if not entry_path.exists():
            errors.append(f"missing entry: {entry_id} {term}")
            continue
        entry = json.loads(entry_path.read_text(encoding="utf-8"))
        if entry.get("Id") != entry_id or entry.get("SourceTerm") != term:
            errors.append(f"identity mismatch: {entry_path}")
        gate_error = depth_gate_error(entry_id, entry_path)
        if gate_error:
            errors.append(gate_error)
        if entry_id in existing_ids or term in existing_terms:
            errors.append(f"already in manifest: {entry_id} {term}")
        additions.append({"termId": entry_id, "sourceTerm": term, "status": "done",
                          "batchId": args.batch_id, "agent": index % 3 + 1})

    if errors:
        raise SystemExit("\n".join(errors))
    print(f"validated {len(additions)} entries for {args.batch_id}; commit={args.commit}")
    if not args.commit:
        return
    for row in additions:
        (BUILD / "terms" / row["termId"] / "STATUS").write_text("done\n", encoding="utf-8")
    with manifest_path.open("a", encoding="utf-8", newline="") as handle:
        for row in additions:
            handle.write(json.dumps(row, ensure_ascii=False, separators=(",", ":")) + "\n")
    print(f"registered {len(additions)} entries for {args.batch_id}")


if __name__ == "__main__":
    main()
