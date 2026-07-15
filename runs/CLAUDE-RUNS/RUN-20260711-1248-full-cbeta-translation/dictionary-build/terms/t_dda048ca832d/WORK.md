# 金鎖 — modifier, sense, depth, and propagation audit

## Exact inventory

- indexed-discovery-path: `indexed_kwic.py` used `search.inverted.bin` v4 postings (stamp `269db8235c174e058e36b01e0af365ac`, verified path checksum) and exact `search.text.bin` confirmation for every multi-character headword/family/palace probe. For 金鎖 it returned 251 all-corpus candidate documents, 150 allowlisted candidates, and 339 sidecar hits in 150 files. The same batched run covered 黃金鎖, 金鎖玄關, 金鎖玄路, 金門, 金闕, 金鋪, 金扉, 金戶, 重門, 寶殿, 玉階, 青鎖, 玉鎖金匙, and 金鎖玉關. The index exposed the apparatus-only `金鎖者毒藥`; it was rejected because `zc` did not verify it.
- Exact apparatus-excluding `zc` concordance: 金鎖 349 occurrences / 155 texts. Preliminary fast discovery returned 339 / 150; it was used for rapid context classification, not reported as the exact total. Family probes: 黃金鎖 45 / 38; 金鎖玄關 53 / 45; 金鎖玄路 24 / 17.
- Comparison terms: 金 37,439 / 452; 銀 2,972 / 373; 金銀 127 / 65; 金佛 201 / 94; 金身 522 / 184; 金剛 5,386 / 398; 銀山 748 / 225; 銀碗 55 / 36.
- Verb-frame probes: 開金鎖 45 / 36; 掣斷金鎖 4 / 4; 脫金鎖 6 / 5; 敲開 6 / 5; 擊碎 3 / 3; 打透 2 / 2.
- Every preliminary fast-discovery match was classified. Heuristic clusters overlapped: 158 barrier frames, 86 chain/fetter frames, 50 equation-or-difficulty frames, 45 黃金 forms, 17 nested bone compounds, and 9 nested lock-link compounds. The residual 63 contexts were inspected individually. Exact evidence and all retained anchors were then checked against `zc`; the fast/exact total mismatch is disclosed rather than silently treating the discovery index as authoritative.
- The finished entry retains 14 exact KWICs in 10 independent source texts: six standalone cards across the two senses, six longer-compound family cards, and two palace-register contrast controls. The four `金鎖玄路/金鎖玄關` cards and two `黃金鎖` cards are marked `family`; nested forms do not buy standalone depth. All 14 were checked with `zc.verify`, including exact FromLb/ToLb synchronization.

## Inherited lead and verdict

The inherited hypothesis was that 金 may mean enlightenment, so 金鎖 might be a symbolically ‘golden’ obstruction rather than a lock associated with the metal. 銀 was used as the required control. **Verdict: reject gold = enlightenment as a corpus-wide code and as the lexical meaning of 金 in 金鎖.** In the lock/barrier family, the architectural controls instead place 金 within conventional ornate-palace diction. In the animal chain/fetter family, the precise contribution of the modifier remains unresolved.

The closest positive evidence is local, not lexical: one doctrinal passage explicitly says ‘gold here is a simile for Dharma-nature’ (金者喻於法性), and Yongjue compares refining ore away until the original gold is pure with refining mind. Neither says that every 金 modifier, or 金 in 金鎖, means awakening. A direct counterexample reverses the proposed value: ‘Chan study is called the gold-shit teaching; not understanding is like gold, seeing through is like shit’ (參禪謂之金屎法。不會如金。勘破如屎). Gold also occurs straightforwardly as meltable material, color, wealth, ornament, and conventional epithet. The silver control behaves in parallel color/material series—e.g. worlds appearing gold-colored, silver-colored, glazed, or agate-colored—and provides no stable enlightenment contrast.

## Two different referents

sense-target-distinguishability: the PreferredTargets name an ornate lock/barrier and an animal-binding chain/fetter. Architecture supplies the ordinary pictured lock behind the barrier deployment; it is not a third lexical object. The animal-binding verbs require a genuinely different thing.

