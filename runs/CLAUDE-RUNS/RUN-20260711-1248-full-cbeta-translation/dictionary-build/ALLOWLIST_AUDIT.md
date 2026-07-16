# Allowlist audit — `zen-corpus.json` (462 texts) vs the full CBETA (4,990 texts)

**Date:** 2026-07-14
**Trigger:** an external check — the Iriya & Koga (*Zengo jiten*, 1991) headword list contained Chan
sayings our corpus could not find. That led here.

**Two methods were used:**
1. **Title-signal screen** over all 4,990 texts (語錄 / 廣錄 / 普說 / 燈錄 / 頌古 / 拈古 / 評唱 / 擊節 /
   公案 / 宗門 / 祖堂 / 僧寶 / 禪林 …). Found 51 excluded.
2. **Structural screen by canon zone** — T47–48 (諸宗部), X63–87 (禪宗部), J (嘉興藏). This catches Chan
   records whose titles carry no genre keyword (信心銘, 證道歌, 十牛圖, 心要 …). **Found 288 excluded.**

Method 2 is authoritative. Method 1 was badly incomplete.

---

## ⚠️ 1. DEFECT — the multi-source gate counts FILES, not WORKS

**Four works already occupy eight allowlist slots.**

| work | allowlist slots | note |
|---|---|---|
| **五燈全書** | `X81n1571`, `X82n1571` | **one work, split across two volumes** |
| **五燈嚴統** | `X80n1568`, `X81n1568` | **one work, split across two volumes** |
| 古尊宿語錄 | `C077n1710`, `D48n8939` | two canon editions of one work |
| 五家語錄 | `J23nB134`, `X69n1326` | two canon editions of one work |

**Why it matters.** The gate promotes a sense to `multi-source` when it holds across **≥2 independent
texts**. A sense attested *only* in 五燈全書 appears in **two** allowlist "texts" and is promoted — **but it
has exactly one independent source.** Any sense resting on a pair from this table is **falsely validated**,
and `multi-source` is the project's central quality claim.

**Fix:** add `work_id` to the corpus manifest; make the gate count **distinct works**. Re-grade every
`multi-source` verdict whose evidence rests on a duplicate pair. Small (0.9% of the corpus), but this is a
**correctness bug**, not cosmetics. **Do this before adding any texts** — otherwise the same trap is
re-created by 宗門統要正續集 (`P154n1519` + `P155n1519`) and 雪嶠信禪師語錄 (`L153n1638` + `L154n1638`),
both of which are one work in two files.

---

## 2. COVERAGE — the Chan sections of CBETA

| zone | texts | in allowlist | excluded | coverage |
|---|---|---|---|---|
| J (嘉興藏, mostly Chan) | 287 | 234 | 53 | 81.5% |
| T47–48 (諸宗部) | 77 | 48 | 29 | **62.3%** — all 29 exclusions are Pure Land (淨土/念佛/安樂集). Correct. |
| **X63–87 (禪宗部)** | **370** | **164** | **206** | **44.3%** |
| **total** | **734** | **446** | **288** | **60.8%** |

**X63–87 is the Chan section of the Xuzangjing and we hold 44% of it.** Of the 288 exclusions, ~157 are
correctly barred by the scope rule (Pure Land, ritual, bios, chronicles, lexicons, 清規, other schools).
**The rest are the gap.**

---

## 3. TIER 1 — unambiguous Chan records, MISSING. Add these. (23 texts, ~20 MB)

