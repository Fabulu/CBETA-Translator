# GATE 3 VERDICT — t_9a5dc768cbc5 平常心是道

VERDICT: PASS

Audited: 2026-07-12, independent re-derivation from corpus (`xml-p5`), allowlist (`zen-corpus.json`), roster (`master-dates.json`). Tag-stripped main-text extraction (foot notes/rdg/sic/orig/cb:mulu excluded).

## 1. KWIC integrity — 5/5 PASS (all verbatim contiguous main text)

| RelPath | KWIC (head) | Found | lb verified |
|---|---|---|---|
| B14n0082 | 若欲直會其道平常心是道謂平常心無造作 | 1x | B 0310b03 ✓ |
| T48n2005 | 南泉因趙州問。如何是道。泉云。平常心是道。 | 1x | T 0295b14 ✓ |
| T51n2076 | 南泉曰。平常心是道。師曰。還可趣向否。 | 1x | T 0276c15 ✓ |
| C077n1710 | 南泉和尚如何是道泉云平常心是道 | 1x | C 0846c17 ✓ |
| B25n0145 | 趙州問南泉。如何是道。泉云。平常心是道。 | 1x | B 0907b11 ✓ |

No ellipsis/stitch/apparatus artifacts.

## 2. Attribution — PASS (matches the exact rule the audit specifies)

- B14n0082 → 馬祖道一: governing cb:mulu (level 2) is **江西大寂道一禪師** (verified in doc order, last head before the KWIC). Passage opens 道一禪師示眾云道不用修但莫污染 (verbatim 1x) — single-speaker 示眾 → Mazu ✓.
- T48n2005 → null: 無門關 two-speaker 南泉因趙州問 ✓. Governing mulu is 平常是道 and it is the **19th case** (22nd mulu minus 3 front-matter entries 禪宗無門關×2 + 目錄) — "case 19" claim verified ✓.
- T51n2076 → null: governing mulu (level 8) is 趙州東院從諗禪師 — Zhaozhou's biography as documented; two-speaker ✓.
- C077n1710 → null: governing mulu verified L1 **龍門佛眼禪師語錄五** → L2 **普說** — exactly as AttributionNote states (Foyan's record, NOT a Zhaozhou section); raised dialogue ✓. Full raise verbatim: 所以趙州問南泉和尚如何是道泉云平常心是道州從此頓息馳求 (1x) ✓.
- B25n0145 → null: embedded dialogue; Zhongfeng's frame remarks 此話流布叢林 / 古今之下鮮有不墮於意識者 / 盡謂著衣喫飯動靜語默一一天真 all verbatim 1x each ✓.
- Roster: 馬祖道一 ✓, 南泉普願 ✓, 趙州從諗 ✓.

## 3. Allowlist — PASS

All 5 occurrence RelPaths + all 7 SourceTexts in zen-corpus.json ✓. Every SourceText attests the headword: B14n0082 2x, T48n2005 1x, T51n2076 3x, C077n1710 10x, B25n0145 3x, T51n2077 4x, D48n8939 9x ✓.

## 4. Explanation honesty — PASS (every quote and count grep-verified)

- Mazu block: 道不用修但莫污染 ✓; 若欲直會其道平常心是道 ✓ (KWIC); 謂平常心無造作無是非無取捨無斷常無凡無聖 verbatim 1x ✓; 非凡夫行非賢聖行是菩薩行 1x ✓; 只如今行住坐臥應機接物盡是道 1x ✓.
- Dialogue block: 州云。還可趣向否。泉云。擬向即乖 (T48n2005) 1x ✓; 道不屬知。不屬不知 (T48n2005) 1x ✓; 知是妄覺不知是無記 (T51n2076) 1x ✓; 無門關 case-title 平常是道 ✓.
- Count claim 還可趣向也無 "15 allowlist hits": measured **15** (in 15 texts) — exact ✓.
- Zhongfeng quotes verified (above).

## 5. Multi-source — PASS (B + T + C canons; Mazu locus and dialogue locus each multiply attested).

## 6. Describe-only — PASS. Literal gloss + the texts' own 謂…-gloss and continuations, closing with the explicit no-further-gloss formula. No interpretive vocabulary detected.

## 7. Nesting/RelatedTerms — PASS. 平常心 = genuine constituent; target entry t_4ccf8aed47d3 exists in terms/. 道不用修 (attested B14n0082) and 道不屬知 (attested T48n2005) are the attested co-lines of the two loci. All genuine.

## Punch list

None blocking. One cosmetic observation: the Note refers to the master as 天目中峰明本 (prose epithet); the roster CanonicalName is 中峰明本. No field impact (occurrence MasterName is null; 中峰 is not in RelatedMasters).

Defects: 0.
