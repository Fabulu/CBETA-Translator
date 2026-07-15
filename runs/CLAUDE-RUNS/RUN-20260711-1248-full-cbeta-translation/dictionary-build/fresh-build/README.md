# Fresh dictionary rebuild (current rules, expanded work-aware corpus)

This tree is the new authoritative construction lane. The historical `terms/`
tree is frozen reference material: its prose, candidate evidence, and research
may be consulted, but no old entry is considered validated merely because it
was previously `STATUS=done`.

## Completion definition

An entry counts once, and only after all current gates pass: exact lexical
evidence and frequency-scaled depth; independent-work source counting; full
passage exact-actor review; ClaimAnchor coverage for every quoted claim;
sense/gloss/flyswatter/inference review; independent semantic KEEP; exact
`zc.verify`; merge verification. The dashboard's primary percentage uses only
this final state.

## Frozen queue order

`queue-sources/ORDER.md` is controlling. Each source list retains its internal
rank and wording. Normalized duplicates link to the earliest controlling row;
they are never silently discarded. Historical entries are attached as
reference drafts to matching rows, not inserted as a competing queue.

## Corpus

The canonical runtime manifest is
`Assets/Data/zen-corpus.json` (schema v2). It currently contains 494 XML files
representing 487 independent works. Every file has a `work_id`; split volumes
and parallel editions never inflate multi-source validation.

## Evidence-first authoring (mandatory from f002)

Workers author `evidence.draft.json` from
`EVIDENCE_DRAFT_TEMPLATE.json`, then run `compile_evidence_draft.py`. The
compiler writes the same `entry.v2.json` schema consumed by the merger,
desktop application, and website; research-only fields never enter production
output. It rejects generic actor boilerplate, calque-first openings, missing
aliases, ungrounded sense decisions, absent modifier/family controls, and
unanchored opening claims before an entry may be checkpointed as drafted.

The compiler's hash-bound `evidence-compile-report.json` is checked by
`checkpoint_fresh_lane.py`. Any worksheet or entry edit invalidates the
receipt and forces recompilation. Wave f001 remains on its existing repair
ledgers; all construction beginning with f002 uses the evidence-first gate.
