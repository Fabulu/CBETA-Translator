# Blind clean-regeneration benchmark — revision round 2

## Result

All five temporary drafts were rebuilt after the independent review. The complete current mechanical bundle now passes against frozen allowlist v2 baseline `42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a` (494 files / 487 works). No live term, repair ledger, or generated termbase was read or changed.

Final temporary cohort:

| entry | exact headword occurrences | independent works | floor | result |
|---|---:|---:|---:|---|
| 合頭語 | 6 | 5 | 6 | hard pass |
| 和尚 | 10 | 7 | 10 | hard pass |
| 棒 | 11 | 8 | 10 | hard pass |
| 正法眼 | 8 | 7 | 8 | hard pass |
| 本來無一物 | 6 | 5 | 6 | hard pass, plus one non-depth ClaimAnchor |

## Revision timing

- Full round-2 worker interval, from review intake/candidate planning to the final frozen-baseline gate: approximately **17 minutes** (18:13–18:29 Europe/Zurich).
- Full-context candidate sweep: **373.57s** wall, 58% CPU, 512368 KiB max RSS. This was the dominant phase and is the passage-reading/extraction work omitted from round 1.
- Failed first draft materialization after the standalone filter exposed an index assumption: **3.61s**.
- Successful revised draft materialization before WORK ledgers: **4.91s**.
- Draft regeneration with WORK ledgers/counts: **12.50s**.
- Initial depth/public run: **19.97s**, failed because the public audit was passed relative rather than absolute temp paths.
- Second depth/public run: **15.31s**, found one real content failure: forbidden loanword `dharma` in 正法眼.
- Final complete bundle after baseline freeze and the English repair: **58.37s**.

These are genuine wall timings preserved in `logs/round2-candidate-time.txt`, `round2-draft-build.txt`, `round2-depth-public-iteration*.txt`, and `round2-complete-gate-final.txt`.

## Independent-review defects repaired

1. Every entry now meets its current v2 frequency-scaled floor and exceeds the four-work spread floor.
2. Every sense marked `multi-source` has at least two independent works; `audit_work_source_validation.py` reports 6/6 valid multi-source senses.
3. All four exact-actor errors were corrected after complete-case reading:
   - 和尚: the `徑山欽和尚` row is compiler narration, not the later unnamed questioner.
   - 棒 physical implement: Dongshan Liangjie utters the `白棒` statement; it is not compiler narration.
   - 棒 counted blow: Changqing Huileng utters `一頓棒`; he is not merely contextual.
   - 正法眼: Bodhidharma directly utters the entrustment line to Huike after `告之曰`; it is not narration.
4. 正法眼 was rejected and rebuilt. All eight depth rows contain standalone `正法眼`; no `正法眼藏` substring row remains. The longer compound is explicitly excluded in its note and WORK family adjudication.
5. All five entries now have `WORK.md` ledgers covering definition search, deployment inventory, omission audit, inference/falsification, family control, search probes, opening verdict, exact-turn review, and source spread. 棒 additionally records sense-target distinguishability.
6. Search aliases are populated. The near-duplicate alternate target in 本來無一物 was removed.
7. The complete gate caught and removed `dharma`; 正法眼 now displays as “eye of the correct teaching.”

## Exact final gate outputs

The authoritative raw transcript is `logs/round2-complete-gate-final.txt`.

- `zc_batch.py verify-entries`: **42/42 evidence rows exact; 0 failures**.
- `audit_attribution.py --json`: **41 occurrences + 1 ClaimAnchor; 0 hard failures**. Actor states: 32 named, 5 narrated, 1 identified non-master, 4 reviewed unnamed; 46 contextual-master links. Four names remain explicitly reported as deferred roster expansions, not hidden hard failures.
- `audit_work_source_validation.py`: **5 entries, 6 senses, 6 multi-source; 0 hard failures**.
- `audit_corpus_baseline.py`: **0 hard failures** against the frozen v2 SHA.
- Current `audit_depth_sense.audit_entry` logic on the temp paths: **5 audited, 0 hard failures**. Non-hard review flags remain for the broad single-sense 和尚/正法眼 concordances and the required 棒 two-sense paraphrase challenge; their WORK ledgers record the adjudications.
- Current `audit_public_feedback.audit` logic on the temp paths: **5/5 pass, 0 flagged**.
- Forbidden English check inside the same bundle: no `Buddhism` or `meditation`; the depth gate also reports no banned framing.
- Attribution packets regenerated: 41 occurrences, all 41 still require exact-turn semantic review (`tierACandidates: 0`, `reviewRequired: 41`). This is expected: packet generation does not replace human review.

## Benchmark implication

Round 1's 11-minute figure was not a valid clean-rebuild time because all entries were under floor and four actor decisions were false. Round 2 shows the added cost of the omitted controls: about 17 more minutes for only five entries, including a 6m14s candidate/full-context extraction sweep, WORK provenance, correction iterations, and a full gate. The revised cohort is mechanically hard-clean, but a second independent semantic KEEP review is still required before calling it merge-ready or using it to authorize a whole-dictionary restart.
