# b010 Batch B research report

Built only the five assigned entries and their research ledgers. All five JSON files parse. Final batch verification is 24/24 exact KWICs with `zc.verify(...).ok == True`, synchronized primary-edition line anchors, headword-bearing KWICs, allowlisted source paths, and exact roster values wherever a master link is set.

## Tight barrier (牢關) — `t_ffb0ee18f1a2`

- Concordance: 594 occurrences in 194 allowlisted texts.
- Structure: 1 corpus-wide, multi-source sense; preferred target “the tight barrier.”
- Curated evidence: 3 verified occurrences in the records of Yuanwu, Dahui, and Mi'an; 3/3 verified. Source inventory additionally includes the Luopu section of the Compendium of Five Lamps and the sole direct “what is the tight barrier?” exchange.
- Depth harvest: the transmitted “last phrase first reaches the tight barrier” formulation; “what is it?” question-answer; break/pass/get-through/hold/close deployment family; six counted collocations.
- Definition search: 1 apparent “tight barrier is...” hit was grammatical spillover, 1 late “called the tight barrier” hit was a criticism of obstructive teaching conduct, and 1 direct question-answer was found. Both exclusions are recorded in `WORK.md`.
- Attribution: later quotations of Luopu's wording remain unlinked rather than being reassigned to the quoting master.

## Dead water (死水) — `t_49704a7bbd4d`

- Concordance: 535 occurrences in 180 allowlisted texts.
- Structure: 1 corpus-wide, multi-source sense; preferred target “dead water.”
- Curated evidence: 5 verified occurrences in five texts; 5/5 verified. Linked masters are Shoushan Xingnian, Liangshan Yuanguan, Yuanwu Keqin, and Xueyan Zuqin.
- Depth harvest: “dead water does not conceal a dragon”; explicit contrast with “living-water dragon”; Yuanwu's living-water/dead-water contrast; Ying'an's equivalence list; Xueyan's “dead water soaking a stone” description.
- Counted morphology: proverb 80; livelihood in dead water 12; dead water soaking a stone 3; living-water dragon 26. The tempting exact compound “withered tree, dead water” returned zero and was not fabricated.
- Definition search: all hits for “called/named dead water” were inspected and the two independently informative formulations were included.

## Raise the whisk (竪拂) — `t_df3e128ab4c1`

- Concordance: seeded spelling 471 occurrences in 90 allowlisted texts.
- Structure: 1 corpus-wide, multi-source sense; preferred target “raise the whisk.”
- Curated evidence: 5 verified occurrences in five texts; 5/5 verified. They cover an answer/demonstration, repeated action, Zhaozhou's response, a raised Zifu case, and explicit later classification as a dead phrase.
- Depth harvest: no direct definition formula exists; the records supply actions and questions about their import but no single gloss. The entry states that absence.
- Variants/collocations: dominant spelling “raise the whisk” (豎拂), 2,438; “raise the fly-whisk” with seeded spelling, 189; “raise up the fly-whisk,” 492; “pick up the mallet and raise the whisk,” 108; “raise a finger and raise the whisk,” 4.
- Ambiguity: the assigned ID hashes the seeded spelling with the older “raise” graph (竪), although the variant using the dominant “raise” graph (豎) is far more frequent. The entry retains the assigned SourceTerm for ID stability and documents both English-paired forms. Root may decide later whether wave-wide headword normalization should invoke the guide's dominant-form exception.

## Mighty-Sound King (威音王) — `t_2229af16905a`

- Concordance: 487 occurrences in 171 allowlisted texts.
- Structure: 1 corpus-wide, multi-source proper-name sense; preferred target “Mighty-Sound King.”
- Curated evidence: 6 verified occurrences in six texts; 6/6 verified.
- Depth harvest: Huangbo-Nanquan exchange with surname reply; Yongjia/Xuanze lineage statement; Xuedou's “second phrase” appraisal; Dahui's critical report; the sole direct late self-definition.
- Counted morphology: before Mighty-Sound King 181; after Mighty-Sound King 33; far side of Mighty-Sound King 17; Mighty-Sound King Buddha 80.
- Attribution: both Huangbo-Nanquan witnesses are two-speaker and null-linked; Xuanze speaks inside Yongjia's section and is null-linked; Dahui and Xuedou use exact roster values.
- The Dahui occurrence is explicitly presented as his criticism of a reported equation, never as the entry's definition.

## Living phrase (活句) — `t_372fb5a2b7ce`

- Concordance: 412 occurrences in 128 allowlisted texts.
- Structure: 1 corpus-wide, multi-source sense; preferred target “a living phrase.”
- Curated evidence: 5 verified, headword-bearing occurrences in five texts; 5/5 verified.
- Depth harvest: primary “speech within speech / speech without speech” definition; later eight-part definition; Gulin's net/call/no-place description; Gulin's living/dead reversal; direct “what is it?” answer; instruction contrasted with dead phrase; neither-dead-nor-living third member.
- Definition-formula inventory: 3 “living phrase is...” hits; 1 “so-called”; 2 “called”; 8 “named”; 1 “what is meant by”; 45 “what is” hits across 35 texts. False grammatical hits and an unrelated Zhuangzi discussion are recorded as exclusions.
- Counted morphology: investigate the living phrase 90; under the living phrase 33; dead phrase 372; neither-dead-nor-living phrase 18.
- The instruction “investigate the living phrase” is translated literally as a recorded action and not reclassified.

## Final checks

- JSON parse/schema shape: 5/5 pass; PascalCase fields; directory IDs preserved.
- Occurrences: 24/24 exact, allowlisted, apparatus-excluded, primary-edition anchors synchronized; every KWIC contains its entry headword.
- SourceTexts: every listed path is allowlisted and attests its headword.
- Roster: every non-null `MasterName` and every `RelatedMasters` value matches an exact roster primary name.
- English-first prose: zero Chinese runs outside parentheses in all audited dictionary prose fields.
- Framing scan: zero occurrences of the imported framing and interpretation vocabulary used by the project gate.
- Files written: five `entry.v2.json`, five `WORK.md`, and this report. No status, manifest, wave-plan, guide, termbase, translated XML, or other term file was touched.
