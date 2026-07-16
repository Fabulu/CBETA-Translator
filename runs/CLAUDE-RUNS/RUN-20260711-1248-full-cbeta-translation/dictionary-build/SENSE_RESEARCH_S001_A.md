# s001-A sense-repair research (read-only)

Date: 2026-07-13  
Terms: 血脈, 評唱, 著語, 君臣, 鐵牛, 本心  
Entry edits: none

All 42 proposed occurrence strings—retained old evidence and new candidates—were independently run through `zc.verify` with `PYTHONIOENCODING=utf-8`; all returned `ok: true`. The line spans below are verifier results. Floors come from the current 606-entry depth gate and remain floors, not targets.

## Proposed structures and depth

| Term | Hits / floor | Proposed senses | Target occurrences |
|---|---:|---:|---:|
| 血脈 | 174 / 6 | 3: Chan connective line; hereditary bloodline; bodily blood vessels/circulation | 7 = 5 + 1 + 1 |
| 評唱 | 103 / 6 | 2: appraise/comment; appraisal-commentary/collection | 6 = 4 + 2 |
| 著語 | 327 / 6 | 2: attach a comment; attached comment | 7 = 5 + 2 |
| 君臣 | 960 / 7 | 2: Caodong lord-minister configuration; ordinary lord and minister | 8 = 6 + 2 |
| 鐵牛 | 1,583 / 7 | 2: Shaanfu iron-ox landmark; iron-ox figure/comparison | 7 = 2 + 5 |
| 本心 | 748 / 7 | 2: original mind; original intention | 7 = 5 + 2 |

## 血脈 (`t_21f09b3726e7`)

### Proposed senses

1. **connective/transmission line** — primary Zen-loaded sense. It joins question and answer, preserves the continuity of sayings, and names lineage transmission or house style.
2. **hereditary bloodline** — family descent, independently used in the allowlisted corpus.
3. **blood vessels / bodily circulation** — physiological referent. This is not merely the etymology of the other senses; the corpus uses it in a medical comparison.

The third sense is required by item 8. A single “literal bloodline” sense cannot cover both father-child descent and blood vessels being brought into healthy mutual generation.

### Proposed occurrence set

**Sense 1 — connective/transmission line (retain five):**

- `T/T48/T48n2003.xml`, `0142b17–0142b19`: `僧又問曹山。如何是枯木裏龍吟。山云。血脈不斷。如何是髑髏裏眼睛。山云乾不盡。` — Caoshan case; unbroken connective line as the answer.
- `T/T48/T48n2003.xml`, `0150a02–0150a03`: `古人自是血脈不斷。所以道。問在答處。答在問處。` — Yuanwu locates the line in question and answer.
- `J/J33/J33nB294.xml`, `0739c06–0739c07`: `古人謂之血脈不斷。血脈不斷，猶目機銖兩也。` — explicit naming formula.
- `T/T48/T48n2003.xml`, `0216a06–0216a08`: `古人見徹此事。各各雖不同。道得出來。百發百中。須有出身之路。句句不失血脈。` — sentence-by-sentence continuity despite differing speech.
- `J/J27/J27nB198.xml`, `0457b28–0457b30`: `此是先大師受用不盡底付與雪關；雪關受用不盡送與石頭長老，接壽昌之血脈、展博山之家風。` — succession and house-style deployment.

**Sense 2 — hereditary bloodline (new):**

- `J/J34/J34nB311.xml`, `0594c08–0594c09`: `父子血脈相承、本無斷絕` — father-child bloodline inherited without interruption.

**Sense 3 — bodily circulation (new):**

- `J/J34/J34nB311.xml`, `0766c12–0766c13`: `偶因外感，發一奇病，幸遇良醫調治，血脈相生` — a doctor treats illness and restores mutually generating blood circulation.

### Family/#0g decision and exclusions

The Zen deviation is the movement from body/family continuity into a connective line through cases, speech, teacher-successor transmission, and house style. `血脈流通` is attested both physiologically and in lineage prose, so it is not a standalone sense test without context. Repeated copies of `血脈不斷` were excluded; the five Zen anchors already cover case answer, question-answer structure, direct naming, sentence continuity, and succession.

