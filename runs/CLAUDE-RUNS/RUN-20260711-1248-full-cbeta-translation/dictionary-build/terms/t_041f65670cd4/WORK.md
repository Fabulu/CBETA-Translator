# WORK — 無心 (t_041f65670cd4)

## Concordance (Zen allowlist only)
- 無心 total ≈ 4,182 hits across 370 allowlist texts.
- Stock phrase 無心是道 ("no-mind is the Way") = 86 hits across **33 allowlist texts** (X81n1571, X80n1568, X80n1565, B25n0144, X85n1593, X79n1557, T51n2076 …). Strongly multi-source.
- Huangbo: 傳心法要 T48n2012A = 23 hits (incl. the flat definition); 宛陵錄 T48n2012B = 8 hits incl. 無心是道.
- Note: Platform-Sutra texts T48n2007 has 0, T48n2008 has 4; primary 無心 doctrine locus is Huangbo, with 本淨 (Huineng lineage) as early independent witness.

## Sense analysis
One corpus-wide sense (SenseKey=null): **"no-mind"** — the mind free of grasping/deliberation/discrimination, NOT blankness. Grounding:
- Huangbo's definition: 無心者無一切心也 (no mind of any kind at all); 但無生心動念有無長短彼我能所等心.
- As whole of practice: 但能無心便是究竟 / 若不直下無心，累劫修行終不成道.
- Self-negating guard against reification: 心自無心，亦無無心者；將心無心，心却成有.
- Named as the Way: 無心是道 (33 texts); paired 即心是佛。無心是道 (Huangbo 宛陵錄; 本淨 in 景德傳燈錄).

