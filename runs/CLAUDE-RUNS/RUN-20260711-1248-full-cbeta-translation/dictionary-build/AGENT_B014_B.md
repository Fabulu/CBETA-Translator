# Agent report — b014 Batch B

Built only the five assigned entries and their `WORK.md` files. No status, manifest, plan, guide, termbase, other term, or translated-text file was touched.

| Term | Count | Senses | Validation | Occurrences | Sources | Depth harvest |
|---|---:|---:|---|---:|---:|---|
| 落處 | 2,117 / 310 | 1 | multi-source | 6 | 5 | Xiuyelin relational definition; knowing/not knowing; origin/destination; case and remark bearings |
| 良久曰 | 2,037 / 79 | 1 | multi-source | 6 | 4 | dialogue and assembly sequence; 曰/云; silent interval and no-speech contrasts |
| 宗門 | 1,934 / 285 | 1 | multi-source | 6 | 5 | Yuanwu graph relation; teaching-family contrasts; questions, tests, titles, strong-point formula |
| 僧堂 | 1,743 / 275 | 1 | multi-source | 6 | 4 | bell, interior seating, movement, beds, door, Xuansha's three-place statement |
| 普請 | 1,567 / 282 | 1 | multi-source | 7 | 5 | direct equal-labor definition; placard; fields, mill, hoeing, attendance, tea-picking |

## QA target

- 31 curated, headword-bearing occurrences drafted from `zc`-verified candidates.
- All senses are corpus-wide null keys; no sense was keyed merely to the originator of an occurrence.
- English-first prose translates every quoted Chinese phrase parenthetically and uses Chinese Chan framing only.
- The new 法 rule was applied: no compound was reflexively rendered with the religious loanword; the Yuanwu 宗門 sentence is translated by its actual source and speech relation.
- Every WORK file records definition formulas, deployment shapes, relations, spread, and omission decisions.
- Final integrated QA: 5/5 JSON files parse with deterministic IDs and exact schema keys; all five headline counts match live `zc.count`; 31/31 headword-bearing KWICs return `zc.verify(...).ok == true` with exact saved bounds; every source attests its headword; all master links occur in the roster; and the English-first/Zen-only scan reports zero violations.

## Ambiguities

- 落處 covers literal destination and the recorded landing or bearing of words, cases, actions, and lives. The corpus repeatedly preserves the same falling-place image, so these were not split into speculative abstract senses.
- 宗門 can label the lineage, its affairs, and books collecting its records. Those are one institutional/source-line family rather than unrelated senses.
- 普請 also appears in extended invitations for the whole assembly to look. The direct equal-labor definition and activity predicates establish shared work as primary.
