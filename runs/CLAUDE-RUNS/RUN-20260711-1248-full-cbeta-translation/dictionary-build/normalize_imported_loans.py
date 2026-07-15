"""Reviewed prose-only normalization of imported English loans in merged entries."""

import argparse
import json
from pathlib import Path


BUILD = Path(__file__).resolve().parent
TERMS = BUILD / "terms"

# Longest/specific constructions first. Generic bare "dharma" is intentionally
# not replaced mechanically because 法 may mean teaching, thing, relation, or a
# word inside a proper title depending on the passage.
REPLACEMENTS = [
    ("the myriad dharmas", "the myriad things"),
    ("myriad dharmas", "myriad things"),
    ("all dharmas", "all things"),
    ("ten thousand dharmas", "myriad things"),
    ("the hundred dharmas in five groups", "the hundred things in five groups"),
    ("no dharma differs from another", "none of the myriad things differs from another"),
    ("what dharma he uses to show people", "what he uses to show people"),
    ("selecting selects what dharma", "selecting selects what"),
    ("listening to the Dharma", "listening to the teaching"),
    ("Dharma-hall", "teaching-hall"),
    ("dharma hall", "teaching hall"),
    ("one-mind Dharma", "teaching of the one mind"),
    ("treasury of the true Dharma eye", "treasury of the true eye of the teaching"),
    ("Treasury of the True Dharma Eye", "Treasury of the True Eye of the Teaching"),
    ("treasury of the true dharma eye", "treasury of the true eye of the teaching"),
    ("treasury of the true dharma-eye", "treasury of the true eye of the teaching"),
    ("true Dharma eye", "true eye of the teaching"),
    ("True Dharma Eye", "True Eye of the Teaching"),
    ("true dharma eye", "true eye of the teaching"),
    ("true dharma-eye", "true eye of the teaching"),
    ("Dharma heirs", "lineage heirs"),
    ("Dharma heir", "lineage heir"),
    ("dharma heirs", "lineage heirs"),
    ("dharma heir", "lineage heir"),
    ("dharma-heir", "lineage-heir"),
    ("Dharma succession", "lineage succession"),
    ("dharma succession", "lineage succession"),
    ("Dharma successor", "lineage successor"),
    ("dharma successor", "lineage successor"),
    ("Dharma Treasure", "Teaching Treasure"),
    ("Dharma Altar", "Teaching Altar"),
    ("Dharma words", "teaching words"),
    ("dharma words", "teaching words"),
    ("dharma-words", "teaching words"),
    ("Dharma gate", "teaching gate"),
    ("dharma gate", "teaching gate"),
    ("dharma-gate", "teaching gate"),
    ("Dharma eye", "eye of the teaching"),
    ("dharma eye", "eye of the teaching"),
    ("dharma-eye", "eye of the teaching"),
    ("Dharma body", "body of the teaching"),
    ("dharma body", "body of the teaching"),
    ("dharma-body", "body of the teaching"),
    ("Buddha-Dharma", "Buddha's teaching"),
    ("Buddha-dharma", "Buddha's teaching"),
    ("buddhadharma", "Buddha's teaching"),
    ("prajñā-samādhi", "complete command of wisdom"),
    ("Prajñā-samādhi", "Complete Command of Wisdom"),
    ("samādhi", "complete command"),
    ("Samādhi", "Complete Command"),
    ("Samadhi", "Complete Command"),
]


def rewrite(value, key=None):
    if isinstance(value, dict):
        return {name: rewrite(item, name) for name, item in value.items()}
    if isinstance(value, list):
        return [rewrite(item, key) for item in value]
    if isinstance(value, str) and key != "Kwic":
        for old, new in REPLACEMENTS:
            value = value.replace(old, new)
    return value


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--ids", nargs="*", help="Optional exact term IDs to restrict the sweep")
    args = parser.parse_args()
    selected = set(args.ids or [])
    changed_files = 0
    replacements = 0
    for directory in TERMS.iterdir():
        if selected and directory.name not in selected:
            continue
        if not (directory / "STATUS").exists() or (directory / "STATUS").read_text(encoding="utf-8").strip() != "done":
            continue
        path = directory / "entry.v2.json"
        if not path.exists():
            continue
        entry = json.loads(path.read_text(encoding="utf-8"))
        before = json.dumps(entry, ensure_ascii=False, sort_keys=True)
        updated = rewrite(entry)
        after = json.dumps(updated, ensure_ascii=False, sort_keys=True)
        if before == after:
            continue
        replacements += sum(before.count(old) for old, _ in REPLACEMENTS)
        path.write_text(json.dumps(updated, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        changed_files += 1
    print(f"changed_files={changed_files} replacement_candidates={replacements}")


if __name__ == "__main__":
    main()
