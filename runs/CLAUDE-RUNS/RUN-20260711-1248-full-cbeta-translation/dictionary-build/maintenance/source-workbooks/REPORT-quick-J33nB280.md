# Source-batched attribution report: J33nB280

Scope: the single candidate occurrence in `quick-J33nB280.md`, covering one complete address and one entry ID. This was attribution-only remediation.

## Regenerated-triage check

The prepared workbook row matched the current regenerated `maintenance/attribution-triage-all.json` exactly for source, entry ID, term, case-cluster ID, line anchors, and KWIC. The current source inventory contains two additional selected rows outside this explicitly assigned one-row workbook; they were not changed in this wave.

## Exact-turn adjudication

The co-located reviewed default Yunwai Ze survived complete-case review; no exception override was required.

- `死語` occurs in Yunwai Ze's sustained address diagnosing several contemporary teaching errors. Yunwai criticizes a class of teachers for treating the ancients' live functioning as dead sayings and appending merely matching phrases.
- The example formulas following `如云` illustrate the criticized method; they do not introduce another speaker for the headword-bearing judgment.

## Changed ID

- `t_432d8c4f7579` 死語

## Counts

Workbook-scoped one row:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 1 |
| Bare unresolved actors | 1 | 0 |
| Notes naming `神鼎雲外澤禪師語錄` and Yunwai Ze | 0 | 1 |
| Exact `zc.verify` successes | not rerun | 1/1 |

Full `audit_attribution.py --json` over the modified entry:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 4 | 4 | 0 |
| Named occurrences | 1 | 2 | +1 |
| Deferred non-roster names | 0 | 1 | +1 |
| Unresolved actors | 3 | 2 | -1 |
| Notes missing exact speaker/state | 3 | 2 | -1 |
| Notes missing source title | 3 | 2 | -1 |
| Hard failures | 14 | 11 | -3 |

The 11 inherited failures belong to untouched out-of-scope occurrences or prose in this entry: two unresolved actors, two notes missing a speaker/state, two notes missing a source, one vague-attributor finding, and four dangling-Chinese findings.

## Mechanical checks

- Signed compile: 1 row, 0 overrides; `real 0.15s`.
- Strict dry-run: 1 prepared row, 1 entry, zero failures; `real 0.25s`.
- Strict apply: 1 prepared row, 1 entry, zero failures; `real 0.48s`.
- Focused gate: the exact `(entry ID, FromLb, KWIC)` was found once; intended master and note matched; stored `FromLb`/`ToLb` anchors passed `zc.verify` 1/1.
- The modified entry and all workflow JSON files parse successfully.
- No merge, commit, or push was performed.
