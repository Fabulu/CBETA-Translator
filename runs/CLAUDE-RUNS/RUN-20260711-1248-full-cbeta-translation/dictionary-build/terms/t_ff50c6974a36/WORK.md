# 五位 (t_ff50c6974a36) — work notes

**STATUS: verified** (Gate 2, adversarial re-derivation from Chinese). Two KWICs REPAIRED — see below.

## Gate 2 verification (independent Claude pass)
Re-grepped each cited file. Found and fixed TWO non-verbatim KWICs (both anti-pattern violations):
- **J10nA158 — ELLIPSIS fix.** Old KWIC ended `…嘗在面門出入……」` — the `……」` was editorial, replacing the real text. Source (lb 0040b17→b20) reads `…嘗在面門出入，未證據者看看。」`. Replaced with the true exact-contiguous span `然後所立君臣、偏正、王子、功勳各五位者，若我臨濟大師要且不然，但曰：「赤肉團上有無位真人嘗在面門出入，未證據者看看。」`. FromLb 0040b18 → **0040b17** (opening 然 is on b17).
- **J26nB188 — STITCH fix.** Old KWIC read `…石女夜不眠。」……「如何是兼中到？」…`, an `……` splice that dropped three whole ranks (偏中正/正中來/兼中至). Replaced with the FULL exact-contiguous walk-through of all five ranks (lb 0756a04→a08): `五位君臣事若何？」師云：「爐中香裊裊。」進云：「如何是正中偏？」師云：「石女夜不眠。」進云：「如何是偏中正？」師云：「秋到樹頭赤。」進云：「如何是正中來？」師云：「泥牛上五台。」進云：「如何是兼中至？」師云：「黃葉頗相似。」進云：「如何是兼中到？」師云：「踏破波底月。」`. ToLb 0756a05 → **0756a08**.
- Verbatim-confirmed (already good): J25nB163 (0256a14), C078n1720 (0787b02), J25nB156 (0062c07); Sense A B25n0144 (0676a05), J26nB180 (0295b04).
- Contamination: none. Sense split kept (Caodong technical sense B = multi-source Dongshan/Caoshan; generic Sense A = provisional). Over-read: none — explanation explicitly deflationary ("a pedagogical device… not a cosmology or a ladder of stages"). RelatedTerms genuine constituents/parallels.


## Concordance (Zen allowlist only)
- **211 allowlist files, ~447 occurrences.** Method as for the other terms (grep → filter to `zen-corpus.json` → verbatim KWIC + nearest `<lb n>`).

## Sense analysis — TWO senses

### Sense B (preferred): the Caodong Five Ranks — SenseKey/MasterName = Dongshan Liangjie
The overwhelmingly dominant sense. 五位 = Dongshan Liangjie's dialectic of 正 (upright/host/whole) and 偏 (crooked/guest/particular) in five interpenetrating configurations:
**正中偏 · 偏中正 · 正中來 · 兼中至(偏中至) · 兼中到** — the full set is walked through in interview form at J26nB188 (正中偏…兼中到).
Recast forms, all attested:
- **君臣五位** (ruler/minister) — the single commonest collocation (五位君臣).
- **五位對賓** (five ranks facing the guest) — Caoshan Benji's catechism: 「某甲從偏位中來，請師正位中接」 (J25nB163, C078n1720 — two independent witnesses).
- **功勳五位** (merit) and **王子五位** (princes) — Dongshan's parallel sets; J10nA158 lists all four schemata (君臣、偏正、王子、功勳各五位).

Corpus-wide, 五位 is the **emblem of the Caodong house**, almost invariably paired with Linji's **三玄三要**: 洞山五位／臨濟三玄 (J25nB156), 不是臨濟三玄不是曹洞五位 (D51n8948), 臨濟三玄…曹洞五位 (dozens more). Effectively "the Caodong method."

Deflationary reading: a *pedagogical device* for showing the reciprocal fit of absolute and particular / host and guest — NOT a cosmology, NOT a graded ladder of attainment.

