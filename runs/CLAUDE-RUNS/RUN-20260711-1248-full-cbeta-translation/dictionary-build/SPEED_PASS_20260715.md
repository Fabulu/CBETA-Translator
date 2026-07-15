# Dictionary build speed passes — 2026-07-15

## Measured bottlenecks

1. The dominant avoidable cost was reconstructing the same full cases separately during authoring,
   gating, and independent review.
2. The structural attribution-packet phase consumed 125.309 seconds of a 204.673-second 30-entry gate.
3. Custom focused gates checked KWIC + `FromLb` but omitted `ToLb`; this deferred 21 cheap span repairs
   until independent review.
4. Reusing long-lived worker threads caused stale-task/report-only turns. Fresh bounded decile workers
   produce more concrete edits per turn.

## Pass 1: shared hash-bound case packet

`prepare_decile_case_packet.py` now extracts exact complete spans, 10k context, source title, independent
work ID, line-anchored headings, current actor fields, and speech/paratext risk once per decile. It can
atomically refresh both entry and worksheet spans before compilation.

Benchmark on f004 A906–915 (10 entries, 64 cases):

- exact-position heading prototype: exceeded 170 seconds and was terminated;
- optimized line-anchor packet, cold: 27.93 seconds wall (26.286 seconds generator time);
- exact-hash cache hit: 2.05 seconds wall;
- stored-span changes: 0; packet size and evidence content remained hash-bound.

## Pass 2: gate scheduling and rejected prototype

An attempted local raw-window optimization for `attribution_packet.py` was benchmarked and rejected:

- existing source-sorted raw mapper: 64.43 seconds for 64 cases;
- prototype: 72.57 seconds for the same cases.

The prototype was removed. The retained improvement is scheduling: use the shared case packet throughout
authoring/review, run the heavier structural attribution packet once at final decile gating, and reuse its
existing exact-entry-hash cache.

Both B focused-gate templates now compare `FromLb` and `ToLb`. The guide makes complete-span equality and
the shared-packet/final-gate schedule mandatory. These changes preserve the final dictionary schema and
quality gates; they eliminate repeated transport work rather than eliminating reading.

## Pass 3: protect independent-review capacity

`pre_review_decile.py` runs the existing production cohort gate with only the expensive final structural
packet deferred. Drafts with span, attribution-canary, generic-prose, public-feedback, depth, count, work,
corpus, or forbidden-language failures cannot be dispatched. The author repairs cheap failures first; the
independent reviewer then spends the turn on the non-mechanical question: what the complete case says and
who, if anyone, utters the headword.

## Pass 4: coalesced publication

Promotion remains immediate and hash-bound, but full root/index/shard regeneration is performed once per
settled parallel review round or explicit checkpoint rather than once per tiny partial result. This removes
repeated whole-dictionary scans while preserving the same final artifacts and the checkpoint agreement gate.

## Pass 5: idempotence regression gate

Independent review twice found duplicated opening sentences introduced by repair/compile helpers. The public
feedback auditor now hard-fails a consecutively duplicated first sentence, with regression tests. This moves
the defect from expensive semantic rereview to the author's cheap pre-review pass.

## Pass 6: immutable approved snapshots

A broad checkpoint helper mutated 17 independently accepted entries and forced 92 full cases back through
review. Promotion now stores exact approved entry and worksheet bytes under the reviewed SHA and records the
snapshot in the root verdict. `restore_root_approved.py` can restore an accidentally changed current KEEP in
seconds; it cannot restore a verdict that a later independent REVISE has superseded. This preserves semantic
review while eliminating repeat work caused solely by out-of-scope mass writes.

## Pass 7: source-first reading order

Shared case packets now include `sourceGroups`: references are grouped by `RelPath` and physical line order,
with the exact title and independent work ID emitted once per source. Closed actor-role, status, and
different-referent vocabularies travel in the same packet. The ordinary entry-first view remains intact and
final output is unchanged; authors can read one source container continuously instead of repeatedly switching
books, while still deciding every utterer from the complete case.
