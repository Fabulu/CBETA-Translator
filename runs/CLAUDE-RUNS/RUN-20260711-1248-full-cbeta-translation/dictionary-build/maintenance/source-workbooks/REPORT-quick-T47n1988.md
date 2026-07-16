# Source-batched attribution report: T47n1988

Scope: the 2 candidate occurrences in `quick-T47n1988.md`, covering 2 complete cases and 2 entry IDs. This was attribution-only remediation.

## Exact-turn adjudication

Both draft title defaults were contradicted by complete-case review and were overridden.

- `垂示` occurs in an impersonal collection contents heading that counts 185 essential chamber sayings and 290 indications and substitute answers. The XML places it after the volume heading and collector byline and before the `室中語要` section heading; it is not a spoken turn by Yunmen Wenyan.
- `竪拂` occurs in the question of an unnamed monk asking Zifu Zhensui about the ancients picking up the mallet and raising the whisk. Zifu gives the following reply, and Yunmen Wenyan later comments on the raised exchange. The six-rung review and parallel passage do not name the monk.

## Changed IDs

- `t_e5259ce8bbf5` 垂示
- `t_df3e128ab4c1` 竪拂

## Counts

Workbook-scoped 2 rows:

| Measure | Before | After |
|---|---:|---:|
| Structured exact-actor states | 0 | 2 |
| Bare unresolved actors | 2 | 0 |
| Impersonal editorial headings | 0 | 1 |
| Reviewed-unnamed questioners | 0 | 1 |
| Exact `zc.verify` successes | not rerun | 2/2 |

Whole-source inventory for `T/T47/T47n1988.xml` across all entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences | 12 | 12 | 0 |
| Named occurrences | 10 | 10 | 0 |
| Structured actor states | 0 | 2 | +2 |
| Bare unresolved occurrences | 2 | 0 | -2 |

Full `audit_attribution.py --json` over the 2 modified entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 12 | 12 | 0 |
| Named occurrences | 5 | 5 | 0 |
| Reviewed-unnamed occurrences | 0 | 1 | +1 |
| Impersonal occurrences | 0 | 1 | +1 |
| Context-master links | 0 | 3 | +3 |
| Unresolved actors | 7 | 5 | -2 |
| Notes missing exact speaker/state | 7 | 6 | -1 |
| Notes missing source title | 9 | 7 | -2 |
| Hard failures | 37 | 31 | -6 |

The 31 inherited failures belong to untouched out-of-scope occurrences or prose in these entries: five unresolved actors, six notes missing a speaker/state, seven notes missing a source, four vague-attributor findings, and nine dangling-Chinese findings.

## Mechanical checks

- Signed compile: 2 rows, 2 overrides; `real 0.15s`.
- Strict dry-run: 2 prepared rows, 2 entries, zero failures; `real 0.20s`.
- Strict apply: 2 prepared rows, 2 entries, zero failures; `real 0.23s`.
- Focused gate: every exact `(entry ID, FromLb, KWIC)` was found once; intended actor states, notes, and context masters matched; stored `FromLb`/`ToLb` anchors passed `zc.verify` 2/2.
- Both modified entry JSON files parse through the audit and focused gate.
- No merge, commit, or push was performed.
