# Retroactive depth-repair plan

Generated from `maintenance/depth-sense-gate.json` after r001 merged (606 entries). The live audit found 406 hard failures. Numerical tiers are rejection floors, never targets; every repair must reopen the definition, run the item-8 different-thing test, cross-check the family, and retain every distinct high-value deployment even when that exceeds the listed floor.

## d001 — highest-frequency failures

### Batch A
- `t_4f7bd98ad40f` 上堂 — 54,942 hits; 4 → floor 10
- `t_7180f7431520` 恁麼 — 28,391; 4 → 10
- `t_51fe593d9ffe` 作麼生 — 25,720; 4 → 10
- `t_1a7e251bda53` 示眾 — 12,002; 5 → 10
- `t_8a06e7d99b19` 法嗣 — 10,572; 5 → 10

### Batch B
- `t_193632bffe7b` 喝 — 35,032; 6 → floor 10
- `t_67bff0d0e5d3` 僧問 — 27,950; 4 → 10
- `t_cc840e36f2da` 且道 — 18,624; 6 → 10
- `t_f25cebd24730` 棒 — 18,109; 8 → 10
- `t_4cc95950b59a` 一句 — 15,708; 6 → 10

### Batch C
- `t_8bd6933e6de3` 一喝 — 12,565; 6 → floor 10
- `t_26ea593a58e2` 下座 — 12,306; 6 → 10
- `t_6abcff898d95` 良久 — 11,566; 5 → 10
- `t_12e8cba30de6` 老僧 — 11,424; 6 → 10
- `t_9f119d7965c2` 拂子 — 11,115; 5 → 10

## d002 — keystone/family follow-up seed

### Batch A
- `t_1d3473614976` 禮拜 — 9,933 hits; research proposes 9 distinct anchors
- `t_acccac1051a4` 衲僧 — 8,708; research proposes 9
- `t_c875e45fbb9d` 世尊 — 8,093; research proposes 10 and must define the figure by Zen deployment
- `t_8879b278cd83` 便打 — 7,585; research proposes 9
- `t_ba8066477571` 喝一喝 — 7,091; research proposes 9

### Batch B — keystones
- `t_87cc840b8f33` 拄杖子 — count against family form 拄杖 (27,727 hits); research proposes 14 distinct anchors
- `t_8f7b20536cb6` 和尚 — 52,209 hits; research confirms teaching master vs ordination preceptor and proposes 14 anchors `[10,4]`

Continue in descending corpus-frequency order from the current gate after d002, regenerating counts after every merge.
