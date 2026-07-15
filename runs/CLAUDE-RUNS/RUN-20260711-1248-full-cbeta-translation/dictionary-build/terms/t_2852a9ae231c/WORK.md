# WORK — 隨波逐浪 (t_2852a9ae231c)

## Concordance (allowlist-only, zen-corpus.json)
- Raw hits: **310 occurrences in 127 allowlist files** (Zen-scoped). Dominated by X-canon lamp records
  (五燈嚴統 X81n1568, 五燈全書 X81/X82n1571, 五燈會元續略 X80n1566, 續燈正統 X84n1583, 禪宗正脉 X85n1593…).

## Sense analysis — TWO senses
1. **Technical: 隨波逐浪句** — the third of the three phrases (三句). Origin passage (X81n1568, mulu
   鼎州德山緣密圓明禪師): `上堂：我有三句語示汝諸人：一句函葢乾坤，一句截斷眾流，一句隨波逐浪。作麼生辯？`
   - Handled as a fixed set 函蓋乾坤 · 截斷眾流 · 隨波逐浪; recurs as a stock **test-question**
     `如何是隨波逐浪句？` with turning-phrase answers (師曰：隨 / 闊 / 春生夏長 / 船子下揚州 / 李白捉月，張騫乘槎).
   - Paired against 截斷眾流: raised 趙州 case verdict `一人隨波逐浪，一人截斷眾流` (X80n1566, X81n1568,
     X82n1571, X83n1574, X84n1583, X84n1585 — very widespread).
2. **Idiom: drift with the waves** — plain, often pejorative: `見色聞聲，隨波逐浪，流轉…` (X85n1593);
   `終日隨波逐浪、妄生枝節者哉` (X84n1583); autobiographical `自少隨波逐浪` (X82n1571).
   - In-corpus equating gloss: `看風使帆，正是隨波逐浪` (X81n1568) — trimming the sail to the wind = 隨波逐浪.

## Attribution evidence
- Three-phrase formulation: **德山緣密 (Deshan Yuanming)**, roster-confirmed (德山緣密, fl.850 d.920),
  Yunmen's heir — his own 上堂. NOT Yunmen personally; do not over-attribute (task framed it as
  "Yunmen's phrase" but the corpus states it in 緣密's section). No literal `雲門三句` string in allowlist.
- Test-question answers (瑞巖智才, etc.) and the idiom occurrences (法秀, 慶善能, 崧溪子定) fall in
  off-roster later sections → MasterName null.
- Zhaozhou pairing = raised case + commenting master (off-roster) → null.

## Self-definition found
- No `隨波逐浪者…也` / `謂之隨波逐浪` self-definition exists (grepped, 0 hits). Closest is the equation
  `看風使帆，正是隨波逐浪` (idiom sense). The technical sense is defined by its **set membership + deployment**.

## Validation
- Sense 1: **multi-source** (德山緣密 formulation + test-question deployed across ≥2 independent texts).
- Sense 2: **multi-source** (idiom across 五燈嚴統 / 續燈正統 / 禪宗正脉).

## KWIC verbatim check
All 6 curated KWICs confirmed exact contiguous substrings of their files (tag-stripped, single source line).
X-canon lb uses ed="X".

## GATE 2 (verify-and-repair, 2026-07-12)
Independent re-derivation. Results:
- **KWICs:** all 6 drafted KWICs re-verified EXACT, unique (count=1), lb ed="X" MATCH, cb:mulu head MATCH.
  1 quote correction: sense-1 Explanation quoted the 上堂 as `示汝諸人，一句…` — the source (X81n1568
  0113c18–19) has a COLON: `我有三句語示汝諸人：一句函葢乾坤`. Fixed. Also reduced the test-question answers
  to the bare attested words (隨；闊；春生夏長；船子下揚州) — the drafted `師曰：闊` matched no file exactly
  (sources have 師云：闊 / 師曰闊). All four answers re-verified as answers to 如何是隨波逐浪句 in ≥2 allowlist
  files each (闊: X78n1556, T51n2077 — the J29nB223 闊 answers 家風, NOT this question; 春生夏長: X68n1319,
  X78n1553, X80n1565; 船子下揚州: X78n1556, X80n1565, X82n1571).
- **Contamination:** 0 — all RelPaths + SourceTexts in zen-corpus.json (462).
- **Attribution:** 0 name fixes. 德山緣密 roster-confirmed (roster's own romanization "Deshan Yuanming";
  the mulu head's 圓明 is his 賜號). X81n1568 places his section under 雲門偃禪師法嗣 — "heir of 雲門文偃"
  verified. All null attributions re-checked at the mulu heads (瑞巖智才 / 育王孤雲權 / 法秀圓通 / 慶善能 /
  崧溪子定 / 介石芳 — none on roster).
- **SENSE-ASSIGNMENT FIX (the main repair):** the 看風使帆，正是隨波逐浪 occurrence (X81n1568 0140c07) was
  filed under sense 2 as an "equating gloss of the idiom" — but its continuation is 截斷眾流，未免依前滲漏:
  the 上堂 deploys the NAMED PHRASE-PAIR, not the plain idiom. Moved to sense 1; the "in-corpus gloss" claim
  deleted. Sense 2 backfilled with the autobiographical witness X82n1571 lb ed=X 0641a09
  `明芳愚懦無知，自少隨波逐浪。` (mulu 瑞安瑞雲介石芳禪師, off-roster → null) so it stays 3-witness /
  multi-source (X85n1593, X84n1583, X82n1571).
- **Describe-only repairs:** deleted "to be carried by circumstances", "frequently spoken pejoratively, of
  one swept along by sense-objects", and Note's "Often carries a negative charge (drifting in delusion)" —
  annotator force-claims. Sense 2 now quotes the attested lines (…見色聞聲，隨波逐浪，流轉三界; 終日隨波逐浪、
  妄生枝節者哉; 自少隨波逐浪) and stops. Dropped AlternateTarget "going along with conditions" (imports 隨緣).
  Sense-2 RelatedTerms 截斷眾流 dropped (that pairing is sense-1 material).
- **Multi-source:** 緣密's 上堂 found in SIX allowlist texts, all his section (X64n1260 圅葢, X68n1319,
  X80n1565, X81n1568, X81n1571, X85n1593 函蓋) — added to Note + SourceTexts.
