# Source-batched attribution report: J37nB386

Scope: the single candidate occurrence in `quick-J37nB386.md`, covering one complete case and one entry ID. This was attribution-only remediation.

## Regenerated-triage check

The prepared workbook row matched the current regenerated `maintenance/attribution-triage-all.json` exactly for source, entry ID, term, case-cluster ID, line anchors, and KWIC. The current source inventory contains seven additional selected rows outside this explicitly assigned one-row workbook; they were not changed in this wave.

## Exact-turn adjudication

The inline-named default Deshan Xuanjian was contradicted and overridden with Yuan'an Feng.

- Deshan Xuanjian speaks only the embedded quotation, `我宗無語句，亦無一法與人`.
- `漏逗` occurs immediately afterward in Yuan'an Feng's appraisal, `全身漏逗了也`. Yuan'an uses the same comment pattern to appraise several earlier masters' sayings in his own hall address.

## Changed ID

- `t_898279a78ecf` 漏逗

## Counts

Workbook-scoped one row:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 1 |
| Bare unresolved actors | 1 | 0 |
| Notes naming `遠菴僼禪師語錄` and Yuan'an Feng | 0 | 1 |
| Exact `zc.verify` successes | not rerun | 1/1 |

Full `audit_attribution.py --json` over the modified entry:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 6 | 6 | 0 |
| Named occurrences | 2 | 3 | +1 |
| Deferred non-roster names | 0 | 1 | +1 |
| Context-master links | 0 | 1 | +1 |
| Unresolved actors | 4 | 3 | -1 |
| Notes missing exact speaker/state | 4 | 3 | -1 |
| Notes missing source title | 4 | 3 | -1 |
| Hard failures | 13 | 10 | -3 |

The 10 inherited failures belong to untouched out-of-scope occurrences or prose in this entry: three unresolved actors, three notes missing a speaker/state, three notes missing a source, and one vague-attributor finding.

## Mechanical checks

- Signed compile: 1 row, 1 override; `real 0.08s`.
- Strict dry-run: 1 prepared row, 1 entry, zero failures; `real 0.15s`.
- Strict apply: 1 prepared row, 1 entry, zero failures; `real 0.19s`.
- Focused gate: the exact `(entry ID, FromLb, KWIC)` was found once; intended master, note, and Deshan context link matched; stored `FromLb`/`ToLb` anchors passed `zc.verify` 1/1.
- The modified entry and all workflow JSON files parse successfully.
- No merge, commit, or push was performed.
