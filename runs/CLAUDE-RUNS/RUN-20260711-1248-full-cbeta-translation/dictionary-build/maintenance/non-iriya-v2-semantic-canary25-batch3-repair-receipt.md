# Non-Iriya canary batch 3 repair receipt

- Review: `non-iriya-v2-semantic-canary25-batch3-independent-cross-review-d7.json`
- Original ledger SHA-256: `2b695ae5c0f7a977a0bc1a2684676f7fb236e654c30fb4b90dc97010cd2ec038`
- Repaired ledger SHA-256: `cc058ac94c1e7aebfc453bc62c7695e240f2dda279575e738e8ac239cb3be2d2`
- Semantic repair: row 5 以拂子擊 changed KEEP → REJECT because the omitted variable object completes an instrumental argument frame.
- Corrected totals: 13 KEEP, 12 REJECT, 0 PROVISIONAL.
- Evidence repair: rows 3, 7, 14, 16, 19, 21, and 24 now use exact contiguous windows; each independently passed `zc.verify` with count 1.
- Preserved: all other 24 semantic decisions, all other evidence, identities, ranks, and counts.
- Authority queue/build/registry/lineage mutation: none.

Stop for focused independent recheck before admission use.
