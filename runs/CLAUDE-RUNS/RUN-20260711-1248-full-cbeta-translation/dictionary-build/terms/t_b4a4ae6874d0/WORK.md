# WORK — 異類中行 (t_b4a4ae6874d0) — CROSS-REF of the buffalo entry

Zen-scoped concordance (462-text allowlist filter applied). Grep of `異類中行` over allowlist files:
**239 hits / 97 texts.** Every KWIC below read verbatim from the named file.

## Concordance (key occurrences — verbatim)

| Chinese (verbatim) | CBETA id · lb | text | master / role |
|---|---|---|---|
| 泉曰。山前檀越家作一頭水牯牛去…今時人。須向異類中行始得。師曰。異即不問。如何是類。泉以兩手拓地 | X80n1565 0127b16 | 五燈會元 | **Nanquan** (+Zhaozhou) |
| 智頭陀前日道。智不到處切忌道著。道著即頭角生 | X80n1565 0172a06 | 五燈會元 | **Nanquan** (智頭陀 origin) |
| 泉曰：「他卻是異類中行。」…「不見道：智不到處切忌道著，道著即頭角生。直須向異類中行。」 | J34nB303 0375c07 | 南嶽…語錄 | **Nanquan** (via 雲巖, indep. witness) |
| 直須向異類中行道取異類中事。洞山曰。此事直須妙會 | T47n1987A 0534c11 | **曹山**元證禪師語錄 | **Caoshan** (Caodong technicalization) |
| 披毛戴角，是類墮；解向異類中行，不斷聲色，是隨墮…不受食，是尊貴墮 | J33nB294 0735a06 | 雲溪俍亭挺語錄 | **Caodong 三墮** doctrine |
| 南泉示眾曰…今時師僧須向異類中行。歸宗曰：雖行畜生行，不得畜生報 | X66n1296 0616b03 | 宗門拈古彙集 | **Guizong** (deflationary gloss) |
| 鴈過長空。影沉寒水…方解向異類中行 | X80n1565 0604a05 | 五燈會元 | **Tianyi Yihuai** (verse, corpus-wide) |
| 十、須向異類中行。凡欲紹隆法種，須盡此綱要 | X79n1559 0186b12 | 嘉泰普燈錄 | corpus-wide (綱要 item 10) |
| 雲門澄因僧問：如何是異類中行？澄云：輕打我，輕打我。僧遂作驢鳴 | X66n1296 1041b06 / J39nB454 / X72n1444 | multiple | **Yunmen-Cheng** stock koan |

## Sense analysis (3 senses)

1. **SenseKey=null (corpus-wide)** — "going among the different kinds." The literal move: the realized person goes down among ordinary beings/beasts, 披毛戴角, not clinging to the exalted. Guizong's 雖行畜生行，不得畜生報 is the plainest gloss. Broadly attested (97 texts), often in verse. **multi-source.**

2. **SenseKey="Nanquan Puyuan"** — locus classicus. The phrase is BORN here: the buffalo + 兩手拓地 exchange, rooted in the 智頭陀 saying 智不到處切忌道著，道著即頭角生. For Nanquan 異類中行 = acting where knowing cannot reach. Attested X80n1565 (twice, distinct exchanges), J34nB303 (indep. via 雲巖), X66n1296, J24nB137, X85n1591. **multi-source.**

3. **SenseKey="Caoshan Benji"** — Caodong technical term. Yaoshan→Yunyan→Dongshan→Caoshan adopt the phrase (via the 雲巖 bridge in J34nB303) and technicalize it: paired with 異類中事, 披毛戴角; woven into the 五位 and the 三墮 (類墮/隨墮/尊貴墮). Attested in Caoshan's own record T47n1987A + the 三墮 in J33nB294 + X79n1559. **multi-source.**

## Multi-source verdict — all three senses PASS

## RECONCILIATION with BUFFALO_ENTRY.v2.json (the assigned cross-ref)
The buffalo entry's Nanquan sense is `disputed` on two points:
- (a) the strongest *self=buffalo* line is Guishan's, not Nanquan's;
- (b) 異類中行 is "at least equally a Caodong technical term."

