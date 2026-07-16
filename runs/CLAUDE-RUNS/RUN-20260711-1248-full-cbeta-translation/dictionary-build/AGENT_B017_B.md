# b017 Batch B report

Completed five assigned entries from scratch with the current guide and `zc.py` workflow:

| Term | ID | Senses | Occurrences | Corpus count | Validation |
|---|---|---:|---:|---:|---|
| 敗闕 | t_b8d2633b12ef | 1 | 6 | 594 hits / 193 files | multi-source |
| 衣鉢 | t_6bc71cc88c2f | 2 | 6 | 513 hits / 115 files | multi-source |
| 淨瓶 | t_85eef19d3d3a | 1 | 5 | 487 hits / 143 files | multi-source |
| 芥子 | t_df9aad1ce22d | 1 | 6 | 442 hits / 146 files | multi-source |
| 五家 | t_44bf96cadfe3 | 1 | 6 | 376 hits / 128 files | multi-source |

Depth notes are saved in each term’s `WORK.md`. The five-house entry preserves the exact corpus enumeration, both Zhongfeng self-definitions, the two-branch genealogy, and the attested five-houses/seven-lineages extension without imposing a later classification. One proposed clean-water-bottle span failed exact verification during research and was excluded before drafting.

Final mechanical QA passed: all five JSON files parse; every ID matches the source-term hash; all 29 saved KWICs contain the headword, are allowlist-scoped, return `zc.verify(...).ok == true`, and have synchronized line bounds; every `SourceTexts` list matches its occurrences; and the English prose passed the imported-framing scan.
