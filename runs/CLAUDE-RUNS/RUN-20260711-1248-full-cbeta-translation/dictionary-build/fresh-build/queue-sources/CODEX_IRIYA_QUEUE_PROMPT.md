# Codex task — register the Iriya sayings queue as the FINAL build wave

> **THIS IS AN UPDATE — REVISION 3.** Earlier revisions of `IRIYA_SAYINGS_QUEUE.md` listed **1,491**
> (rev 1) and **1,973** (rev 2) terms. **Both were wrong** — rev 1 stripped punctuation the corpus retains
> and ignored Japanese shinjitai glyph forms; rev 2 fixed those but corrupted text by taking the transitive
> closure of a variant table (時→寸, 鎮→刕). **The file now lists 2,008 terms, derived with a curated
> non-transitive variant map and sanity-asserted against common graphs. It is authoritative.**
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

## IMPORTANT — counts are provisional

These counts were computed over the CURRENT 462-text allowlist. `ALLOWLIST_AUDIT.md` shows the allowlist is
missing ~23 unambiguous Chan texts (指月錄, 教外別傳, Dahui's 正法眼藏, Yuanwu's 佛果擊節錄, the L-series
語錄 …) and that the multi-source gate counts files rather than works. **If the allowlist is fixed, every
count in the queue is understated and must be re-derived.** Do not treat these numbers as final; they are a
build-order signal.

## Sanity check

Every pre-existing wave still precedes the new `z*` batches, and no new `entry.v2.json` files exist.
