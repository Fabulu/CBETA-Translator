# WORK — 百尺竿頭 (t_53da4e346a6f) "atop a hundred-foot pole"

## Concordance (Zen allowlist only)
- 百尺竿頭: **533 occurrences** across ~130 texts.
- Top files: X82n1571 (23), C078n1720 (17), X80n1565 五燈會元 (14), T51n2077 續傳燈錄 (13), X68n1319 (11), X81n1568 (11)…
- Fixed collocations (allowlist counts): 百尺竿頭進步 41, 竿頭進步 98, 更進一步 73, 十方世界是全身 9, 百尺竿頭坐底人 20, 得入未為真 37. → the term is inseparable from the 進步 imperative.

## Sense analysis — ONE corpus-wide sense
百尺竿頭 = the very top of a hundred-foot pole = the extreme summit of attainment. The whole Chan point is the paradoxical imperative 進步: even at the top you must step forward (百尺竿頭須進步), whereupon 十方世界是全身. Deflationary/anti-complacent: 不動人 ('the motionless one' at the top) is 未為真 ('not yet real') — do not stop at any attainment. No master bends the image; one sense, SenseKey=null, with a clear origin (Changsha).

## Attribution — a genuine FRAMING spread (not a dispute over the reading)
- The VERSE (百尺竿頭不動人。雖然得入未為真。百尺竿頭須進步。十方世界是全身) is **Changsha Jingcen's** in the lamp records: 五燈會元 files it under 湖南長沙景岑招賢禪師 (X80n1565 lb 0094c18–19), his own record, followed by his Q&A 秖如百尺竿頭如何進步。師曰…. Comment-lines call the author 老長沙 (C078n1720 lb 0705a16) — independent corroboration.
- The koan-FORM circulated attached to others: 無門關 Case 46 (竿頭進步) frames the QUESTION 百尺竿頭如何進步 as 石霜和尚's and the verse as 古德's; 石霜示眾 framing recurs (C078n1720 lb 0756a17). The corpus writes only '石霜' — roster has BOTH Shishuang Qingzhu (807–888) and Shishuang Chuyuan/Ciming (986–1039); undisambiguated → those left MasterName null (do not guess).
- Classic interlocutor: 三聖 (Sansheng Huiran) questions the verse's author — 承師有言『百尺竿頭須進步』 (B25n0144). Verse-variant: 不動人 (五燈會元) vs 坐底人 (無門關).
- T51n2076 景德傳燈錄 has 0 hits for the exact string 百尺竿頭 (uses a different form there); origin anchored in 五燈會元 instead.

## Curated occurrences — all verified verbatim, single-line, correct-ed lb (fz_verify ALL-OK)
1. X80n1565 0094c18 「雖然得入未為真。百尺竿頭須進步。」 — MN=Changsha Jingcen (origin verse, his head).
2. T48n2005 0298c12 「石霜和尚云。百尺竿頭如何進步。又古德云。」 — MN=null (Wumenguan Case 46).
3. B25n0144 0623b07 「百尺竿頭須進步，十方世界是全身。」 — MN=null (中峰廣錄; 三聖 questions next).
4. C078n1720 0705a16 「百尺竿頭進步時築着磕著自家底老長沙」 — MN=null (ties 進步 to 老長沙).
5. D46n8930 0030a06 「雖然得入未為真百尺竿頭須進步」 — MN=null (unpunctuated edition witness).

## Validation: multi-source
5 independent texts (五燈會元, 無門關, 中峰廣錄, C078n1720, D46n8930) across Song/Yuan and multiple lineages. Reading stable; only the koan-framing (verse=Changsha/古德; question=石霜) varies — recorded honestly in Note. Not 'disputed' (evidence solid, no attribution fight over the meaning).

## RelatedTerms (genuine)
百尺竿頭進步 (the completion of the phrase / the koan title — real nesting, not coincidental), 更進一步 (a step further), 十方世界是全身 (the resolution line).

