# Agent report — b016 Batch A

Built only the five assigned entries and finalized their research inventories.

| Term | Corpus count | Senses | Occurrences | Sources | High-value harvest |
|---|---:|---:|---:|---:|---|
| one eye (一隻眼) | 992 / 233 | 1 | 5 | 3 | possess/place/lose/exchange/open morphology; Damei loss/exchange case; Xuefeng whole-earth saying; ahead/not-behind appraisal |
| approve and certify (印可) | 761 / 173 | 1 | 5 | 3 | approval with explicit appraisal; six approvals; two-teacher approval; post-approval questioning; approving-teacher versus succession distinction |
| transmit singly (單傳) | 692 / 211 | 1 | 5 | 4 | twelve naming formulas; Zhongfeng direct definition and “transmit what?” question; direct-pointing, mind-seal, and outside-teachings compounds |
| wooden buddha (木佛) | 663 / 181 | 1 | 5 | 3 | two-part Danxia case; relic exchange; gold/clay/wood comparison; two independent eyebrow-question answers; ten direct questions |
| entrust the teaching (付法) | 608 / 143 | 1 | 5 | 5 | entrustment to Kasyapa; transmission-verse label and historical challenge; has/no-teaching verse; narrative closure; recipient ranking |

## Final QA

- 5/5 JSON files parsed with matching directory IDs and the required PascalCase schema.
- 25/25 curated KWICs returned `zc.verify(...).ok == True`, matched their saved primary-edition line bounds, and contained the assigned headword.
- Every `SourceTexts` path is allowlisted and attests its headword. Occurrence links are conservatively null; speaker, quotation, case, and structural provenance remains explicit in English attribution notes.
- Strict conformance scan returned zero imported-framing or interpretation flags.
- Strict English-first scan returned zero Chinese runs outside parentheses in prose.
- All five senses are shared corpus uses and therefore have null sense keys; no historical origin or approving teacher was mis-keyed as a separate meaning.
- The graph 法 is translated by context as “teaching” throughout 付法; no unexplained loan remains. No occurrence required a 三昧 gloss.

Only the five assigned `entry.v2.json`, five `WORK.md`, and this report were written. No status, manifest, plan, guide, termbase, other term, or XML file was touched; no merge was run.
