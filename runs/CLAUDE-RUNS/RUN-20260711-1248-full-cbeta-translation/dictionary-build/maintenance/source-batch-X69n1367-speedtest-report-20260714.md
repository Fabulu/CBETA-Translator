# X69n1367 attribution speed test

Scope: 2 workbook rows in 2 complete cases across 2 entries from the *Recorded Sayings of Chan Master Xiaoyin Daxin* (`笑隱大訢禪師語錄`). The prepared sheet was first reconciled against regenerated triage: entry IDs, terms, source path, line anchors, KWICs, and review classes matched exactly. Every complete case and exact turn was then reviewed.

## Result

- Triage reconciliation: **2/2** exact row matches.
- Cases reviewed: **2/2**.
- Defaults accepted: **1/2**.
- Contradictions overridden: **1/2**.
- Compiler, dry run, and apply: **2/2**, zero failures.
- Strict source gate: **2/2** actor-complete.
- Focused audit: **2/2** exact decisions, source-title notes, KWICs, and anchors.
- Full touched-file audit: **2/2** JSON parse; **12/12** KWICs and exact anchor pairs verify.

## Override

| Term | Default | Reviewed decision | Why the default failed |
|---|---|---|---|
| 不落因果 | Baizhang Huaihai | Huiji Yuanxi | Daxin's biography explicitly establishes `晦機熈禪師` as his teacher and keeps `師` as that antecedent. Huiji Yuanxi asks Daxin where the danger and advantage lie in the two fox-case answers and then shouts when Daxin prepares to reply. Baizhang is the cited case subject. |

Daxin remains the exact actor for `野狐`: his own record says that in the chamber he regularly raised the Baizhang fox case and questioned monks, then gives his own paired `百丈野狐，野狐百丈` comment.

Huiji Yuanxi is named directly in the source. The current attribution audit classifies the new exact assignment as `deferred_non_roster`; a future roster record must use this spelling or normalize the occurrence together.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.18 |
| validated dry run | 0.29 |
| atomic apply | 0.32 |
| strict source gate | 0.48 |
| all-touched KWIC/anchor audit | 0.20 |
| **mechanical pipeline total** | **1.47** |

Manual complete-case review was not instrumented, so no estimate is invented.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 8 | 6 | -2 |
| named occurrences | 2 | 4 | +2 |
| notes missing speaker | 9 | 7 | -2 |
| notes missing source | 9 | 7 | -2 |
| context-master links | 4 | 6 | +2 |
| hard failures | 34 | 28 | -6 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