| CBETA id | work | size | why it belongs |
|---|---|---|---|
| `X66n1297` | **宗鑑法林** | 4.7 MB | Large Chan case-and-verse compendium. |
| `X83n1578` | **指月錄** | 3.9 MB | Major Ming Chan case collection; a standard reference. |
| `X84n1580` | **教外別傳** | 2.3 MB | *"A Special Transmission Outside the Teachings."* The Four Statements are the scope's own boundary condition — and the book named for them is not in the corpus. |
| `X84n1579` | **續指月錄** | 1.8 MB | Continuation of 指月錄. |
| `X85n1592` | **揞黑豆集** | 1.2 MB | Chan lamp/record collection. |
| `X85n1587` | **正源略集** | 1.1 MB | Chan lineage record. (+ `X85n1588` 補遺) |
| `M59n1540` | **大慧普覺禪師普說** | 1.0 MB | **Dahui's formal discourses.** His 語錄 (`T47n1998A`) is in; his 普說 is not. |
| `X67n1309` | **正法眼藏** | 792 KB | **Dahui's** 正法眼藏 (Chinese, not Dōgen's). Core case collection. |
| `X79n1563` | **大光明藏** | 769 KB | Chan case collection. |
| `X65n1283` | **宗範** | 661 KB | Chan-school compilation. |
| `X67n1310` | **拈八方珠玉集** | 590 KB | 拈古 collection — core genre. |
| `X78n1554` | **五家正宗贊** | 574 KB | Five Houses lineage record. |
| `X67n1308` | **徑石滴乳集** | 526 KB | 頌古/拈古 collection. |
| `X87n1620` | **先覺宗乘** | 406 KB | Chan record collection. (+ `X87n1619` 先覺集) |
| `X87n1624` | **林間錄** | 357 KB | **Huihong.** Core Chan miscellany. (+ `X87n1625` 後集) |
| `X67n1301` | **佛果擊節錄** | 350 KB | **Yuanwu's *Jijie lu*** — case commentary by the author of the *Blue Cliff Record*. |
| `X63n1235` | **智證傳** | 268 KB | Huihong. Chan. |
| `X83n1577` | **羅湖野錄** | 250 KB | Chan miscellany, widely cited. |
| `X67n1306` | 焭絕老人天奇直註天童覺和尚頌古 | 208 KB | 頌古 commentary on Hongzhi. |
| `X67n1302` | 焭絕老人天奇直註雪竇顯和尚頌古 | 186 KB | 頌古 commentary on Xuedou. |
| `X86n1601` | 禪燈世譜 | 783 KB | Chan lamp genealogy. |
| `P154n1519` + `P155n1519` | **宗門統要正續集** | 1.4 MB | Chan case compilation. **One work, two files — see §1.** |
| `L153n1637` … `L158n1652` | **8 Ming–Qing Linji 語錄** | ~5.4 MB | 弘覺忞 / 天隱修 / 明覺聰 / 幻有傳 / 密雲悟 / 雪嶠信 / 山茨際 / 明道正覺森. **The entire `L` (乾隆藏) series of Chan 語錄 is absent** — this looks like a whole canon was skipped when `ZEN_TEXT_WORKLIST.md` was built, not eight separate judgements. Worth finding out how. |

---

## 4. TIER 2 — Chan-school doctrine & miscellany. Probably in. **Your call.**

