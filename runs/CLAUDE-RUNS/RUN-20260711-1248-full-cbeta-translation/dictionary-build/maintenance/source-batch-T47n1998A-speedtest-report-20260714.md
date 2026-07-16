# T47n1998A attribution speed test

Scope: 17 workbook rows in 16 complete cases across 15 entries from *Recorded Sayings of Chan Master Dahui Pujue* (`大慧普覺禪師語錄`). Every complete case and exact turn was reviewed. The prebuilt default was treated only as a draft; only contradictions were recorded in the signed override sheet.

## Result

- Cases reviewed: **16/16**.
- Defaults accepted after review: **13/17**.
- Contradictions overridden: **4/17**.
- Compiler: **17/17** rows, **4** overrides, zero errors.
- Dry run: **17/17** prepared across **15** entries, zero failures.
- Apply: **17/17** applied, zero failures.
- Focused gate: **17/17** named actors, exact decision matches, source-title notes, verified KWICs, and exact anchor pairs.
- Full touched-file audit: **15/15** JSON parse; **82/82** KWICs and exact anchor pairs verify.

## Defaults versus overrides

| Term | Default | Reviewed decision | Why the default failed |
|---|---|---|---|
| 牢關 | Dahui Zonggao | Luopu Yuanan | `古者道` marks a quotation; parallel lamp records and Dahui's later explicit retelling name Luopu as the formula's speaker. |
| 言前 | Dahui Zonggao | Fengxue Yanzhao | The complete turn explicitly says `不見風穴和尚道`; Dahui is the later quoter. |
| 一指頭禪 | Dahui Zonggao | Juzhi | The headword line is Juzhi's first-person saying inside Dahui's retelling. |
| 休歇 | Bodhidharma | Huike | Bodhidharma speaks the preceding reassurance; the following clause makes the Second Patriarch Huike the one who rests. |

The other thirteen defaults survived exact-turn review, including Dahui's own actions, pointers, letters, verse, and comments; Linji Yixuan's direct `敗闕` turn; and Zhaozhou Congshen's dog-case answer.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.37 |
| validated dry run | 0.83 |
| atomic apply | 1.25 |
| focused plus all-touched KWIC gate | 1.15 |
| **mechanical pipeline total** | **3.60** |

Manual full-case review was not started under an instrumented monotonic timer, so no invented elapsed estimate is reported. The measurable comparison is clear: only four override objects were edited, while the tool generated and validated all seventeen full decisions.

## Changed entry IDs

`t_5854f7c24ddf`, `t_ffb0ee18f1a2`, `t_b8d2633b12ef`, `t_368268e023e3`, `t_ea138c7335d3`, `t_a0f2bb1de215`, `t_34143e43daf4`, `t_961b548d6462`, `t_75348ebe8a2d`, `t_5b39f18f89ff`, `t_b655ff97e2c3`, `t_da6965508721`, `t_57ef1bbc3a81`, `t_bcf65a900b7a`, `t_6af80faddcf0`.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 72 | 55 | -17 |
| named occurrences | 10 | 27 | +17 |
| notes missing speaker | 73 | 56 | -17 |
| notes missing source | 68 | 52 | -16 |
| hard failures | 250 | 200 | -50 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
