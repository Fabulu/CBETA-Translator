# Quick source-batched exact-actor wave

This wave follows the current guide and `ATTRIBUTION_FIX.md`. It accelerates retrieval, not judgment. Read every
complete case in the assigned workbook, map the exact turn, then update every listed occurrence and its
`AttributionNote`. A title/header owner is only a candidate. Every master must be named; reviewed-unnamed remains
limited to a genuinely unnamed non-master after the six-rung ladder, and impersonal grammar needs explicit evidence.

The three assignments have disjoint entry IDs, so workers may edit them concurrently. These are attribution-only
repairs: do not claim a whole entry is substantively remediated unless all of that entry's other occurrences and
current semantic gates have also been reviewed. Run exact KWIC and attribution audits on every modified file. Do not
merge, commit, or push.

## Worker A — Hongzhi's Extensive Record

- Source: `T/T48/T48n2001.xml`
- Workbook: `quick-T48n2001.md`
- 23 unresolved occurrences across 18 entries; 22 guarded single-record candidates and one named-inline candidate.

## Worker B — Yongjue Yuanxian's Extensive Record

- Source: `X/X72/X72n1437.xml`
- Workbook: `quick-X72n1437.md`
- 18 unresolved occurrences across 15 entries; 17 guarded single-record candidates and one header candidate.

## Worker C — Wuyi Yuanlai's Extensive Record

- Source: `X/X72/X72n1435.xml`
- Workbook: `quick-X72n1435.md`
- 15 unresolved occurrences across 13 entries, collapsed to 14 complete cases; all are guarded single-record candidates.

## Cohort acceptance

- Confirm the three modified entry-ID sets remain disjoint.
- Verify every changed KWIC exactly with `zc`/`zc_batch`.
- Run `audit_attribution.py` on all changed files; report before/after unresolved actor and note counts.
- Spot-check title owner versus exact speaker turn in every complete case; do not mass-fill by title.
- Merge only after root integrates all three reports and the combined mechanical gate passes.