### Sense A (allowed, minor): generic "five positions / five stages" — SenseKey=null
The plain Chinese word, used non-technically even inside Zen texts:
- 寄因五位，乃至果位 (doctrinal path-positions, 因位→果位) [B25n0144, Zutang-ji narrative]
- 第五位聖人 (ordinal "fifth-ranked sage") [J26nB180]
- also embedded in 五十五位 (the 55 stages) [J26nB178]
Disambiguation signal: NO 正/偏/君臣, NO Dongshan/Caoshan, NO 臨濟三玄 pairing → generic.

## Multi-source verdict
- **Sense B: multi-source.** Attested across J10nA158, J26nB188, J25nB163, C078n1720, J25nB156, D51n8948, J27nB189, B27n0152, and many more; multiple masters; both the 正偏 and 君臣/對賓 forms independently witnessed.
- **Sense A: provisional.** Only a handful of witnesses and semantically heterogeneous (ordinal vs. doctrinal path-stages); they cohere only as "the ordinary word, not the Caodong term." Recorded for disambiguation, not asserted as a unified Zen sense.

## Attribution note (NOT a dispute, a division of labour)
The 正偏 (upright-crooked) Five Ranks are **Dongshan Liangjie's** creation; the **君臣** formulation and the **五位對賓** catechism are especially **Caoshan Benji's** systematization. SenseKey set to Dongshan (originator); Caoshan co-attested in two curated occurrences. Witness J10nA158 is a **Linji-house** master reciting the four fivefold schemata *in order to contrast them* with Linji's 無位真人 ("true person of no rank") — a valuable outside witness that still fixes the term's meaning.

