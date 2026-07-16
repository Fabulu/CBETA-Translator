# Source-batched attribution report: J26nB183

Scope: the four workbook rows and four complete cases in `quick-J26nB183.md`, spanning four disjoint entry IDs. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Exception-sheet result

All four complete cases were reviewed before the sheet was signed. All four defaults required overrides. The shortcut parsed `雪竇` in `雪竇石奇禪師語錄` as Xuedou Chongxian, but here Xuedou is the monastery and the record owner is Shiqi Tongyun. The TEI header says `清 通雲說`, while the source biography states `法諱通雲，字石奇` and `師諱通雲，號石奇`.

- `現成公案`: Shiqi Tongyun is the exact speaker in his own hall address.
- `殺活`: the stored headword occurs in an unnamed monk's question; Shiqi Tongyun is the respondent and record owner, not the exact speaker. All six attribution rungs were exhausted before recording the monk as reviewed unnamed.
- `活人劍`: Shiqi Tongyun is the author of the verse headed `關中次韻`.
- `室中`: Shiqi Tongyun is the author of the verse headed `寄友`.

Shiqi Tongyun is source-attested but remains outside the current roster; his exact name is preserved rather than replaced with Xuedou Chongxian or nulled.

## Exact changed IDs

- `t_408abe2e38ca` 現成公案
- `t_26d1f4bf3890` 殺活
- `t_e6eb14b6c1ca` 活人劍
- `t_b96051d06349` 室中

## Before/after counts

Workbook-scoped four rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 3 |
| Reviewed unnamed exact actors | 0 | 1 |
| Unresolved exact actors | 4 | 0 |
| Attribution notes present | 4 | 4 |
| Notes naming `雪竇石奇禪師語錄` | 3 | 4 |
| Exact `zc.verify` successes | not rerun | 4/4 |

Full `audit_attribution.py --json` run over all four modified entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 25 | 25 | 0 |
| Named occurrences | 7 | 10 | +3 |
| Reviewed unnamed occurrences | 0 | 1 | +1 |
| Unresolved actors | 18 | 14 | -4 |
| Notes missing exact speaker/actor state | 21 | 17 | -4 |
| Notes missing source title | 13 | 10 | -3 |
| Deferred non-roster exact names | 2 | 5 | +3 |
| Hard failures | 96 | 85 | -11 |

The 85 remaining audit failures belong to untouched, out-of-scope occurrences and prose in these entries. Quote-anchor counters are unchanged: 85 Chinese prose strings, 48 anchored, 37 dangling.

## Speed and mechanical checks

- Signed-sheet compilation completed in 0.11 seconds; strict dry-run completed in 0.25 seconds and prepared 4/4 rows with zero failures.
- Atomic application completed in 0.27 seconds and reported 4/4 prepared with zero failures.
- All four entry JSON files parse after editing.
- All four touched KWICs pass exact `zc.verify` with their stored source and starting-line anchors.
- All four touched notes contain `雪竇石奇禪師語錄` and the exact named actor or reviewed-unnamed actor state.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
