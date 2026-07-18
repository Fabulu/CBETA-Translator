# Dictionary throughput architecture

This is a quality-preserving speed contract.  The recurring bottleneck is not
the necessary semantic judgment; it is paying repeatedly for the same judgment
or discovering a systemic omission only after a large cohort has been written.

## The invariant

Every stage adds exactly one new class of decision and reuses all unchanged,
SHA-bound decisions from earlier stages.  Independence means a new judgment by
a different reader, not redundant concordance research, XML retrieval, JSON
parsing, or validation.

## Stage contracts

1. **Queue/admission** produces a collision-free ID, canonical ideograph search
   form, punctuated display form where relevant, and disposition.  It does not
   write public prose.
2. **Evidence preparation** produces one immutable complete-case dossier per
   witness: exact KWIC and offsets, line anchors, work ID (not file ID), title,
   section/header context, and enough surrounding text to decide the turn.
   It batch-verifies exact bytes once.
3. **Actor preparation** resolves the utterer of the headword before authoring.
   Local cues may resolve it cheaply.  Any ambiguity is escalated immediately
   through the six-rung ladder.  An unresolved packet is an explicit exception,
   never silently handed to a 100-entry authoring batch.
4. **Semantic adjudication** reads the complete dossier and records explicit
   per-entry decisions: lexical job, sense split, Zen bend, counterexample,
   actor role, modifier/family controls, and retained witnesses.  No public
   prose template is produced here.
5. **Construction** turns those explicit decisions into the unchanged public
   schema.  Each prose claim receives its occurrence anchor while the claim is
   written; anchors are not a later repair pass.  Feedback and attribution
   receipts are serialized from the same explicit decisions.
6. **Canary gate** runs the real changed-cohort contract on the first five
   outputs of every new author/compiler combination.  It must include exact,
   actor, depth/claim-anchor, public-feedback, work-ID, corpus, forbidden-word,
   and cross-entry-template checks.  A systemic failure quarantines at most five
   drafts.  No remainder is authored until the canary is green.
7. **Continuous authoring** writes the remaining cohort without ceremonial
   stops.  Cheap parse/schema checks may run continuously.  One changed-cohort
   gate runs at the agreed checkpoint and one at lane completion.
8. **Independent review** rereads the complete SHA-bound dossiers and evaluates
   the entry's decisions.  It reopens source XML only for a truncated/mismatched
   dossier, actor conflict, possible new sense, or other recorded exception.
9. **Merge** consumes only sealed IDs, runs once per completed wave, and uses
   content-addressed receipts for unchanged entries.  The expensive whole-tree
   gate runs once after reconciliation, not once per worker.
10. **Dashboard/public artifacts** consume the sealed manifest and merge
    receipt.  Counts are derived, never hand-copied.  Dashboard deployment is a
    single downstream operation; ReadZen production remains separately gated.

## Work scheduling

- Give workers long continuous ownership ranges and let an early finisher steal
  an explicitly transferred tail.
- Prepare the next wave's evidence while the current wave is in construction.
- Checkpoint durable ledgers, not conversations.  Do not stop a lane merely to
  report a checkpoint.
- Replace stale-context workers at a durable boundary instead of repeatedly
  reteaching them.
- Resolve exceptions in a narrow lane; do not stall clean work behind one hard
  actor or punctuation case.

## Validation and I/O

- Cache by entry bytes + actual validator dependency hashes + frozen corpus
  hash.  Do not invalidate a green receipt because an unrelated helper changed.
- Run independent read-only audits concurrently, but never launch overlapping
  gates for the same cohort.
- Use the frozen zc/index cache for discovery and exact verification.  Do not
  recursively grep the corpus or the whole Windows worktree.
- During active lanes, never run repository-wide `git status`, `git diff`,
  `find`, or unscoped `rg` on the Windows mount.  Use manifest-listed paths and
  path-scoped Git checks; one global status can put every validator into disk
  wait for minutes.
- Treat `dictionary-build/maintenance/` as a large artifact store, not a small
  source directory. When an assignment supplies exact ledger/review paths, open
  those paths directly; do not run `rg ... maintenance -g '*.json'` to rediscover
  them. Measured directory-wide searches ran for 2+ minutes and contended with
  every corpus reader, while exact JSON access took under a second.
- `zc` keeps a bounded in-process LRU and a durable normalized disk cache. Never
  raise `ZC_MEMORY_CACHE_FILES` to the whole 494-file corpus in parallel workers.
  Batch all cohort queries through one `zc.batch_count` traversal; ten separate
  `zc.count` calls are ten complete corpus scans with identical answers.
- A yielded/timed-out shell call can leave its OS child alive. At wave boundaries
  inspect long-running `find`, broad `rg`, duplicate validators, and duplicate
  temporary builders; terminate only stale duplicates after confirming their
  durable ledger exists. Stale scans are worktree I/O, not harmless background.
- Keep temporary reports and locks on native WSL storage; write only durable
  receipts and entries back to the shared worktree.
- **Publish ledgers atomically.** Assemble and validate the complete JSON in
  memory or at a temporary native-WSL path. Run the cheap and full cohort gates
  there, then replace the durable ledger path in one operation and declare its
  hash. Never stream decisions into the final shared path: reviewers must not
  encounter changing hashes, half-written JSON, or evidence whose line bounds
  have not yet passed `zc.verify`.
- A failed gate reruns only the failed checks on changed entry hashes, followed
  by one final complete cohort gate.

## Failure budget

A defect that affects every entry is a pipeline defect.  The acceptable blast
radius is five canary entries.  Actor ambiguity is resolved before construction;
missing anchors are impossible to serialize; repeated semantic controls are
caught by the canary; duplicate source works are collapsed in the dossier.  A
large cohort may still expose genuinely term-specific semantic problems, but it
must not expose a missing field family shared by the entire cohort.
