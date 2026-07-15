# F005 speed and bottleneck audit

Generated 2026-07-15T18:58:00.850162+00:00

## Result

- Full-case human semantics and repair review dominate elapsed time. Compilation and authoring-risk lint are cheap.
- Durable rework findings: actor/turn 106, unsupported-or-unanchored-prose 38, sense/depth 14, other 34.
- New `authoring_risk_preflight.py` caught all seven known 卓一下 action/performer defects in 0.054 seconds and produced zero flags on the accepted 語言 canary.
- Composite phase means: exactKwic 0.459s, attribution 6.568s, publicFeedback 2.996s, depthSense 5.55s, countClaims 1.362s, workSourceValidation 1.545s, corpusBaseline 1.291s, frozenHistoricalTerms 2.243s, attributionPackets 4.086s.
- Compiler benchmark: 1.032s; semantic JSON output identical: True.

## Safe process change

Run the risk preflight on evidence drafts before compilation/review. Its findings require a human full-case decision. It never writes entries, assigns speakers, or changes the final schema.
