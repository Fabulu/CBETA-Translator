# Remediation speed audit — 2026-07-14

## Baseline and bottlenecks

The owner2 repair ledger refresh reverified 30 entries and refreshed hashes in about 3 seconds. Individual full cohort gates, however, took 5.8–60.4 seconds each; the observed owner2 samples total many minutes because every invocation starts `audit_attribution.py`, `audit_public_feedback.py`, `audit_depth_sense.py`, and attribution-packet generation again. This is orchestration waste, not semantic work.

The irreducible work remains passage reading: identify the exact utterer of the headword after reading the whole exchange, widen context when necessary, and record non-master/narrated/impersonal outcomes. Source lookup is reduced safely by grouping occurrences by `RelPath`, so one opened record/section can settle several entries. `zc.verify` is fast after its normalized-text disk cache is warm and must remain the mechanical gate. Hash/ledger refresh, merge, and deploy are small compared with reading and repeated gates. Agent spawn/handoff and duplicate reviewers become costly when bundles are tiny; reviewers should receive one immutable cohort hash manifest, not repeated per-entry handoffs.

## Safe changes implemented

1. `run_cohort_gate.py` now records per-phase timings (`exactKwic`, attribution, public feedback, depth/sense, packets), so future optimization uses evidence rather than guesses.
2. `maintenance/run_ledger_cohort_gate.py` runs one gate over all completed rows in a repair ledger. This preserves every audit and every exact verification while amortizing process startup, corpus loading, and packet generation.
3. Gate only twice: a cheap targeted check while editing, then one immutable combined hard gate after the cohort stops changing. Never regenerate 30 full gates after each local note fix.

Expected mechanical speedup for a 30-entry cohort is roughly 4–10×, depending on cache warmth and occurrence count. It does not claim to accelerate the human semantic reading itself.

## One-pass operating sequence

1. Build a source-grouped reading packet once; read every occurrence in full context and edit entries.
2. Run `zc_batch`/the repair ledger verifier once across all changed entries.
3. Freeze entry hashes.
4. Run one combined gate and one independent review against that manifest.
5. Apply reviewer findings, freeze again, and run one final combined gate. Merge/deploy once.

Commands:

```bash
python3 maintenance/run_ledger_cohort_gate.py \
  maintenance/semantic-cohorts/semantic-r003-owner2-actor-repair.json \
  --output maintenance/semantic-cohorts/semantic-r003-owner2-actorrepair-combined-gate.json

node eng/tools/merge-dict-entries.js
```

Do not use `--skip-packets` for the final gate. It is only for an explicitly disposable edit-time preflight. Independent review remains mandatory and must read the passages; it should not duplicate corpus extraction or receive stale hashes.

## Controlled clean-rebuild benchmark (required; execution ledger)

The stratified sample is fixed before timing so results cannot be cherry-picked:

| class | entry | reason |
|---|---|---|
| thin/easy | 合頭語 (`t_b2f05c3e4b7d`) | six witnesses, one sense |
| deep/keystone | 和尚 | very high-frequency role term |
| multisense | 棒 | object versus countable blow |
| actor-complex | 正法眼 (`t_970c3f191929`) | unnamed questioners and named respondents |
| dangling claim | 本來無一物 (`t_93ab42fecdca`) | verified alternate textual line needing claim anchoring |

Two workers must start from identical corpus/index access. Worker R repairs a frozen copy of the current entry; worker N receives only the headword and current specifications and writes a new entry under a temporary directory. Neither may inspect the other's output. Record wall-clock start/end, every distinct passage opened, lookup count, validation iterations, hard-gate failures, occurrence/sense/claim coverage, and independent-review findings. Both outputs then receive the same combined gate and blind semantic review. “Merge-ready” means zero hard failures plus independent KEEP.

No measured clean-rebuild result is claimed yet: existing speed-test reports explicitly omit manual review time, so treating their 2–3 second mechanical totals as rebuild time would be false. The experiment must run in temp copies while live r003 integration is frozen. Break-even is the smallest defect class for which median clean rebuild merge-ready time is lower than repair time *and* blind review finds no added semantic loss. Until those measurements exist, regeneration is not authorized.
