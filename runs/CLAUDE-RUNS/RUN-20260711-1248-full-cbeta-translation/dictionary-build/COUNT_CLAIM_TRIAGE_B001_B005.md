# Count-claim triage: b001–b005

Source audit: `maintenance/count-claim-audit-20260712T182330Z.json`.

Method: only audit rows marked `mismatch-or-wrong-candidate` or `no-near-candidate` were triaged. Exact contiguous Chinese was recounted with `zc.count`. Hit/file claims referring to the same phrase and sentence are consolidated into one row. A count such as “211 of 462 texts” contains a corpus-size denominator, not a second phrase count; `462` is therefore not corrected to the candidate phrase's file count. Proximity, optional-punctuation, slash-variant, and file-scoped claims are classified separately because `zc.count(single_phrase)` cannot reproduce those compound predicates.

No entry was edited.

## b001 checkpoint

| Entry · field | Old claim | Actual cited phrase | Current `zc.count` | Classification | Proposed exact prose |
|---|---|---|---:|---|---|
| 棒喝 `Note` | 986 occurrences / 232 files | 棒喝 | 992 / 232 | CONFIRMED_STALE | “The compound ‘stick and shout’ (棒喝) has 992 hits in 232 files.” |
| 棒喝 `Note` | 德山棒 487 hits / 173 files | 德山棒 | 490 / 173 | CONFIRMED_STALE | “Deshan's stick (德山棒) has 490 hits in 173 files.” |
| 本來面目 `Explanation` | “如何是…本來面目” 274 / 116 | 如何是本來面目 | 137 / 74 | WRONG_CANDIDATE/complex proximity claim | Replace the ellipsis-frame count with: “The exact question ‘What is the original face?’ (如何是本來面目) has 137 hits in 74 files.” |
| 本來面目 `Note` | 1,145 / 250 | 本來面目 | 1,320 / 264 | CONFIRMED_STALE | “Original face (本來面目) has 1,320 hits in 264 files.” |
| 本來面目 `Note` | 91 / 57 | 父母未生前本來面目 | 92 / 57 | CONFIRMED_STALE | “The original face before father and mother were born (父母未生前本來面目) has 92 hits in 57 files.” |
| 本來面目 `Note` | combined 是汝／你本來面目 70 / 44 | 是汝本來面目; 是你本來面目 | 19 / 17; 51 / 33 | WRONG_CANDIDATE/complex variant claim | State separately: “‘This is your original face’ occurs as 是汝本來面目 19 times in 17 files and 是你本來面目 51 times in 33 files.” |
| 祖師西來意 `Note` | 1,334 / 217 | 如何是祖師西來意 | 1,858 / 231 | CONFIRMED_STALE | “The fixed question ‘What is the ancestral teacher's meaning in coming from the West?’ (如何是祖師西來意) has 1,858 hits in 231 files.” |
| 平常心 S0 `Note` | 152 / 83 | 平常心是道 | 181 / 100 | CONFIRMED_STALE | “‘Ordinary mind is the Way’ (平常心是道) has 181 hits in 100 files.” |
| 平常心 S1 `Note` | 290 / 116 | 平常心 | 309 / 120 | CONFIRMED_STALE | “Ordinary mind (平常心) has 309 hits in 120 files.” |
| 平常心 S1 `Note` | 26 / 19 | 如何是平常心 | 30 / 19 | CONFIRMED_STALE | “‘What is ordinary mind?’ (如何是平常心) has 30 hits in 19 files.” |
| 露地白牛 `Explanation` | 白牛車 93 / 65 | 白牛車 | 99 / 68 | CONFIRMED_STALE | “White-ox cart (白牛車) has 99 hits in 68 files.” |
| 露地白牛 `Explanation` | 露地牛 49 / 40 | 露地牛 | 53 / 43 | CONFIRMED_STALE | “The shortened ‘open-ground ox’ (露地牛) has 53 hits in 43 files.” |
| 露地白牛 `Note` | 如何是露地白牛 59 / 30 | 如何是露地白牛 | 79 / 34 | CONFIRMED_STALE | “The test-question ‘What is the white ox on open ground?’ (如何是露地白牛) has 79 hits in 34 files.” |
| 賓主 S0 `Explanation` | 賓主句 224 / 105 | 賓主句 | 244 / 112 | CONFIRMED_STALE | “‘Guest-host phrase’ (賓主句) has 244 hits in 112 files.” |
| 賓主 S0 `Explanation` | 一喝分賓主 106 / 66 | 一喝分賓主 | 137 / 77 | CONFIRMED_STALE | “‘One shout distinguishes guest and host’ (一喝分賓主) has 137 hits in 77 files.” |
| 賓主 S0 `Explanation` | 無賓主 209 / 99 | 無賓主 | 238 / 107 | CONFIRMED_STALE | “‘No guest and host’ (無賓主) has 238 hits in 107 files.” |
| 賓主 S0 `Note` | 2,258 / 331 “of 462” | 賓主 | 2,364 / 333 | CONFIRMED_STALE | “Guest and host (賓主) has 2,364 hits in 333 of the 462 allowlisted files.” (`462` remains the corpus size.) |
| 賓主 S1 `Note` | 四賓主 142 / 74 | 四賓主 | 143 / 78 | CONFIRMED_STALE | “Four guests and hosts (四賓主) has 143 hits in 78 files.” |
| 賓主 S1 `Note` | 客看客 4 / 4 | 客看客 | 5 / 5 | CONFIRMED_STALE | “‘Guest sees guest’ (客看客) has 5 hits in 5 files.” |
| 賓主 S2 `Note` | 主中主 539 / 164 | 主中主 | 590 / 172 | CONFIRMED_STALE | “‘Host within host’ (主中主) has 590 hits in 172 files.” |
| 賓主 S2 `Note` | 賓主互換 51 / 34 | 賓主互換 | 60 / 38 | CONFIRMED_STALE | “‘Guest and host interchange’ (賓主互換) has 60 hits in 38 files.” |
| 家風 `Explanation` | “如何是…家風” 1,173 / 141 | 如何是家風; 如何是和尚家風 | 0 / 0; 819 / 113 | WRONG_CANDIDATE/complex proximity claim | Replace the ellipsis aggregate with: “The exact question ‘What is the master's family style?’ (如何是和尚家風) has 819 hits in 113 files.” |
| 五位 `Note` | 1,134 / 211 “of 462” | 五位 | 1,155 / 212 | CONFIRMED_STALE | “Five ranks (五位) has 1,155 hits in 212 of the 462 allowlisted files.” (`462` is the corpus size.) |

