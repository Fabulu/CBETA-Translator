# Source-batched attribution report: J32nB272

Scope: the two workbook rows and two complete cases in `quick-J32nB272.md`, spanning two disjoint entry IDs. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Exception-sheet result

Both complete cases were reviewed before the sheet was signed. One default survived exact-turn review and one required an override.

- `頑空`: Hongzhi Zhengjue is the exact speaker in his own address to the assembly, warning listeners not to recognize stubborn emptiness as themselves. The Hongzhi default was retained.
- `兩堂爭貓`: the headword belongs to the narrative statement that the two halls contended over the cat. The unnamed monks of the two halls are the collective actors; Hongzhi is the later verse commentator and record owner, while Nanquan Puyuan and Zhaozhou Congshen are named contextual actors in the case. All six attribution rungs were exhausted before recording the two groups as reviewed unnamed.

## Exact changed IDs

- `t_31575552ede2` 頑空
- `t_b669cf104663` 兩堂爭貓

## Before/after counts

Workbook-scoped two rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 1 |
| Reviewed unnamed exact actors | 0 | 1 |
| Unresolved exact actors | 2 | 0 |
| Attribution notes present | 2 | 2 |
| Notes naming `明州天童景德禪寺宏智覺禪師語錄` | 0 | 2 |
| Exact `zc.verify` successes | not rerun | 2/2 |

Full `audit_attribution.py --json` run over both modified entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 10 | 10 | 0 |
| Named occurrences | 4 | 5 | +1 |
| Reviewed unnamed occurrences | 0 | 1 | +1 |
| Unresolved actors | 6 | 4 | -2 |
| Notes missing exact speaker/actor state | 10 | 8 | -2 |
| Notes missing source title | 10 | 8 | -2 |
| Context-master links | 0 | 3 | +3 |
| Hard failures | 31 | 25 | -6 |

The 25 remaining audit failures belong to untouched, out-of-scope occurrences and notes in these entries. Quote-anchor counters are unchanged: 20 Chinese prose strings, 15 anchored, 5 dangling.

## Mechanical checks

- Strict dry-run prepared 2/2 rows with zero failures; atomic application completed in 0.37 seconds and reported 2/2 prepared with zero failures.
- Both entry JSON files parse after editing.
- Both touched KWICs pass exact `zc.verify` with their stored source and starting-line anchors.
- Both touched notes contain the full source title and the exact actor name or reviewed-unnamed actor label.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
