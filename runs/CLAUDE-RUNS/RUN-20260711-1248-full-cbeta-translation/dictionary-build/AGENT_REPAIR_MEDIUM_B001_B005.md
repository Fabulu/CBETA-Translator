# Medium repair report: b001–b005

Completed the twelve assigned medium-severity repairs from `AGENT_DEPTH_AUDIT_B001_B005.md`. All existing senses, occurrences, KWICs, anchors, source lists, relationships, counts, and depth inventories were preserved.

| Term | ID | Repair |
|---|---|---|
| 見性 | `t_c13928184189` | Removed the defensive comparison to a metaphysical essence; retained the literal graphs and all recorded definitions and answers. |
| 家風 | `t_c728f3a8e02b` | Recast the stock question English-first: “What is the master's family style?” (如何是和尚家風). |
| 五位 | `t_ff50c6974a36` | Put the English title first and the full Chinese title in parentheses. |
| 無事 | `t_f6dadadcbef5` | Removed the defensive metaphysical/non-action/transcendence comparison; retained the literal “nothing to do” and corpus predicates. |
| 轉語 | `t_f2181872b682` | Rendered the fox-case person's description through conduct rather than an imported rank category. |
| 下語 | `t_7182bedf65d1` | Recast “the assembly all laid down words, none accorded” (眾皆下語不契) and “wording and choice of words” (下語用字) English-first. |
| 見性成佛 | `t_ac2e2908084d` | Rendered the recorded ten-thousand-kalpa wording as “refine the marks,” preserving the quoted contrast. |
| 直指人心 | `t_b8063e3d60b4` | Translated the three Chinese companion phrases English-first, including “seeing nature, becoming buddha” (見性成佛). |
| 教外別傳 | `t_2d4525b4b123` | Rendered the quoted 修證所得 literally as “what any refining and verifying obtains.” |
| 殺人刀 | `t_d7167b5f3236` | Verified the rare graph in “I want you to leap” (要你𨁝跳) and translated the whole reply English-first. |
| 生死事大 | `t_78f95517a347` | Recast the instruction as pasting the two graphs “life and death” (生死) on one's forehead. |
| 兼中到 | `t_61c90d3a8edd` | Replaced “essence and function” with the corpus-consistent “substance and function” in the direct definition. |

## Final gates

- JSON parsing: 12/12 passed.
- Exact contiguous `zc.verify`: 64/64 returned `ok == True`.
- Exact lower bounds: 64/64 `FromLb` and `ToLb` pairs matched `zc.verify`.
- Rare graph check: `zc.verify("J/J26/J26nB178.xml", "要你𨁝跳")` returned `ok == True`, at `0117b21`.
- Prohibited-framing scan over all non-KWIC prose: zero hits.
- Audit-defect stale-string scan: zero hits.
- One preserved 兼中到 occurrence intentionally does not contain that headword: it documents the neighboring fourth-rank 兼中至／偏中至 graph variant and is explicitly justified in the unchanged entry note. Its KWIC and bounds verify exactly.

No status, manifest, termbase, planning, merge, or corpus file was changed.

