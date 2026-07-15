# WORK — 無位真人 (t_0ed8638229a9) · batch b003

**Gloss target:** Linji's "true person of no rank."

## Method
- Zen-scoped concordance (allowlist filter). **Corpus-wide count: 1077 occurrences across 241 texts.** Ubiquitous — a stock figure — but originating from a single coinage.
- Read the locus classicus in the 臨濟錄 and traced the reception.

## Sense analysis
One sense (SenseKey=null, corpus-wide currency). Literal: 位 = rank/office/status; 無位真人 = "a true person of no rank." Linji's most famous 上堂: on the 赤肉團 (lump of red flesh = ordinary body) there is one 無位真人 「常從汝等諸人面門出入」 — always going in and out through the face. Linji's next retort is preserved cautiously as a question and answer: “What is the true person of no rank? A dried shit-stick.” The phrase originates with Linji and then circulates corpus-wide: some later witnesses credit him explicitly, while others receive it as a standing question or redeploy it without repeating the credit. These are different readings and deployments of the same stock figure, not different lexical things.

## Multi-source gate → PASS (multi-source)
| RelPath | Text | Lb | Role |
|---|---|---|---|
| T47n1985 | 臨濟錄 | 0496c10 | 「赤肉團上有一無位真人」 — the coinage (上堂) |
| T47n1985 | 臨濟錄 | 0496c13 | 「無位真人是什麼乾屎橛」 — Linji deflates it |
| C077n1710 | 古尊宿語錄 (Linji juan) | 0640b08 (ed=C) | 「上堂云赤肉團上有一無位真人常」 — independent transmission |
| X64n1260 | (lamp/yulu collection) | 0080b19 | 「西臺辯禪師上堂，舉臨濟無位真人語」 — later reception, credits Linji |
| X64n1260 | " | 0096c22 | 「無位真人突出難辨。頂門上時時顯現，眼睛裏處處」 — gloss echoing 面門出入 |

