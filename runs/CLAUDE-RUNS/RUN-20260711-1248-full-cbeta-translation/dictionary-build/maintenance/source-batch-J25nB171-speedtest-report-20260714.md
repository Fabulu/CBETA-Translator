# J25nB171 attribution speed test

Scope: 2 workbook rows in 2 complete cases across 2 entries from the *Recorded Sayings of Master Tianyin* (`天隱和尚語錄`). Every complete case and exact turn was reviewed; defaults remained drafts until adjudication.

## Result

- Cases reviewed: **2/2**.
- Defaults accepted: **1/2**.
- Contradictions overridden: **1/2**.
- Compiler, dry run, and apply: **2/2**, zero failures.
- Strict source gate: **2/2** actor-complete.
- Focused audit: **2/2** exact decisions, source-title notes, KWICs, and anchors.
- Full touched-file audit: **2/2** JSON parse; **13/13** KWICs and exact anchor pairs verify.

## Override

| Term | Default | Reviewed decision | Why the default failed |
|---|---|---|---|
| 父母未生前 | Huineng | Yinqian | The source explicitly says `印乾一日有省，入室呈偈云`: Yinqian presents the awakening verse. Its second clause echoes Huineng's `本來無一物`, but Huineng is not this occurrence's speaker. |

Tianyin Yuanxiu remains the exact named speaker who answers the Caodong-school question with `君臣道合` in the `君臣` occurrence.

Yinqian is named directly in the source. The current attribution audit classifies this assignment as `deferred_non_roster`; a future roster record must use this primary spelling or normalize the occurrence together.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.12 |
| validated dry run | 0.19 |
| atomic apply | 0.24 |
| strict source gate | 0.36 |
| all-touched KWIC/anchor audit | 0.15 |
| **mechanical pipeline total** | **1.06** |

Manual complete-case review was not instrumented, so no estimate is invented.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 9 | 7 | -2 |
| named occurrences | 4 | 6 | +2 |
| notes missing speaker | 9 | 7 | -2 |
| notes missing source | 10 | 9 | -1 |
| context-master links | 0 | 2 | +2 |
| hard failures | 67 | 62 | -5 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
