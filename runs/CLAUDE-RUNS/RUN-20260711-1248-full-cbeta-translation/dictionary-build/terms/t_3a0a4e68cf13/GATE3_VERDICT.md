# Gate 3 Verdict — 葛藤 (t_3a0a4e68cf13)

VERDICT: PASS

Verifier: independent adversarial pass (fresh model). All evidence re-derived from
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` with a tag-stripping contiguity
checker that DROPS `<note>`/`<rdg>` apparatus content and records `lb n=` at match
start/end (X-canon dual-edition lbs handled: `ed="X"` authoritative).

## Per-sense findings (single sense, SenseKey null)

### Check 1 — KWIC exact + contiguous: 4/4 PASS
1. T/T48/T48n2003.xml 0149a29 「那裏如此葛藤。須是斬斷語言。格外見諦。」 — FOUND x1,
   lb 0149a29 exact (raw line 1428). Context: …不得已而立箇方便語句。如祖師西來。單傳心印。
   直指人心。見性成佛。 then the KWIC — running commentary prose, and the 葛藤↔語言 juxtaposition
   claimed in the Explanation is really there (斬斷語言 immediately glosses the vine-tangle).
2. T/T48/T48n2003.xml 0152c18 「許多葛藤公案。具眼者。試說看。」 — FOUND x1, lb 0152c18 exact.
   Context: 向上一路。千聖不傳…既是不傳。為什麼。却有 + KWIC, directly before 【一二】舉僧問洞山 —
   end-of-commentary flourish before case 12. The collocation 葛藤公案 is genuine.
3. B/B25/B25n0145.xml 0741a10-11 「道者禪葛藤禪。更有脫略機境不受差排者。喚作向上禪。」 —
   FOUND x1, lb 0741a10→0741a11 exact (raw line 3839). Context confirms the doxographic list:
   喚作如來禪祖師禪平實禪杜撰禪文字禪海蠡禪外道禪聲聞禪凡夫禪五味禪棒喝禪拍盲禪道者禪葛藤禪 —
   葛藤禪 is one pejorative label in a list capped by 向上禪, exactly as described.
4. X/X80/X80n1565.xml 0257b24 「上堂。事不獲已。與諸人葛藤。」 — FOUND x1. Checker reported
   lb 0460b18 = the paired `ed="R138"` tag; the file has `<lb ed="X" n="0257b24"/>` +
   `<lb ed="R138" n="0460b18"/>` at this point — entry cites `ed="X"`, correct per spec.

No ellipses, no stitching, no altered punctuation. Each KWIC occurs exactly once in its file.

### Check 2 — RelPath real + Zen allowlist: PASS
T48n2003, B25n0145 (天目中峰廣錄), X80n1565 all exist and are all in `Assets/Data/zen-corpus.json`.
No contamination.

### Check 3 — Multi-source: PASS
Three independent texts / eras: 碧巖錄 (Yuanwu, Song), 天目中峰廣錄 (Yuan), 五燈會元 (Puneng entry).
Same metaphor (words-as-entangling-vines) in all. The Explanation's uncited facet 打葛藤 was
spot-checked: 20 raw hits in T48n2003 alone — the "left in live index" claim is plausible.

### Check 4 — Over-read: none
"Verbal tangle" is the corpus reading; the T48 0149a29 passage itself pairs 葛藤 with 斬斷語言,
which is as close to an in-corpus gloss as one gets. No master-specific uniqueness claim is made.

### Check 5 — Imported abstraction: none
Deflationary throughout; the entry explicitly says "NOT a mystical term." The rendering stays on
the vine metaphor (tangle of words) rather than importing e.g. "conceptual proliferation"
(prapañca) or other general-Buddhist abstractions. Good.

### Check 6 — Attribution honesty: PASS with one minor observation
- T48 0149a29 / 0152c18: Yuanwu's 評唱 — verified structurally (running commentary paragraphs;
  capping phrases live in separate inline `<note>` elements, which my checker strips — both KWICs
  survive the strip, so both are commentary flow). CORRECT.
- X80 0257b24: the head 杭州慶善院普能禪師 (preceded by lineage head 慶善震禪師法嗣) IMMEDIATELY
  precedes the KWIC — the 上堂 is Puneng's own opening. Attribution to Qingshan Puneng CORRECT.
- B25 0741a10: MasterName null. Not wrong — but under-informative. I traced the containing
  division: the KWIC sits inside 法語(二) → 示嗣禪上人 (head at raw line 3780, lb 0739b14) of
  天目中峰廣錄 — i.e. a dharma-instruction BY Zhongfeng Mingben himself. "Speaker unnamed in this
  stretch" is literally true (no 師曰/name marker nearby) but the speaker IS derivable from the
  document structure (Zhongfeng's own 法語). Null is the conservative, non-erroneous choice, so
  this is NOT an ATTRIBUTION_ERROR; it is an optional improvement.

## Issues (tagged)
- (minor, non-blocking) ATTRIBUTION-UNDERCLAIM: B25n0145 0741a10 occurrence could be attributed to
  Zhongfeng Mingben (中峰明本) — the passage is inside his 法語 示嗣禪上人 in 天目中峰廣錄 ·
  evidence: cb:mulu/head 示嗣禪上人 at lb 0739b14 (raw line 3780), no intervening head before
  0741a10 · recommended fix (optional): set MasterName "Zhongfeng Mingben" + note "法語 to
  Chan-man Si in Zhongfeng's own record"; the null form is also acceptable as-is.

## Verified occurrences: 4/4 KWIC confirmed verbatim
