# Agent report — b012 Batch A

Built only the five assigned entries and their `WORK.md` inventories. No status, manifest, wave-plan, guide, termbase, other term, or translated-text file was touched.

| Term | Corpus count | Senses | Validation | Curated occurrences | Sources | High-value harvest |
|---|---:|---:|---|---:|---:|---|
| 老僧 | 11,424 / 388 | 2 | multi-source ×2 | 6 | 5 | first-person and third-person grammar; pronoun alternation; sole “is termed” gloss; raised encounter; verse |
| 桶底脫 | 109 / 75 | 1 | multi-source | 5 | 3 | Xuefeng/Yantou case; Yuanwu comparisons; buddha-answer; literal noodle-bucket control; mechanisms-exhausted collocation |
| 擒縱 | 107 / 73 | 1 | multi-source | 5 | 4 | capture/release opposition; kill/bring-to-life, roll/fold, gather/release strings; humans-and-gods object |
| 黑漆桶 | 105 / 63 | 1 | multi-source | 6 | 4 | break/inside/bottom/hoop morphology; people-label; direct answer; self-comparison; leaping image |
| 迴光返照 | 50 / 35 | 1 | multi-source | 6 | 3 | literal imperative; body-and-mind object; Shitou return; Linji no-other-search clause; heel-search; present-and-past verse; full variant census |

## Mechanical and schema QA target

- Total curated occurrences drafted: 28.
- Every candidate saved here had already returned `zc.verify(...).ok == True` with the stated line bounds and contained its headword during the batched research pass.
- Sense keys are null: the senses are shared corpus usages. 老僧 has two null senses because its speaker self-reference and third-person noun phrase are grammatically distinct corpus-wide uses.
- English precedes and translates every Chinese phrase in prose. Bare Chinese remains in `Kwic` or as parenthetical evidence beside English.
- Roster names are used only for exact safe links; raised cases, quoted poems, uncertain local masters, and unrostered writers remain null with explicit reasons.
- Each `WORK.md` contains the seven-form search, deployment inventory, variants/contrasts, spread, and final omission audit.
- Final batched integration check: 5/5 JSON files parsed, 28/28 KWICs reverified with matching saved bounds and headwords, every `SourceTexts` item attested its headword, every linked master matched the exact roster, and the hard imported-framing scan returned zero hits.

## Unresolved ambiguity

- The unique later gloss calling a “self before the empty eon” an “old monk” is preserved as a local textual gloss but not promoted to a third corpus-wide sense.
- The assigned 迴 form is less frequent than the 回 spelling. The assigned headword is retained for ID/headword integrity, while all graph variants and counts are documented.
- Several lamp-record speakers have explicit Chinese section names but no safe exact project-roster link; their occurrence `MasterName` values remain null.
