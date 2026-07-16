# Read-only semantic/depth audit — b001 through b005

Scope: finished entries assigned to batches b001–b005 in `MANIFEST.jsonl` (75 entries). This is a semantic/depth audit, not a repeat of the global KWIC verification. No entry, status, manifest, termbase, guide, corpus file, or translation was edited.

Severity:

- **High** — sense structure or governing #0/#0b defect that can materially mislead the dictionary reader.
- **Medium** — definite English-first, imported-wording, attribution, or depth defect with a localized remedy.
- **Low** — worthwhile enrichment or wording cleanup that does not currently change the sense.
- **Clean** — no defect found in the requested categories after reading the entry and its WORK ledger.

## b001 checkpoint (15/15 audited)

| ID | Term | Verdict | Exact problem and corpus-grounded remedy |
|---|---|---|---|
| `t_b4a4ae6874d0` | 異類中行 | **High** | `Senses[0].Explanation` translates 畜生報 as “beast's karma”; use the literal result/recompense stated by the line (“does not receive a beast's recompense”). The same field calls Guizong's line a “gloss”; report it as his recorded answer unless the passage explicitly labels it a definition. `Senses[2].SenseKey = Caoshan Benji` is historical/house attribution, not a private meaning: its own Note says the use spans Yaoshan→Yunyan→Dongshan→Caoshan and later Caodong records, so make the house-wide technical sense null-keyed. `Senses[2].PreferredTarget` leaves “異類中事” untranslated; write “the matter among the different kinds (異類中事).” `Senses[0].Explanation` also leaves the rare syllable graph 𭊘 bare in the 唵𭊘啉 transcription; either translate/descriptively identify the whole vocal response or omit the untranslatable graph from prose while retaining it only in KWIC. |
| `t_1c7d25824f85` | 本來面目 | **Clean** | Rich question/answer and contrast inventory; the “cannot be cognized by consciousness” wording is a direct translated predicate, not an imported definition. Three curated anchors are supported by five source texts and a substantial ledger. |
| `t_4ccf8aed47d3` | 平常心 | **High** | Sense ordering violates the technical-first rule: `Senses[1]` (“ordinary mind is the Way”) is the primary Zen-technical saying and should precede the ordinary-question deployment. In `Senses[1].Explanation` and `Occurrences[0].AttributionNote`, “the Way needs no cultivating” imports the banned cultivation family for 道不用修; translate the recorded wording as “the Way does not need refining” or another literal, non-system label. **Depth:** `Senses[0]` has only one curated occurrence although the prose inventories distinct Changsha, Zhaozhou, Xianglin, Doushuai, and “accord with the Way” answers; add headword-bearing anchors for at least the Zhaozhou and one independent later answer, or record explicit exclusion reasons. |
| `t_adde034233ba` | 即心是佛 | **Clean** | The later withdrawal/device deployment is text-drawn and separately anchored; technical sense remains first and both shared senses are null-keyed. |
| `t_ad0a8e5aac3d` | 佛性 | **High** | `Senses[1]` is not a distinct meaning of 佛性: it is Zhaozhou's answer “no” to a predicate about a dog, repeatedly retold by later texts. `SenseKey = Zhaozhou Congshen` therefore buckets a famous deployment as master-specific polysemy. Fold the dog-case evidence, the 有/無 contrasts, and later “word no” citations into the corpus-wide 佛性 article; leave the lexical treatment of 無字 to its own entry. Also replace automatic doctrinal English in that sense: “karmic-consciousness” for 業識性 should be derived literally from its Chan contexts (for example “habit/action-consciousness nature,” subject to concordance), and the defensive “sutra sense of an innate nature” language should be replaced by the exact quoted claim and exact Chan counterexamples. |
| `t_d190cf45c531` | 話頭 | **Clean** | Current two-sense structure distinguishes the word/saying raised for investigation from ordinary conversational turns; no “huatou,” technique, or untranslated instruction remains. Depth includes multiple direct definitions and variants. |
| `t_49efe4fed8d4` | 祖師西來意 | **Clean** | Literal question, answer range, attribution cautions, and case recurrence are present without assigning a hidden point. |
| `t_5d6035b1e800` | 露地白牛 | **Clean** | Direct in-corpus predicates and ox/calf contrasts are retained; the Lotus-text source is reported as documentary provenance rather than imported doctrine. |
| `t_c13928184189` | 見性 | **Medium** | `Senses[0].Explanation` says the phrase is “not … attainment of a metaphysical essence.” This is imported metaphysical framing even in negation. Delete the defensive comparison and let the literal graphs, recorded “what is seeing nature?” answers, and see-nature/become-buddha collocation establish the entry. |
| `t_c728f3a8e02b` | 家風 | **Medium** | `Senses[0].Explanation` gives a fragmented Chinese-first question, `(如何是)（和尚）(家風)？`, leaving 和尚 outside the entry's normal translated-parenthetical convention. Rewrite English first: “What is the master's family style? (如何是和尚家風).” The remaining house/master/lineage range is rich and well supported. |
| `t_ff50c6974a36` | 五位 | **Medium** | `Senses[0].Explanation` leaves the full title 撫州曹山元證禪師語錄 bare after its English description. Put English first with the title parenthetically. The technical Caodong sense is correctly first and null-keyed; the ordinary “five positions/stages” sense is honestly provisional. |
| `t_0f97bfab265c` | 棒喝 | **Clean** | One corpus-wide compound sense, with the stick/shout components, pairings, and attribution limits recorded without turning them into techniques. |
| `t_6da91f8ce284` | 賓主 | **Clean** | Three meanings are genuinely separated: ordinary/shared guest-host, Linji's distinct four-relation system, and the shared Caodong interchange use. The Linji key describes a genuinely distinct master system rather than mere historical origin. |
| `t_c1af3ecba987` | 機鋒 | **Clean** | Direct criticisms prevent rhetorical-quickness overreach; deployment and collocations are present. The translated identity-consciousness warning is explicitly a quoted text predicate. |
| `t_ab6276be6e08` | 末後句 | **Clean** | Primary technical “last word” precedes the concrete dying-words pun; both are corpus-wide and independently anchored. |