## b002 checkpoint

| Entry · field | Old claim | Actual cited phrase | Current `zc.count` | Classification | Proposed exact prose |
|---|---|---|---:|---|---|
| 無心 `Note` | “~4,300” / 370 | 無心 | 4,331 / 370 | APPROXIMATE explicitly marked | The approximation remains defensible; for exact prose use: “The raw string ‘no mind’ (無心) has 4,331 hits in 370 files.” |
| 無心 `Note` | 無心道人 121 / 57 | 無心道人 | 122 / 57 | CONFIRMED_STALE | “‘Person of no mind’ (無心道人) has 122 hits in 57 files.” |
| 無心 `Note` | slash-variant offering formula “in 17 texts” | 不如供養一箇無心道人; 不如供養一個無心道人 | 15 / 15; 2 / 2 | WRONG_CANDIDATE/complex variant claim | State separately: “‘Better than offering to a person of no mind’ occurs as 不如供養一箇無心道人 15 times in 15 files and 不如供養一個無心道人 twice in 2 files.” |
| 勘破 `Note` | 1,486 / 250 | 勘破 | 1,503 / 253 | CONFIRMED_STALE | “Examine and expose (勘破) has 1,503 hits in 253 files.” |
| 勘破 `Note` | 什麼處是勘破處 4 hits | 什麼處是勘破處 | 7 / 7 | CONFIRMED_STALE | “‘Where is the place of examination and exposure?’ (什麼處是勘破處) has 7 hits in 7 files.” |
| 葛藤 `Note` | 2,410 / 334 “of 462” | 葛藤 | 2,530 / 334 | CONFIRMED_STALE | “Entangling vines (葛藤) has 2,530 hits in 334 of the 462 allowlisted files.” |
| 上堂 `Explanation` | 上堂舉 688 / 31 | 上堂舉 | 724 / 40 | CONFIRMED_STALE | “‘Ascended the hall and raised…’ (上堂舉) has 724 hits in 40 files.” |
| 上堂 `Explanation` | 上堂僧問 277 / 18 | 上堂僧問 | 300 / 23 | CONFIRMED_STALE | “‘Ascended the hall; a monk asked…’ (上堂僧問) has 300 hits in 23 files.” |
| 上堂 `Explanation` | 元旦上堂 100 hits | 元旦上堂 | 102 / 46 | CONFIRMED_STALE | “‘New Year's Day hall address’ (元旦上堂) has 102 hits in 46 files.” |
| 上堂 `Explanation` | 上堂良久 81 / 14 | 上堂良久 | 96 / 16 | CONFIRMED_STALE | “‘Ascended the hall; after a long pause…’ (上堂良久) has 96 hits in 16 files.” |
| 上堂 `Note` | “~54,000 occurrences,” context also says 403 files | 上堂 | 54,942 / 402 | APPROXIMATE explicitly marked | The rounded hit count is acceptable, but the file claim is stale. Exact prose: “Hall address (上堂) has 54,942 hits in 402 files.” |
| 上堂 `Note` | 陞座 3,350 / 345 | 陞座 | 3,463 / 348 | CONFIRMED_STALE | “‘Ascend the seat’ (陞座) has 3,463 hits in 348 files.” |
| 作麼生 `Explanation` | 怎麼生 15 / 6 | 怎麼生 | 13 / 5 | CONFIRMED_STALE | “The marginal form ‘how?’ (怎麼生) has 13 hits in 5 files.” |
| 作麼生 `Explanation` | 作摩生 369 hits in one text | 作摩生 | 407 / 1 | CONFIRMED_STALE | “The Patriarchs' Hall Collection writes ‘how?’ as 作摩生 407 times in its one file.” |
| 作麼生 `Explanation` | 又作麼生 5,016 / 363 | 又作麼生 | 5,883 / 370 | CONFIRMED_STALE | “‘And then how?’ (又作麼生) has 5,883 hits in 370 files.” |
| 作麼生 `Explanation` | 作麼生道 2,857 hits | 作麼生道 | 3,336 / 309 | CONFIRMED_STALE | “‘Say it—how?’ (作麼生道) has 3,336 hits in 309 files.” |
| 恁麼 `Explanation` | 恁麼也不得 439 / 118 | 恁麼也不得 | 531 / 117 | CONFIRMED_STALE | “‘Thus will not do either’ (恁麼也不得) has 531 hits in 117 files.” |
| 恁麼 `Explanation` | 與麼 8,334 / 344; auditor chose nearby 總不與麼 | 與麼 | 8,710 / 346 | WRONG_CANDIDATE plus stale count | “The graphic variant ‘thus’ (與麼) has 8,710 hits in 346 files.” |
| 恁麼 `Explanation` | 恁麼則 4,900 / 312 | 恁麼則 | 5,426 / 317 | CONFIRMED_STALE | “‘In that case’ (恁麼則) has 5,426 hits in 317 files.” |
| 恁麼 `Note` | “~27,000” / 400 | 恁麼 | 28,391 / 400 | APPROXIMATE explicitly marked | The approximation is explicit; exact prose would read: “Thus (恁麼) has 28,391 hits in 400 files.” |
| 正法眼藏 `Explanation` | graph-variant aggregate 滅却／滅卻正法眼藏 17 / 14 | 滅却正法眼藏; 滅卻正法眼藏 | 7 / 5; 10 / 9 | WRONG_CANDIDATE/complex variant claim | State the variants separately; the summed 17 hits are reproducible, but a union-file count is not supplied by one `zc.count` call. |
| 作家 `Explanation` | 如何是作家 4 hits | 如何是作家 | 6 / 6 | CONFIRMED_STALE | “‘What is an adept?’ (如何是作家) has 6 hits in 6 files.” |
| 作家 `Explanation` | 不是作家 29 hits | 不是作家 | 28 / 25 | CONFIRMED_STALE | “‘Not an adept’ (不是作家) has 28 hits in 25 files.” |
| 作家 `Explanation` | 還有作家 13 hits | 還有作家 | 15 / 13 | CONFIRMED_STALE | “‘Is there still an adept…?’ (還有作家) has 15 hits in 13 files.” |
| 作家 `Note` | 2,711 / 329 “of 462” | 作家 | 2,744 / 331 | CONFIRMED_STALE | “Adept (作家) has 2,744 hits in 331 of the 462 allowlisted files.” |
| 大悟 `Explanation` | 大疑大悟 31 hits | 大疑大悟 | 33 / 28 | CONFIRMED_STALE | “‘Great doubt, great awakening’ (大疑大悟) has 33 hits in 28 files.” |
| 大悟 `Explanation` | 大悟底人 15 hits | 大悟底人 | 16 / 14 | CONFIRMED_STALE | “‘A person of great awakening’ (大悟底人) has 16 hits in 14 files.” |
| 大悟 `Explanation` | 2,595 / 303 “of 462”; auditor chose nearby 落花難上枝 | 大悟 | 2,711 / 308 | WRONG_CANDIDATE plus stale count | “Great awakening (大悟) has 2,711 hits in 308 of the 462 allowlisted files.” |
| 轉語 `Explanation` | 金佛不度爐 107 hits | 金佛不度爐 | 122 / 78 | CONFIRMED_STALE | “‘A gold buddha does not pass through a furnace’ (金佛不度爐) has 122 hits in 78 files.” |
| 轉語 `Note` | 轉語 1,376 / 261 “of 462” | 轉語 | 1,435 / 265 | CONFIRMED_STALE | “Turning word (轉語) has 1,435 hits in 265 of the 462 allowlisted files.” |
| 無事 `Explanation` | punctuation-variable chiasmus “across 13 texts” | 無事於心，無心於事; 無事於心。無心於事 | 13 / 12; 3 / 2 | WRONG_CANDIDATE/complex punctuation claim | State exact punctuation forms separately, or retain the 13-file union only with a documented union calculation; a single candidate 無心於事 (48 / 36) does not test the chiasmus. |

