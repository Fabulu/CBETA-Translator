# WORK — 向上 (t_0ad9fc2dfdda)

**Rendering:** Senses[0] "upward (the further-up)" — the fixed Chan register; Senses[1] "up / upward (spatial)" — ordinary. Both SenseKey null (polysemy pattern, primary at [0]).

## Method
Zen-scoped concordance (462-text allowlist). KWICs physical-line-bounded, greppable; verified by `verify_all.py`: PASS.

## Counts (allowlist)
- 向上 ~7068× / 394 files. Technical compound family dominates:
  - 向上事 1365× · 向上一路 958× (own entry) · 向上宗乘 462× · 向上關(捩子/棙子) 324× / 59× · 向上一著(子) 279× / 26× · 向上一竅 270× · 向上機 259× · 向上人 229× · 向上巴鼻 63× · 向上鉗鎚 43×
  - Fixed lines: 向上一路千聖不傳 40× / 22 files · 有向上事也無 96× · 更有向上事 25×
  - Contrast: 向下 729×
- Ordinary spatial minority: 向上看 19× · 眉毛向上 8× · 頭向上 11× · 向上是 6×

## Sense decision
Genuinely polysemous, both corpus-wide → two null senses (accepted pattern, primary/technical at Senses[0]). The technical sense is a set register realized in compounds and opposed to 向下; described by attested deployment (monks quote 向上一路千聖不傳 and ask 如何是向上不傳底事; test-question 還有向上事也無 answered 無; 如何是向上宗乘 asked/answered). No "transcendence" gloss imported — the graph says only "upward." Ordinary sense kept separate so the technical reading is not forced onto every 向上 (needed for the hover/highlight distinction the task asks for).

## Attribution
Stock quotations / test-questions across many masters → MasterName null throughout.

## Validation
Both senses multi-source. Technical: 傳燈玉英集 B14n0082, 祖堂集 B25n0144, 古尊宿語錄 C077n1710 + 五燈會元 X80n1565, 大慧語錄-era yulu (向上一路千聖不傳 in 22 texts). Ordinary: J26nB184, B14n0082 (attested in 19 texts via 向上看).

## Cross-links
Senses[0] RelatedTerms: 向上一路 (own entry) · 向下 · 向上事 · 向上宗乘 · 向上關. Senses[1]: 向下.

## Gloss-hygiene hard gate

- sense-target-distinguishability: sense 0 `technical register: the further-up` names the set Chan lexical register attested in 向上一路, 向上事, and 向上宗乘; sense 1 `spatial direction: upward` names literal direction in looking up. The targets identify different uses without relying on their notes, so the split stands.
- Depth enrichment added one independent 向上一竅 question and one unambiguously spatial “raise the eyebrows and look upward” witness. The new evidence preserves, rather than blurs, the technical-register/spatial-direction split.
## Attribution remediation original-606 (2026-07-13)

- Before: 0/8 occurrences named. After: 8/8 named with exact TEI title and speaker notes across both senses.
- Six-rung resolution identified the deploying speakers in lamp, sayings, and verse sources; no nulls remain.
- Non-roster speakers retained in pinyin: Renwang Jun, Qinglin Shiqian, Jingguang Weijue, Muyun Tongmen, and Baozhi.
- Definition/item-8 retest confirms the existing split: the further-up technical register and ordinary spatial upward direction are different things.
- All Chinese evidence is anchored; unresolved ladder cases: none.

## Updated supporting-evidence gate — 2026-07-13

- Rechecked all eight exact-headword witnesses with the exact-KWIC-aware attribution, quote-anchor, and depth gates; all pass.
- Definition/item-8 retest preserves the two-way split: the Chan further-up register and ordinary spatial upward direction denote different things.

## semantic-r001 public-feedback remediation (2026-07-14)

- feedback-inference-verdict: KEEP the technical-register versus spatial-direction split. The further-up register heads named matters, roads, vehicles, barriers, moves, apertures, pivots, people, handles, and implements; spatial use modifies looking, eyebrows, head position, and above/below location.
- feedback-observations: Technical witnesses quote the one road upward that the thousand sages do not transmit, ask about the untransmitted upward matter, ask whether an upward matter still exists, and question the upward vehicle or aperture. Spatial witnesses pair looking up with looking down and attach direction to eyebrows or head.
- feedback-falsification-searches: Rechecked the bare word, upward matter, one road upward, upward vehicle, barrier and latch, move, aperture, pivot, person, handle, pincers and hammer, downward contrast, look upward, eyebrows upward, head upward, and above-is frames. Object-family and body-direction predicates preserve two uses.
- feedback-counterexamples: Clear locational looking and body-position witnesses prevent forcing the technical register onto every occurrence. The many technical compounds and direct questions prevent reducing the entire entry to ordinary vertical direction. The record supplies no single imported gloss such as transcendence.
- feedback-scope: Both senses are corpus-wide. The technical register dominates but remains a lexical family rather than a person-owned doctrine; the spatial minority is independently multi-source.
- lookup-probes: Technical probes covered “further up,” “upward matter,” “one road upward,” “upward vehicle,” and “upward barrier.” Spatial probes covered “upward,” “above,” “look up,” “facing upward,” and “head upward.”
- opening-interpretation-verdict: REVISE in principle from graph-first justification to corpus-earned contrast: the primary is identified by its repeated compound and question family, while the secondary is identified by body and gaze direction. The stored explanations now make these distinct uses explicit without importing a metaphysical target.
- definition-formula-audit: Direct questions about upward matter, vehicle, aperture, and untransmitted road anchor the technical register; body and gaze syntax plus above/below pairing anchor spatial direction. No self-definition collapses the technical family to one explanation.
- nested-family-audit: One road upward, downward, upward matter, vehicle, barrier, aperture, pivot, person, handle, and pincers-and-hammer were rechecked. These are compounds deploying the register, not extra bare-word senses.
- modifier-and-provenance-audit: No feedback modifier is at issue. All eight anchors were re-read and retain exact source-and-speaker attribution.
- semantic-propagation: Preserve technical versus spatial use across one-road-upward, downward, upward matter, vehicle, barrier, aperture, and body-direction entries. Search should distinguish “further-up” questions from ordinary “look up” language.
- final-cohort-gate: `run_cohort_gate.py` hardPass=true; exact KWIC 8/8, attribution hard failures 0, public-feedback flags 0, depth/sense hard failures 0, review flags 0, and forbidden-English matches 0. Report: `maintenance/semantic-cohorts/semantic-r001-owner1-upward-gate.json`.
