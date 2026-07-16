# Agent report: b018 Batch A

Completed only the five assigned entries:

| Term ID | Headword | Preferred target | Corpus count | Curated occurrences |
|---|---|---|---:|---:|
| `t_cf07831c1f12` | 赤灑灑 | bare and uncovered | 368 hits / 143 files | 6 |
| `t_f7aa7ea86229` | 落空 | fall into blankness | 325 hits / 128 files | 5 |
| `t_0c86700b60cb` | 頓漸 | sudden and gradual | 275 hits / 104 files | 5 |
| `t_2b0b654aab0d` | 玄路 | the hidden road | 268 hits / 116 files | 6 |
| `t_560356022866` | 孤明 | solitary brightness | 243 hits / 112 files | 6 |

## Evidence and depth

- 赤灑灑: retained its repeated predicates—without covering, nothing to grasp, fully exposed—plus its pairing with clean nakedness and two explicit qualifications that bareness is not conclusive.
- 落空: retained accusations, Dahui's question about the one who fears blankness, the reply that falling cannot be established when blankness is absent, and the expanded form "fall into blank annihilation" (落空亡).
- 頓漸: placed the Platform Record's literal correction at the center: the teaching has no sudden and gradual; seeing has slowness and speed; people are sharp and dull. Later combinations were reported as textual combinations only, with no staged hierarchy asserted.
- 玄路: retained the three-road grouping, direct questions and replies, the golden-lock hidden road verse and prose explanation, and the explicit "still halfway" qualification.
- 孤明: retained affirmative uses from Hongzhi, Linji, and Dahui together with direct corrections that it is not yet news of arriving home and that assigning a fixed name to it is rejected.

All explanations are English-first; quoted Chinese appears only after its English rendering in parentheses. Every sense is corpus-wide and therefore has a null `SenseKey` and null governing `MasterName`; master names occur only where the source attribution is secure.

## Final gates

- JSON parsing: 5/5 passed.
- Exact contiguous `zc.verify`: 28/28 returned `ok == True`.
- Exact `FromLb` and `ToLb`: 28/28 matched the values returned by `zc.verify`.
- Exact headword present in every KWIC: 28/28.
- Prohibited imported-framing scan over the five entry JSON files: no hits after final cleanup.
- Files touched: only the five assigned `entry.v2.json` files, their five `WORK.md` files, and this report.

