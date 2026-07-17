# Speed/bureaucracy audit: 269-entry repair-to-release workflow

## Bottom line

The quality rules do not require an 8–13 hour release tail. The current mechanical final gate took **190 seconds** for all 269 entries; its slowest component was `audit_depth_sense.py` at **147 seconds**. The long estimate is therefore dominated by repeated human rereads, repair/review handoffs, and receipt churn—not exact verification or installation. A slim, fail-closed path should fit **2–3 hours** if only materially changed entries receive the required independent semantic reread and every unchanged entry keeps its already hash-bound review.

## What the current path actually does

1. Three construction manifests and three 100-row independent reviews are mapped by `maintenance/build_investigation_next300_semantic_review_index.py`.
2. Repairs are applied through many one-off `repair_*`, `postfix*`, `finalize*`, and receipt-writer scripts.
3. Nearly every small repair cohort runs `run_cohort_gate.py`, producing another focused gate and usually another `attribution_packet.py` report.
4. `maintenance/investigation_next300_final_acceptance_269.py` revalidates manifests, semantic-review files, packet/bundle hashes, current entry hashes, corpus hash, roster hash, then runs the full 269 gate.
5. `maintenance/investigation_next300_atomic_install_269.py` rechecks the accepted plan and source/evidence hashes, installs with rollback, merges artifacts, and restores on failure.

Observed bureaucracy: **889 gate JSON files (~475 MB), 292 attribution-packet reports, 158 ledgers, and 1,385 timestamped count-claim reports**. `audit_count_claims.py` writes a new standalone report on every invocation even though `run_cohort_gate.py` already embeds its result.

## Duplication and avoidable serialization

| Current duplication | Evidence | Slim replacement |
|---|---|---|
| Re-reading unchanged full cases in author, repair, feedback, cross-review, and final-review rounds | The final semantic index contract already distinguishes candidate review, targeted post-build review, and mechanical current-byte seal; only two IDs were registered with post-build reviews in the inspected index. | Read once for construction and once independently **only when meaning changed**. Mechanical actor/KWIC/prose-format changes get machine gates, not another semantic reread. |
| Rebuilding attribution packets after exact-turn reading | Final 269 packet reused 242 entries but rebuilt 27; it still took 28 seconds and failed on one identity despite the separate exact and actor audits. | Generate `attribution_packet.py` before human actor review; cache by `packet_input_sha256`; at final release validate fingerprints only. Rebuild packets solely for entries whose occurrence coordinates/actor fields changed. |
| Exact/headword identity checked by `zc_batch.verify_entries`, `attribution_packet.py`, and parts of `audit_attribution.py` | `run_cohort_gate.py` runs all three. | Let `zc_batch.verify_entries` own existence/lb/count; let `audit_attribution.py` own actor schema/roster; packet generation supplies context to the reviewer but is not a third corpus verifier. |
| Repeated JSON loading, hashing, corpus/work checks across subprocesses | `run_cohort_gate.py` launches nine auditors; acceptance then hashes the same entry/evidence files again. | One in-process release auditor loads each entry once and shares entry hashes, work IDs, corpus SHA, forbidden-language scan, source validation, depth inputs, and count cache. Keep the installer's second hash check because it is the transaction boundary. |
| Timestamped count reports plus embedded gate output | 1,385 `count-claim-audit-*.json` files; current full gate count audit itself took only 10 seconds. | Add `--output`/`--no-standalone-report` to `audit_count_claims.py`; under a cohort gate, embed only mismatches and summary. |
| Full child stdout and full nested reports copied into each gate | Gate JSON volume is ~475 MB. | Gate receipt stores summaries, failure rows, entry hashes, and content hashes/paths to detailed reports; omit successful per-occurrence duplication. |
| Many one-off repair → postfix → finalize → receipt stages | Recent sequence includes `repair_release_cohort3.py`, two postfix scripts, receipt writer, ledger finalizer, focused gate, then later merge scripts; similar chains exist for cohorts 1/2 and extra8/9. | One declarative repair batch: expected-before SHA, patch operation, expected semantic-impact flag, validation result, after SHA, and atomic write. A single generated ledger replaces bespoke postfix/finalize scripts. |
| Re-running the entire strict gate after each tiny repair | 124 focused gate reports exist; full packet generation and slow depth checks recur. | During repair run a delta gate on changed IDs. Run the complete 269 gate once after the repair queue reaches zero. |

