# Source-batched attribution report: X70n1397

Scope: the two regenerated-triage workbook rows and complete cases in `quick-X70n1397.md`, spanning the `孤明` and `鼻孔` entries. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Regenerated-triage confirmation

Before signing, both workbook rows were matched against `maintenance/attribution-triage-all.json`. Entry IDs, terms, source, line anchors, KWICs, case-cluster offsets, and the two selected-occurrence total all match the current regenerated triage exactly.

## Exception-sheet result

Both complete cases were reviewed before the sheet was signed. One default survived exact-turn review and one required an override.

- `孤明`: Xueyan Zuqin is the exact speaker in his own hall address at Yuanzhou Yangshan Chan Monastery. The section heading `袁州仰山禪寺語錄` names the monastery, not Yangshan Huiji as speaker, so the Yangshan default was corrected.
- `鼻孔`: Xueyan Zuqin is the exact speaker in his own instruction headed `示轉菴圓上人`, saying that when one gropes and finds the old nostrils, they are still on one's face. The Xueyan default was retained.

## Exact changed IDs

- `t_560356022866` 孤明
- `t_ea138c7335d3` 鼻孔

## Before/after counts

Workbook-scoped two rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 2 |
| Unresolved exact actors | 2 | 0 |
| Attribution notes present | 2 | 2 |
| Notes naming `雪巖祖欽禪師語錄` | 1 | 2 |
| Exact `zc.verify` successes | not rerun | 2/2 |

Full `audit_attribution.py --json` run over both modified entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 14 | 14 | 0 |
| Named occurrences | 6 | 8 | +2 |
| Unresolved actors | 8 | 6 | -2 |
| Notes missing exact speaker | 11 | 9 | -2 |
| Notes missing source title | 7 | 6 | -1 |
| Hard failures | 31 | 26 | -5 |

The 26 remaining audit failures belong to untouched, out-of-scope occurrences and prose in these entries. Quote-anchor counters are unchanged: 32 Chinese prose strings, 28 anchored, 4 dangling.

## Mechanical checks

- Strict dry-run prepared 2/2 rows with zero failures; atomic application completed in 0.23 seconds and reported 2/2 prepared with zero failures.
- Both entry JSON files parse after editing.
- Both touched KWICs pass exact `zc.verify` with their stored source and starting-line anchors.
- Both touched notes contain `雪巖祖欽禪師語錄` and Xueyan Zuqin's exact name.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
