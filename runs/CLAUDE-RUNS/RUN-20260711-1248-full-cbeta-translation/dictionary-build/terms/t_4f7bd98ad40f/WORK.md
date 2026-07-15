# WORK — 上堂 (t_4f7bd98ad40f)

## Frequency (allowlist-scoped)
- 402 allowlist files, 54,942 apparatus-clean occurrences (`zc.count`, 2026-07-13). The standard section-header of recorded-sayings (語錄) collections, hence the huge count.
- Top files: X82n1571 (4463), X64n1260 (2087), X81n1568 (1802), X80n1565 (1762)…

## Concordance (curated, verbatim; speaker confirmed against chapter/section head)
| KWIC | Text | FromLb | Speaker | How confirmed |
|---|---|---|---|---|
| 府主王常侍與諸官請師升座，師上堂，云： | T47n1985 臨濟語錄 | 0496b14 | Linji Yixuan | title level=m 鎮州臨濟慧照禪師語錄; opening lines of the record |
| 師上堂良久云。夫唱道之機。固難諧剖。 | T47n1988 雲門廣錄 | 0545a18 | Yunmen Wenyan | text is 雲門匡真禪師廣錄; 師 = Yunmen |
| 入寺上堂云。古人道盡十方世界。 | T48n2001 宏智廣錄 | 0002a03 | Hongzhi Zhengjue | 泗州大聖普照禪寺上堂語錄 head (mulu, line 960) |
| 上堂令客頭掛上堂牌。維那於僧堂。 | T48n2025 敕修百丈清規 | 1113a14 | (monastic code, null) | procedural passage in the code |

## Sense analysis
Two corpus-wide referents after the item-8 review: (1) the formal teaching-hall address/observance, including the recorded discourse headed by the formula; and (2) the concrete institutional action of ascending the teaching hall or taking its high seat. No master-specific bending — both are shared institutional uses. The four original anchors displayed these defining features:
- sponsored/requested frame with 升座 (Linji, occ. 1)
- 良久 pause stage-direction (Yunmen, occ. 2)
- inaugural 入寺上堂 tied to taking up abbacy (Hongzhi, occ. 3)
- monastic-code procedure with the 上堂牌 placard (百丈清規, occ. 4)

## Multi-source verdict: MULTI-SOURCE
4 independent texts / 3 masters + the monastic code. Overwhelmingly attested.

## Deflation check
Rendered as the literal act "ascend the hall (give a formal Dharma-hall address)", not any mystified reading. Contrast partner 小參/晚參 noted.

## Thin spots / caveats
- Contrast with 小參 (informal convocation) is stated but 小參 not itself curated here (separate term).
- The bare count conflates the narrative verb (師上堂) and the genre header (上堂云); both are the same sense, so not split.

## Gate 2 verification (Claude, 2026-07-11)
- **All 4 KWICs re-derived from source and confirmed EXACT CONTIGUOUS after XML-tag stripping** (targeted grep of each cited file; all matched on a single body line):
  - occ. 1 T47n1985 0496b14 「府主王常侍與諸官請師升座，師上堂，云：」 ✓ (opening of 鎮州臨濟慧照禪師語錄)
  - occ. 2 T47n1988 0545a18 「師上堂良久云。夫唱道之機。固難諧剖。」 ✓ (師=Yunmen, 對機三百二十則)
  - occ. 3 T48n2001 0002a03 「入寺上堂云。古人道盡十方世界。」 ✓ (泗州普照寺 record, Hongzhi)
  - occ. 4 T48n2025 1113a14 「上堂令客頭掛上堂牌。維那於僧堂。」 ✓ (百丈清規, null)
