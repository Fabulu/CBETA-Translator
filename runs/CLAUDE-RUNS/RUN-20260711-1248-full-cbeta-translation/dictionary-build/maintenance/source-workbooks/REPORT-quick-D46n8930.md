# Source-batched attribution report: D46n8930

Scope: the single regenerated-triage workbook row and complete case in `quick-D46n8930.md`, for the `百尺竿頭` entry. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Regenerated-triage confirmation

Before signing, the workbook row was matched against `maintenance/attribution-triage-all.json`. Entry ID, term, source, line anchor, KWIC, case-cluster offsets, source title, and the one selected-occurrence total all match the current regenerated triage exactly.

## Exception-sheet result

The complete case was reviewed before the sheet was signed. The proposed header default contradicted the exact turn and required a full override.

- `百尺竿頭`: the complete line says `所以長沙和尚道`—“therefore Master Changsha said”—before quoting the verse. Changsha Jingcen is therefore the exact quoted speaker of `雖然得入未為真百尺竿頭須進步`. The nearest heading `雪竇語錄中事` names the record being investigated, not Xuedou Chongxian as speaker of Changsha's verse.

## Exact changed ID

- `t_53da4e346a6f` 百尺竿頭

## Before/after counts

Workbook-scoped row:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 1 |
| Unresolved exact actors | 1 | 0 |
| Attribution notes present | 1 | 1 |
| Exact source-and-speaker notes | 0 | 1 |
| Exact `zc.verify` successes | not rerun | 1/1 |

Focused `audit_attribution.py --json` run over the modified entry:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 5 | 5 | 0 |
| Named occurrences | 3 | 4 | +1 |
| Unresolved actors | 2 | 1 | -1 |
| Notes missing exact speaker | 2 | 1 | -1 |
| Notes missing source title | 2 | 1 | -1 |
| Hard failures | 38 | 35 | -3 |

The 35 remaining audit failures belong to untouched, out-of-scope material in this entry, chiefly its quote-anchor backlog and the separate Wumenguan occurrence. Quote-anchor counters remain 44 Chinese prose strings, 17 anchored, and 27 dangling.

## Mechanical checks

- Strict compile and dry-run prepared 1/1 row with zero failures; atomic application prepared and applied 1/1 with zero failures.
- The entry JSON and all workbook decision/report JSON artifacts parse after editing.
- The touched KWIC passes exact `zc.verify` at its stored `0030a06` line anchor.
- `Changsha Jingcen` matches the roster's canonical `names[0]` spelling.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
