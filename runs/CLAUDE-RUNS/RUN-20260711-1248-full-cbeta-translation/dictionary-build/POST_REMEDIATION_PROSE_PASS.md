# Post-remediation reader-prose attribution pass

Status: **mandatory after the full existing-entry semantic retrospective and
before claiming the existing dictionary repaired.** This is a separate global
pass even though current waves enforce the rule entry by entry.

## Purpose

Correct actor metadata is not enough. The reader-facing `Explanation`, `Note`,
and `AttributionNote` prose must tell the reader who stands behind each quoted
or paraphrased claim. A named master may not disappear into “a master,” “the
master,” “one master,” “a teacher,” “the text,” or passive voice.

## Hard rules

1. For every quoted or paraphrased claim, map the prose claim to an anchored
   occurrence and its exact-turn actor.
2. If that actor is a master, name the master in the prose, using the canonical
   roster spelling. A named master is never rendered generically.
3. If the exact actor is a source-attested named non-roster person, name that
   person and state the relevant role when helpful; do not promote the person
   to the master roster merely to make a link.
4. If a non-master actor remains unnamed after the six-rung ladder, say
   explicitly “an unnamed monk,” “an unnamed visitor,” “the compiler,” or the
   other reviewed role. Do not imply that the context master spoke the token.
5. Preserve context separately: “an unnamed monk asks X; Zhaozhou answers Y” is
   correct when those are the exact turns. The book owner, quoter, narrator,
   respondent, quoted speaker, and event actor may all differ.
6. Every Chinese quotation in prose remains anchored. Improve attribution
   rather than deleting evidence.
7. Re-run exact KWIC, actor, English-first, forbidden-English, and website
   rendering gates after prose changes. Any changed entry hash reopens formal
   approval and requires current-hash review.

## Scope and completion evidence

- Scope is every `terms/*/entry.v2.json`, including entries created after this
  file was written. No historical cohort is grandfathered.
- Produce a machine report listing each generic-speaker phrase and its
  adjudication: `REWRITE-NAMED`, `REWRITE-REVIEWED-UNNAMED`, or `FALSE-POSITIVE`.
- Completion requires zero unresolved generic references to a nameable master,
  zero prose claims without an anchored occurrence, current exact-turn actor
  metadata, a deterministic merge, and passing website tests.

