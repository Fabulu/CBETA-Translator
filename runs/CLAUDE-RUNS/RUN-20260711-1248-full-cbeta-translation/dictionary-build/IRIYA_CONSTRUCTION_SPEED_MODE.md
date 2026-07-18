# Iriya construction speed mode — hard process rules

This changes transport and scheduling only. It does not change the public entry schema, corpus,
provenance firewall, full-case exact-turn rule, depth rule, source-title authority, work identity,
independent semantic review, or final gates.

## Universal speed acceptance gate

Guide rule 19 applies to every present and future dictionary workflow change. Before production,
benchmark a representative ten-entry canary through settled re-run, including failures and repair.
The three-lane projection for 500 settled entries must be four hours or less; otherwise the process
remains in speed mode and mass production does not begin. Machine-only authoring overhead must remain
below five seconds per entry and any regression above 10% needs a written justification plus a tested
faster alternative. Store a durable speed receipt with stage timings, output-equivalence evidence,
failure/rework counts, and the superseded path. Never count quarantined or gate-failing drafts as
throughput.

## Measured defects removed

1. Recursive `rg`/`find` scans over the repository, `maintenance/`, `fresh-build/`, `terms/`, or
   `/mnt/c` are forbidden. Several such processes ran for 5–10 minutes at 18–54% CPU. Use an exact
   manifest path, a Python dictionary keyed by ID, `zc`, or the inverted index.
2. Discovery is paid once. The authoritative immutable first-cohort tail transport is
   `maintenance/iriya-construction-001-pos021-end-bulk-transport-v2.json`, SHA-256
   `4841abe755a661e9162486674f98d1d65538df2899def854144e55e5c16dce80`.
   It covers 440 unique IDs and 3,520 canonical-work-distinct candidate cases with zero failures and
   no automatic actor decisions. It uses each queue row's authoritative resolved search form, so
   punctuation-segmented and governed graphic variants are not silently lost. It was generated in
   23.97 seconds. The unversioned transport is superseded and must not be used.
3. Resolve expensive XML section headings only after selecting retained witnesses. Before assigning
   any actor, every retained witness still receives the heading and full six-rung attribution review.
   The precomputed equivalent heading sidecar is
   `maintenance/iriya-construction-001-pos021-end-heading-sidecar-v2.json`, SHA-256
   `0ec89127cc9b0c5d63184355f28870a9cafcf5f1054211f459aeb0306ca175b4`.
   It resolves all 3,520 transport cases from 267 source files with zero failures in 10.28 seconds;
   a fixed-seed sample of twenty rows exactly matched `zc.head`. This is header evidence, never an
   automatic actor decision.
4. `audit_depth_sense.py` batch-counts uncached cohort terms in one corpus traversal. It may not call
   `zc.count` serially once per fresh headword.
5. Positions 1–20 are the calibration sample. After their closure, each lane constructs continuously
   through position 50. Write a durable ledger every ten entries, but do not stop, request release, or
   run the full cohort gate at positions 30 or 40.
6. Per entry: compile the evidence worksheet and verify retained spans. Per ten: focused exact-span,
   actor/public static checks as warranted. Per fifty: full cohort gate, authoritative-title gate,
   deployment-duplication gate, independent full-case semantic review, install/merge/verification,
   and commit checkpoint.
7. Never run `run_cohort_gate.py` on one- or two-entry partials. The compiler is the cheap structural
   authoring gate; the full gate is a checkpoint gate.
8. Ten entries are one authoring/tool packet. Load transport, adjudication reuse, headings, titles,
   and lane manifest once; select and read the retained complete cases for all ten; record the ten
   semantic decisions in one compact table; expand the common schema programmatically; compile and
   verify the packet in one invocation. Do not pay a reasoning/tool startup boundary between entries.
   This batching changes no semantic decision and does not permit an actor inference from metadata.
