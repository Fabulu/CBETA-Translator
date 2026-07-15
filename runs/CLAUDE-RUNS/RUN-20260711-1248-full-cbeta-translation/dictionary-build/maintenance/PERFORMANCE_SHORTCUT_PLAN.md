# Performance shortcut implementation plan

**User directive, 2026-07-13:** after the current retrospective checkpoints, implement the best safe time-savers before expanding the bulk run.

The objective is to remove repeated I/O, obvious attribution work, and audit boilerplate without lowering any semantic or evidence gate.

**Implementation status:** normalized source cache, concise batch query/verification, complete-case attribution packets,
human review workbooks, and the unified cohort mechanical-gate runner are implemented. Automatic speaker writes are
disabled; exact-turn decisions remain human.

## 1. Title-first attribution resolver

Implementation: `attribution_packet.py` now emits title/container candidates, preceding headers, complete structural units, exact KWICs, and fail-closed risk flags. It does not write `MasterName`.

- Extract and cache the Chinese/English book title, named record owner, section/header owner, and TEI responsibility line for every allowlisted text.
- Treat the title owner as a **candidate, never an unconditional truth**. A likely 95% shortcut still produces an unacceptable number of wrong speakers at dictionary scale.
- Classify the container from a verified catalogue; a title merely containing `語錄` is not sufficient (`御選語錄` and other compilations contain many speakers).
- In a verified single-master `語錄` / `廣錄` / `雜錄`, auto-resolve local `師` / `和尚` only when all high-confidence guards pass:
  1. the title maps uniquely to one roster/source-attested master;
  2. the nearest governing section/header is absent or agrees with that owner;
  3. the occurrence is not in a preface, postface, biography, inscription, editorial note, or text by another named contributor;
  4. the **complete encounter/case unit has been read**, from its opening attribution through its closing structural boundary; a bounded KWIC window alone never qualifies;
  5. a speaker-turn map for that whole unit shows that the exact line belongs to the record owner. The title owner may instead be the addressee, person discussed, host of a visiting master, or compiler's subject;
  6. the whole unit contains no unresolved speaker shift, guest master, embedded old case, quotation, citation, `舉`/`拈`/`頌` frame, or conflicting personal name;
  7. the syntax actually makes `師` / `和尚` the speaker or responsible figure rather than the person being discussed.
- In anthologies and lamp records, use the nearest valid entry header first.
- Any title/header/case-turn contradiction fails closed. If the encounter boundary cannot be identified confidently, widen through the next structural boundary or require review. Fall through to wide context and parallel-text searching for collections, embedded old cases, quotations, guest speakers, ambiguous dialogue, or excluded sections.
- Emit the candidate, confidence tier, method, exact title/header evidence, local-window evidence, and every triggered exception; never silently guess.
- Confidence tiers:
  - **A-candidate — guarded review priority:** every mechanical condition above passes, but a human still maps
    the exact turn; this tier cannot write or approve `MasterName`.
  - **B — review suggestion:** title/header strongly suggests a speaker but one guard is unresolved; no automatic `MasterName` write.
  - **C — full ladder required:** collection, conflicting evidence, speaker shift, embedded material, or ambiguous responsibility.
- The fixed 24-case stratified validation failed with one false accept among two A candidates: an inline
  `應庵祖云` quotation was assigned to a title-parser candidate. Tier A writes are therefore disabled. The packet
  now vetoes inline speaker markers, partial title-alias matches, and extracted units that do not contain their
  stored KWIC. Even a future zero-false-accept replay may only improve review ordering; exact-turn human
  confirmation remains mandatory.

## 2. Authoritative indexed corpus cache

Implementation: `zc.py` now maintains a source-fingerprinted apparatus-clean cache under `/tmp/cbeta-zc-cache-v3`; stale/corrupt/unwritable cache states fall back to direct TEI.

- Build from the same 462-text allowlist and normalization contract as `zc.py`.
- Index body text only; exclude `note`, `app`, and `rdg` apparatus.
- Persist normalized text offsets to primary-edition `lb` bounds.
- Persist source fingerprints so stale shards fail closed.
- Support instant exact counts, file counts, KWIC/context retrieval, title/header lookup, and batch verification.
- Prove parity against direct `zc` reads before treating the cache as authoritative.

## 3. Persistent/batch verifier

Implementation: `zc_batch.py` provides multi-term counts, cohort entry verification, cache warming, and JSON-lines persistent jobs.

- Keep the normalized corpus cache alive across all terms in a cohort.
- Verify every occurrence in one batch instead of launching a fresh Python process per query or entry.
- Return exact `ok`, `FromLb`, `ToLb`, allowlist status, and duplicate-count diagnostics.

