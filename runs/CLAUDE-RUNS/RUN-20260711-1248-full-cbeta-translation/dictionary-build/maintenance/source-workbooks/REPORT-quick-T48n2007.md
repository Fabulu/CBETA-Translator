# Source-batched attribution report: T48n2007

Scope: the two workbook rows and two complete cases in `quick-T48n2007.md`, both in the `無相戒` entry. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Exception-sheet result

Both complete cases were reviewed before the sheet was signed. Both Huineng defaults survived exact-turn review.

- At `0337a08`, the title and opening narrative explicitly name Huineng as ascending the high seat, expounding the teaching, and conferring the formless precepts.
- At `0339a12`, Huineng directly tells the audience to receive the formless precepts from within themselves and instructs them to repeat after his own mouth (`逐惠能口道`).

## Exact changed ID

- `t_283fc1afb0ca` 無相戒

## Before/after counts

Workbook-scoped two rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 2 |
| Unresolved exact actors | 2 | 0 |
| Attribution notes present | 2 | 2 |
| Notes naming the full Platform Record title | 0 | 2 |
| Exact `zc.verify` successes | not rerun | 2/2 |

Full `audit_attribution.py --json` run over the modified entry:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 5 | 5 | 0 |
| Named occurrences | 2 | 4 | +2 |
| Unresolved actors | 3 | 1 | -2 |
| Notes missing exact speaker | 3 | 1 | -2 |
| Notes missing source title | 3 | 1 | -2 |
| Hard failures | 10 | 4 | -6 |

The four remaining audit failures belong to one untouched, out-of-scope occurrence and one prose vague-attributor flag in this entry.

## Mechanical checks

- Strict dry-run prepared 2/2 rows with zero failures; atomic application completed in 0.21 seconds and reported 2/2 prepared with zero failures.
- The entry JSON parses after editing.
- Both touched KWICs pass exact `zc.verify` with their stored source and starting-line anchors.
- Both touched notes contain the full source title and Huineng's exact name.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
