# Blind clean-regeneration arm — 2026-07-14

## Isolation and blindness

- Drafting used only the five fixed headwords, `DICTIONARY_ENTRY_GUIDE.md`, `ATTRIBUTION_FIX.md`, `ACTOR_AUDIT.md`, allowlisted corpus/index data, `masters.json`/`master-dates.json`, and current validators.
- No `terms/*/entry.v2.json`, repair ledger, or repair output was opened before the five clean drafts were complete.
- All created files are confined to `maintenance/regen-benchmark-clean/`; no live entry or merged artifact was changed.

## Raw timing

Wall-clock benchmark work began at 17:47:26+02:00 and the final clean hard attribution gate passed at 17:58:42+02:00: **11m16s** for the five-entry arm after specifications had been read. Including audit/spec discovery and setup, the isolated worker interval was approximately **17m**.

| phase | measured wall time | result |
|---|---:|---|
| batched indexed discovery, five headwords | 48.78s | 85% CPU, 187484 KiB max RSS; shared index scan alone 36.106s |
| corpus count + expanded reading-packet extraction | 73.63s | 39% CPU, 328216 KiB max RSS |
| first draft materialization | 3.28s | scripted serialization only; excludes the human reading/reasoning between captured commands |
| exact `zc_batch` validation, iteration 1 | 2.08s | 20/20 exact, zero failures |
| attribution gate, iteration 1 | 4.07s | 9 hard failures |
| exact `zc_batch` validation, iteration 2 | 1.88s | 20/20 exact, zero failures |
| attribution gate, iteration 2 | 2.55s | 3 hard failures |
| attribution gate, iteration 3 | 4.18s | zero hard failures |
| final custom ID/roster/headword/banned-English check | 3.58s | zero failures |

The packet mechanically opened 74 candidate passage windows in 9 distinct source files. Twenty retained evidence rows (19 occurrences plus one ClaimAnchor) received exact source verification and exact-actor review. Lookup count: one five-term indexed batch, five corpus counts in one persistent process, 20 source-file probes, then 20 exact validations per validation iteration.

## Output and coverage

| class | entry | senses | occurrences | claim anchors | clean-arm note |
|---|---|---:|---:|---:|---|
| thin/easy | 合頭語 | 1 | 3 | 0 | independent Yunmen, Chuanzi, and Foyan deployments |
| deep/keystone | 和尚 | 1 | 4 | 0 | title/address sense; one reviewed unnamed questioner |
| multisense | 棒 | 2 | 5 | 0 | split physical staff from counted staff-blow |
| actor-complex | 正法眼 | 1 | 3 | 0 | named Linji, compiler narration, reviewed unnamed questioner |
| dangling claim | 本來無一物 | 1 | 4 | 1 | alternate line stored as ClaimAnchor, not depth-bearing occurrence |

Final machine state:

- 5 JSON drafts parse.
- Deterministic IDs match all five fixed IDs.
- 20/20 evidence rows pass `zc.verify` with exact stored line bounds.
- Attribution audit: 0 hard failures; 15 named exact actors, 2 reviewed-unnamed actors, 3 narrated compiler actors, 25 contextual-master links.
- Every occurrence contains its exact headword; the non-headword alternate line is a ClaimAnchor.
- No non-roster names and no forbidden English `Buddhism`/`meditation`.

## Failures and iterations

Iteration 1 exposed nine hard failures: two prose vague-attributor phrases, two missing closed actor roles caused by draft-constructor argument errors, two missing narration grammar fields from the same error, and three narration notes that did not use the gate's accepted narration wording. Iteration 2 reduced this to the three note-wording failures. Iteration 3 passed.

## Quality cautions / benchmark verdict

This arm is mechanically clean but **not yet “merge-ready” under the audit definition**, because it has not received the required independent blind semantic review and KEEP verdict. The high-frequency `和尚` draft is intentionally conservative and cannot establish exhaustive deployment coverage from four curated rows. `正法眼` needs an independent reviewer to challenge whether compound-heavy `正法眼藏` evidence sufficiently supports the shorter article. The `棒` object/event split is plausible and mechanically well evidenced, but also needs the same independent sense audit.

Therefore this arm does **not authorize replacing the live dictionary yet**. It proves that five complete clean drafts can reach zero current mechanical hard failures in roughly 11 minutes of end-to-end worker time once specifications are loaded, while also showing that advertised “2–3 second regeneration” figures omit the dominant research/review work. A speed comparison requires the repair arm's matched end-to-end timing and identical independent review.

## Reproducibility

Raw outputs and timings are in `logs/01-indexed-kwic.txt`, `logs/02-reading-packet-time.txt`, `logs/03-drafting.txt`, and `logs/04` through `07`. The expanded source packet is `packets/02-reading-packet.json`; the exact draft constructor is `build_clean_drafts.py`; draft JSON is under `drafts/<id>/entry.v2.json`.
