# WORK — 大悟 (t_db4a932ce500)

## Sense analysis
One corpus-wide sense (SenseKey null): **大悟 = "great/thorough awakening; to be greatly awakened"** —
大 great + 悟 wake up. The decisive complete breakthrough, opposed to 小悟 (partial). Mostly a VERB of
the breakthrough moment (於言下大悟). Fixed noun phrase 大悟底人 "a greatly-awakened person."
Deflationary — waking up thoroughly, not a metaphysical Absolute.

Dialectic: 大悟 vs 迷 — Huayan's case 大悟底人為甚麼卻迷 ("why does the greatly-awakened one fall
back into delusion?"), answered 破鏡不重照，落花難上枝 (real awakening does not relapse).

## Multi-source gate → PASS (multi-source)
- 景德傳燈錄 T51n2076: Linji's 於言下大悟 under Dayu; also the Huayan 大悟底人卻迷 case.
- 五燈會元 X80n1565: Huayan Xiujing's 大悟底人為甚麼却迷 case.
- 頌古 J25nB171: explicitly 舉僧問華嚴 … 嚴云 — independent witness confirming Huayan attribution.
- 大悟底人 also recurs across X79/X81/T47n1992 etc. (left in live index).

## Honesty-encoded catch (Note field)
J10nA158 verse-annotation: 大悟十八遍，小悟不計數，**本是宋儒言，非大慧所說** — the famous
"18 great awakenings" self-report popularly pinned on Dahui Zonggao is flagged here as a
Song-Confucian phrase, NOT Dahui's. Recorded as disputed attribution; do NOT credit Dahui.

## Attribution verified
- T51n2076 0299b: 師 = 臨濟義玄 (Linji), Huangbo→Dayu narrative.
- X80n1565 0271b: 師 = 華嚴休靜, via 洞山 dialogue + 後唐莊宗 summons above.
- J25nB171 0564a27: explicit 華嚴 / 嚴云.

## KWIC integrity
Exact contiguous substrings after tag-stripping (raw reads: X80n1565 ~20544-20551; J25nB171
~5195-5202; J10nA158 ~6926-6934; T51n2076 ~9966-9975). Note X80 uses 。 punctuation, J25 uses
，「」？ — each verbatim from its own file.

## Curated: 4 occurrences (4 texts). Validation: multi-source (base word); 十八遍→Dahui disputed.

## GATE 2 verify (Claude adversarial repair)
- Re-grepped every cited file. All 4 KWICs are EXACT CONTIGUOUS substrings after <lb/> tag-strip
  (the split-across-lb cases 大悟底人|為甚麼 in X80 0271b06-07 and 師於言下大悟云。元來黃|檗 in
  T51 0299b28-29 grep as split but reassemble contiguous). Zero ellipses.
- RelPaths: T51n2076, X80n1565, J25nB171, J10nA158 — all in zen-corpus.json. No contamination.
- Attribution re-check at section heads:
  - T51 0299b: section head confirmed 鎮州臨濟義玄禪師 (line 0299b15); 師 = Linji. KEEP.
  - X80 0271b: 師 = 華嚴 (福州東山華嚴 / 後唐莊宗 summons); Huayan's own record, 師曰 answer. KEEP.
  - J25 0564a27: 舉僧問華嚴…嚴云 — TWO-SPEAKER QUOTED CASE CITATION in another author's 頌古.
    FIX: MasterName Huayan Xiujing → null (gate rule: null for two-speaker/quoted lines). Kept as
    corroborating witness; note updated. Multi-source unaffected (T51+X80 already 2 independent texts).
  - J10 0074a07: 大悟十八遍 misattribution line — already null. KEEP uncredited (per gate directive).
- Note prose aligned to the actual record: 大悟十八遍（次），小悟不知其數 → 大悟十八遍，小悟不計數
  (matches J10nA158 verbatim; removed the paraphrase drift).
- STATUS = verified.
