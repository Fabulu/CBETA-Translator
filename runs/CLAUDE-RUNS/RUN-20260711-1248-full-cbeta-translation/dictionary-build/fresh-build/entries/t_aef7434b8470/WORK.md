# WORK — 沒交涉 (t_aef7434b8470)


## Public-feedback reconstruction ledger

- feedback-inference-verdict: PASS — For 沒交涉, the displayed senses are no connection at all; the definitions state only relations observed in the stored turns, contrasts, grammatical frames, and self-descriptions, without promoting an answer or symbolic association into the headword's meaning.
- feedback-observations: 10 exact headword/declared-variant occurrences across 7 source files support 1 different-thing sense(s); actor and source notes remain attached to every evidence row.
- feedback-falsification-searches: Re-tested literal versus Chan-loaded use, word versus title/person, corpus-wide versus master-specific scope, incompatible subject/event frames, and response diversity; only different referents or events justify the 1 retained sense(s).
- feedback-counterexamples: Negative, critical, quoted, narrated, and question-form witnesses were checked against the definition; differences of stance, answer, speaker, or grammar remain visible in evidence rather than being collapsed into an interpretive rule or inflated into polysemy.
- feedback-scope: multi-source; no master-specific sense. Corpus storage counts are concordance context, while the sense claims are limited to the exact witnesses and independent-work spread stored here.
- lookup-probes: utterly beside the point; has nothing to do with it; no bearing whatever; off the mark. These probes cover ordinary English synonyms, word-order variants, and the principal Chan-facing retrieval wording without changing the displayed definition.
- opening-interpretation-verdict: PASS — A reader can identify no connection at all from the PreferredTarget and opening sentence before counts, graph analysis, named examples, or source discussion.
**Gloss target:** "no connection at all / utterly beside the point" (全沒交涉; 且得沒交涉).

## Concordance (Zen allowlist only)
- 沒交涉: 1389 hits / 244 files. Top: X82n1571, X80n1565 五燈會元 (46), T47n1997 圓悟語錄 (44),
  X64n1260 列祖提綱錄 (43), T48n2003 碧巖錄 (42).
- Collocations (grep-verified): 且得沒交涉 37 · 總沒交涉 82 · 了沒交涉 38 · 全沒交涉 21 · 沒交涉處 14 ·
  都沒交涉 6. Rhetorical twin: 有甚麼交涉 114 · 有什麼交涉 114.

## Sense analysis → ONE sense (null, corpus-wide)
沒 (none) + 交涉 (dealings/relevance) = "there is no connection / no bearing." Two deployments of the
one meaning:
1. Flat statement: 石頭 "言語動用沒交涉" / disciple "非言語動用亦沒交涉" (X80 0109b17); 威音已前沒交涉
   (T47n1997 0725a27).
2. Dismissive verdict: 且喜沒交涉 / 且得沒交涉 (miss-verdict), heavy in 碧巖錄/評唱. 廓然無聖。且喜沒交涉
   (T48n2003 0140b19); 料掉沒交涉 (X80 0152b09). Pushed back dialogically: 什麼處沒交涉 (T47n1997 0719c01).
Not two senses — one word, two uses. Stated in Note.

## Multi-source gate → PASS (multi-source)
碧巖錄 (T48n2003), 圓悟語錄 (T47n1997), 五燈會元 (X80n1565), 顓愚衡語錄 (J28nB219) + 240 more. Independent.

## Curated occurrences (all zc.verify ok=True, count=1, main text; ed=X where X-canon)
1. T48n2003 0140b19 (廓然無聖。且喜沒交涉 — 碧巖錄 case 1, Yuanwu)
2. T47n1997 0719c01 (且得沒交涉 / 什麼處沒交涉 — dialogic pushback)
3. X80n1565 0109b17 (石頭 言語動用沒交涉 — flat statement)
4. X80n1565 0114b20 (山曰。三千里外。且喜沒交涉 — 雲巖 exchange)
5. T47n1997 0725a27 (威音已前沒交涉 — flat statement)
6. J28nB219 0663c23 (全沒交涉 — intensified)

## Attribution discipline
Corpus-wide verdict/idiom → MasterName null throughout. RelatedMasters empty (no single owner; the term
is spread across Yuanwu, Shitou, Yunyan, and hundreds of texts).

## Links
RelatedTerms: 且喜沒交涉 (the stock verdict form), 有甚麼交涉 (rhetorical twin), 廓然無聖 (the case where
且喜沒交涉 is most cited).

## Notes / risks
- X80 0109b17: head() mis-picks 京兆興善寺惟寬 header; the exchange is 石頭希遷's — kept two-speaker, null.
- 且喜 vs 且得: both stock; 碧巖錄 favors 且喜, 圓悟語錄 shows both. Noted in Explanation.

## 2026-07-14 fresh-build actor and quotation gate

- Recut the former two-actor Shitou/Yaoshan KWIC into separate exact turns. Shitou Xiqian owns the first statement; Yaoshan Weiyan owns the reply. No occurrence now crosses two headword actors.
- Added the closed-vocabulary `utterer` context link for every named exact actor.
- Anchored every formerly dangling counted form. The two rhetorical twins written with 有 rather than 沒 are ClaimAnchors because they support the prose comparison but are not occurrences of the headword; they buy no depth.
- Current evidence: ten exact headword occurrences plus two claim anchors; all twelve pass `zc.verify` at exact stored bounds and `audit_attribution.py --json` reports zero hard failures.
- Re-tested the definition after the additions: all forms state absence of connection or bearing; dismissive and flat deployments remain readings of one referent, not different things.
