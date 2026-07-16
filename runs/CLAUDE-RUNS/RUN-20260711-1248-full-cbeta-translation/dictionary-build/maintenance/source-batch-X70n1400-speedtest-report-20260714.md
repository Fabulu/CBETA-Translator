# X70n1400 attribution speed test

Scope: 2 workbook rows in 2 complete cases across 2 entries from the *Recorded Sayings of Chan Master Gaofeng Yuanmiao* (`高峰原妙禪師語錄`). The prepared sheet was first reconciled against current triage: entry IDs, terms, source path, line anchors, KWICs, and review classes matched exactly. Every complete case and exact turn was then reviewed.

## Result

- Triage reconciliation: **2/2** exact row matches.
- Cases reviewed: **2/2**.
- Defaults accepted: **2/2**.
- Contradictions overridden: **0/2**.
- Compiler, dry run, and apply: **2/2**, zero failures.
- Strict source gate: **2/2** actor-complete.
- Focused audit: **2/2** exact decisions, source-title notes, KWICs, and anchors.
- Full touched-file audit: **2/2** JSON parse; **9/9** KWICs and exact anchor pairs verify.

Gaofeng Yuanmiao remains the exact speaker in both cases. In the first hall address he describes the person who spends the whole day freely flowing along (`騰騰任運`). In the instruction to attendant Jingxiu he repeatedly says that walking, sitting, dressing, eating, defecating, and urinating are each solely the mass of doubt (`疑團`). The embedded quotations and named figures do not displace his governing voice.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.11 |
| validated dry run | 0.18 |
| atomic apply | 0.25 |
| strict source gate | 0.42 |
| all-touched KWIC/anchor audit | 0.12 |
| **mechanical pipeline total** | **1.08** |

Manual complete-case review was not instrumented, so no estimate is invented.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 7 | 5 | -2 |
| named occurrences | 2 | 4 | +2 |
| notes missing speaker | 7 | 5 | -2 |
| notes missing source | 7 | 5 | -2 |
| hard failures | 23 | 17 | -6 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
