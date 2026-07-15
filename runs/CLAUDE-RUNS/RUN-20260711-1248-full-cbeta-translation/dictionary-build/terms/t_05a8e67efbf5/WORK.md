# WORK — 頭上安頭

Built from scratch with the b018 guide and `zc.py`.

## Corpus harvest

- Headword: 280 hits in 141 allowlisted texts.
- Frame `大似頭上安頭`: 10 hits in 10 texts.
- Prohibition `不得頭上安頭`: 3 hits in 3 texts.
- Corpus comparisons harvested: `為蛇畫足`, `霜加雪上`, `枷上著杻`, `斬頭覓活`, and `迷頭認影`.
- Direct deployments harvested: Linji's “what do you lack?”, the fourth of four ailments, naming tests, and Yuanwu's fist encounter.

## Editorial decision

One corpus-wide idiomatic sense, retaining the literal image: “put another head atop one's head.” The explanation derives unnecessary addition from the texts' own comparisons rather than substituting a modern abstract label.

## Verification

Five curated KWICs across two source collections were passed to `zc.verify`; all return `ok == True`, and the exact verifier bounds are stored.

## Attribution remediation — 2026-07-13

- Before/after: 5 → 11 occurrences; 1 → 11 named occurrences; 5 notes lacking exact source titles → 0; 4 → 0 dangling Chinese evidence strings. Two further headword-bearing witnesses raised source spread from two to four texts.
- Six-rung ladder: Linji Yixuan and Shaolin En resolve from `聯燈會要` section headers; Baiyun Wuliang Cang, Baohua Xian, Yuanwu Keqin, Sanmei Faru Le, and Zhenjing Kewen resolve from immediate `五燈全書` sections/context; Ruiyan Zihong and Jingyin Facheng resolve from immediate `聯燈會要` headers. Yuanwu, not Huqiu Shaolong, speaks the stored verdict in the fist encounter.
- Quote anchoring: added exact allowlisted witnesses for `斬頭覓活`, `為蛇畫足`, `枷上著杻`, and `霜加雪上`; preserved the original orthographic `為虵畫足` occurrence as well.
- Roster expansion needed: Baiyun Wuliang Cang, Baohua Xian, Shaolin En, Sanmei Faru Le, Ruiyan Zihong, Jingyin Facheng, Huayan Shengke, Chaozong Tongren. Their source-attested pinyin remains in occurrence `MasterName` fields.
- Definition/item-8 retest: KEEP one idiomatic sense. The four newly anchored expressions are corpus-native comparisons for unnecessary addition or its contrary, not additional referents of `頭上安頭`.
- Unfindable quotations: none. All eleven stored KWICs passed `zc.verify` with synchronized bounds.

## Support-role gate follow-up — 2026-07-13

- Occurrence 7 is now explicitly `EvidenceRole: family`: it attests the comparison “drawing feet on a snake” (`為蛇畫足`), not the exact headword, and cannot buy headword depth.
- Ten remaining occurrences contain the exact headword across six source texts, so removing the family witness from depth leaves the frequency floor and source-spread floor satisfied.
- Definition/item-8 retest still holds: the snake-feet line is a corpus-native family comparison for added wording, not a second referent of `頭上安頭`.


## 2026-07-14 semantic remediation (r001 owner 2)

- research-paths: apparatus-clean `zc.count`; the existing full-concordance, definition-formula, collocation, and deployment inventory above; and exact `zc.verify` replay of every stored occurrence.
- corpus-count-refresh: 280 hits across 141 allowlisted files.
- observation: X/X79/X79n1557.xml#0086c09, X/X82/X82n1571.xml#0273b14 anchor the defining predicates and distinct deployment classes summarized above.
- minimal-inference: Putting another head atop one's head is an image of needless addition to what is already complete.
- ordinary-bridge: graph/scene layer = put a head atop a head; ordinary referent = needless physical addition; Chan deployment = verdict on added words or operations.
- falsification-searches: rechecked literal uses, definition formulas, longer compounds, grammatical role changes, incompatible predicates, alternate referents, and linked family terms.
- counterexamples: ordinary, family, title, and compound uses were retained only at their demonstrated scope; none was allowed to lend an unanchored sense to the headword.
- scope: corpus-wide unless a retained sense explicitly names a narrower set or local definition.
- verdict: licensed — the opening is the smallest reproducible inference from stored predicates and assigns neither outside symbolism nor speaker intention.
- search-probes: put a head atop the head / add another head / head upon head / unnecessary addition. These are retrieval metadata, not extra interpretation menus.
- nested-compound-verdict: longer compounds were inventoried and do not buy the bare headword's meaning or depth.
- verb-frame-verdict: governing predicates were re-clustered; the retained split/merge follows referent identity rather than noun/verb packaging, role, or favorable/hostile reading.
- sense-target-distinguishability: ONE SENSE — grammatical roles, appraisals, and alternate phrasings do not establish another referent.
- display-modifier-verdict: not applicable; the visible targets make no unsupported construction-material claim.
- family-definition-retest: related and overlapping entries named in the prior inventory were compared; no retained definition requires one witness to mean incompatible things.
- opening-interpretation-verdict: PASS — X/X79/X79n1557.xml#0086c09, X/X82/X82n1571.xml#0273b14 license the reader-ready opening at the stated scope; literal/family counterexamples narrow rather than defeat it.
- omission-audit: every unique prose claim remains anchored or explicitly tied to a recorded count/collocation; no useful quotation was deleted.

### Prescribed public-feedback ledger keys

- feedback-inference-verdict: LICENSED — the reader-facing opening is the least conclusion that makes the stored predicates and deployment classes intelligible; no outside doctrine, symbolism, psychology, or intention is imported.
- feedback-observations: X/X79/X79n1557.xml#0086c09, X/X82/X82n1571.xml#0273b14; the full occurrence/deployment inventory above supplies the remaining observations.
- feedback-falsification-searches: literal/ordinary uses; definition formulas; incompatible predicates; longer nested compounds; alternate referents; titles/persons; and linked family entries were rechecked against the allowlisted concordance.
- feedback-counterexamples: ordinary and compound uses remain at their attested scope and were not allowed to manufacture a headword sense; any retained second sense has its own exact-headword witness.
- feedback-scope: corpus-wide unless a sense target and its anchors explicitly identify a named set, local equation, title, object, or institutional referent.
- lookup-probes: put a head atop the head / add another head / head upon head / unnecessary addition.
- plain-english-image-verdict: PASS — each opening names the referent before frequency, graph parsing, or quotations; concrete images retain the load-bearing ordinary scene.
