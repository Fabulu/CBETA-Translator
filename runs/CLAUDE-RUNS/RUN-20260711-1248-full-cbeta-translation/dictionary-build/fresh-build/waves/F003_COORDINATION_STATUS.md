# f003 coordination status

Updated 2026-07-15 after the 810-entry publication checkpoint.

## Accepted and published

- The authoritative fresh-build completion surface contains 810 `STATUS=done`
  entries.  Do not count a draft, formal hard pass, repair ledger, or author
  checkpoint as accepted.
- C801–850 and C851–900 are closed through independent exact-hash review and
  promotion.

## Quarantined author work

- A601–650: **0/50 accepted from this half**.  The latest independent report
  `f003-laneA-601-650-corrective-fresh-independent-exact-review.json` returns
  0 KEEP / 50 REVISE.  Its dominant defect is noncanonical link identity (120
  occurrence names and 172 context names); all 50 are in a strict-roster author
  pass plus the listed semantic repairs.
- A651–700: 44/50 accepted.  Six residual actor-vs-referent/action/event
  substitutions (文殊, 拄杖, 陞座, 侍者, 消息, 羅漢) are in author repair.
- B701–750: 20 prior KEEPs are accepted.  Thirty repaired rows have a formal
  author gate but still need fresh independent full-case exact-hash review.
- B751–800: 46/50 accepted.  Four residual exact-turn failures (端的, 宗乘,
  化主, 十二時) are in author repair.

## Promotion law

1. An author/formal gate never promotes its own work.
2. A fresh reviewer must inspect every stored occurrence in its complete case,
   bind the verdict to the current entry SHA-256, and answer who utters the
   headword rather than who owns the record.
3. Promote only exact-hash KEEPs with `promote_independent_keeps.py`.
4. Run `publish_fresh_checkpoint.py` immediately after each promoted cohort and
   require root/merged/index/shard counts to agree.
5. The progress site is manually deployed only when the project owner asks.