## b002 checkpoint (15/15 audited)

| ID | Term | Verdict | Exact problem and corpus-grounded remedy |
|---|---|---|---|
| `t_4f7bd98ad40f` | 上堂 | **Clean** | Formal hall-address deployment, heading/action distinction, and genre spread are present; no imported ritual or doctrinal gloss. |
| `t_7180f7431520` | 恁麼 | **Clean** | Deictic and response uses are described from syntax and cases; no “suchness” doctrine was imported from the planning gloss. |
| `t_51fe593d9ffe` | 作麼生 | **Medium** | `Senses[0].Occurrences[0].AttributionNote` asks how one would “cultivate it.” Retranslate the cited 修 locally as “refine/do it” and preserve 作麼生 as “how/in what way,” without naming a cultivation system. The sense and depth are otherwise sound. |
| `t_1a7e251bda53` | 示眾 | **Clean** | Heading, finite verb, and assembly-address deployments are distinguished and supported across records. |
| `t_c945c2cc0e79` | 小參 | **Clean** | Corpus-derived convocation sense, timing/heading variants, and relation to hall addresses are present without ritual inflation. |
| `t_041f65670cd4` | 無心 | **Medium** | `Senses[0].Occurrences[1].AttributionNote` says “eons of cultivation accomplish nothing.” Translate the cited action literally without the banned cultivation category (for example, “even doing/refining for eons accomplishes nothing,” according to the exact Chinese). The Explanation otherwise reports explicit predicates and contrasts rather than a quiet-state technique. |
| `t_7d440e0d91b4` | 公案 | **Clean** | Correctly “public case,” with legal/public-record language, case-raising, comments, and named encounters; no Japanese loan, paradox, riddle, or device framing. |
| `t_f6dadadcbef5` | 無事 | **Medium** | `Senses[0].Explanation` defensively says not to inflate the term to a “metaphysical non-action” or “transcendence.” Purge those imported comparisons and retain the literal “nothing to do/no affair,” plus the corpus's own predicates. |
| `t_b291fe703ff1` | 參禪 | **Clean** | Correctly “investigate Chan,” with literal instructions and use range; no meditation, Japanese loan, or practice category. The scanner's hit on the noun “expression” is not an interpretive claim. |
| `t_dab856504b69` | 作家 | **Clean** | Adept/expert-hand sense, test/appraisal range, and ordinary craft comparison are corpus-grounded and sufficiently harvested. |
| `t_db4a932ce500` | 大悟 | **Clean** | “Great awakening” is described through narrated events, result clauses, and contrasts without an imported attainment theory. |
| `t_3a0a4e68cf13` | 葛藤 | **Clean** | Literal vines and the text-drawn verbal-tangle deployment are both represented; no symbolic meaning is asserted beyond corpus usage. |
| `t_218e4815d84a` | 勘破 | **Clean** | Examine/expose and see-through deployments, active/passive forms, and case tests are well differentiated. |
| `t_f2181872b682` | 轉語 | **Medium** | `Senses[0].Explanation` calls a person in the Baizhang fox case “a person of great cultivation.” Translate the source's 修行 wording by its local conduct/action rather than importing a cultivation rank; keep the turning-word request and its consequence as the textual facts. |
| `t_8ece09f6b91a` | 正法眼藏 | **Clean** | 法 is translated as “teaching,” not “Dharma”; transmission formulae, challenges, and later uses are richly anchored. The fixed quoted 涅槃妙心 phrase is reported as source wording rather than made the headword's doctrine. |

