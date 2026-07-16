# Hard bundle ledger — owner 1 remaining 300

## Initial reconciliation checkpoint

- Input: `maintenance/hard-bundle-inputs/worker-1-remaining-300-triage.json`.
- Packet directory: `maintenance/hard-bundle-inputs/w1-remaining300/`.
- Ground truth: 300 occurrences, 65 sources, 65 decision sheets, and 65 paired complete-case workbooks.
- Current durable state: 0/300 reviewed, 0/65 signed, 0/65 applied, and 0/300 directly XML-verified.
- Every row requires an explicit exact-turn decision after the six-rung ladder; titles and section headers remain candidates rather than automatic actors.
- No merge, commit, or push is authorized.

## Twenty-row review checkpoint

- `T51n2077` was reviewed through its complete cases and XML section hierarchy; the first twenty rows have explicit exact-turn decisions and the twenty-first completes the source.
- Embedded speech remains with its actual speaker, including Fushan Fayuan's quoted four requirements and Qian Duanli's signed death instruction; genuinely unnamed questioners and the unnamed ancient teacher quoted by Daowu retain full six-rung records.

## Applied-source checkpoint: T51n2077

- 21/21 rows passed signed compile, strict dry-run, apply, focused stored-state comparison, and direct `zc.verify` replay with exact `FromLb`/`ToLb` matches.
- Durable progress: 21/300 reviewed, applied, and verified; 1/65 sources signed and complete.

## Thirty- and forty-row review checkpoints

- `X64n1260` contributes rows 22–44. Each of its 23 anthology excerpts was widened to the governing named extract rather than assigned from the generic book title.
- The review distinguishes embedded speech and continuing extract ownership, including Yuanwu Keqin's command opening, Fushan-line and Linji-line speakers, Zhongfeng Mingben's signed memorial prose, and Folang Xing's closing whisk address.
- The research-only helper is still running; no unreturned research has been counted in progress.

## Applied-source checkpoint: X64n1260

- 23/23 rows passed signed compile, strict dry-run, apply, focused stored-state comparison, and direct `zc.verify` replay with exact `FromLb`/`ToLb` matches.
- Durable progress: 44/300 reviewed, applied, and verified; 2/65 sources signed and complete.

## Fifty-row review checkpoint

- `X65n1286` maps the stored turn to Gao'an Dayu, the exact speaker who calls Huangbo Xiyun grandmotherly in the Linji encounter.
- `X66n1296` distinguishes Jingfu's signed preface prose from its impersonal document heading, and maps embedded comments to Luohan Ji, Yuanwu Keqin, Jingfu, and Fengri Yue. The `心地` token remains with its genuinely unnamed questioning monk, with Yunfeng Wenyue retained as respondent and record subject.

## Applied-source checkpoints: X65n1286 and X66n1296

- All 8/8 rows passed signed compile, strict dry-run, apply, focused stored-state comparison, and direct `zc.verify` replay with exact `FromLb`/`ToLb` matches.
- Durable progress: 52/300 reviewed, applied, and verified; 4/65 sources signed and complete.
- The research-only helper has completed its six-rung research pass over 88 rows and is packaging the explicit decision map; those rows remain uncounted until mechanically integrated and gated.

## Sixty-row review checkpoint and four applied sources

- `X66n1298` (1), `X67n1299` (2), `X67n1303` (1), and `X68n1318` (12) contribute 16 fully reviewed rows. The review preserves title/headword occurrences as impersonal where appropriate, keeps headwords in questions with genuinely unnamed monks, and assigns record-owned discourses and verses to their exact named authors rather than the anthology title.
- All 16/16 rows passed signed compile, strict dry-run, apply, focused stored-state comparison, and direct `zc.verify` replay with exact `FromLb`/`ToLb` matches.
- Durable progress: 68/300 reviewed, applied, and verified; 8/65 sources signed and complete.
- Corrected research-helper scope: the first 32 alphabetical sources contain 88 rows, not 93; none are counted until its explicit map is integrated and gated.

## Seventy-row and applied-source checkpoint: X68n1319

- `X68n1319` contributes 7/7 reviewed rows. The review distinguishes the Yongzheng Emperor's own imperial and princely prose from genuinely unnamed monks and attendants speaking to him, and identifies Yunqi Zhuhong as author of the final instruction.
- All 7/7 rows passed signed compile, strict dry-run, apply, focused stored-state comparison, and direct XML replay.

## Integrated research checkpoints through 160 rows

- The helper's explicit 88-row map for the first 32 alphabetical sources was integrated with zero key overlaps, missing keys, or extras.
- Each of the 32 sources independently passed signed compile, strict dry-run, apply, focused stored-state comparison, and direct `zc.verify` replay with exact stored `FromLb`/`ToLb`; 88/88 rows passed with zero failures.
- Material exact-turn corrections include Hongren rather than the title-default Huineng for all three `T48n2008` rows; quoted and anonymous questioners rather than respondent defaults; Yixian for the signed Zhida Qinggui preface; and the generic 導師 quotation in Panshan retained as reviewed-unnamed after parallel witnesses also leave it at 導師.
- Durable progress: 163/300 reviewed, applied, and verified; 41/65 sources signed and complete.

## Final checkpoints through 300 rows

- `X80n1565` added 42/42; `X78n1553`, `X78n1556`, `X79n1557`, and `X79n1559` added 20/20; `X82n1571` added 38/38; and the residual six sources added 19/19.
- Every source passed signed compile, strict dry-run, apply, focused stored-state comparison, and direct `zc.verify` replay with exact stored `FromLb`/`ToLb`.
- Final durable state: 300/300 reviewed, applied, and verified; 65/65 sources signed and complete; remaining=0; zero failures.
- No merge, commit, or push was performed.