- **Attribution: all confirmed, no changes.** occ. 1 describes Linji's act of ascending (師=Linji); occ. 2 Yunmen ascends; occ. 3 Hongzhi's inaugural (whole guanglu is Hongzhi's); occ. 4 monastic code (null). Each KWIC ends before/at the speech opener, so no two-speaker contamination.
- All 4 RelPaths in zen-corpus.json (no contamination). Validation stays **multi-source** (4 texts / 3 masters + monastic code). RelatedTerms (升座/小參/晚參/法堂/普說) are genuine semantic cross-refs, not coincidental prefixes. FromLb values confirmed nearest-preceding <lb n>.
- **STATUS: verified**

## d001-A depth repair (2026-07-13)

- Re-ran item 8 and split the formal teaching-hall address/observance from the physical act of ascending or taking the teaching seat. The Zen-institutional address is primary.
- Preserved all four old anchors and added six verified, non-duplicate classes: completed address by gesture, imperial request, code scheduling, old-case opening, public-question opening, and restriction-release occasion.
- Final depth: 2 senses, 10 occurrences. The address sense has 9 anchors; the physical-action sense has the explicit 升座/師上堂 anchor.
- Family check: 小參 is the contrasting informal convocation; 陞座/升座 overlaps the physical action; neither is silently collapsed into the address sense.
- Omission decision: further holiday headings repeat the represented occasion class and were excluded as padding.

## d001 anti-quota follow-up (2026-07-13)

- Added two institutionally distinct address-sense anchors, bringing depth from 10 to 12 without changing the item-8 split.
- `舉前堂隱元首座秉拂上堂` anchors delegated teaching-seat authority: a front-hall head monk holds the whisk and gives the formal address.
- `禁止上堂，雖力請弗許，至是忻然登座` anchors formal suspension, refusal of requests, and later resumption.
- These are authority/control deployments, not additional holiday headings. The address sense now has 11 anchors; physical ascent retains its explicit paired 升座 anchor.

## 2026-07-13 item-8 target hygiene

- Re-tested all 54,942 hits/402 files against `升座`, `登座`, `法堂`, `小參`, `晚參`, `秉拂`, code scheduling, and address headings. The completed public address and the concrete act of ascending the teaching hall remain different things.
- Replaced the fused action target `ascend the hall; take the teaching seat` with `ascend the teaching hall`; `mount the teaching seat` remains an alternate supported by the paired 升座 construction.
- All twelve occurrences remain under the definitions they support; no discourse heading was reassigned to the physical-action sense.
- sense-target-distinguishability: KEEP — `formal teaching-hall address` names a public discourse event; `ascend the teaching hall` names the physical institutional action that precedes it.

## 2026-07-13 Cohort A complete-case and public-feedback pass

- Re-read all 12 prior anchors in their complete address, code, biography, or embedded-case units. Named 9 address actors exactly: Yunmen Wenyan, Hongzhi Zhengjue (two), Miyun Yuanwu, Yulin Tongxiu, Foyan Qingyuan, Feiyin Tongrong, Yinyuan Longqi, Yongjue Yuanxian, plus Linji Yixuan in the physical-action sense. The Yinyuan witness is the decisive title-owner exception: it appears in Feiyin's record but explicitly assigns the address to Yinyuan.
- The later exact-actor XOR pass supersedes the earlier three-null disposition: the two Baizhang Code prescriptions are explicit `impersonal` institutional event frames with grammar evidence, while the family taxonomy is not anonymous at all. The first preface of *Essentials from the Patriarchs' Addresses* explicitly names Jiexian (戒顯撰), so that occurrence now carries `MasterName: Jiexian`.
- Added the exact family anchor `自上堂、小參、示眾、普說，以至隨機拈示，各有科條。` so the quoted contrast term `小參` is sourced rather than deleted. It is `EvidenceRole: family` and cannot buy headword depth.
- Definition/family retest: the newly named evidence still separates the event from the physical ascent. Hongzhi's old-case opening and Foyan's anonymous-question opening belong to the address event; Linji's paired `升座／師上堂` remains the physical-action anchor.

feedback-observations: Occurrences s1o1–s1o11 establish a requested, scheduled, placarded, delegated, suspended, resumed, and publicly questioned address event; s2o1 directly pairs taking the seat with ascending the hall. The added family occurrence lists hall address beside small convocation, assembly address, and general address.

feedback-inference-verdict: **licensed** — “formal teaching-hall address” is the smallest English referent covering the institutional event, while “ascend the teaching hall” is a different concrete action. The corpus licenses the public and office-bound bend without licensing a doctrine about what the address accomplishes.

feedback-falsification-searches: Rechecked address headings; paired `升座`, `陞座`, and `登座`; placard and schedule language; old-case and public-question openings; delegated `秉拂`; prohibition/resumption; `小參`, `晚參`, `示眾`, and `普說`; title-owner conflicts and embedded speakers.

feedback-counterexamples: Code prescriptions and editorial taxonomies have no personal speaker; Foyan's stored opening contains an anonymous monk's question, and Yinyuan—not Feiyin—owns the delegated address. These prevent assigning every occurrence to a title owner and prevent collapsing the event into any one speaker's discourse.

feedback-scope: Two corpus-wide institutional senses: the completed/public address event and the concrete ascent. Occasion, opening device, and delegated office are deployments of the event, not new referents.

lookup-probes: `hall address`, `teaching hall address`, `formal hall address`, `public hall address`, `ascend the hall address`; physical-action probes `ascend the hall`, `go up to the hall`, `take the high seat`, `mount the high seat`, and `ascend teaching hall` are covered by targets, alternates, and aliases.

opening-interpretation-verdict: **pass** — each sense now opens with the thing and its Chan institutional bend before counts and examples. The primary opening distinguishes the public recorded event from ordinary ascent; the second distinguishes the physical action from its resulting discourse.

sense-target-distinguishability: **KEEP** — `formal teaching-hall address` is a discourse event; `ascend the teaching hall` is the concrete institutional action. The complete-case review yielded no third referent.

## 2026-07-13 independent exact-actor XOR review

- Re-read all thirteen complete structural units and treated every prior attribution as a hypothesis. Every saved KWIC passed `zc.verify`; all stored `FromLb`/`ToLb` values remain exact. The duplicate Yongjue biography sentence occurs twice in the file, and the stored `0577a14–0577a15` anchor was selected by line number and complete biographical unit rather than by string alone.
- Named headword actors retained after whole-unit review: Yunmen Wenyan; Hongzhi Zhengjue (two addresses); Miyun Yuanwu; Yulin Tongxiu; Foyan Qingyuan; Feiyin Tongrong; Yinyuan Longqi; Yongjue Yuanxian; and Linji Yixuan. The title-owner veto remains essential: Yinyuan, not Feiyin, delivers the delegated whisk address. In the Foyan row, Foyan owns the `上堂` event while the adjacent question belongs to an unnamed monk; the note does not assign the monk's words to Foyan.
- `T48n2025:1113a14` takes the impersonal branch. In `上堂令客頭掛上堂牌`, the headword is the scheduled event governed by an institutional directive; the sentence assigns placard and precentor duties but instantiates no historical address-giver. `T48n2025:1122a08` likewise takes the impersonal branch: `凡旦望五參上堂罷` is a temporal condition ('after the hall address is over') for acolyte duties. Both rows now store identity/review fields and concrete `GrammarEvidence`; neither pretends that a compiler is the exact actor.
- `X64n1260:0001a17` no longer takes a null branch. The line sits in the first preface headed `住黃梅四祖雙峰山東吳嗣祖沙門戒顯撰`; rung 1/complete-unit evidence therefore names Jiexian as the exact authorial actor. The later compiler Xingyue is the person discussed, not the speaker. Jiexian is source-attested and already used under that spelling elsewhere in the termbase, but remains pending in the roster expansion.
- Exact-actor result: 11 named rows, 2 reviewed impersonal rows, 0 bare nulls, 0 reviewed-unnamed rows. Every occurrence occupies exactly one XOR branch.
- Definition/sense retest: the attribution corrections do not alter the two referents. The event sense still covers requested, scheduled, placarded, delegated, suspended/resumed, case-raising, and question-opening addresses; the Linji `升座／師上堂` construction remains the sole clearest physical-action witness and therefore keeps that sense provisional. No third referent emerged.
- Family/search/opening retest: `小參`, `晚參`, `示眾`, `普說`, `升座/陞座`, `法堂`, and the five address aliases remain compatible. The primary opening states the institutional public-address bend before evidence; the second states the concrete action. No Chinese prose quote is dangling.
