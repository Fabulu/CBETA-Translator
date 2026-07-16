# Agent refresh batch 2

Completed 2026-07-12. Scope was limited to prose fields in the 29 assigned `entry.v2.json` files. Existing senses, occurrence objects, KWICs, passage paths, line anchors, attribution values, links, and evidence depth were preserved.

## Edited

- `t_097f38f58678` 庭前柏樹子
- `t_0ed8638229a9` 無位真人
- `t_193632bffe7b` 喝
- `t_1d3706324b0c` 打成一片
- `t_2069b9c33315` 君臣
- `t_2738431562e6` 無字
- `t_2f4b60453d19` 承當
- `t_36aa29eb1287` 水牯牛
- `t_46c30c5d57d4` 不立文字
- `t_4cc95950b59a` 一句
- `t_4f7bd98ad40f` 上堂
- `t_53da4e346a6f` 百尺竿頭
- `t_62044e7bbb87` 本分事
- `t_6da91f8ce284` 賓主
- `t_78f95517a347` 生死事大
- `t_7efdfe4296c6` 父母未生前
- `t_87cc840b8f33` 拄杖子
- `t_8bd6933e6de3` 一喝
- `t_93ab42fecdca` 本來無一物
- `t_ac2e2908084d` 見性成佛
- `t_b291fe703ff1` 參禪
- `t_ba841f6e11c8` 乾屎橛
- `t_c728f3a8e02b` 家風
- `t_ccd48e1c9145` 正中來
- `t_cf0513be4012` 宗旨
- `t_d69c18a98053` 喫茶去
- `t_db4a932ce500` 大悟
- `t_ea138c7335d3` 鼻孔
- `t_f6dadadcbef5` 無事

## Skipped

- None.

## Verification

- JSON parse: 29 / 29 passed.
- Curated occurrences checked with `zc.verify`: 154 / 154 returned `ok == True`.
- Stored line anchors compared with `zc.verify`: 0 mismatches.
- Targeted strict #0c scan: 0 prose fields contain Chinese outside parentheses.
- Targeted final-spec framing scan: 0 banned-framing matches.

The refresh retained bare Chinese only in `Kwic`. In prose, Chinese evidence is parenthetical and remains beside its existing English description or translation. The entries for 無字 and 參禪 retain the required renderings “the word ‘no’” and “investigate Chan”; no Japanese loanword or meditation/practice framing was introduced.
