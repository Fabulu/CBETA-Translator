# GATE 3 VERDICT — t_d03aa9267f79 · 大機大用

**VERDICT: PASS**

Independent adversarial audit, 2026-07-12. All checks run from scratch against
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5`, allowlist `zen-corpus.json` (462 texts),
roster `master-dates.json`. Method: XML-tag/apparatus-stripped flow text (teiHeader, `<note>`,
`<cb:mulu>`, `<rdg>` removed; whitespace collapsed), per-char lb mapping.

## 1. KWIC integrity — ALL 7 PASS
| RelPath | Kwic (head) | matches | lb found | lb claimed |
|---|---|---|---|---|
| T/T47/T47n1990.xml | 百丈得大機，黃檗得大用，餘者盡是唱導之師。 | 1 | 0587b18–0587b19 | ✓ |
| C/C077/C077n1710.xml | 百丈得大機黃檗得大用餘者盡是唱道之師 | 1 | 0618a15–0618a16 | ✓ |
| J/J25/J25nB171.xml | 如何是大機大用？如何是大機之用？… | 1 | 0571a02–0571a03 | ✓ |
| C/C077/C077n1710.xml | 見馬祖大機大用然且不識馬祖 | 1 | 0617c03–0617c04 | ✓ |
| J/J25/J25nB171.xml | 可見大機大用不在蒲團禪板上。 | 1 | 0525a15–0525a16 | ✓ |
| B/B27/B27n0152.xml | 以麄放狂亂為大機大用 | 1 | 0611b15–0611b16 | ✓ |
| T/T47/T47n1997.xml | 大機圓應大用縱橫。不墮千聖機關。 | 1 | 0765c11–0765c11 | ✓ |

Exact contiguous verbatim substrings, all unique, punctuation byte-exact (T47n1990's modern
punctuation incl. ？ verified in-file; C/B-canon KWICs correctly unpunctuated; the entry even
correctly preserves the C-canon's 唱道 vs T-canon's 唱導). No ellipsis/stitch/apparatus.

## 2. Attribution — ALL CORRECT (incl. the audit's specific concern)
- **T47n1990 @0587b18 → null**: context verbatim 溈山問師：「百丈再參馬祖因緣…」師云：「此是顯大機大用。」
  溈山云：「馬祖出八十四人善知識，幾人得大機？幾人得大用？」師云：「百丈得大機…」溈山云：「如是，如是。」
  — the line is 仰山's answer INSIDE a 溈山問仰山 two-master dialogue → **null is correct** per the
  raised/two-speaker rule, exactly as flagged in the audit brief ✓.
- **C077n1710 @0618a15 → null**: same dialogue, 古尊宿語錄 百丈懷海禪師語錄 section (cb:mulu verified:
  百丈懷海禪師語錄一), 溈山問仰山…仰山云百丈得大機… → null correct ✓.
- **C077n1710 @0617c03 → null**: 師謂眾曰…黃檗聞舉不覺吐舌…檗曰不然今日囙和尚舉得見馬祖大機大用…
  — 黃檗's reported speech inside 百丈's record, two-speaker → null correct ✓.
- **J25nB171 @0571a02 → null**: 問：「古人道：『一人得大機，一人得大用。』如何是大機大用？…」師答：
  「須知看孔著楔，買帽相頭。」 — the KWIC is the QUESTIONER's words (機緣 section) → null correct ✓.
- **J25nB171 @0525a15 → 天隱圓修**: 晚參，師拈拄杖云：「達磨一宗…觸著則任意揮張，便道大機大用…
  可見大機大用不在蒲團禪板上。」 — the master's own 晚參 critique, exactly as AttributionNote ✓. On roster ✓.
- **B27n0152 @0611b15 → 玉林通琇**: cb:mulu = 客問評註 ✓ (head 客問); under 第三須悟處諦當 (verified),
  in the answering/expounding voice of 玉林's own 語錄: 以鹵莾承當為有力量…以麄放狂亂為大機大用以顢頇為
  透脫無餘 ✓. On roster ✓.
- **T47n1997 @0765c11 → 圜悟克勤**: 小參四 (cb:mulu verified); 師云。大機圓應大用縱橫…舉一機千機截斷。
  拈一事萬事齊彰 — own words ✓. Roster spelling 圜悟克勤 ✓.

## 3. Allowlist — PASS
All RelPaths + all 5 SourceTexts in zen-corpus.json ✓; every SourceText attests the headword ✓.
Note/Explanation-cited files also all allowlisted: C078n1720, X80n1565, T51n2077, T48n2004 ✓
(all four confirmed inside the 23-file 百丈得大機 set).

## 4. Explanation/Note honesty — PASS with 2 minor flags
Phrase checks, 1 hit each in cited file: 此是顯大機大用 (T47n1990 AND C077n1710) ✓;
幾人得大機？幾人得大用？ (T47n1990) ✓; 見馬祖大機大用然且不識馬祖 (C077n1710) ✓;
觸著則任意揮張，便道大機大用 ✓; 學人問著蒲團，即將蒲團打 ✓; 以鹵莾承當為有力量 ✓;
師云。大機圓應大用縱橫 ✓; 大機圓應大用直截 = exactly 1× corpus-wide, in C078n1720 ✓ (as claimed).
File-count claims all EXACT: 大機大用 in **124** files ✓; 大機之用 in **43** ✓; 大用之機 in **10** ✓;
大機 in **245** ✓; 大用 in **330** ✓; 大機圓應 **16× in 12** ✓ (exact); 全機大用 **115×** ✓ (exact);
百丈得大機 in **23 allowlist files** ✓ (exact; the six named texts all in the set).

**Minor flag M1 — occurrence totals vs counting method.** Five occurrence totals are slightly above
my strictest apparatus-stripped count but inside the note-inclusive count, so they reflect a
methodology difference (apparatus `<note>`/mulu text included), NOT fabrication:
276 claimed vs 274 strict / 283 note-inclusive (大機大用); 72 vs 69/75 (大機之用); 13 vs 12/13
(大用之機); 988 vs 975/1007 (大機); 2094 vs 2066/2116 (大用); 447 vs 445/451 (大用現前).
File counts (the structurally load-bearing numbers) are all exact. Recommend standardizing the
counting method (apparatus-stripped) build-wide; not verdict-blocking.

**Minor flag M2 — quote-mark drop in an AttributionNote.** The J25nB171 @0571a02 AttributionNote
quotes "古人道：一人得大機，一人得大用"; the file reads 古人道：『一人得大機，一人得大用。』 (nested
『』 dropped). The inner phrase 一人得大機，一人得大用 IS exact-contiguous; only the frame's
punctuation was altered. KWIC itself unaffected. Restore the 『』 for strict verbatim hygiene.

## 5. Multi-source — PASS
Four roster masters across houses and eras (仰山慧寂's 語錄, 圜悟克勤, 天隱圓修, 玉林通琇),
5 source texts across T/C/J/B canons; the defining dialogue independently witnessed in 23 files. ✓

## 6. Describe-only — PASS
The 大機/大用 split is explicitly the corpus's own (the 溈山–仰山 dialogue distributes the halves),
and the Note pre-empts misreading: "Do NOT read the split as this entry's interpretation; it is the
texts'." Deployment triple (praise / test-question / named error) is observable and each leg is
grep-attested. "機" glossed only lexically (pivot / trigger / loom-mechanism / latent capacity).
Closes with the no-gloss formula. No interpretation found. ✓

## 7. Nesting / RelatedTerms — PASS
大機 (975–1007× / 245 files) and 大用 (2066–2116× / 330) are the genuine constituents — the corpus
itself splits the compound, so relating them is correct, not coincidental-prefix linking.
大機之用 (43 files), 大用之機 (10 files), 全機大用 (115× in 60) all attested compounds ✓.
RelatedMasters all roster-canonical, incl. 百丈懷海/黃檗希運 who anchor the defining dialogue ✓.

## Punch list
1. (minor, optional) M1: standardize occurrence-count methodology; totals drift 1–13 over
   strict apparatus-stripped counts (file counts exact).
2. (minor, optional) M2: restore 『』 in the @0571a02 AttributionNote quote.

**Defects: 0 blocking, 2 minor.**
