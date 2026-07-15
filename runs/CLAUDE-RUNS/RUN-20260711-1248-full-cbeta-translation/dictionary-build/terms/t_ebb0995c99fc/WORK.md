# WORK — 頓悟 (t_ebb0995c99fc)

## Concordance (Zen allowlist only)
- 頓悟 = 878 hits across 183 allowlist files. Corpus-wide Chan doctrinal term.
- 南頓北漸 = 10 hits / 8 files (T48n2008, T51n2076, X80n1565/1568, X64n1276, X81n1571, X85n1593, B14n0082) — the Southern-sudden/Northern-gradual emblem.
- 頓悟漸修 (sudden awaken, gradual cultivate) attested T48n2016, T48n2017, T48n2020, T51n2076, X63n1225/1255, X67n1303, J29nB240 — the Zongmi framework.
- 言下頓悟 (awaken at a word) common in yulu (C078n1720, D48n8939, J24nB137, J26nB182, J27nB198, J34nB311, B14n0082, B25n0145).
- Zongmi four-fold matrix (漸修頓悟／頓悟漸修／漸修漸悟／頓悟頓修) + 楞伽 四漸四頓 in 宗鏡録 T48n2016.

## Sense analysis
ONE corpus-wide sense (SenseKey=null): "sudden awakening" — to awaken all at once, in a single instant, without gradual stages (亦無漸次); the standing opposite of 漸 (gradual). Emblem of the Southern/Huineng school (南頓北漸).
- 頓悟漸修 / 理頓事漸 are the SAME 頓悟 (awakening is sudden) combined with a gradual-cultivation model — not a separate sense; noted in explanation.
- Deflationary: temporal manner of awakening (all-at-once vs by-degrees), NOT a mystical event. Distinguished 頓悟 (awakening at once) vs 頓修 (cultivation at once).

## Speaker attribution
- T48n2008 = 六祖壇經 (Dunhuang); 頓悟頓修…無漸次 is Huineng's discourse (南頓北漸 section) → MasterName="Huineng" (canonical, master-dates.json 慧能/Huineng 638).
- T51n2076 = 景德傳燈錄; 南頓北漸 gloss is biography narrative (帝曰 frame) → null.
- C078n1720; 言下頓悟 fire-story is narrative → null.
- J28nB208 = 古雪哲禪師語錄; 理頓事漸 is the master's 普說, but 古雪哲 not in the 301 canonical list → MasterName=null, named in note (mirrors 無心/Benjing handling).
- T48n2016 = 宗鏡録 (Yongming Yanshou) quoting Zongmi's matrix → quotation → null, Zongmi named in note.

## KWIC verification
All 5 KWICs confirmed EXACT contiguous tag-stripped substrings via /tmp/kwic.py (handles the <pb>-interrupted J28nB208 span, contiguous once tags stripped). FromLb = nearest preceding <lb n>, ed="T"/"C"/"J" per canon. No X-canon curated.

## Multi-source verdict
**multi-source.** Holds across Platform Sutra (T48n2008), 景德傳燈錄 (T51n2076), C078n1720, 古雪哲語錄 (J28nB208), 宗鏡録 (T48n2016) + ~180 other files; consistent Southern-school sense across the whole record.

## RelatedTerms / RelatedMasters
- RelatedTerms: 漸修, 頓悟漸修, 南頓北漸, 頓修, 見性, 悟 (genuine 頓/漸 constituents and the 見性 outcome).
- RelatedMasters: Huineng (paradigm of 頓), Guifeng Zongmi (systematizer of the 頓/漸 × 悟/修 matrix). Both confirmed canonical.

## GATE 2 (Claude adversarial verify+repair)
- KWIC re-derivation: all 5 occurrences re-checked contiguous against the cited file — each found, count=1, EXACT (incl. the <pb>-interrupted J28nB208 span). FromLb = nearest preceding <lb>: T48n2008 0358c27 (ed=T), T51n2076 0269b05 (ed=T), C078n1720 0836a11 (ed=C), J28nB208 0358b22 (ed=J), T48n2016 0626b29 (ed=T). All confirmed. No X-canon → no ed=R ambiguity.
- Contamination: 0. All 5 RelPaths + all 8 SourceTexts in allowlist.
- Attribution: CONFIRMED. T48n2008 六祖壇經, context 師曰：「自性無非…自性自悟，頓悟頓修… — 師 = Huineng, his own discourse → MasterName=Huineng correct. Other four null (景德傳燈錄 narrative / fire-story narrative / 古雪哲 non-canonical / Zongmi quotation) — confirmed.
- Multi-source: holds across T48n2008 (Platform Sutra), T51n2076 (景德傳燈錄), C078n1720, J28nB208, T48n2016 (宗鏡録). Stays multi-source.
- Explanation quotes grep/strip-verified: 自性自悟，頓悟頓修，亦無漸次 (T48n2008, exact w/ ，), 開導發悟有頓漸之異…非禪宗本有南北之號也 (verbatim in the CITED T51n2076 with 。 — grep missed it only because an <lb> splits the phrase), 一漸修頓悟…四頓悟頓修 (T48n2016). REPAIR: normalized the 南頓北漸 gloss punctuation from ，to source 。 so it byte-matches T51n2076. No unverifiable claims remain.
- Deflationary ("temporal manner of awakening, not a mystical event") — no over-read. VERDICT: verified.

## Full remediation

- Refreshed the concordance to 902 hits / 186 texts and rebuilt depth to seven exact headword witnesses plus one marked family witness.
- Added Guifeng Zongmi's explicit definition, Dazhu Huihai's treatise title, and Yulin Tongxiu's direct capacity question; expanded the Hongbian and Guxue contexts.
- Named every speaker or responsible author and normalized `SourceTexts` to the seven occurrence files.
- Re-tested the sense: definition, encounter formula, classification, admonition, and title all retain the same sudden-awakening event; sudden refining remains explicitly distinct.

## Semantic remediation r002

- feedback-inference-verdict: REVISE — the sense was sound, but the former graph-by-graph opening delayed the ordinary English contrast; it now defines the event before presenting classifications.
- feedback-observations: Huineng's no-gradual-stages wording, the encounter formula “awakened suddenly at the words,” Zongmi's explicit definition, and the sudden/gradual matrices all identify the same temporal claim about awakening.
- feedback-falsification-searches: Re-tested title use, encounter narration, Southern/Northern classification, sudden-awakening/gradual-refining combinations, and sudden-refining combinations across 902 hits in 186 texts.
- feedback-counterexamples: A treatise title containing the headword does not turn the headword itself into a title sense, and sudden awakening paired with gradual refining does not make awakening and refining the same event.
- feedback-scope: Corpus-wide event term; the entry reports competing sudden/gradual classifications without deciding whether the event occurs or what it metaphysically consists in.
- lookup-probes: `instant awakening`, `immediate awakening`, `all-at-once awakening`, `sudden enlightenment`, and `instant realization` are explicit aliases; none substitutes a neighboring refining term.
- opening-interpretation-verdict: REPAIRED — the opening now says “an awakening said to occur all at once rather than by gradual stages,” which supplies the usable English contrast immediately.
- plain-english-image-verdict: PASS — “all at once rather than by gradual stages” gives a reader the temporal picture without adding a mystical mechanism.
