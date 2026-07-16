# T48n2006 attribution speed test

Scope: 7 workbook rows in 5 complete cases across 6 entries from *Eye of Humans and Gods* (`人天眼目`). Every complete case and exact turn was reviewed; defaults remained drafts until adjudication.

## Result

- Cases reviewed: **5/5**.
- Defaults accepted: **4/7**.
- Contradictions overridden: **3/7**.
- Compiler, dry run, and apply: **7/7**, zero failures.
- Strict source gate: **7/7** actor-complete.
- Focused audit: **7/7** exact decisions, source-title notes, KWICs, and anchors.
- Full touched-file audit: **6/6** JSON parse; **33/33** KWICs and exact anchor pairs verify.

## Overrides

| Term | Default | Reviewed decision | Why the default failed |
|---|---|---|---|
| 料揀 | Linji Yixuan | Huiyan Zhizhao | The line is the handbook compiler's Linji-house classificatory prose, not a quoted turn by Linji. |
| 主中賓 | Linji Yixuan | Huiyan Zhizhao | The four-position definition is the same compiler exposition; Linji is its school-founder subject. |
| 函蓋乾坤 | Huiyan Zhizhao | Yunmen Wenyan | The immediately preceding biography identifies `師` as Yunmen; Huiyan is the later compiler narrating Yunmen's address. |

The four surviving defaults are Nanyuan Huiyong's direct question to Fengxue Yanzhao, Shishuang Chuyuan's named `慈明` address, Deshan Yuanming's division of Yunmen's saying into three phrases, and Huiyan Zhizhao's Caodong-house editorial definition.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.33 |
| validated dry run | 0.57 |
| atomic apply | 0.80 |
| strict source gate | 0.73 |
| all-touched KWIC/anchor audit | 0.55 |
| **mechanical pipeline total** | **2.98** |

Manual complete-case review was not instrumented, so no estimate is invented.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 24 | 17 | -7 |
| named occurrences | 9 | 16 | +7 |
| notes missing speaker | 29 | 22 | -7 |
| notes missing source | 17 | 13 | -4 |
| context-master links | 0 | 3 | +3 |
| hard failures | 139 | 121 | -18 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
