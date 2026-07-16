# J36nB359 attribution speed test

Scope: 2 workbook rows in 2 complete cases across 2 entries from the *Recorded Sayings of Chan Master Baiyu* (`百愚禪師語錄`). Every complete case and exact turn was reviewed; defaults remained drafts until adjudication.

## Result

- Cases reviewed: **2/2**.
- Defaults accepted: **0/2**.
- Contradictions overridden: **2/2**.
- Compiler, dry run, and apply: **2/2**, zero failures.
- Strict source gate: **2/2** actor-complete.
- Focused audit: **2/2** exact decisions, source-title notes, KWICs, and anchors.
- Full touched-file audit: **2/2** JSON parse; **15/15** KWICs and exact anchor pairs verify.

## Overrides

| Term | Default | Reviewed decision | Why the default failed |
|---|---|---|---|
| 方丈 | Yunmen Wenyan | Baiyu Si | The passage is Baiyu Si's invited address at Yunmen Xiansheng Monastery. `方丈和尚` names the resident abbot whom Baiyu tells the assembly to ask; Yunmen Wenyan is neither speaker nor local section owner. |
| 拾得 | Bodhidharma | Baiyu Si | The occurrence is Baiyu Si's image praise under the immediate heading `寒山拾得`. Bodhidharma belongs to the preceding image heading and does not speak this praise. |

Baiyu Si is established by the book's own record identity and existing source-attested use in the termbase. The current attribution audit nevertheless classifies both assignments as `deferred_non_roster`; a future roster record must use this spelling or normalize the occurrences together.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.11 |
| validated dry run | 0.21 |
| atomic apply | 0.23 |
| strict source gate | 0.33 |
| all-touched KWIC/anchor audit | 0.31 |
| **mechanical pipeline total** | **1.19** |

Manual complete-case review was not instrumented, so no estimate is invented.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 12 | 10 | -2 |
| named occurrences | 2 | 4 | +2 |
| notes missing speaker | 12 | 10 | -2 |
| notes missing source | 12 | 10 | -2 |
| hard failures | 40 | 34 | -6 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
