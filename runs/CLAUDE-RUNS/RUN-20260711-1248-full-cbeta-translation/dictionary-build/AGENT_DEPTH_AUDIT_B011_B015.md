# Semantic and depth audit — b011–b015

Read-only audit under the current `CODEX_HANDOFF.md` and `DICTIONARY_ENTRY_GUIDE.md`. This report is the only audit output; no entry, status, manifest, termbase, or corpus file is edited.

## Audit criteria

- #0 describe rather than interpret; unsupported purpose, symbolism, or hidden-force claims.
- #0b Chinese Chan only: no imported doctrine, meditation/mindfulness, present-moment, dualism, practice/method, Japanese overlays, or afterlife theory promoted into a definition.
- #0c English-first translation, including quoted Chinese outside bare `Kwic` evidence.
- Correct `SenseKey`, sense order, master attribution, and multi-source semantics.
- Depth gate: direct definitions, deployment range, text-drawn contrasts, variants/collocations, historical and genre spread, and documented exclusions.
- Mechanical corroboration: exact ID, allowlist membership, exact headword-bearing KWIC, `zc.verify` line bounds, and occurrence paths represented in `SourceTexts`.

## b011 checkpoint — complete (15/15)

Mechanical corroboration: all 80 occurrences across the fifteen entries are allowlisted, contain the exact assigned headword, return `zc.verify(...).ok == True`, and have stored line bounds identical to verifier output. All IDs match the deterministic source-term hash, and every occurrence path appears in its sense’s `SourceTexts`.

Automated #0b scan found no `Dharma`, `samadhi`, Japanese loan, meditation/mindfulness, practice/method/technique/training/cultivation, present-moment, dualism, metaphysical, symbolic-intent, or related imported-framing language in preferred targets, explanations, notes, or attribution notes. Manual review confirmed that the two fox-body entries report the case’s literal narrated sequence without promoting rebirth or cause-and-effect doctrine into Zen’s meaning.