## Speaker confirmation (verified at nearest chapter/section head)
- T47n1985 0496c10 / 0496c13: 「上堂云」 … 師下禪床/師托開 — the enclosing 師 is Linji throughout the 臨濟錄 → **MasterName=Linji Yixuan**. (The monk's line 「如何是無位真人」 is a questioner, not curated.)
- C077n1710 0640b08: nearest 卷 mulu = 「臨濟慧照禪師諱義玄曹州南華邢氏子…」 → confirmed Linji's section → **Linji Yixuan**. (A second locus at 0762b11 sits under 廣智全悟/笑隱大訴 — a Yuan master quoting it — NOT used.)
- X64n1260 0080b19: line names 「西臺辯禪師上堂，舉臨濟…語」 — act is 西臺辯's, phrase credited to 臨濟 → **MasterName=null** (Xitai Bian not canonicalizable), AttributionNote records it.
- X64n1260 0096c22: anonymous gloss → **null**, curated=false.

## Byte-for-byte KWIC verification
All five KWICs confirmed in tag-stripped source. Single-line; the fuller 「常從汝等諸人面門出入」 continuation was deliberately NOT stitched into the KWIC (it crosses the 0496c10→c11 line break).

## Links
RelatedMasters: Linji Yixuan. RelatedTerms: 赤肉團, 乾屎橛, 面門 (all co-occurring in the locus).

## GATE 2 (Claude adversarial verify+repair)
- All 5 KWICs re-grepped: EXACT contiguous verbatim after tag-strip.
  - T47n1985 0496c10 「赤肉團上有一無位真人」 ✓ (line 278, under 上堂云)
  - T47n1985 0496c13 「無位真人是什麼乾屎橛」 ✓ (line 281, 師托開云 = Linji)
  - C077n1710 0640b08 「上堂云赤肉團上有一無位真人常」 ✓ (line 6718)
  - X64n1260 0080b19 「西臺辯禪師上堂，舉臨濟無位真人語」 ✓ (line 8141, ed="X" correct — NOT the co-located R112)
  - X64n1260 0096c22 「無位真人突出難辨。頂門上時時顯現，眼睛裏處處」 ✓ (line 9365, ed="X" correct)
- Contamination: 0. All RelPaths in allowlist.
- Attribution re-verified at section heads:
  - T47n1985: whole 臨濟錄; 上堂云 + 師 throughout = Linji ✓
  - C077n1710 0640b08: nearest juan head 臨濟慧照禪師語錄一 / 臨濟慧照禪師諱義玄 (line 6688-89) = Linji ✓
  - X64n1260 0080b19: act is 西臺辯's, phrase credited 臨濟 → MasterName=null ✓; 0096c22 anonymous gloss null ✓
- Explanation quotes all grep-verified verbatim (常從汝等諸人面門出入 contiguous across 0496c10→c11 line break; 如何是無位真人 ✓; 舉臨濟無位真人語 ✓). No punctuation alterations found. NO repairs needed.
- Multi-source: attested across 3 independent texts (locus + 古尊宿語錄 transmission + later reception). Note honestly flags single-coinage (Linji). Holds.

STATUS: verified

## Attribution and depth remediation (2026-07-13)

- Before/after: 5 → 10 exact-headword occurrences across 3 → 8 source texts; 3 named → 10 named; partial notes → 10 exact-title, exact-speaker notes.
- The Linji locus was expanded to include the challenge, seizure, demand to speak, rejection, shit-stick question, and return to the abbot's quarters. Later witnesses add explicit questions, answers, quotation, rank/no-rank comparison, and reception without mistaking reception for a new coinage.
- Ladder: exact KWIC-centered context identifies Linji Yixuan, Xitai Bian, Gulin Qingmao, Lingji Xingguan, Shanfeng Xian, the Yongzheng Emperor, Caoyuan Sheng, and Miyun Yuanwu. Exact context corrected repeated-line/header ambiguity in the winter mini-talk and Caoyuan record.
- Roster exceptions retained and disclosed: Xitai Bian, Gulin Qingmao, Lingji Xingguan, Shanfeng Xian, the Yongzheng Emperor, and Caoyuan Sheng are source-attested names absent from the roster. Linji Yixuan and Miyun Yuanwu use roster values.
- Quote anchoring: prose-only Chinese labels that were citations rather than evidence were translated or removed; the erroneous transcription `說似一物即不中` was corrected to the source's `喚作一物即不中`. Every remaining Chinese string is anchored in a stored KWIC; no unfindable quotation remains.
- Definition/item-8 retest: later questioning, glossing, and quotation continue to invoke Linji's same stock figure. They do not establish a second referent or a genuinely master-private later sense.

## C2 semantic peer repair (2026-07-13)

- Removed the unsupported claim that every later witness explicitly frames the phrase as Linji's saying and that its reading remains uniformly Linji's. The entry now distinguishes Linji's source-attested coinage from later circulation and redeployment without inventing a second referent.
- Replaced the imported “sense-gates” gloss of `面門` with literal “face” and punctuated `無位真人是什麼乾屎橛` cautiously as question plus answer.
- Marked the short Linji excerpt as `EvidenceRole=contrast`: it is a same-passage duplicate of the full locus and cannot buy independent exact-evidence depth. Marked the Gulin Qingmao reception witness curated.
- Item-8 retest still supports one sense: the later questions and glosses concern the same Linji-coined stock figure, though they do not impose one uniform interpretation.

## Semantic remediation r001

- observation: Linji coins the figure in the red-flesh and dried-shit-stick exchange; later exact witnesses raise, question, compare, answer, and gloss that named figure under differing attributions.
- minimal-inference: the phrase is one Linji-coined stock figure in corpus-wide circulation, while later readings remain local and sometimes conflicting.
- ordinary-bridge: “rank” evokes graded office or position, and “person of no rank” gives the reader the figure's explicit contrast without claiming an invisible biography.
- falsification-searches: checked Linji locus and duplicate transmission, standing questions, explicit Linji credits, signature-answer lists, true-person-with-rank counterterm, later glosses, calling-it-a-thing challenge, and attribution of quotations.
- opening-interpretation-verdict: keep after revision. It now identifies the stock figure and its circulation before named evidence, without beginning from graph morphology.
- search-recall: approved aliases are true person of no rank, rankless true person, person of no rank, and no-rank true person.
- rejected-inference: later glosses such as “greed” were not universalized; the figure was not turned into a hidden metaphysical person or split by each later reading.
- nested-quote-ledger: reader-facing prose was simplified to anchored English renderings; all remaining evidence claims map to the ten stored KWICs.
- attribution-ledger: all ten occurrences name exact source voices; later raisers and glossers keep responsibility for their words rather than transferring them to Linji.
- family-ledger: red flesh, face, dried shit-stick, true person with rank, and calling it a thing were checked as locus or contrast families without creating extra senses.
- independent-falsification-verdict: keep one sense. Stock question, local answer, comparison, and discursive gloss continue to invoke one figure.
- feedback-inference-verdict: KEEP — corpus-wide circulation is licensed; uniform interpretation is expressly rejected.
- feedback-observations: coinage, deflation, duplicate transmission, named raising, standing question, signature-answer comparison, local gloss, and later challenge are anchored.
- feedback-falsification-searches: searched origin attribution, later uncredited use, conflicting gloss, counterterm, nested locus phrases, and second-referent evidence.
- feedback-counterexamples: Linji's own dried-shit-stick retort and later incompatible answers prevent a single elevated explanation while preserving the named figure.
- feedback-scope: one corpus-wide Linji-coined stock figure with speaker-scoped later readings.
- lookup-probes: true person of no rank; rankless true person; person of no rank; no-rank true person.
- plain-english-image-verdict: PASS — the opening no longer begins with “literally” and tells the reader how the phrase functions in the records.
