# Confucius (孔子) — research inventory

## Discovery provenance (guide §5 item 9)

- Source: `DISCOVERY_1.md` row 16, carried into `REQUESTED_TERMS.md` and `REQUESTED_BUILD_PLAN.md` r002-A.
- Inherited lead: “Confucius; the sage cited and quietly deflated as a foil—the classical sage measured against the Chan standard,” with “Confucius once displayed the first mechanism” as evidence and Zhongni as variant.
- Disposition: **revise and keep.** The direct deflations and first-mechanism line are attested, but “foil” is too narrow: the corpus also praises his words, maps them onto a Chan encounter, pairs his hand with Śākyamuni's, and borrows his nostrils. Preserve the mixed deployment without deciding a single stance.

## Concordance and depth

- Exact allowlisted count: 592 hits in 112 texts; frequency floor 7, retained 9 distinct anchors across 7 texts after the cross-check restored the unique Confucius's Staff deployment.
- Variant/family counts: Zhongni 268/120; “Confucius says” 40/25; “Confucius did not recognize” 6/4; “Confucius once displayed the first mechanism” 1/1.
- Definition sweep: no “Confucius is” lexicographic formula. High-value relations come from explicit textual comparisons and quoted sayings, all retained.
- Deployment inventory: first-mechanism claim; Confucius's sayings recast as blood-drawing staff blows; paired deflation with Bodhidharma; comic audience in a hall address; Dahui's three-figure nostril instruction; emperor-master ranking; mapping a Confucian saying onto the Longtan encounter; Śākyamuni/Confucius hand comparison; explicit agreement with the lineage's purport.

## Sense and family audit

- One figure referent. Confucius, Master Kong, and Zhongni are names for the same person; quotation, praise, comparison, and mockery are stances/deployments rather than senses.
- No separate book-title or office referent was found. Zhongni is retained as a name variant and family search control, not a second article claim.
- This pre-Zen figure is in scope under #0g and does not belong on the Chan lineage roster.
- #0g deviation: the records place Confucius inside Chan comparison—his words verify an encounter, his hand is paired with the flower-holding hand, his nostrils are borrowed, and he is also said not to know writing. The restored Confucius's Staff section is the sharpest lexical bend: it treats his sayings as invisible, blood-drawing staff blows and contrasts a dead-letter figure with a living staff.
- Definition retest after enrichment: **KEEP one figure sense.** The staff passage radically redeploys Confucius but still denotes the same person; it changes neither the referent nor the figure's corpus-wide identity.
- Omission audit: the large Juelang concentration was not allowed to dominate; seven independent sources cover affirmative, comparative, comic, critical, quoted-saying, and staff-language deployments.
- Verification: the added KWIC was checked with `PYTHONIOENCODING=utf-8 zc.py verify` and matched exactly at `0792b29–0792b30`.
## Attribution remediation r002-A (2026-07-13)

- Before: 2/9 occurrences named; 7 null. After: 9/9 named; 0 null.
- Applied the six-rung ladder throughout. The preface witness is Li Changgeng, the Longtan comparison is initiated by layman Pan in Letan Hongying's section, and the lineage-agreement text is Liu Jingchen's prose.
- Li Changgeng, Tianfeng Xing, Letan Hongying, Zhuanyu Guanheng, and Liu Jingchen are identified but absent from the roster; retained in pinyin.
- The useful first-mechanism evidence was anchored and retained; no evidence was deleted.
- Definition/item 8 retest holds: all occurrences invoke the same Confucius figure under different Chan deployments.
- Unresolved ladder cases: none.

## semantic-r001 public-feedback remediation (2026-07-14)

- feedback-inference-verdict: KEEP one figure sense. Confucius, Master Kong, and Zhongni name the same person; praise, quotation, comparison, borrowing, ranking, comic handling, deflation, and staff language are different Chan stances toward him, not different referents.
- feedback-observations: Independent texts say his sayings accord with the lineage's subtle purport, map “I conceal nothing” onto Longtan's encounter, place him beside the Buddha and Laozi as borrowed nostrils, and pair his lute-playing hand with the flower-holding hand. Other named authors say he did not recognize writing, make him laugh and fall over, rank him, or turn his sayings into blood-drawing staff blows.
- feedback-falsification-searches: Rechecked exact name, Zhongni, “Confucius says,” silent knowing, one thread, conceal nothing, first mechanism, nostrils, hand comparison, writing deflation, emperor-master ranking, and Confucius's Staff. Tested for a uniformly hostile foil sense or a separate staff-bearing person; both are contradicted by the mixed predicates.
- feedback-counterexamples: Explicit agreement with lineage purport and positive encounter comparison disprove defining him only as a foil. Sharp deflation and living-staff language disprove presenting him as a protected authority. The dictionary reports this tension without resolving it.
- feedback-scope: One corpus-wide pre-Zen figure, defined by Chan deployment and deliberately kept off the Chan lineage roster.
- lookup-probes: Reader probes covered “Confucius,” “Master Kong,” “Kongzi,” “Zhongni,” and “Confucius's staff.” These are now approved SearchAliases.
- opening-interpretation-verdict: KEEP the mixed-deployment opening. It identifies the classical teacher and immediately states that Chan authors quote, compare, praise, and cut him down rather than reducing him to one stance.
- definition-formula-audit: No lexicographic “Confucius is” formula occurs. Explicit named comparisons, direct quotation, ranking, first-mechanism claim, and staff-section predicates establish how the figure functions in the corpus.
- nested-family-audit: Master Kong and Zhongni remain name variants, while Confucius's Staff is a larger named section and metaphorical deployment. None creates another bare-name sense.
- modifier-and-provenance-audit: No feedback modifier is at issue. All nine anchors were re-read; exact speakers or authors and source titles remain named, including identified non-roster authors.
- semantic-propagation: Preserve mixed positive and negative deployment in linked entries for Zhongni, Longtan, one thread, first mechanism, staff, Buddha, and Laozi. Search should recover the figure under all common English and Chinese-name forms.
- final-cohort-gate: `run_cohort_gate.py` hardPass=true; exact KWIC 9/9, attribution hard failures 0, public-feedback flags 0, depth/sense hard failures 0, review flags 0, and forbidden-English matches 0. Report: `maintenance/semantic-cohorts/semantic-r001-owner1-confucius-gate.json`.