- **Counts refresh** (tag+note-stripped method): 隨波逐浪 = 360 in 136 allowlist files; 隨波逐浪句 = 145 in 71.
- Verdict: **verified** (7 occurrences total: sense 1 = 4, sense 2 = 3, all re-checked).

## Gloss-hygiene hard gate

- sense-target-distinguishability: sense 0 `fixed phrase: following the waves, chasing the swells` names the recognized 隨波逐浪句 in the three-phrase complex; sense 1 `plain idiom: drift with the waves` names the phrase used as an ordinary predicate. This is a named textual object versus an idiomatic action, not a split based only on noun/verb grammar.

## Semantic r003 remediation

- inherited-occurrence-ledger: KEEP all seven exact witnesses; REVISE every reader-facing explanation, note, and attribution note for English-first description and exact source naming. No inherited witness changed sense.
- ordinary-scene: successive waves and swells determine the visible movement; to follow them is to keep moving with each new rise rather than maintain an independently described course. Yuantong Faxiu's sail-trimming equation is the corpus's closest explicit physical comparison.
- nested-compound-audit: the longer question form 'phrase of following the waves' independently marks the technical textual object; shorter overlaps 'follow the waves' and 'chase the swells' also occur outside the full idiom and were not used to invent extra meanings. The nearby idiom 'follow the waves and drift with the current' is a distinct longer lexical object.
- sense-target-distinguishability: `the “follow the waves” phrase` is a named member of Deshan Yuanming's three-part set; `to follow wave after wave` is an action predicated of people. A textual phrase and the action pictured by its wording are different things, not a noun/verb split of one event.
- opening-interpretation-verdict: LICENSED. Set membership, recurring question grammar, and pairing with cutting off the streams establish the named phrase. Successive-water motion plus the three independently attributed personal predicates establish the ordinary action. No universal valuation is inferred.
- observation: Deshan Yuanming lists the expression as one of three phrases; an unnamed monk asks Ruiyan Zhicai what that phrase is; Guyun Quan and Yuantong Faxiu pair it with cutting off the streams. Qingshan Neng, Songxi Ziding, and Shiyu Mingfang predicate the action of beings, criticized people, and themselves.
- minimal-inference: the same words denote both a recognized transmitted phrase and an ordinary wave-following action.
- ordinary-bridge: a phrase can be named by quoting its wording, while the same wording can independently describe an action.
- falsification-searches: searched the exact headword, both two-graph overlaps, the longer question form, the three-phrase family, wave/current variants, sail and boat comparisons, and the contrast with cutting off streams.
- counterexamples: Yuantong Faxiu's sail line immediately continues with the paired technical phrase and therefore stays under the named-phrase sense; critical ordinary uses do not prove that every occurrence is condemnatory.
- scope: corpus-wide two-sense split; the named sense belongs to the transmitted three-phrase family, while the ordinary predicate has independent multi-source use.
- verdict: LICENSED.

- feedback-inference-verdict: REVISE — the old entry was graph-first, Chinese-heavy, and withheld the physical scene. The revision defines the moving-water constraint, names the technical set, and preserves the action/title boundary.
- feedback-observations: three-part enumeration; repeated phrase-question; paired-stream contrast; sail-trimming equation; three independent personal predicates.
- feedback-falsification-searches: exact, overlap, compound, contrast, question, physical-scene, and near-idiom searches were completed.
- feedback-counterexamples: the records criticize several ordinary uses but do not license a universal negative value; the sail comparison belongs to the technical pair in its local context.
- feedback-scope: corpus-wide, with Deshan-family restriction on the named phrase.
- lookup-probes: follow the waves phrase / Deshan's three phrases / three phrases of Deshan / follow the waves / drift with the waves / go with the waves / ride the swells / carried by the current.
- post-remediation-prose-followup: Replaced generic “later records” and “Chan records” summaries with exact actors. The named-phrase prose now distinguishes Deshan Yuanming, a reviewed unnamed questioner with Ruiyan Zhicai as respondent, Guyun Quan, Xuefeng Yicun, Zhaozhou Congshen, and Yuantong Faxiu; the ordinary predicate names Qingshan Neng, Songxi Ziding, and Shiyu Mingfang. The two-sense structure and evidence remain unchanged.
