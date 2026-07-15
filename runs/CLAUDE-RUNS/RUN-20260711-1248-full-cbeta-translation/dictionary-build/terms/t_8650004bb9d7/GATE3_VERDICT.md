# GATE 3 VERDICT — t_8650004bb9d7 · 兼中至

**VERDICT: PASS**

**Auditor:** Gate 3 independent adversarial pass (Frizzle batch, entry.v2.json)
**Date:** 2026-07-12 01:06 +02:00
**Method:** Python re-derivation over `C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5`,
Zen-scoped to the 462-text `zen-corpus.json` allowlist; body text only (`<note>`, `<rdg>`,
`<sic>`, `<orig>` stripped, whitespace collapsed). Every KWIC re-anchored at its `FromLb`.

## 1. KWIC integrity — PASS
All 6 curated KWICs are exact contiguous verbatim substrings, each 1x in its file and confirmed
inside the FromLb line window:
- T47n1986B @0525c05 `兼中至。兩刃交鋒不須避。好手猶如火裏蓮` ✓ (continuation 宛然自有冲天志
  anchored at 0525c06 exactly as the note says ✓)
- T47n1987B @0544b26 `偏中至者中孚也。隨物不礙` ✓; @0544c08 `裏頭才轉身。塵中未帶名。是曰兼中至。不是心不是境` ✓
- T47n1987A @0533c02 `裏頭才轉身。塵中未帶名。是曰偏中至。不是心。不是境` ✓
- C077n1710 @0685a03 `問如何是兼中至師云意氣不從天地得英雄豈藉四時推` ✓
- J25nB174 @0728a15 `乃知偏中至不可以兼中至而重犯兼中到也` ✓
The headline cross-edition fact — at the IDENTICAL prose slot the 本寂 edition (T47n1987B) reads
是曰兼中至 while the 元證 edition (T47n1987A) reads 是曰偏中至 — is grep-confirmed in both files,
including the punctuation split 木舟中虛虛通自在 (1987B) vs 木舟中虛。虛通自在 (1987A) ✓.

## 2. Attribution — PASS
- **洞山良价** (T47n1986B = 瑞州洞山良价禪師語錄): 作五位君臣頌 present 1x; the five rank names
  occur in exactly the claimed cycle order (character positions 正中偏 9064 → 偏中正 9092 →
  正中來 9120 → 兼中至 9148 → 兼中到 9176) ✓.
- **曹山本寂** (both T47n1987A 撫州曹山元證禪師語錄 and T47n1987B 撫州曹山本寂禪師語錄): governing
  cb:mulu `五位旨訣` for all three Caoshan occurrences ✓. Two editions of one master's record,
  correctly attributed to the same master with the edition dispute surfaced.
- **汾陽善昭** (C077n1710): governing mulu chain verified — level-1 `汾陽昭禪師語錄` (卷11), then
  level-2 `語錄` ✓; the lead-in 師因頌五位纔畢便有僧問 is verbatim in the file ✓.
- **覺浪道盛** (J25nB174 = 天界覺浪盛禪師語錄): governing cb:mulu `洞宗標正` ✓; the further quotes
  anchored — 正來偏至，聚訟至今 @0728b24 ✓, 偏中至者，即臣攝政也 @0729a08 ✓, and the section title
  洞曹君臣正偏及功勳父子主賓五位參同宗旨 exists (2x) ✓.
- **密雲圓悟** (Explanation): J10nA158 = 密雲禪師語錄; the quoted stanza 兼中至，一棒當頭沒迴避 is
  in the master's OWN verse set (attendant recites Dongshan's 偈, then 者云：「請和尚各為頌出。」
  師頌云：「正中偏，一棒當頭絕謂言…「兼中至，一棒當頭沒迴避…) — "his own 頌" is exactly right ✓.
All five RelatedMasters on the roster ✓.

## 3. Allowlist — PASS
All occurrence RelPaths and all 6 SourceTexts in zen-corpus.json ✓. Every SourceText attests the
fourth rank: T47n1986B (兼中至 1x), T47n1987B (兼中至 2x + 偏中至), T47n1987A (偏中至 — the
edition-variant witness, explicitly cited as such), C077n1710 (4x), J25nB174 (1x + 偏中至),
J10nA158 (2x) ✓.

## 4. Explanation honesty — PASS (every count re-derived EXACT)
- 兼中至 207 hits / 64 texts — recount: **207 / 64** ✓
- 偏中至 85 / 33 — recount: **85 / 33** ✓
- 兼中到 276 / 84 — recount: **276 / 84** ✓
- 如何是兼中至 88 / 46 — recount: **88 / 46** ✓
- 偏中到 "does not occur" — recount: **0** ✓
All quoted Chinese verified verbatim: Dongshan stanza (incl. 宛然自有冲天志), both Caoshan edition
readings, Fenyang Q&A, Miyun stanza, all three Juelang quotes. The 兼中至/偏中至 cross-edition fact —
the exact item this audit was told to stress — is accurate and correctly framed (two names, one
fourth rank, distinct from fifth-rank 兼中到).

## 5. Multi-source — PASS
Dongshan, Caoshan (2 editions), Fenyang, Miyun, Juelang; T, C, J canons. Earned.

## 6. Describe-only — PASS
Rank-name identification, attested cycle, counts, edition variance, and the corpus's OWN
characterization of the dispute (聚訟至今) and its own glosses (中孚 hexagram; 即臣攝政也 — both
X者…也 self-definitions in the texts). English is translation of quoted text. Ends with the
no-gloss sentence. No banned vocabulary.

## 7. Nesting / RelatedTerms — PASS
偏中至 / 兼中到 / 正中偏 / 偏中正 / 正中來 — all attested rank names; sibling entries exist for
正中偏, 偏中正, 正中來, 兼中到 and link back consistently to 兼中至 ✓.

## Punch list (advisory only — nothing blocks PASS)
1. **(minor)** AlternateTargets contains "variant of the fourth-rank name 偏中至" — a cross-reference,
   not a rendering; arguably belongs in Note/RelatedTerms rather than the target list.
2. **(minor)** 偏中至 itself has no standalone entry yet while the other four rank names do; since the
   entry establishes 偏中至 as the same rank's second name, a stub or redirect entry would complete the
   family navigation.
