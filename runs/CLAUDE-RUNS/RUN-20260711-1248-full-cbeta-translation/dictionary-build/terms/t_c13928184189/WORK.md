# WORK — 見性 (t_c13928184189) — part of 見性成佛

## Concordance (Zen allowlist only)
- 見性 → **2054 hits / 302 files**.
- 見性成佛 (the compound) → **602 hits / 206 files**. i.e. ~1/3 of all 見性 occurrences sit inside the
  four-phrase slogan 直指人心，見性成佛.

## Sense analysis
**One sense (corpus-wide, SenseKey=null): "seeing [one's] nature."** 見 (see) + 性 (nature) — the direct
seeing of one's own original/Buddha-nature. Attested in three tightly-bound frames, all the same act:
- **The school slogan** 教外別傳，不立文字，直指人心，見性成佛 — T47n1985 0495a29 (Linji record) and 200+ texts.
- **明心見性 / 識心見性** near-synonyms — Zhaozhou: 識心見性是不錯路 (X67n1299 0041b07; also J35nB342, J39nB461).
- **見性是佛** equation — Prajnatara case, J26nB184 0552b08 ('何者是佛？…見性是佛').

Nuances that are the SAME sense read differently, not new meanings:
- **Graded** — Xuefeng: 聲聞人見性如夜見月，菩薩人見性如晝見日 (J32nB272 0188c03; J28nB211 parallels); Xuefeng
  rejects the grading with blows.
- **Anti-reified** — Guizong: 悟明見性，是錯用心 (J25nB171 0536b15) — even seeing-nature can be clung to.
So: one corpus-wide sense; no master bends the *referent*. No SenseKey sub-sense needed.

## Multi-source verdict
**multi-source** — decisively (302 texts, every lineage, the defining slogan of the school).

## ewk cross-check (guide §6)
ewk's candidate 見性成佛 = "See nature, become Buddha." **Confirmed against the Chinese:** 見=see, 性=nature,
成=become, 佛=Buddha — the plain reading holds and the corpus backs the "SEEING not studying" emphasis
(見性不在言說, C077n1710 0631b24). Adopted the literal rendering; did NOT import "realize the Absolute" etc.

## Deflationary check
Rendered "seeing [one's] nature"; explanation stresses it is *seeing*, not attainment of a metaphysical
essence, and includes the corpus's own self-critical/graded uses. Avoids over-interpretive gloss.

## Honest thin spots
- 2054 hits not censused; corpus-wide sense from representative sample + counts.
- 見性 also appears in non-slogan collocations (見性源, 洞見性, 見性之功) that are ordinary "see + [the]
  nature [of]" compounds, not the technical term — excluded from curation; not separately mapped.
- The 見性是佛 case (J26nB184) is a *quoted* Indian-patriarch dialogue (Prajnatara), not a Chan master's
  own logion — MasterName left null, flagged in AttributionNote. It is cited because Chan masters deploy it
  as their own touchstone.

## GATE 2 (Claude adversarial verify-and-repair) — 2026-07-11
- **KWICs:** all 5 re-derived by targeted per-file search + tag-strip contiguity check → EXACT
  CONTIGUOUS VERBATIM (no ellipsis, no stitching, no altered punctuation). Char-lengths 21–41.
- **Allowlist:** all 5 RelPaths (T47n1985, J26nB184, X67n1299, J32nB272, J25nB171) ∈ zen-corpus.json. No contamination.
- **FromLb:** all 5 confirmed = nearest preceding `<lb n>` (0495a29, 0552b08, 0041b07, 0188c03, 0536b15).
- **Multi-source:** 5 independent allowlist texts (slogan, Prajnatara equation, Zhaozhou, Xuefeng grading, Guizong warning) → `multi-source` stands.
- **Over-read/abstraction:** literal/deflationary ("seeing [one's] nature", not attainment of a metaphysical essence);
  includes the corpus's own graded + anti-reified uses. OK.