## 4. Automated attribution-note and audit scaffolding

Partial implementation: `run_cohort_gate.py` emits one hash-bearing cohort report for exact KWIC, attribution, quote anchors, depth/sense, public-feedback, forbidden-English, and attribution packets. Semantic verdicts and final approval writes remain deliberately manual/root-owned.

- Generate a source-and-speaker note skeleton from resolved title, section owner, and occurrence metadata.
- Generate WORK gate headings and mechanical observations from audit output; semantic verdicts remain human/agent decisions.
- Produce one cohort acceptance bundle containing exact-WQIC, attribution, quote-anchor, depth, forbidden-English, artifact-parity, and website results.
- Generate approval-file candidates against current bundle hashes; root still adjudicates before pass.

## 5. Execution cadence

- Work in three-entry checkpoints.
- Evidence author finishes and mechanically audits three entries.
- A different agent falsifies them while the author starts the next three.
- Root adjudicates only after requested revisions land on the current hash.
- Merge coherent accepted checkpoints; do not wait for a ten-entry research batch to become durable.
- Source-batched attribution uses a three-wave regeneration cadence. Preplanned sources and within-wave entry sets are
  disjoint, so completing one source does not stale the untouched workbooks for later sources. Run focused ground-truth
  gates and deterministic merge after each wave, but pay the 40–50 second full triage/planner regeneration only after
  three waves (or immediately after any schema/candidate-extraction change).

## 6. Prepared exact-turn workbooks

Implementation: `case_review_workbook.py` converts one or more current entries into a single human worksheet and
parallel JSON packet. Every occurrence arrives with corpus frequency, current sense/role, exact KWIC, extracted
complete structural unit, title/header candidates, inline-speaker risks, and blanks for the exact actor, other roles,
disposition, ladder evidence, and confidence.

- `zc._raw_pos_for_kwic` now blanks apparatus at equal length instead of deleting it; later raw offsets therefore
  remain aligned with the source XML.
- If a stored KWIC crosses sibling paragraph elements, packet extraction widens through `lg`, `div`, head-section,
  or explicitly marked context until the normalized KWIC is actually present.
- A workbook with any missing stored KWIC is not ready for assignment.
- `zc_batch.py count` now emits concise hit/file summaries by default. Use `--per-file --top-files N` only when the
  document histogram is actually needed; this prevents thousands of irrelevant output lines during triage.
- Prepared example: `maintenance/next-wave-prep/` contains the baseline gate, complete-case workbook, JSON packet,
  and one-agent-per-entry briefs for 坐禪, 世尊, and 平常心.

## 7. Source-batched unresolved-actor triage

Implementation: `attribution_triage.py` scans every `STATUS=done` source entry, selects only bare unresolved exact
actors, sorts them by source, and groups all affected dictionary occurrences by their complete structural unit. It is
read-only and never writes or approves `MasterName`.

- Raw XML and normalized-character-to-XML offset maps are built once per source rather than once per occurrence.
  The position array is a compact unsigned-int array and the cache holds one source, fitting source-ranked review.
- The output distinguishes a roster-resolving inline name from generic turn grammar such as `師曰`; the latter is
  never treated as a named speaker.
- Hard-tail rows whose existing note contains exactly one roster-canonical English name get an
  `existing-note-canonical-candidate` draft. Chinese aliases are deliberately not used here because the headword itself
  can collide with a master's alias (for example 法眼). Full-case review still decides whether the named person is
  speaker, quoted person, addressee, or context. This safely promoted 77 rows from the full-ladder tail into signed
  exception-sheet review without auto-approving any.
- Generic offices (`國師`, `禪師`, `和尚`, and similar) are excluded from roster owner matching. Without this veto,
  `普濟玉琳國師語錄` falsely selected Nanyang Huizhong merely through the shared title “National Teacher.”
- Review classes order work without deciding it: inline named candidate, nearest anthology-header candidate,
  guarded single-record candidate, and full ladder/parallel-text review.
- A title or header remains only a candidate. The reviewer must read the complete case and map its exact turn because
  the record owner can be an addressee while a visitor, quoted master, monk, narrator, or impersonal structure acts.
- The triage exception detector is regression-tested against the formal schema: exception state lives in `Status`,
  not `Kind`; an unnamed master never counts complete; impersonal rows require `GrammarEvidence`. This removed reviewed
  non-master/citation rows that the first prototype had incorrectly counted as unresolved.
- After seventeen source batches, the corrected full scan covers 1,711 unresolved occurrences in 243 sources and
  1,599 complete-case clusters. It identifies 23 named-inline, 35 guarded single-record, 206 anthology-header, 61
  existing-note, and 36 co-located-reviewed candidates; 1,350 occurrences remain in the intentionally slower
  full-ladder class. `attribution_source_workbook.py --quick-only` turns any source's candidate cases
  into an agent-ready workbook while omitting the hard tail.
