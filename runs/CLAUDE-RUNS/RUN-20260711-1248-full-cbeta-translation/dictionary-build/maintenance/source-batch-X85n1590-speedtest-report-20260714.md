# X85n1590 attribution speed test

Scope: 2 workbook rows in 2 complete cases within the `印可` entry from the *Jinjiang Chan Lamp* (`錦江禪燈`). The prepared sheet was first reconciled against current triage: entry IDs, terms, source path, line anchors, KWICs, and review classes matched exactly. Every complete case and exact turn was then reviewed.

## Result

- Triage reconciliation: **2/2** exact row matches.
- Cases reviewed: **2/2**.
- Defaults accepted: **1/2**.
- Contradictions overridden: **1/2**.
- Compiler, dry run, and apply: **2/2**, zero failures.
- Strict source gate: **2/2** actor-complete.
- Focused audit: **2/2** exact decisions, source-title notes, KWICs, and anchors.
- Full touched-file audit: **1/1** JSON parse; **5/5** KWICs and exact anchor pairs verify.

## Override

| Term | Default | Reviewed decision | Why the default failed |
|---|---|---|---|
| 印可 (`0143c05`) | Zhang Shangying | Doushuai Congyue | The case establishes `悅` as Doushuai Congyue. He asks whether Zhang, after Donglin Changzong's approval, still doubts the teachings of buddhas and patriarchs. Zhang is commissioner and addressee; Donglin is the approving master named inside the question. |

Nanyue Huairang remains the exact actor in the other occurrence: the section's continuous `師` antecedent is Nanyue, who approves the six disciples individually and tells them that each accords with one part.

## Timing

| Mechanical stage | Seconds |
|---|---:|
| compile signed override sheet | 0.14 |
| validated dry run | 0.17 |
| atomic apply | 0.25 |
| strict source gate | 0.29 |
| all-touched KWIC/anchor audit | 0.26 |
| **mechanical pipeline total** | **1.11** |

Manual complete-case review was not instrumented, so no estimate is invented.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 4 | 2 | -2 |
| named occurrences | 1 | 3 | +2 |
| notes missing speaker | 4 | 2 | -2 |
| notes missing source | 4 | 2 | -2 |
| context-master links | 2 | 4 | +2 |
| hard failures | 18 | 12 | -6 |

Remaining failures belong to non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
