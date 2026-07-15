# Gate 3 Verdict — 平常心 (t_4ccf8aed47d3)

VERDICT: PASS

Independent adversarial re-derivation from `C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5`,
allowlist `Assets/Data/zen-corpus.json` (462 relpaths). Method: full tag+whitespace stripping of each
cited file, exact contiguous substring check, lb recovery at match position, chapter-context reads.

## Per-sense findings

### Sense 1 (generic "ordinary mind") — PASS
1. **KWIC exact:** X/X80/X80n1568.xml — `問：如何是平常心？師曰：要眠即眠，要坐即坐。曰：學人不會，意旨如何？師曰：熱即取涼，寒即向火。`
   EXACT MATCH ×1 at lb 0215b15..0215b17 (R139 numbering; ed="X" lb 0648c01). Text title verified:
   五燈嚴統(第1卷-第9卷) — matches the AttributionNote.
2. **Allowlist:** X80n1568, B25n0145, X64n1260, T51n2076 all in zen-corpus.json.
3. **Multi-source — INDEPENDENTLY CONFIRMED:** I re-derived witnesses beyond the curated one:
   - T51n2076 (景德傳燈錄) 0275a22: `僧問。如何是平常心。師云。要眠即眠要坐即坐。…熱即取涼寒即[向火]` — the same
     exchange in an earlier independent compilation (X80n1568 is a recompilation of it).
   - X64n1260 0623a06: `如何是平常心？師云：敲氷取火，掘地覓天` — a genuinely DIFFERENT exchange, independent witness.
   - B25n0145: 平常心 ×28 (e.g. 0731a10 `麻三斤栢樹子須彌山平常心是道雲門顧趙州無`).
   ≥2 independent attestations → `multi-source` holds.
4. **Explanation quotes all real:** 要眠即眠/熱即取涼 verified above; `饑時喫飯困來眠` — verified verbatim in
   allowlisted J/J36/J36nB357.xml 0603b14, and in the very verse that ties it to the term:
   `了取平常心是道，饑時喫飯困來眠`. No fabricated Chinese.
5. **No imported abstraction:** the gloss is deflationary ("plain daily activity"), grounded in the quoted lines.

### Sense 2 (Mazu/Nanquan — 平常心是道) — PASS
1. **KWIC exact (3/3):**
   - X/X78/X78n1553.xml `道不用修，但莫污染。…平常心是道。何謂平常心？無造作、無是非、無取捨，無斷常、無凡聖。`
     EXACT ×1 at lb 0653b14..16 (R135). **Attribution verified hard:** immediately preceding text is
     `百丈問：如何是佛旨趣？師云：正是汝放身命處。師示眾云：` — exactly as the AttributionNote claims — and the
     chapter head at lb 0651b13 reads `時號江西馬祖焉`, so 師 = Mazu. Title verified: 天聖廣燈錄.
   - X/X68/X68n1318.xml `趙州問南泉：如何是道？泉云：平常心是道。…擬趣即乖。…知是妄覺，不知是無記。` EXACT ×1 at
     0087b10..12 (R119). Title verified: 續古尊宿語要 — matches the note.
   - J/J36/J36nB367.xml `趙州問南泉：「如何是道？」…「擬即乖。」…` EXACT ×1 at 0866a22..23. Title verified:
     寂光豁禪師語錄 — an independent later recension, honestly marked Curated=false.
2. **Allowlist:** all six SourceTexts (X78n1553, X68n1318, J36nB367, J23nB134, X72n1439, X79n1559) allowlisted.
3. **Multi-source:** the two Nanquan-dialogue witnesses are recensions of ONE passage, but Mazu's 示眾
   definition (X78n1553) is an independent attestation of the sense → ≥2 independent witnesses. Holds.
4. **Note claims verified against source:** X79n1559 0345a15–16 reads
   `或又報箇平常心是道，以為極則，天是天、地是地、山是山、水是水、僧是僧、俗是俗…` — the Note's warning about cheap
   over-reading quotes the file accurately. X69n1362 (cf.-citation) is allowlisted.
5. **No over-read / no imported abstraction:** the entry keeps Mazu's qualifier (無造作…無凡聖) and explicitly
   guards against the complacency reading using the corpus's own internal critique. Exemplary honesty.

## Issues (tagged)
- INFO (non-blocking): the Note's index stat "290 hits across 116 allowlist texts" — my tag-stripped
  body recount over the 462 allowlisted files gives 310 hits / 120 texts (method difference: I count
  <note>/apparatus content inside <body>). Same magnitude; not an overclaim.
- INFO (systemic, not this entry's fault): for X-canon files the FromLb values follow the R-edition lb
  (e.g. 0215b15 = ed="R139"), not the ed="X" lb (0648c01). Consistent across all entries I verified —
  the pipeline's lb convention — but flagging so link-resolution (ZenUriParser) is checked against it once.

## Verified occurrences: 4/4 KWIC confirmed verbatim (plus 2 Note-quotes and 1 explanation-quote re-anchored)