## b003 checkpoint

| Entry · field | Old claim | Actual cited phrase | Current `zc.count` | Classification | Proposed exact prose |
|---|---|---|---:|---|---|
| 無位真人 `Note` | proximity frame 舉臨濟…無位真人 14 / 11 | 舉臨濟無位真人; 喚作無位真人 | 13 / 10; 6 / 6 | WRONG_CANDIDATE/complex proximity claim | Replace with exact forms: “‘Raised: Linji's true person of no rank’ (舉臨濟無位真人) has 13 hits in 10 files; ‘called the true person of no rank’ (喚作無位真人) has 6 in 6.” |
| 向上一路 `Note` | 955 / 213 | 向上一路 | 957 / 213 | CONFIRMED_STALE | “The road above (向上一路) has 957 hits in 213 files.” |
| 向上一路 `Note` | optional-punctuation aggregate 向上一路…千聖不傳 338 / 149 | 向上一路千聖不傳; 向上一路，千聖不傳; 向上一路。千聖不傳 | 40 / 22; 284 / 126; 16 / 7 | WRONG_CANDIDATE/complex punctuation claim | Give exact forms separately. Their hits sum to 340; the old 149-file union cannot be inferred by adding file counts because files can overlap. |
| 君臣 `Explanation` | 如何是君 70 / 41 | 如何是君 | 226 / 52 | CONFIRMED_STALE | “‘What is the ruler?’ (如何是君) has 226 hits in 52 files.” Note that this substring also counts longer phrases beginning with those graphs. |
| 君臣 `Note` | 君臣 950 / 217 | 君臣 | 960 / 217 | CONFIRMED_STALE | “Ruler and minister (君臣) has 960 hits in 217 files.” |
| 無字 `Note` | 無字 782 / 174 “of 462” | 無字 | 801 / 173 | CONFIRMED_STALE | “The word ‘no’ (無字) has 801 raw hits in 173 of the 462 allowlisted files.” |
| 開悟 `Explanation` | 言下大悟 450 / 148 | 言下大悟 | 542 / 160 | CONFIRMED_STALE | “‘Greatly awakened at the words’ (言下大悟) has 542 hits in 160 files.” |
| 開悟 `Explanation`, `Note` | 豁然開悟 68 / 38 | 豁然開悟 | 73 / 39 | CONFIRMED_STALE | “‘Suddenly opened and awakened’ (豁然開悟) has 73 hits in 39 files.” Apply in both fields. |
| 本分事 `Explanation` | 本分草料 137 / 65 | 本分草料 | 153 / 72 | CONFIRMED_STALE | “‘Own-share fodder’ (本分草料) has 153 hits in 72 files.” |
| 本分事 `Explanation` | 如何是本分事 25 / 22 | 如何是本分事 | 34 / 28 | CONFIRMED_STALE | “‘What is the own-share matter?’ (如何是本分事) has 34 hits in 28 files.” |
| 本分事 `Explanation` | “~840 occurrences across ~184 files”; auditor chose nearby 分外 | 本分事 | 837 / 184 | APPROXIMATE explicitly marked, wrong candidate | The approximation is accurate. Exact prose: “Own-share matter (本分事) has 837 hits in 184 files.” |
| 本分事 `Note` | 本分 2,482 / 321 | 本分 | 2,558 / 321 | CONFIRMED_STALE | “Own share (本分) has 2,558 hits in 321 files.” |
| 下語 `Explanation` | 令眾下語 26 / 18 | 令眾下語 | 29 / 21 | CONFIRMED_STALE | “‘Order the assembly to lay down words’ (令眾下語) has 29 hits in 21 files.” |
| 下語 `Explanation` | 下語不契 168 / 62 | 下語不契 | 205 / 73 | CONFIRMED_STALE | “‘The laid-down words do not accord’ (下語不契) has 205 hits in 73 files.” |
| 下語 `Explanation` | 下一轉語 185 / 112 | 下一轉語 | 213 / 119 | CONFIRMED_STALE | “‘Lay down one turning word’ (下一轉語) has 213 hits in 119 files.” |
| 下語 `Explanation` | 代云 4,529 / 197 | 代云 | 4,316 / 194 | CONFIRMED_STALE | “‘In their stead, [he] said’ (代云) has 4,316 hits in 194 files.” |
| 下語 `Note` | 著語 398 / 123; auditor chose nearby 評唱 | 著語 | 327 / 120 | WRONG_CANDIDATE plus stale count | “‘Attached words’ (著語) has 327 hits in 120 files.” The candidate 評唱 (103 / 28) is not the counted phrase. |
| 無念 `Note` | 無念 810 / 143 “of 462” | 無念 | 815 / 143 | CONFIRMED_STALE | “No-thought (無念) has 815 hits in 143 of the 462 allowlisted files.” |
| 頓悟 `Explanation` | allowlist count 878 / 183 placed after a book title; auditor chose 頓悟入道要門論 | 頓悟 | 902 / 186 | WRONG_CANDIDATE plus stale count | “Sudden awakening (頓悟) has 902 hits in 186 files.” The title Essential Gate of Entering the Way by Sudden Awakening (頓悟入道要門論) itself has 11 / 9. |
| 疑情 `Note` | 疑情 936 / 208 “of 462” | 疑情 | 985 / 211 | CONFIRMED_STALE | “The doubt (疑情) has 985 hits in 211 of the 462 allowlisted files.” |