1. **Ornate lock/barrier.** Architectural verse places it at palace halls and layered gates amid jade steps, blue-green locked gates, precious glazed halls, jewel curtains, jade palace gates, gold doors, and ninefold gates. Barrier deployments open, pierce, or smash the same obstructing scene at a dark barrier or dark road. Direct equations put an approving mind, ordinary feelings and holy views, and the apparently inactive person with nothing going on behind it. This is the dominant Chan bend.
2. **Figurative chain/fetter.** It entangles a flying phoenix and is burst or pulled apart by a lion cub, elephant-king, or fragrant elephant. Those animal-binding scenes require a chain or fetter, not a gate lock.

The two senses name different things, not noun/verb alternations or different readings. Each has direct anchors and a PreferredTarget distinguishable on its own.

## Modifier classification

- **Materially made of gold:** not established for 金鎖. The corpus readily marks material when it matters—e.g. clay Buddha dissolves, wooden Buddha burns, and golden Buddha melts—but gives no corresponding made-of-gold test for this headword.
- **Conventional ornate-palace diction:** established for the lock/barrier image by the immediate series of jade steps, blue-green locked gates, precious glazed hall, and jewel curtain, plus the independent pairing of jade palace gates and gold doors. This licenses ‘ornate’; it does not license solid gold, gilding, gold color, or a narrower imperial code.
- **Conventional name or epithet:** common elsewhere in the 金 family, but does not define this headword.
- **Figurative whole image:** directly established for both the barrier and chain senses. This licenses calling the complete image figurative; it does not license assigning a hidden lexical meaning to 金 alone.
- **Unresolved remainder:** why the animal chain/fetter carries 金. The entry uses a hyphenated source-image form there without inventing enlightenment, material, value, purity, or preciousness.

display-modifier-verdict: the old PreferredTargets “golden lock” and “golden chain” fail item 13 because an English reader naturally hears construction material. The lock sense is now displayed as `ornate lock-barrier`, which is the smallest palace-register inference supported by the controls. The separate binding sense is displayed as `a binding chain`, because its animal verbs establish the object while the source modifier remains unresolved. No displayed target says or implies “a lock/chain made of gold,” “gilded,” or “gold-colored.”

## Inference ledgers

### A. Ornate lock as architecture and obstruction

- **observation:** the phrase closes palace halls or blocks layered gates within conventional ornate-palace lists; it is also equated with an approving mind, ordinary feelings and holy views, and the difficult remainder of a person with nothing going on, while other records open or smash it at a dark barrier.
- **minimal-inference:** an ornate palace lock supplies the ordinary obstructing scene; the barrier compounds redeploy that same lock image.
- **ordinary-bridge:** a lock at a hall or gate blocks entry until opened; the records preserve that relation across architectural, gate, and barrier verbs.
- **falsification-searches:** all preliminary contexts; opening, closing, piercing, smashing, barrier, road, equation, difficulty, palace, gate, door, color, jewel, and animal-binding frames; close parallels 金門, 金闕, 金鋪, 金扉, 金戶, 重門, 寶殿, 玉階, 青鎖, 玉鎖金匙, and 金鎖玉關.
- **counterexamples:** animal chain/fetter scenes require a different object. Palace architecture does not: it supplies the pictured lock itself.
- **scope:** architectural and barrier-lock family, especially 金鎖玄關 and 金鎖玄路.
- **verdict:** licensed as one ordinary-scene-plus-Chan-deployment sense; split only from chain/fetter.

### B. Binding chain or animal fetter

- **observation:** a phoenix is entangled in it; lion and elephant figures burst, break, or pull it apart.
- **minimal-inference:** these contexts require a binding chain or fetter.
- **ordinary-bridge:** an animal can be entangled by or break a chain, but not be entangled by a gate's lock.
- **falsification-searches:** inspected every animal, break, pull-apart, escape, and 黃金鎖 frame, plus the barrier frames.
- **counterexamples:** openable dark barriers and palace gates require a lock rather than a chain.
- **scope:** animal-binding and breaking frames only.
- **verdict:** licensed; separate sense.

