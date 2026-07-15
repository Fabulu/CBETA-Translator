# WORK — 庭前柏樹子 (t_097f38f58678) · "the cypress tree in the garden"

## Concordance (Zen allowlist only, 462 texts)
- 庭前柏樹子 (full phrase): 317 hits / 130 files. Bare 柏樹子: 583 / 176. Massively multi-source.
- Top files: J38nB406 (天然和尚語錄) 18, T47n1998A (大慧語錄) 18, X82n1571 (五燈全書) 13, J34nB299/B300, 從容錄, etc.

## Sense analysis
ONE sense (corpus-wide). The phrase is uniform across the corpus: it is always *Zhaozhou's answer to 如何是祖師西來意*, later fixed as the koan "趙州柏樹" (從容錄 case 47). No master bends it to a private meaning; later masters raise it as a case. So a single senseKey=null sense with per-occurrence attribution.

## Attribution (checked cb:mulu heads)
- J24nB137 = 趙州祖師語錄 (Zhaozhou's own record); 師 = Zhaozhou → **named Zhaozhou Congshen** for both his utterances (the case at 0358b16; the 佛性 play at 0365b13).
- T48n2004 = 從容錄, mulu shows case 47 "趙州柏樹" → raised case → **null**.
- X66n1296 = 宗門拈古彙集, mulu 蘄州五祖山法演禪師 → 五祖法演 (Wuzu Fayan) raising the case in 示眾 → **null** (per rule: raisings are null; only Zhaozhou's own record carries his name).
- X80n1565 = 五燈會元, mulu 建康府華藏密印安民禪師; 悟 = 圜悟克勤 (Yuanwu) gives the phrase to Anmin → **null**.

## Key findings
- 柏 = cypress/arborvitae, NOT oak. The classic English "the oak tree in the garden" is a mistranslation (flagged in Note).
- The 我不將境示人 rider is the doctrinal heart: the tree is not a symbol pointing at a hidden 西來意; the answer refuses the meaning-behind-the-object move. Grounded verbatim, not imported.
- Zhaozhou's second play (柏樹子還有佛性…待柏樹子成佛) shows the same image resisting both materialism and metaphysics.

## Validation: multi-source (Zhaozhou's record + 從容錄 + 拈古彙集 + 五燈會元, 4 independent texts).
## RelatedTerms: 祖師西來意 (the question it answers — semantic), 柏樹子 (genuine constituent).
## All 5 curated KWICs verbatim-verified (tag-stripped substring); all FromLb exist in-file (ed=X for X-files); all RelPaths in allowlist.

---
## GATE 2 (Claude verify+repair) — 2026-07-11
- 5/5 KWICs re-derived EXACT CONTIGUOUS (tag-stripped substring of the ONE cited file). Zero ellipses/stitches.
- 5/5 RelPaths (J24nB137, T48n2004, X66n1296, X80n1565) in allowlist. Zero contamination.
- 5/5 FromLb = nearest preceding <lb>; X66n1296 (0252b01) + X80n1565 (0408c20) verified ed="X" NOT ed="R". All match.
- Governing cb:mulu heads read: K1 趙州和尚語錄卷上 (Zhaozhou), K2 趙州和尚語錄卷中 (Zhaozhou), K3 從容錄 case 47 趙州柏樹 (raised→null), K4 蘄州五祖山法演禪師 (Wuzu Fayan raising→null), K5 建康府華藏密印安民禪師 (圜悟 gives phrase to Anmin→null).
- **ATTRIBUTION CONFIRMED (flagged call c):** only Zhaozhou's own record (J24nB137, both utterances) carries his name; all three later raisings null. Correct as written — no fix needed.
- Over-read guard: 柏 = cypress not oak (Note correct); tree explicitly NOT a symbol (grounded in 我不將境示人, K1). RelatedTerms 祖師西來意 = genuine semantic link (real). Literal, no imported abstraction.
- STATUS: verified.
## Attribution remediation original-606 (2026-07-13)

- Before: 2/5 occurrences named. After: 6/6 named with exact TEI title and speaker notes.
- Raised-case deployments are attributed to Wansong Xingxiu, Wuzu Fayan, and Yuanwu Keqin rather than left null.
- Added and zc.verify-confirmed Wansong's Huijue-denial passage; the prior sprawling source-name prose was normalized without discarding its useful denial evidence.
- Definition/item-8 retest holds: original answer, named case, later handling, awakening trigger, and attribution denial concern the same cypress case.
- All Chinese evidence is anchored; unresolved ladder and roster cases: none.

## Supporting-evidence repair — 2026-07-13

- Marked the cypress-nature witness as `EvidenceRole: family`; it no longer buys exact-headword depth.
- Added a distinct, exact-headword Dahui Zonggao witness from the Sayings Record of Chan Master Dahui Pujue (大慧普覺禪師語錄), verified at 0863c01–02. Exact depth is now 6 across 5 sources, plus one family witness.
- Definition/item-8 retest holds: Dahui's appraisal that the answer is exceedingly direct is another handling of the same cypress answer/case, not a different lexical thing.

## Semantic remediation r002

- feedback-inference-verdict: REVISE — the old preferred wording inherited “garden,” while the headword locates the tree before a courtyard or hall; the target is now spatially explicit and the legacy oak translation is preserved as a search alias.
- feedback-observations: Zhaozhou gives the cypress answer twice, rejects the object-showing objection, discusses the cypress's buddha-nature, and later speakers raise, appraise, transmit, and even deny attribution of the same case.
- feedback-falsification-searches: Re-tested the full phrase, bare `柏樹子`, cypress-nature questions, the object-showing rider, teaching-seat case raisings, Dahui's directness appraisal, and Huijue's denial across 317 full-phrase hits in 130 texts.
- feedback-counterexamples: The widespread English “oak tree in the garden” is retained for lookup but not adopted as the definition; the source names a cypress or Chinese arborvitae. Huijue's denial also prevents the entry from presenting attribution as uncontested.
- feedback-scope: One physical cypress answer and the named case built from it; later handlings do not create additional lexical referents.
- lookup-probes: `oak tree in the garden`, `cypress tree in the courtyard`, `Zhaozhou's cypress`, `cypress tree in the yard`, and `tree in front of the hall` all retrieve the entry.
- opening-interpretation-verdict: REPAIRED — the first words now place a cypress before the courtyard and identify it as Zhaozhou's answer.
- plain-english-image-verdict: PASS — a specific temple-yard tree stands before the courtyard; the case history follows without turning it into an unstated symbol.