Considered a Huangbo master-specific sense; rejected — Huangbo does not *bend* the word to a private meaning (unlike Nanquan's buffalo), he articulates the shared corpus sense most fully. Kept as one corpus-wide sense with Huangbo as locus classicus.

Boundary caveat recorded in Note: plain-language 無心 = "unintentionally / with no intent" (有心/無心) is a different, non-doctrinal sense and excluded.

## Speaker attribution — how confirmed
- **T48n2012A** = 黃檗山斷際禪師傳心法要 (title No. 2012A); **T48n2012B** = 黃檗斷際禪師宛陵錄 (title No. 2012B). Both are Huangbo Xiyun (黃檗希運 in master-dates.json). The three 傳心法要 occurrences and the 宛陵錄 即心是佛。無心是道 are Huangbo's own words (師云…).
- **T51n2076** = 景德傳燈錄. Read the section head: last mulu before the passage = 司空山本淨禪師; narrative frame 天寶三年…楊光庭入山…師曰。若欲求佛即心是佛。若欲會道無心是道。 → speaker is **司空山本淨** (Sikongshan Benjing), a disciple of the Sixth Patriarch Huineng (法嗣 chain 忍大師…六祖). Independent, early witness. (Benjing/Huineng not in master-dates.json's 301, so RelatedMasters lists only 黃檗希運; Benjing named in the occurrence AttributionNote.)

## KWIC verification
All 5 KWICs confirmed exact contiguous tag-stripped substrings (scripted PASS; tags + layout newlines removed). FromLb = nearest preceding canon-edition (T) `<lb>`.

## Multi-source verdict
**multi-source.** Sense holds across Huangbo (2 independent texts) + Benjing/景德傳燈錄 + 無心是道 in 33 allowlist texts. Consistent across the corpus and ~1000 years.

## GATE 2 (Claude adversarial verify+repair)
- KWIC exactness: all 5 KWICs EXACT contiguous tag-stripped substrings (punctuation-preserving check). Zero ellipsis/alteration.
- Allowlist: T48n2012A, T48n2012B, T51n2076, T48n2016, X68n1319 all IN zen-corpus.json. No contamination.
- FromLb: all confirmed nearest preceding <lb n> (ed="T"); T51n2076 occ[4] = 0242b27 confirmed at <lb n="0242b27" ed="T"/>其議云何。師曰。若欲求佛即心是佛…
- Attribution: occ[0-2] Huangbo (傳心法要 = 斷際心要 / 河東裴休集 byline; Huangbo's discourse) OK; occ[3] Huangbo (宛陵錄) OK; occ[4] 司空山本淨 CONFIRMED at section head — raw context: 司空山本淨禪師者絳州人也…唐天寶三年玄宗遣中使楊光庭入山…師曰。若欲求佛即心是佛。若欲會道無心是道。 The KWIC is entirely the master's (師曰) answer, single speaker. Correct.
- Multi-source: Huangbo (2 independent texts) + Benjing/景德傳燈錄 + 無心是道 across 33 texts. Confirmed.
- Over-read check: explanation is deflationary/self-cancelling (亦無無心者), grounded in Huangbo's Chinese; no imported mystical Absolute. Good.
- RelatedTerms (無心是道/即心是佛/無念/平常心/道): 無心是道 & 道 are genuine constituents; others deliberate semantic links. Kept.
- VERDICT: verified. No repairs needed.

## Gate-3 REVISE fix — 2026-07-11 19:10
Fixed Explanation final sentence (debate conflation) per GATE3_VERDICT.md. Verified against T/T51/T51n2076.xml:
- Exchange 1: 遠禪師 (Chan-master Yuan, intro'd @0242c06 時有遠禪師者) charges self-contradiction @0242c12-13 (適言無心是道。今又言身心本來是道。豈不相違); Benjing answers @0242c14 (無心是道心泯道無。心道一如故言無心是道).
- Exchange 2: 志明禪師 @0243a01 (若言無心是道。瓦礫無心亦應是道); Benjing answers by EMBRACING sameness @0243a06 (窮本不有何處存心。焉得不同草木瓦礫), then 志明杜口而退 @0243a07 — NOT the 心泯道無 formula.
Rewrote sentence to separate the two exchanges + two objectors and correct the polarity of Benjing's reply to the tiles objection (acceptance, not rebuttal). No Kwic changed; all 5 KWICs verbatim. STATUS=verified.

## D003-C depth, family, and item-8 reopening

- Refreshed concordance: 4,331 hits / 370 files; final 9 anchors across 7 occurrence source texts. Six anchors establish the primary Chan-loaded sense, including Huangbo's flat definition, self-negation, slogan, Benjing's independent answer, and Dahui's previously unanchored full direct definition. Three independent sources establish ordinary lack of intent.
- Definition harvest: retained Huangbo's “no-mind means no mind of any kind” and added the exact Dahui definition that the prose already relied on: not insensate earth, wood, tile, or stone; on encountering conditions, settled and grasping nothing. Both are attributed rather than harmonized by the annotator.
- `sense-target-distinguishability: KEEP` — pair 1, **no-mind** names the term Huangbo, Dahui, and Benjing define and debate; pair 2, **without intending to** modifies an act or correspondence whose intent is absent. Unintentional sexual misconduct/killing, unintentionally planting willows, and an unplanned textual coincidence cannot be translated as Huangbo's “no-mind,” while Huangbo's definition cannot be translated as merely accidental action.
- Family/definition retest: `無心是道` (“no-mind is the Way”), `無心道人` (“no-mind man of the Way”), `有心` (“with intent”), and `無心插柳` (“unintentionally plant willows”) separate the two families cleanly. Huangbo's and Dahui's definitions remain mutually compatible as attributed corpus statements; the new ordinary evidence invalidates the former exclusion but does not weaken the primary definition.
- #0g retest: the primary sense foregrounds direct Chan definitions, public debate, self-negation, and Dahui's criticism of merely verbal no-mind. The ordinary sense preserves precept-case intent language and prevents Zen-loaded translation from swallowing plain narrative grammar.
- Omission audit: the previously prose-only Dahui definition is now anchored. The three ordinary deployment classes—rule-violation narrative, proverb, and authorship disclaimer—are all anchored; no duplicate slogan witnesses were added.

## Attribution remediation — original-606 batch

- Before: 9 occurrences; named 5, null 4; all 9 notes lacked canonical speakers, 6 lacked exact titles, 1 vague attributor, and 48 Chinese evidence strings dangled.
- After: 17 occurrences, all named; all 17 notes name exact title and speaker; all 28 retained evidence strings are anchored. Added verified witnesses for Huangbo's offering aphorism, Dahui's true-no-mind continuation, Huineng's teaching-eye answer, both Sikongshan Benjing debates, Luopu's test-question, Falan Yuancheng's gruel answer, and Yulin Tongxiu's barrier verse.
- Unfindable quotation: the former exact-looking string `但無生心動念有無長短彼我能所等心` is a composite paraphrase of Huangbo's surrounding definition, not the contiguous wording of the saved source. It is no longer presented as a Chinese quotation; Huangbo's exact attested formulations remain anchored.
- Roster gaps: Sikongshan Benjing (3 occurrences), Falan Yuancheng, Meixi Fudu, and Chuiwan Guangzhen are absent from the roster (6 audit failures).
- Definition/item-8 retest: the enlarged evidence strengthens the split between Chan no-mind and ordinary lack of intent; no third referent emerged.

## Independent peer repair — 2026-07-13

- Rewrote the primary explanation and note English-first while preserving every anchored Huangbo, Dahui, Huineng, Sikongshan Benjing, Falan, and Yulin claim.
- Removed the inference that no-mind “remains responsive”; the entry now reports the observable fact that records define, debate, question, and answer about it.
- `sense-target-distinguishability: KEEP` — “no-mind” names the state/domain explicitly defined and debated in the primary witnesses; “without intending to” marks the absent intent of an act or correspondence. The latter is described as a distinct lexical use, not misleadingly as a second physical referent.
- All seventeen exact-headword witnesses remain assigned to the same two senses as before; no support-only occurrence and no third thing appeared.

## 2026-07-14 semantic remediation

- Research route: `zc_batch.py count`, `indexed_kwic.py`, and exact XML verification. Website-v3 cross-check was unavailable because Node is not installed in this shell.
- Counts: 無心 4,331 / 370; 無心者 65 / 26; 所謂無心 2 / 2; 謂之無心 3 / 3; 名為無心 2 / 2; 喚作無心 5 / 4; 何謂無心 2 / 2; 如何是無心 20 / 17; 無心道人 122 / 57; 無心是道 94 / 34; 無心於事 48 / 36.
- inherited research decision: **keep the two-sense split and revise the opening**. The primary graph-first sentence was replaced by the corpus-earned tension between defined no-mind, self-negation, non-inertness, public testing, and correction.
- ordinary bridge: 無心 can predicate a person/state as having no mind, or adverbially mark an act as lacking intention. English cannot cover those two grammatical/referential uses with one precise target without blurring Huangbo's definition into accidental action.
- incompatible-frame audit: the primary family takes equations, definitions, a named 無心道人, offering comparisons, and disputes over 無心是道; the ordinary family modifies violations, planting, and textual coincidence. These require the retained split.
- counter-deployment audit: Dahui Zonggao rejects insensate earth/wood/tile/stone blankness, Huangbo Xiyun rejects manufacturing no-mind with mind, and Yulin Tongxiu says no-mind still faces barriers. Those narrow the primary sense rather than creating hostile senses.
- nested/family audit: 無心道人, 無心是道, 無心境界, 無心於事, 無心插柳, and 有心/無心 contrasts were classified by frame. Compounds and sayings do not lend their whole referents to bare 無心, but they expose the two lexical families.
- family propagation: the `平常心` correction remains compatible; `無念`, `道`, `即心是佛`, `有心`, and `無心插柳` retain independent entries or leads and should not inherit “no-mind” indiscriminately.
- modifier verdict: not applicable.

feedback-inference-verdict: **licensed** for the primary sense and **direct** for lack of intent. Huangbo's and Dahui's explicit definitions license the no-mind/non-inertness opening; violation, proverb, and authorship frames directly license “without intending to.”

feedback-observations: T/T48/T48n2012A.xml@0380a17, @0380b02–04, and @0380b12–14 define and self-negate no-mind; T/T47/T47n1998A.xml@0890c25–0891a02 distinguishes it from inertness and mere speech; T/T51/T51n2076.xml@0242c06–0243a07 debates no-mind and the Way; B/B27/B27n0152.xml@0550b02–03 supplies the barrier correction. J/J39/J39nB447.xml@0391c23–24, J/J29/J29nB239.xml@0498c06–08, and X/X68/X68n1319.xml@0553b04–06 supply three direct absent-intent frames.

feedback-falsification-searches: all definition/naming formulas above; 無心道人 and offering frames; 無心是道 equations and objections; inertness controls 土木瓦石 and 頑然無知; action frame 無心於事; intentionality contrasts 有心/無心; ordinary families 無心插柳, unintentional violations, and 無心而自合; question frames 如何是無心 and 何謂無心.

feedback-counterexamples: inert matter prevents defining primary no-mind as simple unawareness; the barrier verse and critiques prevent treating its slogans as final formulas. Unintentional violations, planting, and coincidence cannot mean the defined no-mind and therefore preserve the second sense.

feedback-scope: primary Chan-loaded sense is corpus-wide and directly defined across Huangbo Xiyun, Dahui Zonggao, Sikongshan Benjing, and later tests; ordinary lack of intent is corpus-wide lexical grammar in three independent deployment classes.

opening-interpretation-verdict: **KEEP AFTER REVISION** — the primary opening now states the direct definitions, non-inertness, self-negation, public testing, and corrective tension before quotations; the ordinary opening already states its referent and frames.

sense-target-distinguishability: **KEEP** — `no-mind` names the state/person/domain explicitly defined and debated; `without intending to` modifies an act or correspondence whose intention is absent.

lookup-probes: no-mind; no mind; without mind; no-mind person; having no mind; unintentionally; without intending; without intent; by accident.

- Primary `SearchAliases`: `no mind`, `without mind`, `no-mind person`, `having no mind`.
- Ordinary `SearchAliases`: `unintentionally`, `without intending`, `without intent`, `by accident`.
- Final cohort gate: `run_cohort_gate.py t_041f65670cd4` returned `hardPass: true`; exact KWIC 17/17, attribution failures 0, public-feedback flags 0, depth/sense hard failures 0, forbidden-English matches 0. Report: `maintenance/semantic-cohorts/semantic-r001-owner1-wuxin-gate.json`.