### C. Gold means enlightenment

- **observation:** a few passages locally compare gold with Dharma-nature or a refined mind.
- **minimal-inference:** those passages establish their own stated similes only.
- **ordinary-bridge:** none safely carries the simile into 金鎖 without an in-context equation.
- **falsification-searches:** 金/銀 definition formulas, similes, color and material series, gold/silver paired terms, the closest 金鎖 families, and every 金鎖 context.
- **counterexamples:** ‘not understanding is like gold; seeing through is like shit’; gold and silver occur in parallel color/material lists; meltable golden Buddhas are physical controls.
- **scope:** local explicit similes only, not the modifier in this entry.
- **verdict:** rejected.

### D. Gold means materially golden, gilded, gold-colored, valuable, pure, or prized here

- **observation:** the graph 金 permits those readings elsewhere; this headword itself gives no composition statement or value equation.
- **minimal-inference:** use the established conventional ornate-palace register for the lock/barrier sense; withhold a narrower explanation for the animal chain/fetter modifier.
- **ordinary-bridge:** the palace series licenses ornamented/splendid diction but is insufficient for construction material, gilding, color, enlightenment, value, or purity.
- **falsification-searches:** material-destruction controls, color series, refining comparisons, palace imagery, 金門/金闕/金鋪/金扉/金戶, jade/gold pairings, and gold/silver compounds.
- **counterexamples:** figurative chains and barriers need not be metal objects; the corpus marks literal material explicitly when it matters; the palace registers coordinate several elevated modifiers without material predicates.
- **scope:** ornate-register inference for lock/barrier only; unresolved modifier for chain/fetter.
- **verdict:** all material and narrower symbolic claims rejected.

### E. Palace-register control and sense-merge test

- **observation:** Dadian Baotong describes palace halls not being closed with these locks; Zhuanyu Heng places the lock at layered gates within a series of jade steps, blue-green locked gates, a precious glazed hall, and a jewel curtain; Zhe'an Jingfan pairs jade palace gates with gold doors and ninefold gates.
- **minimal-inference:** the pictured fastening belongs to conventional ornate-palace diction, not solid-gold, gilded, or gold-colored hardware.
- **ordinary-bridge:** gates and halls are closed or made difficult to pass by locks.
- **falsification-searches:** residual contexts outside barrier/chain clusters and every close/door/gate/palace frame.
- **counterexamples:** the animal-binding scenes require a chain/fetter; the barrier scenes preserve the same blocking function as the architectural lock.
- **scope:** architectural control for the lock/barrier sense.
- **verdict:** merge with barrier; it is the ordinary/stylized lock scene, not a third referent.

## Contamination and nesting audit

- 金鎖骨 / 金鎖子骨 are distinct anatomical or relic compounds; their 17 matches do not buy depth for standalone 金鎖.
- 金鎖子 and 黃金鎖子 are nested link/object forms; nine relevant nested matches were excluded from the standalone depth count unless the surrounding grammar independently deployed 金鎖.
- A fast sidecar exposed the apparent self-gloss `金鎖者毒藥` in a Bodhidharma-prophecy note. It is apparatus contamination: `zc.find` returned no allowlisted occurrence and `zc.verify` failed. No ‘poison’ sense was created.
- Other proper compounds, armor images, keys, and inherited prophecy lines were inspected; none forced a fourth standalone sense.

## Attribution ladder

Every stored occurrence names its governing speaker and text. Yongjue Yuanxian, Baiyu Si, Weian Deran, Huangbo Wunian Shenyou, Baichi Yuan, Wuming Huijing, Shiyu Mingfang, and Zhean Jingfan are established by their own recorded-sayings titles or governing sections. Zhuanyu Heng is established by the title 紫竹林顓愚衡和尚語錄. Dadian Baotong is established by the 祖堂集 section header 大顛和尚 and the surrounding first-person exchange. No occurrence remains assigned to a bare ‘master,’ ‘monk,’ or ‘text.’

