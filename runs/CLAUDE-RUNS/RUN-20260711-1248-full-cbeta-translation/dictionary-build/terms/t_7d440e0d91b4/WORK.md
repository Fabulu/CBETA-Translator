# WORK — 公案 (t_7d440e0d91b4)

## Sense split
One corpus-wide sense (SenseKey null): a recorded encounter/saying of the old masters,
figured on the legal case-record of a government office, raised as a touchstone/test.
No master bends it to a private meaning, so no master-specific sense.

## Multi-source gate: PASS (multi-source)
Three independent Zen witnesses, Song→Yuan:
- B25n0145 天目中峰廣錄 (中峰明本) — the explicit definition (公府之案牘; 舉公案以决之). LOCUS CLASSICUS.
- T48n2003 碧巖錄序 (三教老人) — 世間法中吏牘語; 立為公案，留示叢林. Prefatory essay, not a dated
  master saying → occurrence MasterName null, noted.
- T48n2005 無門關 (無門慧開 self-preface) — 將古人公案作敲門瓦子.

## Attribution checks
- B25n0145 answer speaker "幻" = 幻住 = Zhongfeng's studio name. Confirmed his words.
- 碧巖錄序 signed 三教老人書 (line 0140a04); the 公案 essay is inside that 序 (head 碧巖錄序, 0139a03).
- 無門關 preface: 慧開 names himself at 0292b16 immediately before the cited line.

## KWIC verification
All fragments greped verbatim in-file (crossing spans join only across <lb>/<pb> tags,
contiguous after tag-strip). PreferredTarget "public case" (ewk "Case" supported); avoided
"koan" as primary per deflationary rule.

## Occurrences curated: 5 (2 Zhongfeng, 2 BCR-preface, 1 Wumen)

## GATE 2 (Claude adversarial verify+repair) — VERIFIED
- All 5 KWICs re-grepped in cited files: exact-contiguous-verbatim after tag+whitespace strip
  (cross-<lb> joins only, no ellipsis, no altered punctuation).
- Allowlist: B25n0145, T48n2003, T48n2005 all in zen-corpus.json. Zero contamination.
- Attribution confirmed at section heads: B25 = 或問…幻曰 (幻住 = 中峰明本, his own answer);
  T2003 = 三教老人 preface (MasterName null, correct — a prefacer not a dated master);
  T2005 = 慧開 self-names (臣僧慧開謹言 / 慧開紹定戊子夏) — Wumen's own preface. All correct.
- FromLb/ToLb re-derived = nearest preceding <lb n>: all 5 correct (incl. 0798a02→a03 cross-lb).
- Multi-source (3 independent texts, Song→Yuan) upheld; RelatedTerms are genuine semantic
  associates (no coincidental-prefix relation); rendering stays deflationary ("case", not "koan").
- No repairs needed. STATUS=verified.

## 2026-07-13 full remediation
- Rebuilt to 9 total / 8 exact witnesses across 6 exact sources at 3,800 hits / 363 files; the headword-absent legal gloss is family evidence.
- Preserved the corpus's legal case-file self-definition, teacher adjudication, Wumen's gate-tile statement, Yuanwu's historical question, old-case tracing, a renewed public exchange, and a ready-made case.
- All KWICs/bounds and both audits pass; broad-single-sense review was consciously retained as one public case-record object.
## 2026-07-13 independent cross-entry attribution repair

- The `未挂古帆，現成公案` passage is the same exact X82 witness used in the 現成 entry.
  Its section heading is `開封淨因佛日惟嶽禪師`; the project-standard/source-conservative
  name is **Jingyin Weiyue**, not Furi Weiyue. Entry and note now agree across both headwords.


## 2026-07-14 semantic remediation (r001 owner 2)

- research-paths: apparatus-clean `zc.count`; the existing full-concordance, definition-formula, collocation, and deployment inventory above; and exact `zc.verify` replay of every stored occurrence.
- corpus-count-refresh: 3800 hits across 363 allowlisted files.
- observation: B/B25/B25n0145.xml#0798a02, B/B25/B25n0145.xml#0798b13 anchor the defining predicates and distinct deployment classes summarized above.
- minimal-inference: A public case is a recorded encounter or saying of the old masters that is raised and set before someone in public interview.
- ordinary-bridge: graph/scene layer = public/government case-file; ordinary referent = recorded case; Chan deployment = legal language bent into public encounter evidence.
- falsification-searches: rechecked literal uses, definition formulas, longer compounds, grammatical role changes, incompatible predicates, alternate referents, and linked family terms.
- counterexamples: ordinary, family, title, and compound uses were retained only at their demonstrated scope; none was allowed to lend an unanchored sense to the headword.
- scope: corpus-wide unless a retained sense explicitly names a narrower set or local definition.
- verdict: licensed — the opening is the smallest reproducible inference from stored predicates and assigns neither outside symbolism nor speaker intention.
- search-probes: public case / case record / public case file / recorded encounter / case of the old masters. These are retrieval metadata, not extra interpretation menus.
- nested-compound-verdict: longer compounds were inventoried and do not buy the bare headword's meaning or depth.
- verb-frame-verdict: governing predicates were re-clustered; the retained split/merge follows referent identity rather than noun/verb packaging, role, or favorable/hostile reading.
- sense-target-distinguishability: ONE SENSE — grammatical roles, appraisals, and alternate phrasings do not establish another referent.
- display-modifier-verdict: not applicable; the visible targets make no unsupported construction-material claim.
- family-definition-retest: related and overlapping entries named in the prior inventory were compared; no retained definition requires one witness to mean incompatible things.
- opening-interpretation-verdict: PASS — B/B25/B25n0145.xml#0798a02, B/B25/B25n0145.xml#0798b13 license the reader-ready opening at the stated scope; literal/family counterexamples narrow rather than defeat it.
- omission-audit: every unique prose claim remains anchored or explicitly tied to a recorded count/collocation; no useful quotation was deleted.

### Prescribed public-feedback ledger keys

- feedback-inference-verdict: LICENSED — the reader-facing opening is the least conclusion that makes the stored predicates and deployment classes intelligible; no outside doctrine, symbolism, psychology, or intention is imported.
- feedback-observations: B/B25/B25n0145.xml#0798a02, B/B25/B25n0145.xml#0798b13; the full occurrence/deployment inventory above supplies the remaining observations.
- feedback-falsification-searches: literal/ordinary uses; definition formulas; incompatible predicates; longer nested compounds; alternate referents; titles/persons; and linked family entries were rechecked against the allowlisted concordance.
- feedback-counterexamples: ordinary and compound uses remain at their attested scope and were not allowed to manufacture a headword sense; any retained second sense has its own exact-headword witness.
- feedback-scope: corpus-wide unless a sense target and its anchors explicitly identify a named set, local equation, title, object, or institutional referent.
- lookup-probes: public case / case record / public case file / recorded encounter / case of the old masters.
- plain-english-image-verdict: PASS — each opening names the referent before frequency, graph parsing, or quotations; concrete images retain the load-bearing ordinary scene.
