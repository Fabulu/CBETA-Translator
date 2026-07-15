# GATE 3 VERDICT — t_2852a9ae231c · 隨波逐浪

VERDICT: REVISE

**Auditor:** Gate 3 independent adversarial pass (Claude, 2026-07-12). Re-derived from
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` via tag-stripped exact-substring search
(apparatus `<note>`/`<rdg>` dropped), lb tracked per `ed`, governing `cb:mulu` tracked.

## DEFECTS (fix before merge)

### D1 — MAJOR (attribution / false roster claim): sense 1, occ 4 (X/X81/X81n1568.xml, 0140c07)
The AttributionNote claims the section master 東京法雲寺法秀圓通禪師 is "not on roster" and
nulls MasterName. **False.** `Assets/Data/master-dates.json` contains:
`{"names": ["Yuantong Faxiu", "圓通法秀"], "floruit": 1027, "death": 1090, "school": "Yunmen", ...}`.
法秀圓通禪師 of 法雲寺 in 東京 IS 圓通法秀 (Faxiu 1027–1090, Yunmen line — school matches; the
lookup missed the reversed name order 法秀圓通 vs roster 圓通法秀). The 上堂 (看風使帆，正是隨波逐浪…)
is his own sermon under his governing mulu. → Set `MasterName: "圓通法秀"` (roster canonical) and
correct the note; also add him to `RelatedMasters` if that is the convention for occurrence masters.

### D2 — MINOR (inaccurate structural claim in Note, sense 1)
Note: "The 上堂 is carried in six allowlist texts, **all in 緣密's section**." Verified carried in
exactly those six (grep 一句隨波逐浪: X64n1260, X68n1319, X80n1565, X81n1568, X81n1571, X85n1593 — one
hit each, none elsewhere among cited texts) — but in **X64n1260 (列祖提綱錄)** it is NOT in a 緣密
section: it sits in the topical anthology section 五參提綱 (governing mulu `五參提綱(住持○五參提綱僅集禪燈諸錄…)`)
with inline attribution 德山密禪師上堂. Attribution to 緣密 still holds (the text names him), but
"all in 緣密's section" is wrong for this witness. → Reword (e.g. "five in 緣密's section; X64n1260
carries it in the 五參提綱 anthology attributed 德山密禪師").

### D3 — NIT (wrong co-located R-edition lb in note): sense 2, occ 3 (X/X82/X82n1571.xml, 0641a09)
AttributionNote says "co-located ed=R142 0048a06 not used"; the actual co-located ed=R142 lb at the
match is **0048a09**. FromLb/ToLb (ed=X 0641a09) are correct, so impact is zero, but the stated
R-number is wrong. → Fix or drop the R reference.

## VERIFIED CLEAN (everything else)

### 1. KWIC integrity — ALL 7 VERBATIM ✅
All seven Kwics are exact contiguous substrings of MAIN text (not apparatus), unique in-file
(count=1), FromLb=ToLb verified at both match ends:
- S1: X81n1568 0113c19 (函葢乾坤，一句截斷眾流，一句隨波逐浪。作麼生辯？ — 一句 does open on 0113c18 as noted);
  X81n1568 0058a10 (如何是隨波逐浪句？師曰：隨。); X80n1566 0477a16 (師曰：一人隨波逐浪，一人截斷眾流。);
  X81n1568 0140c07 (看風使帆，正是隨波逐浪。)
- S2: X85n1593 0477b14; X84n1583 0617b19; X82n1571 0641a09
Source's 函葢 variant preserved verbatim in the KWIC ✅. X-canon lbs use ed="X" ✅.

### 2. Attribution (apart from D1) ✅
- Occ S1-1 governing mulu 鼎州德山緣密圓明禪師 ✅, under header 雲門偃禪師法嗣 (raw offsets: header
  838026 → section head 840134 → sermon) — Note's 法嗣 claim verified; 德山緣密 on roster ✅;
  three-phrase set correctly NOT attributed to 雲門文偃 himself.
- S1-2 mulu 明州瑞巖智才禪師 — 智才 not on roster → null ✅ (answer is the section master's; note transparent).
- S1-3 mulu 慶元府育王孤雲權禪師 — raised 趙州 case (古磵寒泉…州曰：死 confirmed in context) + off-roster
  commenting master → null ✅ correct handling of a raised case.
- S2-1 mulu 慶善能禪師, S2-2 mulu 河南府陝州熊耳山崧溪子定禪師, S2-3 mulu 瑞安瑞雲介石芳禪師 — none on
  roster → null ✅. S2-3 is autobiographical (明芳 self-reference) as noted ✅.

### 3. Allowlist ✅
All RelPaths (occurrences + 8 SourceTexts sense 1 + 3 SourceTexts sense 2) in zen-corpus.json.
Headword attested in every SourceText (raw grep counts): X81n1568=12, X81n1571=5, X82n1571=20,
X80n1566=4, X85n1593=7, X80n1565=9, X68n1319=6, X64n1260=7, X84n1583=7 ✅.

### 4. Explanation honesty ✅
- 我有三句語示汝諸人：一句函葢乾坤，一句截斷眾流，一句隨波逐浪。作麼生辯？ — verbatim X81n1568 0113c18–19 ✅;
  隨波逐浪句 is listed third ✅.
- Test-question answers all attested in allowlist texts: 隨 (X80n1565/X81n1568/X81n1571),
  闊 (X81n1568; X81n1571 writes 濶), 春生夏長 (X80n1565/X81n1568/X81n1571/X85n1593),
  船子下揚州 (X80n1565/X85n1593/X82n1571) ✅.
- 一人隨波逐浪，一人截斷眾流 — X80n1566 0477a16 ✅; 看風使帆，正是隨波逐浪。截斷眾流，未免依前滲漏 — X81n1568 0140c07 ✅.
- Orthography claim verified precisely: the X85n1593 carrying writes 函蓋乾坤; X68n1319/X80n1565/
  X81n1568/X81n1571 write 函葢乾坤; X64n1260 writes 圅葢乾坤 (covered by the note's "函葢/圅葢") ✅.
- S2 quotes 祇為心塵未脫，情量不除，見色聞聲，隨波逐浪，流轉三界 (X85n1593 0477b14), 終日隨波逐浪、妄生枝節者哉
  (X84n1583 0617b19), 明芳愚懦無知，自少隨波逐浪 (X82n1571 0641a09) — all verbatim ✅.

### 5. Multi-source ✅
Sense 1: six carryings of the 上堂 + test-question witnessed in ≥4 texts (X80n1565, X81n1568,
X81n1571, X85n1593, X82n1571). Sense 2: three independent texts. Both `multi-source` justified.

### 6. Describe-only ✅
Both explanations report deployment (named phrase of a fixed set, stock test-question, pairing
with 截斷眾流, plain idiom spoken of persons) with grep-verified quotes and close with the
no-gloss disclaimer. No intent/force vocabulary, no menu-of-readings.

### 7. Nesting / RelatedTerms ✅
截斷眾流 · 函蓋乾坤 · 三句 genuine (co-occurring in the quoted 上堂 and pairings); the required
隨波逐浪 ↔ 截斷眾流 link is present. Sense split (named 句 vs plain idiom) is well-drawn; the
看風使帆 line is correctly kept under sense 1 as the note explains.

## Punch list summary
1. D1: set MasterName=圓通法秀 on S1-occ4; fix "not on roster" note. (MAJOR)
2. D2: reword "all in 緣密's section" for X64n1260. (MINOR)
3. D3: R142 co-located lb is 0048a09, not 0048a06. (NIT)
4. Observation (schema, non-corpus): both senses carry SenseKey=null; if SenseKey is a merge key,
   two corpus-wide senses may collide — consider distinct keys (e.g. "句"/"idiom") if the store
   requires uniqueness. Not a corpus-fact defect.

Defects: 3 (1 major, 1 minor, 1 nit).
