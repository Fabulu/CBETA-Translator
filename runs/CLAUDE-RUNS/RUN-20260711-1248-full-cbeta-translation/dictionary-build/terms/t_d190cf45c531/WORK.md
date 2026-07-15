# 話頭 — WORK

**Id:** t_d190cf45c531 · a word or saying raised for investigation, versus a remark in an exchange

## Concordance (Zen allowlist only; verbatim)
Enormous: **297 allowlist texts / 2488 hits.** Two clearly distinct usages. Selected defining hits:

| hit (verbatim) | CBETA | lb | master |
|---|---|---|---|
| 洞山，山云：好箇話頭，只欠進語。 | X79n1557 | 0573a09 | Dongshan Liangjie |
| 老大宗師話頭也不識。師曰：放你三十棒。 | X85n1590 | 0597b04 | (unattrib.) |
| 只就動止處。看箇話頭…僧問趙州。狗子還有佛性也無 | T47n1998A (大慧語錄) | 0886a04 | Dahui Zonggao |
| 就未拔處。看箇話頭。僧問趙州…州云無。行住坐臥但時時提掇 | T47n1998A | 0900b05 | Dahui Zonggao |
| 通身力量用在一句無義味話頭上…一朝黑漆桶忽然爆裂 | X72n1437 (博山參禪警語) | 0424a18 | Wuyi Yuanlai |
| 直至宋朝，始有看話頭、作工夫之說…提起箇話頭 | X72n1440 | 0938a16 | (retrospective) |
| 令參：狗子無佛性，且問誰拖汝死屍來？ | X70n1400 (高峰語錄) | 0700a09 | Gaofeng Yuanmiao |

## Sense analysis
1. **null / corpus-wide (older, literal)** — "speech-head" = the utterance/topic on the table (頭 a noun-suffix, cf. 石頭/舌頭). A master appraises a remark (Dongshan: 好箇話頭，祇欠進語) or scolds a monk for missing its point (話頭也不照顧 / 話頭也不識). No mystique. Pre-Song and persistent.
2. **corpus-wide raised-word deployment** — one short saying or question is explicitly looked at, raised, taken up, remembered, recited, or investigated. The attested examples include “no,” the dog question, the cypress tree in the courtyard, three pounds of hemp, and “who drags this corpse?” The later retrospective says, “not until the Song was there talk of looking at a saying” (直至宋朝，始有看話頭之說). These are literal recorded instructions and descriptions, not a method category imported by the entry.

## Multi-source verdict
**Both senses multi-source.**
- Older sense: Dongshan (Caodong), plus the recurring 話頭也不識/不照顧 idiom across many yulu.
- Raised-word sense: Dahui's own record tied to Zhaozhou's “no,” Yongjue Yuanxian's hall address, Gaofeng's record, direct definitions, and the explicit Song-origin retrospective supply independent texts across later periods.

