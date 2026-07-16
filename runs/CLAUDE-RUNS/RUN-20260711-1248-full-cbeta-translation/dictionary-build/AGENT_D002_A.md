# d002-A completion report

Implemented the researched depth enrichment for 禮拜, 衲僧, 世尊, 便打, and 喝一喝. No `STATUS`, manifest, merged termbase, or integration file was changed.

## Results

- 禮拜 (`t_1d3473614976`): 9 occurrences across 4 source texts. Revised the English target to “to make a ritual bow or prostration” and added emperor, return-to-assembly, and explicit bow-rise-action deployments.
- 衲僧 (`t_acccac1051a4`): 9 occurrences across 6 source texts. Added the patch-robed monk's eye, gate, conduct, and air deployments, foregrounding the public standard without turning compounds into new kinds of monk.
- 世尊 (`t_c875e45fbb9d`): 10 occurrences across 10 source texts. Preserved the flower-sermon and mount/leave-seat core, then added the birth case, Yunmen's explicitly attributed appraisal, woman-in-composure case, grass-temple case, and color-changing-jewel question. The World-Honored One is defined as the Zen-invoked figure through these Chan deployments, not through outside hagiography.
- 便打 (`t_8879b278cd83`): 9 occurrences across 6 occurrence-source texts. Added first-person reciprocal striking, strike-and-expel, and a monk's palm slap to the master.
- 喝一喝 (`t_ba8066477571`): 9 occurrences across 8 source texts. Added the four-shout/text-stated-classification sequence, monk-shouts-and-exits, shout-before-seat-ascent, and shout-before-return-to-quarters deployments.

## Definition, family, and sense audit

Each entry was reopened against its full enriched evidence and related family:

- 禮拜 remains one bodily ritual action; actor and consequence are deployments.
- 衲僧 remains one human-role referent; individual, collective, possessive, and compound grammar do not create another kind of monk.
- 世尊 remains one title/person referent across many public cases. Later appraisals remain attributed to their speakers.
- 便打 remains one stock verbal action; agent, recipient, instrument, and consequence do not create another act.
- 喝一喝 remains one finite performed action; speaker and event position do not create another thing, and the four recorded classifications were not promoted into four senses.

The resulting five single-sense decisions are positive item-8 adjudications, not defaults. Each `WORK.md` now records definition-form searches, deployment classes, exclusions, family compatibility, #0g deviation, and the final omission audit.

## Mechanical verification

- Parsed all five JSON entries successfully.
- Re-ran every retained and added occurrence through `zc.verify`: 46/46 returned `ok: true`, with exact stored `FromLb` and `ToLb` matches.
- Ran the hash-aware gate:

  `python3 audit_depth_sense.py --ids t_1d3473614976 t_acccac1051a4 t_c875e45fbb9d t_8879b278cd83 t_ba8066477571`

- Result: `audited: 5`, `hardFailed: 0`, `batchCluster: null`.
- The five review flags are the expected `broad-concordance-single-sense-review` prompts. The item-8 decisions above and in each `WORK.md` explicitly adjudicate them.
- Audited entry hashes were registered in `maintenance/depth-sense-gate.json` by the gate.
