# Iriya offset-2 batch 5 — focused evidence-repair receipt

- Review authority: C cross-review SHA-256 prefix `0e3c5c`.
- Repaired ledger: `maintenance/iriya-manual-batch5-offset2-ledger.md`.
- Scope: canonical index 137 / queue 138 / `t_821c947ecbf5` / `知恩者少`, Evidence 2 only.
- Defect: the prior D48 KWIC omitted intervening text and was therefore not an exact contiguous corpus span.
- Repair: replaced it with the supplied contiguous `D/D48/D48n8939.xml` span `0023b03–0023b06`, including the following `進云` response context.
- Exact verification: `zc.verify` returned `ok: true`, `fromLb: 0023b03`, `toLb: 0023b06`, `count: 1`.
- Preserved without change: KEEP disposition, component unit, deployment reason, exact frequency, first witness, complete-case window, and all other nine rows.
- Mutation boundary: author-ledger evidence repair and this receipt only; no entry, build, queue, or lineage action.
- Status: stopped for focused recheck.
