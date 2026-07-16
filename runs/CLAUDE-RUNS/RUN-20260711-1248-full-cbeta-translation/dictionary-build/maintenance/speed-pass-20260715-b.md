# Speed pass — 2026-07-15 (post B1031–1040 repair)

## Measured failure cost

The six-entry repair compiled in about five seconds and the four mechanical gate
families plus 36 `zc.verify` calls completed in about twenty seconds.  Machine
work is therefore not the limiting cost.  The expensive loop is late discovery
of attribution-policy failures after prose and worksheets have already been
written.  This cohort exposed 23 such failures on the first strict-attribution
run, chiefly role labels serialized as `identified-non-master`, noncanonical
roster links, and closed-role violations.

## Immediate fast path

1. Run strict attribution on the first two completed entries of every author
   batch, before continuing the batch.  This is a schema/policy canary, not an
   independent semantic review.
2. Treat `identified-non-master` as requiring an actual personal name.  A label
   such as “the lamp-record narrator” must be `reviewed-unnamed`, `narrated`, or
   `impersonal`, with the six rungs recorded.  This single construction rule
   would have prevented 15 of the 23 late failures here.
3. Resolve roster spellings before prose drafting.  If no canonical name is in
   the roster or validated pending packet, retain the source-supplied name in a
   reviewed label and leave `MasterName` null.  Never mint a plausible English
   link and discover the broken roster form at the end.
4. Generate actor boilerplate, `DraftActorProof`, and attribution-note clauses
   from one structured actor decision.  Do not hand-fill the same decision in
   four fields.  Translation and semantic prose remain human-read work; repeated
   schema text is mechanical.
5. Run compilation and the cheap gates cohort-wide, not once per entry.  Keep
   per-entry hash receipts, but one process invocation should audit the whole
   cohort.  The corpus cache then stays warm and output is smaller.
6. Make author checkpoints semantic: every ten entries record selected works,
   excluded different-thing witnesses, exact actor decisions, and current
   hashes.  Do not checkpoint generated boilerplate that can be reconstructed.

## Quality boundary

No shortcut replaces reading the complete case or `zc.verify`.  The safe speedup
is moving deterministic rejection earlier and generating duplicated structure
from one decision.  Candidate discovery, sense adjudication, exact-turn actor
reading, corpus-bounded inference, and independent review stay unchanged.

## Expected effect

For batches resembling this repair, early actor canaries remove most repair
round-trips.  Since the actual machine gates take seconds, the gain is primarily
one fewer author/reviewer cycle per affected cohort rather than faster commands.
