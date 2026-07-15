# GATE 3 VERDICT — t_d4661c1b4dbb · 正中偏

VERDICT: PASS

**Auditor:** Gate 3 independent adversarial pass (Claude, 2026-07-12). Re-derived from
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` via tag-stripped exact-substring search
(apparatus `<note>`/`<rdg>` dropped), lb tracked per `ed`, governing `cb:mulu` tracked.

## 1. KWIC integrity — ALL 6 VERBATIM ✅
Every Kwic is an exact contiguous substring of the cited file's MAIN text (not apparatus),
unique in-file (count=1), and both FromLb and ToLb sit on the claimed line:

| # | RelPath | Claimed lb | Found (ed=X/T, start=end) | Count | In notes? |
|---|---------|-----------|---------------------------|-------|-----------|
| 1 | X/X85/X85n1593.xml | 0480a22 | 0480a22=0480a22 (R146 co-located 0217a10 — matches note) | 1 | no |
| 2 | T/T47/T47n1986B.xml | 0525c01 | 0525c01=0525c01 (incl. the odd 師。作 punctuation — verbatim) | 1 | no |
| 3 | X/X85/X85n1593.xml | 0481a13 | 0481a13=0481a13; 舍事入理 continues on 0481a14 as note claims | 1 | no |
| 4 | X/X85/X85n1593.xml | 0481a17 | 0481a17=0481a17 | 1 | no |
| 5 | X/X81/X81n1571.xml | 0682c17 | 0682c17=0682c17; 月明 continues 0682c18 as note claims | 1 | no |
| 6 | X/X82/X82n1571.xml | 0279b20 | 0279b20=0279b20 | 1 | no |

No ellipsis, no stitching, no added punctuation. X-canon lbs use ed="X" (correct edition).

## 2. Attribution — CORRECT ✅
- Occ1 governing mulu: 洞山良价禪師 ✅ (verse = Dongshan; five-ranks rule honored).
- Occ2: T47n1986B title verified as 瑞州洞山良价禪師語錄 (title level="m"); 師 = Dongshan; the
  verse is introduced 師。作五位君臣頌云 ✅. (T47n1986A is the 筠州…悟本 recension — the entry cites the right one.)
- Occ3/Occ4 governing mulu: 曹山本寂禪師 ✅ (prose 旨訣 definitions = Caoshan; NOT cross-attributed to Dongshan).
- Occ5 governing mulu: 明州天童宏智正覺禪師 ✅; roster has 宏智正覺 ✅.
- Occ6 governing mulu: 南昌府百丈瑞白明雪禪師 — 明雪/瑞白 NOT in master-dates.json → MasterName null is correct ✅.
  The note's claim that the same sermon is carried in 五燈會元續略 X80n1566 verified (found at X 0472b15,
  same mulu head 南昌府百丈瑞白明雪禪師) ✅.
- Rosters: 洞山良价, 曹山本寂, 宏智正覺 all present in master-dates.json ✅.

## 3. Allowlist — CLEAN ✅
All 6 occurrence RelPaths + all 8 SourceTexts in zen-corpus.json. Headword attestation per
SourceText (raw grep, tag-crossing hits would only raise these): X85n1593=8, X81n1571=11,
X82n1571=13, X84n1583=11, X84n1585=5, X86n1605=2, T47n1986B=1, T47n1987A=6 — every
SourceText attests 正中偏 ✅.

## 4. Explanation honesty — ALL PHRASES ATTESTED ✅
Grep-verified verbatim in the cited allowlist files:
- 正中偏，三更初夜月明前。莫怪相逢不相識，隱隱猶懷舊日嫌 — X85n1593 (0480a22–23) ✅
- 師作五位君臣頌曰 — X85n1593 0480a20 ✅ (X81n1571 titles the same verse 五位正偏頌 — the entry does not claim otherwise)
- 正位即空界，本來無物。偏位即色界，有萬象形。正中偏者，背理就事。偏中正者，舍事入理 — X85n1593 0481a12–14 ✅
- 五位君臣旨訣 / 師因僧問五位君臣旨訣 — X85n1593 0481a12 ✅
- 君為正位，臣為偏位 / 君視臣是正中偏 — X85n1593 0481a16–17 ✅
- 如何是正中偏？師曰：雲散長空後，虗堂夜月明 — X81n1571 0682c17–18 ✅ (虗 matches source glyph)
- 以濟宗論之 / 正中偏奪人也，偏中正奪境也 — X82n1571 0279b20 ✅ and X80n1566 0472b15 ✅

## 5. Multi-source — HOLDS ✅
Verse: X85n1593 (禪宗正脉) + T47n1986B (語錄) + X81n1571 (as 五位正偏頌, grep-confirmed 正中偏，三更…).
Gloss: X85n1593 + X81n1571 0647b22 + T47n1987A 0527a05–07. ≥2 independent texts for each
component; masters 洞山/曹山/宏智 independent. `multi-source` is justified.

## 6. Describe-only — CLEAN ✅
Explanation reports graphs, the in-corpus self-definition (背理就事), the ruler-minister
assignment, deployment as a stock test-question, and the attested 濟宗 mapping — all quoted
and grep-verified. No intent/point/force vocabulary. Closes with the no-further-gloss
disclaimer. No menu-of-readings.

## 7. Nesting / RelatedTerms — GENUINE ✅
偏中正 · 正中來 · 兼中至 · 兼中到 · 五位 · 君臣 all co-occur in the very passages quoted
(verse sequence 0480a22ff; 旨訣 answer; 君臣 allegory). Five Ranks interrelation as required.

## Punch list (non-blocking observations)
1. **Borderline (judgment call, not a defect):** Occ5's KWIC is a two-speaker exchange — the
   headword is uttered by the anonymous monk (如何是正中偏？), Hongzhi speaks only the answer.
   MasterName=宏智正覺 matches the governing mulu and follows the buffalo-pilot precedent
   (attributing in-section 僧問/師曰 exchanges to the section master), and the AttributionNote
   states the structure transparently. A stricter two-speaker→null policy would null it;
   flagged for the editor's convention decision, consistent across entries.

Defects: 0.