| ID | Term | Verdict | Findings |
|---|---|---|---|
| `t_5b39f18f89ff` | 狗子無佛性 | **Low** | Semantically sound and unusually disciplined: the later compound is treated as a case title/shorthand, the word “no” is translated as no, both yes/no forms are related rather than split, and all distinct deployment shapes are harvested. One small #0c/style repair is warranted: the Note renders `如何是狗子無佛性` as the ungrammatical “what is a dog has no buddha-nature?” Write “What is ‘a dog has no buddha-nature’?” The same awkward form occurs in occurrence note 5. |
| `t_207efae5f6bd` | 死句 | **Clean** | Two direct corpus definitions, the living/dead contrast, the instruction to investigate the living phrase, direct answers, a later explicit example list, the neither-dead-nor-living member, and Gulin’s reversal are all retained. No abstract theory is imposed over the classifications. |
| `t_8107ccae18eb` | 拈提 | **Clean** | Graph values, transitive action, publication/genre label, agent noun, deliberately withheld comment, and Dagui’s appraisal are represented across independent records. The relation to dragging mud and carrying water is explicitly text-drawn. |
| `t_db103ad2434d` | 鬼窟裏 | **Clean** | The entry keeps the cave spatial and reports the corpus’s verbs—reckon, calculate, sit, enter/emerge, make a living—plus the black-mountain extension. No quiet-state, meditation, or metaphysical enclosure is invented. WORK records the lack of a direct definition and the exclusion of redundant witnesses. |
| `t_bbee6625a4d5` | 赤肉團上 | **Clean** | Literal bodily wording remains primary. Linji’s true-person line and Nanyuan’s thousand-fathom-wall line are correctly kept as distinct case families, with no cross-attribution and no imported abstraction. Positional variants and exact exclusions are documented. |
| `t_a0634fecce83` | 拖泥帶水 | **Clean** | Direct answers (“seven hands and eight feet,” “no small amount of disorder”), answer/appraisal range, reverse word order, explicit negative questions, and the reciprocal thousand-fathom-wall relation satisfy the depth gate. The entry does not turn the phrase into a modern psychological label. |
| `t_6c20139c8cc0` | 銀山鐵壁 | **Clean** | Direct question, Foyan’s self-report, Baiyun’s quoted before/after contrast, Xueyan’s route-of-meaning classification, Zhongfeng deployments, and collapse verse are all included. The image is not replaced by “impenetrable mind” or another abstraction. |
| `t_02d93ab1ca2e` | 透關 | **Clean** | Passed/unpassed person contrast, Zhongfeng’s corrective distinction, productive eye/phrase compounds, and gatekeeper syntax are represented. The entry adds no destination, attainment system, or technique beyond the recorded barrier grammar. |
| `t_21926ca0b92e` | 頂門眼 | **Clean** | Possess/open/look/illuminate/strike-blind deployments, the direct question exchange, exact collocation counts, and later Zhaozhou appraisal are present. It explicitly declines to manufacture an anatomical or imported-perception theory. |
| `t_c327d2a1fc8c` | 金剛眼睛 | **Clean** | Direct “blind” answer, possession statements, twelve-ounce line, four-part enumeration, and ten-directions predication give good range. The entry does not reconcile the deliberately varied predicates into a theory of special perception. |
| `t_07d808115439` | 言語道斷 | **Clean** | Literal graphs, the paired mental-activity formula, Trust in Mind verse, explicit rejection of shut eyes/darkness, direct question, quoted correspondent, and immediate counter-formulation with mental activity not extinguished are all surfaced. This is a strong #0b entry because silence is not substituted as the meaning. |
| `t_6f138f2956d8` | 不落因果 | **Clean** | The real Baizhang wild-fox case is reconstructed with its speakers, two answers, stated five-hundred-life consequence, funeral request, and later comments. `大修行底人` is rendered locally as “person of great conduct,” not as a cultivation rank. The narrated fox lives remain report, not doctrine. |
| `t_1307081cf96c` | 不昧因果 | **Clean** | Corpus-wide null sense is correct. Baizhang’s answer, Wansong’s explicit contrast, the unnamed monk’s objection, Dahui’s refusal to collapse the issue, and later same/different questions preserve disagreement rather than imposing a universal gloss. |
| `t_10ca0857a11b` | 評唱 | **Clean** | Verb and genre noun are established through explicit titles, commentator/verse-author constructions, the encounter→verse→commentary sequence, and a critical burning-poem heading. Multi-name titles are appropriately unlinked. |
| `t_300236cb6368` | 當面錯過 | **Clean** | Verdict, warning, letter acknowledgment, staff action, ordinary visible-event sequence, close graph variants, and later-period limitation are all documented. The object remains unstated where the records leave it unstated. |

### b011 totals

- **14 clean**
- **1 low-severity English rendering repair**
- **0 medium / 0 high**
- No sense-order or key misuse found.
- No not-thin failure found; each WORK inventory explicitly accounts for self-definition searches, distinct deployments, relations/variants, spread, and omissions.

## b012 checkpoint — complete (15/15)

Mechanical corroboration: all 80 occurrences are allowlisted, exact-headword-bearing, `zc.verify` clean, and line-synchronized; IDs and `SourceTexts` coverage are complete. The automated imported-framing scan returned no direct loan hits, but manual review found one negation-blind semantic import in 黑漆桶.

