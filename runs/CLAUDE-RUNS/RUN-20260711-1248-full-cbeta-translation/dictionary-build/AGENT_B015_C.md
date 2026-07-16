# Agent report — b015 Batch C

Built only the five assigned entries and their `WORK.md` files. No status, manifest, plan, guide, termbase, other term, or translated XML was touched.

| Term | Count | Senses | Validation | Occurrences | Sources | Depth harvest |
|---|---:|---:|---|---:|---:|---|
| 宗師 | 1,445 / 257 | 1 | multi-source | 6 | 5 | direct role statement; Dahui criteria; Yuanwu appraisals/enumeration; claimant rebuke |
| 心印 | 1,374 / 280 | 1 | multi-source | 6 | 6 | shape negations; continuity; iron-ox mechanism; three materials; reciprocal sealing; conduct contrast |
| 無住 | 1,252 / 239 raw | 1 | multi-source | 6 | 5 | Huineng definition/contrast; Fayan case; no-root answer; Shenhui reply; Wuzhu-name exclusion |
| 經行 | 1,022 / 273 | 1 | multi-source | 6 | 3 | east-west pacing; health/rest contrast; hall/pines/peak/after-meal walking |
| 定慧 | 962 / 196 | 1 | multi-source | 7 | 5 | Huineng one-body and no-sequence definitions; water/wave; Huangbo; provisional-name correction; direct graph predicates |

## Final QA

- All 5 entry files parse as JSON and retain their exact assigned IDs.
- Live allowlist counts are recorded above: 宗師 1,445 / 257; 心印 1,374 / 280; 無住 1,252 / 239; 經行 1,022 / 273; 定慧 962 / 196.
- All 31 curated occurrences return `zc.verify(...).ok == True`, their saved `FromLb`/`ToLb` values exactly match the verifier, and every KWIC contains its headword.
- Every listed source text is attested by at least one saved occurrence; all master links passed the roster check in the integrated validation pass.
- All five senses are corpus-wide null keys.
- English-first prose translates every Chinese phrase parenthetically.
- No religious loan rendering was used for 法; every occurrence is translated by its sentence as teaching, thing, or all things.
- 經行 remains literal walking/pacing only. 定慧 is rendered from direct Chan definitions as stability and discernment, never as an imported state pair.
- Every WORK file records formula searches, deployment range, relations, spread, and omission decisions.
- Final framing/English scan: zero #0b/#0c errors.

## Ambiguities

- Raw 無住 counts include the monk named Wuzhu; proper-name hits were explicitly excluded from sense evidence.
- Raw 心印 counts include bestowed titles and monk names; those are not treated as a technical sense.
- 宗師 ranges from honorific/appraisal to rebuke of false claimants, but all uses concern the same lineage-master role.
