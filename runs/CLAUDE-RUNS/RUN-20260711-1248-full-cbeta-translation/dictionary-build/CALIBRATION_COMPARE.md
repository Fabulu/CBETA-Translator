# Calibration comparison: 話頭

## Bottom line

The two entries are materially different. The Codex entry is better as a mechanically reliable, current-`zc.py`, merge-ready entry: its ten quotations all contain the headword, all return `zc.verify(...).ok == true`, all line ranges exactly match the verifier, its stated counts reproduce, and its multi-master sense is corpus-wide rather than incorrectly keyed to one master. The reference entry is better in several lexicographic respects: it explains 頭 (“head”) as a noun suffix, gives a stronger early appraisal example from Dongshan Liangjie, and harvests more self-definitions, a historical contrast, and the rare “saying-tail” and “saying-waist” forms.

Overall verdict: **Codex is modestly better on correctness, schema semantics, and the new #0b/#0c rules; the reference is better on historical and self-definitional richness.** It is not the same entry, and neither dominates in every respect.

## 1. Codex entry summary

- **Two corpus-wide senses, both with `SenseKey: null`:**
  1. Primary: “the word or saying raised for investigation.”
  2. Secondary: “a saying, remark, or thread of an exchange.”
- **Ten curated occurrences:** five per sense, from five files and multiple independently attributed masters or master-sections.
- **Self-definition found:** Hanyue Fazang says, “What is called a saying is one matter or one thing before the eyes” (所謂話頭者，即目前一事一法也).
- **Deployment covered:** looking at, raising, taking up, remembering, reciting, and investigating a specified word/question/saying; plus appraising, returning, recognizing, and referring back to words in an exchange.
- **Counts:** 2,575 headword hits in 297 allowlisted texts, plus ten re-derived collocation counts.
- **Verification:** 10/10 KWICs return `ok == true`; all ten stored `FromLb`/`ToLb` pairs equal the current verifier output.

## 2. Point-by-point comparison

### Sense structure

The Codex entry puts the specifically requested Zen-dictionary sense first and treats it as corpus-wide. That agrees with its evidence: Dahui Zonggao, Zhongfeng Mingben, Wuyi Yuanlai, Hanyue Fazang, and a transmission-record biography all use 話頭 for a specified word, question, or saying that is looked at, raised, remembered, recited, or investigated.

The reference entry puts the ordinary conversational sense first and keys the second sense to `Dahui Zonggao`. The reference’s own second-sense evidence extends well beyond Dahui—to Yongjue Yuanxian, Gaofeng Yuanmiao, and later records—so a master-specific `SenseKey` does not fit §4 as well as a null corpus-wide key. Its label “From Dahui on” is useful historical framing, but historical origin and master-specific meaning are not the same schema relation.

For the ordinary sense, the reference prefers “the point of a saying” and “the gist of what was said.” Codex stays closer to the observable noun—“a saying, remark, or thread of an exchange”—and therefore makes fewer claims about a hidden “point” or “gist.”

**Advantage: Codex** for primary ordering, schema semantics, and describe-don’t-interpret discipline.

### Literal account

Codex gives the graph values as “speech/saying” + “head/front.” The reference is more linguistically precise in observing that 頭 can function as a noun suffix, with “stone” (石頭) and “tongue” (舌頭) as examples. That prevents an overly concrete reading of “head.”

**Advantage: reference.** Codex’s literal line is serviceable but less exact.

### Occurrences and attribution

Codex has ten occurrences against the reference’s seven. Codex’s first sense draws on Dahui, Zhongfeng, Boshan/Wuyi, Hanyue, and the student biography in the Five Lamps Compendium. Its second sense includes the Sansheng–Xuefeng exchange, Yunmen Xianqin’s “a fine saying,” Yunmen Wenyan’s “return my saying,” the Dongshan Shouchu/Fuyan Liangya “three pounds of hemp” exchange, and Yuantong Faxiu’s “what does your saying say?” Non-null names use exact roster spellings; raised, biographical, or two-speaker passages are null with reasons.

All seven reference KWIC strings also verify as exact substrings. However, six of its seven stored starts disagree with current `zc.verify`, and every `ToLb` is null. Examples:

- Dongshan appraisal: stored `0573a09`; verifier gives `0080b08–0080b10`.
- “Grand old master” rebuke: stored `0597b04`; verifier gives `0156c12–0156c13`.
- Yongjue’s flavorless saying: stored `0424a18`; verifier gives `0396b05–0396b07`.
- Song retrospective: stored `0938a16`; verifier gives `0653a16–0653a17`.
- Gaofeng biography: stored `0700a09`; verifier gives `0700c18`.

The reference’s Gaofeng KWIC verifies, but it does not contain the headword 話頭; it supplies contextual evidence for an assigned question rather than an occurrence of the dictionary headword. Codex kept every occurrence headword-bearing.

The reference nevertheless has one especially strong ordinary-sense choice that Codex lacks: Dongshan Liangjie’s “a fine saying; it only lacks a follow-up line” (好箇話頭，只欠進語). That is earlier and lexically sharper than Codex’s later “fine saying” example.

**Advantage: Codex overall** for anchors, headword-bearing evidence, breadth, and attribution hygiene; **reference** for the single Dongshan appraisal example.

### Counts and variants

Every numerical statement in Codex was re-run against the current allowlist through `zc.count`. The reference appears to retain counts from an earlier concordance state. Current results include:

| Phrase | Reference | Current `zc.py` / Codex |
|---|---:|---:|
| headword 話頭 (“saying”) | 2,488 | 2,575 |
| 還我話頭來 (“return my saying”) | 71 | 78 |
| 話頭也不識 (“does not even recognize the saying”) | 130 | 185 |
| 記得話頭 (“remember the saying”) | 59 | 68 |
| 好箇話頭 (“a fine saying”) | 19 | 24 |
| 參話頭 (“investigate a saying”) | 173 | 187 |
| 看話頭 (“look at a saying”) | 104 | 118 |
| 看箇話頭 (“look at a saying”) | 39 | 45 |
| 提起話頭 (“take up a saying”) | 34 | 37 |
| 無義味話頭 (“a saying without the flavor of meaning”) | 34 | 39 |
| 死話頭 (“dead saying”) | 39 | 40 |

Codex reports fewer collocation types, but all its reported counts reproduce exactly. The reference covers more valuable variants—especially “dead saying” (死話頭), “saying-tail” (話尾), and “saying-waist” (話腰)—but several totals need refreshing.

**Advantage: Codex** for numerical reliability; **reference** for variant breadth.

### Self-definitions and richness

Codex foregrounds one explicit definition from Hanyue Fazang and reports it as that author’s statement rather than a universal definition.

The reference is richer. It includes:

1. Ruibai Mingxue’s “what is called the saying is one’s own native ground…” definition.
2. Hanyue Fazang’s statement that “this one question… is therefore called the saying.”
3. Hanyue’s contrast: “in ancestral-teacher Chan it is called the saying; among Confucians it is called the investigation of things.”
4. The interview sequence asking about “saying-head,” “saying-tail,” and “saying-waist.”
5. A later text’s explicit retrospective that talk of “looking at the saying” arose in the Song.
6. The text-drawn warning against holding a “dead saying” without raising doubt.

These are valuable describe-only facts because they are presented as named texts’ own statements. Codex found a second Hanyue definition during research but did not include it, so its blind draft left real corpus value unused.

**Advantage: reference.** This is the main respect in which Codex is worse.

### #0b Zen-only discipline and #0c English

Codex does not call the headword a practice, method, technique, meditation, Japanese kōan exercise, or present-moment instruction. It translates the actions literally: look at, raise, take up, remember, recite, and investigate. Its English targets do not require the reader to know an untranslated loanword, and all Chinese in prose is paired with English.

The reference is substantially updated toward the same standard and usually quotes potentially loaded language as the text’s own claim. It still includes the untranslated alternate target “huatou,” raw Chinese `RelatedTerms`, and “critical phrase,” which is less strictly aligned with #0c and the user’s specific instruction to render the term as the word/saying raised for investigation. Its “doing the work” phrase is defensible where it translates the text’s own 作工夫, but it should remain visibly a quotation and not become the entry’s category.

**Advantage: Codex.**

## 3. Where Codex is better

- All ten KWICs verify and all ten line ranges are synchronized.
- Every occurrence contains the headword.
- All stated counts reproduce with the current toolkit.
- The multi-master sense is correctly corpus-wide rather than master-keyed.
- The requested word/saying-under-investigation sense is primary.
- The ordinary sense avoids asserting an unseen “point” or “gist.”
- English targets and prose follow the revised #0b/#0c framing more strictly.
- More curated occurrences and clearer null-attribution reasons.

## 4. Where Codex is worse

- It misses the noun-suffix analysis of 頭.
- It includes only one self-definition although the corpus contains several useful author-specific definitions.
- It omits the Song-period retrospective, the “dead saying” contrast, and the rare “saying-tail/saying-waist” forms.
- Its ordinary-sense evidence is broader, but the reference’s Dongshan “fine saying; only lacks a follow-up” passage is the sharper defining example.
- It leaves `RelatedTerms` empty to avoid untranslated identifiers; the schema would benefit from a clarified policy allowing link keys while requiring translated display prose.

## 5. Rule ambiguities encountered

1. **Master-specific key versus historical origin.** The evidence may support Dahui as a major early source without making the later multi-master meaning a `Dahui Zonggao`-specific sense. §4 favors a null key, but the reference uses a Dahui key.
2. **How far #0c reaches into schema identifiers.** `SourceTerm` and `Kwic` must remain Chinese, and `RelatedTerms` normally need exact source-term keys for linking. The new rule explicitly names target/explanation/note prose, but also says Chinese should appear nowhere else untranslated. Codex left `RelatedTerms` empty rather than risk non-linkable English labels.
3. **Later self-definitions.** A late master’s “what is called…” formula is high-value evidence, but it should be attributed to that author rather than silently promoted into a timeless corpus definition.
4. **The literal role of 頭.** “Head/front” follows the graph, while the reference’s noun-suffix analysis better fits compounds such as “stone” and “tongue.” The guide does not say how much historical grammar may be asserted without an in-corpus grammatical gloss.
5. **Sense boundary.** The specified saying under investigation grows out of the ordinary “saying/remark” noun. The distinct verbs and later explicit definitions justify two senses, but they remain historically and lexically connected rather than unrelated homonyms.

## Final assessment

**Did Codex do better, worse, different, or exactly the same?** Different, and modestly better overall. Codex wins on verifiability, current counts, schema use, sense ordering, attribution discipline, and the revised English/Zen-only rules. The reference wins on linguistic nuance, historical range, self-definitions, contrasts, and rare variants. The best eventual merged entry would keep Codex’s structure and verified anchors while selectively adding the reference’s noun-suffix note, Dongshan appraisal, explicitly attributed self-definitions, and text-drawn contrasts after refreshing every count and line anchor.
