# Attribution shortcut validation

## Verdict

**FAIL. Tier A is not safe.** The 24-case stratified validation found **1 false accept among only 2 Tier A candidates**. The required zero-false-accept condition is not met.

The failing case is unambiguous. In `雪竇石奇禪師語錄`, the exact line begins `應庵祖云` (“Ancestor Yingan said”). The shortcut nevertheless proposes **Xuedou Chongxian** because it reads the place-name `雪竇` as a person and ignores both `石奇` (the record owner, Shiqi Tongyun) and the explicit inline speaker, Yingan Tanhua. No risk flag was emitted.

## Results

| Measure | Result |
|---|---:|
| Records | 24 |
| Strata | 4 × 6 |
| Tier A candidates | 2 |
| Tier A true accepts | 1 |
| Tier A false accepts | **1** |
| B-review records | 22 |
| Conservative false rejects | 8 |
| Correct review decisions | 14 |

The eight false rejects are a coverage problem, not a safety problem. Whole-unit reading confirms clean title-owner speech for Hongzhi Zhengjue (two cases), Wuyi Yuanlai, Duanqiao Miaolun, Xuefeng Yicun, Weilin Daopei, Zhaozhou Congshen, and Huanglong Huinan. Broad `embedded`, `excluded contributor`, and `anonymous interlocutor` flags were often triggered by neighboring material rather than the headword-bearing turn.

## Stratified findings

### Verified single-master clean turns

Only Baiyun Shouduan's `金翅鳥` verse reached Tier A and was correct. Five other clean title-owner turns were sent to review because risks were calculated on an overbroad unit or document region. These are recoverable false rejections if risk calculation is narrowed to the exact turn.

### Own-record addressee, visitor, and anonymous turns

Review was generally correct. The cases show why title or section ownership cannot establish a turn's speaker:

- Huang Tingjian says `和尚` to Huitang.
- Zhaozhou says `和尚` to Nanquan.
- Huike says `和尚` to Bodhidharma inside Bodhidharma's section.
- Fayun speaks after receiving a whisk from the record owner.
- Dongshan supplies `不行鳥道` after an anonymous question.
- Huangbo Rong is the named questioner in another master's biographical section.

Speaker, addressee, visitor, section owner, and record owner are separate roles.

### Anthology and header turns

Local headers can work, but only when they are genuinely local. A broad lineage header misidentified Xishan's `拂子` case as Nanquan material; the normalized source itself explicitly opens `蘇州西山和尚`. The Cishou `金毬` packet exposed a separate structural defect: its `caseText` began in the anthology preface and did not contain the stored KWIC at all. A packet that does not contain its own KWIC cannot qualify for Tier A.

### Prefaces, contributors, and embedded quotations

The review gate correctly prevented several title-owner errors:

- Zhou Chi, not Yuanwu Keqin, wrote the `碧巖錄序` line `一棒一痕`.
- Cao Benrong supplies the biographical/stupa prose in `普濟玉琳國師語錄`.
- Miyun Yuanwu is the `本師` quoted inside Wufeng's record.
- `古德云` introduces the `假銀` verse; the exact speaker is an unnamed old worthy, not Xueguan Zhiyin. The current stored MasterName is wrong.
- `應庵祖云` identifies Yingan Tanhua, but the shortcut generated the sole Tier A false accept.

## Blocking rule gaps

1. **Do not resolve bare place or monastery aliases as people when a fuller personal title is present.** `雪竇石奇禪師語錄` must parse `石奇` as the owner; bare `雪竇` is a place/title component here, not Xuedou Chongxian.

2. **Inline speaker markers must veto title-first acceptance.** Before Tier A, inspect the exact turn and its introducing clause for `X云`, `X曰`, `X道`, `祖云`, `古德云`, `頌曰`, preface signatures, and equivalent role labels. A non-owner marker either determines the speaker or forces review.

3. **The packet must contain its own KWIC.** Require normalized `caseText` to contain normalized `storedKwic`; otherwise rebuild or widen the unit and force review.

4. **Calculate risk on the exact headword-bearing turn.** An anonymous monk or embedded quotation elsewhere in a large extracted unit must not taint a clean owner utterance.

5. **Resolve grammatical roles, not just ownership.** Speaker, addressee, quoted speaker, quoter, subject, contributor, local section owner, and book owner must remain distinct.

## Recommendation

Do not enable the current title-first Tier A shortcut. After the three blocking guards—full-title disambiguation, inline-speaker veto, and KWIC-in-unit containment—rerun this same fixed 24-case set. Tier A must return **0 false accepts** before any broader benchmark.

The complete case ledger, exact risk flags, expected speakers, and whole-unit findings are in `attribution-shortcut-validation.json`.

## Guard repair smoke test

After this failure, `attribution_packet.py` was changed to force review for inline speaker markers, partial
title-alias matches, and extracted units that do not contain their normalized stored KWIC. Replaying the
failing `J26nB183 0504a13` locus now emits `inline-speaker-marker` and `title-owner-alias-partial` and assigns
`B-review`, not Tier A. This closes the demonstrated false-accept path; it does **not** reverse the validation
verdict or enable automatic writes. A full fixed-set replay is required before claiming broader classifier safety,
and exact-turn human confirmation remains mandatory regardless.