`X63n1234` 臨濟宗旨 · `X65n1279` 五宗原 · `X65n1282` 五家宗旨纂要 · `X63n1236` 曹洞五位顯訣 ·
`X63n1237` 寶鏡三昧本義 · `X64n1270` 十牛圖頌 · `X63n1231` 心賦注 (Yongming Yanshou) ·
`X65n1280` 闢妄救略說 · `X65n1281` 御製揀魔辨異錄 (Yongzheng's Chan polemic) ·
`X67n1312` 通玄百問 · `X67n1313` 青州百問 ·
`X86n1610` 雲臥紀譚 · `X86n1611` 叢林盛事 · `X87n1613` 枯崖漫錄 · `X87n1616` 山菴雜錄 ·
`J26nB181` 布水臺集 · `J31nB267/268` 牧雲和尚 · `J29nB230/231` 天王水鑑海和尚 ·
`J25nB167` 徹庸和尚谷響集 · `J28nB207` 古瓶山牧道者究心錄

Also the 證道歌 and 溈山警策 commentary clusters (`X63n1239/1240/1241`, `X65n1291/1292/1293/1294`) —
commentaries on Chan verse texts. In or out is a genuine scope decision.

---

## 5. CORRECTLY EXCLUDED — leave alone

Barred by the scope rule (*"minus Pure Land / Vinaya / Tiantai / Huayan / sutras / stele inscriptions /
eminent-monk bios / chronicles / lexicons / anthologies"*):

- **Lexicons:** `X64n1261` **祖庭事苑** (1108 — the Song Chan glossary, i.e. this dictionary's own historical
  ancestor; excluded *by rule*, and correctly — but read it), `B19n0103` 禪林象器箋, `X87n1618` 祖庭指南
- **Eminent-monk bios:** `X79n1560/1561/1562` 僧寶傳 family, `X87n1626` 高僧摘要, `X78n1543` 東林十八高賢傳,
  `X86n1598` 曹溪大師別傳, `X86n1597` 布袋和尚傳
- **Ritual/liturgy:** `X74n1465/1480/1481/1483/1491/1492/1493` (三時繫念儀範, 大悲心咒行法, 准提/藥師三昧行法,
  禮舍利塔儀式, 禮佛儀式, 供諸天科儀)
- **Pure Land:** all 29 T47 exclusions; `X78n1547/1551/1552`
- **Tiantai:** `T46n1933` 南嶽思大禪師立誓願文 (Huisi), `X77n1535` 智者大師別傳註
- **Esoteric:** `X88n1654` 惠果和尚行狀 (Huiguo — Kūkai's teacher)
- **清規:** `X63n1244` 百丈清規證義記, `X63n1246/1247/1251` 入眾日用/須知, 叢林兩序須知
- **Chronicles:** `X75n1515` 續佛祖統紀, 年譜 (`J01nA042`, `J29nB245`, `B22n0119`)
- **Anthologies/commentary:** `X64n1262/1263/1264/1265/1266` 禪林寶訓 family
- **Stele:** `I01n0089` 造像記
- **Modern:** `Y40n0038` 中國禪宗史, `B25n0142` 神會和尚遺著 (critical edition)

**Your call, leaning exclude:** `X62n1182` 徹悟禪師語錄 (徹悟際醒 = **12th Pure Land patriarch**, titled 禪師);
`X62n1179` 省菴法師語錄 (**11th Pure Land patriarch**, 法師); `J33nB279` 虛舟禪師註八識規矩頌 (Chan author,
**Yogācāra** content).

---

## 6. DO NOT ADD — duplicate editions of texts already in

`X68n1315` 古尊宿語錄 (in as `C077n1710`, `D48n8939`) · `X65n1295` 禪宗頌古聯珠通集 (in as `C078n1720`) ·
`J29nB241` 慶忠鐵壁機禪師語錄 (in as `J29nB240`) · `X71n1424` 投子義青禪師語錄 (in as `X71n1423`)

Adding these deepens the §1 defect.

---

## Recommended order of work

1. **Fix the gate (§1).** Count works, not files. Re-grade affected verdicts. *This is the correctness item.*
2. **Add Tier 1 (§3)** — 23 texts, ~20 MB, ~+5% corpus.
3. **Re-run counts on all 641 built entries.** Some `provisional` senses will legitimately become
   `multi-source`; some counts are understated today.
4. Decide Tier 2 (§4) and the three borderline texts in §5.
5. **Find out how the `L` series got skipped** — a whole canon absent is a process failure, not a judgement.

---

## Caveats — read before acting

- **Classification is by title.** It will miss a Chan record with an idiosyncratic title and may over-flag.
  **Open the text before adding it.** A title is a hypothesis, not evidence — the same rule the dictionary
  lives by.
- The ~90 remaining items among the 288 are noise my regex failed to bar (rituals, bios, Pure Land,
  Tiantai, 清規). They are correctly excluded.
- **Adding texts changes every count in the dictionary.** Counts, `files`, and multi-source verdicts are all
  computed over the allowlist. This is not a cheap change; it is a re-baseline.