## b004 checkpoint

| Entry · field | Old claim | Actual cited phrase | Current `zc.count` | Classification | Proposed exact prose |
|---|---|---|---:|---|---|
| 庭前柏樹子 `Explanation` | answer within eight characters of 西來意: 256 / 111 | proximity claim; exact headword 庭前柏樹子 | headword 401 / 151 | WRONG_CANDIDATE/complex proximity claim | `zc.count(西來意)` cannot test the window. For exact prose use: “The garden cypress (庭前柏樹子) has 401 hits in 151 files”; retain the 8-character claim only after rerunning a defined window audit. |
| 教外別傳 `Explanation` | probe 別傳箇／個什麼 “13 files” | 別傳箇什麼; 別傳個什麼 | 11 / 11; 2 / 2 | WRONG_CANDIDATE/complex graph-variant claim | State the two exact forms separately. The auditor's candidate 個什麼 (504 / 90) is merely the tail, not the cited probe. Related 甚麼 forms are separate: 別傳箇甚麼 18 / 15 and 別傳個甚麼 8 / 7. |
| 教外別傳 `Explanation` | adjacent to 不立文字 48 / 42; reverse 38 / 31 | punctuation/order aggregate | 不立文字教外別傳 3 / 3; 不立文字，教外別傳 40 / 37; reverse 7 / 7 and 29 / 22 | WRONG_CANDIDATE/complex adjacency claim | List exact punctuation forms separately, or retain the aggregate only with a reproducible adjacency calculation. `zc.count(不立文字)` does not test adjacency. |
| 不立文字 `Explanation` | within ten characters of 直指人心: 140 / 87 | proximity aggregate | 不立文字直指人心 10 / 8; 不立文字，直指人心 92 / 64 | WRONG_CANDIDATE/complex proximity claim | Replace with exact forms above, or rerun and document the ten-character window calculation. |
| 不立文字 `Note` | followed within ten characters by 教外別傳 53 / 47; reverse 39 / 32 | proximity aggregate | exact principal forms: 3 / 3 and 40 / 37; reverse 7 / 7 and 29 / 22 | WRONG_CANDIDATE/complex proximity claim | Do not substitute the corpus-wide 教外別傳 count. List exact punctuation forms or document a separate window count. |
| 三玄三要 `Explanation` | 第一玄 178 / 110 | 第一玄 | 190 / 114 | CONFIRMED_STALE | “‘First mystery’ (第一玄) has 190 hits in 114 files.” |
| 三玄三要 `Explanation` | full three-item device list 9 / 7; auditor chose 照用一時行 | punctuation-variable list | comma form 3 / 3; full-stop form 1 / 1; unpunctuated 0 / 0 | WRONG_CANDIDATE/complex punctuation claim | State exact list forms separately, or retain 9 / 7 only with an explicit multi-punctuation union. The individual first member 三玄三要四料揀 has 7 / 5. |
| 百尺竿頭 `Explanation` | 不動人 8 / 8 | 不動人 | 17 / 16 | CONFIRMED_STALE | “‘Unmoving person’ (不動人) has 17 hits in 16 files.” |
| 百尺竿頭 `Explanation` | optional-punctuation question aggregate 88 / 58 | 百尺竿頭如何進步; comma; full stop | 72 / 51; 13 / 10; 3 / 1 | WRONG_CANDIDATE/complex punctuation claim | The hits sum exactly to 88, so the old aggregate is plausible; report the three exact forms separately unless a union-file calculation is retained for the 58 files. |
| 本地風光 `Explanation`, `Note` | 382 / 161; auditor chose nearby 風光 or 本來面目 | 本地風光 | 454 / 173 | WRONG_CANDIDATE plus stale count | “Native-ground scenery (本地風光) has 454 hits in 173 files.” Apply in both fields. |
| 本地風光 `Explanation` | 本地風光本來面目 4 / 2 | 本地風光本來面目 | 9 / 4 | CONFIRMED_STALE | “‘Native-ground scenery, original face’ (本地風光本來面目) has 9 hits in 4 files.” |
| 本地風光 `Explanation` | 蹋著本地風光 23 / 13 | 蹋著本地風光; 踏著本地風光 | 11 / 4; 22 / 16 | WRONG_CANDIDATE/graph-variant aggregate | State separately: the 蹋 graph has 11 / 4 and the 踏 graph has 22 / 16. The old value matches neither exact form. |
| 直指人心 `Explanation` | paired with 見性成佛 in 470 of 624 hits / 190 files | proximity/co-occurrence; exact 直指人心見性成佛 | exact contiguous 63 / 29 | WRONG_CANDIDATE/complex proximity claim | “The unpunctuated four-phrase segment ‘directly point at the human mind, see nature and become buddha’ (直指人心見性成佛) has 63 hits in 29 files.” Retain 470 / 190 only after a defined proximity rerun. |
| 直指人心 `Explanation` | preceded by 不立文字 118 / 79 | proximity aggregate | 不立文字直指人心 10 / 8; 不立文字，直指人心 92 / 64 | WRONG_CANDIDATE/complex proximity claim | List the two exact forms, or document the wider punctuation/window calculation. |
| 麻三斤 `Explanation` | 麻三斤 490 / 174 | 麻三斤 | 531 / 175 | CONFIRMED_STALE | “Three pounds of hemp (麻三斤) has 531 hits in 175 files.” |
| 麻三斤 `Note` | 洞山麻三斤 76 / 54 | 洞山麻三斤 | 84 / 59 | CONFIRMED_STALE | “‘Dongshan's three pounds of hemp’ (洞山麻三斤) has 84 hits in 59 files.” |
| 麻三斤 `Note` | split notation (貼秤麻)(三斤) 12 / 10; auditor chose 三斤 | 貼秤麻三斤 | 11 / 9 | WRONG_CANDIDATE plus stale count | “‘Three pounds of scale-weight hemp’ (貼秤麻三斤) has 11 hits in 9 files.” |
| 截斷眾流 S1 `Explanation` | followed within 30 characters by 隨波逐浪 151 / 66; reverse 59 / 39 | proximity aggregate | head phrases: 截斷眾流 370 / 144; 隨波逐浪 360 / 136. Exact comma-linked forms: 5 / 5 and reverse 1 / 1 | WRONG_CANDIDATE/complex proximity claim | Do not replace the window claim with either headword count. Exact alternative: “‘Cut off the many streams, follow waves and chase swells’ (截斷眾流，隨波逐浪) has 5 hits in 5 files; the reverse comma form has 1 in 1.” |

