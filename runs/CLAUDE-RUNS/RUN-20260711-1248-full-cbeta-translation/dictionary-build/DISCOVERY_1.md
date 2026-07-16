# DISCOVERY_1 — Legendary / historical PEOPLE used as Zen examples

**Category (flyswatter #0g):** named non-master figures the Chan records invoke as stock teaching
*examples* — legendary laymen, immortals, recluses, everyman names, cultural allusion-figures — whom
ordinary Chinese wouldn't specially mark but Zen LOADS with a teaching sense.

**Method:** brainstormed ~130 figures → `zc.count` (allowlist-scoped) → read `zc.find` contexts to confirm
a real Zen deviation. **Deduped** against the 263 existing `termbase.v2.json` headwords, against
`REQUESTED_TERMS.md`, and against the **301-name master roster** (`Assets/Data/master-dates.json`) — any
figure on the roster was DROPPED.

**Dropped as roster masters:** 龐居士 / 龐蘊 (Layman Pang), 靈照 (his daughter Lingzhao), 靈雲, 俱胝, 丹霞.
**Dropped as already-requested:** 廣額 / 屠兒 (the butcher Guang'e), 商山四皓 / 四皓 / 紫芝, 鵝王.
**Dropped as plain scriptural reference / lineage patriarch (no Zen deviation beyond ordinary Buddhism):**
世尊 (8093), 釋迦 (6608), 瞿曇 (1609), 迦葉 (3674), 大迦葉 (209), 阿難 (1896), 善財 (1572), 龍女 (277).
**Dropped for heavy literal contamination:** 李白 (mostly 桃紅**李白** = "plums are white", not the poet);
布袋 alone (mostly 布袋 = literal "cloth sack" — use 布袋和尚 for the monk); 太公 (mostly 齊太公 historical);
鵬 (collides with master names 雲葢**鵬**禪師); 老子 (in Zen often = "this old man / I", not Laozi).

**Note (`寒山` roster check):** the roster's only "Hanshan" is 憨山德清 (Ming master) — DIFFERENT characters
from the poet 寒山. The poet is safe.

---

## Ranked candidate list (by lexicographic value)

| # | Headword | Literal English | Zen deviation / repurposing | Freq (hits/files) | Attesting phrase |
|---|----------|-----------------|------------------------------|-------------------|------------------|
| 1 | **傅大士** | Fu Dashi (Great Being Fu) | Legendary lay bodhisattva-teacher held up as the archetypal enlightened householder; author of the 心王銘 "Mind-King Inscription" masters quote as authority; paired with Vimalakirti as the lay standard. Variant **善慧大士** (65/26). | 551 / 173 | 不作維摩詰，又似傅大士 |
| 2 | **維摩詰** | Vimalakirti | The archetypal enlightened LAYMAN who out-argues the bodhisattvas and answers non-duality with silence — a householder besting the monastics. Variant **淨名** ("Pure Name", 673/184); bare 維摩 = 2017/313. | 263 / 116 | 一言勘破維摩詰／維摩默然 |
| 3 | **寒山** | Cold Mountain (Hanshan) | The mad Guoqing recluse-poet, emblem of eccentric unbound enlightenment; invoked as a pair with Shide (see #4). | 1453 / 271 | 寒山逢拾得，兩箇一時癡 |
| 4 | **拾得** | Shide ("the Foundling") | Hanshan's partner; the laughing-hand-clapping free man (放行則拾得搖頭，寒山拊掌). Pairs with #3. | 1338 / 268 | 放行則拾得搖頭，寒山拊掌 |
| 5 | **布袋和尚** | the hemp-sack monk (Budai) | The fat wandering monk read as Maitreya's incarnation — emblem of carefree plenitude who "puts down the sack". (Use this form: bare 布袋 1738 is mostly the literal cloth sack.) | 200 / 105 | 因號布袋和尚再來 |
| 6 | **誌公** | Reverend Zhi (Baozhi) | Liang-dynasty thaumaturge-poet quoted as authority (誌公云…), author of the 十二時歌/十四科頌 sung in the records. Variants 志公 (136/43), 寶誌 (83/50). | 494 / 133 | 故誌公云：內外追尋覔總無 |
| 7 | **張公喫酒李公醉** | "Zhang drinks the wine, Li gets drunk" | Everyman-proverb deployed as a Zen non-answer answer (e.g. to 木佛因何院主墮眉鬚) — cause and effect landing on the wrong man. Pure flyswatter everyman. | 125 / 84 | 師曰：張公喫酒李公醉 |
| 8 | **張三李四** | "Zhang-three, Li-four" (any Tom/Dick) | Generic everyman names given as the answer to "what is the student's own self?" — the self is just anybody. | 80 / 41 | 僧問…學人自己。師曰：張三李四 |
| 9 | **莊周** | Zhuangzi (Zhuang Zhou) | The butterfly-dream sage; 莊周蝶夢 = the illusoriness of fixed identity; also 三千劍客獨許莊周. Variant 莊生 (93/37). | 168 / 93 | 羸得莊周蝶夢長 |
| 10 | **婆子** | the old woman [of the cases] | The recurring anonymous crones who out-Zen the masters (趙州勘婆子, 婆子燒庵, Deshan's rice-cake woman). A stock teaching-character. (Relate to termbase 老婆 "grandmotherly".) | 1597 / 252 | 趙州勘婆子話…趙州若不勘破，婆子一生受屈 |
| 11 | **邯鄲** | Handan [the Handan walk] | 邯鄲學步 — the man who went to learn Handan's elegant gait and forgot his own; masters' image for imitators who lose themselves (者僧不是邯鄲人，為甚麼學唐步). | 104 / 56 | 邯鄲學唐步…者僧不是邯鄲人 |
| 12 | **卞和** | Bian He | Offered the genuine uncut jade thrice and had both feet cut off (卞和三獻玉，刖却一雙足) — unrecognized true worth / the price of authenticity. | 78 / 47 | 堪笑卞和三獻玉，縱榮刖却一雙足 |
| 13 | **許由** | Xu You | Refused the empire when Yao offered it and washed his ears clean of the offer — emblem of refusing rank and power (pairs with 巢父 40/29). | 75 / 37 | 堯…要以天下讓許由；許由…恐天下所累 |
| 14 | **南柯** | the Southern Bough [dream] | 南柯一夢 — a whole lifetime's glory lived out in an ant-kingdom dream; life as illusory. Sibling of 黃粱 (#17). | 59 / 44 | 喚醒南柯夢裏人／今日看來總是南柯一夢 |
| 15 | **漁父** | the fisherman[-recluse] | The untethered fisherman-recluse (漁父笛, 漁父詞, 漁父之勇) — freedom outside office and doctrine. | 426 / 167 | 且將漁父笛，閑向海邊吹 |
| 16 | **孔子** | Confucius | The sage cited and quietly deflated as a foil (孔子曾呈第一機; 願學孔子) — the classical sage measured against the Chan standard. Variant 仲尼 (268/120). | 592 / 112 | 孔子曾呈第一機 |
| 17 | **堯舜** | Yao and Shun | The sage-kings as the image of effortless ideal rule, routinely deflated (真天子不假堯舜敕文; 天子假敕堯舜無). | 383 / 146 | 真天子不假堯舜敕文 |
| 18 | **黃粱** | the yellow-millet [dream] | 黃粱夢 — Lu Sheng's lifetime of glory dreamed while the millet cooked at Handan; illusory worldly attainment. Sibling of 南柯. | 49 / 39 | 覺來黃粱炊尚未熟 |
| 19 | **鍾馗** | Zhong Kui (the demon-queller) | The legendary demon-catcher repurposed in Chan wordplay/appraisal (鍾馗小妹, 鍾馗解舞, 鍾馗嚇你). | 70 / 43 | 如何是主中賓？師云：鍾馗小妹 |
| 20 | **呂洞賓** | Lü Dongbin (the immortal) | The Daoist immortal who rejects impermanent alchemy ("五百年後依舊是石") — foil for Daoist immortality vs the Chan standard. Style-name variant 純陽 (25/18). | 20 / 14 | 昔日呂純陽投鍾離學仙 |

---

## Adjacent / optional (flagged, NOT counted in the 20 — belong to a different sub-category)

These carry huge Zen load but are **impossible-actor type-figures / images / objects**, not named
legendary people — better suited to a "stock impossible-figure" or "discernment-object" category:

- **石女** (the stone woman) 1114/233 & **木人** (the wooden man) 940/231 — the impossible non-dual actor
  (石女生兒, 木人吹笛, 石女弄琵琶). Extremely high value; recommend a dedicated pair-entry elsewhere.
- **牧童** (the herdboy) 334/139 — the oxherding-pictures boy (普明禪師頌) = the self/practitioner-image.
- **爛柯** (the rotted axe-handle) 47/33 — the woodcutter Wang Zhi who watched immortals and returned to
  find generations gone; time-collapse allusion. Borderline person; could be a #14/#18-style allusion entry.
- **秦鏡** (the mirror of Qin) 29/25 & **驪龍** (the black dragon's pearl) 364/152 — discernment/appraisal
  OBJECTS (當軒秦鏡絕狐蹤; 驪龍頷下奪明珠), not people.

## Feedback for coordinator
- **紫芝** (already requested for 商山四皓/紫芝歌) is contaminated by a monk's dharma-name usage in the
  corpus (示**紫芝**禪者 = "instructing the practitioner Zizhi"; **紫芝**法孫). The 廣額/allusion-vs-literal
  disambiguation warning applies to 紫芝 too — worth a note when that entry is authored.
