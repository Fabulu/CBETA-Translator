# WORK — 拂子 (t_9f119d7965c2) "the fly-whisk"

**Status:** drafted · one corpus-wide sense (SenseKey null) · Validation: multi-source

## Method
Zen-scoped concordance over the 462-text allowlist via `scratchpad/stripped2`. Counts/KWICs
grep-verified; all 5 persisted occurrences re-validated verbatim + From/To lb (validate_entries.py, OK).

## Concordance facts (allowlist only)
- 拂子 TOTAL **11,099** in 378 texts.
- Handling verbs: 擊拂子 **1,068** (+拂子擊禪床 288); 豎起拂子 507 + 竪起拂子 492 ≈**999**;
  舉拂子 **871**; 拈拂子 **368**; 揮拂子 343; 擲下拂子 276.
- 拂子頭 **374**; 拂子打圓相 **208**.
- Naming test 喚作拂子則觸 **31** (26 texts). Impossible object 龜毛拂子 **106**; 兔角拄杖 28.
- Implement cluster: 拄杖 **27,649**; 竹篦 **1,553**; 如意 **1,317**.

## Sense
One sense: the physical fly-whisk (拂 brush + 子 suffix) — handle + bushy tuft, for flies/dust,
carried as a master's teaching/ceremonial implement. Defined by accumulated attested USE: it is
almost always the object of a gesture verb (raise/strike/lift/pick-up/throw-down/wave/draw-a-
circle). Self-referential test in the corpus: 喚作拂子則觸，不喚作拂子則背. Stock unreal variant
龜毛拂子. Object named, gestures described, no gloss added.

## Attribution
All curated occurrences are narrated exchanges, first-person recollections, or generic uphall
acts → MasterName null; AttributionNotes name the actor the text names. 3o1: 百丈懷海 raises the
whisk in a narrated exchange in the 黃檗希運 section of 傳燈玉英集 (also at B25n0144 0563a04) →
RelatedMasters 百丈懷海.

## Multi-source
Attested across B14, B25, J25nB175, J10nA158, C077, D48 — implement + gestures + naming test all
recur widely.

## Files
`entry.v2.json` (1 entry, 1 sense, 12 curated occurrences), `WORK.md`, `STATUS=drafted`.

## d001-C depth enrichment

- Added five verified deployments: lineage-credential bestowal, recipient acceptance, drawing/cutting with the whisk, showing it as the answer to the patriarch's coming from the West, and using it to mime a flute.
- The definition now foregrounds both teaching-seat authority and explicit succession transfer/receipt. All ten occurrences still concern the same implement, so no credential or gesture sense was split off.
- Final d001-C depth: 10 curated occurrences, one object sense.

## Anti-quota follow-up

- Added two further genuinely distinct deployments: direct striking/seizure of the fly-whisk in an encounter, and the text's explicit functional contrast “the staff establishes; the fly-whisk sweeps away.”
- Re-tested the whole family and definition. Striking a person, seizing the implement, and contrasting its role with the staff still concern the same physical object; no gesture, credential, or possession sense was split off.
- Final depth after follow-up: 12 curated occurrences.

## 2026-07-13 retrospective remediation

### Search and evidence ledger

- Both required discovery indexes were run before revision. The desktop exact-confirmation index reported 拂子 10,709 / 381 files; the website index reported 11,236 / 385. These are discovery-index measurements, not substituted for XML verification. The earlier direct concordance count differed because the indexes and current allowlist snapshots are not identical.
- Family/deployment probes on both indexes: 豎拂子, 舉拂子, 秉拂, 付拂子, 接拂子, 龜毛拂子, 拂子頭, 拂子掃蕩, 拄杖建立.
- Definition-formula probes on both indexes: 拂子者, 所謂拂子, 謂之拂子, 名為拂子, 喚作拂子, 何謂拂子, 如何是拂子.
- The formula probes yielded naming tests and sparse nominal constructions, but no source self-definition that displaced the ordinary object description plus corpus deployment account.
- Every retained KWIC was rechecked against XML with `zc.verify`; saved FromLb/ToLb bounds match.

feedback-inference-verdict: The inherited core inference holds after falsification: the referent is the physical fly-whisk, while Chan bends it into a teaching-seat implement, public-interview act, and transmissible emblem of authority. The revision makes the ordinary referent explicit before the Zen deployment and does not decode the object as a hidden inner state.

