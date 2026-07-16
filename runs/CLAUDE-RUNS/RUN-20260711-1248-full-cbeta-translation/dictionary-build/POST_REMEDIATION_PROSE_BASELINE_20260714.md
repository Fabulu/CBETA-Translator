# Post-remediation prose-attribution baseline — 2026-07-14

Command:

`PYTHONIOENCODING=utf-8 python3 audit_attribution.py --json`

Scope at baseline: 636 `STATUS=done` entries, 742 senses, 4,285 stored
occurrences. This is a raw detector baseline, not an adjudicated defect count;
categories overlap and false positives must be recorded rather than silently
rewritten.

## Raw results

- 2,128 total hard flags.
- 1,167 Chinese prose strings not matched to a stored KWIC (`dangling_chinese`).
  The governing instruction is to anchor these claims, not delete them merely
  to clear the audit.
- 339 generic reader-prose attributors (`vague_attributor`).
- 287 attribution notes missing the exact speaker (`note_missing_speaker`).
- 280 attribution notes missing the source title (`note_missing_source`).
- 38 invalid context-master structures.
- 17 missing attribution notes.

Positive inventory at baseline: 3,888 named occurrences, 331 reviewed unnamed
actors, 66 impersonal documentary voices, 4,268 existing attribution notes,
4,169 already anchored Chinese prose strings, and 924 context-master links.

## Completion rule

Re-run after the existing-entry semantic retrospective and process every raw
flag under `POST_REMEDIATION_PROSE_PASS.md`. Each flag must end as a current-hash
repair or an explicit false-positive adjudication. Completion requires zero
unresolved nameable-master prose, zero unanchored retained claims, exact-turn
actor/source notes, deterministic merge, and passing website tests.

