# Final read-only complete-case review: 鳥道, 玄路, 金, 銀, 銀彈子, 本來面目

Date: 2026-07-13

Scope: current `entry.v2.json` files for the six calibration entries, reviewed after the latest repair passes. This audit did not edit any entry and did not merge. It verified every stored KWIC, reconciled speaker/actor identity against widened source context and section/book ownership, checked `primary` / `family` / `contrast` assignments, and retested each opening inference against the complete cases.

Mechanical result: 84/84 stored occurrences pass `zc.verify`; no KWIC or line-boundary drift. One exact duplicate remains in 金. Semantic/attribution result: 鳥道, 玄路, and 銀彈子 are clean; 金, 銀, and 本來面目 require the targeted corrections below.

## Executive findings

### Definite corrections required

1. **金 s1 o7 names the wrong Fayan.** The source is `T/T47/T47n1995.xml`, titled `法演禪師語錄`; widened context is the `舒州白雲山海會演和尚語錄` record and the speaker calls himself `白雲`. The line `師云。金屑雖貴落眼成翳` is Wuzu Fayan's, not Fayan Wenyi's. Replace `MasterName: Fayan Wenyi` with roster-exact `Wuzu Fayan`, and propagate the correction through the Explanation, AttributionNote, and RelatedMasters.

2. **金 s1 o15 and o16 are the same witness stored twice.** Both are `C/C078/C078n1720.xml`, `0674a24–0674b02`, with identical KWIC and Zhengtang Bian attribution. Keep one actor-pure no-headword `contrast` occurrence and remove the duplicate. This is not two independent comments.

3. **金 s1 o9 has regressed from `family` to `contrast`.** Its exact lexical object is the longer `黃金` / `白銀` pair. Under the nested-compound gate, it is family evidence for bare 金; unlike the gold-and-shit row, it is not functioning as a contradiction to the opening inference. Restore `EvidenceRole: family`. After fixing o9 and the duplicate, the honest 金 role count is **20 total = 10 primary, 8 family, 2 contrast**. Sense 1 is 18 total = 8 primary, 8 family, 2 contrast; the Metal sense remains 2 primary.

4. **銀 s1 o6 is assigned to the wrong person.** In `X/X82/X82n1571.xml` at `0508a17–18`, the nearest stale head points into the preceding Dabian material, but the complete case explicitly begins `錢塘接待法鐘覺禪師`. It is Fazhong Jue's opening-of-the-furnace hall address: `入我大冶爐中鎔化過，無論金銀銅鐵錫...`. It is not a saying of the nun Yizhen En. Replace the MasterName and all dependent prose with source-attested **Fazhong Jue (法鐘覺)**, marked non-roster if he is still absent from the roster.

5. **銀 s1 o23 is assigned to the wrong person.** In `X/X85/X85n1591.xml` at `0239b19`, complete context is explicitly the section `安籠玉泉月幢了禪師`. The hall speaker says `門外春水白如銀`. It is **Yuedang Liao (月幢了)**, not Tiemei Zhen. Replace the MasterName and AttributionNote, and remove Tiemei Zhen from RelatedMasters unless another retained witness supports him.

6. **本來面目 s1 o8–o9 misidentify both sides of the exchange.** The complete autobiographical case in `J/J28/J28nB210.xml` says the future Qiyuan went to `金粟參石車和尚`. Thereafter `粟問` means the master at Jinsu, **Shiche Tongcheng (石車通乘)**; it does not mean Doushuai Huocun. The respondent denoted by `師` is the subject of the record, roster-exact **Qiyuan Xinggang (祇園行剛)**, not “Fushi Qiyuan” (which conflates the Crouching Lion monastery name with her personal name). Thus o8 should name Shiche Tongcheng and o9 Qiyuan Xinggang. Rewrite the Explanation, both AttributionNotes, book rendering, and RelatedMasters accordingly. The actor-pure role split itself is correct: Shiche's headword question is `primary`; Qiyuan's no-headword answer `眉橫鼻直` is `contrast` support.

### No correction required

- **鳥道:** all ten actors reconcile. In particular, the Three Roads line in Fenyang's record is reported Dongshan speech, not Fenyang's substitute answer; the memorial-line narrator is Shen Xun, while Xingqin is the traveler and Mizang the person sought; Fuchuan is not involved here. Roles remain **9 primary, 1 contrast**, split as seven trackless-flight primary witnesses plus one headword-free Tianyi comparison, and two concrete mountain-trail primary witnesses.
- **玄路:** all nine actors reconcile. Dongshan owns the Three Roads statement; Fenyang, Hongzhi, Xisou, Xueguan, Touzi, and Shanfeng each own their stored turns. Caoshan and Yongjue use the longer `金鎖玄路` compound and are correctly `family`. Roles remain **7 primary, 2 family**.
- **銀彈子:** Nanquan owns the exact barter exchange; Zhengtang owns the appended no-headword price verse; Tian'an and Wuyi independently own the iron-palm kneading line. Roles remain **3 primary, 1 contrast**. Parallel transmissions inventoried but not stored do not create extra depth.
- **本來面目 other rows:** Huineng is the speaker behind `盧曰`; Helin asks Muyun and Muyun raises the fist; Nanyue owns both his instruction and later reply while the challenge is anonymous; Baichi owns his rebuke; Dongshan owns `不行鳥道`; and the Fuchuan section is explicitly `福州覆船山洪薦禪師`, so Fuchuan Hongjian is correct there.

