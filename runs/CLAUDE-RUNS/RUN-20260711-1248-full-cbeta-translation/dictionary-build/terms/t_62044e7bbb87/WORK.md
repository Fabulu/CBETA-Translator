# WORK — 本分事 (t_62044e7bbb87)

**Gloss target:** "the matter of one's own lot / the fundamental affair"
**Batch:** b003

## Concordance (Zen allowlist only)
- ~955 occurrences across ~80 allowlist texts and dozens of masters. One of the most
  frequent stock terms in the record. Zen-scoped via `zen-corpus.json` (462 paths).
- Search helper: scratchpad/search.py (allowlist-filtered; nearest ed-matching `<lb n>`).

## Sense analysis
- ONE corpus-wide sense (SenseKey null). No distinct master-specific bending survives
  scrutiny — it is a shared stock term. Zhaozhou's 以本分事接人 is a famous *method*
  using the term, not a different meaning; kept the sense unified.
- Literal: 本分 = one's own portion/allotment/lot; 事 = matter. → "the matter that is
  one's own basic lot," set against 分外 ("extra / outside one's lot").
- Deflationary axes captured: (a) not borrowed from outside (不從千聖借豈向萬機求);
  (b) what a real teacher works with, not doctrine (以本分事接人); (c) it's simply
  yours, even pointing suffices (是你本分事); (d) levels to the ordinary (喫粥喫飯是汝本分事);
  (e) anti-reification pairing with 分外 (你纔說本分事早是分外了).
- Corpus self-gloss: 本分事者即當人本命元辰之落處 (J36nB357).
- Mundane-sense caveat noted (本分 = "proper duty," e.g. 比丘持戒是本分事) — flagged in Note.

## Multi-source gate → PASS (multi-source)
Trivially satisfied: dozens of independent texts/masters. Curated 5 across B14/J24/B25/J28/J38.

## Curated occurrences (all KWIC = exact contiguous, tag-free, line-bounded; verified by grep)
1. B14n0082 0246b07 — 師曰不從千聖借豈向萬機求 (Shanglan Lingchao)
2. J24nB137 0358b16 — 若是宗師，須以本分事接人始得 (Zhaozhou; 以本分事接人 locus)
3. B25n0144 0631b08 — 指學人云：「是你本分事。」 (null; two-speaker)
4. J28nB202 0078c06 — 喫粥喫飯是汝本分事 (null; ordinary-activity leveling)
5. J38nB409 0293c01 — 你纔說本分事，早是分外了 (null; 本分/分外 axis)

## Links
- RelatedMasters: 趙州從諗, 南泉普願
- RelatedTerms: 分外事, 向上事, 本命元辰, 己事

## Notes / risks
- KWICs #1/#2 span a following speaker on the same line but are cut at safe tag-free points;
  #3/#4/#5 chosen to sit within one `<lb>` line (many full sentences cross lb boundaries).
- MasterName left null for stock monk-master Q&A where section head not individually verified.

---

## GATE 2 (Claude adversarial verify+repair) — VERIFIED
- KWICs: all 5 exact-contiguous (count=1 each), tag-stripped substring match confirmed by script. Zero ellipses.
- Allowlist: all RelPaths (B14n0082, J24nB137, B25n0144, J28nB202, J38nB409, C077n1710) in zen-corpus.json. Prose-cited J36nB357, J33nB294 also in allowlist. No contamination.
- FromLb: all 5 match nearest preceding <lb n>. OK.
- Attribution: #1 上藍令超 CONFIRMED (section head 洪州上藍令超禪師; 僧問如何是上藍本分事 / 師曰…). #2 趙州從諗 CONFIRMED (師上堂謂眾曰…, Zhaozhou's own hall-talk; also 師示眾云老僧此間即以本分事接人…自有三乘十二分教接佗). #3/#4/#5 null (correct).
- Explanation quotes all grep-verified: 不從千聖借豈向萬機求, 以本分事接人 (×3), 喫粥喫飯是汝本分事, 你纔說本分事早是分外了, 三乘十二分 collocation (Zhaozhou 示眾, accurate).
- REPAIR: Note self-gloss quote corrected to exact source form 本分事者，即當人本命元辰之落處也 (was missing internal comma + trailing 也). Claim verified true (J36nB357).
- Multi-source: trivially holds (dozens of texts). Senses: single corpus-wide sense correct.
- Verdict: VERIFIED.

## S001 sense repair

- Reopened item-8 test against the full definition and family. Split the Chan technical “fundamental matter” from ordinary role-duty.
- Added the corpus self-definition, the early unanswered public question, and the precept-duty witness. Final depth: 8 occurrences, 2 senses; ordinary duty remains provisional at one source.
- All anchors re-verified exactly with `zc.py`; source scope and anchors synchronized.

