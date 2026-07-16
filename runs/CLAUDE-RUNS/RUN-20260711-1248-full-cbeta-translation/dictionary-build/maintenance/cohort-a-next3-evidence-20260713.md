# Cohort A next-three evidence pass — 2026-07-13

**Evidence reviewer:** Codex subagent `/root/feedback_lexicography`

**Scope:** `t_67bff0d0e5d3` 僧問, `t_cc840e36f2da` 且道, `t_6abcff898d95` 良久. No merge was performed.

## Results

| Entry | Evidence verdict | Exact-actor result |
|---|---|---|
| 僧問 | ready for independent/root review, with a schema-level attribution exception | 0/11 named: every exact questioner is genuinely unnamed; all respondents/later raisers are named in notes |
| 且道 | ready for independent/root review | 11/11 exact speakers named |
| 良久 | ready for independent/root review, with one documented weak row | 10/11 exact actors named; the Songshan Junji exchange's pausing monk is genuinely unnamed |

All 33 KWICs verify exactly. Depth has zero hard failures; the three broad-single-sense review flags are explicitly adjudicated in `WORK.md`. Public-feedback/search/opening gates pass 3/3. Every one of 14 retained Chinese prose strings is anchored. `SourceTexts` exactly equals the occurrence-path set in all three entries.

## Exact-actor structural finding

Rule 10 now correctly says to name the exact speaker or actor rather than the respondent, title owner, addressee, person discussed, or later commentator. For 僧問, the headword itself encodes the questioning monk as actor. In all eleven curated cases the record calls that person only `僧`; the full six-rung ladder does not yield a personal name. Assigning Zhaozhou Congshen, Linji Yixuan, Oxhead Zhiwei, or another famous respondent to `MasterName` would create a clickable but false statement that the respondent spoke the headword question.

The same issue occurs once in 良久: `僧良久` explicitly makes an unnamed monk the actor of the pause, while Songshan Junji speaks only afterward. That counterexample is semantically necessary because it proves the duration does not encode a master's authoritative silence.

Accordingly, the source files preserve twelve honest nulls. The current `audit_attribution.py` unconditionally counts a null as both `null_master` and `note_missing_speaker`, even when the note documents all six failed rungs; it cannot represent a reviewed genuine-unnamed exception. Root must adjudicate whether to (a) add an explicit reviewed-unnamed state/field, (b) permit a narrowly documented null, or (c) revise the schema to distinguish exact actor from named respondent/context master. Populating `MasterName` with respondents is rejected as contrary to Rule 10.

## Entry changes

- **且道:** all eleven speakers resolved from whole cases; prose and notes now name Yuanwu Keqin, Guanghui Yuanlian, Wuzu Fayan, Zhaozhou Congshen, Guyin Yuncong, Ying'an Tanhua, and Puhua as applicable. Unsupported shorthand was corrected to anchored forms. Search aliases and public-feedback ledger added.
- **良久:** named The Buddha, Shoushan Xingnian, Zongxian Minghui, Yunfeng Wenyue, Yinyuan Longqi, Yulin Tongxiu, Mahakasyapa, Dayang Jingxuan, and Huanglong Huinan; preserved one genuinely unnamed monk; removed three stale `SourceTexts`; search aliases and public-feedback ledger added.
- **僧問:** every note now names the source, records the unnamed questioner, names the respondent/later raiser, and documents the ladder. The opening was rewritten around the public-interview event without misassigning anonymous speech. Search aliases and public-feedback ledger added.
