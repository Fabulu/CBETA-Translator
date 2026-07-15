# 棒喝 (t_0f97bfab265c) — work notes

**STATUS: verified** (Gate 2, adversarial re-derivation from Chinese).

## Gate 2 verification (independent Claude pass)
Re-grepped each cited file; every KWIC confirmed EXACT CONTIGUOUS after XML-tag stripping:
- B27n0152 `德山入門便棒臨濟入門便喝不動棒喝還有為人處也無` ✓ (lb 0552a19→a20; 還/有 split by an lb tag but contiguous)
- J28nB202 `臨濟出世後，唯以棒喝示徒，凡見僧入門便喝。` ✓ (lb 0088c03)
- J20nB098 `德山、臨濟，棒喝交馳，機鋒掣電，令你捫摸不入、插足不得` ✓ (lb 0516b15→b16)
- J26nB185 `問：「德山棒、臨濟喝，除此二途，有何指示？」師打云：「喚作棒喝，入地獄如箭。」` ✓ (lb 0583a26→a27) — **FromLb 0583a27 → 0583a26** (opening 問 is on a26).
- J27nB198 `近時有一等拍盲禪者，高談臨濟專事棒喝，離此別無長處` ✓ (lb 0449c05→c06) — **FromLb 0449c06 → 0449c05** (opening 近 is on c05).
- Contamination: none — all 7 RelPaths allowlisted. Multi-source solid. Single corpus-wide sense correct (master-specific attributions live in components 棒/德山棒 & 喝/臨濟喝). Over-read: none — gloss literal + keeps the self-critical register. RelatedTerms genuine.


## Concordance (Zen allowlist only)
- **230 allowlist files, ~487 occurrences.** Method as for the other terms (grep → filter to `zen-corpus.json` → verbatim KWIC + nearest `<lb n>`).

## Sense analysis
**One sense, corpus-wide (SenseKey=null).** 棒喝 = "blows and shouts" — the fixed compound of 棒 (the stick/blow, Deshan Xuanjian: **德山棒**) and 喝 (the shout, Linji Yixuan: **臨濟喝**). It names the abrupt, wordless, physical teaching of the Linji–Deshan style.

Signature collocations, all attested:
- **德山棒、臨濟喝** — explicit dual attribution [J26nB185].
- **入門便棒／入門便喝** — "as soon as [a monk] enters the gate, [Deshan] hits / [Linji] shouts" [B27n0152; J28nB202].
- **唯以棒喝示徒** — Linji "taught only with stick and shout" [J28nB202].
- **棒喝交馳** — "blows and shouts flying back and forth," the commonest phrase; function = cut off conceptual grasping so the student "gets no finger-hold, no foot-hold" [J20nB098].

**Self-critical register (important, kept in the gloss):** the corpus repeatedly turns the term against its own abuse —
- 喚作棒喝，入地獄如箭 ("call it 'stick-and-shout' [as a thing] and you drop into hell like an arrow") [J26nB185]
- 瞎棒狂喝 (blind stick, wild shout), 亂施棒喝, 拍盲禪者…專事棒喝 [J27nB198, J27nB194].
This is not a second sense — it is the same referent being criticized; folded into Explanation/Note.

## Multi-source verdict
**multi-source** — B27n0152, J28nB202, J20nB098, J26nB185, J27nB198, plus C077n1710, D48n8939 and many more; two originating masters; ~1000-year spread. The reading is stable.

## Master-specific senses?
**Not warranted for the compound.** Although the components carry firm single-master attributions (棒→Deshan, 喝→Linji), the two-graph word 棒喝 is *always* used jointly as the paired emblem. The master-specific senses live in the component terms **德山棒** and **臨濟喝** (listed under RelatedTerms), which should get their own entries — NOT in 棒喝. Splitting 棒喝 into Deshan/Linji senses would misrepresent how the compound is actually used.