- `attribution_wave_planner.py` greedily packs quick source batches into three-worker waves whose entry-ID sets are
  disjoint, eliminating shared-checkout collisions. A configurable per-worker occurrence cap prevents a giant lamp
  record from monopolizing one wave. The regenerated ten-wave plan covers 103 candidate occurrences across 30 sources.
- The first production source checkpoint resolved 90/90 assigned occurrences across 78 entries and five sources. The
  combined source gate passed; all 443 occurrences in touched files verified exactly. Title/header contradictions were
  common and real, confirming that batch retrieval can be automated while exact-turn judgment cannot.

## 8. Signed exception sheets and validated bulk apply

Implementation: `make_attribution_override_sheet.py`, `compile_attribution_override_sheet.py`, and
`apply_attribution_decisions.py` remove the post-review file-edit bottleneck.

- The generator places the unique inline/header/title owner in `defaultMasterName` as an unapproved draft and pairs the
  sheet with the complete-case workbook. The reviewer reads every case and records full actor decisions only for
  contradictions.
- The compiler refuses to expand defaults unless `reviewedAllCases` is explicitly true and reviewer/time are signed.
  It generates source-and-speaker notes for confirmed defaults and preserves custom notes/context for overrides.
- The applier supplies no guesses. It validates exact actor XOR, forbids unnamed masters, requires all six ordered
  rungs for an unnamed non-master, requires grammar evidence for impersonal rows, exact-matches every stored occurrence,
  reruns `zc.verify` and line-anchor equality, validates the entire sheet before any write, then atomically rewrites each
  affected entry once.
- After the first speed-test wave, a downstream gate caught one note-label mismatch that the initial applier permitted.
  The applier now also requires every note to repeat the exact `MasterName` or reviewed `ActorLabel`, and compiled sheets
  require the exact source title in the note. This turns that discovered defect into a pre-write rejection.
- Six regression tests cover the exception schema, unnamed-master prohibition, impersonal evidence, and decision XOR.
  Empty/unsigned sheets fail closed. This reduces a 30-row reviewed source from 30 hand-edited entry operations to one
  signed sheet containing only the observed exceptions plus one mechanical apply.
- First production proof: the T48n2003 worker compiled and applied 27 reviewed decisions across 22 entries with zero
  dry-run or apply failures; all 117 occurrences in touched files retained exact KWIC/anchor parity. The surrounding
  three-source wave completed 70/70 assigned actors across 59 entries and verified 343/343 touched-file occurrences.

## 9. Cross-paragraph case-span extraction

Implementation: when a stored KWIC crosses sibling paragraphs, `attribution_packet.py` now maps the verified first and
last normalized KWIC characters back to raw XML and extracts the minimal first-to-last `<p>` span before considering a
large `lg`, `div`, or head section. The span is flagged `coarse-or-uncertain-case-boundary`, so a reviewer must still
widen when the encounter opening or exact actor is absent.

Production trigger: one X79 workbook case had widened to a 383,383-character `<lg>` although its longest stored KWIC
was 118 characters. The corrected span is 163 characters, contains the stored KWIC exactly, exposes the governing
named comment and turns, and retains the mandatory boundary-confirmation flag—a roughly 2,350× reduction in irrelevant
review text without pretending that a truncated unit is automatically complete.

## Acceptance benchmark

The speed layer is accepted only if:

- indexed and direct-TEI counts agree under the same normalization;
- batch verification reproduces current `zc.verify` results and line bounds;
- title-first resolution passes hand checks on own records, anthologies, embedded cases, and explicit speaker shifts;
- no new null speaker, dangling quote, nested-compound depth, or stale-artifact regression appears;
- measured wall time improves on representative high-frequency and attribution-heavy entries.

## 10. Concurrent composite-gate audits

Implementation: `run_cohort_gate.py` performs exact KWIC verification first, then runs the seven independent,
read-only cohort audits concurrently. Their payloads, exit codes, phase timings, and hard-pass conjunction are
unchanged; no semantic decision is automated.

- Two-entry canary: 2.287 s serial baseline → 1.630 s concurrent (29% less wall time).
- Thirty-two-entry production cohort: 5.091 s concurrent versus 11.914 s summed audit durations plus 0.690 s exact
  verification (about 60% less wall time than serial execution would require).
- The attribution and depth audits remain the longest individual mechanical phases. They now overlap instead of
  blocking each other, so optimizing either one further cannot weaken or bypass the other.