## 評唱 (`t_10ca0857a11b`)

### Proposed senses

1. **appraise and comment** — activity/verb; a named commentator works on another master's verses or cases.
2. **appraisal-commentary / commentary collection** — resulting textual product, genre, section, or book.

The act and resulting work are different things. Book titles that grammatically name a commentator performing the act remain under sense 1; explicit `評唱集` and a work that can be burned belong to sense 2.

### Proposed occurrence set

**Sense 1 — activity (retain four):**

- `T/T48/T48n2003.xml`, `0140a10–0140a11`: `師住澧州夾山靈泉禪院評唱雪竇顯和尚頌古語要`
- `T/T48/T48n2004.xml`, `0226b08`: `萬松老人評唱天童覺和尚頌古從容庵錄`
- `X/X67/X67n1303.xml`, `0267c05`: `林泉老人評唱投子丹霞頌古總序`
- `X/X67/X67n1304.xml`, `0323c11–0323c12`: `林泉老人評唱丹霞淳禪師頌古虗堂集`

These give the Blue Cliff, Serenity, Empty Valley, and Empty Hall title constructions with different commentators and verse authors.

**Sense 2 — product (retain two):**

- `X/X73/X73n1449.xml`, `0071b18–0071b19`: `由機緣而頌古作，由頌古而評唱集` — explicit documentary sequence ending in commentary collections.
- `J/J28/J28nB208.xml`, `0396b06–0396b07`: `焚萬松評唱公案` — a poem title treats Wansong's appraisal-commentary on cases as a burnable textual product.

### Family/#0g decision and exclusions

Keep separate from 著語, 代語, 下語, 頌古, and 拈古. 評唱 is extended prose appraisal/commentary around cases or verses, not a capping remark or substitute answer. No bare-headword self-definition was found; explicit authorship grammar and the `評唱集` formation sequence establish the split. More commentator-title repetitions were excluded as padding.

## 著語 (`t_0a686fa27769`)

### Proposed senses

1. **attach a comment** — the commenting operation, including being asked to comment.
2. **attached comment / capping comment** — the resulting short remark and named commentarial device.

The old combined KWIC contains two headword occurrences with different grammar. Replace it with two exact shorter occurrence objects rather than assigning one combined object ambiguously to both senses.

### Proposed occurrence set

**Sense 1 — operation (five):**

- `T/T48/T48n2003.xml`, `0144a07`: `雪竇著語云。勘破了也。` — split from the former combined KWIC; Xuedou attaches the comment.
- `T/T48/T48n2001.xml`, `0054b10`: `雪竇著語云。今日共者漢游山。圖箇甚麼。`
- `X/X66/X66n1296.xml`, `0069c20`: `雪竇顯於莫妄想處著語云：塞却鼻孔。` — comments at a specified point in a case.
- `X/X66/X66n1296.xml`, `0074c04`: `徑山杲於下座處著語云：葛藤不少。` — Dahui comments at the departure point.
- `J/J39/J39nB466.xml`, `0852b24`: `五臺山有一尊宿，設十二問，請師著語。` — a living master is asked to attach comments to twelve questions.

**Sense 2 — resulting comment/device (two):**

- `T/T48/T48n2003.xml`, `0144a08`: `眾中謂之著語。` — split from the former combined KWIC; direct communal naming formula.
- `T/T48/T48n2003.xml`, `0164b24–0164b25`: `此謂之著語。落在兩邊。` — second direct naming and appraisal of the resulting comment.

### Family/#0g decision and exclusions

