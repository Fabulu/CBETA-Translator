# X84n1583 attribution speed test

Scope: 11 workbook rows in 10 complete cases across 10 entries from *Continuation of the Lamp's Orthodox Lineage* (`續燈正統`). Every complete case and exact turn was reviewed. The prebuilt default was treated only as a draft; only the observed contradiction was recorded in the signed override sheet.

## Result

- Cases reviewed: **10/10**.
- Defaults accepted after review: **10/11**.
- Contradictions overridden: **1/11**.
- Compiler, dry run, and apply: **11/11**, zero failures.
- Focused gate: **11/11** actor-complete, exact-decision matched, source-titled, and KWIC/anchor verified.
- Full touched-file audit: **10/10** JSON parse; **62/62** KWICs and exact anchor pairs verify.

## Override

| Term | Default | Reviewed decision | Why the default failed |
|---|---|---|---|
| 一著 | Huqiu Shaolong | unnamed monk (reviewed non-master) | The monk, not Huqiu, asks where the single move falls before buddhas and living beings have arisen. The line, expanded context, section header, title, TEI header, and two parallel versions leave him unnamed; Huqiu gives the answer. |

The remaining ten defaults survived exact-turn review, including Huqiu Shaolong as the actor who enters Yuanwu Keqin's room, Dahui Zonggao's chamber and interview turns, Wanfeng Shiwei's final verse, Huanyuan Fuyu's request for instruction, and Yunqi Zhuhong's biographical act of writing `生死事大` on his desk.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.39 |
| validated dry run | 0.74 |
| atomic apply | 1.18 |
| strict source gate | 0.80 |
| all-touched KWIC/anchor audit | 1.01 |
| **mechanical pipeline total** | **4.12** |

Manual complete-case review was not instrumented, so no estimate is invented. The compact packets materially reduced navigation: all ten cases were reviewable in one workbook, and only one full exception object required editing.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 50 | 39 | -11 |
| named occurrences | 12 | 22 | +10 |
| reviewed unnamed non-masters | 0 | 1 | +1 |
| notes missing speaker | 55 | 44 | -11 |
| notes missing source | 52 | 42 | -10 |
| hard failures | 205 | 173 | -32 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