## Curated occurrences (5)
B27n0152 (dual attribution + 入門便棒/喝) · J28nB202 (Linji's standing method) · J20nB098 (棒喝交馳 + function) · J26nB185 (both named + self-critique) · J27nB198 (critique of mechanical 棒喝). All verbatim-verified.

## Honest thin spots
- MasterName null on all curated occurrences: these are later hall-talk/commentarial passages *about* Deshan and Linji, not first-person sayings by them; the originating masters are captured as RelatedMasters instead.
- 德山棒 / 臨濟喝 as standalone terms were not separately concordanced here (flagged as RelatedTerms / future entries).

## Attribution/depth remediation (2026-07-13)

- Rebuilt the attribution layer: 11 occurrences now carry exact speaker and source-title notes. Source-attested names not yet on the roster were preserved for the separate roster-expansion pass.
- Expanded from five to ten exact-headword anchors across ten texts. The eleventh witness, Tianran Hanshi's `一棒一喝`, is explicitly marked `EvidenceRole: family` and does not count toward the exact-headword floor.
- The enrichment deliberately includes positive formulas, mechanical imitation, correction, rebuke, and named collocations. All Chinese evidence in Explanation/Note is anchored and every KWIC/range passes `zc.verify`.
- Definition/item-8 retest: the compound consistently names the paired blows-and-shouts repertoire. Praise, criticism, and correction evaluate the same referent; master attribution belongs to the components and does not create a second compound sense.

## Independent semantic cross-check — 2026-07-13

- Kept one sense and replaced the fused PreferredTarget with the directly distinguishable `stick-blows and shouts`. All eleven anchors and the exact/supporting distinction remain unchanged and verified.

## 2026-07-14 semantic remediation

- Research route: `zc_batch.py count`, `indexed_kwic.py`, and exact XML verification. Website-v3 cross-check was unavailable because Node is not installed in this shell.
- Counts: 棒喝 992 / 232; 棒喝者 7 / 6; 所謂棒喝 2 / 2; 謂之棒喝 1 / 1; 名為棒喝 1 / 1; 喚作棒喝 23 / 13; 何謂棒喝 and 如何是棒喝 0. Signature families: 德山棒臨濟喝 27 / 10; 棒喝交馳 185 / 110; 棒喝齊施 33 / 19; 棒喝門庭 6 / 6.
- inherited research decision: **keep with revision**. One fixed compound remains correct, but the opening now pictures the paired actions and foregrounds both the Deshan/Linji house association and the corpus's self-criticism.
- ordinary scene: one person strikes with a stick or delivers a blow and shouts. The countable `一棒一喝` family confirms actions rather than two abstract doctrines.
- Chan bend: the pair becomes a named encounter repertoire and house-style formula, but masters also rebuke labeling, mechanical imitation, exclusive specialization, and indiscriminate use.
- incompatible-frame audit: verbs and predicates include enact/apply, fly back and forth, cross together, recognize/call, rebuke, and name a person of the repertoire. They all concern the same paired practice/repertoire; appraisal polarity and agent nominalization do not create different things.
- component audit: bare 棒 itself distinguishes implement and blow, but within this fixed compound the attested frames pair the blow/stick action with 喝. 德山棒 and 臨濟喝 retain their own component histories and cannot create two senses of the compound.
- nested-family audit: 棒喝交馳, 棒喝齊施, 棒喝交加, 棒喝門庭, 棒喝人, and 謬行棒喝者 were checked as collocations and role constructions. None supplies a separate named work, person, or object.
- family propagation: the clarified action/repertoire opening remains compatible with 德山棒, 臨濟喝, 喝, and the independently split 棒 entry; it does not collapse bare stick and blow senses back together.
- modifier verdict: not applicable.

feedback-inference-verdict: **licensed** — direct Deshan/Linji equations and action frames license the paired stick-blow/shout repertoire; repeated warning frames license the narrower statement that naming or imitating the repertoire is explicitly contested.

feedback-observations: B/B27/B27n0152.xml@0552a19–20 gives the paired gate formula; J/J28/J28nB202.xml@0088c03 and J/J20/J20nB098.xml@0516b15–16 show followers and blows/shouts in motion; J/J26/J26nB185.xml@0583a26–27, J/J27/J27nB198.xml@0449c05–06, J/J39/J39nB447.xml@0417b06–09, J/J36/J36nB359.xml@0632c11–12, and X/X72/X72n1435.xml@0327b10–12 supply labeling, imitation, correction, and rebuke.

feedback-falsification-searches: all definition/naming formulas above; origin-family 德山棒 and 臨濟喝; action/count family 一棒一喝; motion frames 棒喝交馳 and 棒喝交加; joint-application frame 棒喝齊施; institutional frame 棒喝門庭; agent frames 棒喝者 and 棒喝人; warning frames 喚作棒喝, 專事棒喝, 謬認棒喝, and 亂施棒喝.

feedback-counterexamples: passages that say calling an action “stick-and-shout” leads astray and that rebuke mechanical practitioners prevent defining the term as automatically effective or uniformly approved. They evaluate the same repertoire and do not establish another referent.

feedback-scope: corpus-wide fixed compound; Deshan/Linji origin attribution belongs to the components, while praise, use, correction, and criticism span later named records.

opening-interpretation-verdict: **KEEP AFTER REVISION** — the opening now identifies the visible paired actions, their characteristic Chan house deployment, and the attested self-critical limit before examples.

lookup-probes: stick-blows and shouts; blows and shouts; stick and shout; beating and shouting; Deshan's stick and Linji's shout.

- `SearchAliases`: `blows and shouts`, `stick and shout`, `beating and shouting`, `Deshan's stick and Linji's shout`.
- sense-target-distinguishability: not applicable; origin attribution, positive use, and criticism all concern one compound repertoire.
- Final cohort gate: `run_cohort_gate.py t_0f97bfab265c` returned `hardPass: true`; exact KWIC 11/11, attribution failures 0, public-feedback flags 0, depth/sense hard failures 0, forbidden-English matches 0. Report: `maintenance/semantic-cohorts/semantic-r001-owner1-banghe-gate.json`.
