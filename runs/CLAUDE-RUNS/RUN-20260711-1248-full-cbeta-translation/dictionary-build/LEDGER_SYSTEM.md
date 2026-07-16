# Dictionary-Build Ledger — durable, resumable, loss-bounded work system

Design for the actual dictionary-creation phase (many terms, many sessions). Requirements
(user, 2026-07-11): agents must **write findings continually**, work must **never be lost**,
and any fresh session must be able to **pick up exactly where we left off** across plan/usage
limits. This is the organizational system that meets those criteria.

## Principle: the orchestrator owns the truth; workers are disposable
The GUARANTEE against lost work is not "the worker saves often" (a worker can be killed mid-write
and earlier agents in this project reported the harness sometimes blocking subagent file writes).
The guarantee is: **a worker researches a SMALL slice and RETURNS structured findings; the
orchestrator (main thread) persists them to the ledger the instant the worker returns.** If a
worker dies, its term is simply still `todo`/`researching` and gets retried — at most one slice of
in-flight work is redone, never lost. Workers that *can* also append to their own WORK.md do so as
belt-and-suspenders, but persistence never depends on it.

Small slices (1 term per worker, or ≤10 for cheap mechanical passes) keep the redo window tiny.

## Directory layout (under this run)
```
dictionary-build/
  LEDGER_SYSTEM.md        # this file (the design + rules)
  STATUS.md               # human rollup: counts by status, current batch, resume pointer
  MANIFEST.jsonl          # APPEND-ONLY status ledger; last line per termId wins
  terms/
    <termId>/             # termId = DictionaryStore.ComputeId(sourceTerm) = "t_<12hex>" (deterministic!)
      STATUS              # one token: todo|researching|drafted|validated|done|skipped|disputed
      WORK.md             # APPEND-ONLY journal: findings, concordance hits, decisions, timestamps
      entry.v2.json       # the durable draft entry (DictionaryFile-shaped, single entry)
  batches/
    <batchId>.md          # one batch = one orchestration pass: terms covered, worker ids, outcome
```
`termId` is deterministic (the same hash the store uses), so re-touching a term always lands in the
same dir — **idempotent, never duplicated.**

## MANIFEST.jsonl — the resumable index (append-only)
One JSON object per line; to read current state, replay and keep the LAST line per `termId`:
```
{"termId":"t_36aa29eb1287","sourceTerm":"水牯牛","status":"done","updatedUtc":"...","batchId":"b003","senses":2,"validation":"multi-source"}
```
Append-only means an interrupted write costs at most the last line; never a corrupted index.
`status` also lives in `terms/<id>/STATUS` as a redundant per-term copy.

## Status lifecycle
`todo` → `researching` → `drafted` → `validated` → `done`
side states: `skipped` (with reason: not-Zen / too-generic), `disputed` (needs human), `needs-review`.
- `drafted` = entry.v2.json exists with senses + occurrences but not yet multi-source-checked.
- `validated` = multi-source gate applied; validation field set (multi-source/provisional/disputed).
- `done` = validated + curated + ready to merge into termbase.v2.json.

## Roles
- **Orchestrator (main thread / me):** seeds the term queue; picks the next `todo` slice; spawns
  workers; on each return, writes `terms/<id>/entry.v2.json` + appends WORK.md + appends a MANIFEST
  line + updates STATUS.md; writes a `batches/<id>.md` per pass. Keeps TASK_LOG pointed at the
  current batch. NEVER holds unpersisted findings across more than one worker return.
- **Worker (subagent):** gets ONE term (or a tiny batch) + the Zen concordance tools; returns a
  structured payload = {entry.v2.json content, WORK.md journal notes, proposed status}. Told to
  return incrementally-safe output and to also append its own WORK.md if writes are permitted.

## The term queue (step 0, before entries)
The set of terms to build is itself a work product: candidate Zen terms mined from the Zen corpus
(the prescriptive allowlist). That extraction pass seeds MANIFEST.jsonl with `todo` lines (one per
candidate term, deterministic termId). Term selection uses judgment (skip generic particles) — see
SPEC_v1 "MULTI-SOURCE VALIDATION" + word-selection open question.

## Resume protocol (a fresh session after a plan limit runs THIS)
1. Read `dictionary-build/STATUS.md` (rollup + "current batch" + "next action" pointer).
2. Replay `MANIFEST.jsonl` → current status per termId → list all non-`done`/`skipped` terms.
3. For each `researching`/`drafted` term, read its `terms/<id>/WORK.md` + `entry.v2.json` to see how
   far it got; continue from there (idempotent — safe to re-run).
4. Pick the next slice of `todo` terms; open a `batches/<newId>.md`; spawn workers; persist on return.
5. When a term hits `done`, it is eligible to merge into the real `termbase.v2.json` via DictionaryStore.
Never start from scratch; the manifest + per-term dirs are the memory.

## Loss-bounding rules (the "plan limit" defense)
- Persist on EVERY worker return; keep ≤1 worker's output unpersisted at a time.
- Slices small (1 term default). A killed session loses only in-flight (unreturned) workers.
- STATUS.md rewritten after every batch with an explicit "NEXT ACTION" line, so recovery is 1 read.
- All ledger files are plain text in the repo → git-committable checkpoints between batches.

## Optional accelerator (same-session only)
The Workflow harness journals every agent return and supports resume-from-runId (cached unchanged
steps). It can drive a batch as a pipeline(term → research → validate → return). Useful WITHIN a
session; it does NOT replace this file ledger, which is what survives ACROSS sessions/plan limits.
Requires explicit user opt-in ("use a workflow" / ultracode) before use.

## Kickoff checklist (when dictionary creation actually starts)
- [ ] Confirm whether subagents can write files here (probe once); if not, rely on return-and-persist.
- [ ] Run the term-extraction pass → seed MANIFEST.jsonl with `todo` terms.
- [ ] Set STATUS.md batch pointer; begin batch b001.
- [ ] Wire `done` terms → merge into termbase.v2.json (DictionaryStore.SaveAsync writes v2 + legacy).
