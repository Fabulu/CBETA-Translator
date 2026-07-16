# Depth / not-thin audit of the first 128 dictionary entries

Date: 2026-07-12  
Scope: all 128 `terms/t_*/entry.v2.json` files present when the audit began  
Mode: read-only; no entry, manifest, status, termbase, or corpus file was changed

## Result

The collection is mechanically strong and much richer than its occurrence counts alone suggest, but it is not yet uniformly through the §0f depth gate.

- **8 high-priority depth defects**: a knowingly incomplete sense inventory, five omitted explicit self-definitions/equivalences, one omitted corrective definition, and one omitted morphological/historical label.
- **5 medium-priority omissions**: useful attested deployment shapes or explicit formulae absent from otherwise serviceable entries.
- **21 monitor/research-expansion entries**: no demonstrated unique omission, but their frequency or thin evidence inventory justifies a deliberate final search pass.
- **94 entries with no actionable depth defect found** in this pass.

This is a depth audit, not a conformance rewrite. A few prose problems noticed incidentally are mentioned only where they affect the depth judgment.

## Method

1. Parsed every current entry and inventoried senses, curated occurrences, explanations, notes, related terms, and variants.
2. Ran one cached, allowlist-scoped `zc.count()` pass for all 128 source terms.
3. Searched all matching allowlist files for the §0f formula families: `X者`, `所謂X`, `謂之X`, `名為X`, `名曰X`, `喚作X`, `何謂X`, and `如何是X`.
4. Scanned immediate left/right graph neighbors for omitted high-frequency compounds and morphological extensions.
5. Manually discounted false positives: grammatical `者` adjacency, nested strings, table-of-contents repetition, duplicated recensions, and formulae whose content was already represented in English.
6. Re-read the complete articles for every high- and medium-priority candidate. Every candidate quoted below was then run through `zc.verify()` and returned `ok == True`.

Raw frequency was treated only as a review signal. A four-occurrence entry can be deep, while a six-occurrence entry can still omit the corpus's clearest definition.

## High priority

### H1 — `t_6f47a97d45b0` — 序, “preface; sequence/order; rank”

The article deliberately covers only the bibliographic “preface” sense and explicitly says that order, sequence, and other running-prose senses are not covered. That conflicts with one-article-per-source-term storage and longest-match highlighting: the single graph will be recognized outside headings too. This is a **sense-inventory defect**, not merely thin prose.

Current live evidence: the raw graph has 4,782 hits in 400 allowlist texts; narrower searches return `次序` (“sequence/order”) 74 hits in 64 texts, `兩序` (“the two ranks/divisions”) 598/129, `序曰` (“the preface says”) 30/23, `序云` (“the preface states”) 61/35, `序文` (“prefatory text”) 76/66, `序分` (“introductory division”) 14/13, and `序跋` (“prefaces and postscripts”) 19/12.

Follow-up: search those exact compounds, separate bibliographic **preface**, ordinary **sequence/order**, and the institutional **two ranks/divisions** if the contexts sustain three senses. Do not use the noisy raw single-graph count as a sense count.

### H2 — `t_c13928184189` — 見性, “seeing one's nature”

The entry is already substantial and includes several question-and-answer self-definitions, but it omits a distinct direct definition in the lamp record: “one has never seen it; when the locus and form of seeing cannot be obtained, perceiver and perceived are both cut off—this is called seeing nature.” This is exactly the kind of additional self-definition §0f says must not be dropped.

Verified candidate: `T/T51/T51n2076.xml`, lb `0231a21–0231a22`: `未嘗見。求見處體相不可得。能所俱絕。名為見性。` (“One has never seen it. When the locus and form of seeing cannot be obtained, perceiver and perceived are both cut off; this is called seeing nature.”)

Follow-up: `zc.count("名為見性")`, inspect the parallel witnesses in `T51n2076`, `X80n1565`, and `X81n1571`, identify the underlying master/case, and curate one primary witness plus a note on the recension spread.

