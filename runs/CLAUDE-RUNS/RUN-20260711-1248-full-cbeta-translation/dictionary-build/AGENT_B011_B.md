# b011 Batch B research report

Built only the five assigned entries and their `WORK.md` ledgers. All five entries use one corpus-wide, multi-source sense. Final batch verification: 25/25 exact headword-bearing KWICs return `zc.verify(...).ok == True` with synchronized primary-edition anchors.

## Dead phrase (死句) — `t_207efae5f6bd`

- Concordance: 372 occurrences in 116 allowlisted texts.
- Evidence: 5 verified occurrences in five texts.
- Depth: primary “speech within speech / speech without speech” definition; later “verbal explanation / reasoning track / thought / distinguishing” definition; Blue Cliff instruction and result clauses; direct living/dead question-answer; explicit classification of raised gestures.
- Morphology: investigate dead phrase 82; under dead phrase 29; living phrase 412; neither-dead-nor-living phrase 18.
- Self-definition search: 2 “called” hits, 9 “named” hits, and 26 “what is” hits across 23 texts were inspected. Gulin's explicit living/dead reversal is preserved.

## On the lump of red flesh (赤肉團上) — `t_bbee6625a4d5`

- Concordance: 316 occurrences in 144 allowlisted texts; broader “lump of red flesh,” 441.
- Evidence: 5 verified occurrences in four texts.
- Depth: Linji's “true person of no rank” saying in his own record and a lamp witness; Nanyuan Huiyong's distinct “wall stands a thousand fathoms” saying in two witnesses; Dahui's later recombination.
- Variants: “beside the lump of red flesh,” 36; no exact occurrence using “inside” in place of “on.”
- Attribution: Linji and Dahui use exact roster links; Nanyuan remains null. The two public-record saying families are not cross-attributed.

## Pass through the barrier (透關) — `t_02d93ab1ca2e`

- Concordance: 266 occurrences in 112 allowlisted texts.
- Evidence: 4 verified occurrences in four texts.
- Depth: Huanglong retrospective defining passed versus unpassed by relation to the gatekeeper; Zhongfeng's correction distinguishing case-stringing from the tight barrier underfoot; Yuanwu's assembly challenge; Chijue Daochong exchange.
- Morphology: eye that passes the barrier 97; phrase that passes the barrier 1; already-passed person 12; not-yet-passed person 14. Broader non-headword verbs are separately counted and not folded into the entry count.
- Formula harvest: 30 apparent person-form hits, 1 “called,” 1 “named,” 3 “call it,” and 13 “what is” hits across 12 texts.

## The road of language is cut off (言語道斷) — `t_07d808115439`

- Concordance: 171 occurrences in 71 allowlisted texts.
- Evidence: 5 verified occurrences in four curated files, with the lamp-record correction attested in a fifth SourceText.
- Depth: Trust in Mind verse; recurrent pairing with “place of mental activity extinguished”; explicit rejection of shut eyes/darkness; Hongzhi's direct question; Dahui's quoted correspondent; immediate counter-formulation retaining the road cut off while changing mental activity to “not extinguished.”
- Morphology: paired phrase 128; exact unpunctuated full pair 12; punctuated pair 17; shut-eye collocation 29; “no words to say” 19.
- Attribution: Hongzhi links exactly; quoted verse and correspondent speech remain null.

## Appraise and comment (評唱) — `t_10ca0857a11b`

- Concordance: 103 occurrences in 28 allowlisted texts.
- Evidence: 6 verified occurrences in six texts.
- Depth: Yuanwu/Blue Cliff title construction; Wansong/Book of Serenity; Linquan's Empty Valley and Empty Hall titles; explicit sequence from recorded encounters to verses to appraisal-commentary collections; Gu Xuezhe's critical poem heading.
- Deployment: verb, authorship construction, title, resulting collection noun, historical sequence, and critical object.
- Collocations: exact adjacent “appraise/comment on verses on old cases” 1; “appraisal-commentary on public cases” 3; “appraisal-commentary words” 1. No direct lexical self-definition was found; explicit titles provide the structural definition.

## Final QA

- JSON parse/schema/PascalCase: 5/5 pass; directory IDs preserved.
- Occurrences: 25/25 exact, allowlisted, headword-bearing, and line-synchronized.
- SourceTexts: every path is allowlisted and attests the headword.
- Roster: every non-null occurrence/sense relation uses an exact primary roster name.
- English-first: zero Chinese runs outside parentheses in dictionary prose fields.
- Corpus prose check: every multi-graph Chinese evidence span in the entry prose has a positive allowlisted `zc.count`; negative-search results are described only in English.
- Framing scan: zero banned imported framing or interpretation vocabulary.
- Unresolved ambiguity: none affecting sense structure. Unrostered or structurally multi-speaker witnesses remain null rather than receiving guessed links.
- Files written: five `entry.v2.json`, five `WORK.md`, and this report only. No statuses, manifests, plans, guides, termbase files, other terms, or translations were touched.
