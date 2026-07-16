# Gloss-hygiene repair plan

The 2026-07-13 item-8 correction makes this pass mandatory before returning to new-term production:

- noun/verb grammar for the same event or product is one sense;
- different readings, master deployments, or contextual subtypes are not different things;
- every retained pair must be distinguishable from `PreferredTarget` alone;
- every preferred target is one clean gloss, never a semicolon fusion or a sentence using the headword;
- every retained multi-sense `WORK.md` needs an exact `sense-target-distinguishability:` pairwise ledger;
- touching an entry also reopens depth, definition, family, English-first, #0g, and exact-anchor QA.

Completed and merged:

- the 44-entry candidate/fused-target wave identified by `GLOSS_HYGIENE_AUDIT_606.md` and the semicolon detector;
- post-merge: 606 entries, 3,470 occurrence anchors, 6,940/6,940 shipped occurrence copies exact-verified.

Remaining snapshot after that merge: 60 retained multi-sense entries lack the exact distinguishability ledger. Process
them in deterministic ID order, five entries per agent per wave. The live detector is the authoritative queue:
all `STATUS=done` entries with `len(Senses) > 1` whose `WORK.md` lacks `sense-target-distinguishability:`.

## L001

### Batch A
- `t_04bce52397dc` 三昧
- `t_0b56e6349db2` 徵
- `t_0e49b88aecba` 傳燈
- `t_12e8cba30de6` 老僧
- `t_18b083a026ba` 提起

### Batch B
- `t_18ec645f99f7` 賓中主
- `t_1a7e251bda53` 示眾
- `t_2069b9c33315` 君臣
- `t_20a56b9c1026` 鐵牛
- `t_250794fa9636` 野狐

### Batch C
- `t_2d92f15fa0ab` 雲水
- `t_35249f3cbae1` 驢腳
- `t_44d14cd3a935` 師子
- `t_4dd50050b279` 拾得
- `t_51f93b6474e8` 湊泊

After each L-wave: combined hash-aware audit must have zero hard failures and no batch cluster; merge all done entries;
run the shipped sync audit; regenerate the missing-ledger queue. Do not treat the ledger as clerical paperwork: if the
pair cannot be defended from exact corpus referents, merge or repair it first.
