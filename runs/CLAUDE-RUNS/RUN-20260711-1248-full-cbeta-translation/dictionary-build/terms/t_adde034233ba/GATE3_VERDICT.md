# Gate 3 Verdict — 即心是佛 (t_adde034233ba) — FINAL RE-VERIFICATION (round 2, fresh verifier)

VERDICT: PASS

Verifier: Gate 3 round 2 (Fable, fresh instance), 2026-07-11. Supersedes the round-1 REVISE
verdict previously in this file. All evidence re-derived directly from
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` (XML tags + whitespace stripped; second
pass with `<note>` apparatus removed — every KWIC matches in BOTH passes, i.e. main reading
flow, not notes-only). Allowlist = `Assets/Data/zen-corpus.json`. WORK.md used as context
only; nothing trusted from it.

## Verified occurrences: 9/9

Every curated KWIC is an EXACT CONTIGUOUS verbatim substring of its cited file; zero
ellipses; every FromLb anchor independently re-checked (KWIC text found adjacent to the
cited `<lb n>` in the raw XML):

| RelPath | lb | KWIC | anchor |
|---|---|---|---|
| T/T48/T48n2005.xml | 0296c28 | verbatim | OK |
| B/B25/B25n0144.xml | 0364b10 | verbatim | OK |
| J/J10/J10nA158.xml | 0048a17 | verbatim | OK |
| J/J27/J27nB198.xml | 0464c10 | verbatim | OK |
| T/T48/T48n2012A.xml | 0380a14 | verbatim | OK |
| J/J34/J34nB306.xml | 0427a10 | verbatim | OK |
| J/J38/J38nB408.xml | 0274c21 | verbatim | OK |
| J/J28/J28nB211.xml | 0456a27 | verbatim | OK |
| J/J23/J23nB134.xml | 0545c04 | verbatim | OK |

## Allowlist / contamination
All occurrence RelPaths and all SourceTexts (including sense 2's occurrence-less
C/C077/C077n1710.xml) are in zen-corpus.json — zero contamination. C077n1710's inclusion
is substantive, not decorative: grep shows 即心即佛 x17, 非心非佛 x12, 為止小兒啼 x2 in
that file.

## The round-1 REVISE fix — CONFIRMED CORRECT (both halves)

1. **J10nA158 exchange now correctly framed as the monk's observation TO Huizhong.**
   Source (tag-stripped, at the 0048a17 anchor):
   `舉：「忠國師因僧問：『如今和尚亦言即心是佛，諸方尊宿亦言即心是佛，那得有異？和尚豈合自是非他？』忠云：『夫法有名異體同、或名同體異，因茲濫矣。…』`
   The 諸方尊宿亦言即心是佛 line is spoken BY the questioning monk; Huizhong (忠國師)
   REPLIES with 名異體同. The Explanation's current wording — "a monk observed to Nanyang
   Huizhong that 諸方尊宿亦言即心是佛 …, to which Huizhong replied that the name differs
   but the substance is one (名異體同)" — matches the source's speaker structure. The
   round-1 misattribution ("Huizhong notes …") is gone.

2. **Sikong Benjing attribution of the B25n0144 line stands.** Re-derived: chapter head
   `司空山本淨和尚嗣六祖` anchored at lb 0364b02; `敕令中使楊光庭往司空山` follows
   (~0364b05); the KWIC (`中使設禮再請，師曰：「為當求佛，為復問道？若求作佛，即心是佛；
   若欲問道，無心是道。」中使不會`) sits at 0364b10 inside the Benjing chapter with no
   intervening chapter/speaker change — the 師 is Sikong Benjing. MasterName, the rewritten
   AttributionNote (楊光庭 / Zutang ji / chapter head, no 國師 title), the Explanation's
   Benjing credit, and the RelatedMasters addition are all correct. Huizhong is retained
   only for the genuine J10nA158 case.

## Remaining checks (all senses)
- **Multi-source:** Sense 1 = Mazu (T48n2005) + Benjing (B25n0144) + Huizhong case
  (J10nA158) + Damei (J27nB198) + Huangbo (T48n2012A): 5 texts, 3 canons, multiple masters.
  Sense 2 = Mazu (J34nB306) + Damei (J38nB408) + Nanquan (J28nB211) + stock (J23nB134) +
  C077n1710. Both `multi-source` labels hold.
- **No over-read:** sense 2 assigns the triad's third member to Nanquan, keeps the
  J23nB134 認奴作郎 line Curated:false / MasterName null as a floating stock warning —
  attribution honesty per guide §5/§6.
- **No imported abstraction:** renderings are literal/deflationary ("this very mind is
  Buddha"); the "redirection, not metaphysical identity-claim" reading is grounded in the
  quoted 向外求佛 within the Huangbo occurrence itself. The Huangbo gloss 心外更無別佛 is
  verbatim in T48n2012A (`汝但除却凡情聖境。心外更無別佛。`) — the Gate-2 misquote fix holds.
- **AttributionNotes:** T48n2005 correctly identified as the 無門關 (case 30, 即心即佛) —
  the earlier Blue-Cliff mislabel remains fixed. J24nB138 0381a10 parallel for the stock
  line spot-checked in round 1 and unchanged.
- **RelatedTerms:** 即心即佛 / 非心非佛 / 無心是道 / 見性成佛 are deliberate semantic
  links, not coincidental-prefix auto-relations (guide §5b).

## Issues (tagged)
- INFO (non-blocking): the clause "to which Huizhong replied that the name differs but the
  substance is one (名異體同)" compresses Huizhong's fuller reply, which distinguishes
  名異體同 (菩提/涅槃/真如/佛性) from 名同體異 (真心/妄心) and faults 諸方 for
  錯將妄心便謂真心. The quoted phrase is verbatim from the reply, the speaker structure is
  now right, and the load-bearing claim (the phrase's shared currency) rests on the monk's
  exact line — so this does not block. Optional polish for a later pass: "replied by
  distinguishing name and substance (名異體同 / 名同體異)".
- INFO (carried over, non-blocking): sense-2 occurrences attest the variant 即心即佛 rather
  than the literal SourceTerm — disclosed in the sense Note; corpus itself equates the
  forms (T48n2005 case title 即心即佛 over body text 即心是佛).

## Verified occurrences: 9/9
