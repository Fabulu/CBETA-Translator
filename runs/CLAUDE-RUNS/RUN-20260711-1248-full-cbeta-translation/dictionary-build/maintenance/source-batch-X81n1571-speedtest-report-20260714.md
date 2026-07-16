# X81n1571 attribution speed test

Scope: 5 workbook rows in 5 complete cases across 5 entries from the *Complete Book of the Five Lamps* (`五燈全書(第1卷-第33卷)`). Every complete case and exact turn was reviewed; defaults remained drafts until adjudication.

## Result

- Cases reviewed: **5/5**.
- Defaults accepted: **5/5**.
- Contradictions overridden: **0/5**.
- Compiler, dry run, and apply: **5/5**, zero failures.
- Strict source gate: **5/5** actor-complete.
- Focused audit: **5/5** exact decisions, source-title notes, KWICs, and anchors.
- Full touched-file audit: **5/5** JSON parse; **34/34** KWICs and exact anchor pairs verify.

The complete cases confirmed Nanyang Huizhong as the speaker of the lion-roar contrast, Baizhang Huaihai as the actor who breaks the sauce jars and returns to the abbot's quarters, Zhaozhou Congshen as the actor who closes the monks' hall door, Changsha Jingcen as the respondent defining `異類`, and Yangshan Huiji as the speaker describing his own whisk-based examination procedure. No default contradicted the exact turn.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.06 |
| validated dry run | 0.09 |
| atomic apply | 0.16 |
| strict source gate | 0.17 |
| all-touched KWIC/anchor audit | 0.18 |
| **mechanical pipeline total** | **0.66** |

Manual complete-case review was not instrumented, so no estimate is invented. An initial gate call used a nonexistent stale triage filename and was immediately rerun with the current all-source triage; it performed no entry writes and is excluded from the successful-pipeline timing above.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 26 | 21 | -5 |
| named occurrences | 8 | 13 | +5 |
| notes missing speaker | 32 | 27 | -5 |
| notes missing source | 32 | 27 | -5 |
| hard failures | 103 | 88 | -15 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