- **Nesting (§5b):** RelatedTerms (見性成佛, 明心見性, 直指人心, 教外別傳, 佛性) — 見性 is a genuine constituent of the
  first two, the rest are deliberate slogan/semantic cross-refs. No coincidental prefixes. OK.
- **Verdict: VERIFIED.** No corrections required; entry.v2.json unchanged.

## GATE 3 FIX (Claude, Ms. Frizzle) — 2026-07-11 17:42 +02:00
- Gate 3 (Fable) verdict: REVISE — two attribution errors in prose.
- **Fix (a) — 波羅提 ≠ Prajnatara / not a patriarch.** Verified `J/J26/J26nB184.xml` lb 0552b08 (line 1170):
  「舉：『南天竺國異見王，問波羅提尊者曰：「何者是佛？」提曰：「見性是佛。」』」 — speaker is 波羅提尊者
  (the venerable Boluoti who converts the heretic-king 異見王 in the Bodhidharma cycle), NOT Prajnatara
  (般若多羅) and not a patriarch. Explanation reworded to "the venerable Boluoti (波羅提)"; occ1 AttributionNote
  corrected from "Prajnatara (波羅提)" to "the venerable Boluoti (波羅提尊者)…(not Prajnatara/般若多羅, not a patriarch)".
- **Fix (b) — moon/sun grading is the monk's, rejected by Xuefeng.** Verified `J/J32/J32nB272.xml` lb 0188c03
  (line 885): 「舉：『僧問雪峰：「聲聞人見性如夜見月，菩薩人見性如晝見日。未審和尚見性如何？」峰打三下。』」 —
  the grading is proposed by the questioning monk (僧問); 峰打三下 = Xuefeng REJECTS it with three blows.
  Explanation reworded so the grading is the monk's position that Xuefeng rejects; Note phrase "Xuefeng's grading"
  → "Xuefeng's rejection of the monk's moon/sun grading". (occ3 AttributionNote was already correct — unchanged.)
- **All 5 KWICs unchanged** (verbatim-contiguous). No occurrences added/removed. Validation stays `multi-source`.
- **STATUS = verified.**
## Public-feedback inference ledger

- feedback-inference-verdict: `accepted-with-limits` — the corpus directly supplies the act/claim and several explicit definitions, but it also preserves named objections and non-defining answers. The entry reports all of them without selecting a doctrinal winner.
- feedback-observations: school formula, `seeing nature is Buddha`, `road that does not go wrong`, three blows, `misuse of mind`, and three explicit definition frames are independently anchored and speaker-owned.
- feedback-falsification-searches: searched exact headword, `名為見性`, `見性者`, `所謂見性`, `如何是見性`, and `見性人`; compared defining equations against blows, rebukes, and non-defining responses; tested for distinct referents rather than differing readings.
- feedback-counterexamples: Xuefeng Yicun answers the graded question with blows, and Guizong Zhichang calls awakening and seeing nature a misuse of mind. These prevent a one-sided explanatory definition but do not create a second referent.
- feedback-scope: corpus-wide single sense; all definitions and objections remain explicitly owned by their speakers.
- lookup-probes: `see one's nature`, `seeing one's nature`, `see one's true nature`, `see your nature`, `nature-seeing`, `see nature become Buddha`, `realize one's nature`.
- opening-interpretation-verdict: `pass` — the opening identifies a Chan claim/question/test, then immediately names the mutually constraining corpus witnesses.

## Sense and depth audit

- sense-split verdict: one referent. Formula, definition, question, answer, and criticism are different deployments or readings of seeing one's nature, not different things.
- depth: 8 exact anchors from 8 independent texts against 2,148 hits in 304 texts; direct definitions added from Yunqi Zhuhong and Chaozong Tongren after the earlier six-anchor draft failed the frequency floor.

## Exact-turn attribution correction (2026-07-13)

- Removed the Xuefeng case: every stored `見性` there is uttered by the unnamed questioning monk; Xuefeng only answers with blows.
- Replacement: Yuanwu Keqin's own `更說什麼直指人心。更覓什麼見性成佛。` (T47n1997 0735b02–03). Eight independent exact anchors remain.
