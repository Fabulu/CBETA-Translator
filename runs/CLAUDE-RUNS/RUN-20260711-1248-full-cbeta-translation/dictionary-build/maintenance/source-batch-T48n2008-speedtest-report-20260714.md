# T48n2008 attribution speed test

Scope: 4 workbook rows in 3 complete cases across 3 entries from the *Platform Sutra of the Sixth Patriarch* (`六祖大師法寶壇經`). Every complete section and exact turn was reviewed; defaults remained drafts until adjudication.

## Result

- Cases reviewed: **3/3**.
- Defaults accepted: **0/4**.
- Contradictions overridden: **4/4**.
- Compiler, dry run, and apply: **4/4**, zero failures.
- Strict source gate: **4/4** actor-complete.
- Focused audit: **4/4** exact decisions, source-title notes, KWICs, and anchors.
- Full touched-file audit: **3/3** JSON parse; **20/20** KWICs and exact anchor pairs verify.

## Overrides

| Term | Default | Reviewed decision | Why the default failed |
|---|---|---|---|
| 一行三昧 | Huineng | Qisong | The occurrence is in `六祖大師法寶壇經贊`, whose byline explicitly says `宋明教大師契嵩撰`; Qisong writes the definition while Huineng is the scripture's subject. |
| 無相戒 (two rows) | Huineng | Qisong | Both definition and appraisal belong to the same explicitly bylined praise by Qisong, not to Huineng's speech. |
| 律 | Huineng | Fahai | The occurrence is narration in `六祖大師緣起外紀`, explicitly compiled by `門人法海等`; Fahai is the named lead compiler, while Huineng is the ordination recipient and biographical subject. |

Qisong and Fahai are named directly from the section bylines. The current attribution audit classifies these four assignments as `deferred_non_roster`, so their future roster records must use these same primary spellings or normalize the occurrences together.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.14 |
| validated dry run | 0.23 |
| atomic apply | 0.28 |
| strict source gate | 0.35 |
| all-touched KWIC/anchor audit | 0.24 |
| **mechanical pipeline total** | **1.24** |

Manual complete-case review was not instrumented, so no estimate is invented.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 12 | 8 | -4 |
| named occurrences | 8 | 12 | +4 |
| notes missing speaker | 17 | 13 | -4 |
| notes missing source | 18 | 14 | -4 |
| context-master links | 0 | 4 | +4 |
| hard failures | 50 | 38 | -12 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
