# Item-8 sense sweep: done single-sense entries `t_a*`–`t_f*` (606-entry state)

Date: 2026-07-13  
Mode: read-only; no `entry.v2.json` was changed.

## Result

- Done, single-sense articles examined: **198**
- Clean after adjudication: **190**
- Confirmed/probable split candidates: **8**
  - Confirmed: **7**
  - Probable, requiring rewrite-time adjudication: **1**
- Semicolon-target detector hits: **1** (`敗闕`), adjudicated **false positive**.

The sweep inspected every assigned preferred target, explanation, note, curated-occurrence grammar, and overlapping family. `maintenance/depth-sense-gate.json` was used only as an initial queue. Targeted `zc.py` searches tested literal versus Zen-loaded uses, word versus title/person, concrete object versus act, and corpus-wide versus master-specific deployment. Every passage below is allowlisted and was reverified with exact `FromLb`/`ToLb`.

## Confirmed candidates

### 1. `律` (`t_c0a6177c9c2f`) — rule/code versus poetic meter

The existing single target “code” does not cover the graph in `詩律`, “poetic meter.” These are different things.

- Precept/Vinaya code: `T/T51/T51n2076.xml`, `0251a03`–`0251a04`: `是大乘戒律。胡不依隨哉。` — “These are the greater-vehicle precept codes; why not follow them?”
- Poetic meter/form: `J/J39/J39nB458.xml`, `0710b27`–`0710b28`: `明季八股文章，唐晚七言詩律。` — “late-Ming eight-legged prose; late-Tang seven-character poetic meter.”

Recommendation before rewrite: split code/rule from poetic meter; keep the Zen/precept sense first. Family review should also separate statutory `法律`, “law,” if its exact passages establish a third object rather than a compound paraphrase.

### 2. `迦葉` (`t_ba6668ef3e6e`) — Mahākāśyapa versus Kāśyapa Buddha

The current article defines the flower-sermon patriarch, but the same graphs name the earlier Kāśyapa Buddha. The corpus itself places them in different roster positions.

- Mahākāśyapa the patriarch: `X/X80/X80n1565.xml`, `0031a07`–`0031a08`: `唯迦葉尊者破顏微笑。` — “only Venerable Kāśyapa broke into a smile.”
- Kāśyapa Buddha, sixth of the seven buddhas, explicitly distinguished from the first patriarch: `B/B25/B25n0144.xml`, `0299b09`–`0299b10`: `第六迦葉佛，第七釋迦佛。第一大迦葉祖，第二阿難祖` — “sixth, Kāśyapa Buddha; seventh, Śākyamuni Buddha; first patriarch, Mahākāśyapa; second patriarch, Ānanda.”

Recommendation before rewrite: split the two figures. Define each by Chan deployment, and do not merge them merely because English commonly writes both as Kāśyapa.

### 3. `立雪` (`t_d2892b1eaae0`) — Huike’s case-event versus literal standing in snow

The current article centers Huike’s lineage case. The corpus also uses the same verb-object phrase for a heron physically standing in snow.

- Huike’s named case-event: `X/X80/X80n1565.xml`, `0042c22`–`0042c23`: `汝久立雪中。當求何事。` — “you have long stood in the snow; what do you seek?”
- Literal animal posture: `T/T51/T51n2077.xml`, `0480b05`–`0480b06`: `問四威儀中如何履踐。師曰。鷺鶿立雪。` — “asked how to proceed within the four deportments, the master said: a heron stands in the snow.”

Recommendation before rewrite: split the Zen-loaded Huike event from ordinary physical standing in snow. Different appraisals of Huike’s event remain readings, not senses.

### 4. `普說` (`t_bb19ed0e0fab`) — general-address event/genre versus delivering one

The corpus supports both a named event/genre and a verb phrase. Item 8 expressly treats a genuinely different word class as polysemy.

- Event/genre requiring its own drum: `X/X63/X63n1250.xml`, `0666b06`–`0666b07`: `陞座、小參、普說法皷，轉藏有藏皷` — “mounting the seat, informal convocation, and general address have the teaching drum; turning the canon has the canon drum.”
- Verb, “give a general address on this”: `X/X72/X72n1435.xml`, `0272c23`–`0272c24`: `今日因高麗國晦曇上座請普說此` — “today, because senior Huitan from Korea requested [me] to give a general address on this…”

Recommendation before rewrite: noun/event first, verbal “give a general address” second. Do not split headings from events; those denote the same genre.

### 5. `擔荷` (`t_efa1e241a7f0`) — figuratively shoulder/assume versus physically carry a load

The current article folds physical-load language into the figurative target “shoulder.” One exact passage, however, has an actual shoulder-load of firewood.

- Figuratively bear/assume: `T/T47/T47n1997.xml`, `0738c08`–`0738c09`: `驀然有箇承當得。擔荷得。趣向得。行履得。` — “suddenly there is one who can accept it, shoulder it, head toward it, and carry it out.”
- Physical load: `X/X70/X70n1390.xml`, `0471a23`–`0471a24`: `爭得盧公賽子雲，一肩擔荷柴衝折。` — “how could Master Lu rival Ziyun? Carrying firewood on one shoulder, the pole breaks.”

Recommendation before rewrite: split physical carrying from figurative assumption/responsibility. Compounds naming “this matter,” a seal, or house-work remain within the figurative family.

### 6. `逍遙` (`t_ff59d753a7b1`) — verb/state versus master title versus mountain/place

This is a direct word/title/place collision. The current “roam at ease” sense cannot cover the lamp headings.

