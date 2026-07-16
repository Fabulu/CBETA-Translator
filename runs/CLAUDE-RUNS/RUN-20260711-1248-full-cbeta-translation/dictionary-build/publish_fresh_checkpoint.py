#!/usr/bin/env python3
"""Audit, merge, verify, and optionally publish a fresh-build checkpoint.

The command deliberately fails unless every public artifact has exactly the
hash-approved root KEEP ID set.  With --deploy it also refuses success until
the stable Cloudflare URL reports that same count.
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
from pathlib import Path


HERE = Path(__file__).resolve().parent
REPO = HERE.parents[3]
FRESH = HERE / "fresh-build"
MERGED = FRESH / "merged"
DEFAULT_DASHBOARD = Path("/mnt/c/programmieren/readzendictprogress")
STABLE_DATA = "https://readzen-dict-progress.pages.dev/data/progress.json"


def run(command: list[str], cwd: Path, *, capture: bool = False, timeout: int | None = None) -> str:
    proc = subprocess.run(
        command,
        cwd=cwd,
        text=True,
        encoding="utf-8",
        errors="replace",
        check=True,
        timeout=timeout,
        stdout=subprocess.PIPE if capture else None,
    )
    return proc.stdout if capture else ""


def load(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def root_review_paths() -> list[Path]:
    return sorted((FRESH / "waves").glob("f[0-9][0-9][0-9]-root-review.json"))


def approved_ids() -> set[str]:
    approved: set[str] = set()
    for path in root_review_paths():
        rows = load(path).get("entries", {})
        overlap = approved.intersection(rows)
        if overlap:
            raise SystemExit(f"root-review IDs repeated across waves: {sorted(overlap)}")
        approved.update(
            entry_id for entry_id, row in rows.items() if row.get("verdict") == "KEEP"
        )
    return approved


def verify_artifacts(expected: set[str]) -> None:
    entries = load(MERGED / "termbase.v2.json").get("Entries", [])
    merged_ids = [row["Id"] for row in entries]
    if len(merged_ids) != len(set(merged_ids)):
        raise SystemExit("duplicate IDs in termbase.v2.json")
    if set(merged_ids) != expected:
        raise SystemExit(
            f"merged/root mismatch: missing={sorted(expected-set(merged_ids))} "
            f"extra={sorted(set(merged_ids)-expected)}"
        )

    shard_ids: list[str] = []
    for path in sorted((MERGED / "termbase").glob("*.json")):
        payload = load(path)
        rows = payload if isinstance(payload, list) else payload.get("Entries", payload.get("entries", []))
        shard_ids.extend(row["Id"] for row in rows)
    if len(shard_ids) != len(set(shard_ids)) or set(shard_ids) != expected:
        raise SystemExit(
            f"shard/root mismatch: rows={len(shard_ids)} unique={len(set(shard_ids))} "
            f"expected={len(expected)}"
        )

    index = load(MERGED / "termbase.index.json").get("Terms", [])
    if len(index) != len(expected):
        raise SystemExit(f"index/root count mismatch: index={len(index)} expected={len(expected)}")


def stable_count() -> tuple[str, str]:
    raw = run(
        [
            "curl", "-fsSL", "-A", "Mozilla/5.0", "-H", "Cache-Control: no-cache",
            f"{STABLE_DATA}?t={time.time_ns()}",
        ],
        REPO,
        capture=True,
    )
    payload = json.loads(raw)
    return payload["headline"][0]["value"], payload["updatedUtc"]


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--deploy", action="store_true")
    parser.add_argument("--dashboard", type=Path, default=DEFAULT_DASHBOARD)
    parser.add_argument("--polls", type=int, default=12)
    args = parser.parse_args()

    windows_build = HERE.as_posix().replace("/mnt/c/", "C:/")
    reviews = root_review_paths()
    if not reviews:
        raise SystemExit("no fresh root-review ledgers found")
    for review in reviews:
        wave = review.name.split("-", 1)[0]
        audit_raw = run(
            [
                "cmd.exe", "/d", "/c", "node", "eng/tools/audit-fresh-root-review.js",
                f"--build-dir={windows_build}", f"--wave={wave}",
            ],
            REPO,
            capture=True,
        )
        print(audit_raw.strip(), flush=True)
    expected = approved_ids()
    expected_count = len(expected)
    # Publication must prove live quality, not merely that ledgers, hashes and
    # generated artifacts agree.  Roster completion is tracked separately;
    # every other cohort rule is fail-closed here over the complete live set.
    live_paths = [FRESH / "entries" / entry_id / "entry.v2.json" for entry_id in sorted(expected)]
    quality_report = HERE / "maintenance" / "fresh-publication-quality-current.json"
    run(
        [sys.executable, str(HERE / "run_cohort_gate.py"), *map(str, live_paths),
         "--defer-roster", "--skip-packets", "--output", str(quality_report)],
        HERE,
    )
    quality = load(quality_report)
    quality_hashes = {row["id"]: row["sha256"] for row in quality.get("entries", [])}
    current_hashes = {
        entry_id: __import__("hashlib").sha256((FRESH / "entries" / entry_id / "entry.v2.json").read_bytes()).hexdigest()
        for entry_id in expected
    }
    if not quality.get("hardPass") or quality_hashes != current_hashes:
        raise SystemExit("whole-tree publication quality gate did not hard-pass at current hashes")
    run(
        [
            "cmd.exe", "/c", "node", "eng/tools/merge-dict-entries.js",
            f"--terms-dir={(FRESH / 'entries').as_posix().replace('/mnt/c/', 'C:/')}",
            f"--out={MERGED.as_posix().replace('/mnt/c/', 'C:/')}",
            "--fresh",
        ],
        REPO,
    )
    verify_artifacts(expected)
    print(f"[checkpoint] root, merged, index, and shards agree: {expected_count} entries", flush=True)

    if not args.deploy:
        return 0

    run(["python3", "scripts/update_progress.py"], args.dashboard)
    local = load(args.dashboard / "data" / "progress.json")
    target = f"{expected_count:,}/{int(local['diagnostics']['rowCount']):,}"
    if local["headline"][0]["value"] != target:
        raise SystemExit(
            f"dashboard source stale: {local['headline'][0]['value']} != {target}"
        )
    # Name the Windows shim explicitly.  `CALL npx ...` hangs when invoked
    # through WSL on some machines; `npx.cmd` returns a real exit status.
    try:
        run(
            [
                "cmd.exe", "/d", "/c", "npx.cmd", "--yes", "wrangler", "pages", "deploy", ".",
                "--project-name=readzen-dict-progress", "--branch=main", "--commit-dirty=true",
            ],
            args.dashboard,
            timeout=45,
        )
    except subprocess.TimeoutExpired:
        # Wrangler can finish the upload yet leave its Windows cmd shim alive
        # under WSL.  The stable-domain equality check below, not shim exit,
        # decides whether publication succeeded.
        print("[checkpoint] deploy shim timed out; verifying stable domain", flush=True)
    for attempt in range(1, args.polls + 1):
        try:
            value, updated = stable_count()
        except (subprocess.CalledProcessError, json.JSONDecodeError) as exc:
            print(f"[checkpoint] stable poll {attempt}: fetch failed ({exc})")
        else:
            print(f"[checkpoint] stable poll {attempt}: {value} updated={updated}", flush=True)
            if value == target:
                return 0
        time.sleep(5)
    raise SystemExit(f"stable dashboard did not converge to {target}")


if __name__ == "__main__":
    raise SystemExit(main())
