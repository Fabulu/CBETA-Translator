# WORK — t_61c90d3a8edd · 兼中到

**Rendering:** "arrival within both"
**Senses:** 1 (corpus-wide, SenseKey null). **Validation:** multi-source.
**Concordance (allowlist-scoped):** 276 occurrences in 84 texts (X-canon lamp/commentary + T-canon 語錄/傳燈錄).

## Method
Allowlist-only concordance; nearest-`<lb>` tracked; every KWIC raw-verified verbatim (6/6 PASS).
FromLb = ed="X" for X-canon (ed="R" noted, not used); ed="T" for the source verse.

## What the corpus shows (describe-only)
- **Literal:** 兼 both / holding-together + 中 within + 到 arrive/reach.
- **Origin:** the FIFTH and terminal rank of Dongshan Liangjie's Five Ranks. His own record
  (T47n1986B) has 師。作五位君臣頌云…兼中到。不落有無誰敢和。人人盡欲出常流。折合還歸炭裏坐;
  the closing 折合還歸炭裏坐 recurs 24× / 20 files.
- **Deployment:** fixed catechism 如何是兼中到 (104× / 57 files), capped variously (撥開雲外路。
  脫去月明前; 崑崙夜裏行; 十道不通耗 — each grep-verified as a cap to this question, X80n1565 /
  T51n2077).
- **Self-definitions (grep-verified):** 體用如如為兼中到 (X1457); 此即兼中到也。造道之極，理事俱泯
  (X1437); 兼中至，約功位雙彰時立，兼中到，約功位雙泯時立 (X1441); the epithet 功位俱隱 recurs 27×.
- **Shout mapping (grep-verified):** the 五燈全書 memorial 上堂 (X1571, entry of 百丈瑞白明雪禪師):
  我此一喝，聖凡情盡，能所兩忘，妙盡有無，是兼中到也; in its 濟宗 restatement 兼中到，即元要妙旨也.

## Variant question (per task — 兼中到 vs 兼中至)
Resolved from the corpus: **兼中到 (到) is the stable 5th rank.** 到 and 至 are near-synonym
graphs (both "arrive"); the texts distinguish 5th 兼中到 from 4th 兼中至 by the 功位雙泯 (both
extinguished) vs 功位雙彰 (both manifest) contrast (X1441). The real cross-text instability is at
the **4th-rank name**: 兼中至 (207×) is emended to 偏中至 (85×) by 寂音 (Juefan Huihong), recorded
and rejected in X1437 (大悞後學). So 兼中到 should not be confused with the 4th-rank pair
兼中至／偏中至.

## Attribution
T47n1986B verse → Dongshan Liangjie. Catechism + commentary occurrences raised/analytical →
MasterName null. cb:mulu head verified: X80n1565@0296a08 sits in the entry of 福州普賢善秀禪師.

## GATE 2 (verify-and-repair) — 2026-07-12
Independent re-derivation (linearizer with <note>/<rdg>/<orig> dropped; counts cross-checked by a
second gap-tolerant-regex method).
- KWICs: 6/6 EXACT CONTIGUOUS; lbs 6/6 correct (co-located ed=R claims verified). Contamination: 0.
  Attribution: verse = 洞山良价, catechism/commentary null — correct per rule.
- REPAIRED (draft counts under-derived): 269/82 → **276/84**; 如何是兼中到 80/47 → **104/57**;
  折合還歸炭裏坐 16/14 → **24/20**; 功位俱隱 24 → **27**; 兼中至 191 → **207**; 偏中至 83 → **85**.
- REPAIRED (quote fidelity): Dongshan verse and 此即兼中到也。造道之極… quoted with file
  punctuation; the vague "maps it onto a single shout and onto Linji's categories" replaced with
  the exact attested lines 我此一喝…是兼中到也 and 兼中到，即元要妙旨也 (X82n1571, added to
  SourceTexts).
- REPAIRED (links): dropped 雲居道膺 from RelatedMasters (no attested content in this entry);
  曹山本寂 kept (his record T47n1987A/B uses 兼中到 3×). RelatedTerms (the interrelating ranks +
  the 4th-rank variant pair) kept.
- JSON valid.

## Files
- entry.v2.json (1 entry, 1 sense, 6 curated occurrences). STATUS = verified.

## 2026-07-13 full remediation
- Corrected the concordance to 275 hits / 84 files and rebuilt to 7 total / 6 exact witnesses across 6 exact sources.
- Marked the fourth-rank naming dispute as contrast evidence. Added Gulin Qingmao's adjacent fourth/fifth catechism and kept 兼中至 (fourth, both manifest) distinct from 兼中到 (fifth, both extinguished).
- All KWICs/bounds and both audits pass.

## 2026-07-14 semantic remediation (r002 owner 2)

- research-paths: exact count/replay and adjacent-rank countersearches in `semantic-r002-owner2-countercounts-3.json`.
- feedback-inference-verdict: LICENSED — Dongshan Liangjie's own verse titles the fifth rank, and later sources explicitly distinguish it from the fourth.
- feedback-observations: T/T47/T47n1986B.xml#0525c07; X/X72/X72n1441.xml#0681b18; X/X82/X82n1571.xml#0164b20.
- feedback-falsification-searches: 兼中到, 如何是兼中到, 兼中至, 偏中至, adjacent catechisms, and fourth-rank naming dispute.
- feedback-counterexamples: 兼中至 and 偏中至 belong to the fourth-rank dispute and cannot be aliases or witnesses for the fifth.
- feedback-scope: one named fifth rank; explanations attributed to exact speakers.
- lookup-probes: arrival within both / arriving within both / fifth of the Five Ranks / fifth Dongshan rank.
- observation: the headword titles Dongshan's fifth verse and is contrasted with the fourth where merit and position are manifest rather than extinguished.
- minimal-inference: calling it the fifth rank is directly supported by ordered enumeration; no outside rank theory is imported.
- ordinary-bridge: arrival-language is retained in the English target before later house explanations.
- falsification-searches: adjacent name collision, alternate fourth-rank label, title sequence, incompatible enumerations, and nested compounds.
- counterexamples: the rejected fourth-rank alteration is retained as contrast evidence only.
- scope: fixed house category.
- verdict: licensed.
- nested-compound-verdict: no longer term donates a separate bare sense.
- verb-frame-verdict: title, catechism question, and explicit identification all point to the same rank.
- sense-target-distinguishability: ONE SENSE — later explanations and answer lines do not create different ranks.
- family-definition-retest: 兼中至 and 偏中至 stay distinct from 兼中到.
- opening-interpretation-verdict: PASS.
- omission-audit: all seven cards, including the contrast card, remain.
- plain-english-image-verdict: PASS.
- display-modifier-verdict: not applicable.
- Independent-review repair (2026-07-14): the Liutong and Gulin fifth-rank tokens occur in unnamed monks' questions; both respondents answer without repeating the headword and now remain context only. Both questioners exhausted the six-rung ladder. The fifth-rank definition and fourth/fifth contrast remain unchanged; the inference-bearing opening now names the fifth rank directly.
