# Source-batched attribution report: T48n2005

Scope: the three workbook rows and three complete cases in `quick-T48n2005.md`, spanning three disjoint entry IDs. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Exception-sheet result

All three complete cases were reviewed before the sheet was signed. One default survived exact-turn review and two required overrides.

- `不落因果`: the unnamed old man and former abbot is the exact speaker recounting his former answer and five hundred wild-fox births. Baizhang Huaihai is the respondent who supplies the requested turning word, not the speaker of the stored span. All six attribution rungs were exhausted before recording the old man as reviewed unnamed.
- `草鞋`: Wumen Huikai's inline `無門曰` directly governs the headword phrase asking what Zhaozhou's straw sandal on his head means. The Wumen default was retained.
- `麻三斤`: `頌曰` introduces Wumen Huikai's capping verse after his prose comment. Dongshan Shouchu speaks “three pounds of hemp” in the preceding case, but Wumen is the author of the stored verse.

## Exact changed IDs

- `t_6f138f2956d8` 不落因果
- `t_fb9ab5bac0bf` 草鞋
- `t_ce2a5ef71afe` 麻三斤

## Before/after counts

Workbook-scoped three rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 2 |
| Reviewed unnamed exact actors | 0 | 1 |
| Unresolved exact actors | 3 | 0 |
| Attribution notes present | 3 | 3 |
| Notes naming `無門關` | 0 | 3 |
| Exact `zc.verify` successes | not rerun | 3/3 |

Full `audit_attribution.py --json` run over all three modified entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 15 | 15 | 0 |
| Named occurrences | 3 | 5 | +2 |
| Reviewed unnamed occurrences | 1 | 2 | +1 |
| Unresolved actors | 11 | 8 | -3 |
| Notes missing exact speaker/actor state | 12 | 9 | -3 |
| Notes missing source title | 11 | 8 | -3 |
| Context-master links | 2 | 5 | +3 |
| Hard failures | 82 | 73 | -9 |

The 73 remaining audit failures belong to untouched, out-of-scope occurrences and prose in these entries. Quote-anchor counters are unchanged: 70 Chinese prose strings, 25 anchored, 45 dangling.

## Speed and mechanical checks

- The final strict dry-run prepared 3/3 rows with zero failures; atomic application completed in 0.25 seconds and reported 3/3 prepared with zero failures.
- All three entry JSON files parse after editing.
- All three touched KWICs pass exact `zc.verify` with their stored source and starting-line anchors.
- All three touched notes contain `無門關` and the exact named actor or reviewed-unnamed actor label.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
