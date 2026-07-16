# Source-batched attribution report: J39nB453

Scope: the single candidate occurrence in `quick-J39nB453.md`, covering one complete hall address and one entry ID. This was attribution-only remediation.

## Regenerated-triage check

The prepared workbook row matched the current regenerated `maintenance/attribution-triage-all.json` exactly for source, entry ID, term, case-cluster ID, line anchors, and KWIC. The current source inventory contains one additional selected row outside this explicitly assigned one-row workbook; it was not changed in this wave.

## Exact-turn adjudication

The header-derived default Dongshan Liangjie was contradicted and overridden with Yuanjie Ying.

- `那畔` occurs in Yuanjie Ying's opening hall address at Ancient Dongshan Anou Chan Monastery. Yuanjie says that a phrase from beyond Awesome Sound encompasses the whole and that the complete body appears with not a mote established.
- Dongshan in the venue heading names the monastery. It does not identify Dongshan Liangjie as the speaker of Yuanjie's address.

## Changed ID

- `t_dc81acde25fd` 那畔

## Counts

Workbook-scoped one row:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 1 |
| Bare unresolved actors | 1 | 0 |
| Notes naming `元潔瑩禪師語錄` and Yuanjie Ying | 0 | 1 |
| Exact `zc.verify` successes | not rerun | 1/1 |

Full `audit_attribution.py --json` over the modified entry:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 5 | 5 | 0 |
| Named occurrences | 1 | 2 | +1 |
| Deferred non-roster names | 0 | 1 | +1 |
| Unresolved actors | 4 | 3 | -1 |
| Notes missing exact speaker/state | 4 | 3 | -1 |
| Notes missing source title | 4 | 3 | -1 |
| Hard failures | 16 | 13 | -3 |

The 13 inherited failures belong to untouched out-of-scope occurrences or prose in this entry: three unresolved actors, three notes missing a speaker/state, three notes missing a source, and four dangling-Chinese findings.

## Mechanical checks

- Signed compile: 1 row, 1 override; `real 0.09s`.
- Strict dry-run: 1 prepared row, 1 entry, zero failures; `real 0.17s`.
- Strict apply: 1 prepared row, 1 entry, zero failures; `real 0.26s`.
- Focused gate: the exact `(entry ID, FromLb, KWIC)` was found once; intended master and note matched; stored `FromLb`/`ToLb` anchors passed `zc.verify` 1/1.
- The modified entry and all workflow JSON files parse successfully.
- No merge, commit, or push was performed.
