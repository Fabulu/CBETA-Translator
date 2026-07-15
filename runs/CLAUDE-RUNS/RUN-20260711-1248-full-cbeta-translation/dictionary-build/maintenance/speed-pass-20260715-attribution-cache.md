# Speed pass — attribution packet reuse (2026-07-15)

## Bottleneck confirmed

The earlier A918–925 profile placed 68.4% of the mechanical gate time in
attribution-packet generation. `attribution_packet.py` already computed a
prose-insensitive evidence fingerprint, but it did not read or reuse its own
prior output. Consequently, even a prose-only revision rebuilt every complete
TEI case and raw-position map.

## Implemented

`attribution_packet.py` now reuses prior packets entry-atomically when the
stored evidence fingerprint is unchanged. One altered occurrence invalidates
that entry only; other unchanged entries are carried forward. The output now
reports reused/rebuilt entry and occurrence counts. Full-case reading remains
mandatory, and no actor is inferred by the cache.

## Benchmark

Measured on three real B1073–1080 entries (17 occurrences):

- cold packet generation: 2.96 seconds
- unchanged warm rerun: 0.34 seconds
- speedup: 8.7x
- warm integrity: 3/3 entries and 17/17 occurrences reused; zero rebuilt

Python compilation and the existing source-grouping test pass. No entry,
worksheet, promotion, merge, or site artifact was changed by this speed pass.

## Next highest-return controls

1. Keep one stable attribution output path per cohort so the new cache can hit;
   version-suffixed output names throw the cache away.
2. Before prose, reject overlapping occurrences from the same work and passage.
   The B1091 repair found two stored spans from one Zhufeng passage masquerading
   as depth.
3. Bind every occurrence to a proposed sense before counting depth. B1073 mixed
   literal grass with lineage stock, creating late semantic repair.
4. Run a claim-entailment canary before independent review: verbs such as
   `embody`, `test`, `locate`, and explicit negatives such as `the sources do
   not` require named occurrence keys. This catches expensive prose overreach
   without replacing human judgment.
5. Retain source-first reading packets. Compilation and `zc.verify` are already
   sub-second/cheap relative to full-case semantic and actor reading; weakening
   either gate would save little and recreate known errors.
