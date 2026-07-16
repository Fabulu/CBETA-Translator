# T51n2077 attribution speed test

Scope: 2 workbook rows in 2 complete cases across 2 entries from the *Continuation of the Lamp Record* (`續傳燈錄`). Every complete case and exact turn was reviewed; defaults remained drafts until adjudication.

## Result

- Cases reviewed: **2/2**.
- Defaults accepted: **0/2**.
- Contradictions overridden: **2/2**.
- Compiler, dry run, and apply: **2/2**, zero failures.
- Strict source gate: **2/2** actor-complete.
- Focused audit: **2/2** exact decisions, source-title notes, KWICs, and anchors.
- Full touched-file audit: **2/2** JSON parse; **11/11** KWICs and exact anchor pairs verify.

## Overrides

| Term | Default | Reviewed decision | Why the default failed |
|---|---|---|---|
| 燈錄 | Juding | impersonal lamp-record title and contents heading | The headword is inside `續傳燈錄` in the document identifier, volume title, and contents heading. These are noun-phrase metadata before the lineage list and biography, with no human subject or speech marker. |
| 逢祖殺祖 | Dahui Zonggao | Linji Yixuan | The immediately preceding words explicitly say `所以臨濟和尚道`; Linji speaks the three-part killing formula. Dahui then asks why a good teacher would say this and what its principle is. |

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.14 |
| validated dry run | 0.26 |
| atomic apply | 0.29 |
| strict source gate | 0.40 |
| all-touched KWIC/anchor audit | 0.18 |
| **mechanical pipeline total** | **1.27** |

Manual complete-case review was not instrumented, so no estimate is invented.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 9 | 7 | -2 |
| named occurrences | 2 | 3 | +1 |
| impersonal occurrences | 0 | 1 | +1 |
| notes missing speaker | 9 | 8 | -1 |
| notes missing source | 9 | 7 | -2 |
| context-master links | 0 | 2 | +2 |
| hard failures | 27 | 22 | -5 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
