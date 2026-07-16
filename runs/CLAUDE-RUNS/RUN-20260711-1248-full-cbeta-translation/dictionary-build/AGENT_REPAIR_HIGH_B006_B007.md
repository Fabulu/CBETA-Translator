# Repair report: seven high-severity b006–b007 findings

Repaired only the seven entries named in the depth audit:

1. `t_81147ad4e8bf` — Four Selections (四料揀)
2. `t_4e10d7c80fbc` — Four Modes of Illumination and Function (四照用)
3. `t_193632bffe7b` — a shout (喝)
4. `t_8f41e0da5a71` — what is its purport? (意旨如何)
5. `t_8a016f49e5b8` — ponder or think over (思量)
6. `t_cf0513be4012` — essential purport (宗旨)
7. `t_326be1e9c98a` — withered tree (枯木)

## Changes

- Reconstructed every corrupted explanation and note as readable English while retaining the ledgers' definitions, contrasts, deployment types, attribution cautions, and historical distinctions.
- Translated the Four Selections, Four Illumination-and-Function modes, Four Shouts, Yaoshan exchange, purport exchanges, transmission formulas, and withered-tree fixed phrases.
- Replaced “what is the point?” with “what is its purport?”
- Removed the classification of a shout as a ritualized teaching-shout. The entry now records only the vocal act, Linji's four comparisons, host-and-guest exchange, later glosses, and ordinary verb sense.
- Refreshed every retained numerical claim used in the rewritten prose with the current `zc` index. This corrected minor drift in the Four Selections and shout totals and preserved the audited withered-tree counts.
- Kept the distinction between Linji's original four statements and the later Four Selections label; kept the Taisho apparatus exclusion for the Four Illumination-and-Function passage.

## Evidence preservation

- All seven IDs and source terms were preserved.
- All 38 existing curated occurrences were retained; no occurrence was added, removed, or moved between senses.
- Every `RelPath`, `FromLb`, `ToLb`, `Kwic`, master key, curated flag, and every `SourceTexts` array was preserved.
- One occurrence's explanatory attribution note was rewritten solely to remove the remaining ritual classification; its evidence fields are unchanged.

## Final QA

- Seven JSON files parse successfully.
- All 38 preserved KWICs return `zc.verify(...).ok == True` with the saved bounds.
- All 38 cited paths remain on the Zen allowlist.
- Current prohibited-framing scan over reader-facing prose: zero flags.
- Bare-Chinese scan outside parenthetical references and schema-exempt fields: zero flags.
- No occurrence, status, manifest, guide, plan, termbase, merge, or corpus file was changed.
