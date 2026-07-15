# Codex task — register the Iriya sayings queue as the FINAL build wave

> **THIS IS AN UPDATE — REVISION 3.** Earlier revisions of `IRIYA_SAYINGS_QUEUE.md` listed **1,491**
> (rev 1) and **1,973** (rev 2) terms. **Both were wrong** — rev 1 stripped punctuation the corpus retains
> and ignored Japanese shinjitai glyph forms; rev 2 fixed those but corrupted text by taking the transitive
> closure of a variant table (時→寸, 鎮→刕). **The file now lists 2,008 terms, derived with a curated
> non-transitive variant map and sanity-asserted against common graphs. It is authoritative.**
>
> **PRE-BUILD ADMISSION GATE (2026-07-15):** Before constructing any Iriya entry, read
> `IRIYA_PREBUILD_AUDIT.md` and its complete `IRIYA_PREBUILD_AUDIT.json` worksheet. The audit preserves
> all 2,008 candidates but flags component-only couplets, one-work/thin forms, punctuation-sensitive
> matches, close nested families, duplicate normalized queries, possible general question frames, and
> count shifts on the frozen 494-file / 487-work corpus, manifest
> `42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a`. The resumable working
> packets are `fresh-build/iriya-admission/packet-001.json` through `packet-021.json`, governed by
> `fresh-build/iriya-admission/ledger.json`; they checkpoint every 50 candidates and preserve all 2,008
> ranks exactly once. Every row requires a recorded `ACCEPT`, `REVISE`, `DEFER`, or `REJECT`
> Zen-deployment admission decision from corpus context; flagged rows additionally require
> resolution of every mechanical flag. A mechanically clean row is not automatically accepted. Nothing is silently deleted,
> and frequency alone cannot overrule a lexical-boundary failure.
>
> **WHOLE-QUEUE QUARANTINE:** do not construct even one Iriya entry until all 2,008 packet rows have a
> first-pass disposition from the guide and every proposed `KEEP (couplet)` or `PROVISIONAL` has a separate,
> matching independent disposition at the exact reviewed packet bytes. `KEEP (component)` routes only the
> named component and never builds the rejected couplet headword; `REJECT` never becomes construction-eligible. Run
> `validate_iriya_admission.py --build-gate`; any nonzero exit keeps the entire Iriya construction phase locked.
>
> **If you already registered an earlier version: reconcile, do not duplicate.** Add only the missing terms,
> keep existing ids stable, drop any term no longer on the list, and report exactly what changed. If you have
> not registered anything yet, just proceed normally.

## What you are doing

Read `runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build/IRIYA_SAYINGS_QUEUE.md`.
It lists **2,008 candidate headwords** (Chan sayings / capping phrases / proverbs), each with a precomputed
`t_*` id, the Iriya-form term, the variant-normalised query string, an exact-pair count, and an anchor count.

**Register them into the build queue as the LAST wave. Do not build any of them.**

## Hard constraints

1. **LOWEST PRIORITY IN THE PROJECT.** Schedule strictly *after* every existing queue is exhausted:
   `WAVE_PLAN.md`, `NEXT500_TERMS.md`, `NEXT500_BUILD_PLAN.md`, `NEXT100_BUILD_PLAN.md`,
   `NEXT100_SAYINGS_CANDIDATES.md`, `REQUESTED_BUILD_PLAN.md`, `RELATED_INVESTIGATION_BACKLOG.md`.
   Use a batch-id prefix that sorts last (`z001`, `z002`, …) and record the ordering explicitly in whatever
   plan/ledger file governs wave order.

2. **DO NOT BUILD ANY ENTRIES.** No `entry.v2.json`. No agent spawns. Registration and queue bookkeeping only.
   If `register_terms.py` refuses to register a term without a built entry, **do not force it** — record the
   queue in the plan/ledger layer only, and report what you did and why.

3. **PROVENANCE FIREWALL — non-negotiable.** These headwords come from the *headword index* of Iriya & Koga,
   *Zengo jiten* (1991), via the public `cjkvi-dict/zendic.txt`. **No gloss, definition, example or sense from
   that book has been taken, and none may ever be.** The book is in copyright and guide §5 #0b forbids deriving
   a definition from any other dictionary. Iriya's list is a **selection signal only**. Every sense, gloss,
   occurrence and KWIC in any resulting entry must be derived independently from the corpus and verified with
   `zc.verify`. Carry this warning verbatim into any plan or ledger file you create.

4. **Deduplicate before registering.** Exclude any term already present in a built entry
   (`terms/t_*/entry.v2.json` → `SourceTerm`), in `MANIFEST.jsonl`, or in any queue file listed in (1).
   Report how many were dropped as duplicates, and which.

5. **The two count columns are not interchangeable.** `Pair` is the real count of the saying. `Anchor` is an
   **upper bound / recurrence signal** for couplets and often counts a generic component (e.g.
   `三世諸佛、口掛壁上` anchors on `三世諸佛`, 2,830 hits, which is not the saying). Preserve both columns and
   the warning; never let `Anchor` be read as a concordance count.

6. **Candidates, not authorities.** Note in the plan that if the corpus does not support a distinct lexical
   article, the correct outcome is **reject with a stated reason**. Substrings of larger compounds must be
   re-adjudicated, not auto-promoted.

7. **The 217 "not attested" terms at the end of the queue file are NOT to be registered or built.** They are
   recorded as a corpus-divergence finding only.

8. **Additive only.** Do not modify, reorder, or renumber any existing entry, plan, or wave.

## Deliverables

- The 2,008 terms registered/queued as the final wave, in the file's rank order, batched consistently with
  existing wave sizes.
- `IRIYA_QUEUE_REGISTRATION_REPORT.md` stating: how many registered, how many dropped as duplicates (and
  which), whether a prior registration (1,491 or 1,973 terms) existed and how it was reconciled, the batch ids assigned,
  where wave order is recorded, and confirmation that nothing was built and no existing file was modified.

## IMPORTANT — counts are frozen-corpus counts, not semantic verdicts

The original queue columns are historical selection signals. The authoritative re-audit columns in
`IRIYA_PREBUILD_AUDIT.json` were recomputed over the locked 494-file / 487-work corpus and count independent
`work_id` values rather than files. They remain triage evidence only: frequency and work spread cannot prove
that a string is a lexical unit, carries a Chan-specific deployment, or deserves a separate article.

## Sanity check

Every pre-existing wave still precedes the new `z*` batches, and no new `entry.v2.json` files exist.
