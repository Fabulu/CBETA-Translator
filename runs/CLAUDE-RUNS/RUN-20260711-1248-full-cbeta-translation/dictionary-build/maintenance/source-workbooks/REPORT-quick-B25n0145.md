# Source-batched attribution report: B25n0145

Scope: the two workbook rows and two complete cases in `quick-B25n0145.md`, spanning two disjoint entry IDs. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Exception-sheet result

Both complete cases were reviewed before the sheet was signed. One default survived exact-turn review and one required an override.

- `單傳`: Zhongfeng Mingben is the exact speaker in his own `天目中峰廣錄`, saying that Bodhidharma's coming west is called single transmission and direct pointing. Bodhidharma is the person discussed, not the speaker; the Bodhidharma default was corrected.
- `逢祖殺祖`: Zhongfeng Mingben is the exact speaker in his own address, describing the single raised sharp sword and the phrases “meet a buddha, kill the buddha; meet a patriarch, kill the patriarch.” The Zhongfeng default was retained.

## Exact changed IDs

- `t_643fab6ecc1b` 單傳
- `t_1bbc921aed44` 逢祖殺祖

## Before/after counts

Workbook-scoped two rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 2 |
| Unresolved exact actors | 2 | 0 |
| Attribution notes present | 2 | 2 |
| Notes naming `天目中峰廣錄` | 0 | 2 |
| Exact `zc.verify` successes | not rerun | 2/2 |

Full `audit_attribution.py --json` run over both modified entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 10 | 10 | 0 |
| Named occurrences | 1 | 3 | +2 |
| Unresolved actors | 9 | 7 | -2 |
| Notes missing exact speaker | 9 | 7 | -2 |
| Notes missing source title | 9 | 7 | -2 |
| Context-master links | 0 | 1 | +1 |
| Hard failures | 27 | 21 | -6 |

The 21 remaining audit failures belong to untouched, out-of-scope occurrences and notes in these entries. All 19 Chinese prose strings remain anchored.

## Mechanical checks

- Strict dry-run prepared 2/2 rows with zero failures; atomic application completed in 0.38 seconds and reported 2/2 prepared with zero failures.
- Both entry JSON files parse after editing.
- Both touched KWICs pass exact `zc.verify` with their stored source and starting-line anchors.
- Both touched notes contain `天目中峰廣錄` and Zhongfeng Mingben's exact name.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
