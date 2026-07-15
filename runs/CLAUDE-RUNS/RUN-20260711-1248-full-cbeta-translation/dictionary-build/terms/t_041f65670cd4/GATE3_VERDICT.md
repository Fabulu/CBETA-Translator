# Gate 3 Verdict — 無心 (t_041f65670cd4) — POST-REPAIR RE-VERIFICATION

VERDICT: PASS

Independent adversarial re-verification after the prior REVISE (debate-conflation fix). Fresh
Gate 3 pass; ALL evidence re-derived from the raw TEI at
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` via tag+whitespace-stripped exact-substring
matching with lb anchoring. WORK.md used as context only. This verdict supersedes the prior
REVISE verdict (2026-07-11); the one flagged issue is confirmed FIXED.

## The prior REVISE issue — confirmed FIXED

The old Explanation conflated two distinct T51n2076 exchanges. The repaired Explanation now
separates them, and BOTH pairings re-derive exactly from the source (full passage 0242b19–0243a10
extracted and read):

1. **Exchange 1 (遠禪師, at court):** intro at 0242c05–06 `召兩街名僧碩學。赴內道場與師闡揚佛理。時
   有遠禪師者。抗聲謂師曰。今對聖上校量宗旨…` — so "open court debate before the emperor" (今對聖上)
   and the title 遠**禪師** are both verbatim source (note: the source says 禪師, not 法師 — the
   repaired entry is here MORE accurate than the prior verdict's own suggested wording; the text
   later also calls him 遠公, 0242c23). His charge @0242c12–13: `適言無心是道。今又言身心本來是道。
   豈不相違` = exactly the entry's "that 無心是道 and 身心本來是道 conflict." Benjing's answer
   @0242c14: `師曰。無心是道心泯道無。心道一如故言無心是道` — the entry's 無心是道，心泯道無，心道一如
   is correctly paired with THIS exchange. VERIFIED.
2. **Exchange 2 (志明禪師, tiles and pebbles):** @0242c29–0243a01 `又有志明禪師者。問曰。若言無心是道。
   瓦礫無心亦應是道` — the entry's challenger name (志明禪師), sequencing ("a later challenger"), and
   quote are exact. Benjing's reply @0243a05–07: `六根尚無見聞覺知憑何而立。窮本不有何處存心。焉得不同
   草木瓦礫。志明杜口而退` — the entry now correctly says he turns the objection "not with that formula
   but by accepting the sameness at the level of emptiness (窮本不有…焉得不同草木瓦礫), whereupon
   Zhiming falls silent." Correct pairing AND correct polarity (acceptance, not rebuttal). VERIFIED.

## Per-occurrence findings (sense 1 of 1)

1. **T/T48/T48n2012A.xml @0380a17** — `無心者無一切心也。` EXACT contiguous, 1 hit, nearest
   `<lb n="0380a17" ed="T"/>`. Context: `…不如供養一個無心道人。何故。無心者無一切心也。如如之體…`
   (傳心法要, Huangbo's discourse). PASS.
2. **T/T48/T48n2012A.xml @0380b02–04** — `但能無心。便是究竟。學道人若不直下無心。累劫修行終不成道。`
   EXACT, 1 hit, nearest lb 0380b02. PASS.
3. **T/T48/T48n2012A.xml @0380b12–13** — `此心即法。法外無心。心自無心。亦無無心者。` EXACT, 1 hit,
   nearest lb 0380b12; source continues `將心無心。心却成有。` — the Explanation's 將心無心，心却成有
   quote verified in place (却 matches source). PASS.
4. **T/T48/T48n2012B.xml @0384b01** — `即心是佛。無心是道。` EXACT, 1 hit, nearest lb 0384b01.
   Context: `問如何是佛。師云。即心是佛。無心是道。但無生心動念有無長短彼我能所等心。…` — confirms the
   AttributionNote ("in answer to 如何是佛") and the Explanation's 但無生心動念… quote (宛陵錄). PASS.
5. **T/T51/T51n2076.xml @0242b27–28** — `若欲求佛即心是佛。若欲會道無心是道。` EXACT, 1 hit, nearest
   `<lb n="0242b27" ed="T"/>`. Frame: `唐天寶三年玄宗遣中使楊光庭入山…師曰。若欲求佛即心是佛…`
   (天寶三年 = 744 CE ✓, envoy 中使楊光庭 ✓). Section head `<cb:mulu level="5">司空山本淨禪師` @0242b19
   immediately precedes. Lineage re-verified: fascicle-5 TOC lists 司空山本淨禪師 under
   `第三十三祖慧能大師法嗣四十三人` (@0235a23), and the biography opens 幼歲披緇于曹谿之室受記 — a
   Sixth-Patriarch disciple as claimed. PASS.

## Checks

- **KWIC exact + contiguous:** 5/5 verbatim, no ellipsis, no stitching, all at the cited lb.
- **Allowlist:** T48n2012A, T48n2012B, T51n2076 + SourceTexts T48n2016, X68n1319 — ALL in
  `zen-corpus.json`. No contamination.
- **Multi-source:** HOLDS. Huangbo (two independent texts) + Benjing/景德傳燈錄 (independent
  Huineng-lineage witness). The "33 Zen texts" claim re-derived independently: rg fixed-string
  無心是道 over the corpus = 71 files, of which EXACTLY 33 on the allowlist (B14n0082, B25n0144,
  C077n1710, C078n1720, D48n8939, J28nB208, J34nB299, J36nB358, J36nB359, J37nB391, T47n1989,
  T47n1998A, T48n2012B, T48n2016, T51n2076, X68n1318, X69n1364, X70n1382, X70n1400, X70n1401,
  X71n1412, X71n1420, X79n1557, X79n1559, X80n1565, X80n1568, X81n1571, X82n1571, X83n1574,
  X84n1583, X84n1585, X85n1593, X86n1607).
- **Over-read:** NONE remaining — the previously flagged debate conflation is fixed (see above);
  no uniqueness claim; the decision to keep one corpus-wide sense with Huangbo as locus classicus
  is consistent with the 33-text spread.
- **Imported abstraction:** none — "no-mind" is literal and deflationary; the self-cancelling
  reading (亦無無心者) is Huangbo's own Chinese; the Note fences off the plain-language
  "unintentionally" sense and refuses the mystical-Absolute reading.
- **Attribution honesty:** all five attributions re-confirmed against titles/section heads
  (2012A 傳心法要 / 2012B 宛陵錄 = Huangbo; T51n2076 mulu = 司空山本淨). Nothing floating or
  laundered.

## Issues (tagged)

None. The single prior issue (OVERREAD: debate conflation) is verified fixed against the primary
source, with correct challenger names, correct quote pairings, and correct polarity.

## Verified occurrences: 5/5 KWIC confirmed verbatim
