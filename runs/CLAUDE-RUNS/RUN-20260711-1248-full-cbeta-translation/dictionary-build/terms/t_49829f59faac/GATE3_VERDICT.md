# GATE 3 VERDICT — t_49829f59faac 函蓋乾坤

VERDICT: PASS

Audited: 2026-07-12, independent re-derivation from corpus (`xml-p5`), allowlist (`zen-corpus.json`), roster (`master-dates.json`). Tag-stripped main-text extraction (foot notes/rdg/sic/orig/cb:mulu excluded; inline notes kept).

## 1. KWIC integrity — 6/6 PASS (all verbatim contiguous main text)

| RelPath | KWIC (head) | Found | lb verified |
|---|---|---|---|
| T47n1988 | 函蓋乾坤目機銖兩。不涉春緣。 | 1x main text | T 0563a23 ✓ |
| T48n2006 | 師示眾云。函蓋乾坤。目機銖兩。不涉萬緣。 | 1x | T 0312a07 ✓ |
| T48n2006 | 洞然明白。則函蓋乾坤也。 | 1x | T 0312a11 ✓ (inside `<note place="inline">` as documented) |
| J29nB244 | 函蓋乾坤事莫窮，頭頭物物露真風 | 1x | J 0718b16 ✓ |
| X80n1565 | 昔日雲門有三句。謂函蓋乾坤句。截斷眾流 | 1x | ed=X 0373a24 ✓ |
| X80n1565 | 僧問。如何是函蓋乾坤句。 | 1x | ed=X 0315a15 ✓ |

No ellipsis, no stitching, no apparatus-only matches. T47n1988 KWIC correctly begins AFTER 示眾云天中; apparatus note n=0563002 verified verbatim: `天中＝大眾【明】` (foot text) — exactly as the AttributionNote states.

## 2. Attribution — PASS

- T47n1988 → 雲門文偃: governing section is 垂示代語 in Yunmen's own 廣錄; passage opens 示眾云 and continues 作麼生承當。代云。一鏃破三關 (0563a24, verified). Single-speaker ✓.
- T48n2006 0312a07 → null: compiler retelling (眾無對。自代云。一鏃破三關。後來德山圓明密禪師。遂離其語為三句 — all verified verbatim), under mulu/head 雲門宗 → 三句 ✓.
- T48n2006 0312a11 → 圜悟克勤: gloss confirmed inside `<note place="inline">` opening 圓悟曰, spanning lbs 0312a10–a12 with 則函蓋乾坤也 on 0312a11 ✓. Roster has 圜悟克勤 ✓.
- J29nB244 verse → null ✓; X80n1565 上堂 raising (under 嘉定府九頂寂惺惠泉禪師) → null ✓; X80n1565 0315a15 僧問 exchange under head 信州西禪欽禪師 (verified; answer 師曰。天上有星皆拱北) → null ✓.
- Roster: 雲門文偃 ✓, 德山緣密 ✓, 圜悟克勤 ✓.

## 3. Allowlist — PASS

All 4 occurrence RelPaths + all 7 SourceTexts in zen-corpus.json ✓. Quoted-in-explanation files (B25n0144, J25nB175, J25nB171, J40nB472, X78n1556, T51n2077) also all allowlisted ✓. Every SourceText attests 函蓋乾坤: T47n1988 2x, T48n2006 5x, J29nB244 4x, X80n1565 4x, X78n1556 10x, X85n1593 9x, T51n2077 8x ✓.

## 4. Explanation honesty — PASS (every count/quote grep-verified)

- 如何是函蓋乾坤句: claimed "54 occurrences in 33 allowlist texts" — measured **54 in 33** exactly ✓.
- 涵蓋乾坤 variant: claimed once in allowlist at J40nB472 — measured **1x, J40nB472 only**; context verbatim 有時芥子藏身，有時涵蓋乾坤 ✓ (and, as claimed, not a 雲門三句 context).
- 函蓋相稱 14x / 函蓋相應 21x — "recurs" ✓; B25n0144 著函，函蓋相稱故 verbatim 1x ✓.
- Answers verified as answers to the question: 日出東方夜落西 (T51n2077 ✓, X78n1556 ✓), 遍界黑漫漫 (J25nB175 ✓), 吞吐虛空 (J25nB171 ✓).
- Verse 函蓋乾坤體自然，箇中原不著毫端: attested 1x, J25nB171, as 頌 on 舉雲門三句 ✓. Verse 函蓋乾坤事莫窮… = the J29nB244 KWIC ✓.
- Edition-variant claim 不涉春緣 (T47n1988) vs 不涉萬緣 (T48n2006 ✓, J29nB244 1x ✓) — verified.
- Yuanwu gloss 本真本空。一色一味。非無妙體。不在躊躇。洞然明白。則函蓋乾坤也 verbatim 1x in T48n2006 ✓.

## 5. Multi-source — PASS (T + J + X canons, 4 independent witnesses in occurrences alone).

## 6. Describe-only — PASS. Literal graph gloss + attested deployment + structural facts; closes with the no-further-gloss formula. No intent/force vocabulary found.

## 7. Nesting/RelatedTerms — PASS. 截斷眾流/隨波逐浪 = the other two 三句 (attested in KWICs themselves); 目機銖兩/不涉萬緣 = co-lines of Yunmen's original triad (attested); 雲門三句 attested 74x in 44 allowlist texts. All genuine.

## Punch list (non-blocking nits, 2)

1. **Punctuation-normalized citation.** Explanation cites `函蓋乾坤句、截斷眾流句、隨波逐浪句 (人天眼目 T48n2006; also J29nB244)`. The 、-punctuated string is verbatim only in J29nB244; T48n2006 reads `曰函蓋乾坤句。截斷眾流句。隨波逐浪句` (。 separators). Characters identical, punctuation differs — recommend attributing the exact punctuation to J29nB244 only.
2. **Wrong co-located R-lb in a parenthetical.** AttributionNote for X80n1565 0373a24 says "(co-located ed=R138 0693a09 not used)"; the actual co-located R lb is **0693a08**. Cosmetic — the FromLb/ToLb fields correctly use ed=X 0373a24.

Defects: 0 blocking, 2 nits.
