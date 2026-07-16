# Source-batched attribution report: B25n0143

Scope: the single candidate occurrence in `quick-B25n0143.md`, covering one complete modern-prose paragraph and one entry ID. This was attribution-only remediation.

## Regenerated-triage check

The prepared workbook row matched the current `maintenance/attribution-triage-all.json` exactly for source, entry ID, term, case-cluster ID, line anchors, and KWIC. It is the source's sole currently selected unresolved row.

## Exact-turn adjudication

The title-derived Shenhui default was contradicted and overridden with Hu Shi.

- `律` occurs inside `音律不差` in Hu Shi's modern scholarly postscript on the five-watch songs. Hu Shi contrasts acceptable musical regulation with unreadable wording and lack of literary value.
- The inline note after the following phrase `下語用字，全不可讀` identifies that wording judgment as using Shen Yifu's *Yuefu Zhimi*; the headword-bearing `音律不差` remains in Hu Shi's framing prose.
- Shenhui is the subject of the manuscript collection, not the author of this twentieth-century paragraph.

## Changed ID

- `t_c0a6177c9c44` 律

## Counts

Workbook-scoped one row:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 1 |
| Bare unresolved actors | 1 | 0 |
| Notes naming `神會和尚語錄的第三個敦煌寫本：南陽和尚問答雜徵義（劉澄集）` and Hu Shi | 0 | 1 |
| Exact `zc.verify` successes | not rerun | 1/1 |

Full `audit_attribution.py --json` over the modified entry:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 9 | 9 | 0 |
| Named occurrences | 5 | 6 | +1 |
| Deferred non-roster names | 1 | 2 | +1 |
| Context-master links | 1 | 2 | +1 |
| Unresolved actors | 4 | 3 | -1 |
| Notes missing exact speaker/state | 7 | 6 | -1 |
| Notes missing source title | 7 | 6 | -1 |
| Hard failures | 19 | 16 | -3 |

The 16 inherited failures belong to untouched out-of-scope occurrences or prose in this entry: three unresolved actors, six notes missing a speaker/state, six notes missing a source, and one vague-attributor finding.

## Mechanical checks

- Signed compile: 1 row, 1 override; `real 1.05s`.
- Strict dry-run: 1 prepared row, 1 entry, zero failures; `real 1.15s`.
- Strict apply: 1 prepared row, 1 entry, zero failures; `real 1.58s`.
- Focused gate: the exact `(entry ID, FromLb, KWIC)` was found once; intended master, note, and Shenhui context link matched; stored `FromLb`/`ToLb` anchors passed `zc.verify` 1/1.
- The modified entry and all workflow JSON files parse successfully.
- No merge, commit, or push was performed.
