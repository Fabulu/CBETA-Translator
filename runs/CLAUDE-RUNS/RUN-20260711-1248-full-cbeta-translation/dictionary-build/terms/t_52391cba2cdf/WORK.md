# WORK — 三玄三要 (t_52391cba2cdf) "Three Mysteries and Three Essentials"

Authored 2026-07-11 20:49 +02:00. Batch b004, agent 3 (Ms. Frizzle field trip).

## Concordance (Zen allowlist only, 462 texts)
- 三玄三要 exact compound: **TOTAL 458 occ across 157 allowlist files.** Densest: J25nB171 (25), J34nB299 (19), J34nB300 (16), X64n1260 (12), C077n1710 (11), T48n2006 (9). Pervasive across yulu/denglu/handbooks → obviously multi-source.
- The compound is Linji-house shorthand. Its DOCTRINAL SOURCE is the un-compounded formula in the Linji lu.

## Sense analysis
One corpus-wide sense (SenseKey=null). The term has a single meaning across the corpus: Linji 臨濟義玄's layered measure of teaching-depth. Two registers, not two senses:
1. **Doctrinal / origin** — Linji's formula 一句語須具三玄門，一玄門須具三要，有權、有用 (T47n1985 0497a19-a20). A single teaching-phrase (一句) must contain three "mysterious gates" (三玄門), each gate three "essentials" (三要), each holding 權 (provisional/expedient) and 用 (function). The 三要 also drives his first-phrase answer (0497a15-a16: 三要印開朱點側…).
2. **Tradition shorthand** — Fenyang 汾陽善昭 canonizes the compound (T47n1992 0598c17: 一句中有三玄三要。賓主歷然) and his verse (0597b07-b08). Handbooks (人天眼目 T48n2006) systematize the enumeration and note it all folds into one shout (0311b20: 自是一喝中。體攝三玄三要也).

## Attribution evidence (governing cb:mulu heads)
- T47n1985 = 鎮州臨濟慧照禪師語錄; 師 = 臨濟義玄 (師又云 / 師云). ✓ roster.
- T47n1992 = 汾陽無德禪師語錄 (mulu1 汾陽無德禪師語錄序); the 三玄三要 prose and verse are Fenyang's. 五燈會元 X80n1565 corroborates: verse sits under head 汾州太子院善昭禪師. ✓ 汾陽善昭 in roster.
- T48n2006 = 人天眼目 handbook, 臨濟宗 section; the enumeration lines are editorial exposition → MasterName=null.

## Multi-source verdict: MULTI-SOURCE
Two independent masters in their own records (臨濟義玄 T47n1985, 汾陽善昭 T47n1992) + independent handbook exposition (人天眼目 T48n2006) + 五燈會元 witness. Well clear of the ≥2 gate.

## Anti-pattern checks
- KWICs: all verbatim contiguous substrings after tag+whitespace strip (verified via friz_verify.py). Note-free spans chosen.
- No ellipsis / no stitching.
- lb from ed="T" (no X-canon files cited here except SourceTexts list; the cited occurrences are all T-canon → single lb system).
- Deflationary gloss: rendered "mysteries/essentials" literally; explicitly flagged that the fixed 玄/要 enumerations are later commentators', not Linji's — avoids importing a fabricated metaphysical triad.
- 分明 vs 明明 verse variant: matched each KWIC to ITS file (T47n1992 = 分明).