## b003 checkpoint (15/15 audited)

| ID | Term | Verdict | Exact problem and corpus-grounded remedy |
|---|---|---|---|
| `t_cd14935fc028` | 頌古 | **Clean** | Correctly a verse-on-old-cases genre/action, with headings, authorship, and commentarial sequence represented; no “koan” overlay. |
| `t_0ed8638229a9` | 無位真人 | **Clean** | Linji's wording, location predicates, later quotations, and person/rank graph senses are reported without metaphysical expansion. |
| `t_edabab064644` | 疑情 | **Medium** | `Senses[0].Occurrences[4].AttributionNote` calls Xueyan's narrated actions at age nineteen “training.” Replace that external category with “Xueyan recounting what he did at nineteen.” The Explanation's “break/smash the mass of doubt” is not an overread: it translates the text's own 撲破疑團. |
| `t_2069b9c33315` | 君臣 | **Clean** | Ordinary ruler-minister language and the Caodong relations are described from direct equations and diagrams; no generic dualism/nonduality gloss. |
| `t_7182bedf65d1` | 下語 | **Medium** | Strict #0c failure in `Senses[0].Explanation`: `眾皆下語不契` is left bare before its English; rewrite “the assembly all laid down words, none accorded (眾皆下語不契).” `Senses[0].Note` likewise leaves `下語用字` bare; write “wording and choice of words (下語用字).” Sense/deployment depth is otherwise strong. |
| `t_ebb0995c99fc` | 頓悟 | **High** | `Senses[0].Explanation`, `Note`, and `Occurrences[4].AttributionNote` repeatedly impose “cultivation” on 修/頓修/漸修 and even name a “sudden/gradual × awakening/cultivation analysis.” Retain the text-drawn fourfold formulas but translate them literally as sudden/gradual awakening paired with sudden/gradual refining (or another corpus-checked non-system verb). Attribute each scheme to its record; do not turn the combinations into an endorsed developmental system. Counts and formula inventory can remain. |
| `t_1e41b014d80e` | 向上一路 | **Clean** | Road-above/upward phrase, “not transmitted by a thousand sages” collocation, questions, and answers are present without defining an abstract transcendent realm. |
| `t_d35dc9e3723e` | 無念 | **Clean** | Platform-record self-definitions and contrasts are foregrounded; “complete command” replaces automatic samādhi and the entry rejects blankness by quoting the text, not doctrine. |
| `t_2738431562e6` | 無字 | **Clean** | Correctly “the word no,” with Zhaozhou case syntax and literal instructions; no Japanese “Mu,” mantra, technique, or untranslated loan. |
| `t_62044e7bbb87` | 本分事 | **Clean** | Direct self-definition, one's-own/share wording, 分外 contrast, and deployment range satisfy the depth gate. |
| `t_16140def874d` | 主人公 | **Clean** | “Master-in-charge” is grounded in direct calls and in the text's own contrast with 識神; the identity-consciousness wording reports that explicit contrast rather than defining a metaphysical self. |
| `t_ba841f6e11c8` | 乾屎橛 | **Clean** | Literal object, three major case deployments, and explicit no-gloss discipline meet the exemplar bar. |
| `t_7efdfe4296c6` | 父母未生前 | **Clean** | Literal temporal wording, question/answer range, face/body collocations, and later comments are present without “original state” mystification. |
| `t_ac2e2908084d` | 見性成佛 | **Medium** | `Senses[0].Explanation` says one may “cultivate the marks for ten thousand kalpas.” Retranslate the quoted 修 wording as forming/refining the marks, according to the exact line, without the cultivation category. The Four-Statements and imperative/declarative deployments remain sound. |
| `t_33d49f4710be` | 開悟 | **Clean** | Opening/awakening graph sense and narrated “came to understand” uses are well spread; no attainment ladder is inferred. |