- Verb/state, roam at ease: `T/T51/T51n2076.xml`, `0313b24`: `任性逍遙隨緣放曠。` — “roam at ease according to your nature and range freely following conditions.”
- Master title/name, Master Xiaoyao: `T/T51/T51n2076.xml`, `0262b24`: `逍遙和尚逍遙和尚。一日師在禪床上坐。` — “Master Xiaoyao. One day the master sat on the Chan seat.”
- Place-name, Mount Xiaoyao: `T/T51/T51n2076.xml`, `0325c24`: `江西逍遙山懷忠禪師` — “Chan Master Huaizhong of Mount Xiaoyao in Jiangxi.”

Recommendation before rewrite: three referents must be represented or deliberately routed: lexical “roam at ease,” the master roster identity, and the geographic name. The latter two must not be explained as states of ease.

### 7. `清規` (`t_ee9dd8b4eb5b`) — enforceable monastic code versus book title

The code and a named work containing that code are different objects.

- Enforceable house code: `X/X82/X82n1571.xml`, `0446c11`: `你是持戒人，為什不守清規？` — “you are a precept-keeping person; why do you not observe the monastic code?”
- Title: `T/T48/T48n2025.xml`, `1109c16`–`1109c17`: `No.2025勅修百丈清規勅修百丈清規` — title matter identifying *The Imperially Revised Baizhang Monastic Code*.

Recommendation before rewrite: retain the code as primary and add the named-work sense. Generic references to “Baizhang’s code” require case-by-case assignment; they are not automatically book titles.

## Probable candidate

### 8. `作家` (`t_dab856504b69`) — Chan adept versus accomplished poet

The dominant sense is the tested Chan adept or expert teacher. One public exchange explicitly applies the same word to a poet. Because both can be paraphrased broadly as “expert,” this is marked probable rather than confirmed pending full rewrite-time colligation review; nevertheless, the person-role and domain differ and the current bare target “an adept” hides that fact.

- Chan adept/master: `T/T48/T48n2003.xml`, `0170c28`–`0170c29`: `大凡作家宗師。要與人解粘去縛。` — “in general, an adept master must unstick and unbind people.”
- Accomplished poet: `T/T51/T51n2077.xml`, `0485c25`–`0485c26`: `曰作家詩客。師曰。一條紅線兩人牽。` — “he said, ‘an accomplished poet’; the master said, ‘one red thread pulled by two people.’”

Recommendation before rewrite: test whether `作家詩客` is productive enough for a separate corpus-wide “expert/accomplished poet” sense. Do not import the modern default “author/writer”; the verified passage says poet.

## Explicit false positives and clean high-risk families

- **`敗闕` (`t_b8d2633b12ef`) — semicolon false positive.** “Failure; exposed fault” is one adverse outcome stated two ways, not two objects. `既於學士面前各納敗闕` (“each incurred failure before the scholar,” `X/X82/X82n1571.xml`, `0048c08`–`0048c09`) and `那裏是他敗闕處` (“where is his point of failure?” `T/T47/T47n1998A.xml`, `0831a20`) differ syntactically but denote the same failure/fault. Merge-smell adjudication: keep one sense unless new evidence supplies an independently referential object.
- **`一隻眼` (`t_ccae22e8375d`) — no physical-eye split established.** The researched corpus uses the one-eye image as Chan appraisal: “possess one eye,” “lose/exchange one eye,” and “the ten-direction world is one eye.” No independent injury/anatomy narrative was found. Different appraisals of the same image are readings.
- **`參同契` (`t_f6c2a28b1c6e`) — title only in the tested corpus.** Exact-title searches found Shitou’s poem and quotations from it, not a second generic agreement or a second work with the exact title.
- **`家風` (`t_c728f3a8e02b`) — no literal domestic-custom split established.** The sampled family consistently names a master’s, monastery’s, or Chan house’s style; kinship wording is internal to that house metaphor.
- **`小參` (`t_c945c2cc0e79`) — genre only.** Lists such as `普說小參問答勘辯之屬` enumerate general addresses, informal convocations, question-and-answer, and examination; they do not establish a verb “investigate a little.”
- **`鼻孔` (`t_ea138c7335d`) — one anatomical image, many deployments.** Nose-hole language is twisted, pierced, blocked, smelled through, or used in appraisals, but targeted searches did not find a second object called a nostril.
- **`玄關` (`t_a8b4f101d192`) and `牢關` (`t_ffb0ee18f1a2`) — no independent architecture/military referent established.** Their actions remain within the recurring Chan barrier/pass image; an ordinary literal gloss alone is not proof of a separate corpus sense.
- **`應諾` (`t_a784d81e277b`) — acknowledgement family only.** Answering a call, acknowledging an instruction, and answering before leaving are forms of one responsive act, not separate things.
- **`作家` detector caveat.** It is the sole probable rather than confirmed candidate; generic “expert” may ultimately cover both roles, but the poet colligation must be explicitly adjudicated during rewrite.

## Clean-count basis

All remaining assigned articles were inspected and counted clean for item 8 at this pass. “Clean” means no second thing was established by the article plus targeted family searches; it does not certify depth, English, attribution, or other gates. In particular, high-frequency abstract or grammatical entries such as `只如`, `且道`, `這箇`, `切忌`, and `作麼生會` can take many referents or complements without becoming polysemous themselves. Likewise, concrete images such as `草鞋`, `露柱`, `劍刃`, `金鎖`, and `鼻孔` remain the same object when masters redeploy them, unless a separate title/person/object is attested.

No rewrite should begin until these candidates are reviewed by the coordinator, as requested.
