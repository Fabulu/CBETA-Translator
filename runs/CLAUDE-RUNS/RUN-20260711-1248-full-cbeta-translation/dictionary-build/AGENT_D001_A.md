# d001-A completion report

Repaired only 上堂, 恁麼, 作麼生, 示眾, and 法嗣. No `STATUS`, manifest, termbase, or integration file was changed.

## Result

- 上堂: 2 senses, 10 occurrences. Split the formal teaching-hall address/observance from the physical act of ascending or taking the teaching seat.
- 恁麼: 1 sense, 10 occurrences. Inference, condition, temporal use, understanding, negation, and graphic variation remain one deictic sense.
- 作麼生: 1 sense, 11 occurrences. Added distinct dialogue functions and the attested 作勿生, 作摩生, and 怎麼生 forms without splitting readings.
- 示眾: 2 senses, 10 occurrences. Split verbal public address from physically displaying an object to the assembly.
- 法嗣: 2 senses, 11 occurrences. Split the person-role “lineage heir” from the relation/status “Dharma succession; lineage affiliation.”

## Family and deviation checks

- 上堂 is cross-checked against 小參 and 陞座/升座. Its Zen bend is the lexicalized formal public address and recorded-sayings heading.
- 恁麼 is cross-checked against the separate 恁麼則 entry and the 與麼 variant.
- 作麼生 is cross-checked against 作麼生道; its public-interview demand is described without assigning intent.
- 示眾 is cross-checked against 上堂, 小參, 拈花, and 拂子. The flower defines the invoked Zen Buddha through the flower-sermon deployment; the whisk is recorded as the teaching-seat implement, not as a projected symbol.
- 法嗣 is cross-checked against 嗣法. `得法嗣何人` is treated as succession affiliation, not globally mistranslated as acquiring heirs.

Every retained old occurrence and every new occurrence was re-run through `zc.verify`; all 52 returned `ok: true`, and stored `FromLb`/`ToLb` values matched the verifier. JSON parsing and per-sense occurrence/source consistency were also checked.

## Anti-quota follow-up

- 上堂 was enriched from 10 to 12 with delegated whisk-holding address authority and formal prohibition/resumption of hall addresses. Final split: 11 address/observance anchors, 1 physical-ascent anchor.
- 示眾 was enriched from 10 to 11 with written-verse public instruction after an assembly response. Final split: 7 verbal/public-instruction anchors, 4 physical-display anchors.
- The three added KWICs passed exact `zc.verify` and line-span checks. The existing definitions and sense boundaries still hold; no `STATUS`, manifest, termbase, or merge file was touched.