9. Before beginning the next packet, every emitted ten-entry packet must pass the four cheap defect
   preflights on its exact manifest-derived paths: `audit_attribution.py --json`,
   `audit_work_source_validation.py`, `audit_batch_semantic_templates.py`, and the batch-counted
   `audit_depth_sense.py --paths`. Repair the compact
   decisions or shared emitter and regenerate the packet; never defer their failures to position 50.
   This catches bad actor ownership, inadequate witness depth/source spread, and generic template
   prose at the cheapest boundary without paying the complete cohort gate per entry. This fourth
   preflight became mandatory after Lane B reached position 50 with nine entries below the six-witness
   floor; a late depth repair is a speed defect, not an acceptable checkpoint surprise.
   The semantic-template gate must normalize **sentence structure**, not merely exact text after
   replacing the headword and preferred target. A unique descriptor inserted into “the X cases support
   the literal target,” “six witnesses preserve the X construction,” or equivalent generic Zen-bend,
   split, alias, modifier, and family sentences remains stock and fails once. This guard was added after
   C91–100 passed the old exact-repeat gate yet independent reading rejected all ten reader openings;
   C101–110 was caught author-side immediately by the strengthened preflight.
10. Every proposed process change is presumed to contain avoidable overhead until its speed receipt
    proves otherwise. Audit for duplicated reading, duplicated validators, serial subprocesses,
    repeated parsing, over-wide evidence packets, checkpoint/worker startup frequency, collision
    waits, and repair caused by permissive templates. Retire the superseded path when the replacement
    passes so both cannot run accidentally.
11. Record each retained case's semantic judgment once. The compact decision contains the selected
    immutable case index, exact recut KWIC, the literal headword-bearing clause inside it, exact
    utterer (or complete compact non-master/narrated/impersonal decision), one case-specific
    grammatical proof sentence, and any genuinely additional context. Generic “X utters/owns the
    headword” proof is forbidden because it does not distinguish a heading owner from the utterer.
    `author_from_packet` deterministically expands transport paths/titles/work IDs, the utterer
    `ContextMasters` link, six-rung scaffolding, `AttributionNote`, and draft proof into the unchanged
    schema. Authors must not manually retype those duplicate facts. This is transformation, not actor
    inference: the helper refuses incomplete, generic, invalid-role, or malformed-span decisions. The
    same transformation writes `WORK.md` from the already-recorded case indices, work IDs, actor proofs,
    sense test, and flyswatter finding. A missing WORK ledger is therefore an emitter failure, not ten
    clerical author tasks; only genuinely term-specific research-gate markers are added by hand.
12. Worker context and worker startup are both measured resources. A lane worker receives one large,
    collision-free assignment of up to one hundred entries with `fork_turns=none`, exact absolute
    paths, and precomputed transport. It processes that assignment as internal ten-entry packets,
    writes an append-only checkpoint after every ten and a recovery ledger after every fifty, and
    continues without respawn. Keep only the compact decisions and current packet in live context;
    durable ledgers, not conversation history, carry prior packets. After authoring, rotate the three
    workers across lanes for independent review so no worker reviews its own lane. Explicitly release
    a worker only after its large assignment or rotated review is settled. A ten-entry *tool packet*
    is not a ten-entry *agent lifetime*. This preserves bounded validation while removing repeated
    worker startup and context-fork overhead.
13. Run `maintenance/kill_runaway_dictionary_scans.py --min-seconds 20` during orchestration polls.
    It terminates forbidden broad `find .`, `/mnt/c` searches, repository-wide `rg`, and `git status`
    processes that survive the grace period. A live audit found concurrent scans consuming roughly
    20–40% CPU each for two to four minutes despite exact paths already being supplied. Store the scan
    guard receipt; a killed scan is a process defect to repair in the next worker prompt.
14. Structural-case slices are author-navigation keyed by candidate `caseIndex`; they contain no actor
    judgment. Never map a persisted occurrence ordinal back to the same numeric case index, because
    author selection reorders and drops candidates. Independent review reads the persisted exact recut
    KWIC/attribution packet. During authoring, compare a selected structural slice with its same-index
    immutable transport case and fall back whenever it is incomplete or overbroad. Anonymous actor
    fallback is forbidden; a packet that defaults unresolved cases to null MasterName is quarantined.