## b005 checkpoint

| Entry · field | Old claim | Actual cited phrase | Current `zc.count` | Classification | Proposed exact prose |
|---|---|---|---:|---|---|
| 兼中到 `Note` | 276 / 84 | 兼中到 | 275 / 84 | CONFIRMED_STALE | “Arrival within both (兼中到) has 275 hits in 84 files.” |
| 兼中至 `Explanation` | 兼中至 207 / 64 | 兼中至 | 206 / 64 | CONFIRMED_STALE | “Arrival from both (兼中至) has 206 hits in 64 files.” |
| 兼中至 `Explanation` | neighboring fifth rank 兼中到 276 / 84 | 兼中到 | 275 / 84 | CONFIRMED_STALE | “The fifth rank, arrival within both (兼中到), has 275 hits in 84 files.” |
| 正中來 `Note` | 288 / 85 | 正中來 | 287 / 85 | CONFIRMED_STALE | “Coming from within the upright (正中來) has 287 hits in 85 files.” |
| 四賓主 `Explanation` | “0 occurrences in the primary Linji Record (T47n1985)”; auditor chose nearby title 臨濟錄 and found 1 / 1 corpus-wide | file-scoped 四賓主 claim | corpus-wide 四賓主: 143 / 78; file-scoped T47n1985 main text: 0 | WRONG_CANDIDATE/file-scoped claim | “The compound ‘four guests and hosts’ (四賓主) has 143 hits in 78 allowlisted files, but zero in the main text of T47n1985.” The old zero is not disproved by `zc.count(臨濟錄)`; it requires the preserved file-scoped check. |

## Coverage and disposition

- b001: all 32 flagged audit claims covered in the b001 table.
- b002: all 43 flagged audit claims covered in the b002 table.
- b003: all 34 flagged audit claims covered in the b003 table.
- b004: all 23 flagged audit claims covered in the b004 table.
- b005: all 5 flagged audit claims covered in the b005 table.
- Total: 137/137 flagged claims triaged. Hit/file claims sharing one prose count statement were consolidated into one row.

The dominant defect is genuine count drift. The important exceptions are the corpus-size denominator false positives and claims defined by proximity, punctuation unions, graph-variant unions, or a single-file absence. Those must not be mechanically replaced by the count of whichever nearby phrase the audit guessed.

