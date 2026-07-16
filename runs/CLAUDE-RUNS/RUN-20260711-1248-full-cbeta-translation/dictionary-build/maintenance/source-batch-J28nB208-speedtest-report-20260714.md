# J28nB208 attribution speed test

Scope: 1 workbook row in 1 complete case within the `斬貓` entry from the *Recorded Sayings of Chan Master Guxue Zhe* (`古雪哲禪師語錄`). The prepared sheet row matched current triage exactly. Its workbook case-level review-class label was stale and was corrected from `full-ladder-or-parallel-needed` to the row's and triage's `co-located-reviewed-candidate` before review.

## Result

- Triage reconciliation: **1/1** exact row match; **1** stale case-label correction.
- Cases reviewed: **1/1**.
- Exact actor default confirmed: **1/1**.
- Full custom decisions supplied to preserve all named contextual masters: **1/1**.
- Compiler, dry run, and apply: **1/1**, zero failures.
- Strict source gate: **1/1** actor-complete.
- Focused audit: **1/1** exact decision, source-title note, KWIC, and anchors.
- Full touched-file audit: **1/1** JSON parse; **5/5** KWICs and exact anchor pairs verify.

Guxue Zhe is the exact governing speaker. In the release-pond address he first quotes Shakyamuni Buddha's rescue injunction, then cites Nanquan Puyuan cutting the cat and Guizong Zhichang cutting the snake and asks what expedient could have rescued them. Nanquan and Guizong are named case actors inside Guxue's public question, not substitute speakers. A custom override preserved Guxue's confirmed actor while recording all three contextual figures and their roles.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.16 |
| validated dry run | 0.30 |
| atomic apply | 0.39 |
| strict source gate | 0.61 |
| all-touched KWIC/anchor audit | 0.11 |
| **mechanical pipeline total** | **1.57** |

Manual complete-case review was not instrumented, so no estimate is invented.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 2 | 1 | -1 |
| named occurrences | 3 | 4 | +1 |
| notes missing speaker | 5 | 4 | -1 |
| notes missing source | 5 | 4 | -1 |
| context-master links | 0 | 3 | +3 |
| hard failures | 15 | 12 | -3 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