15. Optimize evidence selection before optimizing review. Among semantically equivalent witnesses from
    distinct works, the authoring packet should rank short, structurally complete, single-turn cases ahead
    of mixed-actor windows, embedded quotations, prefaces, lineage headings, and section boundaries. This
    ranking is navigation only: it must not choose an actor, suppress a distinct sense, reduce the depth or
    distinct-work floor, or replace the author's corpus-wide deployment check. The author records why any
    lower-ranked difficult witness was necessary. A ten-entry canary must show the same semantic/source
    coverage and fewer repair deltas before this ranking becomes the default.
16. Independent actor review remains mandatory until a labeled canary proves a narrower review policy has
    100% recall on known semantic deltas. Use the already double-read B21–50 set (180 occurrences, 35 real
    deltas) as the first regression corpus. Mechanical hard-pass is not a substitute. Risk scoring may only
    prioritize review; it may not exempt cases unless it catches all 35 historical deltas and a fresh
    ten-entry packet produces zero missed deltas under full shadow review. This makes speed experiments
    falsifiable without quietly trading attribution quality for throughput.
17. There is exactly one editable authority for a fresh entry: `evidence.draft.json`. `entry.v2.json`
    is a deterministic compiled product and must never be repaired independently. Every authoring,
    repair, and review-accepted change is first written to the worksheet and compiled once into the
    product. Before a packet can pass, compiling every worksheet must succeed and reproduce its sibling
    entry byte-for-byte. Missing worksheets, compile drift, or `siblingEntryParity=false` are hard
    failures. This gate exists because 79 of the first 190 products had diverged from their worksheets;
    later compilation resurrected stale actors, RelatedMasters, prose, and overbroad KWICs.
18. Pay for full semantic review once per complete checkpoint, not through expanding partial scopes.
    The independent reviewer reads every retained occurrence in the complete lane checkpoint and emits
    one exhaustive coordinate list. One repair pass applies that closed list, and a different reviewer
    rereads the changed coordinates. If changed-only rereview discovers an untouched defect in the same
    entry, the original review was incomplete: record it as a process failure and require the next
    checkpoint reviewer to finish the whole packet before handoff. Mechanical validators are hash-cached
    and run once per unchanged artifact set; a receipt may reuse a still-matching result instead of
    spawning the same validator again.
19. Every compact occurrence from lane position 81 onward records one closed `voiceLayer`: `direct-turn`,
    `question-turn`, `quoted-original`, `transmitted-verse`, `compiler-narration`, `embedded-copy`, or
    `impersonal`. A quoted original must also name the outer raiser/appraiser in `ContextMasters`; a
    transmitted verse cannot inherit the compiler as actor without an explicit local attribution; and
    the retained KWIC contains exactly one governed headword/search-form span. The emitter rejects these
    failures before product generation. This targets the first streamlined decile's measured repair
    causes: missing raised-speaker context, compiler-as-verse-author guesses, copied-quotation boundaries,
    and multi-span KWICs.
20. Validator caches bind every input they inspect. In particular, the depth/sense cache key includes
    both `entry.v2.json` and `WORK.md` hashes. Materializing or revising a work ledger must invalidate the
    old result; `AUDIT_DEPTH_EPHEMERAL=1` is a diagnostic escape hatch, not the production solution.
21. Any new semantic generator, classifier, or materially changed decision helper must pass a
    two-entry full-case canary through attribution, exact-span, template, work-source, depth, and
    compiler-parity gates before it may emit a ten-entry packet. A helper that assigns
    `compiler-narration` inside a complete case containing an explicit 曰/云/問/答 marker must also carry
    the per-occurrence `speechMarkerReviewed=true` acknowledgment earned by reading that complete case;
    a narrow recut KWIC cannot hide a question or answer marker earlier in the case. This field is
    decision provenance and compiles away; it does not alter the reader schema. The canary is a speed
    gate: quarantining two bad products is cheaper than repairing ten confident actor guesses.

## Safety boundary

The transport is navigation, not judgment. `automaticActorDecision` is always null. Authors and
reviewers independently read the complete retained cases. `MasterName` remains only the exact utterer
of the headword. Quoted originals, later raisers, questioners, respondents, compiler narration, and
impersonal clauses remain structurally distinct. Every saved KWIC still passes `zc.verify` with exact
text, `FromLb`, and `ToLb`.
