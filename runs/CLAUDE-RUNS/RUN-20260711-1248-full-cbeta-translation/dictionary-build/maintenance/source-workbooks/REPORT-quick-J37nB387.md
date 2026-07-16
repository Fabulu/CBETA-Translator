# Source-batched attribution report: J37nB387

Scope: the single regenerated-triage workbook row and complete case in `quick-J37nB387.md`, for the `玄關` entry. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Regenerated-triage confirmation

Before signing, the workbook row was matched against `maintenance/attribution-triage-all.json`. Entry ID, term, source, line anchor, KWIC, case-cluster offsets, source title, and the one selected-occurrence total all match the current regenerated triage exactly.

## Exception-sheet result

The complete case and source container were reviewed before the sheet was signed. The prepared `Danxia Tianran` default was rejected as a monastery-name false positive.

- `玄關`: an unnamed monk asks about the day's release from the retreat restriction; Gusu Zun answers `蹋倒玄關無障礙` ("Kick down the dark barrier—without obstruction") and then answers the monk's follow-up. The source is *The Recorded Sayings of Chan Master Gusu Zun* (`古宿尊禪師語錄`). Its section heading `住鄧州丹霞禪寺語錄` says that Gusu speaks while residing at Danxia Chan Monastery in Dengzhou; it does not identify Danxia Tianran as the speaker. Gusu Zun is therefore the exact respondent.

## Exact changed ID

- `t_a8b4f101d192` 玄關

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
| Occurrences audited | 6 | 6 | 0 |
| Named occurrences | 1 | 2 | +1 |
| Unresolved actors | 4 | 3 | -1 |
| Notes missing exact speaker | 4 | 3 | -1 |
| Notes missing source title | 4 | 3 | -1 |
| Deferred non-roster speakers | 1 | 1 | 0 |
| Hard failures | 12 | 9 | -3 |

The nine remaining audit failures belong to untouched, out-of-scope occurrences `o1`, `o4`, and `o6`. Gusu Zun is source-attested but absent from the current roster, so the audit retains one honest deferred non-roster speaker rather than treating this resolved exact actor as anonymous.

## Mechanical checks

- Strict compile and dry-run prepared 1/1 row with zero failures; atomic application prepared and applied 1/1 with zero failures.
- The entry JSON and all workbook decision/report JSON artifacts parse after editing.
- The touched KWIC passes exact `zc.verify` at its stored `0417c22`–`0417c24` line range.
- The complete encounter assigns the headword turn to Gusu Zun and keeps the unnamed monk as questioner, not exact actor.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
