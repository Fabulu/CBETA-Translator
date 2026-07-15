# WORK — 葛藤 (t_3a0a4e68cf13)

## Sense analysis
One corpus-wide sense (SenseKey null): **葛藤 = "verbal tangle / entangling words"** — literally
kudzu-and-wisteria creepers that twist and choke; in Chan the fixed metaphor for WORDS/verbiage that
entangle. Deflationary and negative: the mess of language one should cut through, not a mystical term.

Facets (one sense): 打葛藤 "spin out vines/verbiage"; 葛藤公案 "vine-tangle koan"; 葛藤禪 "vine-Chan"
(pejorative); 截斷／斬斷葛藤 "cut off the tangle"; 與諸人葛藤 "deal you some verbiage."

## Key defining occurrence
碧巖錄 (Yuanwu): 那裏如此葛藤。**須是斬斷語言** — 葛藤 glossed directly as 語言 (words). This nails
the deflationary reading; no imported abstraction needed.

## Multi-source gate → PASS (multi-source)
- 碧巖錄 T48n2003 (Yuanwu Keqin): 葛藤 = 語言 equation; 葛藤公案; plus 打葛藤 / 截斷葛藤 (index).
- B25n0145: doxographic 葛藤禪 label among pejorative Chan-types (…道者禪葛藤禪…喚作向上禪).
- 五燈會元 X80n1565: 慶善普能 上堂…與諸人葛藤 (named master, self-deprecating).
- Three independent texts, consistent metaphor.

## Attribution
- T48n2003: Yuanwu's 評唱.
- X80n1565 0257b24: inline head 杭州慶善院普能禪師 → Qingshan Puneng.
- B25n0145 0741a: doxographic list, speaker unnamed in the stretch → MasterName null (noted).

## KWIC integrity
Exact contiguous substrings after stripping <lb/>/<pb/>/<anchor> tags (raw reads: T48n2003 ~1424-1431,
1743-1748; B25n0145 ~3833-3841; X80n1565 ~19514-19519). X80 KWIC's 已 is anchor-wrapped in source but
remains as text after tag-strip → contiguous.

## Curated: 4 occurrences (3 texts). Others (打葛藤/截斷葛藤/葛藤窠, ~1300 raw hits) left in live index.

## GATE 2 verify (Claude adversarial repair) — PASS, no changes
- Re-grepped every cited file. All 4 KWICs EXACT CONTIGUOUS after <lb/>/<anchor> tag-strip.
  X80 0257b24 KWIC 上堂。事不獲已。與諸人葛藤。 — 已 is anchor-wrapped in source but remains as
  text after strip → contiguous verbatim. B25 0741a10-11 道者禪葛藤禪…喚作向上禪 straddles lb,
  reassembles contiguous. Zero ellipses.
- RelPaths T48n2003, B25n0145, X80n1565 — all in zen-corpus.json. No contamination.
- Attribution re-check:
  - T48 0149a29 / 0152c18: Yuanwu Keqin 評唱. KEEP.
  - B25 0741a10: doxographic list of pejorative Chan-types, no named speaker in the stretch →
    MasterName null. KEEP.
  - X80 0257b24: inline head 杭州慶善院普能禪師 → Qingshan Puneng; 上堂 self-opening. KEEP.
- STATUS = verified.
