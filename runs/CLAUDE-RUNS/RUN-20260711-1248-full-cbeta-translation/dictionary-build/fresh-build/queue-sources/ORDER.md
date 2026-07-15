# Frozen queue-source precedence

Process every source in this order and preserve every file's internal order.
Do not remove a duplicate: record its link to the earliest canonical row and
continue. Counts are recomputed against the current 479-file / 473-work corpus.

1. `WAVE_PLAN.md` — original curated core through b036, including the initial
   prebuilt-headword preface as a required-reference set.
2. `REQUESTED_TERMS.md` and `REQUESTED_BUILD_PLAN.md` — explicit user requests,
   including later hard-gate additions and their stated priority families.
3. `NEXT500_BUILD_PLAN.md` and its discovery/reference files.
4. `NEXT100_BUILD_PLAN.md` and `NEXT100_SAYINGS_CANDIDATES.md` — sayings,
   idioms, material explanations, and inherited interpretation leads.
5. `RELATED_INVESTIGATION_BACKLOG.md` plus `NEXT500_RELATED_POOL.tsv` — all
   720 retained investigation leads, with keep/revise/reject disposition.
6. `IRIYA_FINAL_BUILD_PLAN.md`, `IRIYA_SAYINGS_QUEUE.md`, and
   `CODEX_IRIYA_QUEUE_PROMPT.md` — 1,969 deduplicated candidates in ranked
   revision-2 order; headword-selection signal only, never definition source.

The copied files beside this document are immutable queue snapshots. Their
checksums are recorded in `SHA256SUMS`.

