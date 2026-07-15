# WORK — 作麼生 (t_51fe593d9ffe)

## Frequency (allowlist-scoped)
- 412 allowlist files contain it (extremely high frequency). The stock challenge-interrogative of Chan dialogue.

## Concordance (curated, verbatim; speaker confirmed against chapter/section head)
| KWIC | Text | FromLb | Speaker | How confirmed |
|---|---|---|---|---|
| 是爾如今與麼聽法底人作麼生擬修他、證他、莊嚴他？ | T47n1985 臨濟語錄 | 0499b16 | Linji Yixuan | 鎮州臨濟慧照禪師語錄; 師云 discourse |
| 師問溈山。併却咽喉唇吻。作麼生道。山曰。却請和尚道。 | X80n1565 五燈會元 | 0071b18 | Baizhang Huaihai | mulu 洪州百丈山懷海禪師 (line 5544); 溈山．五峯．雲巖侍立次，師問溈山 |
| 恁麼不恁麼總不得。子作麼生。師罔措。 | X80n1565 五燈會元 | 0109b01 | Shitou Xiqian, to Yaoshan | mulu 澧州藥山惟儼禪師; 頭(=石頭)曰…子作麼生, 師(=藥山)罔措 |
| 後溈山問仰山：「此二尊宿意作麼生？」仰山云：「和尚作麼生？」 | T47n1985 臨濟語錄 | 0503a25 | Guishan Lingyou ↔ Yangshan Huiji | 勘辨 section (mulu line 859); appended Guishan/Yangshan exchange |

## Sense analysis
Single corpus-wide sense: colloquial "how? / in what way? / what about it?" — the demand for an immediate live response, sibling-and-answer to the demonstrative 恁麼 ("thus how?"). Fixed collocations curated:
- 作麼生擬… "how would you…" (Linji) — the plain interrogative
- 作麼生道 "how will you speak?" — demand to utter (Baizhang→Guishan)
- 子作麼生 "you — what will you do?" — challenge to the person (Shitou→Yaoshan)
- 意作麼生 "what was the meaning?" — probe of a case (Guishan testing Yangshan)

## Multi-source verdict: MULTI-SOURCE
2 independent texts (臨濟語錄, 五燈會元); masters Linji, Baizhang, Shitou, Guishan, Yangshan. Pervasive.

## Deflation check
Rendered as a live challenge "how?/what?", not an abstract metaphysical "what is the ultimate." The interrogative is aimed at the student to force a response, per the collocations.

## Thin spots / caveats
- occ. 4 is a Guishan/Yangshan exchange in the *Linji* record's 勘辨 section (two speakers); MasterName left null with AttributionNote naming both. Both added to RelatedMasters.
- occ. 3 (子作麼生) is the same passage cited under 恁麼 — deliberate cross-link between the two deictics, each entry quoting the phrase relevant to it.
- 作麼生 vs 如何/如何是: near-synonymous interrogatives; 如何是 tends toward "what is…?", 作麼生 toward "how / what will you do?". Related, not merged.

## Gate 2 verification (Claude, 2026-07-11)
- **All 4 KWICs re-derived from source and confirmed EXACT CONTIGUOUS after XML-tag stripping.** occ. 4 (後溈山問仰山…意作麼生) is split across `<lb>` (此二尊宿 ends 0503a25 / 意作麼生 continues 0503a26 — grep on the whole string fails; joins contiguously). Verified by reading around each.
- **KWIC fix (1): occ. 2 trimmed** from "師問溈山。併却咽喉唇吻。作麼生道。山曰。却請和尚道。" → "師問溈山。併却咽喉唇吻。作麼生道。" — dropping Guishan's reply (山曰…) so the span is Baizhang's single-speaker question, keeping a clean MasterName=Baizhang Huaihai. 師=百丈懷海 confirmed at chapter head (溈山．五峯．雲巖侍立次。師問溈山).
- **Attribution: no other changes.** occ. 1 Linji (single-speaker sermon) ✓. occ. 3 Shitou (子作麼生 his utterance; 師罔措 narration) ✓. occ. 4 already MasterName=null (Guishan↔Yangshan two-speaker) ✓.
- All RelPaths in zen-corpus.json (no contamination). Validation stays **multi-source** (臨濟語錄 + 五燈會元, multiple masters). RelatedTerms all genuine (恁麼/與麼 sibling deictics; 如何/如何是 synonym interrogatives). FromLb values confirmed nearest-preceding <lb n>.
- **STATUS: verified**

## d001-A depth repair (2026-07-13)

