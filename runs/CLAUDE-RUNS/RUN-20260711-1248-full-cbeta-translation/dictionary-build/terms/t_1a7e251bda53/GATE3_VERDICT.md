# Gate 3 Verdict — 示眾 (t_1a7e251bda53)

VERDICT: PASS

Independent adversarial re-derivation (fresh model, Claude Opus 4.8 acting as Gate 3). All checks
re-derived from the raw TEI in `C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5`; WORK.md was not
trusted as evidence.

## Per-occurrence findings

1. **T/T47/T47n1997.xml @0721a02** — `陞座示眾云。鉤頭有餌。句裏無私。`
   EXACT contiguous tag-stripped substring, 1 hit, nearest `<lb ed="T">` = 0721a02. Context:
   `…師指法座云。毘耶借座燈王…陞座示眾云。鉤頭有餌…` — the 師 of 圓悟佛果禪師語錄 (docNumber
   No. 1997 confirmed in header; TOC reads 圓悟佛果禪師語錄目錄) ascending the seat. Attribution
   圜悟克勤 correct. PASS.
2. **X/X64/X64n1260.xml @0002b20–21** — `上堂示眾，不立別名者，俱借五參一事統攝。`
   EXACT, 1 hit, nearest `<lb ed="X">` = 0002b20 (co-located R112 lb correctly ignored per spec).
   Context: `提綱篇首，如先德單目，上堂示眾，不立別名者，俱借五參一事統攝。` — genuinely the
   compiler's editorial matter of 列祖提綱錄 (byline 沙門戒顯撰 confirmed). MasterName null correct.
   The Explanation's claim that the editor subsumes un-named 上堂/示眾 records under the 五參
   convocation is exactly what the Chinese says. PASS.
3. **X/X64/X64n1260.xml @0015b09–10** — `黃檗和尚示眾云：汝等諸人盡是不著便底`
   EXACT; the quote occurs 2× in the file (0015b09 and 0171a11); the cited lb matches the first,
   ed="X". The line itself names 黃檗和尚 (`復舉：黃檗和尚示眾云：…還知大唐國裏無禪師麼`), so
   MasterName 黃檗希運 with the "cited (復舉)" AttributionNote is honest. PASS.
4. **X/X79/X79n1557.xml @0014a06–07** — `世尊在靈山會上，拈花示眾，眾皆默然`
   EXACT, 1 hit, nearest `<lb ed="X">` = 0014a06 (R136 lb ignored). Context continues `唯迦葉破顏
   微笑。世尊云。吾有正法眼藏…` — the flower-holding story in 聯燈會要 (docNumber No. 1557).
   MasterName 世尊, used as the literal-display exemplar. PASS.

## Checks

- **Allowlist:** T47n1997, X64n1260, X79n1557 + SourceTexts X82n1571, T51n2077 ALL present in
  `Assets/Data/zen-corpus.json`. No contamination.
- **Multi-source:** Yuanwu yulu (T47n1997) + 列祖提綱錄 (editorial + cited Huangbo, X64n1260) +
  聯燈會要 (X79n1557) = ≥3 independent Zen texts. SourceTexts spot-check: 示眾 appears 1034× in
  X82n1571 and 138× in T51n2077. `multi-source` justified.
- **Over-read:** none. Single corpus-wide genre-marker sense; no uniqueness claim made.
- **Imported abstraction:** none. "Instruction to the assembly" is deflationary and literal; the
  physical-display root (拈花示眾, 舉拂示眾) is grounded — 舉拂示眾 independently confirmed present in
  allowlist texts incl. T47n1997 itself.
- **Attribution honesty:** all four attributions verified against chapter/section context (see above);
  the editorial-preface and cited-Huangbo cases are explicitly flagged in AttributionNotes.

## Issues (tagged)

- None.

## Verified occurrences: 4/4 KWIC confirmed verbatim