| ID | Term | Verdict | Findings |
|---|---|---|---|
| `t_438eb81f17bf` | 心行處滅 | **Clean** | Literal syntax, fixed 言語道斷 pair, explicit parallel, direct question, quoted equation, and the “mental activity is not extinguished” counter-witness are all represented. It explicitly avoids equating the phrase with blankness. |
| `t_90435e47b008` | 休去歇去 | **Low** | The command, Shishuang sequence, contrary sequence, later refusal, Dahui/Zhongfeng criticisms, and literal imperatives make this deep and non-prescriptive. The Note’s claim that the excluded late naming formula “overlays unrelated terminology” is too vague to audit: identify and translate the exact wording and exclusion reason, or delete that editorial sentence. |
| `t_ddab56ede4ef` | 桶底脫 | **Clean** | The literal noodle-bucket control case, Xuefeng recollection with Yantou’s challenge, direct Buddha-answer, and mechanism-exhausted collocation prevent an abstract “breakthrough” theory from replacing the image. |
| `t_398a33955019` | 默照 | **Clean** | Hongzhi’s affirmative wording, Dahui’s explicit opposition, followers/ghost-cave language, and Sanfeng’s direct critical naming formula are all attributed. The contested Chinese range is preserved without importing a meditation system. |
| `t_824cfb1434b1` | 擒縱 | **Clean** | The military capture/release register is surfaced through the corpus’s coordinated command phrases, explicit objects, “with me” wording, and great-teacher predicate. Kill/bring-to-life and roll-out/fold-up relations are text-drawn rather than mystical. |
| `t_47e7132eb361` | 六根門頭 | **Clean** | Eye/ear/nose/tongue/body/mind are translated, and the locative deployment is built from recognize, scrape, shine, and bare predicates. It explicitly refuses a general sensory theory. |
| `t_3bf26be0cd43` | 黑漆桶 | **Medium** | Evidence harvest is strong—person-label, answer, self-comparison, hoop/bottom/inside morphology, break/overturn/leap verbs—but the opening asserts that it is “the pitch-dark, benighted or unenlightened mind” and that breaking it “is awakening.” Neither conclusion is a quoted corpus predicate, and both import the familiar enlightenment-state frame. Remove those claims and lead with the attested facts: it labels people, answers a question, is used in a student’s self-comparison, and when Rujing says to break it the next stated words are “the ten directions are open and empty.” The Note’s “benighted-mind epithet” needs the same repair. |
| `t_b26bfa9e399e` | 迴光返照 | **Clean** | Literal four-graph action, orthographic variants, imperatives, named objects/complements, Linji/Shitou witnesses, and “illuminating present and past” are present. It explicitly declines to rename the instruction as an exercise or external system. |
| `t_2facdfa49dd9` | 末後牢關 | **Clean** | Final/locked/barrier graphs, pass-through forms, direct “how many?” question, non-measurable/not-understood predicates, and the related last-sentence formula are concise but adequate. |
| `t_5f1287817ebd` | 野狐禪 | **Clean** | Rare adverse label is defined from tear-apart, sweep-away, and show-off-tricks deployments, with the Baizhang allusion distinguished from wider uses. The fox-life verse remains a recorded case allusion rather than afterlife doctrine. |
| `t_dec67da1f076` | 沈空滯寂 | **Clean** | Literal critical verbs, 有/無 obstruction contrast, rejected diagnostic list, arhat-picture caption, “two-vehicles Chan illness,” and graph variant are all included. The entry clearly states that the phrase is condemnatory in these sources. |
| `t_12e8cba30de6` | 老僧 | **Clean** | Correct technical-first order: speaker-centered “this old monk” precedes ordinary third-person “an old monk.” Pronoun equivalence, possession/location/action grammar, indefinite marker, and exceptional local gloss are carefully distinguished. |
| `t_8a06e7d99b19` | 法嗣 | **Clean** | “Lineage heir” avoids Dharma, and headings, predicates, predictions, counts, and “succession not detailed” catalogue language establish a stated teacher-successor relation. The entry correctly warns that a lineage heading does not identify encounter speakers. |
| `t_acccac1051a4` | 衲僧 | **Clean** | Literal robe/person, direct question, own-affair answer, and productive household/eye/handle/portion/gate compounds give strong institutional range without “practitioner” or training language. |
| `t_0fb794f515bd` | 法身 | **Clean** | “Teaching-body” is argued from Qianyan’s in-corpus word comparison, then tested against several deliberately divergent definitions, questions, functions, sides, speech forms, and illnesses. One corpus-wide sense with master-specific answers as witnesses is structurally correct. |

### b012 totals

- **13 clean**
- **1 low**
- **1 medium**
- **0 high**
- No key/order misuse or not-thin failure found.

## b013 checkpoint — pending

## b014 checkpoint — pending

## b015 checkpoint — pending
