# GATE 3 VERDICT — t_ed962dfd1158 四賓主

VERDICT: PASS

Audited: 2026-07-12, independent re-derivation from corpus (`xml-p5`), allowlist (`zen-corpus.json`), roster (`master-dates.json`). Tag-stripped main-text extraction (foot notes/rdg/sic/orig/cb:mulu excluded); apparatus swept separately where the entry makes apparatus claims.

## 1. KWIC integrity — 5/5 PASS (all verbatim contiguous MAIN text)

| RelPath | KWIC (head) | Found | lb verified |
|---|---|---|---|
| T47n1985 | 此是膏肓之病不堪醫，喚作客看主。 | 1x main text | T 0501a07→0501a08 ✓ |
| T48n2006 | 四賓主者。師家有鼻孔。名主中主。 | 1x | T 0311b16 ✓ |
| T48n2006 | 學人無鼻孔。名賓中賓。與曹洞賓主不同。 | 1x | T 0311b18 ✓ |
| J29nB233 | 昔臨濟大師建立四料揀、四賓主、四照用、三玄、三要 | 1x | J 0352b19 ✓ |
| X73n1457 | 主看賓，有時賓看主，有時主看主，有時賓看賓 | 1x | ed=X 0859b19 ✓ |

**The specific trap checked:** the T47n1985 KWIC is the Taisho MAIN text (客-form, 不堪醫 + 喚作客看主). The 賓-forms live ONLY in the Ming apparatus — verified: foot-text note n=0505003 (次下明本有…) contains 不堪醫治喚作賓看主 / 此是主看賓 / 喚作賓看賓, exactly as the AttributionNote describes. The KWIC is NOT apparatus-derived ✓.

## 2. Attribution — PASS

- T47n1985 → 臨濟義玄: the passage sits in the main record body between the preface mulus and the 勘辨 mulu (offset check: KWIC at 10155; 勘辨 mulu at 13875) — "the section preceding 勘辨" ✓. Immediate context is continuous 道流-addressed instruction (道流！如禪宗見解…參學之人大須子細，如主客相見便有言論往來…), single speaker ✓. All four prose configurations verified at claimed lbs: 喚作客看主 0501a08 ✓, 此是主看客 0501a10 ✓, 此喚作主看主 0501a13 ✓, 呼為客看客 0501a15 ✓; 如主客相見便有言論往來 at 0501a03.
- T48n2006 (both) → null: compiler exposition; governing mulu/head verified **臨濟門庭** ✓. Caodong contrast quote verified under **曹洞門庭** ✓.
- J29nB233 → null: governing mulu verified **示敏上座** ✓; an enumeration of Linji's sets, not Linji's words ✓.
- X73n1457 → null: correct — and doubly so, since the passage is framed as a quotation (如臨濟大師云：山僧有時一喝如金剛寶劒…有時主看賓，有時賓看主…) — raised → null ✓ (see punch item 2).
- Roster: 臨濟義玄 ✓.

## 3. Allowlist — PASS with one rule-letter advisory

All 5 occurrence RelPaths + all 7 SourceTexts in zen-corpus.json ✓. Headword attestation per SourceText: T48n2006 6x, J29nB233 2x, X73n1457 1x, J25nB171 11x, J33nB294 7x, J34nB299 6x ✓ — and **T47n1985 0x** (see punch item 1).

## 4. Explanation honesty — PASS (every claim grep-verified)

- Central negative claim "四賓主 does NOT occur in T47n1985 (0 occurrences, main text and apparatus alike)": verified — **0x main text, 0x apparatus-inclusive** ✓. A verified negative, exactly as the guide values.
- Full 人天眼目 definition verbatim 1x: 四賓主者。師家有鼻孔。名主中主。學人有鼻孔。名賓中主。師家無鼻孔。名主中賓。學人無鼻孔。名賓中賓。與曹洞賓主不同 ✓.
- Caodong contrast verbatim 1x: 四賓主。不同臨濟。主中賓。體中用也 (under 曹洞門庭) ✓.
- 看-scheme quotes: main-text 客-forms ✓ (lbs above); Ming-apparatus 賓-forms ✓ (note 0505003); X73n1457 有時賓看主，有時主看主，有時賓看賓 ✓.
- Grouping quote = J29nB233 KWIC ✓.
- 四賓主 corpus presence: 130x in 78 allowlist texts (comfortably multi-source).

## 5. Multi-source — PASS (T + J + X canons; label attested in 6 cited texts + 70 more).

## 6. Describe-only — PASS. Enumeration schemes, edition variants, the texts' own 有鼻孔/無鼻孔 definition and their own 不同 contrast; closes with the no-further-reading formula. No interpretive vocabulary detected.

## 7. Nesting/RelatedTerms — PASS. 賓主 = genuine constituent; target entry t_6da91f8ce284 exists in terms/. 四料揀/四照用/三玄三要 co-occur in the very enumeration KWIC (三玄三要 attested 488x corpus-wide). All genuine.

## Punch list (non-blocking advisories, 2)

1. **SourceTexts includes T47n1985, which does not attest the headword (0x).** Technically against the letter of "every SourceText attests the headword." Here it is the entry's central, verified structural fact (the record the label systematizes), documented in Explanation, Note, AND AttributionNote — so not contamination and not silent. Accepted as documented exception; if the schema consumer assumes SourceTexts ⇒ headword-attesting, consider carrying T47n1985 in Note only.
2. **X73n1457 occurrence: KWIC does not contain the headword, and the note omits the quotation frame.** The file DOES attest 四賓主 once, elsewhere (…如他宗之四賓主．三玄要… — a Caodong-side passage likening the schools' devices), so the SourceText is sound, but the anchored KWIC shows only the 看-scheme enumeration; moreover that enumeration is introduced as 如臨濟大師云 (a fused four-shouts + guest-host quotation), which the AttributionNote describes only as "discourse enumeration." MasterName null remains correct (raised). Recommend: mention the 如臨濟大師云 framing, and/or add the actual 四賓主 locus as the attesting anchor.

Defects: 0 blocking, 2 advisories.
