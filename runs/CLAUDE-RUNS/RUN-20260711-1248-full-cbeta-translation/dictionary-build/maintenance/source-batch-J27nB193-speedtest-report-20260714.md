# J27nB193 attribution speed test

Scope: 8 workbook rows in 8 complete cases across 7 entries from *Recorded Sayings of Chan Master Yinyuan* (`隱元禪師語錄`). Every complete case and exact turn was reviewed. The prebuilt defaults were treated only as drafts; every contradiction was recorded in the signed override sheet.

## Result

- Cases reviewed: **8/8**.
- Defaults accepted: **0/8**.
- Contradictions overridden: **8/8**.
- Reviewed decisions: **7** Yinyuan Longqi; **1** genuinely unnamed monk.
- Compiler, dry run, and apply: **8/8**, zero failures.
- Focused gate: **8/8** actor-complete, exact-decision matched, source-titled, and KWIC/anchor verified.
- Full touched-file audit: **7/7** JSON parse; **40/40** KWICs and exact anchor pairs verify.

## Default failure pattern

All eight defaults proposed Huangbo Xiyun because the section headings contain `黃檗`. In this source, `黃檗` names Huangbo Mountain and Wanfu Monastery, the setting of Yinyuan Longqi's own recorded sayings; it does not identify the Tang master Huangbo Xiyun.

Seven headword-bearing turns are Yinyuan Longqi's direct speech. In the remaining `象王` occurrence, an unnamed monk says `象王行處絕狐蹤`, and Yinyuan immediately drives him back with blows. The six-rung review and corpus parallels do not name that monk.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.35 |
| validated dry run | 0.41 |
| atomic apply | 0.94 |
| strict source gate | 0.77 |
| all-touched KWIC/anchor audit | 0.77 |
| **mechanical pipeline total** | **3.24** |

Manual complete-case review was not instrumented, so no estimate is invented. The compact workbook made the shared structural error immediately visible while still requiring turn-by-turn review for the monk's line.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 29 | 21 | -8 |
| named occurrences | 10 | 17 | +7 |
| reviewed unnamed non-masters | 0 | 1 | +1 |
| notes missing speaker | 28 | 21 | -7 |
| notes missing source | 30 | 23 | -7 |
| hard failures | 114 | 89 | -25 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