## Honest thin spots
- Sense A is deliberately thin (2 curated); could be padded but that would overstate a peripheral generic usage.
- The five rank-names appear in many texts *without* the literal graph 五位 (e.g. J26nB178's 正中偏/偏中正/正中來/兼中至 sequence, which I instead used for 家風); I curated only occurrences that contain the string 五位 itself.
- MasterName null on nA158 and nB188 (speaker not the originating master / not in `master-dates.json`); Caoshan tagged on nB163 and C078.

## Item-8 retained-sense ledger

- `sense-target-distinguishability: KEEP` — **the Five Ranks** is a named technical set marked by upright/crooked or ruler/minister constituents; **five positions** counts ordinary doctrinal, ordinal, or path positions. A named system and an ordinary set of five positions are different referents, not different readings of the same system.
- Depth/source retest: 1,155 allowlisted hits in 212 files; 7 curated anchors across 7 source texts (5 technical, 2 ordinary). The technical evidence spans the Dongshan/Caoshan family and outside citation; the ordinary evidence is kept deliberately distinct rather than padded with technical occurrences.
- Family/definition retest: `正偏五位`, `君臣五位`, `五位對賓`, Dongshan, and Caoshan identify the technical family; ordinal `第五位` and path-position grammar identify the ordinary family. The ordinary preferred target is now the clean standalone **five positions**, with **five stages** retained only as an alternate.
- #0g retest: the entry locates the named Five Ranks in their Chan house deployment and preserves Linji-house contrast, while refusing to relabel every ordinary fivefold sequence as the Caodong system.

## 2026-07-14 semantic hard-pass ledger

- feedback-inference-verdict: PASS — `the Caodong Five Ranks` names the technical system and the opening immediately identifies its five upright/crooked configurations, ruler/minister recasting, house affiliation, and interview deployment; `five positions` identifies the ordinary count.
- feedback-observations: Dongshan Liangjie supplies the five named rank verses. Caoshan Benji defines upright as the formless realm and crooked as the realm of forms, maps the pair to ruler and minister, and stages the system as an interview. Huiyan Zhizhao classifies the merit, ruler-and-minister, prince, substance/function, and succession families. Foguo and Miyun supply outside-house contrasts; generic witnesses count doctrinal, prenatal, categorical, and ordinal positions.
- feedback-falsification-searches: Rechecked 五位 1155/212, 正中偏 315, 偏中正 296, 兼中到 270, 正中來 269, 兼中至 192, 偏中至 86, 五位君臣 188, 君臣五位 46, 洞山五位 111, 曹洞五位 30, 洞上五位 13, 五位王子 41, 王子五位 11, 功勳五位 20, 五位功勳 17, 五位對賓 16, 五十五位 9, 五位百法 3, and 在胎五位 1.
- feedback-counterexamples: Generic ordinals and doctrinal classifications lack the Caodong constituents and cannot be projected into Dongshan's scheme. Conversely, ruler/minister, upright/crooked, rank names, Dongshan/Caoshan attribution, or Linji three-mystery contrast identify the technical system rather than an arbitrary count of five.
- feedback-scope: Two corpus-wide referents: a named Caodong technical system and the ordinary count of five positions, ranks, stages, or categories.
- lookup-probes: Technical: `Caodong Five Ranks`, `Dongshan's five positions`, `upright and crooked five ranks`, `ruler and minister five ranks`, `Caoshan's five-rank system`. Generic: `five stages`, `five positions`, `fifth rank`, `five doctrinal positions`, `fivefold classification`.
- opening-interpretation-verdict: PASS — the technical opening explains the configurations and their observable house and interview use; the generic opening states the ordinary ordinal or categorical function.
- definition-and-sense-verdict: KEEP the split. A named five-member Caodong system and an unrestricted count of five positions are different referents, not readings or grammar of one scheme.
- sense-target-distinguishability: KEEP — `the Caodong Five Ranks` and `five positions` are distinguishable from PreferredTarget alone.
- family-verdict: Upright/crooked, ruler/minister, merit, princes, facing-the-guest, Linji contrast, variant fourth-rank name, ordinal, hundred-elements, fifty-five-stage, and prenatal families were cross-checked and assigned by local evidence.
- provenance-verdict: All seventeen stored KWICs are exact. Ten newly stored passages anchor every formerly dangling Chinese claim rather than deleting it; all 72 Chinese prose strings now match an occurrence. Every occurrence names its source and speaker or retains a complete six-rung reviewed-unnamed actor record.
- propagation-verdict: Added five natural English retrieval probes per sense, replaced the parenthetical preferred target with a clean standalone target, and preserved all substantive source interpretations with exact anchors.
- final-gate: `semantic-r002-owner1-wuwei-gate.json` hardPass=true; 17/17 exact KWICs verified, 72/72 Chinese prose strings anchored, and zero exact or attribution failures; entry SHA-256 `08fd34364e3952b4479714c20f9f5ca4d8e8081a963a702105892d8c47a9e504`.

## 2026-07-14 reviewer3 ownership and exact-turn correction

- sense-owner verdict: REVISE. The technical sense remains corpus-wide (`SenseKey: null`), so its former sense-level `MasterName: Dongshan Liangjie` contradicted the explicit note that historical origin does not make later shared vocabulary private. The sense-level owner is now null; Dongshan remains fully named in the explanation, occurrence, and related roster.
- Sanyi exact-turn verdict: REVISE. The exact token occurs in the unnamed question `如何是洞上五位`; Sanyi Mingyu's answer `除夜舊門神` does not repeat it. The occurrence now has a six-rung reviewed-unnamed questioner packet, with Sanyi retained only as context respondent and record owner.
- ownership-propagation: historical origin, systematization, later house circulation, and exact utterance are separate metadata questions. A corpus-wide sense must not silently acquire a private owner, and a respondent must not inherit a token spoken only in the question.
- revised-final-gate: `maintenance/semantic-cohorts/semantic-r002-owner1-wuwei-gate.json` hard-passed with 17/17 exact KWICs, all 72/72 Chinese prose strings anchored, and zero attribution, public-feedback, or depth/sense failures. Current entry SHA-256 `6cb3136178516752fc94a2fbafa05cb4376f1529c919fe92fc29df1f76b97bd7`.
