# T48n2004 attribution speed test

Scope: 6 workbook rows in 6 complete cases across 6 entries from the *Book of Serenity* (`萬松老人評唱天童覺和尚頌古從容庵錄`). Every complete case and exact turn was reviewed; defaults remained drafts until adjudication.

## Result

- Cases reviewed: **6/6**.
- Defaults accepted: **1/6**.
- Contradictions overridden: **5/6**.
- Compiler, dry run, and apply: **6/6**, zero failures.
- Strict source gate: **6/6** actor-complete.
- Focused audit: **6/6** exact decisions, source-title notes, KWICs, and anchors.
- Full touched-file audit: **6/6** JSON parse; **32/32** KWICs and exact anchor pairs verify.

## Overrides

| Term | Default | Reviewed decision | Why the default failed |
|---|---|---|---|
| 頌古 | Yelü Chucai | Wansong Xingxiu | The passage is Wansong's signed letter to Yelü, not Yelü's prose. |
| 不落因果 | Baizhang Huaihai | unnamed old man | The old man reports his own earlier answer; Baizhang supplies the different later turning word. |
| 猫兒 | Nanquan Puyuan | Wansong Xingxiu | The span is Wansong's commentary weighing proposed rescue and later disputes; Nanquan is its case subject. |
| 如何是祖師西來意 | Zhaozhou Congshen | unnamed monk | The headword is the monk's repeated question; Zhaozhou answers with the cypress. |
| 無住 | Fayan Wenyi | Vimalakirti | Manjusri asks the quoted question and Vimalakirti answers that not-dwelling has no root; Fayan belongs to the surrounding case. |

Dasui Fazhen's repeated answer `隨他去` was the only default that survived exact-turn review.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.11 |
| validated dry run | 0.21 |
| atomic apply | 0.29 |
| strict source gate | 0.67 |
| all-touched KWIC/anchor audit | 0.87 |
| **mechanical pipeline total** | **2.15** |

Manual complete-case review was not instrumented, so no estimate is invented.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 23 | 17 | -6 |
| named occurrences | 7 | 11 | +4 |
| reviewed unnamed non-masters | 2 | 4 | +2 |
| notes missing speaker | 25 | 19 | -6 |
| notes missing source | 25 | 19 | -6 |
| context-master links | 3 | 11 | +8 |
| hard failures | 91 | 73 | -18 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