## L002-A item-8 ledger and full retest (2026-07-13)

- sense-target-distinguishability: KEEP “one's own fundamental matter” versus “one's proper duty.” Pair 1–2 names different referents: the stock matter asked about, pointed back to the questioner, and contrasted with “extra,” versus an assigned institutional responsibility such as a bhikshu's duty to keep the precepts.
- Family retest: “outside one's lot” (分外), “higher matter” (向上事), “own root-destiny” (本命元辰), “one's own affair” (己事), “keep the precepts” (持戒), and the wider “own-lot” compounds remain compatible with the two referents. The precept-duty passage is not used to redefine the public-interview term, and the Chan passages are not used to erase ordinary assigned duty.
- Definition retest: Zhaozhou's “receive people with the own-lot matter,” Shanglan's answer, the direct root-destiny definition, the early unanswered question, and the own-lot/extra contrast all retain the primary sense. The institutional admonition alone anchors the distinct role-duty sense, which remains provisional rather than overgeneralized.
- #0g retest: Chan bends an ordinary allotment expression into a frequent public question, direct self-definition, pointed answer, and named reception formula. The entry reports those forms without converting them into a technique or an interpretation of what a master intended.
- Depth decision: eight anchors across seven source texts meet the 837-hit floor; each prose claim used to distinguish the referents has an exact witness, and no duplicate was added.

## 2026-07-13 full remediation
- Rebuilt to 9 total / 8 exact witnesses across 7 exact sources at 837 hits / 184 files; Shanglan's answer is family evidence because its stored line lacks the question.
- Retained the item-8 split between the public-interview fundamental matter and an assigned role-duty. The duty witness explicitly preserves precept keeping as a hard rule.
- Added Yuanwu's exact deployment and completed speaker/title attribution. All KWICs/bounds and both audits pass.

## semantic-r001 public-feedback remediation (2026-07-14)

- feedback-inference-verdict: KEEP the matter-versus-duty split. A public-interview concern pointed back to the person and contrasted with “extra” is a different thing from an obligation assigned to a role, such as a bhikshu's duty to keep precepts.
- feedback-observations: Zhaozhou says a lineage teacher receives people with the fundamental matter and points it back as the questioner's own. Baichi includes eating, bodily functions, and responsive activity yet still demands the phrase; Danxia says speaking of it has already made it extra; Xiuyelin directly defines it as the resting place of one's root-destiny. Yunxi alone supplies the distinct institutional duty predicate.
- feedback-falsification-searches: Rechecked direct what-is questions, receive-people construction, pointing to “your own,” gruel-and-rice, own-lot versus extra, root-destiny definition, unanswered question, clear-yet-not-let-pass, precept keeping, bhikshu, proper duty, outside one's lot, higher matter, and one's own affair. Incompatible public-question and institutional-obligation predicates sustain the split.
- feedback-counterexamples: The precept witness prevents collapsing every occurrence into a Chan technical concern and preserves the precepts carve-out as a hard rule. Its single-source status prevents overgeneralizing the duty sense. Shanglan's headword-less answer remains family evidence and cannot buy exact depth.
- feedback-scope: The fundamental-matter sense is corpus-wide and multi-source. The role-duty sense remains provisional and directly attributable at one source.
- lookup-probes: Primary probes covered “fundamental matter,” “one's own affair,” “one's real concern,” “matter of one's own lot,” and “original matter.” Duty probes covered “proper duty,” “assigned duty,” “role obligation,” and “monastic duty.”
- opening-interpretation-verdict: KEEP both corpus-earned openings. The primary begins with the matter belonging to one's own lot and immediately shows public questioning; the duty sense begins with an institutional obligation and its precept witness.
- definition-formula-audit: The direct root-destiny formula, pointed answer, reception formula, own/extra contrast, and public questions define the first referent. “Keeping precepts is a bhikshu's proper duty” directly anchors the second.
- nested-family-audit: Outside one's lot, higher matter, root-destiny, one's own affair, precept keeping, and wider own-lot compounds remain linked without collapsing the referents. One family witness stays excluded from exact-headword depth.
- modifier-and-provenance-audit: No feedback modifier is at issue. All nine anchors were re-read; eight exact-headword and one family witness retain exact source-and-speaker attribution.
- semantic-propagation: Preserve fundamental concern versus assigned obligation in outside one's lot, higher matter, root-destiny, own affair, and precept entries. Search must distinguish “my fundamental matter” from “my proper duty.”
- final-cohort-gate: `run_cohort_gate.py` hardPass=true; exact KWIC 9/9, attribution hard failures 0, public-feedback flags 0, depth/sense hard failures 0, review flags 0, and forbidden-English matches 0. Report: `maintenance/semantic-cohorts/semantic-r001-owner1-benfenshi-gate.json`.
