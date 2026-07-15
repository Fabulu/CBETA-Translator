# GATE 3 VERDICT — t_dc02eefd07f5 · 偏中正

VERDICT: PASS

**Auditor:** Gate 3 independent adversarial pass (Claude, 2026-07-12). Re-derived from
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` via tag-stripped exact-substring search
(apparatus `<note>`/`<rdg>` dropped), lb tracked per `ed`, governing `cb:mulu` tracked.

## 1. KWIC integrity — ALL 6 VERBATIM ✅
Every Kwic is an exact contiguous substring of the cited file's MAIN text (not apparatus),
unique in-file (count=1), FromLb=ToLb verified at both ends of the match:

| # | RelPath | Claimed lb | Found (start=end) | Count | In notes? |
|---|---------|-----------|-------------------|-------|-----------|
| 1 | X/X85/X85n1593.xml | 0480a23 | 0480a23=0480a23; 無真… continues 0480a24 as note claims | 1 | no |
| 2 | X/X81/X81n1571.xml | 0647b22 | 0647b22=0647b22 — full gloss on one line, as note claims | 1 | no |
| 3 | T/T47/T47n1987A.xml | 0527a07 | 0527a07=0527a07; T-canon 。-only punctuation verbatim | 1 | no |
| 4 | X/X85/X85n1593.xml | 0481a17 | 0481a17=0481a17; 臣向 does open the line-crossing (KWIC correctly starts 君是…) | 1 | no |
| 5 | X/X81/X81n1571.xml | 0682c18 | 0682c18=0682c18 | 1 | no |
| 6 | X/X82/X82n1571.xml | 0279b20 | 0279b20=0279b20 | 1 | no |

No ellipsis, no stitching, no added punctuation. X-canon lbs use ed="X".

## 2. Attribution — CORRECT ✅
- Occ1 governing mulu: 洞山良价禪師 ✅ (verse = Dongshan).
- Occ2 governing mulu: 撫州曹山元證本寂禪師 ✅ (五燈全書; roster 曹山本寂; 元證 posthumous title, as noted).
- Occ3: nearest *non-empty* mulu is 曹山大師語錄序, but the empty `<cb:mulu n="1" type="卷">`
  (juan-1 body start, raw offset 4590) precedes the match (offset 5450): the passage sits in
  the MAIN body of 撫州曹山元證禪師語錄, which opens 師諱本寂… — so 師曰 = 曹山本寂 ✅ (audited
  specifically; not a preface quotation).
- Occ4 governing mulu: 曹山本寂禪師 ✅. Prose 旨訣/君臣 definitions consistently Caoshan; verse
  consistently Dongshan — the five-ranks cross-attribution rule is honored.
- Occ5 governing mulu: 明州天童宏智正覺禪師 ✅; roster has 宏智正覺 ✅.
- Occ6 governing mulu: 南昌府百丈瑞白明雪禪師 — not in master-dates.json → null correct ✅;
  duplicate carrying in X80n1566 verified (X 0472b15, same mulu head) ✅.

## 3. Allowlist — CLEAN ✅
All occurrence RelPaths + all 8 SourceTexts in zen-corpus.json. Headword attestation
(raw grep; tag-crossing only raises counts): X85n1593=8, X81n1571=11, X82n1571=14,
X84n1583=10, X84n1585=6, X86n1605=2, T47n1987A=4, and T47n1986B — raw grep shows 0 because
the string crosses an `<lb>` tag, but the tag-stripped stream attests it at T 0525c02
(偏中正。失曉老婆逢古鏡…) ✅. Every SourceText attests the headword.

## 4. Explanation honesty — ALL PHRASES ATTESTED ✅
- 偏中正，失曉老婆逢古鏡。分明覿面別無真，休更迷頭猶認影 — X85n1593 0480a23–24 ✅; also T47n1986B 0525c02 ✅
- 正位即空界…偏中正者，舍事入理 — X85n1593 0481a12–14; X81n1571 0647b22; T47n1987A 0527a05–07 ✅
- 因有僧問五位君臣旨訣。師曰 — T47n1987A 0527a05 ✅
- 君為正位，臣為偏位 / 臣向君是偏中正 — X85n1593 0481a16–17 ✅ (also X81n1571, grep-confirmed)
- 如何是偏中正？師曰：白髮老婆羞看鏡 — X81n1571 0682c18 ✅
- 以濟宗論之 / 正中偏奪人也，偏中正奪境也 — X82n1571 0279b20 ✅; X80n1566 0472b15 ✅
"The exact inverse of 正中偏者，背理就事" is a structural statement of the attested parallel
text, not an annotator gloss — acceptable.

## 5. Multi-source — HOLDS ✅
Verse: X85n1593 + T47n1986B + X81n1571 (grep-confirmed 偏中正，失曉…). Gloss: X85n1593 +
X81n1571 + T47n1987A. ≥2 independent witnesses per component; three roster masters. Justified.

## 6. Describe-only — CLEAN ✅
Graphs, in-corpus self-definition, allegory assignment, test-question deployment, attested
濟宗 mapping — all quoted, all verified. No intent/force vocabulary; closes with the
no-further-gloss disclaimer.

## 7. Nesting / RelatedTerms — GENUINE ✅
正中偏 · 正中來 · 兼中至 · 兼中到 · 五位 · 君臣 all co-occur in the quoted passages. The
偏中正 ↔ 正中偏 mutual cross-reference exists in both entries.

## Punch list (non-blocking observations)
1. **Borderline (same convention question as t_d4661c1b4dbb):** Occ5's KWIC is two-speaker —
   the headword is in the anonymous monk's question; Hongzhi speaks the answer.
   MasterName=宏智正覺 matches the governing mulu and buffalo-pilot precedent, and the note is
   transparent; a stricter two-speaker→null policy would null it. Editor's call.

Defects: 0.
