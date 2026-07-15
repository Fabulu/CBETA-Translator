# GATE 3 VERDICT — t_52391cba2cdf · 三玄三要

VERDICT: PASS

**Auditor:** Gate 3 (independent adversarial), 2026-07-11
**Method:** tag-stripped stream match with lb re-anchoring (note/rdg content excluded), raw-XML context pulls for attribution frames, allowlist grep, phrase-honesty greps.

## 1. KWIC integrity — 6/6 EXACT-CONTIGUOUS
| # | RelPath | FromLb | Result |
|---|---------|--------|--------|
| 1 | T/T47/T47n1985.xml | 0497a19 | exact, 1 hit, anchors at 0497a19 ✓ |
| 2 | T/T47/T47n1985.xml | 0497a15 | exact, 1 hit, anchors at 0497a15 ✓ |
| 3 | T/T47/T47n1992.xml | 0598c17 | exact, 1 hit, anchors at 0598c17 ✓ |
| 4 | T/T47/T47n1992.xml | 0597b07 | exact, 1 hit, anchors at 0597b07, verse spans b07–b08 ✓ |
| 5 | T/T48/T48n2006.xml | 0311b19 | exact, 1 hit, anchors at 0311b19 ✓ |
| 6 | T/T48/T48n2006.xml | 0311b20 | exact, 1 hit, anchors at 0311b20 ✓ |

No ellipsis, no stitching, no added punctuation. All in main text (not notes).

## 2. Attribution — all correct
- **Occ 1–2 (T47n1985):** single-master record 鎮州臨濟慧照禪師語錄. Occ 1 frame: `師又云：「一句語須具三玄門…汝等諸人作麼生會？」下座` — Linji's own 上堂 close. Occ 2 frame: `上堂，僧問：「如何是第一句？」師云…` — 師 = Linji. MasterName 臨濟義玄 ✓.
- **Occ 3 (T47n1992 0598c17):** inside `因摘菊花小參云。…若能於此明得去。一句中有三玄三要。賓主歷然` — Fenyang's own 小參 prose (師 of 汾陽無德禪師語錄). MasterName 汾陽善昭 ✓.
- **Occ 4 (0597b07):** immediately preceded by `師云。若人會得此三句。已辨三玄。更有三要語在。切須薦取。不是等閒與大眾頌出` — the verse is delivered by Fenyang himself, followed by `師云。會麼恁麼會得` ✓.
- **Occ 5–6 (T48n2006):** governing cb:mulu = 臨濟門庭 (the handbook's editorial "Linji house style" chapter, no 云-frame speaker). MasterName null ✓ — correct per raised/editorial rule.

## 3. Allowlist — clean
All cited RelPaths (T47n1985, T47n1992, T48n2006) and all SourceTexts (plus X80n1565, J25nB171) present in zen-corpus.json. X80n1565 contains 三玄三要 4×, J25nB171 23× — SourceTexts honest.

## 4. Explanation honesty — all phrases attested
- 一句語須具三玄門，一玄門須具三要，有權、有用 ✓ (T47n1985)
- 三要印開朱點側，未容擬議主賓分 ✓ (T47n1985)
- 一句中有三玄三要。賓主歷然 ✓; 三玄三要事難分。得意忘言道易親 ✓ (T47n1992)
- 三玄者。玄中玄。體中玄。句中玄。三要者 ✓; 自是一喝中。體攝三玄三要也 ✓ (T48n2006)
- 三玄三要四料揀 — grep: 6 corpus files ✓ (the "listed beside his other devices" claim holds)
- 四賓主 ✓ (7 hits in T48n2006 alone); 四料揀 ✓ (11 hits)
- **Variant claim verified:** AttributionNote says T47n1992 reads 一句分明該萬象 while 人天眼目's copy reads 明明 — confirmed: T48n2006 contains 一句明明該萬象 exactly once, in its copy of the Fenyang verse (raw grep misses it due to a line break; tag-stripped stream finds it). A precise, honest philological note.

## 5. Multi-source — genuine
Two independent masters' own records (Linji T47n1985; Fenyang T47n1992) plus an independent handbook (T48n2006). `multi-source` justified.

## 6. Nesting / RelatedTerms — genuine
三玄 and 三要 are true constituents of the compound (per §5b, correct to relate). 四料揀 / 四賓主 / 一喝 are genuine co-listed Linji-house devices (三玄三要四料揀 collocation attested in 6 files), not character-overlap coincidences.

## Punch list
None. Defect count: 0.