feedback-observations: The corpus predicates raising, throwing, drawing, striking, seizing, showing, miming, bestowing, receiving, and naming of the same implement. The succession passages make institutional force directly observable rather than inferred from symbolism alone.

feedback-falsification-searches: Searched definition formulas, ordinary handling verbs, public-interview uses, staff/whisk contrasts, impossible-object forms, bestowal and receipt, and apparent title/record-owner attribution. Read the complete local encounter or address around every saved occurrence, not merely the KWIC.

feedback-counterexamples: The ordinary fly/dust implement remains the referent, and the corpus also contains routine gesture uses that do not independently assert lineage transmission. These counterexamples prevent “authorization credential” from replacing the object definition or becoming a separate sense.

feedback-scope: One corpus-wide object sense survives the different-things test. Gesture, office, interview, and transmission are deployments of the same implement, not noun/verb splits or paraphrastic senses.

lookup-probes: fly whisk; fly-whisk; whisk; horsehair whisk; teaching whisk; ceremonial whisk; duster. SearchAliases were added for spacing and natural-English lookup, while PreferredTarget remains concise and AlternateTargets retain ordinary variants.

opening-interpretation-verdict: Pass after revision. The explanation opens with what the object is and does, then states where Chan bends it. Graph composition follows later and is not used as the definition.

### Attribution reconstruction

- Applied the six-rung ladder and the tightened complete-case safeguard. A title could identify a record owner, but attribution was assigned only after reconstructing the whole encounter/address unit.
- Explicit exceptions to title-first attribution: Wufeng Ruxue recounts his teacher Miyun Yuanwu; Fayun, not Jifei Ruyi, speaks the receipt line; Donglin Changzong, not the biographical subject Wanshan Shaoci, performs the whisk strikes; Baizhang Huaihai performs the headword actions inside Huangbo Xiyun's narrated section.
- Own-record addresses were assigned to Beijian Jujian, Sanyi Yu, Chuiwan Guangzhen, and Xueguan Zhiyin only after checking the surrounding unit for quoted cases, visitors, or speaker shifts.
- Source section reconstruction identifies Sanping Yizhong, Foyan Qingyuan, Xingkong Guan, and Xishan Heshang. Fayun and Xishan are source-name forms pending any roster expansion; no bare unnamed speaker remains.
- Every AttributionNote now names both the exact source title and the exact speaker attached to MasterName.

### Quote and sense hygiene

- All Chinese evidence quoted in Explanation or Note is represented by a stored occurrence. AttributionNote source titles are metadata, not free-standing evidence claims.
- The target is not split into “whisk,” “teaching whisk,” “authority emblem,” or noun/verb variants. All refer to the same physical object. Conversely, the entry does not collapse the distinct actors in multi-speaker encounters.
- Search aliases are lookup aids only; they do not silently broaden the source term to every brush or ceremonial implement.
## 2026-07-13 second independent exact-turn repair

- Marked `龜毛拂子` as `EvidenceRole: family`; the longer impossible-object phrase no longer buys standalone `拂子` depth.
- Split the mixed Donglin/Wanshan KWIC into two exact-action rows: Donglin Changzong strikes with the fly-whisk; Wanshan Shaoci later seizes it and bows. Each action now carries its own actor.

## 2026-07-13 independent A4 full-case review

- Re-read all thirteen complete cases and retained one physical-object sense. Teaching-seat display, striking, seizure, drawing, miming, bestowal, receipt, and succession are incompatible deployments of neither a second object nor a countable act; they all predicate actions of the same fly-whisk.
- Trimmed the Baizhang Huaihai witness to `百丈豎起拂子`. The inherited span included Huangbo Xiyun's question, Baizhang's raising, Huangbo's follow-up, and Baizhang's throwing action under one `MasterName`; the saved row now contains Baizhang's exact headword action only.
- Removed the now-unsaved throwing detail from reader-facing prose rather than letting a surrounding-context claim masquerade as an anchored occurrence. Fixed the Foyan Qingyuan attribution-note typo without changing its evidence.
- Final A4 result: twelve standalone witnesses plus one family row; thirteen total occurrences, all named and exact-verifying. The broad-concordance single-sense review flag is adjudicated KEEP ONE by the different-things test.