### H3 — `t_1a7e251bda53` — 示眾, “address/showing to the assembly”

The entry says that the term stands beside “small gathering” (`小參`) but omits the corpus's clearest in-text genre comparison: beating the drum and ascending the hall at night is called a small gathering; doing so today is called an address to the assembly; the text then says the two have no different statement. This is both a self-definition and a text-drawn contrast/equivalence.

Verified candidate: `X/X71/X71n1409.xml`, lb `0114b11–0114b12`: `夜來撾鼓升堂，喚作小參；今日陞堂撾鼓，謂之示眾。示眾、小參，初無二說；夜來、今日，豈有兩般？` (“Last night, beating the drum and ascending the hall was called a small gathering; today, ascending the hall and beating the drum is called an address to the assembly. Address to the assembly and small gathering originally have no two statements; how could last night and today be two different things?”)

Follow-up: search `謂之示眾`, `喚作小參`, and the full paired line. Add the exact comparison and link the two entries in both directions.

### H4 — `t_16140def874d` — 主人公, “master-in-charge”

The article has excellent coverage of Ruiyan, Zhaozhou, the identity-consciousness contrast, and later test questions, but it misses the clearest explicit equivalence found in Hanyue's record: mind, on each person's own share, is called “oneself” and also “master-in-charge.” That is a second self-definition, not redundant commentary.