## b004 checkpoint (15/15 audited)

| ID | Term | Verdict | Exact problem and corpus-grounded remedy |
|---|---|---|---|
| `t_427fa502a11b` | 話墮 | **Clean** | Speech-slip, reciprocal “both fell,” verdict, and adjacent 墮負 evidence are harvested without assigning hidden intent. |
| `t_b8063e3d60b4` | 直指人心 | **Medium** | `Senses[0].Note` leaves 見性成佛 bare. Write “seeing nature, becoming buddha (見性成佛).” The slogan's direct-pointing syntax and four-phrase context are otherwise sound. |
| `t_2d4525b4b123` | 教外別傳 | **Medium** | `Senses[0].Explanation` translates 修證所得 as “what any cultivating-and-realizing attains.” Replace the cultivation noun with literal “what any refining/doing and verifying obtains,” checked against the sentence. Keep Zhongfeng's direct definitions, Yunmen's question, and the objection/reply as attributed textual claims. |
| `t_53da4e346a6f` | 百尺竿頭 | **Clean** | Pole-top image, “advance a step” continuation, case/question uses, and variants are densely documented without symbolic interpretation. |
| `t_66792ea088de` | 拈古 | **Clean** | Picking-up-old-cases action and genre heading are distinguished, with commentarial sequence and variants. |
| `t_ce2a5ef71afe` | 麻三斤 | **Clean** | Literal weight/object, Dongshan Shouchu case, retellings, appraisals, and no-gloss discipline are all present. |
| `t_52391cba2cdf` | 三玄三要 | **Medium** | `Senses[0].Note` says the graphs state “no metaphysical triad.” Purge the imported metaphysical comparison and state only that Linji supplies the layering formula while Fenyang and later handbooks supply varying enumerations. The scanner's other 玄 flag is a nested-parenthesis false positive: each graph already has English. |
| `t_d7167b5f3236` | 殺人刀 | **Medium** | `Senses[0].Explanation` leaves the rare graph 𨁝 bare in `(要你)𨁝(跳)`. Translate the whole answer English-first, e.g. “I want you to leap (要你𨁝跳),” after confirming the graph reading. The kill/give-life, hold/release, and reversal relations are otherwise exceptionally rich. |
| `t_0a686fa27769` | 著語 | **Clean** | Direct “the assembly calls it attached words” definition, genre/action range, and capping/comment relations satisfy #0f. |
| `t_831f84399d0b` | 本地風光 | **Clean** | Literal native-ground scenery, question forms, and named answers are recorded without turning scenery into a symbol. |
| `t_46c30c5d57d4` | 不立文字 | **Clean** | Negative syntax, Four-Statements placement, Huineng's recorded warning, and text/letter distinctions are present; no anti-intellectual doctrine is inferred. |
| `t_223c2f6ade25` | 一大事因緣 | **Clean** | Platform-record self-definition, appearance-in-the-world formula, “one great matter” shorter form, and case uses are fully harvested. |
| `t_fd1759947989` | 大死 | **Clean** | Great-death/brought-back-to-life pairing, questions, verdicts, and direct predicates are text-grounded rather than recast as a state technique. |
| `t_f7bdd2def0ec` | 截斷眾流 | **High** | Sense order violates technical-first: `Senses[1]`, the middle member of Yunmen's three phrases, is the primary Zen-technical sense and should be `Senses[0]`; the free verbal phrase follows. `Senses[0].Occurrences[1].AttributionNote` says the function is “beyond expression”; use Hongzhi's exact predicate, “cannot be reached by formulation” (詮表不及), rather than an interpretive abstraction. The Deshan Yuanming origin correction and Yunmen association are strong and should be retained. |
| `t_097f38f58678` | 庭前柏樹子 | **Clean** | Literal garden cypress, Zhaozhou exchange, later challenges/comments, variants, and no-symbol discipline are all present. |

## b005 checkpoint (15/15 audited)

