# WORK — 正法眼藏 (t_8ece09f6b91a)

**Batch:** b002 · **Status:** verified (Gate 2)

## Term
正法眼藏 — "treasury of the true dharma eye": the transmitted essence/seeing of the
lineage. Task brief: 世尊拈花…正法眼藏 origin; also Dahui's/Dōgen's title use — kept Zen-to-Zen.

## Concordance (Zen allowlist only)
- Raw allowlist hits: **267 texts / 1,191 occurrences**.
- Top texts: X82n1571 (39), X78n1556 (31), X64n1260 (29), X80n1565, J26nB178, 無門關,
  景德傳燈錄, 臨濟語錄 …
- Multi-source; single corpus-wide sense.

## Sense analysis
One sense (SenseKey = null): literally the storehouse (藏) of the eye (眼) of the true
Dharma (正法) — the correct seeing the lineage transmits. Three usage registers, all the
same sense: (a) the Buddha's flower-transmission formula (吾有正法眼藏…付囑摩訶迦葉);
(b) patriarch-to-patriarch handover (付囑於汝，勿令斷絕); (c) a master's OWN transmitted
teaching (Linji's 吾正法眼藏). Masters interrogate it rather than mystify it (Wumen).

## Multi-source gate → PASS (multi-source)
5 curated occurrences / 4 texts:
1. Śākyamuni (World-Honored One) — 五燈會元 X80n1565 0031a08 (origin, 釋迦牟尼佛 section).
2. Wumen Huikai — 無門關 T48n2005 0293c19 (無門曰, case 6; problematizes transmission — verified 無門曰 precedes).
3. (Indian patriarch) — 景德傳燈錄 T51n2076 0208c21 (8th patriarch Buddhanandi → Buddhamitra; MasterName null, precise note).
4. Linji Yixuan — 臨濟語錄 T47n1985 0506c03 (師 = Linji, deathbed; his own 正法眼藏).
5. Baiyun Shouduan — 五燈會元 X80n1565 0389c05 (section head 舒州白雲守端禪師; 開堂示眾, living-lineage use).

## KWIC verification
All 5 KWICs re-checked as EXACT contiguous substrings of the normalized source (incl.
fullwidth 「」／？ preserved verbatim in the Linji passage); each contains 正法眼藏; all
RelPaths on allowlist; all FromLb nearest-preceding `<lb>` present in file.

## Notes / risks
- Kept strictly Zen-to-Zen: Dahui's 正法眼藏 collection (X67n1309, NOT on allowlist) and
  Dōgen's Shōbōgenzō (Japanese, not in corpus) are mentioned only as reception in the
  Note, never cited as evidence.
- Jingde handover: transmitter identified as Buddhanandi (8th patriarch) → Buddhamitra
  (9th) by reading the section head; MasterName left null (Indian ancestor, not in index).
- Deflationary rendering "treasury of the true dharma eye"; avoided "Absolute/ultimate
  reality" abstraction — the corpus itself questions whether it is even transmissible.

## Gate 2 — independent adversarial verify (Claude, Opus)
Re-derived from source by targeted grep of each cited file. VERDICT: **verified**, 0 repairs.
- **KWIC (5/5 exact-contiguous verbatim after tag-strip, incl. fullwidth ：「」？，):**
  1. X80n1565 0031a08 `世尊曰。吾有正法眼藏。涅槃妙心。實相無相。微妙法門。不立文字。教外別傳。付囑摩訶迦葉。` — lines 2520–2522 (lb splits), contiguous.
  2. T48n2005 0293c19 `正法眼藏作麼生傳。設使迦葉不笑。正法眼藏又作麼生傳。若道正法眼藏有傳授。黃面老子誑謼閭閻。` — lines 293–295, Wumenguan case 6.
  3. T51n2076 0208c21 `我今以如來正法眼藏付囑於汝勿令斷絕。` — lines 1814–1815, Buddhanandi→Buddhamitra.
  4. T47n1985 0506c03 Linji deathbed span `師臨遷化時據坐云：「吾滅後…向這瞎驢邊滅却。」言訖端然示寂。` — lines 1151–1155, verbatim incl. punctuation.
  5. X80n1565 0389c05 `開堂示眾云。昔日靈山會上。世尊拈華。迦葉微笑。世尊道。吾有正法眼藏。分付摩訶大迦葉。次第流傳。無令斷絕。至于今日。` — lines 29429–29431, contiguous.
- **Contamination:** 0. All RelPaths on allowlist. **Dahui's off-allowlist 正法眼藏 collection and Dōgen's Shōbōgenzō are NOT cited as occurrences — only noted as reception in the entry Note. Confirmed no occurrence points to either.**
- **Attribution (all confirmed):** #1 世尊 = Śākyamuni (釋迦牟尼佛 origin section). #2 無門曰 (line 291) = Wumen. #3 MasterName null — Indian patriarch handover (Buddhanandi→Buddhamitra), correct. #4 師=Linji — T47n1985 = 鎮州臨濟慧照禪師語錄, 師諱義玄 (line 1156); his own 正法眼藏. #5 師=Baiyun Shouduan — section head 舒州白雲守端禪師 (line 29368, lb 0388c19).
- **Multi-source:** holds (4 texts, distinct speakers). **FromLb:** all = nearest preceding `<lb n>`. **RelatedTerms** 拈花/涅槃妙心/教外別傳 = genuine constituents of the transmission formula.

## #0f.8 sense repair (2026-07-13)

- Reopened the entry against the work-title family. The allowlisted X72n1444 heading `重刻正法眼藏序` ('Preface to the Recut *Treasury of the True Eye of the Teaching*') and its prose `裒以成帙，目曰正法眼藏` ('gathered it into fascicles and titled it *Treasury of the True Eye of the Teaching*') prove a recut, prefaced book distinct from the transmitted treasury.
- Split transmitted treasury from book title. The transmitted sense retains all five existing witnesses. The title sense is provisional because its two defining anchors come from one independent allowlisted work.
- Cross-check result: the inherited expression remains valid in the primary sense; not every occurrence within the title preface was reassigned, because that preface also discusses the inherited expression.
- Post-repair verification: 7/7 occurrences exact and line-synchronized with `zc.verify`.

## Gloss-hygiene hard gate

- `sense-target-distinguishability:` KEEP sense 0 “lineage-transmission phrase: the treasury of the true eye of the teaching” versus sense 1 “book title: Treasury of the True Eye of the Teaching.” The targets alone identify an inherited lineage expression versus a recut, fascicled, prefaced book.
- Capitalization alone was removed as the distinction. Exact phrase anchors remain under the lineage referent; the heading and “titled it” anchors remain under the book referent.
- Family/definition retest: the book title repeats and discusses the inherited phrase, but the preface's physical-book predicates prove the second referent. No occurrence was moved merely for capitalization.
## Public-feedback inference ledger

- feedback-inference-verdict: `accepted-with-limits` — the exact graphs refer both to a transmitted lineage possession and to Dahui Zonggao's compiled book; the split follows different referents, not capitalization or noun grammar.
- feedback-observations: the Buddha, Buddhanandi, Linji Yixuan, Wumen Huikai, and Baiyun Shouduan deploy the phrase in transmission claims and questions; Zhanran Yuancheng explicitly calls the separately recut item a book assembled into fascicles and titled `正法眼藏`.
- feedback-falsification-searches: tested phrase-as-possession against title headings, recut/preface language, fascicle language, and the explicit expression `是書`; tested whether the two displayed targets remained distinguishable without capitalization.
- feedback-counterexamples: occurrences inside a book preface may discuss the inherited phrase rather than the title. Title context, not mere location inside the preface, controls assignment.
- feedback-scope: phrase sense is corpus-wide; title sense is provisional and tied to the independently identified case collection.
- lookup-probes: `true Dharma eye`, `true teaching eye`, `treasury`, `storehouse`, `lineage teaching treasury`, `Treasury of the True Dharma Eye`, `Dahui case collection`, `Zhengfa Yanzang`.
- opening-interpretation-verdict: `pass` — the first opening says what the transmitted phrase does in the record; the second immediately identifies a separately attested book.

## Exact-turn attribution correction (2026-07-13)

- Split the Linji deathbed span into Linji's instruction, Sansheng Huiran's reply, and Linji's final verdict, each with its actual speaker.
- In Baiyun Shouduan's sermon the headword-bearing `世尊道` quotation is credited to the Buddha; the note names Baiyun as reteller and preserves his succession comment.
- The phrase sense now has seven exact-turn occurrences; both title-sense anchors are unchanged.
