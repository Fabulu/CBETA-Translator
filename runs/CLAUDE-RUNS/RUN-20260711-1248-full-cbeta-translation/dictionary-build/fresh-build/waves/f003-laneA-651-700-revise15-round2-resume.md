# A651–700 revise15 round-2 repair checkpoint

2026-07-15: all 15 case-specific repairs were written and compiled. The formal
cohort gate verified 101/101 KWICs exactly; public-feedback, work-source,
corpus-baseline, and frozen-history gates pass. The durable gate report is
`f003-laneA-651-700-revise15-round2-formal-gate.json`.

The cohort is not yet formally hard-passing. Remaining depth additions:

- 出世 7/8
- 住持 7/8 (also replace the semicolon-fused target with one institutional gloss)
- 阿育王 3/6 and 3/4 independent sources
- 文殊 7/8
- 目連 5/6
- 侍者 8/10
- 陞座 7/8
- 羅漢 5/8
- 拄杖 9/10
- 藥師 5/6

Remaining prose gate: translate Chinese speech-frame snippets in AttributionNote
for 出世, 佛祖, 富樓那, 消息, 錯, 拄杖, and 道得. The 羅漢 and 藥師 explanation
strings were already changed in the worksheets to remove the dangling rejected
Chinese false-hit phrases, and 佛祖's invalid `preface-author` role was changed
to the closed role `compiler`; recompile and rerun the gate to confirm.

Do not touch the 35 prior KEEP entries. Their hashes were asserted unchanged by
the round-2 repair script. Do not promote or merge this cohort before independent
review.

## Completion update — 2026-07-15

The remaining work above is complete. Fifteen genuine non-catalogue witnesses
were added from distinct works, plus one further distinctive `出世` witness to
break the artificial at-floor batch cluster. Attribution notes are English-first,
retain the exact source title, and identify their speaker or actor. The forbidden
unnamed-master occurrence for `羅漢` was replaced with a roster-linked Miyun
Yuanwu witness.

Final author-side formal gate:
`f003-laneA-651-700-revise15-round2-final5-formal-gate.json`

- entries: 15
- exact KWIC: 117/117
- exact failures: 0
- hardPass: true
- prior KEEP hashes preserved: 35/35

The author has not independently reviewed or promoted these repairs.
