# Independent audit: zc LRU and Iriya author-ledger gates

Date: 2026-07-18  
Reviewer: c19-e8  
Scope: `zc.py` bounded file LRU and `validate_iriya_author_ledger.py` only. No authority, build, queue, registry, or lineage mutation.

## Verdict

- **zc bounded in-memory LRU: PASS.** The patch changes retention/transport only. Normalized text and compact line-map payloads survive eviction/reload exactly; configured bound 3 held after eight distinct loads; the evicted first payload reloaded from the durable cache with identical length, edge text, line-map length, and first/last line values, and became MRU. The durable cache still validates normalizer version plus source size/mtime (unless the existing explicit frozen-cache override is set), writes atomically, and falls back to source parsing when absent/corrupt/unwritable.
- **Iriya author-ledger validator: REVISE found, fixed, now PASS.** The original gate failed open for empty ledgers, arbitrary truthy `queryResolution`, wrong lane/order, and missing source/KWIC in full mode. It also performed ten full corpus scans for ten rows. The corrected gate requires a nonempty ledger with exact `reviewedCount`, sequential unique ordinals, lane modulo when `offset` is present, and a structured `queryResolution.attestedForm` actually contained in the KWIC. Missing source/KWIC/work ID now fails before full verification. Full counts are reproduced with one `zc.batch_count` traversal.

## Evidence

- Focused tests: `python3 -m unittest -v test_validate_iriya_author_ledger.py` — **8/8 PASS**. They cover good association, empty/count mismatch, ordinal/lane mismatch, the known copied-wrong-row payload pattern, structured attested-form resolution, full-mode missing fields/query, and complete mocked zc parity.
- Live cheap fixtures: batch15 offset0 **PASS 10/10**; offset1 **PASS 10/10**. The original broken offset2 ledger at SHA `b35c4df77fa797f53c01057f3ec7354c28f872cdacfd818c3f7d98e2e472948c` was independently documented as nine copied-query failures; the regression test reproduces that failure class. Its live file was repaired concurrently and now passes association at SHA `32dd3a9842c92f998c8512a6597cb0490c4f706c1e0beebc7217685356373f89`.
- Full live offset0 gate completed in 5.76 seconds with the single-traversal implementation and correctly exposed two stale stored count triples; all evidence verification/anchors/titles/work identities otherwise cleared. This is ledger drift, not a validator false failure.
- `git diff --check` — PASS.

## Audited hashes

- `zc.py`: `fb35bb30cbf3a246baa73546dc2e5a028362c4290b6f72e852e1a7d214c91198`
- `validate_iriya_author_ledger.py`: `e22db1e5ca1b3b0c392c0808182c4fd0b7aa919e91d034e713a7ec1e3f6ac9d1`
- `test_validate_iriya_author_ledger.py`: `7b12d6351b92ba36bbf6a4a4c2c61a4f36aa8bc70d53c39a684cabd50918c0b2`

## Residual boundary

These gates are mechanical. They prove row association, exact corpus existence, anchors, source labels, canonical work identities, and reproduced counts; they deliberately do not decide whether the cited cases semantically justify KEEP/PROVISIONAL/REJECT.
