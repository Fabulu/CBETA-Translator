# X72n1437 source-batched attribution repair

Scope: the 18 workbook-listed occurrences in 15 entries drawn from `X/X72/X72n1437.xml`, *Extensive Record of Chan Master Yongjue Yuanxian* (`永覺元賢禪師廣錄`). This is an attribution-only source pass, not whole-entry remediation.

## Result

- Complete cases and exact turns reviewed: **18/18**.
- Assigned occurrences updated: **18/18**.
- Exact `zc.verify` success with stored `FromLb`/`ToLb` equality: **18/18**.
- Full modified-file KWIC audit: **84/84** occurrences verify and all **84/84** stored anchor pairs match.
- Every assigned `AttributionNote` names `永覺元賢禪師廣錄`: **18/18**.
- Exact actor branches: **16 named**, **1 reviewed-unnamed**, **1 impersonal quotation scene**.
- JSON parse: **15/15 changed files**.

## Actor decisions

| Entry | Id | Workbook occurrence(s) | Exact actor |
|---|---|---:|---|
| 嗣法 | `t_7ccccfa5fe9a` | S1/O2 | Weilin Daopei, the signed preface writer |
| 這箇 | `t_d7883993745e` | S1/O6 | Yongjue Yuanxian |
| 窠臼 | `t_b4e104076bdf` | S1/O3 | unnamed lay questioner; six-rung review complete |
| 戒 | `t_292ac4c33b4f` | S1/O1 | two source-unattributed old sayings quoted by Yongjue; impersonal quotation scene |
| 戒 | `t_292ac4c33b4f` | S1/O2 | Yongjue Yuanxian |
| 泥牛入海 | `t_1aa768c331fa` | S1/O4 | Yongjue Yuanxian |
| 漏逗 | `t_898279a78ecf` | S1/O4 | Yongjue Yuanxian |
| 知解 | `t_6edb551acb53` | S1/O6 | Yongjue Yuanxian |
| 明心見性 | `t_dc5f4386a0ed` | S1/O2 | Yongjue Yuanxian |
| 一切現成 | `t_d02b40e03f5d` | S1/O2 | Yongjue Yuanxian |
| 一念不生 | `t_d065698c14a8` | S1/O1 | Zhangzhuo Xiucai, explicitly named verse author |
| 一念不生 | `t_d065698c14a8` | S1/O2 | Yongjue Yuanxian |
| 回互 | `t_1e3d3a5173a6` | S1/O1 | Yongjue Yuanxian, commentator |
| 金鎖玄路 | `t_5ddde30711a4` | S1/O4 | Yongjue Yuanxian, commentator |
| 寶鏡三昧 | `t_5db4dbd2bc17` | S1/O2 | Yongjue Yuanxian, commentator; Dongshan and Caoshan are persons discussed |
| 正中來 | `t_ccd48e1c9145` | S1/O3, S1/O4 | Yongjue Yuanxian, commentator |
| 綱宗 | `t_80ea075a6c5d` | S1/O5 | Lin Zhifan, signed biographer; Yongjue is the biography subject |

## Cohort audit before/after

The audit covers all 84 occurrences in the 15 files, so remaining failures describe non-assigned rows and whole-entry prose.

| Metric | Before | After | Change |
|---|---:|---:|---:|
| named occurrences | 13 | 29 | +16 |
| deferred non-roster names | 1 | 3 | +2 |
| unresolved actors | 71 | 53 | -18 |
| notes missing speaker | 80 | 63 | -17 |
| notes missing source | 64 | 51 | -13 |
| hard failures | 290 | 242 | -48 |
| dangling Chinese strings | 63 | 63 | 0 (out of scope) |

The assigned source rows themselves are complete: 16 named actors plus the two schema-valid exception branches account for all 18. No merge, commit, or push was performed.
