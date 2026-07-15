# 家風 (t_c728f3a8e02b) — work notes

**STATUS: verified** (Gate 2, adversarial re-derivation from Chinese).

## Gate 2 verification (independent Claude pass)
Re-grepped every cited file (targeted, single-file) and confirmed each KWIC is an EXACT CONTIGUOUS substring after XML-tag stripping:
- B14n0082 `僧問如何是天柱家風師曰時有白雲來閉戶更無風月四山流` ✓ (spans lb b07→b09)
- J24nB137 `問：「如何是和尚家風？」…我卻識你家風。」` ✓ (lb 0361c26→c27)
- J25nB156 `佛祖家風，衲僧活計，…也要仔細。` ✓ (lb 0059a02→a03)
- J26nB178 `僧問：「臨濟家風則且置，曹洞宗旨是如何？」師云：「牽連斷貫索。」` ✓ (lb 0111c04→c05)
- J27nB191 `因僧問：「如何是古佛家風？」師曰：「銀蟾初出海，何處不分明？」` ✓ (lb 0167b24→b25; 銀蟾/初 split by an lb tag in the raw XML but contiguous after stripping)
- **Fix:** B14n0082 FromLb 0169b08 → **0169b07** (the KWIC's opening 僧 sits on b07).
- Contamination: none — all 6 RelPaths in zen-corpus.json allowlist.
- Multi-source: 333 files — solid. Over-read: none (gloss is literal/deflationary, explicitly anti-mystical). RelatedTerms all genuine (synonyms 宗旨/宗風/門庭 + stock-question frame 如何是和尚家風).
- No other changes.


## Concordance (Zen allowlist only)
- **333 allowlist files, ~613 occurrences.** Distributed across every collection (B, C, D, J, T, X). This is one of the most broadly attested terms in the corpus.
- Method: grep over `xml-p5`, filtered to the 462 `zen-corpus.json` relpaths; KWIC extracted verbatim (tags stripped), nearest preceding `<lb n>` recorded.

## Sense analysis
**One sense, corpus-wide (SenseKey=null).** 家風 = "family wind" = the distinctive teaching-style / household-manner of a master or a lineage.

Dominant construction is the stock interview question **如何是（和尚）家風？** answered with a concrete homely image rather than doctrine:
- 天柱家風 → 時有白雲來閉戶 (white clouds close the door)
- 和尚家風 → 秋收冬藏 (harvest in autumn, store in winter) [C077n1710]
- 和尚家風 → 老僧耳背，高聲問…你問我家風，我卻識你家風 (playful, reflexive) [J24nB137]

Scales freely from a single teacher to a whole lineage/tradition:
- 臨濟家風, 曹洞…, 雲門延慶家風, 風穴家風 (house of a lineage)
- 佛祖家風 / 古佛家風 (buddhas-and-ancestors; deflationary: = gruel and rice, daily life) [J25nB156]
- 野老家風, 老衲家風, 窮措家風 (self-deprecating "plain/poor" styles)

Tone is domestic and self-characterizing, deliberately plain — never a metaphysical essence. Renders literally as "family wind / house style"; do NOT inflate to "spiritual heritage" or similar.

## Multi-source verdict
**multi-source** — trivially. 333 texts, dozens of masters, all collections, ~1000-year spread. The reading (house/lineage teaching-style) is stable everywhere.

## Master-specific senses?
**None warranted.** 家風 is a house-neutral word applied to *every* master and lineage; no master bends it to a private meaning. The lineage compounds (臨濟家風 etc.) are the same sense at a larger scale, not distinct senses.

## Curated occurrences (5)
B14n0082 (天柱, stock Q&A) · J24nB137 (reflexive play) · J25nB156 (佛祖家風 = daily life) · J26nB178 (臨濟家風 vs 曹洞宗旨, lineage scale) · J27nB191 (古佛家風, tradition scale). All verbatim-verified against source.

## Honest thin spots
- Per-occurrence MasterName left null: the curated passages come from later Ming/Qing 語錄 (J collection) where the speaking master is not in `master-dates.json`; B14n0082 is Tianzhu Chonghui (崇慧), likewise not in the master list. Attribution notes name the text-identified master where known.
- RelatedTerms 宗風/宗旨/門庭 are adjacent "house-style / house-teaching" words seen co-occurring (e.g. 臨濟家風…曹洞宗旨); not separately concordanced here.
