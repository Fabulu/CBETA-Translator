# b024 Batch A report

Built the five assigned entries under the full updated guide, including frequency-scaled harvesting, whole-family revalidation, the different-things sense test, and the flyswatter test.

| Term | Corpus count | Senses | Occurrences | Distinct harvest |
|---|---:|---:|---:|---|
| 命根 | 829 / 226 files | 2 | 6 | locative display, root comparison, break, public question, famous quotation, vital faculty |
| 生緣 | 742 / 231 | 3 | 6 | Huanglong barrier, verse, criticism, native place, causal condition |
| 三寸 | 653 / 209 | 4 | 7 | tongue, elliptical speech, hook-distance, thickness, breath |
| 喪身失命 | 645 / 217 | 1 | 6 | tree, dog, snake, appraisal, Linji retrospective, entering-room sword |
| 牧牛 | 559 / 178 | 2 | 6 | exchange, comparison, self-definition, criticism, title sequence |

The enrichment/sense audit produced required splits rather than reading menus: 生緣 has three referents; 三寸 has four; 命根 distinguishes the Chan life-root from the enumerated vital faculty; 牧牛 distinguishes activity from textual title. 喪身失命 remains one idiom across its cases.

Final QA: all five JSON files parse. All 31 occurrences pass exact contiguous `zc.verify`, are allowlisted, and match stored primary-edition `FromLb`/`ToLb`. The final 三寸 entry was specifically rechecked after a cross-batch collision; its final SHA-256 is `18fd6d3e801e2f18b6333b5ed240301754b4537d47632fe539bb89be6bf8191b`. No status, manifest, plan, maintenance, or termbase integration file was touched.
