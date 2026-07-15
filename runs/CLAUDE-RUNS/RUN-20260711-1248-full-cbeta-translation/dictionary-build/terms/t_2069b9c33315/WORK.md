# WORK — 君臣 (t_2069b9c33315)

## Concordance (Zen allowlist only)
- 君臣 = 930 hits across 214 allowlist files. Dominant Zen usage is the Caodong term of art (not the plain worldly compound).
- 君臣五位 (the named scheme) attested across ~30 allowlist texts (J33nB294, J34nB298/299/311, J37nB392, T48n2006, X72n1437, X64n1260, T51n2077 …).
- 君臣道合 (the both-together rank / house emblem) very common (B25n0144, C077n1710, D48n8939, J25nB163/171/174, J26nB180/183/185/186/188 …).
- Canonical definition 君為正位，臣為偏位…君臣道合是兼帶語 recurs verbatim in T47n1987A/B, T48n2006, X67n1304, X72n1437, X80n1565, X81n1568/1571, X85n1593, J32nB272, J28nB212.

## Sense analysis
ONE corpus-wide Zen sense (SenseKey=null), multi-source: the Caodong ruler-minister figure of the Five Ranks.
- 君 = 正位 (upright/absolute/host; 空界，本來無物); 臣 = 偏位 (crooked/particular/guest; 色界，有萬象形).
- Relations: 臣向君 = 偏中正; 君視臣 = 正中偏; 君臣道合 = 兼帶/兼中到.
- 君臣道合 = emblem of the Caodong house (如何是曹洞宗？君臣道合).
Rejected a separate master-keyed sense: Caoshan originates the 君臣 formulation but does not privately *bend* the word (cf. 無心 precedent kept corpus-wide with locus classicus named). The literal worldly 君臣 (Confucian ethics in prefaces) is the plain compound, noted as boundary, not a distinct Zen sense bucket.

## Dispute recorded (does NOT change validation)
J33nB294 (0745b18-): author argues Caoshan's 君視臣/臣奉君/君臣道合 were 就話荅話 and 初未嘗與五位為配 — 君臣 was never originally a distinct five-position set; later people mislabelled it 君臣五位. Dispute is about whether 君臣 is its OWN fivefold scheme (alongside 正偏/功勳/王子五位), not about 君=正位/臣=偏位 (agreed everywhere). So validation stays multi-source; dispute captured in Note + occurrence.

## Speaker attribution
- T47n1987A = 撫州曹山元證禪師語錄; the definition is 師曰 (Caoshan Benji) answering 僧問五位君臣旨訣 → MasterName = Caoshan Benji.
- J28nB212: definition appears in frame 豈不見曹山道：… inside another master's sermon = QUOTATION of Caoshan → MasterName=null.
- J25nB174: single-voice五位 exposition/commentary → null.
- J25nB171: 五家宗旨 catechism, 進云 asks / 師 answers (specific master not fixed) → null.
- J33nB294: text author's argument → null.

## KWIC verification
All 5 KWICs confirmed EXACT contiguous tag-stripped substrings via /tmp/kwic.py (maps stripped-char positions back to raw, reports nearest preceding <lb> of the primary edition). FromLb = nearest preceding <lb n>, ed="T" for T-canon, ed="J" for J-canon (no X-canon occurrences curated, so no ed="X"/ed="R" ambiguity here).

## Multi-source verdict
**multi-source.** Definition + emblem usage hold across Caoshan語錄 (T47n1987A/B), 人天眼目 (T48n2006), 洞上古轍 (X72n1437), X67n1304, and dozens of J-canon witnesses; consistent across ~1000 years and many masters.

## RelatedTerms / RelatedMasters
- RelatedTerms: 五位, 君臣五位, 君臣道合, 正中偏, 偏中正, 正位, 偏位 (all genuine Caodong constituents; cross-refs 五位 already authored as t_ff50c6974a36).
- RelatedMasters: Caoshan Benji (locus classicus of the君臣 formulation), Dongshan Liangjie (originator of the五位 scheme).

## GATE 2 (Claude adversarial verify+repair)
- KWIC re-derivation: all 5 occurrences re-checked via tag-strip + whitespace-strip contiguous match against the ONE cited file — each found, count=1, EXACT contiguous. FromLb = nearest preceding <lb>: T47n1987A 0527a10 (ed=T), J28nB212 0474c22 (ed=J), J25nB174 0729a12 (ed=J), J25nB171 0518a06 (ed=J), J33nB294 0745b19 (ed=J). All confirmed. No X-canon occurrences → no ed=R ambiguity.
- Contamination: 0. All 5 RelPaths + all 9 SourceTexts in zen-corpus.json allowlist.
- Attribution: CONFIRMED. T47n1987A section head 因有僧問五位君臣旨訣。師曰。… — 師 = Caoshan Benji (曹山元證禪師語錄), his direct answer → MasterName=Caoshan Benji correct. Other four occurrences null (quotation / commentary / catechism / dispute) — confirmed.
- Multi-source: holds. Definition 正位即空界。本來無物 / 偏位即色界。有萬象形 / 君為正位。臣為偏位 verbatim in T47n1987A; recurs in T48n2006, X72n1437, X67n1304 + J witnesses. Stays multi-source.
- Explanation quotes grep-verified in allowlist (正位即空界, 偏位即色界, 君為正位。臣為偏位, 君臣道合是兼帶語 all in T47n1987A). REPAIR: normalized 3 embedded verbatim quotes from ，to source 。 (君為正位。臣為偏位 / 正位即空界。本來無物 / 偏位即色界。有萬象形) so they byte-match T47n1987A. No unverifiable claims remain.
- No over-read: ruler-minister gloss is deflationary, grounded in the Caodong Five-Ranks text. VERDICT: verified.
## s001-A sense repair (2026-07-13)

