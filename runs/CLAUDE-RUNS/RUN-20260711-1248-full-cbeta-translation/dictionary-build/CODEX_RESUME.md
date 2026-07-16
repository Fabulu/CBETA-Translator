# Codex resume checkpoint — 2026-07-12 12:15 Europe/Zurich

> **NEWEST LIVE POINTER (2026-07-14):** Read `LIVE_RESUME_20260714.md` first.
> It supersedes all historical counts and describes the active collision-free
> r002-r005 retrospective queues, current ledgers, agents, and restart procedure.

## ⛔ LATEST RECONCILED STATE — 2026-07-13 (TRUST THIS OVER EVERY OLDER NUMBER BELOW)

- **621 entries are done and merged.** Waves b001–b036 and requested waves r001–r002 are complete. Do not rebuild them.
- **STOP BEFORE r003 MERGE: attribution remediation is active.** Read `ATTRIBUTION_FIX.md`, guide item 10,
  and `ATTRIBUTION_REMEDIATION_PLAN.md`. Repair the original 606-entry snapshot and separately inspect the
  15 r002 additions. Anchor dangling evidence rather than deleting it. Apply the gate to r003 before merge.
- **Attribution remediation checkpoint:** r002 is complete (15/15). In the original-606 cohort, 41 entries
  have cleared mechanics and independent semantic review; exact IDs and active cohorts are in
  `ATTRIBUTION_PROGRESS.md`. Trust that ledger and do not redo completed batches. Roster membership checking is
  deferred until the separate roster expansion lands; preserve source-attested names meanwhile.
- The merged/local occurrence sync immediately before the new queues was **7,124/7,124 exact copies verified**
  (3,562 unique anchors). Later depth/sense repairs D001–D003 are merged; all retained multi-sense entries carry
  distinguishability ledgers. Re-run current audits after each new wave rather than trusting old report totals.
- **r003 is the current research wave.** Its three five-entry batches are assigned from
  `REQUESTED_BUILD_PLAN.md`; root alone audits, registers, marks STATUS, and merges. Continue requested r003–r009
  before the new automatically discovered queue.
- The user's latest explicit direction authorizes the next expansion. `NEXT500_TERMS.md` now contains exactly
  **500 unique curated terms**, selected jointly by exact allowlist frequency and attested Chan-specific deployment;
  `NEXT500_BUILD_PLAN.md` assigns them to n001–n034. This is not a raw-frequency dump.
- `NEXT100_SAYINGS_CANDIDATES.md` contains **100 separately verified sayings, idioms, material images, and cultural
  curiosities** with 101 exact anchors; `NEXT100_BUILD_PLAN.md` assigns them to s001–s007 after the 500. Preserve
  literal image, inherited material explanation, and demonstrated Chan use as three distinct layers.
- Guide §5 item 9 is now law: candidate provenance and existing interpretations must be copied into `WORK.md` as
  research leads and receive an explicit keep/revise/reject result after concordance testing. Never silently discard
  earlier research, but never let it override the Chinese.
- Build order: finish r002–r009 → n001–n034 → s001–s007 → adjudicate all 720 rows in
  `RELATED_INVESTIGATION_BACKLOG.md` and build every independent Zen-specific headword that survives. The 720-pass
  must record KEEP, REVISE/MERGE-INTO-FAMILY, ROSTER, VARIANT, SUBSTRING/NOISE, or REJECT without losing the
  inherited lead in `NEXT500_RELATED_POOL.tsv`. Keep #0g, precept/public-interview gates, scaled depth,
  different-things-only sense splits, gloss hygiene, exact `zc.verify`, English-first prose, audit, register, and
  merge after every wave.

## ⚠ LATEST USER OVERRIDE (2026-07-12, after b022)
- The earlier "DO NOT auto-derive 500" instruction is CANCELLED by the user's latest explicit direction.
  Current order: priority requested families; b023–b036; all remaining `REQUESTED_TERMS.md`; #0g deviation pass;
  derive the next curated 500 deduplicated terms from the allowlisted corpus; begin building them under #0–#0g.

