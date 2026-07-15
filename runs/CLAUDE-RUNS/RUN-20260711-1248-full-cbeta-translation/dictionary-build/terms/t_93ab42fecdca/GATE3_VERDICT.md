# GATE 3 VERDICT — t_93ab42fecdca · 本來無一物

**VERDICT: PASS**

**Auditor:** Gate 3 independent adversarial pass (Frizzle batch, entry.v2.json)
**Date:** 2026-07-12 01:06 +02:00
**Method:** Python re-derivation over `C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5`,
Zen-scoped to the 462-text `zen-corpus.json` allowlist; body text only (`<note>`, `<rdg>`,
`<sic>`, `<orig>` stripped, whitespace collapsed). Every KWIC re-anchored at its `FromLb`.

## 1. KWIC integrity — PASS
All 6 curated KWICs are exact contiguous verbatim substrings, each 1x in its file and confirmed
inside the FromLb line window:
- T48n2008 @0349a07 `明鏡亦非臺；本來無一物，何處惹塵埃？` ✓
- B14n0082 @0167a07 `本非樹心鏡亦非臺本來無一物何假拂塵埃` ✓; @0236b12
  `師有時垂語曰直道本來無一物猶未消得佗鉢袋子` ✓
- B25n0144 @0416b11 `洞山答曰：「直道本來無一物，也未得衣缽在。」` ✓ (祖堂集's punctuation is in
  the source file)
- C077n1710 @0633b05 `問本來無一物無物便是否師云無亦不是菩提無是處` ✓
- B27n0152 @0563b02 `舉六祖云本來無一物長慶云萬象之中獨露身` ✓

## 2. Attribution — PASS (including the two rules this audit was told to stress)
- **T48n2008 → 慧能** ✓: received 宗寶 edition (六祖大師法寶壇經), governing cb:mulu `1 行由`,
  and the verse is introduced `惠能偈曰` (text spells 惠能; roster 慧能 — disclosed in the note) ✓.
  Huineng attribution in the RECEIVED Platform Sutra is exactly the sanctioned call.
- **B14n0082 @0167a07 → 慧能** ✓ with honest framing: 傳燈玉英集（殘卷）, governing cb:mulu
  `三十二祖弘忍大師` (episode falls in Hongren's chapter — disclosed); the narrative
  `能自秉燭令童子於秀偈側寫一偈云菩提本非樹…` and `大師後見此偈` are verbatim in the file ✓.
  Same episode, not a later raising — attribution defensible and transparently argued.
- **洞山良价 verdict, two independent witnesses** ✓: B25n0144 (祖堂集) mulu `洞山和尚`, question
  形 時時勤拂拭…因什摩不得衣缽 verbatim ✓; B14n0082 mulu `筠州洞山良价禪師` (師 = 洞山) ✓.
- **黃檗希運** ✓: C077n1710 governing mulu `黃檗斷際禪師宛陵錄`; BOTH couplet quotes anchored at
  0633b04 and 0636c03, both under the 宛陵錄 mulu ✓.
- **B27n0152 raising → MasterName null** ✓: 舉六祖云… in 普濟玉琳國師語錄 小叅 — a later raising,
  correctly null per the raised-case rule.

## 3. Allowlist — PASS (including the negative control)
All occurrence RelPaths and all 5 SourceTexts in zen-corpus.json ✓.
**Negative control verified: the Dunhuang 壇經 T48n2007 contains ZERO instances of 本來無一物**
(its third line is 佛性常清淨, present 1x; 何處有塵埃 1x) — and T48n2007 is correctly NOT listed
in SourceTexts, with the contrast explicitly quarantined in the Note as "reported for contrast
only" ✓. Every listed SourceText attests the exact headword (counts 1/5/3/5/3) ✓.

## 4. Explanation honesty — PASS (every count re-derived EXACT)
- 本來無一物 214 hits / 106 texts — recount: **214 / 106** ✓
- 本無一物 34 / 30 — recount: **34 / 30** ✓
- Received-text verse `菩提本無樹，明鏡亦非臺；本來無一物，何處惹塵埃` — verbatim 1x in T48n2008 ✓
- 祖堂集 "carries the verse twice, with first lines 菩提本無樹 and 身非菩提樹" — verified: two verse
  instances (張日用 writes 身非菩提樹，心鏡亦非臺。本來無一物，何處有塵埃; 盧行者's 偈 菩提本無樹，
  明鏡亦非臺。本來無一物，何處有塵埃), couplet 何處有塵埃 2x ✓
- 傳燈玉英集 variant (first line 菩提本非樹, couplet 本來無一物何假拂塵埃) ✓
- Dongshan verdict, both wordings, both witnesses ✓
- Huangbo: couplet quoted twice (0633b04, 0636c03) ✓; Q&A 無亦不是 ✓
- Third-line claim: in the received verse 本來無一物 is indeed line 3 ✓

## 5. Multi-source — PASS
壇經 (T), 祖堂集 (B), 傳燈玉英集 (B), 古尊宿語錄 (C), 玉琳語錄 (B); masters 慧能, 洞山良价,
黃檗希運. Earned.

## 6. Describe-only — PASS
Literal gloss, verse-recension facts (Dunhuang vs received — a text-critical observation the corpus
itself evidences), attested deployments (verdict / quotation / Q&A / stock raising — genre labels,
not readings). The English inside parentheses is translation of quoted Chinese. Ends with the
no-gloss sentence. No banned vocabulary.

## 7. Nesting / RelatedTerms — PASS
本來面目 ↔ 本來無一物: 本來面目 exists as its own entry (t_1c7d25824f85); the relation is a
deliberate semantic link (both 本來-terms from the Huineng cluster), not a coincidental prefix ✓.
何處惹塵埃 / 菩提本無樹 / 明鏡亦非臺 are the verse's attested constituent lines; 本無一物 is the
grep-verified contracted form. All genuine.

## Punch list (advisory only — nothing blocks PASS)
1. **(minor, other entry)** The link is not reciprocated: t_1c7d25824f85 本來面目 lists
   RelatedTerms [父母未生前, 不思善不思惡, 見性] without 本來無一物. If bidirectional linking is
   the convention, add the back-link on the 本來面目 side (no change needed in THIS entry).
2. **(note)** The B14n0082 verse variant reads 心鏡亦非臺 (not 明鏡); the entry quotes the KWIC
   correctly and only claims the first line and couplet as variants, so no error — merely a further
   variant the Explanation could mention.
