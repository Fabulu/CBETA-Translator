# Full occurrence actor re-audit

Status: mandatory, active, crash-resumable. Governing sources, in order:
`DICTIONARY_ENTRY_GUIDE.md`, `ATTRIBUTION_FIX.md`, `ACTOR_AUDIT.md`.

## Decision boundary

Humans/models read the complete passage and answer exactly one question: who
utters the stored headword? Tools may find passages, widen context, show titles,
locate parallels, validate roster spellings, detect role/schema defects, and
run `zc.verify`. No heuristic, title parser, regex, or classifier may assign or
approve the actor.

This is deliberately a reading pass with mechanical guardrails, not a
mechanical attribution pass with occasional reading.

## Per-occurrence durable row

Each row records entry ID, occurrence ID/stable fingerprint, former
`MasterName`, exact KWIC, complete-case/context locator, reader decision,
decision evidence, new `MasterName`, `ActorAttribution`, closed-role
`ContextMasters`, headword-in-KWIC result, `zc.verify` result, reviewer,
entry-before hash, entry-after hash, and disposition.

Allowed decision classes:

1. named master utters the headword;
2. another person utters it (named master; `identified-non-master` with the
   source-attested personal name; or six-rung `reviewed-unnamed` non-master);
3. compiler/narrator uses it (`narrated`);
4. no human actor (`impersonal`).

Allowed roles are exactly the seventeen in `ACTOR_AUDIT.md`. Any additional
description belongs in `GrammarEvidence`.

## Wave protocol

Partition entries into three collision-free owner queues. Owners checkpoint
after every entry and append every former-name/new-actor discrepancy to a
separate findings ledger. Independent reviewers read the passage again; they do
not approve from the owner's explanation. A changed entry hash invalidates its
prior review.

Sequence the audit behind each entry's semantic retrospective: an actor audit
performed before a later semantic rewrite would immediately become stale. While
semantic r003 is active, actor owners may process only already-remediated r001
and r002 IDs; r003 becomes eligible after its current hashes merge and register,
then r004, and so on. A later edit to any audited entry hash automatically
reopens its actor row and independent review.

Before a wave merges, require: every occurrence has a reader decision; every
named attribution was positively checked; every null has the correct complete
exception record; every KWIC contains its headword; every changed/re-cut anchor
passes `zc.verify`; roles are closed-vocabulary; prose names the recovered actor
or explicitly names the reviewed role; dangling quotes remain anchored.

Then run the normal semantic/preflight gates, merge twice deterministically,
test the website, register the wave, and update the dashboard.

## Completion report

Report total occurrences read, named-master utterers, other utterers, narrated,
impersonal, reviewed-unnamed, KWICs re-cut/replaced, verification results, and
every occurrence where the former `MasterName` was not the headword utterer.
