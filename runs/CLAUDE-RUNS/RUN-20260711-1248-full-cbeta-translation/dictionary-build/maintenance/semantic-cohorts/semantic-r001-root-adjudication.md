# Semantic retrospective r001 — root adjudication

Date: 2026-07-14

Scope: 90 disjoint existing entries assigned across owners 1–3.

Verdict: **ACCEPT all 90 current entry hashes for merge.**

Evidence considered:

- `validate_semantic_wave.py semantic-r001`: 90/90 completed, 90/90 current-hash gate reports, zero failures.
- `validate_semantic_reviews.py semantic-r001`: 90/90 current-hash independent KEEP verdicts, zero failures after the seven revise findings were repaired and re-reviewed.
- `semantic-r001-root-gate.json`: root rerun over all 90 entries, 888/888 exact KWICs, zero exact failures, hard pass.
- Root cross-entry smell scan: 90 entries, 27 multi-sense entries, 888 occurrences; depth range 5–19 and median 9. No empty targets/explanations, semicolon or colon targets, duplicate targets, sense-without-occurrence defects, alias sets over five, or forbidden English.

The independent cycle produced substantive corrections rather than rubber-stamping: `粥飯僧` was merged to one human-role referent; `坐禪` was split between the regulated seated procedure and explicit non-postural redefinition; nested `岑大蟲` was demoted to family evidence; four owner-1 retrieval/target defects were repaired. Root accepts those final boundaries and the remaining independent KEEP verdicts because the full current-hash gate and cross-entry checks agree.

This decision does not by itself assert artifact parity or website rendering. Those gates require the post-merge deterministic artifact comparison and website test run.
