# Structural repair report — b001–b005

Only the three assigned `entry.v2.json` files were changed. Existing root-edited prose and evidence were retained except where the obsolete two-sense references in 佛性 had to be removed during the structural merge.

## 平常心 — `t_4ccf8aed47d3`

- Moved the technical “ordinary mind is the Way” sense to `Senses[0]`.
- Kept the free “ordinary mind” question-and-answer sense as `Senses[1]`.
- Added an exact headword-bearing witness from Zhaozhou’s own record: “What is ordinary mind?” — “Foxes, wolves, and jackals are it” (`J/J24/J24nB137.xml`, `0362a13`).
- Added an independent later witness under Xianglin Chengyuan: “What is ordinary mind?” — “At dawn, the morning greeting; in the evening, take care” (`X/X80/X80n1565.xml`, `0309b19–0309b20`).
- Both new occurrences were independently verified before insertion, and `Xianglin Chengyuan` was confirmed as an exact roster name.

## 佛性 — `t_ad0a8e5aac3d`

- Folded the Zhaozhou dog-case deployment into the single corpus-wide sense; the entry now has one null-key sense rather than presenting the dog case as separate polysemy.
- Retained the root-edited literal answer “no” and Zhaozhou’s stated reply “because it has action-consciousness nature.”
- Retained all eight previously verified occurrences, including Zhaozhou’s dog, cypress, and “clothes worn against the skin” passages; Guishan’s inversion; and Dahui’s instruction to look at the saying.
- Unioned every former source path, related master, and related term. Added the previously omitted `X/X79/X79n1557.xml` occurrence path to `SourceTexts`, so every retained occurrence path is now represented.
- Removed obsolete “sense 2” cross-references and the separate-sense framing.

## 截斷眾流 — `t_f7bdd2def0ec`

- Moved the Yunmen-three-phrases technical sense to `Senses[0]`.
- Moved the free verbal phrase “cutting off the myriad streams” to `Senses[1]`.
- Preserved the Deshan Yuanming origin correction, the Yunmen association, all six verified occurrences, and all existing source paths.
- Preserved Hongzhi’s exact predicate: the function “cannot be reached by formulation.”

## Final checks

- All three files parse as JSON.
- Every resulting occurrence was rerun through `zc.verify`: 20 of 20 return `ok == True`, with stored `FromLb`/`ToLb` exactly matching verifier output and every KWIC containing its assigned headword.
- Every occurrence `RelPath` is present in its sense’s `SourceTexts`.
- The #0b scan over preferred targets, explanations, notes, and occurrence attribution notes returned no imported-framing hits.
- The two newly written occurrence notes were checked for #0c English-first rendering, and no new untranslated Chinese was introduced. Root's pre-existing explanatory prose was preserved during the structural moves and merge.
- No status, manifest, plan, guide, termbase, merge, or corpus file was changed.
