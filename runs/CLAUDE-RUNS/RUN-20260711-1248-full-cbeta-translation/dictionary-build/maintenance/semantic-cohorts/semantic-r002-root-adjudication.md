# Semantic retrospective r002 — root adjudication

Date: 2026-07-14

Scope: 90 disjoint existing entries assigned across owners 1–3.

Verdict: **ACCEPT all 90 current entry hashes for merge.**

Evidence considered:

- `validate_semantic_wave.py semantic-r002`: 90/90 completed, 90/90
  current-hash hard-pass gate reports, zero failures.
- `validate_semantic_reviews.py semantic-r002`: 90/90 independently reviewed
  KEEP at the current entry hashes, zero failures.
- `semantic-r002-root-gate-final.json`: root rerun over all 90 entries,
  696/696 exact KWICs, zero exact failures, hard pass.
- Root cross-entry smell scan: 90 entries, 30 multi-sense entries, 696
  occurrences; depth range 7–17 and median 7. No empty targets or
  explanations, semicolon/colon targets, duplicate targets,
  sense-without-occurrence defects, alias sets over five, or forbidden English.

The independent cycle was substantive. It required inference-bearing rather
than image-only wording for `木雞`, repaired questioner/quoted-speaker ownership
across numerous public exchanges, named the `斬蛇` case rather than presenting
an imperative, and replaced several etymological `Literally ...` openings with
corpus-grounded descriptions. Root's final smell pass then removed a redundant
sixth `木雞` alias and made the `咦` exclamation target colon-free; both changes
were hard-gated and independently re-reviewed at their final hashes.

This decision does not itself establish generated-artifact parity or website
rendering. Those are post-merge gates below the source-entry acceptance gate.
