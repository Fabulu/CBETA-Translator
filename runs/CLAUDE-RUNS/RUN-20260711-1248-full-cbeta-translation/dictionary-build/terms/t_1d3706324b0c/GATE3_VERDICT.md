# GATE 3 VERDICT — t_1d3706324b0c · 打成一片

**VERDICT: PASS**

Independent adversarial audit, 2026-07-12. All checks run from scratch against
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5`, allowlist `zen-corpus.json` (462 texts),
roster `master-dates.json`. Method: XML-tag/apparatus-stripped flow text (teiHeader, `<note>`,
`<cb:mulu>`, `<rdg>` removed; whitespace collapsed), per-char lb mapping.

## 1. KWIC integrity — ALL 6 PASS
Every Kwic is an exact contiguous verbatim substring of its cited file, unique (1 match),
with the match's lb range equal to the claimed FromLb..ToLb:

| RelPath | Kwic (head) | matches | lb found | lb claimed |
|---|---|---|---|---|
| B/B25/B25n0145.xml | 自然打成一片。直得內心外境… | 1 | 0762b02–0762b03 | ✓ |
| J/J25/J25nB171.xml | 十二時中須教打成一片，自無不辦之理。 | 1 | 0579a01–0579a01 | ✓ |
| J/J25/J25nB154.xml | 打成一片，也無生的心，… | 1 | 0033b28–0033b29 | ✓ |
| J/J26/J26nB188.xml | 打成一片，情境俱亡，… | 1 | 0757a20–0757a20 | ✓ |
| B/B27/B27n0152.xml | 行住坐臥動靜閒忙打作一片 | 1 | 0614a01–0614a02 | ✓ |
| J/J25/J25nB163.xml | 老僧四十年方打成一片。 | 1 | 0240a22–0240a23 | ✓ |

No ellipsis, no stitching, no added punctuation (B-canon KWICs correctly unpunctuated;
J-canon punctuation matches source byte-for-byte). No apparatus text needed for any match.

## 2. Attribution — ALL CORRECT (verified at governing cb:mulu / speaker context)
- **B25n0145 @0762b02 → 中峰明本**: governing cb:mulu = 示鄭廉訪 (head 示鄭廉訪雲翼字鵬南) ✓.
  Continuous first-person instruction on 趙州無字 practice; immediately preceded by
  纔有此心即間[0762b02]斷矣。乆乆綿密 — exactly as AttributionNote says. Own words ✓. On roster ✓.
- **J25nB171 @0579a01 → 天隱圓修**: cb:mulu = 與吳迪美居士 (head 與吳迪美居士三) — a letter ✓.
  Preceding text 工夫切不可當有當無…這一著子不可放過 matches AttributionNote quote ✓. On roster ✓.
- **B27n0152 @0614a01 → 玉林通琇**: cb:mulu = 工夫說 ✓, treatise prose, own words ✓. On roster ✓.
- **J25nB163 @0240a22 → null**: cb:mulu = 華嚴大意 ✓. Text verbatim: 昔香嚴和尚在溈山，問一答十…
  痛與一劄，嚴不知所以，于是入南陽卓菴擊竹有省。後出世曰：「老僧四十年方打成一片。」 followed by
  趙州和尚二十餘年 / 高峰三十年不出死關 — raised/recounted saying, null correct ✓.
- **J25nB154 @0033b28 → null**: cb:mulu = 示生禪人 ✓ (法語). 唯菴德然 confirmed OFF-roster → null correct ✓.
- **J26nB188 @0757a20 → null**: cb:mulu = 上堂 ✓; head-quote phrases 可將個本參話頭頓在目前 /
  如雞抱卵、如貓捕鼠 occur immediately before the KWIC line ✓; title = 入就瑞白禪師語錄 ✓;
  瑞白明雪 confirmed OFF-roster → null correct ✓.

## 3. Allowlist — PASS
All 6 occurrence RelPaths + all 6 SourceTexts in zen-corpus.json ✓. Every SourceText attests
the headword (or its stated variant 打作一片 for B27n0152) ✓. Note-cited files J25nB159 and
J27nB189 also allowlisted ✓.

## 4. Explanation/Note honesty — PASS (all quoted phrases grep-verified, all counts exact)
- 打成一片 = **311× in 139 files** — measured 311× in 139 ✓ EXACT.
- 打作一片 = **6× in 6 files** — measured 6× in 6 ✓ EXACT.
- 古人四十年打成一片 "recurs (3 files)" — measured exactly 3 files (J20nB098, J33nB294, J36nB359) ✓.
- Quoted phrases, each 1 hit in the cited file: 纔有此心即間斷矣。乆乆綿密自然打成一片 (B25n0145) ✓;
  地獄天堂打成一片。菩提煩惱坐斷兩頭 (B25n0145, in 中峰's own 廣錄, section 別傳覺心) ✓;
  古人道我數十年打成一片又云我數十年尚有走作 (B27n0152) ✓; 佛法世法打成一片 (J25nB159) ✓;
  所謂行住坐臥動靜閒忙打作一片工夫到得一片時節若至其理自彰 (B27n0152) ✓.
- Note's 湧泉景欣 claim: J27nB189 (title verified = 三宜盂禪師語錄) reads verbatim
  湧泉景欣禪師云：『我四十年在裏許，尚有走作，汝等諸人，莫開大口。』 — the text itself names
  湧泉景欣 as speaker, exactly as the Note states ✓.

## 5. Multi-source — PASS
Three roster masters attributed in their own records (中峰明本, 天隱圓修, 玉林通琇) + three further
independent witnesses (off-roster, correctly nulled). 6 files across B/J canons, Yuan→Qing,
Linji and Caodong lines. `multi-source` justified ✓.

## 6. Describe-only — PASS with 1 minor flag
The Explanation reports graphs, deployment, dated decades, and the texts' own contrasts, and closes
with the no-gloss formula ✓. **Minor flag:** Note sentence "打成一片 (unbroken / continuous) vs 間斷
(a break)" — the parenthetical "(unbroken / continuous)" is a semantic gloss of the headword beyond
its literal sense ("pounded into one piece"). It is inferred from the attested 間斷/走作 contrasts,
not stated by any text. Recommend deleting the parenthetical or rephrasing to gloss only 間斷/走作
(which ARE literal). Not verdict-blocking: it annotates the contrast structure, not the term's
doctrinal force, and the surrounding claims are all grep-attested.

## 7. Nesting / RelatedTerms — PASS
RelatedTerms = [打作一片]: genuine attested variant graph (6× in 6 files), same syntactic slot
(玉林's 工夫說 uses it exactly where others use 打成一片). Not a coincidental prefix. Correct ✓.

## Punch list
1. (minor, optional) Remove/reword the "(unbroken / continuous)" gloss in Note — see §6.

**Defects: 0 blocking, 1 minor.**
