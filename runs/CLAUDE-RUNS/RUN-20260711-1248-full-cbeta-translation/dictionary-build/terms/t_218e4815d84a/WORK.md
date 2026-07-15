# WORK — 勘破 (t_218e4815d84a)

**Batch:** b002 · **Status:** verified (Gate 2)

## Term
勘破 — "to see through / examine and expose" a monk in a dharma-encounter.
Stock forms in the task brief: 勘破了也, 一勘便破.

## Concordance (Zen allowlist only)
- Raw allowlist hits: **250 texts / 1,484 occurrences** (filtered from full-canon grep via `zen-corpus.json`).
- Top texts: X66n1296 (76), X82n1571 (62), C078n1720 (47), X79n1557 (29), X80n1565, T48n2003, T47n1998A, T48n2004, T47n2000 …
- Overwhelmingly multi-source; single corpus-wide sense.

## Sense analysis
One sense (SenseKey = null). 勘 = to investigate/test; 破 = to break/expose. A master
tests someone and sees right through them. No master bends the word idiosyncratically,
so no master-specific sense. The paradigm is Zhaozhou's Mt. Wutai crone case
(臺山婆子…勘破了也), which recurs verbatim across denglu, koan-commentary and yulu — the
basis for the multi-source gate.

## Multi-source gate → PASS (multi-source)
5 curated occurrences, 5 masters, 5 independent texts:
1. Zhaozhou Congshen — 五燈會元 X80n1565 0092b11 (attribution verified: section head 趙州觀音院從諗禪師, 師 = Zhaozhou).
2. Xuedou Chongxian — 碧巖錄 T48n2003 0144a07 (雪竇著語, Deshan–Guishan case).
3. Wansong Xingxiu — 從容錄 T48n2004 0233b20 (萬松道…, reflexive 累及萬松).
4. Dahui Zonggao — 大慧語錄 T47n1998A 0850b14 (師 = Dahui, inverted use, then strikes).
5. Zhaozhou (via Xutang Zhiyu) — 虛堂語錄 T47n2000 0994b29 (州…勘破了也, raised & commented on).

## KWIC verification
All KWICs re-checked as EXACT contiguous substrings of the tag+whitespace-stripped
source file (no ellipsis, no stitching). Every KWIC contains 勘破; every RelPath on the
allowlist; every FromLb the nearest preceding `<lb ed=…>` and present in the file.
(Verifier: normalized-stream substring test.)

## Notes / risks
- The 臺山婆子 case underlies 3 of 5 occurrences but through independent commentators
  (denglu / 從容錄 / 虛堂語錄) — genuine independent witnesses, plus Xuedou (different
  case) and Dahui (own live encounter) for breadth.
- Deflationary rendering "to see through / test and expose"; avoided any "penetrate
  emptiness" abstraction.

## Gate 2 — independent adversarial verify (Claude, Opus)
Re-derived from source by targeted grep of each cited file. VERDICT: **verified**, 0 repairs.
- **KWIC (5/5 exact-contiguous verbatim after tag-strip):**
  1. X80n1565 0092b11 `師歸院謂僧曰。臺山婆子為汝勘破了也` — file line 7126, verbatim.
  2. T48n2003 0144a07 `雪竇著語云。勘破了也。一似鐵橛相似。` — lines 959–960 (splits at lb, contiguous after strip).
  3. T48n2004 0233b20 `萬松道。勘破了也。` — line 1435, verbatim.
  4. T47n1998A 0850b14 `師云。老僧被爾勘破。僧擬議。師便打。` — lines 4097–4098 (lb split), contiguous.
  5. T47n2000 0994b29 `州歸院云。婆子被我勘破了也。` — lines 1786→1788 across pb 0994c/lb 0994c01, contiguous.
- **Contamination:** 0. All 5 RelPaths on `zen-corpus.json` allowlist.
- **Attribution (all confirmed):** #1 Zhaozhou — under section head 趙州觀音院從諗禪師 (X80n1565 line 7045, lb 0091b05); narrative 待我去勘過…師歸院 confirms 師=Zhaozhou. #2 雪竇著語云 (self-attributing) = Xuedou. #3 萬松道 (self) = Wansong. #4 師=Dahui — T47n1998A is 大慧普覺禪師語錄, own 問僧 examination. #5 州=Zhaozhou's raised line in Xutang yulu (honestly noted; single-speaker, not two-speaker → MasterName kept).
- **Multi-source:** holds (5 texts, 4 distinct masters). **FromLb:** all = nearest preceding `<lb n>`. **RelatedTerms** 轉語/勘辨 = genuine semantic siblings, not coincidental prefixes.

## Gate-3 REVISE fix — 2026-07-11 19:10
Removed fabricated collocation "一勘便破" from Explanation per GATE3_VERDICT.md. GREP-confirmed 一勘便破 = 0 files corpus-wide (fixed-string rg over CbetaZenTexts/xml-p5; control 勘破了也 = 159 files, tooling sound). Deleted the clause "and 一勘便破 means 'one test and he is exposed.'"; no replacement added since the attested stock form 勘破了也 (in the preceding clause) already carries the point. No Kwic changed; all 5 KWICs verbatim. STATUS=verified.