## Gate 2 verification (Frizzle-adversarial, re-derived from source) — STATUS=verified
- All 5 KWICs EXACT CONTIGUOUS after tag-strip (script). Zero ellipses.
- All 5 FromLb = nearest-preceding <lb>; X80n1565 ed="X" correct. All match claimed.
- All 5 RelPaths in zen-corpus.json allowlist. Zero contamination.
- Attribution confirmed at governing cb:mulu head: bcgt-1 → 湖南長沙景岑招賢禪師 (Changsha Jingcen ✓; context 師示偈曰 = his own verse). 石霜 lines correctly left null (2 石霜 in roster, undisambiguated — per gate guidance).
- RelatedMasters (Changsha Jingcen, Sansheng Huiran) exact in roster.
- Explanation verse quote 百尺竿頭不動人。雖然得入未為真。百尺竿頭須進步。十方世界是全身 confirmed present in X80n1565 (carries source line-breaks 百尺竿[nl]頭…, contiguous in content). 承師有言 (B25n0144), 坐底人 variant (T48n2005) confirmed.
- No repairs needed.

## GATE 3 repair (Claude, 2026-07-11 21:46 +02:00) — STATUS=verified
Applied Gate-3 MAJOR punch #1 + NIT #3 + count restatement (#2).
- occ B25n0144 @0623b07: text is 祖堂集 (TEI title 祖堂集; 中峰/中峯 count=0 in file), NOT 中峰廣錄 (=B25n0145). GREP-verified: line 11159 governing mulu head 岑和尚 (岑和尚嗣南泉); line 11207-08 introduces the verse 師當時有偈曰 → Changsha Jingcen's OWN verse. Set masterName null → "Changsha Jingcen"; rewrote attributionNote accordingly.
- Note dates: Shishuang Chuyuan 986–1039 → 987–1040 (master-dates.json: floruit 987, death 1040). GREP-verified.
- Note counts restated as reproducible hit/file pairs, GREP-verified over the 462-file allowlist (tags/notes/rdg stripped, 4 method-variants all agree): 百尺竿頭進步 41 hits/36 files; 竿頭進步 98/73; 更進一步 73/53; 十方世界是全身 9/9; 百尺竿頭坐底人 20/17. NOTE: my reproducible count for 百尺竿頭進步 is 41/36, which does NOT match the verdict's stated 53/41 — the verdict's number is not reproducible by any of 4 grep methods (with/without body-restriction, with/without note-strip all give 41/36; loose intervening-char match gives 184). Wrote the grep-verified figure per the hard "GREP-verify before writing" rule.

## GATE 3 count CORRECTION (Claude/Frizzle, 2026-07-11 22:24 +02:00) — STATUS=verified
The prior GATE-3 pass's method was too strict: it stripped tags but did NOT collapse line-break
whitespace, so it missed occurrences of these CJK phrases that are split across a source line break
(e.g. `百尺竿頭進\n步`, `一似鐵橛\n相似`). Adding a whitespace-collapse step (strip tags → remove ALL
whitespace) over the same 462-file allowlist (notes/rdg removed) REPRODUCES the gate verdict's figures
exactly. This is the correct method — the intervening whitespace is a typesetting artifact, not a real gap.
GREP-verified (scratchpad count.py, method C):
  百尺竿頭進步 53 hits / 41 files  (was 41/36 — 12 hits hidden across line breaks)
  竿頭進步     113 / 81            (was 98/73)
  更進一步     81 / 60             (was 73/53)
  十方世界是全身 15 / 15           (was 9/9)
  百尺竿頭坐底人 29 / 26           (was 20/17)
All five HIT counts match the gate verdict's independent recount (53/113/81/15/29). entry.v2.json Note
updated to these figures; parenthetical method note changed to "…stripped and line-break whitespace
collapsed". No other field touched. JSON re-parsed OK; STATUS=verified; Validation=multi-source.
