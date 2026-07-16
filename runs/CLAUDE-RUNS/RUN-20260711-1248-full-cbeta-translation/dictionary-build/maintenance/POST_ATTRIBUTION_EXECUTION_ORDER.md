# Post-attribution execution order

Reconciled read-only on 2026-07-16. This file records the durable work order after the current attribution cross-review closes. It does not modify an entry, promote a review result, merge data, or reorder a queue.

## Authority

For production state, use the frozen fresh-build files, not historical progress prose in `CODEX_RESUME.md` or `CODEX_HANDOFF.md`:

1. `fresh-build/state.json` — corpus invariant.
2. `fresh-build/queue.json` — durable IDs, ordinals, phases, and source provenance.
3. `fresh-build/queue-sources/ORDER.md` plus the immutable source snapshots and `fresh-build/queue-sources/SHA256SUMS` — source precedence and internal order.
4. `DICTIONARY_ENTRY_GUIDE.md` — current semantic, evidence, attribution, inference, depth, and validation gates.
5. `fresh-build/iriya-admission/ledger.json`, `IRIYA_ADJUDICATION_GUIDE.md`, and the revision-3 Iriya snapshots — the final-wave quarantine.

`CODEX_RESUME.md` and `CODEX_HANDOFF.md` remain useful history, but their older merged counts and cancelled/uncancelled expansion directives conflict internally. They do not override the frozen fresh-build state.

## Frozen corpus gate

- Baseline SHA-256: `42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a`.
- Files: 494.
- Independent works: 487.
- Production drafting is forbidden if `fresh-build/state.json`, an entry's `CorpusBaselineSha256`, `zc.py`, or `Assets/Data/zen-corpus.json` disagrees with that baseline.
- Source independence is counted by distinct `work_id`, never file count. Split volumes and duplicate editions do not satisfy the multi-source gate.

The older `ORDER.md` sentence saying 479 files / 473 works and the older Iriya prompt sentence saying 462 texts are stale narrative text; `state.json` and guide §2/§5 establish 494 / 487.

## Directly provable current inventory

From `fresh-build/queue.json` and the live `fresh-build/entries/*/entry.v2.json` tree:

- Queue: 3,967 rows, all with stable unique IDs and no duplicate-pointer rows.
- Entry files present: 1,204.
- Queue rows without an entry file: 2,763.
- Present by phase: core-prebuilt 23/23; core 517/517; requested 110/110; NEXT500 450/500; sayings100 82/100; investigation720 22/720; Iriya 0/1,996; late-requested 0/1.
- Missing by phase: NEXT500 50; sayings100 18; investigation720 698; Iriya 1,996; late-requested 1.
- Earliest physically missing ordinary queue row is ordinal 951, `語句` (`t_2f1483b84db4`), from `NEXT500_BUILD_PLAN.md`.
- The explicit late request is `問答` (`t_47a8c4d45a14`). `fresh-build/queue-sources/LATE_REQUESTED_TERMS.md` says it is appended only to preserve ordinal stability but must be scheduled in the first unassigned construction wave after the active frozen wave.

The queue's `state` fields all still say `pending`; therefore presence of a live entry file plus its review ledgers, not that field, is the only directly provable construction count here. No merged/published count is inferred.

## Durable remaining construction order

After attribution cross-review and promotion are completely closed:

1. **Late explicit public-interview request:** build `問答` first, as directed by `fresh-build/queue-sources/LATE_REQUESTED_TERMS.md`. Its physical ordinal 3967 must not be moved; scheduling priority is metadata, not renumbering.
2. **Finish the NEXT500 gaps:** the 50 missing rows, beginning at ordinal 951 `語句`, preserving `NEXT500_BUILD_PLAN.md` order.
3. **Finish the sayings/trivia gaps:** the 18 missing rows from `NEXT100_BUILD_PLAN.md`, preserving its order and the inherited research leads in `NEXT100_SAYINGS_CANDIDATES.md`.
4. **Finish the investigation backlog:** the 698 missing rows from `RELATED_INVESTIGATION_BACKLOG.md`, preserving its frequency order. Every row receives keep/revise/reject adjudication; a negative result with a reason is durable work.
5. **Iriya semantic admission, then construction:** adjudicate all 2,008 revision-3 source candidates before building any Iriya entry. Only KEEP/PROVISIONAL results surviving deduplication flow into the already queued 1,996 `z*` rows. Construction stays last.

The old `WAVE_PLAN.md` and requested-plan phases are exhausted in the live tree and must not be rebuilt. Their terms remain reference material for deduplication and related-term navigation.

## Iriya hard quarantine

Authoritative files:

- `IRIYA_SAYINGS_QUEUE.md` and `CODEX_IRIYA_QUEUE_PROMPT.md` revision 3: 2,008 source candidates.
- `fresh-build/iriya-admission/ledger.json`: 2,008 candidates, 21 packets, 0 adjudicated at reconciliation time, checkpoint every 50.
- `fresh-build/queue-sources/IRIYA_FINAL_BUILD_PLAN.md`: 1,996 deduplicated queued headwords, all unbuilt.
- `IRIYA_ADJUDICATION_GUIDE.md`: semantic disposition rules.

No Iriya construction may begin until all 2,008 admission rows have exactly one disposition and `validate_iriya_admission.py --build-gate` exits zero. Mechanical cleanliness is not acceptance; exact-form absence is a segmentation signal; `Pair` is the exact-form count while `Anchor` is only an upper bound; one-work evidence is PROVISIONAL, not automatic rejection. Iriya is a headword-selection signal only—no gloss, definition, example, or sense may come from the Japanese dictionary.

## Gates on every resumed entry

The complete current `DICTIONARY_ENTRY_GUIDE.md` governs; in particular:

- corpus-first, English-first, describe without outside interpretation, but make the smallest reproducible inference the corpus requires;
- open with a short, specific account of what the thing is or does in these Chan contexts;
- surface where Chan bends the word (#0g), including precepts and public question-and-answer as in-scope categories;
- distinguish senses only when the corpus uses the word for different things, never merely noun/verb grammar or alternate readings of one use;
- scale depth to frequency and claim complexity; do not treat 3–6 examples as a cap;
- count multi-source support by independent work IDs;
- read the complete case and assign `MasterName` only to the utterer of the exact headword; every master mentioned in reader prose must have the correct structured link and role;
- anchor every Chinese quotation; verify every occurrence and claim anchor exactly with `zc.verify` and preserve exact line endpoints;
- run the inference, lookup-alias, gloss-hygiene, forbidden-English, depth, actor-distribution, attribution, identity, claim-anchor, and semantic-canary gates at author checkpoints rather than postponing defects to final review;
- the reader-facing words `Buddhism` and `meditation` are forbidden; use corpus-derived English and `dhyana` only when 禪 itself requires that category word.

Term-specific gate: before drafting `業`, `無繩自縛`, or `撥無因果`, read `KARMA_RESEARCH_BRIEF.md` fully and complete the exact `WORK.md` research ledger required by guide §5. The requested-term evidence, objections, word-versus-concept control, and counterexamples are tests, not inherited conclusions.

## Next executable step

Do not dispatch new construction while attribution packets are still awaiting cross-review. Once every attribution packet has an independent final verdict, promote only stable reviewed hashes through the existing promotion gate, rerun the whole live-tree attribution/exact-KWIC audits, and keep merge/publication separate.

If that gate is clean, open the next construction wave with `問答` as its first assigned entry (without moving ordinal 3967), then fill the missing NEXT500 rows starting at ordinal 951 `語句`. Each author checkpoints durably before replacement; no Iriya authoring is permitted.
