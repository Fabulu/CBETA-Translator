# Non-Iriya canary batch 2 repair receipt

- Independent review: `non-iriya-v2-semantic-canary25-batch2-independent-cross-review-d7.json`
- Reviewed ledger SHA-256: `8444711b20dcf5263e5bae964123eac0c2bd7785a380f58adade2fbe42382734`
- Repaired ledger SHA-256: `8454e1d72c394f367e98162ee4052a05c247d616641281b94bebbd9ac9f7cca1`
- Semantic changes: rows 4, 6, 7, 9, 10, 13, 16, 19, and 25 changed from KEEP to REJECT using the review's nested/family/productive-frame rulings.
- Corrected totals: 5 KEEP, 20 REJECT, 0 PROVISIONAL.
- Evidence changes: rows 6, 8, and 13 now display contiguous exact windows; all three independently pass `zc.verify` with count 1.
- Preserved: the other 16 decisions and their evidence, every identity/rank/count, and the frozen selection.
- Authority queue/build/registry/lineage mutation: none.

Focused independent recheck is required before admission use.