| ID | Term | Verdict | Exact problem and corpus-grounded remedy |
|---|---|---|---|
| `t_d4661c1b4dbb` | 正中偏 | **Clean** | Primary Five-Ranks meaning, Dongshan/Caoshan equations, verse, variants, and rank relations are direct and well spread. |
| `t_2852a9ae231c` | 隨波逐浪 | **Clean** | Yunmen-three-phrases technical use is first; ordinary “drift with the waves” use is correctly secondary and separately anchored. |
| `t_dc02eefd07f5` | 偏中正 | **Clean** | Technical rank is first and defined through direct corpus equations and contrasts, not generic dualism. |
| `t_78f95517a347` | 生死事大 | **Medium** | `Senses[0].Explanation` says one must paste “the two graphs 生死” on the forehead, leaving the graphs bare. Write “the two graphs ‘life and death’ (生死).” The fixed line, “impermanence is swift” pairing, inscription, plea, address, and biography deployments are otherwise unusually complete. |
| `t_ccd48e1c9145` | 正中來 | **Clean** | Rank order, direct definitions, route/coming predicates, verses, and relation to the other four ranks are well harvested. |
| `t_61c90d3a8edd` | 兼中到 | **Medium** | `Senses[0].Explanation` automatically renders 體用 as “essence-and-function.” Use the corpus-consistent literal pair “substance and function” in the quoted self-definition (“substance and function, thus-and-thus, is arrival within both”); avoid importing metaphysical “essence.” Structure and anchors are otherwise sound. |
| `t_1d3706324b0c` | 打成一片 | **Clean** | Physical image, direct predicates, object/result syntax, and contrasts are rich; prior loan-word cleanup is present. |
| `t_e6eb14b6c1ca` | 活人劍 | **Clean** | Giving-life/killing-sword pair, reversals, hold/release equations, questions, and appraisals are all represented. |
| `t_d03aa9267f79` | 大機大用 | **Clean** | Capacity/function graphs, paired and separated forms, questions, appraisals, and master spread are sufficiently documented. |
| `t_1da939bf1267` | 呵佛罵祖 | **Clean** | Literal reviling/cursing action, praise/rebuke contexts, and named examples are reported without making the behavior a technique or doctrine. |
| `t_8650004bb9d7` | 兼中至 | **Clean** | Fourth-rank variants, direct equations, verses, and contrast with 兼中到 are present and technical sense is primary. |
| `t_93ab42fecdca` | 本來無一物 | **Clean** | Textual variant history, Huineng attribution, line/verse deployment, and “not one thing” literal wording are responsibly distinguished. |
| `t_49829f59faac` | 函蓋乾坤 | **Clean** | Dominant graph form, lid/box reading, three-phrase placement, Deshan Yuanming formulation, and 涵蓋 variant are documented. |
| `t_9a5dc768cbc5` | 平常心是道 | **Clean** | This dedicated saying entry already keeps the technical sense primary and uses direct Mazu/Nanquan predicates without the older cultivation wording found in the separate 平常心 article. |
| `t_ed962dfd1158` | 四賓主 | **Clean** | Linji's four configurations, direct enumerations, later tests, speaker cautions, and distinction from ordinary 賓主 are well grounded. |

## Audit totals and repair order

- **75/75 entries audited.**
- **54 clean** in the requested semantic/depth categories.
- **21 with proposed repairs:** 5 high-severity and 16 medium-severity.
- No remaining automatic **“Dharma”** or **“samādhi”** was found in entry prose in b001–b005.
- No Japanese `huatou`, `Mu`, `koan`, `zazen`, `satori`, `kenshō`, Dōgen, or Japanese-source overlay was found.
- No additional corpus search was needed to establish the listed defects: each is visible from the entry's own cited Chinese, sense structure, and WORK inventory. Exact Chinese should still be checked with `zc` during repair whenever 修, 報, 業識性, 體用, or a rare graph is retransliterated/retranslated.

Recommended repair sequence:

1. **Structural first:** 佛性 sense split/key; 平常心 and 截斷眾流 ordering; 異類中行 Caoshan key.
2. **Imported framing:** all listed cultivation/training wording, 異類中行 “karma,” 佛性 “karmic-consciousness,” and the negative metaphysical comparisons.
3. **Strict English-first:** the localized 家風, 五位, 下語, 直指人心, 殺人刀, and 生死事大 fields.
4. **Depth:** add the missing distinct ordinary-answer anchors to 平常心 sense 0 or record explicit exclusions.

This report is the only file written by the audit.