- Re-ran item 8: the questioned object and answer type vary, but 作麼生 remains one interrogative. 作勿生, 作摩生, and 怎麼生 are graphic forms, not senses.
- Preserved four old anchors and added seven verified classes: renewed follow-up, understanding, normative 合 form, ultimate insistence, and three historical graphic strata.
- Final depth: 1 sense, 11 occurrences.
- Family check: 作麼生道 remains the narrower speech-demand entry; bare 作麼生 also asks for an act, understanding, or alternative.
- The #0g deviation is its recurrent public-interview demand for the next accountable word or act; no intent is assigned.

## Gloss-hygiene and family retest

- Item 8: `how?`, `in what way?`, and `what about it?` are English renderings of one interrogative, not different referents. Retained one clean preferred target, `how?`, and moved the other renderings to alternates.
- Family retest: 作勿生, 作摩生, and 怎麼生 remain documented graphic forms of the same interrogative, not senses. 作麼生道 is a narrower compound because 道 supplies the speech demand; 如何 and 如何是 remain related interrogatives rather than additional senses here.

## Attribution remediation — 2026-07-13

- Before: 8 exact-headword occurrences / 3 unlabelled supporting variants; 4 exact sources; required exact floor 10.
- Labelled all three graphic forms `family`. Re-ran the title-and-speaker ladder for every inherited anchor, including the otherwise unlabelled formal outline preserved by compiler Xingyue.
- Added exact witnesses from Dahui Zonggao and Tianran Hanshi. After: 10 exact / 3 support across 6 exact sources.
- Item 8 retest: different questioned objects, constructions, and answer types do not change the interrogative into a different thing. The historical spellings are readings/forms of the same use, not senses.
- Definition retest: the enlarged sample confirms the public-interview gloss; Dahui's pause-and-answer and Tianran's whisk action show that the demanded response may be verbal or enacted.
- Audits: 13/13 KWICs exact with declared lb bounds; attribution hard failures 0; depth/sense hard failures 0. The broad-concordance single-sense flag was reviewed and adjudicated as one sense.

## Speaker-level peer remediation — 2026-07-13

- Split the mixed Guishan/Yangshan KWIC into two exact, independently attributed utterances; neither speaker now inherits the other's words.
- Corrected three governing speakers after expanded-context review: Liao'an Qingyu for the imperial-birthday address, Wufeng Ziqi for the understanding test, and Wanshan Shaoci for the exchange with Zhaojue.
- Replaced the unnamed monk's 怎麼生 question with a directly attributable Yaoshan Weiyan question to Daowu Yuanzhi. Added the natural alternate `what?`.
- Definition/sense retest: all 14 occurrences still ask the same interrogative thing; speaker correction and the enlarged sample strengthen, rather than alter, the public-interview definition. Final depth: 11 exact / 3 support across 6 exact sources.
- Final cohort audit: all KWICs exact with declared lb bounds; attribution hard failures 0; all prose Chinese anchored.

## Semantic remediation r001

- observation: eleven exact anchors and three historical-form controls all ask how, what, in what way, or what the addressee will say, understand, or do next in public exchange.
- minimal-inference: Chan records turn an ordinary interrogative into recurrent interview machinery by making the answer publicly accountable; the questioned object and response medium vary without changing the interrogative.
- ordinary-bridge: “how?” and “what?” are natural English renderings of one colloquial question, not separate definitions.
- falsification-searches: checked questioned objects, speech and action answers, normative 合 forms, ultimate 畢竟 forms, graphic variants, 如何-family interrogatives, and the narrower 作麼生道 construction.
- opening-interpretation-verdict: keep. It already defines the plain interrogative before giving named exchanges and states the observable Chan bend without inventing intent.
- search-recall: approved aliases are how, what, in what way, what about it, and how then.
- rejected-inference: “demand for enlightenment” was rejected as too broad; the anchors demand many kinds of accountable next response.
- nested-quote-ledger: all sixteen Chinese strings in reader prose are anchored by exact or explicitly labelled family occurrences.
- attribution-ledger: all fourteen occurrences have exact named actors and English-first source notes; three form witnesses are labelled family and do not buy depth.
- family-ledger: 作勿生, 作摩生, and 怎麼生 are graphic forms; 作麼生道 is narrower because 道 supplies the speech verb; 如何 and 如何是 remain related interrogatives.
- independent-falsification-verdict: keep one sense. Different constructions and responses are uses of one question, not different things.
- feedback-inference-verdict: KEEP — public accountability is the smallest repeated deployment claim licensed by the stored exchanges.
- feedback-observations: questions elicit speech, understanding, a shout, a whisk blow, silence, and an enacted response across independent texts.
- feedback-falsification-searches: searched ordinary questions, action versus speech frames, formal variants, sibling interrogatives, and counterresponses.
- feedback-counterexamples: simple information-like questions and historical spellings preserve the ordinary interrogative and constrain any stronger technical gloss.
- feedback-scope: one corpus-wide colloquial interrogative with recurrent public-interview deployment; no claim that every question has one intended answer.
- lookup-probes: how; what; in what way; what about it; how then.