Keep separate from `下語` (lay down/supply a response), `代語` (respond in another's place), `評唱` (extended appraisal), and `頌古` (verse on an old case). Zen makes 著語 a formal case-commentary device placed at an exact line or turn; the evidence does not license a generic “speech” sense. Additional `雪竇著語云` repetitions were excluded after author-at-case, author-at-stage-direction, and requested-comment deployments were represented.

## 君臣 (`t_2069b9c33315`)

### Proposed senses

1. **Caodong lord-and-minister configuration** — primary Zen-technical sense: lord/upright, minister/crooked, their directional relations, and accord.
2. **lord and minister** — ordinary political/ethical social roles.

The second is not an alternate reading of the Caodong ranks. The corpus explicitly places ordinary lord-minister duty beside father-child, spouses, age hierarchy, and friendship.

### Proposed occurrence set

**Sense 1 — Caodong configuration (retain five plus one):**

- `T/T47/T47n1987A.xml`, `0527a10–0527a12`: `君為正位。臣為偏位。臣向君是偏中正。君視臣是正中偏。君臣道合是兼帶語。` — locus definition.
- `J/J28/J28nB212.xml`, `0474c22–0474c23`: `君為正位，臣乃偏位，臣向君是偏中正，君視臣是正中偏，君臣道合` — independent transmission of the defining map.
- `J/J25/J25nB174.xml`, `0729a12`: `兼中到者，即君臣道合也。` — relation to the arriving-within-both rank.
- `J/J25/J25nB171.xml`, `0518a06`: `如何是曹洞宗？」師云：「君臣道合。」` — the configuration as an answer naming the Caodong house.
- `J/J33/J33nB294.xml`, `0745b19–0745b20`: `曹山就話荅話，當面具陳，初未嘗與五位為配。後人不辯，以此為君臣五位語` — intra-corpus historical dispute about later fivefold classification.
- `X/X71/X71n1405.xml`, `0047b15–0047b16`: `謂具三玄三要者，覿體全真；謂分五位君臣者，宛轉回互。` — house-device comparison: three mysteries/three essentials versus five lord-minister positions.

**Sense 2 — ordinary social roles (two new):**

- `J/J37/J37nB392.xml`, `0580c26–0580c27`: `父子有親，君臣有義，夫婦有別，長幼有序，朋友有信。`
- `J/J39/J39nB463.xml`, `0800c13–0800c14`: `父子有親，君臣有義，長幼有序，朋友有信`

### Family/#0g decision and exclusions

Cross-check with `五位`: that family entry covers the five-position system broadly; 君臣 covers this specific Caodong relational vocabulary. `王子五位`, `正偏`, and `功勳` remain related systems, not additional 君臣 senses. The Zen deviation is the conversion of political hierarchy into named rank relations and a house-identifying response. More verbatim copies of Caoshan's defining formula were excluded; the second copy is retained only for early/later source spread, while the new house-comparison and dispute anchor distinct facts.

## 鐵牛 (`t_20a56b9c1026`)

### Proposed senses

1. **Shaanfu iron ox** — the named cast landmark, including impossible actions still predicated of that landmark.
2. **iron-ox figure/comparison** — imagined iron ox used in comparisons, the iron-ox mechanism, mosquito-on-iron-ox phrasing, and hornless iron-ox answers.

The split is a named public object versus a figurative comparison or constructed image. Do not split each impossible action or each horned/hornless form again.

### Proposed occurrence set

**Sense 1 — Shaanfu landmark (retain one plus one):**

- `X/X78/X78n1556.xml`, `0646c20–0646c21`: `問：如何是學人轉身處？師云：陝府灌鐵牛。問：如何是學人親切處？師云：河西弄師子。` — watering the named Shaanfu iron ox as an answer.
- `T/T51/T51n2077.xml`, `0525b25–0525b26`: `嘉州大像出關來。陝府鐵牛入西蜀。` — the Shaanfu landmark made to enter western Shu.

**Sense 2 — figure/comparison (retain five):**

- `X/X80/X80n1565.xml`, `0109b06–0109b07`: `師曰。某甲在石頭處。如蚊子上鐵牛。祖曰。汝既如是。善自護持。` — explicit simile for Funiu's position at Shitou.
- `X/X80/X80n1565.xml`, `0186b03–0186b04`: `聲前非聲。色後非色。蚊子上鐵牛。無汝下觜處。` — mosquito figure with nowhere to bite.
- `X/X80/X80n1565.xml`, `0230b06–0230b07`: `祖師心印。狀似鐵牛之機。去即印住。住即印破。祇如不去不住。印即是。不印即是。` — explicit comparison of the patriarchal mind-seal to the iron-ox mechanism.
- `T/T51/T51n2077.xml`, `0568b11–0568b13`: `石門巇嶮鐵關牢。舉目重重萬仞高。無角鐵牛衝得破。毘盧海內作波濤。` — hornless iron ox breaking the iron barrier.
- `T/T51/T51n2077.xml`, `0478c04–0478c06`: `僧問。祖祖相傳傳祖印。師今得法嗣何人。師曰。無角鐵牛眠少室。生兒石女老黃梅。` — hornless iron ox in a lineage answer.

### Family/#0g decision and exclusions

The Zen deviation is observable: a public cast landmark becomes an answer and animated actor, while iron-ox figures enter comparisons about biting, barriers, lineage, and the mind-seal. `陝府鐵牛吞却乾坤` was excluded because landmark animation is already represented by watering and travel. Additional verbatim copies of `鐵牛之機` and `蚊子上鐵牛` were excluded as duplicates.

## 本心 (`t_734eadab549a`)

### Proposed senses

1. **original mind** — inherited Buddhist/Chan term paired with original nature and described through corpus predicates.
2. **original intention** — a person's prior intention, wish, or purpose in an event or succession decision.

The second is established by event prose, not merely inferred from an English translation. “This day's affair was not my 本心” is immediately expanded by “I had no intention to reside on a mountain”; another succession account says nobody had accorded with the speaker's 本心 except a named attendant.

### Proposed occurrence set

**Sense 1 — original mind (retain five):**

- `T/T48/T48n2016.xml`, `0426a13–0426a15`: `心無形色。無根無住。無生無滅。亦無覺觀可行。若有可觀行者。即是受想行識。非是本心。`
- `T/T51/T51n2076.xml`, `0208c17`: `欲識汝本心非合亦非離`
- `T/T48/T48n2008.xml`, `0349a21–0349a22`: `不識本心，學法無益；若識自本心，見自本性`
- `T/T51/T51n2076.xml`, `0218a05–0218a06`: `本心不寂要假寂靜。本來寂故何用寂靜。`
- `T/T51/T51n2076.xml`, `0233b01`: `祖祖佛佛只說如人。本性本心別無道理。`

**Sense 2 — original intention (two new):**

- `X/X81/X81n1568.xml`, `0007c14–0007c15`: `大眾，此日之事，故非本心。實謂祇箇住山寧有意，向來成佛亦無心。` — today's event was not the speaker's original intention; the following words explicitly deny an intention to reside on the mountain.
- `J/J39/J39nB435.xml`, `0005b29–0005b30`: `我雖則繼席六載，無有契我本心者，惟明元隨我十餘年` — in a succession discussion, the speaker says none accorded with his original intention except Mingyuan.

### Family/#0g decision and exclusions

Cross-check with `本性`: the Platform Record pairs recognizing original mind with seeing original nature, but the two headwords are not synonyms to merge. `真心` and `無心` are neighboring terms with their own predicates. The original-mind sense shows no license for a single imported metaphysical definition; retain the corpus's exclusions, pairings, and correction of imposed stillness. Copies of the same “this day's affair was not my intention” passage in T51, X80, X81, and X82 are one transmitted event, so only one is retained. `乖違本心` was excluded as ambiguous between original mind and intention.

## Registration cautions

- Split first, then assign occurrences; do not place one occurrence object under two senses.
- For 著語, replace the old two-hit combined KWIC with separate exact action and product KWICs.
- For 血脈, do not collapse hereditary descent and bodily circulation into a vague “literal” sense.
- For 鐵牛, preserve the named Shaanfu landmark boundary while avoiding a new sense for every animated predicate.
- No entry, WORK, STATUS, manifest, or termbase file was changed during this research task.
