# Source-batched attribution report: J29nB223

Scope: the single candidate occurrence in `quick-J29nB223.md`, covering one complete case and one entry ID. This was attribution-only remediation.

## Regenerated-triage check

The prepared workbook row matched the current `maintenance/attribution-triage-all.json` exactly for source, entry ID, term, case-cluster ID, line anchors, and KWIC. The current source inventory contains two additional selected rows outside this explicitly assigned one-row workbook; they were not changed in this wave.

## Exact-turn adjudication

The existing-note default Bodhidharma was contradicted and overridden with Shanhui.

- `釋迦老子` occurs in Shanhui's own small convocation. After raising his whisk, Shanhui challenges the assembly to speak a sentence where Shakyamuni Buddha could not speak and take a step where Bodhidharma could not go.
- Shakyamuni Buddha and Bodhidharma are the two invoked masters whose limits frame Shanhui's challenge. Neither speaks the stored sentence.

## Changed ID

- `t_77821881a767` 釋迦老子

## Counts

Workbook-scoped one row:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 1 |
| Bare unresolved actors | 1 | 0 |
| Notes naming `山暉禪師語錄` and Shanhui | 0 | 1 |
| Exact `zc.verify` successes | not rerun | 1/1 |

Full `audit_attribution.py --json` over the modified entry:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 7 | 7 | 0 |
| Named occurrences | 4 | 5 | +1 |
| Deferred non-roster names | 1 | 2 | +1 |
| Context-master links | 0 | 2 | +2 |
| Unresolved actors | 3 | 2 | -1 |
| Notes missing exact speaker/state | 6 | 5 | -1 |
| Notes missing source title | 7 | 6 | -1 |
| Hard failures | 17 | 14 | -3 |

The 14 inherited failures belong to untouched out-of-scope occurrences or prose in this entry: two unresolved actors, five notes missing a speaker/state, six notes missing a source, and one vague-attributor finding.

## Mechanical checks

- Signed compile: 1 row, 1 override; `real 0.19s`.
- Strict dry-run: 1 prepared row, 1 entry, zero failures; `real 0.30s`.
- Strict apply: 1 prepared row, 1 entry, zero failures; `real 0.35s`.
- Focused gate: the exact `(entry ID, FromLb, KWIC)` was found once; intended master, note, and both invoked-master context links matched; stored `FromLb`/`ToLb` anchors passed `zc.verify` 1/1.
- The modified entry and all workflow JSON files parse successfully.
- No merge, commit, or push was performed.