This entry resolves the ownership question cleanly and WITHOUT contradiction:
- Point (a) concerns the **buffalo-as-self** image, not the phrase 異類中行 — untouched here.
- Point (b): the CBETA Chinese shows the phrase **originates with Nanquan** (智頭陀 story, J34nB303 explicitly has 雲巖 carry it from Nanquan toward 藥山), and Caodong **adopts and technicalizes** it. So it is not a symmetric dispute: Nanquan = origin (multi-source), Caodong = downstream technical home (multi-source). Both are honestly senses; neither is "disputed" as to the phrase itself.
- Net: the buffalo entry's caveat B ("異類中行 is a Caodong term too") is CONFIRMED and given its own sense here; the buffalo `disputed` flag stays correct for the *buffalo-as-self* claim, which this entry does not disturb.

## Honest thin spots
- Did not census all 239 hits; corpus-wide sense asserted from representative sample + count.
- The 三墮 (類墮/隨墮/尊貴墮) is traditionally Caoshan's but my curated witness (J33nB294) is a later master quoting it; Caoshan's OWN record (T47n1987A) independently attests his technical use of 異類中行/異類中事, so the Caodong sense stands on ≥2 witnesses regardless.
- 天衣義懷 verse attribution taken from the 五燈會元's own framing; verse circulates detached in J25nB171/X64n1260.
- No ids or passages fabricated; every snippet read directly from the named allowlist file.

---
## GATE 2 (Claude adversarial verify+repair) — 2026-07-11
Re-derived every KWIC from the cited allowlist file (tag-stripped exact-substring search).
- **KWIC fixes (1):** sense-null occ X79n1559 0186b12 had a FABRICATED trailing `。` after `方坐得這曲彔床子` (source continues `，受得天下人…`). Trimmed to end at `曲彔床子` — now exact contiguous. All other KWICs (X80n1565 buffalo + 智頭陀, J34nB303, T47n1987A, J33nB294, X66n1296, X80n1565 verse) confirmed verbatim as-is.
- **Contamination:** NONE. All 11 RelPaths (occurrences + SourceTexts + prose ids incl. J32nB273) confirmed in zen-corpus.json.
- **FromLb corrections (7):** most FromLb pointed at the keyword line, not the KWIC start. Reset to nearest `<lb>` before each occurrence's first char: 0604a05→0604a04, 0616b03→0616b02, 0127b16→0127b15, 0172a06→0172a05, 0375c07→0375c06, 0534c11→0534c09, 0735a06→0735a05 (0735a06 was a false-match line; the 三墮 passage `隨緣放曠…披毛戴角，是類墮` starts at 0735a05). X79n1559 0186b12 correct.
- **Multi-source:** all 3 senses retain ≥2 independent verbatim allowlisted witnesses (null: X80n1565/X66n1296/X79n1559; Nanquan: X80n1565/J34nB303; Caoshan: T47n1987A/J33nB294) — validations honest, no downgrade.
- **Over-read / nesting:** renderings literal/deflationary; RelatedTerms are genuine semantic constituents (披毛戴角, 水牯牛, 異類中事, 類墮…), not coincidental prefixes. No changes needed.
- STATUS → verified.

## 2026-07-13 item-8 target and sense correction

- Re-tested the 268-hit/103-file concordance against `水牯牛`, `披毛戴角`, `異類中事`, the Nanquan exchanges, and the Caodong three-falls material. Nanquan and Caodong attach the phrase to distinct exchange clusters, but each still refers to going among the different kinds.
- Collapsed the three-sense interpretation menu to one corpus-wide sense and retained seven exact, non-duplicate witnesses. Nanquan and Caoshan remain on the master roster and in occurrence attribution; their deployments remain fully described without becoming separate definitions.
- Removed the earlier interpretive target `acting where knowing cannot reach`. The corpus explicitly associates the warning about knowing and speech with the phrase, but does not define that association as a new referent.
- Family re-test: 水牯牛 remains the animal used in several linked passages; 異類中事 and the falls remain Caodong collocations. None overturns the single literal target.
- sense-target-distinguishability: MERGE — the corpus-wide, Nanquan, and Caodong targets were different readings and technical settings of `going among the different kinds`, not different things.

## 2026-07-14 public-feedback semantic review

