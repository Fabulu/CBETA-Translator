# Independent remediation review — 上堂 (2026-07-13)

## Disposition

- Entry: `上堂` (`t_4f7bd98ad40f`)
- Result: **PASS after correction**
- Current entry SHA-256: `f4043e70d6ef6837743211301a44cfe0a5d6bd18844a8f547e333a8ea93ab8bf`
- Merge/commit/push: **not run**

This pass independently re-read the complete structural unit around every saved occurrence under the guide's exact-actor XOR gate. Prior names and prior null decisions were treated as hypotheses. The review also re-tested the entry's definition, two-sense structure, opening interpretation, depth, search aliases, family relations, quoted evidence, source equality, and all exact anchors.

## Correction made

The earlier report described three reviewed nulls. That was one too many.

The family taxonomy at `X64n1260:0001a17` occurs in the first preface of *Essentials from the Patriarchs' Addresses* (`列祖提綱錄`). Its byline is explicit: `住黃梅四祖雙峰山東吳嗣祖沙門戒顯撰`. Jiexian therefore owns the authorial sentence that lists `上堂`, `小參`, `示眾`, and `普說`. The occurrence now carries `MasterName: Jiexian`, and the attribution note names both source and actor. The row remains `EvidenceRole: family`, so it does not buy exact-headword depth. `Jiexian` is already the source-attested spelling used elsewhere in the termbase but is not yet in the roster; the current attribution auditor reports that as deferred, not unresolved.

The two Baizhang Code rows remain non-personal, but bare null is no longer acceptable. Each now occupies the explicit `impersonal` branch with all identity/review fields and concrete grammar evidence:

- `T48n2025:1113a14`: `上堂令客頭掛上堂牌` uses the hall address as the nominal event frame of a directive—at that event, the guest-prefect is to hang the placard. The full Sacred Anniversary procedure assigns multiple offices and later says that the resident abbot mounts the seat, but it instantiates no historical address-giver at the stored clause. Assigning Baizhang, compiler Dehui, or corrector Dashen would confuse source/container context with the exact actor.
- `T48n2025:1122a08`: `凡旦望五參上堂罷` makes completion of the scheduled hall address a temporal condition for the acolytes' following duties. It has no personal speech turn or historically instantiated subject.

Exact-actor result: **11 named + 2 reviewed impersonal + 0 reviewed-unnamed + 0 bare null**.

## Complete-unit actor decisions

| Row | Complete-unit decision | Exact actor branch |
|---|---|---|
| s1o1 `T47n1988:0545a18` | Yunmen's own record opens the complete address with `師上堂良久云`; no speaker shift precedes the headword. | Yunmen Wenyan |
| s1o2 `T48n2001:0002a03` | Hongzhi's Puzhao Monastery section identifies him as `覺上座`; the inaugural address is his. | Hongzhi Zhengjue |
| s1o3 `T48n2025:1113a14` | Institutional directive; `上堂` is the scheduled nominal event frame, not a historical person's turn. | impersonal / prescriptive scene |
| s1o4 `J10nA158:0021b02` | In Miyun's Tiantong record, `師` raises his hand, declares the address complete, and returns to the abbot's quarters. | Miyun Yuanwu |
| s1o5 `B27n0152:0498b15` | Imperial command requests the Wanshan Hall address; the ensuing complete unit repeatedly identifies Yulin as `師`. | Yulin Tongxiu |
| s1o6 `T48n2025:1122a08` | Code schedule; completion of the hall address is a temporal clause governing later acolyte duties. | impersonal / temporal frame |
| s1o7 `T48n2001:0002b06` | Hongzhi raises an older Luopu exchange inside his own address. Luopu belongs to the embedded case; Hongzhi owns the headword event. | Hongzhi Zhengjue |
| s1o8 `D48n8939:0005b11` | The complete Foyan address opens `上堂僧問`; Foyan owns `上堂`, while an unnamed monk owns the adjacent question. | Foyan Qingyuan |
| s1o9 `J26nB178:0108a23` | The release-from-restriction address is within Feiyin's own address section and continues in his voice. | Feiyin Tongrong |
| s1o10 `J26nB178:0117c23` | Title-owner veto: Feiyin's record explicitly says front-hall head monk Yinyuan held the whisk and gave this address. | Yinyuan Longqi |
| s1o11 `X72n1437:0577a14` | The biography's named subject is Yongjue; it says he prohibited hall addresses, refused requests, then mounted the seat. The sentence occurs twice in the file; the saved line selects the correct biographical unit. | Yongjue Yuanxian |
| s1o12 `X64n1260:0001a17` | The first preface explicitly names its author with `戒顯撰`; the compiler Xingyue is discussed, not speaking. | Jiexian |
| s2o1 `T47n1985:0496b14` | Linji's opening public encounter pairs the officials' request to take the seat with `師上堂，云`; `師` is Linji. | Linji Yixuan |

The Foyan and Yinyuan rows are the main anti-automation controls. A title-only system could miss the anonymous question nested in Foyan's address or falsely assign Yinyuan's delegated address to Feiyin.

## Semantic and reader-facing review

- **Sense split: keep.** `formal teaching-hall address` names the public institutional discourse event/recorded product; `ascend the teaching hall` names the concrete institutional action. The targets distinguish different referents rather than noun/verb packaging of one gloss. The Linji `請師升座，師上堂，云` row directly anchors the action and keeps that second sense honestly single-source/provisional. No third referent appeared.
- **Opening: pass.** The primary opening tells the reader what the event is and how Chan institutionalizes the ordinary ascent phrase before presenting frequency and deployments. The second opening states the concrete action and its contrast with the resulting address.
- **Depth: pass.** Twelve exact-headword occurrences plus one family anchor cover address opening, inaugural occasion, code placard, completion by gesture, imperial request, code scheduling, old-case raising, public-question opening, release from restriction, delegated whisk authority, prohibition/resumption, and concrete ascent. The family anchor does not count toward the depth floor.
- **Searchability: pass.** The two senses cover natural probes for `hall address`, `teaching hall address`, `formal/public hall address`, `ascend the hall`, `go up to the hall`, and `take/mount the high seat`. No alias claims that a dependent compound is the headword.
- **Family: pass.** `升座/陞座`, `法堂`, `小參`, `晚參`, `示眾`, and `普說` remain compatible. The family taxonomy explicitly distinguishes several address formats; it does not collapse them into one sense.
- **Quotes: pass.** All six Chinese prose strings are anchored. No useful evidence was deleted.
- **Forbidden English: pass.** Neither forbidden reader label appears.

## Mechanical gates

- `zc.verify` with UTF-8 environment: **13/13 pass**, exact stored `FromLb` and `ToLb`.
- `SourceTexts` equality: **2/2 senses pass**.
- `audit_attribution.py`: **0 hard failures**; 11 named, 2 impersonal, 1 deferred non-roster name.
- `audit_depth_sense.py`: **1 audited, 0 hard failures, 0 review flags**.
- `audit_public_feedback.py`: **1 passing, 0 flags**.
- `run_cohort_gate.py`: **hardPass true**, 13 exact KWICs, 0 exact failures, 0 forbidden-English findings.
- Machine report: `maintenance/cohort-a-shangtang-independent-gate-20260713.json`.
- Retrieval/review packet: `maintenance/cohort-a-shangtang-independent-gate-20260713-attribution-packets.json`; all 13 rows remained review-required, as expected under title/header veto rules.

## Residual dependency

`Jiexian` needs the roster expansion agent's canonical registration before the website can guarantee a resolved master link. The occurrence itself is no longer unattributed: the Chinese preface names him directly.
