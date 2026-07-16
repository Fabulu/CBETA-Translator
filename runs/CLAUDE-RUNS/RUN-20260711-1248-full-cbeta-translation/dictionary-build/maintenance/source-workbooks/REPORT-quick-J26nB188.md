# Source-batched attribution report: J26nB188

Scope: the single candidate occurrence in `quick-J26nB188.md`, covering one complete case and one entry ID. This was attribution-only remediation.

## Regenerated-triage check

The prepared workbook row matched the current `maintenance/attribution-triage-all.json` exactly for source, entry ID, term, case-cluster ID, line anchors, and KWIC. The current source inventory contains six additional selected rows outside this explicitly assigned one-row workbook; they were not changed in this wave.

## Exact-turn adjudication

The existing-note default Bodhidharma was contradicted and overridden with Ruibai Mingxue.

- `野狐禪` occurs in Ruibai Mingxue's own extended address. Ruibai calls the views of various quarters “wild-fox Chan” while criticizing attempts to define lineages through isolated formulas.
- Bodhidharma is the quoted case speaker whose `實無功德` reply to Emperor Wu of Liang is being misused by the targets of Ruibai's criticism. He does not speak the headword-bearing judgment.

## Changed ID

- `t_5f1287817ebd` 野狐禪

## Counts

Workbook-scoped one row:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 1 |
| Bare unresolved actors | 1 | 0 |
| Notes naming `入就瑞白禪師語錄` and Ruibai Mingxue | 0 | 1 |
| Exact `zc.verify` successes | not rerun | 1/1 |

Full `audit_attribution.py --json` over the modified entry:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 5 | 5 | 0 |
| Named occurrences | 0 | 1 | +1 |
| Deferred non-roster names | 0 | 1 | +1 |
| Context-master links | 0 | 1 | +1 |
| Unresolved actors | 5 | 4 | -1 |
| Notes missing exact speaker/state | 5 | 4 | -1 |
| Notes missing source title | 5 | 4 | -1 |
| Hard failures | 20 | 17 | -3 |

The 17 inherited failures belong to untouched out-of-scope occurrences or prose in this entry: four unresolved actors, four notes missing a speaker/state, four notes missing a source, two vague-attributor findings, and three dangling-Chinese findings.

## Mechanical checks

- Signed compile: 1 row, 1 override; `real 0.13s`.
- Strict dry-run: 1 prepared row, 1 entry, zero failures; `real 0.22s`.
- Strict apply: 1 prepared row, 1 entry, zero failures; `real 0.25s`.
- Focused gate: the exact `(entry ID, FromLb, KWIC)` was found once; intended master, note, and Bodhidharma context link matched; stored `FromLb`/`ToLb` anchors passed `zc.verify` 1/1.
- The modified entry and all workflow JSON files parse successfully.
- No merge, commit, or push was performed.
