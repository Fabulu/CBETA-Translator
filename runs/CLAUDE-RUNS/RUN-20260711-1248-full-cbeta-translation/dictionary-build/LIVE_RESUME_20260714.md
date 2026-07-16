# Live crash-resume checkpoint — 2026-07-14 13:35 Europe/Zurich

## Highest-priority override — ACTOR_AUDIT, 2026-07-14

Read `ACTOR_AUDIT.md` after the guide and `ATTRIBUTION_FIX.md`. Its ruling is
immediate and retrospective across every occurrence in every entry:

- `MasterName` means only the person who utters the exact headword. A record
  owner, section subject, respondent, person described, or nearby master belongs
  in `ContextMasters`, never in `MasterName` unless he owns that exact turn.
- Read the complete passage for every occurrence. Search/index/title machinery
  may locate and package evidence, but may never decide the actor or approve a
  positive attribution. Positive names require the same contextual scrutiny as
  reviewed-unnamed findings.
- Record one of four outcomes: named master utterer; named/unnamed non-master
  utterer; compiler narration (`Status=narrated`); or no human actor
  (`Status=impersonal`). Never invent a speaker.
- Use only the 17 closed roles in `ACTOR_AUDIT.md`; put all finer prose in
  `GrammarEvidence`.
- Re-cut every KWIC that lacks its own `SourceTerm`, re-derive its lb, and pass
  `zc.verify`. Never delete an evidentiary quote to evade this repair.
- Maintain a change ledger for every occurrence whose previous `MasterName`
  proves not to be the headword utterer. The old 424 entries are not
  grandfathered.

The previous statement below that attribution remediation was complete is
superseded. It meant only that the earlier schema had no unresolved master
actors; the actor audit proved that many positive attributions were unearned.

Durable execution state: `build_actor_reaudit_queues.py` generated three
collision-free ledgers under `maintenance/actor-reaudit/` covering the current
641 entries and 4,342 occurrences, balanced at 1,447 / 1,448 / 1,447
occurrences with unique entry ownership and unique occurrence keys. Run
`validate_actor_reaudit.py` on completed rows. Sequence this pass behind each
entry's semantic wave so later prose/definition edits do not stale the actor
approval. `ACTOR_REAUDIT_PLAN.md` is the operational specification.

## Newest checkpoint — 2026-07-14 approximately 14:45 Europe/Zurich

- r002 is fully complete and formally registered as
  `semantic-r002-retrospective-20260714`: 90/90 evidence rows current-hash
  hard-pass, 90/90 cyclic independent KEEP, root gate 696/696 exact KWIC,
  zero smell-scan defects, deterministic repeat merge, and website tests green.
- The canonical locally merged termbase now contains 635 entries. The repeat
  merge changed zero shards. Six missing historical `STATUS=done` files were
  restored during artifact parity reconciliation.
- The remediation ledger now reports 224/641 formally complete, 417 remaining,
  and zero stale approvals.
- r003 is active in all three owner lanes. Derive exact live counts from
  `semantic-r003-owner{1,2,3}.json`; do not restart or re-review r002.
- r002 registration exposed and resolved two reusable mechanical guards:
  reviewer ledgers must store `subjectEntrySha256` as a field rather than only
  mentioning the hash in prose, and source entries must use schema-normalized
  `Validation=provisional` / `Curated=true` values so public artifacts can equal
  their source exactly. `validate_semantic_reviews.py` now distinguishes a
  missing review hash from a stale one.
- `preflight_semantic_wave.py` is now a mandatory acceleration gate. Run its
  `--stage owner` mode before independent review to batch-fix WORK/schema/hash/
  alias/STATUS defects, and its `--stage integration` mode after merge before
  registration. The completed r002 cohort passes integration mode cleanly.
- After the existing-entry retrospective, run the mandatory all-entry reader
  prose pass in `POST_REMEDIATION_PROSE_PASS.md`. Exact actor metadata must be
  surfaced in `Explanation`, `Note`, and `AttributionNote`; a nameable master
  may never remain generic prose. This pass is part of completion, not optional
  polish.

This is the newest authoritative pointer. Read this before the older historical
numbers in `CODEX_RESUME.md` or `CODEX_HANDOFF.md`.

## Current phase

- Attribution remediation is complete: the current ground-truth scan has zero
  unresolved master actors. Preserve that invariant during every edit.
- The active job is the full semantic/depth/public-feedback retrospective over
  597 previously published entries. The entries are partitioned into three
  collision-free rolling queues of 199 entries each.
- r001 is completely evidence-repaired, independently reviewed, root-gated,
  merged twice, website-tested, and formally registered as
  `semantic-r001-retrospective-20260714`.
- r002 is active. Do not redo r001 or completed r002 rows. The JSON ledgers under
  `maintenance/semantic-cohorts/` are authoritative row by row.

## Exact durable progress at checkpoint

- r002 owner1: 22 complete, 1 in progress, 7 pending.
- r002 owner2: 30 complete.
- r002 owner3: 25 complete, 1 in progress, 4 pending. The worker's prose
  checkpoint reported 24 complete, but the ledger contains 25; trust the ledger
  and reconcile at cohort finish rather than discarding any row.
