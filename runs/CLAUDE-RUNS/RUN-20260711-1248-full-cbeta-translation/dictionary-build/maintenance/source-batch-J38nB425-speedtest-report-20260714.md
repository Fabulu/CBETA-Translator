# J38nB425 attribution speed test

Scope: 1 workbook row in 1 complete portrait praise within the `機用` entry from the *Complete Record of Chan Master Jifei* (`即非禪師全錄`). The prepared sheet was reconciled against current regenerated triage: entry ID, term, source path, line anchor, KWIC, and review class matched exactly. The entire praise, its signature, and every named master were then reviewed.

## Result

- Triage reconciliation: **1/1** exact row match.
- Cases reviewed: **1/1**.
- Inline-name defaults rejected: **1/1**.
- Full custom exact-turn decisions supplied: **1/1**.
- Named contextual figures preserved: **5/5**.
- Compiler, dry run, and apply: **1/1**, zero failures.
- Strict source gate: **1/1** actor-complete.
- Focused audit: **1/1** exact decision, source-title note, all context-master links, KWIC, and anchors.
- Full touched-file audit: **1/1** JSON parse; **7/7** KWICs and exact anchor pairs verify.

Zheng Puyuan is the exact author, identified by `三山弟子鄭溥元熏沐拜題` immediately after the praise. Jifei Ruyi is its portrait subject. Zheng compares Jifei's responsive operation with Linji Yixuan's and his verbal sweep with Dahui Zonggao's, then names him as Yinyuan Longqi's direct heir and Feiyin Tongrong's dharma-grandson. The initial heuristic instead treated `雪峰` in Jifei's title as Xuefeng Yicun; that master is neither the author nor the subject here. The attribution audit flags Zheng Puyuan and contextual Jifei Ruyi for separate roster integration; both are explicitly named in the source.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.15 |
| validated dry run | 0.28 |
| atomic apply | 0.30 |
| strict source gate | 0.35 |
| all-touched KWIC/anchor audit | 0.10 |
| **mechanical pipeline total** | **1.18** |

Manual complete-case review was not instrumented, so no estimate is invented.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 6 | 5 | -1 |
| named occurrences | 1 | 2 | +1 |
| notes missing speaker | 6 | 5 | -1 |
| notes missing source | 6 | 5 | -1 |
| context-master links | 0 | 5 | +5 |
| hard failures | 20 | 17 | -3 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
