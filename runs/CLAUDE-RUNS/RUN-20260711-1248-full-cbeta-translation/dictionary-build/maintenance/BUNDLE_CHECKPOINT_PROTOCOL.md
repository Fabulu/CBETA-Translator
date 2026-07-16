# Bundle checkpoint protocol

Large attribution bundles must be crash-resumable from disk. Chat messages are not durable state.

After every completed source (quick queue) or every completed ten-occurrence chunk (full-ladder queue), the worker must:

1. Finish signed compile, strict dry-run, apply, focused actor/source gate, JSON parse, and exact `zc.verify` for that unit.
2. Write or atomically replace `maintenance/bundle-ledgers/<bundle-id>.json`.
3. Append the human-readable finding to `maintenance/bundle-ledgers/<bundle-id>.md`.
4. Record: bundle ID, worker, source/chunk ID, occurrence keys, exact actor decisions, overrides, context figures, verification counts, completed units, failed/deferred units, remaining units, and `nextUnit`.
5. Never mark a unit complete before its files and gates are complete. A failure records the error and leaves that unit pending; safe independent later units may continue.

Resume rule: read the ledger first, verify the last completed unit still matches entry files, and start at `nextUnit`. Never redo ledger-complete units unless their file hashes changed.

The root agent combines and merges only ledger-complete units. After merge, it records the merge result and deterministic second-merge result in the same ledger.
