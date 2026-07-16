# b010 Batch A — research and draft report

Scope was limited to the five assigned term directories. No status file, manifest, wave plan, guide, termbase, other term, or translated XML was touched.

## Results by term

### 三關 — “three checkpoints” (`t_5e59b126e608`)

- Corpus: 578 hits in 210 allowlisted texts.
- Structure: one corpus-wide, multi-source sense; five curated occurrences in three source texts.
- Sources: *Record of the Lamp Published in the Jiatai Era*, *Wansong’s Comments on Tiantong’s Ancient Cases in the Record of Further Inquiry*, and *Strict Lineage of the Five Lamps*.
- Depth harvest: Huanglong Huinan’s three questions; Doushuai Congyue’s complete three questions; Wansong’s explicit definition broadening the label to several masters’ three sayings and three mysteries; the Qinshan “one arrow” public case; Yunmen’s self-supplied “one arrow pierces three checkpoints” answer.
- Collocations counted: “Huanglong’s three checkpoints” (黃龍三關), 139/87; “Doushuai’s three checkpoints” (兜率三關), 5/3; “one arrow pierces three checkpoints” (一鏃破三關), 117/69; “three checkpoint sayings” (三關語), 71/35.
- Ambiguity resolved: the texts do not supply one universal list. The article describes a family of named threefold sets and does not key the sense to Huanglong merely because his set is prominent.

### 玄旨 — “mysterious purport” (`t_23204fbd253c`)

- Corpus: 564 hits in 162 allowlisted texts.
- Structure: one corpus-wide, multi-source sense; six curated occurrences in four source texts.
- Sources: *Jingde Record of the Transmission of the Lamp*, *Linked-Lamp Compendium*, *Compendium of the Five Lamps*, and *Strict Lineage of the Five Lamps*.
- Depth harvest: Guizong’s extended answer; Zhaozhou’s “money hanging on the wall”; Daoqin’s wording-based reply; Jiashan’s reciprocal tongue statement; the Inscription on Trusting Mind’s “not recognizing the mysterious purport”; and a biographical “greatly awakened to the mysterious purport” deployment.
- Counts: “What is the mysterious purport?” (如何是玄旨), 39/23; “not recognizing the mysterious purport” (不識玄旨), 48/35; “greatly awakened to the mysterious purport” (大悟玄旨), 19/14; “realized the mysterious purport” (領悟玄旨), 6/6.
- Ambiguity retained: masters give different answers to the same question formula. The article records the answers without imposing one explanatory force.

### 保任 — “guard and maintain; vouch for” (`t_91d84c849fc7`)

- Corpus: 517 hits in 180 allowlisted texts.
- Structure: two corpus-wide senses. The primary “guard and maintain” sense is multi-source with five curated occurrences in three texts. The transitive “vouch for” sense is provisional with one curated occurrence; its second corpus witness repeats the same exchange.
- Sources: *Jingde Record of the Transmission of the Lamp*, *Extended Record of Yongjue Yuanxian*, *Tiansheng Expanded Lamp Record*, and *Linked-Lamp Compendium*.
- Depth harvest: Baizhang’s cattle-herder comparison; Guizong’s eye-film answer; “Take care!” as an answer; Yongjue’s unique explicit definition; Deshan Zhixian’s correction “guarding and maintaining is itself wrong”; and Damei’s assurance construction.
- Counts: “How to guard and maintain?” (如何保任), 158/89; “from beginning to end, how to guard and maintain?” (始終如何保任), 12/11; “throughout the day, how to guard and maintain?” (時中如何保任), 9/9.
- Attribution safeguard: Deshan Zhixian was deliberately left unlinked instead of being confused with the roster’s Deshan Xuanjian.
- Ambiguity retained: the recorded answers conflict at the surface level; the article reports each and does not harmonize them.

### 言下大悟 — “greatly awakened at those words” (`t_4b1991d604f8`)

- Corpus: 542 hits in 160 allowlisted texts.
- Structure: one corpus-wide, multi-source narrative-formula sense; six curated occurrences in four texts.
- Sources: *Linked-Lamp Compendium*, *Collected Ancient Cases of the Chan Lineage*, *Continued Record of the Transmission of the Lamp*, and *Complete Book of the Five Lamps*.
- Depth harvest: distinct preceding speech in the Daoxin, Damei, Wuxie, Yanyang, Shoushan-lineage, and Huanglong records; distinct aftermaths including continued attendance, breaking a staff, a bow and verse, and a spoken response.
- Related formulas counted: “awakened at the words” (言下契悟), 41/28; “suddenly awakened at the words” (言下頓悟), 65/40; “at the words, suddenly greatly awakened” (言下豁然大悟), 10/9; “thoroughly penetrated at the words” (言下大徹), 34/31; “had an insight at the words” (言下有省), 215/89.
- Self-definition search: none found; the phrase is consistently a narrative formula, not a term the texts stop to define.
- Attribution safeguard: speaker and person reported awakened were kept distinct in every note.

### 大用現前 — “great function appears” (`t_0b8912312b92`)

- Corpus: 447 hits in 170 allowlisted texts.
- Structure: one corpus-wide, multi-source sense; six curated occurrences in four independent texts.
- Sources: *Jingde Record of the Transmission of the Lamp*, *Linked-Lamp Compendium*, *Dahui Pujue’s Record*, and *Blue Cliff Record*.
- Depth harvest: the unique direct equivalence with “the unequalled dharma-body”; Changqing’s verbal answer; Yunmen’s staff action and call; Fengxue’s assembly address; Yuanwu’s blade-of-grass/sixteen-foot-golden-body pair; and Dahui’s explicit rejection of applying the label to the Guizong snake and Nanquan cat cases.
- Counts: “great function appears, not retaining rules” (大用現前不存軌則), 21/12 without intervening punctuation; “What is great function appearing?” (如何是大用現前), 22/19; “at the encounter, great function appears” (臨機大用現前), 2/2.
- Ambiguity retained: question responses vary. Dahui’s corrective witness is foregrounded so two named public cases are not silently made definitions.

## Final validation

- JSON/schema: 5/5 parse; exact PascalCase schema keys; deterministic ID hashes match directory IDs.
- Corpus/source gate: every cited path is allowlisted; every `SourceTexts` path attests its entry’s headword.
- Occurrence gate: 29/29 curated KWICs return `zc.verify(...).ok == True`; all saved `FromLb`/`ToLb` values match current `zc`; all 29 contain their headword.
- Roster gate: every non-null sense/occurrence/related master value is an exact canonical roster spelling.
- Prose gate: the strict local scan found zero imported-framing or interpretive-language flags. Chinese evidence in prose is accompanied by an English translation; bare evidence remains in `Kwic`.
- Depth gate: each `WORK.md` records definition-formula searches, deployment shapes, contrasts/corrections, collocations or variants, exclusions, and a final omission audit.
- No unresolved blocker. The only retained uncertainties are explicitly described attribution gaps where no safe canonical roster value exists.