## RelatedTerms rationale
三玄, 三要 (genuine constituents), 四料揀, 四賓主 (Linji's sibling devices, listed together in sources e.g. 三玄三要四料揀), 一喝 (the shout that "embraces" the schema). Not coincidental prefixes.

## GATE 2 (Claude adversarial verify+repair) — 2026-07-11 · STATUS=verified
Re-derived every occurrence from source (whitespace-normalized exact-substring + lb-anchor script).
- **KWICs (6): all EXACT CONTIGUOUS**, zero ellipses, each verified as a whitespace-normalized substring of its cited file. #corrected = 0.
- **FromLb/ToLb: all 6 confirmed** as nearest-preceding ed="T" lb (single-ed T-canon files). #corrected = 0.
- **Contamination: 0.** All occurrence RelPaths + all 5 SourceTexts in zen-corpus.json. Extra SourceTexts contain the term (X80n1565=4, J25nB171=23). #removed = 0.
- **Attribution (confirmed at governing structure):**
  - T47n1985 0497a19/0497a15 → governing mulu 鎮州臨濟慧照禪師語錄; 師=臨濟義玄. The un-compounded layering formula is Linji's. ✓ kept 臨濟義玄.
  - T47n1992 0598c17 / 0597b07 → mulu 汾陽無德禪師語錄 卷上; 0597b07 preceded by `師云…與大眾頌出` (師=Fenyang) → verse is Fenyang's; compound canonized by 汾陽善昭 in his own record. ✓ kept 汾陽善昭.
  - T48n2006 0311b19/0311b20 → 人天眼目 editorial handbook (臨濟門庭 section) → null correct.
  - #attribution fixes = 0.
- **Explanation quotes:** all six quoted spans are curated KWICs; parenthetical collocation 三玄三要四料揀 verified verbatim in allowlist text X80n1565 (五燈會元). #unverified-claims removed = 0.
- **Multi-source:** 2 independent masters in own records (臨濟義玄, 汾陽善昭) + independent handbook → holds. No downgrade.
- Deflationary gloss intact (literal mysteries/essentials; Note flags fixed 玄/要 enumerations as later commentators', not Linji's).
- entry.v2.json unchanged (clean). VERDICT: verified.

## Attribution remediation — 2026-07-13

- Before: 3 exact-headword occurrences / 3 unlabelled component supports; 2 exact sources; required exact floor 7.
- Labelled Linji's two component formulas and the handbook enumeration `family`; none counts toward exact-headword depth. Assigned the compiler Zhizhao to the handbook voice.
- Added four exact witnesses from Tianyin Yuanxiu, Qiran Chaozhi, Jie'an Wujin, and Luyan He. After: 7 exact / 3 support across 6 exact sources.
- Item 8 retest: formula, verse, house-device list, handbook explanation, and interview question all name the same device. No separately referential second thing appeared; one sense still holds.
- Definition retest: the new evidence confirms the Linji-house identification and live question form while warning against presenting any later enumeration as a universally stable definition.
- Audits: 10/10 KWICs exact with declared lb bounds; attribution hard failures 0; depth/sense hard failures 0. The broad-concordance single-sense flag was reviewed and adjudicated as one sense.

## Speaker-level peer remediation — 2026-07-13

- Normalized the handbook compiler's full name to Huiyan Zhizhao in both occurrences and their notes.
- Reclassified the Luyan interview as supporting evidence under its actual phrase speaker, an unnamed monk in Luyan He's assembly; Luyan remains explicitly identified as respondent and in the explanatory discussion.
- Added a verified exact witness from Qiran Chaozhi instructing hearers to remove the Linji devices, preserving the exact-headword floor without borrowing the monk's question as Luyan's speech.
- Definition/sense retest: enumeration, question, answer, list, and instruction still refer to the same named Linji-house device, not different things. Final depth remains 7 exact, now with 4 explicitly labelled supports.
- Final cohort audit: all 11 KWICs exact with declared lb bounds; attribution hard failures 0; all prose Chinese anchored.

## Synthetic-link correction — 2026-07-13

- Removed the Luyan exchange rather than fabricate a live person link for its unnamed monk. Its exact phrase occurs only in the monk's question; Luyan's answer does not establish a distinct lexical fact and is unnecessary for depth because Qiran Chaozhi supplies named, direct public-question evidence.
- Removed the corresponding Luyan-specific prose claim, source, and related-master link. This is an explicit omission decision based on attribution quality, not deletion of an unanchored Chinese prose quotation.
- Depth after omission: 7 exact / 3 support. The single-sense definition still holds: all retained evidence refers to the same Linji-house device.


## 2026-07-14 semantic remediation (r001 owner 2)

- research-paths: apparatus-clean `zc.count`; the existing full-concordance, definition-formula, collocation, and deployment inventory above; and exact `zc.verify` replay of every stored occurrence.
- corpus-count-refresh: 506 hits across 164 allowlisted files.
- observation: T/T47/T47n1985.xml#0497a19, T/T47/T47n1985.xml#0497a15 anchor the defining predicates and distinct deployment classes summarized above.
- minimal-inference: The three mysteries and three essentials name a Linji-house verbal device that later masters enumerate and test in public interviews.
- ordinary-bridge: graph/scene layer = three mysteries and three essentials; ordinary referent = named verbal framework; Chan deployment = Linji-house device and public test.
- falsification-searches: rechecked literal uses, definition formulas, longer compounds, grammatical role changes, incompatible predicates, alternate referents, and linked family terms.
- counterexamples: ordinary, family, title, and compound uses were retained only at their demonstrated scope; none was allowed to lend an unanchored sense to the headword.
- scope: corpus-wide unless a retained sense explicitly names a narrower set or local definition.
- verdict: licensed — the opening is the smallest reproducible inference from stored predicates and assigns neither outside symbolism nor speaker intention.
- search-probes: three mysteries and three essentials / Linji three mysteries / three mysterious gates and essentials / three mysteries three essentials. These are retrieval metadata, not extra interpretation menus.
- nested-compound-verdict: longer compounds were inventoried and do not buy the bare headword's meaning or depth.
- verb-frame-verdict: governing predicates were re-clustered; the retained split/merge follows referent identity rather than noun/verb packaging, role, or favorable/hostile reading.
- sense-target-distinguishability: ONE SENSE — grammatical roles, appraisals, and alternate phrasings do not establish another referent.
- display-modifier-verdict: not applicable; the visible targets make no unsupported construction-material claim.
- family-definition-retest: related and overlapping entries named in the prior inventory were compared; no retained definition requires one witness to mean incompatible things.
- opening-interpretation-verdict: PASS — T/T47/T47n1985.xml#0497a19, T/T47/T47n1985.xml#0497a15 license the reader-ready opening at the stated scope; literal/family counterexamples narrow rather than defeat it.
- omission-audit: every unique prose claim remains anchored or explicitly tied to a recorded count/collocation; no useful quotation was deleted.

### Prescribed public-feedback ledger keys

- feedback-inference-verdict: LICENSED — the reader-facing opening is the least conclusion that makes the stored predicates and deployment classes intelligible; no outside doctrine, symbolism, psychology, or intention is imported.
- feedback-observations: T/T47/T47n1985.xml#0497a19, T/T47/T47n1985.xml#0497a15; the full occurrence/deployment inventory above supplies the remaining observations.
- feedback-falsification-searches: literal/ordinary uses; definition formulas; incompatible predicates; longer nested compounds; alternate referents; titles/persons; and linked family entries were rechecked against the allowlisted concordance.
- feedback-counterexamples: ordinary and compound uses remain at their attested scope and were not allowed to manufacture a headword sense; any retained second sense has its own exact-headword witness.
- feedback-scope: corpus-wide unless a sense target and its anchors explicitly identify a named set, local equation, title, object, or institutional referent.
- lookup-probes: three mysteries and three essentials / Linji three mysteries / three mysterious gates and essentials / three mysteries three essentials.
- plain-english-image-verdict: PASS — each opening names the referent before frequency, graph parsing, or quotations; concrete images retain the load-bearing ordinary scene.