## Opening-inference audit

### 鳥道 — KEEP

The opening inference is both useful and properly scoped. `鳥道無蹤`, `經行鳥道沒蹤由`, and the attributable goose-crossing-sky comparison support a flight-course that leaves no fixed trace. The complete Dongshan case explicitly says one travels it, then distinguishes it from the original face; therefore “trackless course” is licensed, while “an impossible route nobody can follow” would not be. The second sense is genuinely a different thing: memorial and travel prose put people on dangerous rope-bridge / sheep-gut / bird trails across named mountains. The two-sense split is mandatory and correct.

### 玄路 — KEEP

The opening correctly makes this a named Caodong road/course rather than a physically dark road or a universal synonym for “the Way.” Dongshan's Three Roads is exact; later predicates independently say it turns, can be penetrated or crossed, can be opened, and can still be only halfway. “Hidden road” is a minimal ordinary bridge for the compound, and dark/mysterious road/path/way/route belong as search aliases rather than additional displayed senses. The golden-lock compound remains correctly local family evidence.

### 金 — KEEP SEMANTICS; REPAIR ATTRIBUTION/ROLES

The opening's central conclusion survives complete-case review. Shenhui explicitly quotes gold as a comparison for the nature of reality; Yongjue explicitly maps refining away ore to clarifying mind; Tiantai Deshao explicitly uses “like gold with gold” for equal, unmixed positions. These support local purity, intrinsic-nature, equality, and awakening-side deployments. Their simile/equation grammar does **not** support a second lexical sense “enlightenment” for every bare 金. Rahulata, Huiyuan, monetary, color, gold-and-shit, gold-dust, and gold-buddha controls correctly prevent that universalization. Replace Wuzu Fayan's name, deduplicate Zhengtang, and restore the Nanquan yellow-gold row to family; do not weaken or globalize the opening inference.

### 銀 — KEEP SEMANTICS; REPAIR TWO ATTRIBUTIONS

The one-sense decision is sound: metal, commodity/money, and “white like silver” comparison retain the same ordinary silver referent. Assay, price, monastery accounting, casting, and color similes are independently attested. The entry also correctly refuses to project one secret brightness or rank theory across silver bowl, silver mountain, silver cage, white-silver world, and silver pellet. The Nanquan/Zhengtang material licenses only a case-local value ranking. Correct Fazhong Jue and Yuedang Liao; those name repairs do not change the definition.

Current role count is **28 total = 11 primary, 15 family, 2 contrast**. Nanquan's `銀彈子` row could defensibly be called family because it is a nested compound, but its present `contrast` role is semantically intelligible as the local ranking countercase and does not buy bare-silver depth. This is a consistency choice, not a hard semantic error; if strict longest-match role uniformity is desired, change it to family and retain Zhengtang alone as contrast.

### 銀彈子 — KEEP

The opening earns the local inference that Nanquan places the requested Chan explanation on the gold-pellet side and the offered scripture lecture on the silver-pellet side. The barter direction plus Zhengtang's price verse licenses a local value ranking. Tian'an and Wuyi's independent kneading lines block a universal equation of silver pellet with inferior realization. The caution about unknown material construction is appropriate: the sources picture a tradable or kneadable pellet but do not self-gloss whether it is solid silver, silver-colored, or conventionally named.

### 本來面目 — KEEP SEMANTICS; REPAIR THE SHICHE/QIYUAN CASE

The opening properly treats original face as a recurring question about what is originally one's own, while refusing to turn one answer into a universal gloss. Tianru says each person has one and has not recognized it; Huanglong says to recognize it; Huineng and other cases place it before evaluative thought or before one's parents were born. The Dongshan complete case explicitly distinguishes traveling the bird course from the original face and then answers `不行鳥道`; storing the answer as no-headword support rather than a definition is correct. Only the Shiche/Qiyuan names and dependent prose require repair.

Current role count remains **12 total = 7 primary, 5 contrast** after the name correction.

## Recommended repair order

1. Fix the five wrong MasterName assignments: Wuzu Fayan; Fazhong Jue; Yuedang Liao; Shiche Tongcheng; Qiyuan Xinggang.
2. Propagate each correction through Explanation, AttributionNote, SourceTexts/RelatedMasters as applicable.
3. Delete one of the two identical Zhengtang rows in 金.
4. Restore 金's yellow-gold / white-silver Nanquan row to `family`.
5. Rerun `zc_batch.py verify-entries`, attribution gate, depth/sense gate, duplicate-occurrence check, and the six-entry cohort gate.
6. Do not merge as part of this audit; merge only after the coordinating repair pass accepts and applies these findings.