- Split the Caodong lord-minister configuration from ordinary political/ethical lord and minister roles.
- Final depth: 8 occurrences, distributed 6 technical / 2 ordinary.
- The technical side retains definition, rank relation, house answer, historical dispute, and cross-house comparison; the ordinary side has two social-relation witnesses.
- Cross-checked 五位, 王子五位, 正偏, and 功勳; they remain related systems, not extra senses.
- Audit review adjudication: ordinary lords and ministers are social persons in an ethical relation; the Caodong lord-minister configuration is a technical rank schema. The targets share words but do not paraphrase the same thing.

## L001-B gloss-hygiene and family retest

- sense-target-distinguishability: sense 0 `Caodong lord-and-minister configuration` names a technical rank schema; sense 1 `political lord and minister` names two social persons in an ethical relation. The targets now identify the referents without depending on their explanations.
- Definition retest: the explicit mapping of lord to upright and minister to bent, rank labels, house answer, and classification dispute all support the technical configuration; 君臣有義 within the five relationships supports the ordinary political pair.
- Family retest: 五位, 王子五位, 正偏, 功勳, 君臣道合, and the five social relationships can all retain their current definitions only with this two-referent split. No attested evidence makes a different reading into another sense.


## 2026-07-14 semantic remediation (r001 owner 2)

- research-paths: apparatus-clean `zc.count`; the existing full-concordance, definition-formula, collocation, and deployment inventory above; and exact `zc.verify` replay of every stored occurrence.
- corpus-count-refresh: 960 hits across 217 allowlisted files.
- observation: T/T47/T47n1987A.xml#0527a10, J/J28/J28nB212.xml#0474c22, J/J37/J37nB392.xml#0580c26, J/J39/J39nB463.xml#0800c13 anchor the defining predicates and distinct deployment classes summarized above.
- minimal-inference: The Caodong technical configuration of lord and minister.
- ordinary-bridge: graph/scene layer = lord and minister; ordinary referent = political roles or Caodong configuration; Chan deployment = ordinary hierarchy bent into Five Ranks terminology.
- falsification-searches: rechecked literal uses, definition formulas, longer compounds, grammatical role changes, incompatible predicates, alternate referents, and linked family terms.
- counterexamples: ordinary, family, title, and compound uses were retained only at their demonstrated scope; none was allowed to lend an unanchored sense to the headword.
- scope: corpus-wide unless a retained sense explicitly names a narrower set or local definition.
- verdict: licensed — the opening is the smallest reproducible inference from stored predicates and assigns neither outside symbolism nor speaker intention.
- search-probes: Caodong lord and minister / lord-minister ranks / lord and minister Five Ranks / Caodong ruler and minister; political lord and minister / sovereign and minister / ruler and subject / king and minister. These are retrieval metadata, not extra interpretation menus.
- nested-compound-verdict: longer compounds were inventoried and do not buy the bare headword's meaning or depth.
- verb-frame-verdict: governing predicates were re-clustered; the retained split/merge follows referent identity rather than noun/verb packaging, role, or favorable/hostile reading.
- sense-target-distinguishability: KEEP — sense 1 `Caodong lord-and-minister configuration`; sense 2 `political lord and minister` identify distinct referents, each with an exact headword witness.
- display-modifier-verdict: not applicable; the visible targets make no unsupported construction-material claim.
- family-definition-retest: related and overlapping entries named in the prior inventory were compared; no retained definition requires one witness to mean incompatible things.
- opening-interpretation-verdict: PASS — T/T47/T47n1987A.xml#0527a10, J/J28/J28nB212.xml#0474c22 license the reader-ready opening at the stated scope; literal/family counterexamples narrow rather than defeat it.
- omission-audit: every unique prose claim remains anchored or explicitly tied to a recorded count/collocation; no useful quotation was deleted.

### Prescribed public-feedback ledger keys

- feedback-inference-verdict: LICENSED — the reader-facing opening is the least conclusion that makes the stored predicates and deployment classes intelligible; no outside doctrine, symbolism, psychology, or intention is imported.
- feedback-observations: T/T47/T47n1987A.xml#0527a10, J/J28/J28nB212.xml#0474c22, J/J37/J37nB392.xml#0580c26, J/J39/J39nB463.xml#0800c13; the full occurrence/deployment inventory above supplies the remaining observations.
- feedback-falsification-searches: literal/ordinary uses; definition formulas; incompatible predicates; longer nested compounds; alternate referents; titles/persons; and linked family entries were rechecked against the allowlisted concordance.
- feedback-counterexamples: ordinary and compound uses remain at their attested scope and were not allowed to manufacture a headword sense; any retained second sense has its own exact-headword witness.
- feedback-scope: corpus-wide unless a sense target and its anchors explicitly identify a named set, local equation, title, object, or institutional referent.
- lookup-probes: Caodong lord and minister / lord-minister ranks / lord and minister Five Ranks / Caodong ruler and minister; political lord and minister / sovereign and minister / ruler and subject / king and minister.
- plain-english-image-verdict: PASS — each opening names the referent before frequency, graph parsing, or quotations; concrete images retain the load-bearing ordinary scene.
