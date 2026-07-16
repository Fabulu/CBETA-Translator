# T48n2003 source-batched attribution repair

Scope: all 27 workbook rows in 25 complete-case clusters across 22 entries from *Blue Cliff Record of Chan Master Foguo Yuanwu* (`佛果圜悟禪師碧巖錄`). Attribution only; no claim of whole-entry remediation.

## Result

- Complete cases reviewed: **25/25**.
- Assigned occurrences repaired: **27/27**.
- Decision-sheet dry run: **27/27 prepared**, zero failures.
- Decision-sheet apply: **27/27 applied** across **22/22** entries, zero failures.
- Focused source gate: **27/27** exact decision matches and **27/27** notes name the source.
- Actor states: **24 named**, **2 reviewed-unnamed non-master speakers**, **1 impersonal source-unattributed aphorism**.
- Full touched-file audit: **22/22** JSON parse; **117/117** KWICs verify; **117/117** stored anchor pairs match.

## Exact actor summary

- Yuanwu Keqin owns his narrative, case pointers, definitions, and commentary in 12 rows.
- Embedded direct or enacted actors were restored instead of assigning the title owner: Dongshan Shouchu, Jingqing Daofu (2), Xianglin Chengyuan, Danyuan Yingzhen, Wuzu Fayan, Yunmen Wenyan, Dasui Fazhen, Sansheng Huiran, Xuedou Chongxian, and Baozhi.
- `萬法歸一`: an unnamed monk is the exact questioner; Zhaozhou Congshen is linked only as respondent.
- `參堂`: an unnamed Chan visitor is the exact speaker; Xuedou Chongxian is linked as requester and later verdict-giver.
- `劍刃`: `古人道` introduces a source-unattributed old saying. It remains an impersonal quoted formula and is not reassigned to Yuanwu.
- `雪竇與南泉把手共行`: Xuedou Chongxian is the primary linked actor and Nanquan Puyuan is retained as the explicitly named co-actor.

## Changed entry IDs

`t_90e46d995978`, `t_ce2a5ef71afe`, `t_b191c4fa2e9f`, `t_2310fbae5dc4`, `t_bcc96a299271`, `t_b8c3ecb60618`, `t_b90a5f36ec86`, `t_72e01bbb3474`, `t_94be914de45d`, `t_8d9558f7f8a5`, `t_fd1759947989`, `t_e5259ce8bbf5`, `t_644a3152952c`, `t_f758d1e27978`, `t_75348ebe8a2d`, `t_f04c29743e77`, `t_e96268628f2c`, `t_d95b944e0749`, `t_ea138c7335d3`, `t_19784084ccb4`, `t_1d1a833551a9`, `t_93360aaedb7c`.

## Whole-file attribution delta

| Metric | Before | After | Change |
|---|---:|---:|---:|
| unresolved actors | 109 | 82 | -27 |
| named occurrences | 5 | 29 | +24 |
| notes missing speaker | 113 | 87 | -26 |
| notes missing source | 100 | 74 | -26 |
| hard failures | 443 | 364 | -79 |

The remaining counts describe the 90 non-assigned occurrences and whole-entry prose. No merge, commit, or push was performed.