## Slim quality-preserving pipeline

1. **Freeze scope once (1–2 min).** Build the 269 manifest/index; record corpus SHA, `work_id` map SHA, protected lineage SHA, entry before-hashes, and authoritative review bindings in one immutable release state.
2. **Repair in three parallel lanes (60–100 min).** Each lane receives large stable cohorts. Authors read a case only when the defect is semantic/actor-specific. Every 50 entries checkpoint one compact delta ledger. A cheap local preflight catches schema/templates, forbidden words, exact headword presence, duplicate KWIC identity, role vocabulary, and source-label shape before handoff.
3. **Independent delta review (35–60 min, parallel with later repair work).** A reviewer rereads every occurrence only for entries marked `semantic-impact=true`. The receipt is bound directly to the resulting current SHA. Actor-only changes still require complete-turn actor adjudication, but not a second definition review unless the actor change alters the claimed deployment.
4. **Delta mechanical gate (5–15 min total).** Run exact `zc` verification, actor audit, distinct-work/depth/count checks, corpus baseline, and template/public-feedback checks only on changed IDs. Rebuild attribution packets only for changed packet fingerprints.
5. **One final cohort seal (3–5 min).** Run the complete 269 gate once. Preserve: exact KWIC/anchor verification; strict actor/roster check; frozen corpus and distinct `work_id` gate; protected lineage hash; semantic-review receipt coverage; forbidden-language/template/public-feedback checks. Do not emit successful occurrence bodies repeatedly.
6. **Accept and install (5–10 min).** `investigation_next300_final_acceptance_269.py` emits the plan. `investigation_next300_atomic_install_269.py` retains its independent source/evidence rehash, lock, same-filesystem staging, rollback backup, and reverse restore.
7. **Artifact integrity (2–5 min).** After `merge-dict-entries.js`, assert that every installed ID appears in `termbase.v2.json`, the index, and the correct shard; compare each published entry's substantive payload/hash to the live source; recheck corpus and lineage hashes. Roll back on failure.

## Exact consolidation targets

- Extend `run_cohort_gate.py` with `--mode preflight|delta|release`, `--packet-changed-only`, and compact receipts. Keep it as the single orchestrator.
- Consolidate overlapping entry scans from `audit_depth_sense.py`, `audit_count_claims.py`, `audit_work_source_validation.py`, `audit_corpus_baseline.py`, and the local forbidden scan into one shared-process `audit_release_entries.py`; preserve their existing rule functions and outputs.
- Keep `audit_attribution.py` as actor authority and `zc_batch.verify_entries` as exact-span authority. Demote `attribution_packet.py` from repeated validator to cached review-context producer.
- Replace cohort-specific `repair_*`/`postfix*`/`finalize*` scripts with one SHA-guarded `apply_repair_batch.py` driven by declarative JSON operations.
- Fold `build_investigation_next300_semantic_review_index.py` into acceptance as an incremental registry update, or run it once after every semantic receipt—not after mechanical repairs.
- Keep acceptance and installer separate; their second hash boundary and rollback are valuable and cheap.

## Estimated saving

- Mechanical gating: from repeated 2–3 minute whole/focused gates to delta checks plus one final gate: **save ~30–75 minutes** in a repair-heavy tail.
- Receipt/report generation and agent handoffs: **save ~30–60 minutes**.
- Eliminate unjustified rereads of semantically unchanged entries: the decisive saving, **~4–8 hours** depending on how many of 269 were being reread again.
- Expected end-to-end repair tail with three lanes: **about 2–3 hours** (60–100 min repairs, 35–60 min overlapping independent delta review, 15–25 min gates/install/artifact integrity), assuming the remaining material-change set is modest rather than all 269.

The non-negotiable two-reader rule remains intact for material semantic changes. The shortcut is to stop treating hash bookkeeping, actor mechanics, and unchanged meanings as reasons for a third or fourth complete-case semantic reading.