## ⚠ RECONCILED AFTER KILL (2026-07-12, ~20:55) — TRUST THIS, THE NUMBERS BELOW ARE STALE
- The process was killed mid-run and reconciled by the coordinating agent. **True state: 293 entries done AND
  merged** (validated: 293/293 parse OK, all `STATUS=done`, zero half-writes; termbase.v2.json re-merged to 293).
- Everything below that says "**218**" is STALE — do NOT trust it; do NOT redo b001–b019. **Reconcile from the
  ground truth**: scan `terms/` (every `STATUS=done` dir) against `WAVE_PLAN.md`; **waves b001–b019 are complete;
  resume at b020.**
- ⛔ **PHASE-2 DIRECTIVE (overrides the "next 500" objective): after b036, DO NOT auto-derive 500 terms.** The next
  work is the curated `REQUESTED_TERMS.md`, built under the guide's NEW §5 #0–#0g (esp. #0g the flyswatter test).
  See the big block at the top of `CODEX_HANDOFF.md`. Also apply the #0g deviation lens to pre-#0g existing entries.

## Governing objective

Finish the dictionary through b036 (~536 terms). **⛔ DO NOT auto-derive the next 500 terms — that plan is
CANCELLED (user, 2026-07-12).** After b036, STOP; the ONLY next work is the curated `REQUESTED_TERMS.md` (ewk
requests + flyswatter-test discovery gaps + Buddha-family incl. 佛), built under the guide's NEW §5 #0–#0g
standard (esp. #0g the flyswatter test: surface the deviation; Buddhist/pre-Zen figures the masters invoke are
Zen figures defined by their Zen deployment). Also apply the #0g deviation lens to the pre-#0g existing entries.
See the PHASE-2 DIRECTIVE at the top of `CODEX_HANDOFF.md`.

## Current durable state

- Rich termbase merged: **218 entries** (b001-b014 plus seven rebuilt legacy entries).
- Every merged term directory is `STATUS=done`; unmerged research drafts deliberately have no `STATUS`. `MANIFEST.jsonl` contains the 158 merged entries only.
- Latest full merged occurrence audit: **2,378 / 2,378 occurrence copies verified** (218 termbase + 218 local copies); report `maintenance/entry-audit-20260712T144350Z-dry-run.json`.
- Latest expanded conformance audit: **zero hard flags** across the 218-entry merged set and live drafts then present; report `maintenance/conformance-audit-20260712T144046Z.json`.
- Latest strict English-first audit: **zero violations**; report `maintenance/english-prose-audit-20260712T144046Z.json`.
- Latest live-tree framing report `maintenance/conformance-audit-20260712T124527Z.json` found one flag only in unmerged b011 鬼窟裏; root removed that word. The merged 158-entry subset has zero known hard flags.
- Latest live-tree strict English-first report `maintenance/english-prose-audit-20260712T124528Z.json` has zero violations across all 168 files then present, including unmerged drafts.
- All invalid roster link values were normalized; report `maintenance/master-link-normalization-20260712T083849Z.json` and ZIP backup beside it.
- Waves b009 and b010 are complete and merged. b011 is the current integration wave; b012 is partially in research.
- Every remaining wave through b036 now has a current read-only `maintenance/bNNN-preflight.json` report. b013-b035 contain 15 planned terms each and b036 contains 9; the cached pass prepared 354 terms. No future term directory was created.
- Durable three-agent allocations now exist for every wave through b036 as `BNNN_ASSIGNMENTS.md`: five terms per agent for full waves and three each for b036. These are queues only, not seeded entries.
- Existing translated-text scope located: 12 base `xml-p5t` files plus 9 community XML files. Body-only audit found 654
  flagged paragraphs/lines: practice 284, meditation 106, method/technique 68, koan 47, dualism 43, mindfulness 17,
  Mu 12, reincarnation/afterlife 7, present-moment 4, Japanese overlay 3, plus 71 enlightenment review hits. Stable
  paragraph XML IDs are preserved. Report: `maintenance/translation-framing-audit-20260712T102705Z.json`.