- r002 reviewer2 (owner2 reviewing owner3): 24 reviewed at the initial snapshot;
  the one REVISE was `歸宗斬蛇`, and it has now been repaired and independently
  re-reviewed as KEEP. New entry SHA is
  `d3a2a5757b62bf26ed1ecd7e8731ba6e42c96fe42492407682d9964d76b3326b`, and its
  current gate is hard-pass 7/7. Derive the fresh reviewed count from the ledger.
- r002 reviewer1 and reviewer3 are waiting until their evidence-owner work lets
  them begin the cyclic reviews.
- r003 owner2: 10 complete, 5 in progress, 15 pending. Active batch:
  `僧堂`, `分疏不下`, `合頭語`, `喪身失命`, `困來即眠`.
- r003 owners1 and 3 have not started because their r002 evidence remains active.
- Total retrospective evidence complete at checkpoint: 177/597 if owner1's newly
  checkpointed 22nd row is counted; derive fresh counts from the ledgers after a
  restart rather than copying this aggregate.

## Rolling ownership (do not repartition)

- owner1: `/root/feedback_lexicography`
- owner2 and reviewer of owner3: `/root/repair_bird_path`
- owner3: `/root/feedback_lexicography/remaining137_research`
- Queue manifests: `maintenance/semantic-rolling-owner1.json`, owner2, owner3.
- Cohort ledgers: `maintenance/semantic-cohorts/semantic-r00{2..5}-owner{1..3}.json`.
- Cyclic reviews: reviewer1 reviews owner2, reviewer2 reviews owner3, reviewer3
  reviews owner1. A reviewer never edits the reviewed entry; REVISE goes back to
  its evidence owner, is re-gated on the new hash, and is re-reviewed.

If the process restarts, spawn three workers again with these same owners and
tell each one to derive its first unfinished row from its durable ledger. Each
worker continues across r002-r005 without stopping at wave boundaries. Close a
finished worker before replacing it.

## Mandatory gates and invariants

Read `DICTIONARY_ENTRY_GUIDE.md` in full before editing. Apply all current rules,
including #0g, scaled depth, different-things-only sense splits, gloss hygiene,
opening corpus-grounded interpretation, English-first prose, banned English
words, exact speaker naming, and quote anchoring. Every changed definition must
be re-tested against the whole concordance; enrichment may expose a real second
referent. Preserve corpus inference but introduce no outside interpretation.

- Exact final KWIC verification is mandatory with `PYTHONIOENCODING=utf-8` and
  `zc.py`; the inverted/KWIC indices are discovery accelerators, never a substitute
  for verifying the final stored anchor.
- Every master actor must be exactly named. A non-master actor may remain unnamed
  only after review. Read the full case so title-owner inference does not steal a
  quoted turn from another speaker.
- Anchor dangling Chinese claims; do not delete them merely to pass a gate.
- `Buddhism` and `meditation` may not appear in dictionary English. Prefer
  `dhyana` where that is what the corpus supports.
- Do not commit or push.

## Next deterministic actions

1. Let all three owners finish r002 evidence; owner2 may continue r003 while
   periodically reviewing newly completed owner3 rows.
2. Complete all 90 cyclic r002 reviews. Route and repair every REVISE finding,
   then re-review the current entry hash.
3. Run `validate_semantic_wave.py semantic-r002` and
   `validate_semantic_reviews.py semantic-r002` (the required argument includes
   the `semantic-` prefix). At 13:37 the partial evidence validator found 82/90
   current-hash hard passes with zero failures; the review validator found 24/90
   current KEEP decisions with zero failures.
4. Root-gate all 90 current entries, run the cross-entry smell/depth/sense/alias/
   forbidden-English scan, and write a root adjudication report.
5. Merge with `node eng/tools/merge-dict-entries.js`, run it a second time and
   require zero further shard changes; run all website tests.
6. Register the accepted cohort with `register_semantic_wave.py`, recompile the
   remediation ledger, and deploy the progress dashboard.
7. Continue r003-r005 identically. Then proceed through the already documented,
   deduplicated requested/NEXT500/NEXT100/720-investigation construction queues.

## Dashboard

- Local dashboard: `C:\programmieren\readzendictprogress`.
- Public URL: `https://readzen-dict-progress.pages.dev`.
- Local data was refreshed at approximately 13:32 Europe/Zurich on this checkpoint.
- Deploy about every 30 minutes or every three accepted waves with:
  `cmd.exe /d /s /c "C:\programmieren\readzendictprogress\scripts\update_and_deploy.cmd"`

## Important repaired identity/artifact facts

- The canonical `喫茶去` ID is `t_bc9b4740f883`; old simplified-derived ID
  `t_d69c18a98053` is obsolete and merge code contains an explicit replacement.
- `大蟲` (`t_627549d4c466`) and `蝦蟆` (`t_bfa342b75391`) have `STATUS=done`.
- Do not hand-edit generated termbase/site artifacts; edit
  `terms/<id>/entry.v2.json`, gate, then merge.
