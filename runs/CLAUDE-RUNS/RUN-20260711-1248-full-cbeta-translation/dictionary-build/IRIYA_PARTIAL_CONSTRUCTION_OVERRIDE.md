# Iriya partial-cohort construction override

Status: **authoritative scheduling override, 2026-07-18**

The project owner explicitly directed construction to begin from already vetted Iriya
candidates before the remaining admission queue is complete. This supersedes only the
whole-queue scheduling quarantine in `DICTIONARY_ENTRY_GUIDE.md` item 8c.14 and
`CODEX_IRIYA_QUEUE_PROMPT.md`. It does not weaken any semantic, provenance, corpus,
depth, attribution, exact-evidence, independent-review, or publication gate.

A partial cohort is construction-eligible only when every row:

1. appears in the SHA-bound trusted registry;
2. has a buildable disposition (`KEEP`, `KEEP (component)`, `KEEP (couplet)`, or
   `PROVISIONAL`);
3. is backed by the registry's recorded independent acceptance;
4. resolves to one unique construction headword and deterministic ID;
5. does not collide with an authoritative fresh-build or installed entry; and
6. is frozen in a lane manifest before any authoring write.

Unreviewed and rejected rows remain locked. Iriya/Koga remains a selection source only:
no definition, gloss, example, interpretation, or sense may cross the provenance
firewall. Authors derive every substantive field from the frozen Chan corpus and follow
`DICTIONARY_ENTRY_GUIDE.md` in full. The first five entries in each lane are a complete
contract canary. Durable checkpoints occur after every 50 completed lane entries.

Partial construction does not authorize ReadZen production publication. Local merge and
installation checks, backup commits, and progress-dashboard updates remain permitted.