- Read-only translation triage is complete in `TRANSLATION_REPAIR_PLAN.md`: 654 raw flags collapsed to 403 anchored
  units; 545 confirmed hard repairs, 31 false positives to retain, and 78 source checks. Deduplicating exact base/community
  mirrors leaves 502 logical findings (417 confirmed, 25 false, 60 review). All 21 XML files parse with unique `xml:id`s.

## Rules that must survive compaction

- Preserve every verified `Kwic`, `RelPath`, and attribution unless corpus evidence disproves it.
- Every changed/new KWIC must return `zc.verify(...).ok == True`; use `PYTHONIOENCODING=utf-8`.
- English prose first; Chinese evidence only parenthetically with its English. Bare Chinese remains only in `Kwic`.
- No huatou/koan/zazen/Mu/Japanese/Korean overlay, practice/method/meditation/present-moment/dualism/doctrine framing.
- 話頭 = word/saying/remark/question/exchange; 無 = no; 分別 = distinguish; 參禪 = investigate Chan.
- SenseKey is null unless a meaning is genuinely master-specific; historical origin alone is insufficient.
- `法` is not automatically “Dharma”: translate the corpus relation (`法嗣` = lineage heir); derive `法眼`/`法身` from Chan evidence. This rule is now in both guide and handoff.
- `三昧` is translated as “complete command,” not retained as unexplained “samādhi”; named compounds take their local description from direct Chan definitions.
- `坐禪` calibration: exact mind-king/seat compounds searched above return zero, so no “seat of the mind-king” equation. Use “sitting Chan”; `禪床` = Chan seat; include Platform definitions and textual critiques.
- `WAVE_PLAN.md` was purged on 2026-07-12 of all residual imported-framing prompt text (including old koan/huatou/meditation/practice glosses); a case-insensitive framing scan now returns zero matches.

## Local maintenance scripts

- `zc.py`: corpus count/find/title/head/verify.
- `audit_sync_entries.py`: all occurrence and anchor verification.
- `audit_conformance.py`: imported-framing gate.
- `audit_english_prose.py`: strict #0c gate.
- `audit_count_claims.py`: count-claim review aid.
- `audit_count_claims.py` now tests every nearby Chinese candidate plus the headword, rather than blindly binding a count to the last single graph; this removed false mismatches in compound breakdowns. The b009 claims all match current `zc` counts.
- `audit_translation_framing.py`: read-only body scan for the 21 existing translation XML files.
- `normalize_master_links.py`, `normalize_english_pair_order.py`: already run; backups in `maintenance/`.
- `refresh_b008_prose.py`, `refresh_low_prose.py`, `purge_overlay_terms.py`: reviewed migrations already applied.
- `AGENT_WAVE_PROMPT.md`: canonical reusable research-agent prompt; fill wave/batch/list for every dispatch.
- `preflight_wave.py` / `preflight_remaining.py`: current counts, IDs, and top sources; all b009-b036 reports now exist.
- `generate_wave_assignments.py`: created the balanced `BNNN_ASSIGNMENTS.md` queues.
- `register_wave.py`: after root QA, dry-run then `--commit` to create per-term `STATUS=done` and append validated manifest rows; never run before the integration gate.
- Merge: from repo root, `node eng/tools/merge-dict-entries.js`.

## Immediate continuation

1. The 88-entry final-spec prose refresh is complete and re-merged. Batch reports: `AGENT_REFRESH_BATCH_1.md`, `AGENT_REFRESH_BATCH_2.md`, and `AGENT_REFRESH_BATCH_3.md`; combined targeted occurrence verification was **475/475**.
2. Root global integration gates passed: 128/128 JSON, 0 framing flags, 0 English-first violations, and 1,362/1,362 occurrence copies verified.
3. b009 is complete and merged: 15 new entries, 82 curated occurrences, and all 1,562 merged/local occurrence copies verify. The 13 actionable depth repairs are also complete, including 18 added occurrences and the three-sense 序 repair.
4. Waves b001-b014 are complete and merged. The merged 218-entry set has 2,378/2,378 verified occurrence copies and zero expanded conformance/English-first flags.
5. b015 A is complete (5 entries, 26/26 occurrences verified); B and C are active. b016 A is active in the third lane.

