# Source-batched attribution report: J29nB233

Scope: the single regenerated-triage workbook row and complete case in `quick-J29nB233.md`, for the `鬱鬱黃花` entry. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Regenerated-triage confirmation

Before signing, the workbook row was matched against `maintenance/attribution-triage-all.json`. Entry ID, term, source, line range, KWIC, case-cluster offsets, source title, and the one selected-occurrence total all match the current regenerated triage exactly.

## Exception-sheet result

The complete case and its parallel passages were reviewed before the sheet was signed. The existing-note default assigned the respondent rather than the exact headword speaker and required a full override.

- `鬱鬱黃花`: the stored headword appears in the question `禪師何故不許…鬱鬱黃花無非般若`, addressed to Dazhu Huihai. Dazhu's reply begins only after `珠云`. The abbreviated J29 witness calls the questioner `華嚴座主`; the older parallel in the *Jingde Record of the Transmission of the Lamp* names him `講華嚴志座主`. The exact source-attested speaker is therefore Huayan Lecturer Zhi, not Dazhu Huihai. Dazhu remains linked as addressee and respondent.

## Exact changed ID

- `t_e931d476fd02` 鬱鬱黃花

## Before/after counts

Workbook-scoped row:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 1 |
| Unresolved exact actors | 1 | 0 |
| Attribution notes present | 1 | 1 |
| Exact source-and-speaker notes | 0 | 1 |
| Context-master links | 0 | 1 |
| Exact `zc.verify` successes | not rerun | 1/1 |

Focused `audit_attribution.py --json` run over the modified entry:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 5 | 5 | 0 |
| Named occurrences | 0 | 1 | +1 |
| Unresolved actors | 5 | 4 | -1 |
| Notes missing exact speaker | 5 | 4 | -1 |
| Notes missing source title | 5 | 4 | -1 |
| Context-master links | 0 | 1 | +1 |
| Hard failures | 15 | 12 | -3 |

The 12 remaining audit failures belong to untouched, out-of-scope occurrences in this entry. Quote-anchor counters remain mechanically clean: all 8 detected Chinese prose strings are anchored. Huayan Lecturer Zhi is source-attested but absent from the current roster, so the audit records one honest deferred non-roster speaker.

## Mechanical checks

- Strict compile and dry-run prepared 1/1 row with zero failures; atomic application prepared and applied 1/1 with zero failures.
- The entry JSON and all workbook decision/report JSON artifacts parse after editing.
- The touched KWIC passes exact `zc.verify` across its stored `0333a21`–`0333a24` range.
- Dazhu Huihai matches the roster's canonical `names[0]`; Huayan Lecturer Zhi is retained as a source-attested deferred non-roster speaker after the parallel-passage rung supplied his name.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