- research-paths: full exact concordance; all required definition formulas; direct-question, animal-conduct, buffalo, fur-and-horns, recompense, sounds/forms, three-falls, trace-free goose, and Caodong collocation families; exact replay and complete-unit attribution review of seven occurrences.
- feedback-inference-verdict: REVISE THE OPENING, KEEP ONE SENSE — ordinary syntax and repeated animal predicates license ‘move among other kinds of beings.’ Guizong Zhichang's explicit animal-conduct/recompense contrast and the public-question family license the narrower corpus deployment; they do not license a universal equation with realization, compassion, or acting beyond knowledge.
- feedback-observations: `異類中行` has 268 hits in 103 texts; `向異類中行` 129/67; `如何是異類中行` 47/30; `畜生行` 57/35; `不得畜生報` 18/17; `異類中事` 14/11; `披毛戴角` 220/100. Nanquan uses buffalo, ground-touching, knowledge, speech, and horns; Guizong contrasts conduct and recompense; Caodong records place the phrase beside other-kind matters and the falls.
- feedback-falsification-searches: searched direct definitions, ordinary interspecies motion, every case question, animal and non-animal objects, master-specific menus, the three falls, and neighboring `異類` compounds. No person, title, place, or master-owned referent distinct from the motion emerged.
- feedback-counterexamples: Tianyi Yihuai's trace-free geese and Nantang Yuanjing's ten requirements broaden deployment beyond the Nanquan buffalo and the Caodong falls. Guizong's refusal to equate animal conduct with animal recompense blocks a crude ‘become an animal’ definition. Lanting Ting, not Caoshan Benji, is the responsible voice of the later three-falls witness.
- feedback-scope: the entry says that the phrase names movement among other kinds of beings and reports its attested animal/public-interview clusters. It does not rule on the status of the mover or reduce all uses to one doctrinal interpretation.
- lookup-probes: `go among other species`; `move among animals`; `walk among other kinds`; `among different beings`; `animal conduct without animal recompense`.
- opening-interpretation-verdict: PASS after revision — the opening now names the relation, the contrast class, and the dominant animal scene before presenting distinct speaker deployments.
- definition-and-sense-verdict: KEEP one sense. Nanquan, Guizong, Tianyi, Nantang, Caoshan, and Lanting deploy the same motion in different arguments and technical clusters; origin, later adoption, and commentary are not different lexical things.
- sense-target-distinguishability: one sense only. Preferred and alternate targets remain translations of the same motion rather than a menu of competing readings.
- family-verdict: compatible with `披毛戴角`, `水牯牛`, `異類中事`, `類墮`, `隨墮`, and `尊貴墮`; these are collocations and neighboring technical families, not definitions imported into the headword.
- provenance-verdict: all seven notes now name source and responsible voice. The final occurrence is corrected from Caoshan Benji to Lanting Ting, with Caoshan retained only as an invoked figure.
- propagation-verdict: later school technicalization and an originating master's case must not become separate senses unless they name different things. Define the shared verb relation, then attribute each local predicate and preserve counterstatements that narrow overinterpretation.
- final-gate: `maintenance/semantic-cohorts/semantic-r002-owner1-yileizhongxing-gate.json` hard-passed with 7/7 exact KWICs and zero attribution, public-feedback, or depth/sense failures. Final entry SHA-256 `1f18014e932b43a7fe9633ee38c1c5b9fd1daa50d85d7a66a49c620d5a1422f9`.

## 2026-07-14 reviewer3 quoted-turn repair

- Rechecked the two multi-speaker witnesses. In the lineage case collection, Nanquan Puyuan speaks the exact headword line and Guizong Zhichang responds without it. In the Caoshan record, the passage explicitly marks the exact line as Nanquan's quotation and Caoshan's following comment lacks the token.
- Assigned Nanquan as exact speaker in both; retained Guizong and Caoshan only as respondent/commentator context.
- Re-gate: `semantic-r002-owner1-yileizhongxing-gate.json` hardPass=true; 7/7 exact KWICs and zero attribution failures; repaired entry SHA-256 `2d2923b2d1b29afb0bc7a4a1b06b89e42897673a651f6c676cfa408a22ab6959`.
