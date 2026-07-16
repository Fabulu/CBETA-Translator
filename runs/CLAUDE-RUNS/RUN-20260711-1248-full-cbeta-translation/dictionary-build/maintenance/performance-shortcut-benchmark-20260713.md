# Performance shortcut benchmark — 2026-07-13

## Apparatus-clean persistent cache

- Source corpus in the 462-text allowlist: approximately 204 MB of TEI XML.
- Cold normalization/cache build: 30.6 seconds.
- Persistent cache: 462 fingerprinted files, 112 MB in `/tmp/cbeta-zc-cache-v3`.
- Warm self-test/count run: 3.7 seconds, down from 30.6 seconds; results unchanged.
- Eight-query parity set (`和尚`, `棒`, `拄杖子`, `拂子`, `金`, `銀`, `鳥道`, `玄路`):
  - direct TEI: 23.589 seconds;
  - cached apparatus-clean corpus: 2.476 seconds;
  - exact hit/file/per-file parity: **PASS**;
  - speedup: approximately **9.5×**.

The cache key binds the normalizer version plus source size and nanosecond mtime. A missing, stale, corrupt, or unwritable cache falls back to direct source XML.

## Batch verification

- Nine accepted calibration entries: 111 occurrences.
- Direct TEI verification: 8.80 seconds.
- Cached batch verification: 0.65 seconds.
- Exact `ok`, `FromLb`, `ToLb`, and failure-list parity: **PASS**.
- Speedup: approximately **13.5×**.

## Cohort gate runner

- Three-entry calibration smoke test: 33 exact KWICs plus attribution, public-feedback, depth/sense, and forbidden-English gates.
- Result: hard PASS in 2.535 seconds.
- The generated bundle remains mechanical evidence only; semantic and exact-turn review are still required.

## Complete-case attribution packets

- A three-entry/49-occurrence checkpoint produced 49 complete structural units with zero missing KWIC locations.
- Packets show title, container class, title-owner candidates, preceding headers, whole enclosing case text, stored KWIC, and risk flags.
- Only guarded single-record cases without detected exclusions may become Tier-A candidates. The reviewer must still map the exact speaker turn.
- The first validation immediately caught a sponsor-list heading (`助刻姓氏`) that could have passed a weaker title-only rule; sponsor/editor headings were added to the fail-closed exclusions.
## Exact-turn packet preparation follow-up

- Fixed a raw-offset defect: deleting TEI apparatus before building a raw-position map shifted every later packet
  into the wrong XML location. Equal-length apparatus blanking preserves exact offsets.
- Prepared 坐禪, 世尊, and 平常心 as one 23-occurrence workbook.
- Before structural widening, only 20/23 extracted units contained their own stored KWIC.
- After widening through enclosing `lg`/`div`/head boundaries, **23/23 contain the stored KWIC**.
- The three-entry workbook plus machine packet generated in about five seconds on a warm cache; the separate baseline
  mechanical gate took 2.07 seconds.
- A 20-term count that previously flooded output with thousands of per-file rows now defaults to a concise
  `{term,hits,files}` result. Per-file histograms remain available explicitly.
