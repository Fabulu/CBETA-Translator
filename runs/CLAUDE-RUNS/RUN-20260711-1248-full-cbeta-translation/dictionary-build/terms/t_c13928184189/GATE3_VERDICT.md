# Gate 3 Verdict — 見性 (t_c13928184189) — RE-VERIFY after repair

VERDICT: PASS

Independent adversarial re-derivation (Gate 3, fresh pass, 2026-07-11). This entry was
previously REVISE'd for two prose attribution errors: (1) 波羅提 mislabeled as
"Prajnatara" and "patriarch"; (2) the moon/sun grading of 見性 presented as Xuefeng's own.
Both repairs are verified correct against the primary Chinese; all other checks re-run and
hold. Merge as-is.

## Prior-fix verification (the reason for this re-verify)

**Fix (a) — 波羅提 = Boluoti, NOT Prajnatara — FIXED CORRECTLY.**
Re-derived from `J/J26/J26nB184.xml`, nearest lb = 0552b08 (offset-mapped): the passage
reads 「舉：『南天竺國**異見王**，問**波羅提尊者**曰：「何者是佛？」提曰：「見性是佛。」…』」 —
the speaker is 波羅提尊者 answering the heretic-king 異見王; 般若多羅 (Prajnatara, the 27th
patriarch) is a different figure and 波羅提 is not a patriarch. The Explanation now says
"the venerable Boluoti (波羅提) says flatly 見性是佛" and occ1's AttributionNote reads
"Quoted case of the venerable Boluoti (波羅提尊者)… not Prajnatara/般若多羅 and not a
patriarch" — exactly the demanded fix; no trace of the old wording remains.

**Fix (b) — the grading is the MONK's; Xuefeng REJECTS it with three blows — FIXED CORRECTLY.**
Re-derived from `J/J32/J32nB272.xml`, nearest lb = 0188c03: 「舉：『**僧問**雪峰：「聲聞人見性
如夜見月，菩薩人見性如晝見日。未審和尚見性如何？」**峰打三下**。後問巖頭，頭打三掌。…』」 — the
grading sits inside the monk's question (僧問); Xuefeng's answer is three blows (and Yantou's
three slaps). The Explanation now reads "Masters resist grading it — a monk brings Xuefeng
the canonical grading… and Xuefeng answers with three blows, rejecting the tiered scheme,"
and the Note says "Xuefeng's rejection of the monk's moon/sun grading." Correct per source.

## Per-sense findings

### Sense 0 — "seeing [one's] nature" (multi-source)

1. **KWIC exact + contiguous: PASS (5/5).** Every KWIC re-verified as an EXACT CONTIGUOUS
   substring of its cited file after XML-tag/whitespace stripping (script check, found x1
   each; no ellipsis, no stitching, no altered punctuation):
   - `T/T47/T47n1985.xml` — 佛法有教外別傳、不立文字、直指人心、見性成佛。 (23 chars). Nearest lb = 0495a29 ✓
   - `J/J26/J26nB184.xml` — 『何者是佛？』提曰：『見性是佛。』王曰：『師見性否？』提曰：『我見佛性。』 (37 chars). Nearest lb = 0552b08 ✓
   - `X/X67/X67n1299.xml` — 問：如何是不錯路？師云：識心見性是不錯路。 (21 chars). Nearest lb = 0041b07 ✓
   - `J/J32/J32nB272.xml` — 僧問雪峰：『聲聞人見性如夜見月，…』峰打三下。 (41 chars). Nearest lb = 0188c03 ✓
   - `J/J25/J25nB171.xml` — 歸宗和尚道：『參學人，切忌錯用心。悟明見性，是錯用心；成佛作祖，是錯用心； (37 chars). Nearest lb = 0536b15 ✓

2. **RelPath real + Zen: PASS.** All 5 files exist under `xml-p5` and all 5 RelPaths are
   present in `Assets/Data/zen-corpus.json` (grep-verified). No contamination.

3. **Multi-source: PASS.** Five separate texts, distinct speakers/frames: the four-phrase
   slogan (T47n1985); the 異見王/波羅提 case (J26nB184); Zhaozhou — speaker re-confirmed from
   the file itself, the section header 「趙州。僧問：如何是不錯路？師云：識心見性是不錯路。」
   follows the 趙州諗禪師 material at 0041b07 (X67n1299); the Xuefeng case (J32nB272); the
   Guizong warning quoted with 示眾，舉：「歸宗和尚道… (J25nB171). Raw counts (2054/302; 206
   slogan texts) not re-derived; non-load-bearing.

4. **Over-read: none remaining.** Both prior over-reads corrected (above). The entry now
   states the corpus's own self-critical uses (Xuefeng's rejection, Guizong's 錯用心)
   accurately, which guards against reification rather than committing it.

5. **Imported abstraction: none.** "seeing [one's] nature" is deflationary and literal;
   "(Buddha-)nature" is licensed by the witness itself (提曰：『我見**佛性**。』); the plain
   reading of 見性成佛 is adopted rather than a metaphysical-essence gloss.

6. **Attribution honesty: PASS.** Boluoti correctly named and de-patriarch'd; the grading
   correctly assigned to the monk; MasterName null on the slogan and the quoted Indian
   case; Zhaozhou/Xuefeng/Guizong attributions match their sources.

## Issues (tagged)

- None blocking. Carried observations (no action forced):
  - occ0's slogan sits in prefatory material of the Linji-record file (Bodhidharma-arrival
    frame, 「逮二十八祖菩提達磨…是時中國始知佛法有教外別傳…」), not Linji's own sermon; the
    note claims only "here in the Linji record," which is literally true, MasterName null.
  - occ3's AttributionNote opens elliptically ("Grades seeing-nature (moon-at-night vs
    sun-by-day); Xuefeng rejects…") — the implied subject is the monk, and the second
    clause plus the corrected Explanation/Note make the reading unambiguous. Cosmetic only.
  - occ4 MasterName "Guizong Zhichang" from bare 歸宗和尚道 — conventional identification;
    acceptable.

## Verified occurrences: 5/5 KWIC confirmed verbatim
