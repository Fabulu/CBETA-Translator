# B25n0144 attribution speed test

Scope: 5 workbook rows in 5 complete cases across 5 entries from the *Patriarchal Hall Collection* (`祖堂集`). Every complete case and exact turn was reviewed; defaults remained drafts until adjudication.

## Result

- Cases reviewed: **5/5**.
- Defaults accepted: **2/5**.
- Contradictions overridden: **3/5**.
- Compiler, dry run, and apply: **5/5**, zero failures.
- Strict source gate: **5/5** actor-complete.
- Focused audit: **5/5** exact decisions, source-title notes, KWICs, and anchors.
- Full touched-file audit: **5/5** JSON parse; **28/28** KWICs and exact anchor pairs verify.

## Overrides

| Term | Default | Reviewed decision | Why the default failed |
|---|---|---|---|
| 冷暖自知 | Huineng | Huiming | Huiming reports his own entry to Huineng after receiving Huineng's instruction; Huineng is instructor and addressee. |
| 如何是佛法大意 | Qingyuan Xingsi | reviewed unnamed monk | The headword is the monk's question; Qingyuan is the respondent. |
| 罔措 | Nanyang Huizhong | Emperor Daizong | Nanyang pauses after the emperor asks about the seamless stupa; the emperor is the person recorded as bewildered. |

Huineng's definition of `一行三昧` and Bai Juyi's personal receipt of the mind-precept (`心戒`) were the two defaults that survived exact-turn review.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.20 |
| validated dry run | 0.34 |
| atomic apply | 0.45 |
| strict source gate | 0.38 |
| all-touched KWIC/anchor audit | 0.09 |
| **mechanical pipeline total** | **1.46** |

Manual complete-case review was not instrumented, so no estimate is invented.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 20 | 15 | -5 |
| named occurrences | 5 | 9 | +4 |
| reviewed unnamed non-masters | 3 | 4 | +1 |
| notes missing speaker | 24 | 19 | -5 |
| notes missing source | 25 | 20 | -5 |
| context-master links | 3 | 6 | +3 |
| hard failures | 76 | 61 | -15 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