Root semantic QA changed the unmerged 法嗣 target from the doctrinally loaded “Dharma heir” to the corpus-structural “lineage heir” throughout its prose and attribution notes; evidence/anchors were unchanged.

The imported-loan sweep is complete. Initial report `maintenance/conformance-audit-20260712T135936Z.json` found 48 entries/85 flags; reviewed normalization plus `AGENT_LOAN_SWEEP_A.md`, `AGENT_LOAN_SWEEP_B.md`, and `AGENT_LOAN_SWEEP_C.md` cleared them without changing evidence. The expanded audit is now zero and corrected prose is merged.

Active agent-thread reuse at this checkpoint: `/root/b010_batch_c` is b016 A after completing b015 A; `/root/b010_batch_b` is b015 B; `/root/b011_batch_a` is b015 C. Root owns all registration, STATUS, manifest, merge, and global QA.

The translation triage is complete. `DEPTH_AUDIT_128.md` and all 13 actionable repairs are complete; report `AGENT_DEPTH_REPAIRS.md`.

## Checkpoint 2026-07-12 17:31 Europe/Zurich — b015 merged

- Registered and merged: b001-b015 plus 7 legacy stubs, 233 term entries total.
- b015 root gate: 15/15 entries passed `register_wave.py b015`; registration committed.
- Merged termbase: `C:\temp\NewTranslationrepos\CbetaZenTranslations\termbase.v2.json` (233 entries).
- Exact occurrence audit: `maintenance/entry-audit-20260712T152857Z-dry-run.json`; 2,558/2,558 occurrence copies verified, 98 reviewed headword-free legacy evidence copies.
- Expanded conformance audit: `maintenance/conformance-audit-20260712T152831Z.json`; zero hard flags.
- English-first audit: `maintenance/english-prose-audit-20260712T152829Z.json`; zero violations.
- Calibration-critical `坐禪` entry passed: Platform Record graph-by-graph definition, Nanyue tile case, Linji denial, mind-king critique, attached-sitter verse, Chan-seat word-boundary evidence, and hall arrangement; it asserts no meditation/practice/method framing and no unsupported mind-king-seat equation.
- Active workers after checkpoint: b016-B (`/root/b010_batch_c`), b016-C (`/root/b010_batch_b`), b017-A (`/root/b011_batch_a`). b016-A is already complete and durable in `AGENT_B016_A.md`.
- Next root gate: wait for b016-B/C, inspect semantically, dry-run and commit `register_wave.py b016`, merge, run occurrence/conformance/English audits, then update this checkpoint again.
- Progress after the b015 checkpoint: b016-C completed (`AGENT_B016_C.md`, 29/29 verified) and passed root semantic review; b016-B has all five entries written with 25/25 verification and is in final prose/report QA. Active agent assignments are now b016-B, b017-A, and b017-B. b016-A/C are durable and root-reviewed; b016 awaits only B's final report before registration.
- **Checkpoint 2026-07-12 17:59 Europe/Zurich — b016 merged:** b001-b016 plus 7 legacy stubs, 248 merged entries. b016 registered 15/15 after root semantic review. Exact occurrence report `maintenance/entry-audit-20260712T155833Z-dry-run.json`: 2,716/2,716 occurrence copies verified, 98 reviewed headword-free legacy evidence copies. Expanded conformance report `maintenance/conformance-audit-20260712T155748Z.json`: zero hard flags. English report `maintenance/english-prose-audit-20260712T155745Z.json` caught exactly one bare `者` in the unregistered in-progress 覷破 draft; the responsible b017-A agent was notified. No registered/merged b016 entry has a violation. Active agents: b017-A, b017-B, b017-C. Next gate: finish all three, confirm the 覷破 fix, register/merge/audit b017, update checkpoint, then immediately assign b018 A/B/C.
- Root semantic QA on completed b017-A revised prose only in `觀心` (`t_37261001c332`): translated `唯觀心一法` as “Only inspecting mind, this one thing...” instead of method-like “one act,” and `凝心入定` as “congealing mind and entering settledness” instead of imported “fixed absorption.” No evidence, anchors, counts, or IDs changed.
- **Checkpoint 2026-07-12 18:38 Europe/Zurich — b017 merged:** 263 merged entries total (b001-b017 plus 7 legacy stubs). `maintenance/entry-audit-20260712T163700Z-dry-run.json` verifies 2,898/2,898 occurrence copies, with the same 98 reviewed headword-free legacy evidence copies. `maintenance/conformance-audit-20260712T163636Z.json` and `maintenance/english-prose-audit-20260712T163634Z.json` cover the 263-entry live/merged tree; English has zero violations. Root prose-only corrections before registration: 觀心 removed method-like “one act” and imported “fixed absorption”; 入定 changed “Zen-scoped” to “allowlisted Chinese Chan corpus.” b017 registered 15/15 and merged.
- b001-b005 semantic/depth audit is complete: `AGENT_DEPTH_AUDIT_B001_B005.md`, 54 clean, 5 high and 16 medium repairs. High: 異類中行, 平常心, 佛性, 頓悟, 截斷眾流. This proves clean mechanical audits do not substitute for semantic/depth review.
- b006-b010 read-only audit is in progress and durably checkpointed through b009 in `AGENT_DEPTH_AUDIT_B006_B010.md`; found multiple #0c/corrupted-prose and framing repairs. Agent is closing b010 before switching to b018-B.
- Active wave: b018-A (`/root/b011_batch_a`), b018-C (`/root/b010_batch_c`), with b018-B to start on `/root/b010_batch_b` immediately after its b010 audit report. Critical guards sent for 頓漸, 打坐, 本參, and the Xijiang-water case.
- Root began high-severity prior-entry repairs while b018 agents research. Prose fixes saved in: 異類中行 (karma→recompense, Guizong answer not “gloss,” vocal response described, English-first collocations, Caoshan key null, translated target); 平常心 (道不用修→“does not need refining,” English-first title/attribution); 佛性 (Zhaozhou key null, 業識性→“action-consciousness nature,” removed defensive sutra framing, all phrases English-first); 頓悟 (修 rendered “refining,” removed cultivation-system/Zongmi overlay); 截斷眾流 (詮表不及→“cannot be reached by formulation,” English-first attribution). Still required before this repair set is closed: reorder 平常心 and 截斷眾流 technical senses first; fold 佛性 dog-case deployment into the corpus-wide sense; add/justify the missing 平常心 ordinary-answer anchors; run parse/conformance/English/zc occurrence audit and remerge. Do not lose these remaining structural tasks.
- **Checkpoint 2026-07-12 19:31 Europe/Zurich — b018 registered, merge intentionally deferred:** all 15 b018 entries passed dry-run and commit validation; b018 is registered. Do NOT run the merge until the three active repair agents finish, because they are editing already-done entries and a merge could capture a partial write. b018 batch totals: A 28/28 verified, B 28/28, C 32/32 (88/88). Root semantic review passed 頓漸, 落空, 打坐, 本參, and the West River case under #0b/#0c. Current merged termbase remains 263 entries until the repair gate; after repairs, merge should produce 278 entries.
- Active agents at this checkpoint: `/root/b010_batch_b` structural repairs 平常心/佛性/截斷眾流; `/root/b010_batch_c` seven high corrupted b006–b007 entries; `/root/b011_batch_a` twelve remaining medium b001–b005 entries. Root has additional saved repairs and exact remaining tasks in `PRIOR_ENTRY_REPAIR_LOG.md`.
- **Checkpoint 2026-07-12 20:10 Europe/Zurich — b018 + prior repairs atomically merged:** 278 merged entries. Combined pre-merge gate initially found 7 framing and 6 English-first flags; `AGENT_REPAIR_COMBINED_GATE.md` fixed all 10 affected entries with 54/54 witnesses preserved. Clean reruns: `maintenance/conformance-audit-20260712T180601Z.json` and `maintenance/english-prose-audit-20260712T180559Z.json` (no flags reported). Post-merge exact audit: `maintenance/entry-audit-20260712T180858Z-dry-run.json`, 3,080/3,080 occurrence copies verified; 98 reviewed headword-free legacy evidence copies unchanged.
- Prior repair reports now complete: `AGENT_REPAIR_STRUCTURAL_B001_B005.md` (20/20), `AGENT_REPAIR_MEDIUM_B001_B005.md` (64/64), `AGENT_REPAIR_HIGH_B006_B007.md` (38/38), `AGENT_REPAIR_REMAINDER_B006_B010.md` (70/70 including new exact 活句 witness), plus root edits logged in `PRIOR_ENTRY_REPAIR_LOG.md`. Evidence/links were preserved except the explicitly authorized added 活句 occurrence and two new 平常心 ordinary-answer witnesses.
- Count-claim audit `maintenance/count-claim-audit-20260712T175359Z.json`: 603 claims, 198 marked `mismatch-or-wrong-candidate`. This is an OPEN TRIAGE task: report mixes true stale counts with candidate-association false positives (for example a 一喝 paragraph's separate 喝 count is associated with 一喝). Do not auto-rewrite all 198. Triage deterministically by exact candidate/context, then update only confirmed stale claims.
- Read-only semantic audit checkpoints: `AGENT_DEPTH_AUDIT_B011_B015.md` complete through b012 (b011: 14 clean/1 low; b012: 13 clean/1 low/1 medium 黑漆桶 imported awakening framing); `AGENT_DEPTH_AUDIT_B016_B018.md` complete for b016 only; b017/b018 unaudited there. These remain open for later continuation/repair.
- Active agents now: b019-A `/root/b010_batch_b`, b019-B `/root/b010_batch_c`, b019-C `/root/b011_batch_a`. Next root gate is b019 registration/merge/audits; then b020. All three slots are occupied.
- Progress after 278-entry checkpoint: root repaired read-only audit findings 狗子無佛性 (question grammar), 休去歇去 (specific exclusion reason), and 黑漆桶 (removed unenlightened-mind/awakening/“Zen bend” overlay; retained concrete labels, comparisons, morphology, verbs, and Rujing continuation). These done-entry edits are not yet remerged; next b019 merge will include them.
- Count auditor improved to distinguish nearby candidate phrases from unrelated earlier Chinese in the same paragraph. Rerun `maintenance/count-claim-audit-20260712T182330Z.json` over 281 live entries (includes 3 in-progress drafts): 609 claims, 122 `mismatch-or-wrong-candidate`, 76 `no-near-candidate`, down from the unsafe undifferentiated 198. Still do not auto-rewrite. `/root/b011_batch_a` is temporarily triaging b001–b005 claims read-only in `COUNT_CLAIM_TRIAGE_B001_B005.md` while waiting for b019-A/B.
- b019-C complete: 5 entries, 33/33 exact verified, `AGENT_B019_C.md`; root inspected 老婆禪 and 直心. b019-A/B active with targeted cautions sent for 攝心/明心見性 and 觸目菩提/掛搭.
- b019 is registered (15/15) but merge is intentionally deferred while b016 done entries are being repaired. b019 totals: A 27/27, B 30/30, C 33/33; 90/90 batch witnesses. Root semantic review passed 攝心 (with 靜坐 tightened to “sit still”), 明心見性, 觸目菩提, 掛搭, 老婆禪, and 直心.
- `AGENT_REPAIR_HIGH_B016.md` complete: 印可 unsupported seal/realization/“Zen bend” theory removed; 王老師 keyed and linked to exact roster `Nanquan Puyuan`; 本性空 40/30 reconciled with three exact representative deployments added. 19/19 occurrences verified. `/root/b010_batch_c` still owns b016 medium/lower repairs; do not merge until it finishes.