Verified candidate: `X/X70/X70n1403.xml`, lb `0776a01–0776a02`: `何者謂之心？心在諸人分上喚作自己，又喚作主人公。` (“What is called mind? Mind, on each person's own share, is called oneself and is also called the master-in-charge.”)

Follow-up: search `喚作自己` + `喚作主人公`, inspect the surrounding Hanyue address, and add this as an independently attributed definition alongside Ruiyan and Zhaozhou.

### H5 — `t_c1af3ecba987` — 機鋒, “pivotal edge / sharp pivotal point”

The current article extrapolates from crossbow-trigger and blade-edge imagery and says the term's speed and edge receive no further gloss. It omits a particularly important **corrective** corpus passage that first calls lightning-fast, non-deliberative performance “a swift pivotal edge not falling into the thinking root,” then immediately says this is identity-consciousness playing tricks. Without that line, the deployment inventory overweights praise and misses an explicit criticism of the very gloss the article foregrounds.

Verified candidate: `T/T47/T47n1998A.xml`, lb `0915b22–0915b24`: `似閃電光。擬議不來。呵呵大笑。謂之機鋒俊快不落意根。殊不知。正是業識弄鬼眼睛。` (“Like a flash of lightning: deliberation cannot reach it, and one laughs aloud. This is called a swift pivotal edge that does not fall into the thinking root. Little does one know that it is precisely identity-consciousness playing tricks with ghostly eyes.”)

Follow-up: search the full `謂之機鋒俊快` formula and compare it with the current favorable collocations. Rewrite only to report that the record uses the label both approvingly and critically; do not decide what a pivotal edge accomplishes.

### H6 — `t_1d3706324b0c` — 打成一片, “pounded into one piece”

The entry is rich in later instructional deployments, contrasts with interruption/running-off, and paired terms said to become one piece. It nevertheless omits an explicit Blue Cliff Record equivalence: being on the “single-color side” is also called being pounded into one piece. A later record then explicitly denies that merely knowing the “silver mountain and iron wall” is being pounded into one piece. Together these give a high-value definition-and-correction pair.

Verified candidate: `T/T48/T48n2003.xml`, lb `0180a02–0180a03`: `亦謂之普賢境界一色邊事。亦謂之打成一片。` (“It is also called the one-color-side affair of Samantabhadra's sphere; it is also called being pounded into one piece.”)

Follow-up: search `謂之打成一片`, then inspect `T48n2003` and `X70n1402`. Represent both the equivalence and the later rejection `不可謂之打成一片` (“it cannot be called pounded into one piece”) without importing an explanation.

### H7 — `t_326be1e9c98a` — 枯木, “withered tree”

The article covers the crone-burning-the-hermitage case and fixed withered-tree imagery, but it omits a corpus-internal morphological/historical label: a community whose students sat for long periods without lying down, standing rigid like stumps, was called the “withered-tree assembly” (`枯木眾`). This is unusually strong describe-only evidence for how the noun forms an epithet.

Verified candidate: `X/X80/X80n1565.xml`, lb `0119b17–0119b18`: `學眾有長坐不臥。屹若株杌。天下謂之枯木眾也。` (“Among the students were those who sat for long periods without lying down, standing rigid like stumps; throughout the realm they were called the withered-tree assembly.”)

Follow-up: search `枯木眾`, `長坐不臥`, and parallel lamp witnesses. Add the label and distinguish it from `枯木堂` (“withered-tree hall”), `枯木禪` (“withered-tree Chan”), and the base image if those collocations prove independently useful.

### H8 — `t_d11d5f0c78a5` — 以心傳心, “transmitting mind by mind”

The entry covers transmission narratives, the contrast with verbal transmission, and a critical use. It misses an explicit historical self-definition that attributes the wording to Bodhidharma and embeds it in Huike's question about textual canons.

Verified candidate: `X/X64/X64n1276.xml`, lb `0808c16–0808c18`: `以心傳心者，是達磨大師之言也。因可和尚次問：此法有何文字教典習學？大師答云：我法以心傳心，不立文字。` (“As for ‘transmitting mind by mind,’ these are Great Master Bodhidharma's words. When Master Ke next asked, ‘What written canons are there for studying this teaching?’ the great master answered, ‘My teaching transmits mind by mind and does not set up written words.’”)

Follow-up: search `以心傳心者` and `我法以心傳心不立文字`; determine the text's source/quotation chain and include the attribution as a text claim, not as external history.

## Medium priority

### M1 — `t_970c3f191929` — 正法眼, “true Dharma eye”

The standalone article currently gives only possession/loss/brightening/blinding deployments. It omits a direct test question with the answer “a broken sand-pot,” distinct from the longer `正法眼藏` (“treasury of the true Dharma eye”) question already harvested in that compound's entry.

Verified candidate: `X/X82/X82n1571.xml`, lb `0132b13–0132b14`: `菴問：如何是正法眼？師遽答曰：破沙盆。` (“The hermitage master asked, ‘What is the true Dharma eye?’ The master immediately answered, ‘A broken sand-pot.’”)

Follow-up: search the standalone `如何是正法眼` while excluding the following graph `藏` (“treasury”); identify and compare its repeated case witnesses.

### M2 — `t_81147ad4e8bf` — 四料揀, “Four Selections”

The article already contains the four statements and a later grading gloss, so this is not a missing sense. It does, however, omit a compact later naming formula that explicitly calls the preceding case the Four Selections.

Verified candidate: `T/T47/T47n1998A.xml`, lb `0881b06–0881b07`: `這箇是適來上座請益底公案。謂之四料揀。` (“This is the case about which the senior monk just asked; it is called the Four Selections.”)

Follow-up: search `謂之四料揀`; add only if it contributes attribution or dating beyond the current `人天眼目` witness.

### M3 — `t_6edb551acb53` — 知解, “intellectual understanding”

The entry documents rebuke formulas well but omits the fixed compound `知解不消` (“intellectual understanding not digested”) and the text's immediate comparison to poison. This expands the morphological and evaluative range without requiring interpretation.

Verified candidate: `T/T48/T48n2012A.xml`, lb `0382c20`: `所謂知解不消。皆為毒藥。` (“What is called intellectual understanding not digested is all poison.”)

Follow-up: search `知解不消`, `食不消`, and the parallel `T48n2006` witness; record the food/digestion wording as the text's own comparison.

### M4 — `t_fd1759947989` — 大死, “great death”

The article mentions the rare compound “great death, great life” but omits an explicit formula equating it with passing the heavy barrier. This is useful but comes from one late explanatory witness and should not displace the much broader Zhaozhou–Touzi deployment.

Verified candidate: `X/X68/X68n1319.xml`, lb `0523c20–0523c21`: `是則名為透重關，名為大死大活者。` (“This is called passing the heavy barrier; it is called one who greatly dies and greatly lives.”)

Follow-up: search `名為大死`, `大死大活`, and `透重關`; include with explicit source attribution and provisional weight if independent witnesses do not support the equivalence.

### M5 — `t_87cc840b8f33` — 拄杖子, “staff”

The physical handling and major cases are well represented. The missing deployment is metalinguistic: masters repeatedly ask whether the object may be called a staff, say it must not be called a staff, or contrast calling/not calling it a staff. This is a distinct observable use of the noun, not another physical gesture.

Follow-up searches: `喚作拄杖子` (“call it a staff”), `不得喚作拄杖子` (“must not call it a staff”), `不喚作拄杖子` (“not call it a staff”), and `但喚作拄杖子` (“only call it a staff”). These are **search leads, not quoted KWIC candidates**; verify and curate a representative positive/negative pair before adding prose.

## Monitor / deliberate final search pass

These 21 entries showed no proved unique omission, but should receive an explicit §0f checklist before final release because a very broad corpus footprint is represented by a relatively small inventory, or because a legacy article remains unusually short:

- `t_8f7b20536cb6` 和尚 (“master”): check title morphology and address forms, especially “old master,” “great master,” post-name title, and ordination-specific use.
- `t_67bff0d0e5d3` 僧問 (“a monk asked”): sample anonymous, subsequently identified, quoted/raised, and compiler-narrative forms.
- `t_e4d6ebff1bb2` 如何是佛 (“what is Buddha?”): the substring nests inside longer questions; retain the current exclusion logic and check whether the three answers span the genuinely distinct standalone deployments.
- `t_1e3d3a5173a6` 回互 (“mutual interchange”): the current Stonehouse-style self-gloss is strong; review later `所謂回互` (“what is called interchange”) discussions only for genuinely new contrasts.
- `t_db4a932ce500` 大悟 (“great awakening”): re-run every `名為大悟` (“called great awakening”) hit and record explicit exclusions where the passage is outside the entry's grounded sense.
- High-frequency formula/gesture entries needing spread checks, exact IDs: `t_7180f7431520`, `t_4f7bd98ad40f`, `t_51fe593d9ffe`, `t_cc840e36f2da`, `t_f25cebd24730`, `t_6abcff898d95`, `t_9f119d7965c2`, `t_8bd6933e6de3`, `t_c945c2cc0e79`, `t_1d3473614976`, `t_0e7b683790e8`, `t_ada407625f42`, `t_2f4b60453d19`, `t_8f41e0da5a71`, `t_4e30d47a452c`, `t_ef39bdc0eb99`.

For this last group, low curated/raw ratios are not defects by themselves. The follow-up should stratify by deployment shape and period/genre rather than add random occurrences.

## Entries with no actionable depth defect found

The remaining 94 entries passed this audit's practical omission test. “Passed” means no missing high-value finding was demonstrated by the definition-formula, deployment, contrast, variant, morphology, or spread checks; it is not a claim that no future corpus reading can improve them.

`t_01a355a89ba1`, `t_041f65670cd4`, `t_07343f750f68`, `t_097f38f58678`, `t_0a686fa27769`, `t_0ad9fc2dfdda`, `t_0ed8638229a9`, `t_0f97bfab265c`, `t_121b66b78c9e`, `t_15026800437e`, `t_193632bffe7b`, `t_19705602b956`, `t_1c7d25824f85`, `t_1da939bf1267`, `t_1e41b014d80e`, `t_2069b9c33315`, `t_218e4815d84a`, `t_223c2f6ade25`, `t_26d1f4bf3890`, `t_2738431562e6`, `t_2852a9ae231c`, `t_2d4525b4b123`, `t_33d49f4710be`, `t_36aa29eb1287`, `t_37771a869b4f`, `t_3a0a4e68cf13`, `t_427fa502a11b`, `t_46c30c5d57d4`, `t_48e808f5d2a7`, `t_49829f59faac`, `t_49efe4fed8d4`, `t_4aa0ae72820e`, `t_4cc95950b59a`, `t_4ccf8aed47d3`, `t_4e10d7c80fbc`, `t_52391cba2cdf`, `t_53da4e346a6f`, `t_5d6035b1e800`, `t_5ddde30711a4`, `t_61c90d3a8edd`, `t_62044e7bbb87`, `t_66792ea088de`, `t_6b8e3b4f44bb`, `t_6da91f8ce284`, `t_7182bedf65d1`, `t_78f95517a347`, `t_7bd745af24d7`, `t_7c1991e9eabb`, `t_7d440e0d91b4`, `t_7efdfe4296c6`, `t_830700de49fb`, `t_831f84399d0b`, `t_84043ffcdf90`, `t_8650004bb9d7`, `t_882860247a9b`, `t_8879b278cd83`, `t_8a016f49e5b8`, `t_8ece09f6b91a`, `t_937f63a4fb51`, `t_93ab42fecdca`, `t_9a5dc768cbc5`, `t_ab6276be6e08`, `t_ac2e2908084d`, `t_ad0a8e5aac3d`, `t_adde034233ba`, `t_aef7434b8470`, `t_b291fe703ff1`, `t_b4a4ae6874d0`, `t_b8063e3d60b4`, `t_ba841f6e11c8`, `t_c728f3a8e02b`, `t_c891f0944482`, `t_ccd48e1c9145`, `t_cd14935fc028`, `t_ce2a5ef71afe`, `t_cf0513be4012`, `t_d03aa9267f79`, `t_d190cf45c531`, `t_d35dc9e3723e`, `t_d4661c1b4dbb`, `t_d69c18a98053`, `t_d7167b5f3236`, `t_dab856504b69`, `t_dc02eefd07f5`, `t_e6eb14b6c1ca`, `t_e84753568cda`, `t_ea138c7335d3`, `t_ebb0995c99fc`, `t_ed962dfd1158`, `t_edabab064644`, `t_f2181872b682`, `t_f6dadadcbef5`, `t_f7bdd2def0ec`, `t_ff50c6974a36`.

## Verified-candidate ledger

All quoted candidates in this report were verified during this audit:

| Term ID | Source | lb | `zc.verify(...).ok` |
|---|---|---:|---:|
| `t_c13928184189` | `T/T51/T51n2076.xml` | `0231a21–0231a22` | `True` |
| `t_1a7e251bda53` | `X/X71/X71n1409.xml` | `0114b11–0114b12` | `True` |
| `t_16140def874d` | `X/X70/X70n1403.xml` | `0776a01–0776a02` | `True` |
| `t_c1af3ecba987` | `T/T47/T47n1998A.xml` | `0915b22–0915b24` | `True` |
| `t_1d3706324b0c` | `T/T48/T48n2003.xml` | `0180a02–0180a03` | `True` |
| `t_326be1e9c98a` | `X/X80/X80n1565.xml` | `0119b17–0119b18` | `True` |
| `t_d11d5f0c78a5` | `X/X64/X64n1276.xml` | `0808c16–0808c18` | `True` |
| `t_970c3f191929` | `X/X82/X82n1571.xml` | `0132b13–0132b14` | `True` |
| `t_81147ad4e8bf` | `T/T47/T47n1998A.xml` | `0881b06–0881b07` | `True` |
| `t_6edb551acb53` | `T/T48/T48n2012A.xml` | `0382c20` | `True` |
| `t_fd1759947989` | `X/X68/X68n1319.xml` | `0523c20–0523c21` | `True` |

## Bottom line

The principal risk is no longer bad anchors or uniformly thin entries. It is **selective under-harvesting**: an article can be long and accurate yet still omit the corpus's second explicit definition or its strongest corrective witness. The eight high-priority items should be repaired before treating the first 128 as having passed §0f. The 21 monitor entries need deliberate checklists, not automatic expansion.