## Search-alias probes

- Lock/barrier sense: gold lock, golden lock, palace lock, golden barrier.
- Chain sense: gold chain, gold fetter, golden restraint, golden binding.

Aliases improve English recall without pretending that road/path/way or lock/chain are always interchangeable senses.

## Family-propagation ledger

- **金鎖玄路 — revise/audit:** belongs to the figurative barrier family; remove any material-gold, preciousness, purity, or enlightenment claim not independently anchored.
- **玄關 — keep, then audit:** retain its own evidence; ensure 金鎖玄關 is treated as a barrier family form, not proof that standalone 金 means awakening.
- **凡情聖見 — keep:** Yongjue's exact equation supports the relation to the barrier sense; it does not support a gold-symbolism claim.
- **無事人 — keep:** Baichi's warning supports the difficult-obstruction deployment.
- **銀山鐵壁 — keep, then audit:** it is an obstruction control. The paired line does not create a symbolic gold-versus-silver code.

No family entry was edited in this task. These decisions are queued so the coordinator can propagate them deliberately.

## Omission and prose audit

- Every quoted Chinese claim in the entry is anchored by a stored occurrence.
- Each occurrence explains who speaks, where it appears, and what the line contributes.
- Translations describe the wording and deployment without deciding whether the masters approve, reject, transcend, or prescribe anything.
- Depth-role verdict: **6 standalone + 6 family + 2 contrast.** Sense 1 retains four standalone anchors (`玄關金鎖`, `金鎖難`, and two architectural lock scenes); sense 2 retains two standalone animal-chain anchors. Each sense therefore has independent standalone support after longer compounds are excluded.
- No nested compound is used to inflate depth, no apparatus-only text is admitted, and no unverified inherited interpretation survives.

## Public-feedback gate record

- feedback-observations: direct equations and opening/smashing predicates establish a barrier; animal entangling/breaking predicates establish a chain or fetter; palace gates and halls supply the ordinary ornate lock scene, while immediate jade/blue-green/glaze/jewel controls identify its register.
- feedback-inference-verdict: licensed as two distinguishable referents—ornate lock/barrier and binding chain/fetter. Architecture is the ordinary/stylized deployment of the same blocking lock, not a third thing. The lock modifier belongs to conventional ornate-palace diction; the chain modifier remains unresolved.
- feedback-falsification-searches: all exact 金鎖 contexts, 黃金 forms, barrier and chain verb frames, architecture, 金/銀 equations and paired images, material/color controls, refining similes, nested compounds, apparatus contamination, and close palace-poetry parallels were checked.
- feedback-counterexamples: local gold/Dharma-nature and refining comparisons do not transfer into this compound; the gold-shit saying reverses a fixed value; silver often behaves in parallel material/color series; animal-binding frames prevent one blurry sense, while palace and barrier frames preserve one blocking lock image.
- feedback-scope: conventional ornate-palace diction is licensed for the lock/barrier family; binding only for the animal-chain family; no corpus-wide symbolic value is assigned to `金`.
- lookup-probes: gold/golden/palace lock and barrier; gold/golden chain, fetter, restraint, and binding.
- modifier-relation-verdict: `conventional-name` in the broader sense of conventional ornate-palace diction for the lock/barrier image; `unresolved` for the chain/fetter modifier; `figurative-image` for the complete barrier and chain deployments.
- material-claim-verdict: rejected—no direct predicate establishes that this headword is made of gold; negative mentions in the article document that failed hypothesis.
- symbolism-verdict: rejected at headword scope—gold is not shown to mean enlightenment, value, purity, or preciousness in 金鎖.
- verb-frame-verdict: split—open/pierce/smash/close/barrier frames share one lock referent, while entangle/break/pull-apart animal frames denote a chain/fetter.
- opening-interpretation-verdict: **pass** — the lock sense first names the ornate palace fastening/barrier and explicitly rejects solid-gold hardware; the chain sense first names the animal-binding referent and its incompatible verbs. Both give the corpus-earned image before quotations without inventing symbolism.
