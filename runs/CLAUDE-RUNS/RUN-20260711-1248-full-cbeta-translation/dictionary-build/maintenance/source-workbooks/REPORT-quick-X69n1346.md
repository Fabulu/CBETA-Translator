# Source-batched attribution report: X69n1346

Scope: the two regenerated-triage workbook rows and complete cases in `quick-X69n1346.md`, spanning the `立地` and `本地` entries. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Regenerated-triage confirmation

Before signing, both workbook rows were matched against `maintenance/attribution-triage-all.json`. Entry IDs, terms, source, line anchors, KWICs, case-cluster offsets, and the two selected-occurrence total all match the regenerated triage exactly.

## Exception-sheet result

Both complete cases were reviewed before the sheet was signed. Both Xuefeng Yicun defaults required overrides. In `雪峰慧空禪師語錄`, Xuefeng is the monastery association and Huikong is the record owner; the source biography explicitly identifies him as `福州雪峯東山慧空禪師`.

- `立地`: Xuefeng Huikong is the exact speaker in his own hall address, telling the assembly that if they were to awaken on the spot, birth and death and even the ten directions of empty space would disappear.
- `本地`: Xuefeng Huikong is the exact speaker in his own memorial hall address, stating that his second purpose is to display the deceased patron's native-ground scenery, then marking the air with the whisk.

Xuefeng Huikong is source-attested but remains outside the current roster; his exact name is preserved rather than replaced with Xuefeng Yicun or nulled.

## Exact changed IDs

- `t_73fb9441f4fb` 立地
- `t_bf4ad761840f` 本地

## Before/after counts

Workbook-scoped two rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 2 |
| Unresolved exact actors | 2 | 0 |
| Attribution notes present | 2 | 2 |
| Notes naming `雪峰慧空禪師語錄` | 0 | 2 |
| Exact `zc.verify` successes | not rerun | 2/2 |

Full `audit_attribution.py --json` run over both modified entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 14 | 14 | 0 |
| Named occurrences | 0 | 2 | +2 |
| Unresolved actors | 14 | 12 | -2 |
| Notes missing exact speaker | 14 | 12 | -2 |
| Notes missing source title | 14 | 12 | -2 |
| Deferred non-roster exact names | 0 | 2 | +2 |
| Hard failures | 44 | 38 | -6 |

The 38 remaining audit failures belong to untouched, out-of-scope occurrences and prose in these entries.

## Mechanical checks

- Strict dry-run prepared 2/2 rows with zero failures; atomic application completed in 0.36 seconds and reported 2/2 prepared with zero failures.
- Both entry JSON files parse after editing.
- Both touched KWICs pass exact `zc.verify` with their stored source and starting-line anchors.
- Both touched notes contain `雪峰慧空禪師語錄` and Xuefeng Huikong's exact name.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
