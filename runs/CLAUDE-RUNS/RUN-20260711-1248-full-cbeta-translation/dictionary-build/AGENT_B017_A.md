# Agent report — b017 Batch A

Built only the five assigned entries and their durable `WORK.md` ledgers. No status, manifest, plan, guide, termbase, translated XML, or other term was touched.

| Term | Live count | Senses | Validation | Occurrences | Sources | Depth harvest |
|---|---:|---:|---|---:|---:|---|
| 出身處 | 620 / 162 | 1 | multi-source | 6 | 5 | direct one-word answer; question forms; line/answer predicate; have/lack contrast; geographic wordplay; route variants |
| 覷破 | 509 / 154 | 1 | multi-source | 6 | 6 | direct assertions; active/passive syntax; objects; resultative and emphatic forms; case comments |
| 觀心 | 474 / 84 | 1 | multi-source | 7 | 4 | Daoxin-Farong challenge; two direct treatise definitions; correct-inspection formula; three explicit criticisms; concentration warning |
| 傳衣 | 430 / 162 | 2 | multi-source + provisional | 8 | 5 | Hongren's trust/contention/stopping definition; transfer narratives; later question/criticism; distinct single-record robe-distribution stage direction |
| 杜撰 | 381 / 145 | 1 | multi-source | 6 | 4 | direct naming formula; surname/name wordplay; Dahui self-application; personal and speech verdicts; Du Mo false-etymology exclusion |

## Final QA

- All five `entry.v2.json` files parse and retain their exact assigned IDs.
- Live allowlist counts were re-derived with `zc.count` and match the table.
- All 33 curated KWICs return `zc.verify(...).ok == True`; the final saved bounds exactly match the verifier after the one last 覷破 synchronization.
- Every KWIC contains its exact uninterrupted headword, and every listed source text is occurrence-attested.
- Governing heads and titles were inspected for all 33 occurrences. Non-null master links use roster spellings; raised, narrated, two-speaker, or unsafe later witnesses remain null.
- All six senses are corpus-wide null keys. 傳衣's later robe-distribution use is provisional because all seven hits are in one record.
- Final entry-prose scan found zero imported-framing terms and zero bare untranslated Chinese outside translated parenthetical evidence.

## Rule-sensitive decisions

- 觀心 is rendered literally as “inspect mind.” Its affirmative definitions and explicit criticisms are attributed to their texts; none is converted into an imported quiet-attention category.
- 傳衣 translates 法 by function as “teaching,” never as an unexplained religious loan.
- 覷得破 and the negative/positive 覷不破/覷得破 contrast remain in prose as verified morphology, but were not retained as KWICs because neither contains the uninterrupted headword 覷破.
- 杜默 was checked and excluded as a source for 杜撰's meaning: the allowlist hits refer to a named monk or to silence and contain no derivation of the headword.