## Honest thin spots
- The raised-word sense is corpus-wide, not keyed to Dahui. Dahui supplies prominent directions, while later masters define, repeat, criticize, and vary the deployment.
- X72n1440's Song-dating line is a later retrospective; masterName left null with an AttributionNote rather than guessing the compiler.
- The single most-important 話頭 (Zhaozhou's 無) is documented under the 佛性 entry (sense Zhaozhou); cross-linked via RelatedTerms 無 / 狗子無佛性.

---

## GATE 2 (Claude adversarial verify + repair) — STATUS: verified

- **All 7 occurrence KWICs verbatim exact-contiguous** in their cited files (targeted per-file search; raw breaks were only `<lb/>` whitespace). Zero ellipses.
- **No contamination:** all RelPaths (X79n1557, X85n1590, X80n1565, T47n1998A, X72n1437, X72n1440, X70n1400, X63n1255) are in zen-corpus.json.
- **Multi-source honest:** sense 1 older/literal (Dongshan X79n1557 + unattrib. X85n1590; 話頭也不照顧 also verbatim in X80n1565) and sense 2 kanhua method (Dahui T47n1998A ×2 + Boshan X72n1437 + Song-retrospective X72n1440 + Gaofeng X70n1400) both ≥2 independent witnesses → multi-source retained.
- **Repair made:** Explanation sense 1 quoted Dongshan as `好箇話頭，祇欠進語` (祇). The source (X79n1557 @0573a09) reads `只欠進語` (只). Corrected to 只.
- FromLb on the Gaofeng occurrence (X70n1400 0700a09) verified correct — the nearest preceding `<lb>` to 見雪巖欽 is 0700a09 (a duplicate-`n` lb from another edition earlier in the file caused a false-positive scare, resolved).
- RelatedTerms (看話, 疑情, 提撕, 無, 狗子無佛性, 進語, 公案) are genuine semantic relations, not coincidental prefixes.

## Gate-3 REVISE fix (2026-07-11 17:47 +0200)
Fixed the X72n1437 misattribution + one prose nit flagged by Gate-3 (Fable).
Verified against source C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5\X\X72\X72n1437.xml:
<title level="m">永覺元賢禪師廣錄</title> (line 9); KWIC's 上堂 opens "今日張華宇居士命老僧冒登此座" at lb 0396a18 (ed=X) / 0424a06 (ed=R125), and "一句無義味話頭上..." at 0396b06/0424a18 — i.e. Yongjue Yuanxian's OWN upper-hall talk, not Boshan's 參禪警語.
Changes (sense 2):
- Fix (a): Occurrence 3 (X72n1437) MasterName: "Wuyi Yuanlai" -> "Yongjue Yuanxian"; rewrote AttributionNote (dropped "Boshan / Chan Police Warnings", now cites his 上堂 to 張華宇). Explanation "(Boshan: 用在一句無義味話頭上)" -> "(Yongjue Yuanxian: ...)". RelatedMasters: "Wuyi Yuanlai" -> "Yongjue Yuanxian".
- Fix (b): AttributionNote on the 0900b05 occurrence read "(提撕)" but the source line reads 提掇 (prose parenthetical, not KWIC) -> corrected to "(提掇)". Explanation's separate 提撕 use stands (genuine Dahui vocabulary, T47n1998A 0886a03).
All 7 KWIC strings unchanged (verbatim). Validation stays multi-source. STATUS=verified.

## L004-A item-8 ledger and full retest (2026-07-13)

- sense-target-distinguishability: KEEP “the word or saying raised for investigation” versus “a saying, remark, or turn in an exchange.” Pair 1–2 names different referents: a selected word/question explicitly assigned or raised for continued examination versus the conversational words just spoken and appraised, returned, remembered, or misunderstood in an encounter.
- Family retest: “look at a saying,” “raise the saying,” “take it up,” “remember the saying,” “return my saying,” “a fine saying,” “does not recognize the saying,” public case, doubt, follow-up line, Zhaozhou's “no,” the dog question, and the cypress-tree answer can all remain true without turning every conversational remark into an assigned word or every assigned word into a newly spoken turn.
- Definition retest: Hanyue's direct definitions and Ruibai's formula remain evidence for the raised referent; Dongshan, the two Yunmens, and Fuyan Liangya retain the conversational referent. The split is by object and deployment, not by rival readings of one occurrence, grammar, or a Dahui-specific ownership claim.
- #0g retest: the ordinary “speech-head” is bent in later records into a selected word or saying that is explicitly looked at, raised, remembered, and investigated. The entry translates those actions literally and does not relabel them with Japanese or technique vocabulary.
- Depth decision: thirteen anchors across nine source texts exceed the 2,575-hit floor and preserve direct definitions, explicit assignments, retrospective dating, criticism, public question, conversational appraisal, returned speech, and case commentary without duplicate padding.

## semantic-r001 public-feedback remediation (2026-07-14)

- feedback-inference-verdict: KEEP two different referents. The primary sense is a selected word, question, or saying assigned or raised for continued examination; the second is the remark or conversational turn just spoken in an exchange. This is not a noun/verb or interpretive-reading split.
- feedback-observations: Direct definitions by Hanyue Fazang and Ruibai Mingxue, Dahui Zonggao's immediate assignment of Zhaozhou's dog question, and the action frames “look at,” “investigate,” “raise,” “take up,” and “originally investigated” establish the primary referent. Dongshan Liangjie's appraisal, Yunmen Wenyan's demand to return his saying, and Fuyan Liangya's report about understanding another speaker's words establish the conversational referent.
- feedback-falsification-searches: Re-ran the full allowlist count for the headword and definition formulas, including “what is called the saying,” “therefore it is called the saying,” “what is the saying?,” and “how is the saying?”; also retested “look at,” “investigate,” “raise,” “take up,” “originally investigated,” “dead saying,” “return my saying,” “fine saying,” and “does not recognize the saying.” Tested the rare nested forms “saying-tail” and “saying-waist” and the neighboring families public case, looking at sayings, doubt, and follow-up line.
- feedback-counterexamples: Conversational appraisals and demands to return the speaker's words disprove a blanket equation of every occurrence with an assigned object of investigation. Explicit assignments, definitions, and sustained-examination predicates disprove reducing every occurrence to a generic remark. The retrospective dating claim remains attributed evidence, not the dictionary's ruling on origin.
- feedback-scope: Both senses are corpus-wide. Dahui Zonggao is a prominent primary-sense witness but does not own it; later independent masters define and deploy it. The conversational use spans multiple encounter collections and speakers.
- lookup-probes: Reader probes covered “investigated saying,” “assigned saying,” “saying under examination,” “critical phrase,” “word under investigation,” “spoken remark,” “conversational turn,” “words just spoken,” and “remark under discussion.” These are now sense-approved SearchAliases rather than silent editorial synonyms.
- opening-interpretation-verdict: Replaced the graph-first opening with the corpus-earned deployment: later records select a word, question, or saying and explicitly direct people to continue examining it. The ordinary bridge is a unit of speech becoming the object of continued attention; the second sense preserves the ordinary use for words in the present exchange.
- definition-formula-audit: Twenty-four hits in twelve texts for the headword plus the following particle, six in four for “what is called the saying,” two in one for “therefore it is called the saying,” one for “what is meant by the saying,” and three in three for “how is the saying?” The stored direct definitions represent independent definitional and questioning frames without treating every formula as agreement.
- nested-family-audit: “Saying-tail” and “saying-waist” occur as rare wordplay in a question to Tiemei Sanbazhang; they do not license general body-part definitions. Looking at sayings, doubt, public case, follow-up line, Zhaozhou's “no,” the dog question, and the cypress-tree answer remain related but separate entries or families.
- modifier-and-provenance-audit: No feedback modifier is at issue. Each source-and-speaker note was re-read; vague own-record labels and Chinese-first titles were replaced with English-first source titles plus exact named speakers. The reviewed unnamed monk remains named by role only after the stored six-rung review, while Tiemei remains context rather than the question's speaker.
- semantic-propagation: The distinction must be preserved in 看話, 疑情, 公案, 進語, 無, and 狗子無佛性. Those entries must not silently collapse a selected saying under examination into any conversational remark, or vice versa.
- final-cohort-gate: `run_cohort_gate.py` hardPass=true; exact KWIC 13/13, attribution hard failures 0, public-feedback flags 0, depth/sense hard failures 0, review flags 0, and forbidden-English matches 0. Report: `maintenance/semantic-cohorts/semantic-r001-owner1-huatou-gate.json`.
