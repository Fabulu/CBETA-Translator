# GATE3 VERDICT — t_f7bdd2def0ec · 截斷眾流

VERDICT: PASS

**Auditor:** Gate 3 (independent adversarial, Claude/Fable) · **Date:** 2026-07-11
**Method:** All KWICs re-derived from raw TEI XML (tag-stripped, whitespace-normalized) with raw-offset mapping back to `<lb>` / `<cb:mulu>`; explanation phrases re-grepped across the 462-file allowlist.

## 1. KWIC integrity — 6/6 PASS (verbatim, contiguous, no ellipsis/stitching)
- S1K1 `T/T47/T47n1997.xml` — found; lb `0754a23` exact; mulu `小參二`.
- S1K2 `T/T48/T48n2001.xml` — found; lb `0071c25` exact; mulu `明州天童山覺和尚小參`.
- S2K1 `X/X80/X80n1565.xml` — found; lb `ed="X" 0308a11` exact (correct X edition). Note: file reads `我有三句語示汝諸人…`; the KWIC starts at 有 — still a contiguous verbatim substring, compliant. The 函葢 (variant of 函蓋) is faithfully preserved as in the file.
- S2K2 `T/T48/T48n2006.xml` — found; lb `0312a08` exact; mulu `三句`.
- S2K3 `X/X80/X80n1565.xml` — found; lb `ed="X" 0373a24` exact; mulu `嘉定府九頂寂惺惠泉禪師`.
- S2K4 `T/T48/T48n2006.xml` — found; lb `0312b06` exact; mulu `問答`.

## 2. Attribution — PASS (including the specific Deshan-Yuanming re-check)
- **S2K1:** governing mulu/head = `鼎州德山緣密圓明禪師` (五燈會元). In-file context: `鼎州德山緣密圓明禪師…上堂。我有三句語示汝諸人。一句函葢乾坤。一句截斷眾流。一句隨波逐浪。作麼生辯。` — Deshan Yuanming DOES state the three phrases first-person in his own section. MasterName "Deshan Yuanming" correct; in roster.
- **Yunmen NOT falsely attributed:** no occurrence carries MasterName "Yunmen Wenyan". S2K2's 人天眼目 三句 context confirms the entry's account verbatim: Yunmen's 示眾 is `函蓋乾坤。目機銖兩。不涉萬緣。作麼生承當…自代云。一鏃破三關`, then `後來德山圓明密禪師。遂離其語為三句` — the three-句 FORMULATION is Deshan Yuanming's, extracted from Yunmen's word, exactly as the entry's Note says. Honest, not smoothed over.
- S1K1 Yuanwu (T47n1997 verified 圓悟佛果禪師語錄, his own 小參) and S1K2 Hongzhi (T48n2001 verified 宏智禪師廣錄, his 小參) — both correctly named, both in roster.
- S2K2 doxographic 人天眼目 → null, correct. S2K3 (Jiuding Huiquan reciting `昔日雲門有三句`) and S2K4 (問答 catechism, `宗云`) → null; conservative and rule-consistent for raised material.

## 3. Allowlist — PASS. All 4 distinct RelPaths present in zen-corpus.json.

## 4. Explanation honesty — PASS
- `若截斷眾流去。把住要津不通凡聖` = S1K1 verbatim; `截斷眾流之機。則詮表不及` = S1K2 verbatim.
- `如何湊泊` — 47 allowlist files. `如何是截斷眾流句` — 43 allowlist files ("stock student question" fully justified).
- Turning answers: `銕蛇橫古路` = S2K4 verbatim; `大地坦然平` — 10 allowlist files.
- `後來德山圓明密禪師。遂離其語為三句。` = S2K2 verbatim; `有三句語示汝諸人…作麼生辯` = S2K1 verbatim; `昔日雲門有三句` = S2K3 verbatim.

## 5. Multi-source — PASS
- Sense 1: two independent masters in their own records (圜悟 T47n1997, 宏智 T48n2001) — gate satisfied.
- Sense 2: two independent texts (五燈會元 X80n1565 with two separate loci; 人天眼目 T48n2006), plus the 43-file spread of 如何是截斷眾流句 — comfortably multi-source.

## 6. Nesting / RelatedTerms — PASS
- 隨波逐浪 — genuine: co-occurs contrastively inside S1K1, S1K2, and S2K1 KWICs themselves. (The requested 截斷眾流↔隨波逐浪 link is real, not coincidental.)
- 函蓋乾坤 — genuine: co-member of the triad, present in the KWICs.
- 雲門三句 — genuine: literal string in 44 allowlist files.

## Punch list
None. No defects found.

Defect count: 0
