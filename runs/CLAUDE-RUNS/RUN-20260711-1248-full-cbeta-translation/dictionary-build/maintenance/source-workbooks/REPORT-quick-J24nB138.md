# Source-batched attribution report: J24nB138

Scope: the single candidate occurrence in `quick-J24nB138.md`, covering one complete case and one entry ID. This was attribution-only remediation.

## Regenerated-triage check

The prepared workbook row matched the new `maintenance/attribution-triage-all.json` exactly for source, entry ID, term, case-cluster ID, line anchors, and KWIC. It is the source's sole currently selected unresolved row.

## Exact-turn adjudication

The Yunmen Wenyan default survived complete-case review; no exception override was required.

- `顧鑒咦` names Yunmen Wenyan's three-part encounter sequence: Yunmen looks toward a monk, says `鑒`, and when the monk prepares to answer says `咦`.
- The monastic community gives Yunmen the resulting label `顧鑒咦`; the unnamed monk is only the interlocutor who prepares to answer.
- The following sentence reports a later shortening of the label and does not change the actor of the stored sequence.

## Changed ID

- `t_461377f9b1da` 顧鑒咦

## Counts

Workbook-scoped one row:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 1 |
| Bare unresolved actors | 1 | 0 |
| Notes naming `雲門匡真禪師語錄` and Yunmen Wenyan | 0 | 1 |
| Exact `zc.verify` successes | not rerun | 1/1 |

Full `audit_attribution.py --json` over the modified entry:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 4 | 4 | 0 |
| Named occurrences | 1 | 2 | +1 |
| Unresolved actors | 3 | 2 | -1 |
| Notes missing exact speaker/state | 3 | 2 | -1 |
| Notes missing source title | 3 | 2 | -1 |
| Hard failures | 14 | 11 | -3 |

The 11 inherited failures belong to untouched out-of-scope occurrences or prose in this entry: two unresolved actors, two notes missing a speaker/state, two notes missing a source, three vague-attributor findings, and two dangling-Chinese findings.

## Mechanical checks

- Signed compile: 1 row, 0 overrides; `real 0.16s`.
- Strict dry-run: 1 prepared row, 1 entry, zero failures; `real 0.34s`.
- Strict apply: 1 prepared row, 1 entry, zero failures; `real 0.39s`.
- Focused gate: the exact `(entry ID, FromLb, KWIC)` was found once; intended master and note matched; stored `FromLb`/`ToLb` anchors passed `zc.verify` 1/1.
- The modified entry and all workflow JSON files parse successfully.
- No merge, commit, or push was performed.
