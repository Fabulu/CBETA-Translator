# #0f.8 sense audit — ID bands t_5… through t_9…

Report only; no entry was rewritten. Audit date: 2026-07-13.

## Scope and test

- Audited all 105 `STATUS=done` entry directories whose ID begins `t_5`, `t_6`, `t_7`, `t_8`, or `t_9`.
- Re-read `DICTIONARY_ENTRY_GUIDE.md` §5, depth-gate items 7–8. A candidate is reported only where the corpus may use the exact headword for a different referent, object, or word-class—not merely where an annotator could give the same use different readings.
- Checked every `PreferredTarget` for the semicolon smell. The only hits in this band are `序` (“rank; division”, “sequence; order”), `思量` (“ponder; think over”), and `休去歇去` (“rest; cease”). The last two are synonymic English renderings of one act, not split evidence. `序` already has three correctly separated senses.
- Inspected current senses, explanations, and curated KWICs, then checked compound/title/person overlaps against the allowlisted corpus paths. Raw XML searches below were restricted to `Assets/Data/zen-corpus.json`; apparatus hits were not used as decisive anchors.

## High-confidence candidates

### 1. `佛` (`t_57145bc70a8d`) — person/title vs physical image

Current state: one corpus-wide sense, “buddha,” despite the explanation and 11 witnesses covering the historical Buddha, generic buddhas, answers to “what is buddha?”, and hostile/negative formulas.

The missed different THING is a buddha image. The allowlisted corpus has 635 raw `木佛` matches in 180 files, with unambiguous grammar such as `丹霞於慧林寺遇天大寒，取木佛燒火向` (“Danxia, meeting severe cold at Huilin Monastery, took a wooden buddha and burned it for warmth”), `何得燒我木佛` (“how can you burn my wooden buddha?”), and `木佛何有舍利` (“how could a wooden buddha have relics?”). An allowlisted case heading explicitly equates the shortened form with the object: `丹霞燒佛` with the note `佛像` (“Danxia burns the buddha”; “buddha image”), X67n1309 at 0268a11; the case body then says he took a wooden buddha. Other corpus formulas enumerate material images: `金佛不度爐，木佛不度火，泥佛不度水，真佛內裏坐` (“a gold buddha does not pass through the furnace; a wooden buddha does not pass through fire; a clay buddha does not pass through water; the true buddha sits within”).

Recommendation before rewriting: split at least (a) buddha / the Buddha as a person or title and (b) a buddha image/statue. Anchor the image sense with the shortened `燒佛` passage that the text itself annotates `佛像`, not merely with a compound substring. Re-test whether “the Buddha” (Śākyamuni) versus “a buddha” is grammatical individuation inside one title/category rather than a third sense; present evidence does **not** yet require that third split.

### 2. `正法眼藏` (`t_8ece09f6b91a`) — transmitted “treasury” vs book title

Current state: one sense, “the treasury of the true eye of the teaching.” Its prose uses `重刻正法眼藏序` (“Preface to the Recut *Treasury of the True Eye of the Teaching*”) as if it were only evidence for the transmitted object.

The title use is mechanically visible. X67n1309 has a TEI division/head at 0556a02–03: `重刻正法眼藏序` (“Preface to the Recut *Treasury of the True Eye of the Teaching*”). X86n1607 likewise cites `慧正法眼藏序` as a named preface. This is the item-8 “word vs title” family: the inherited/transmitted `正法眼藏` in `吾有正法眼藏…付囑摩訶迦葉` is not the same referent as a bound/edited work that can be recut and prefaced.

Recommendation: split (a) the transmitted treasury/eye of the teaching, primary, from (b) *Zhengfa Yanzang* / *Treasury of the True Eye of the Teaching*, the work title. Anchor the title sense in the actual heading and identify the work/edition carefully; do not infer that every occurrence in a preface is automatically the title.

### 3. `三關` (`t_5e59b126e608`) — generic three checkpoints vs distinct named masters’ sets

Current state: one corpus-wide sense, “three checkpoints,” whose explanation itself says the expression names several different explicitly enumerated sets. Its five witnesses include Huanglong Huinan’s three questions, Doushuai Congyue’s different three questions, Wansong’s roster of still other masters’ threes, and the separate idiom `一鏃破三關` (“one arrow pierces three checkpoints”).

This is not alternate interpretation of one set. `黃龍三關` (“Huanglong’s three checkpoints”) is repeatedly a case heading and has the fixed Buddha-hand / donkey-foot / birth-circumstance referent; `兜率三關語` (“Doushuai’s three checkpoint sayings”) denotes the different see-nature / escape-birth-and-death / destination set. The entry already quotes the passages that prove the referents differ. The situation closely matches item 8’s mandated `三句` family, where Yunmen’s three and Linji’s three are separate senses.

Recommendation: preserve a corpus-wide generic “three checkpoints/barriers” sense, then split the named sets at least for Huanglong and Doushuai if the model treats recurrent named taxonomies consistently with `三句`. Do not create a new sense for every accidental set of three. Separately adjudicate `一鏃破三關`: it may be the ordinary countable barrier noun rather than another named taxonomy.

## Medium-confidence candidates requiring targeted confirmation

### `尊宿` (`t_7887dc8d449f`) — venerable elder vs component of a work title

The person/title sense is well supported. The same exact graphs are also embedded in the stable bibliographic title `古尊宿語錄` (*Recorded Sayings of the Ancient Venerable Elders*), the title of allowlisted C077n1710, D48n8939, and X68n1315 and repeatedly the object of verbs such as read, publish, and recut. Item 8 names word-vs-title as a split family, but here `尊宿` is only one component inside a longer title; splitting the shorter entry may duplicate a future `古尊宿語錄` entry. Confirm the project policy on title components. Preferred resolution: keep `尊宿` as the person-role and create/link the full work-title entry, unless exact standalone shorthand `尊宿` demonstrably names the book.

### `一著` (`t_549e7766dfa1`) — move noun vs segmentation/verb noise

The current five anchors consistently show the countable “move” noun (`放過一著`, `末後一著`, `向上一著`, `一著子`). The stated 3,121-hit count is vulnerable to unsegmented matches in strings such as `一著衣` (“once [he] puts on clothing” / “one garment,” depending syntax), where `著` is a verb or classifier rather than the move noun. This may expose a second word-class, or merely show that the raw exact-string concordance is not a word segmenter. Sample `一著衣` and other high-frequency right neighbors before preserving the count. Split only if multiple independent passages use the exact two-graph unit as a stable different thing; otherwise correct the count/deployment inventory, not the senses.

### `理事` (`t_9bdac4a01636`) — coordinate pair vs “manage affairs”

The current five anchors all establish the coordinate nouns “principle and affairs.” Wider Chinese permits verbal `理事` (“manage affairs”) and agent compounds such as `理事人` (“person managing affairs”). The allowlist-filtered first pass did not yield a clean independent Chan anchor before the reporting cutoff; apparent `料理事人` hits located outside the allowlist cannot establish a sense. Run exact allowlist searches for `理事人`, `理眾事`, `料理事`, and syntactic objects. If found across Zen records, this is a different word-class and should split. Until then it remains a candidate, not a defect.

### `須彌` (`t_5d84cccab8df`) — mountain vs misleading compound families

The entry’s mountain name is coherent. Searches surface `須彌座` (“Sumeru pedestal/seat”) and `須彌燈王` (“Sumeru-Lamp King [Buddha]”), both different referents, but they are longer lexical compounds rather than uses of standalone `須彌` for those objects/persons. Treat these as related-entry candidates, not a split, unless standalone ellipsis is found. Current evidence is a likely false positive.

## Existing multi-sense entries: adjudication

- `湊泊`: **clean**. “Find an approach/get a purchase” and literal “come together temporarily” are different deployments/referents, separately anchored.
- `轉身`: **clean**. Figurative/interactional “turn oneself around” and bodily turning are separately anchored; retain only while the first remains an observable deployment rather than an interpretive gloss.
- `無相`: **clean and exemplary** word vs person/title split: “without marks” versus Master Wuxiang.
- `衣鉢`: **clean and exemplary** object vs succession token.
- `賓主`: **not a merge defect in its current form**. It now carries “guest and host” and “the four guest-and-host [relations].” The second is a named four-member Linji taxonomy, not the formerly reported paraphrase “guest and host (interchanging).” Retain if its separate anchors remain taxonomy-specific.
- `序`: **clean**. Institutional rank/division, textual preface, and sequence/order are three different things/word-classes. Semicolons inside senses are English synonym pairs, not hidden splits.
- `三句`: **clean and exemplary** generic vs Yunmen-specific vs Linji-specific named sets.
- `和尚`: **clean after enrichment**. Teaching/presiding master and ordination preceptor are separately anchored. Direct address is correctly kept in the first sense because address syntax changes no referent.
- `保任`: **clean on present evidence**. “Guard and maintain” and “vouch for/guarantee” are different verbal relations, separately anchored.

## Semicolon detector adjudication

- `思量` — “to ponder; think over”: **one thing stated twice; do not split**. The cases `思量箇不思量底` and `非思量` negate/contrast the same verb and do not manufacture another referent.
- `休去歇去` — “rest; cease”: **one doubled imperative, not two senses**. The headword itself coordinates near-synonymous commands; criticisms and contrary formulas are different attitudes toward the same command, hence readings/deployments rather than things.
- `序` — already split correctly as described above.

## Clean remainder / no reportable split found

The following entries showed one coherent referent or construction after entry/family inspection. “Clean” means no item-8 split candidate was found in this audit, not that depth, English, counts, or #0g prose were exhaustively certified:

`作麼生`, `三玄三要`, `行腳`, `百尺竿頭`, `孤明`, `放行`, `山河大地`, `客塵`, `寸絲不掛`, `狗子無佛性`, `本性`, `露地白牛`, `寶鏡三昧`, `金鎖玄路`, `野狐禪`, `兼中到`, `本分事`, `波羅提木叉`, `昏沈`, `漸修`, `單傳`, `全機`, `非心非佛`, `冷暖自知`, `拈古`, `僧問`, `騰騰任運`, `良久`, `照用`, `淨裸裸`, `銀山鐵壁`, `一行三昧`, `情識`, `迷頭認影`, `知解`, `不落因果`, `恁麼則`, `恁麼`, `下語`, `咄`, `本心`, `明暗`, `付法`, `釋迦老子`, `生死事大`, `動念即乖`, `格外`, `宗風`, `嗣法`, `任運`, `公案`, `父母未生前`, `綱宗`, `拈提`, `四料揀`, `擒縱`, `未在`, `本地風光`, `具眼`, `淨瓶`, `兼中至`, `拄杖子`, `惺惺`, `便打`, `漏逗`, `法嗣`, `一喝`, `把定`, `一口吸盡西江水`, `意旨如何`, `參請`, `盡大地`, `參堂`, `目前`, `本來無一物`, `體露`, `一物`, `持戒`, `律師`, `正法眼`, `阿難`, `宗師`, `拈華微笑`, `平常心是道`, `一歸何處`, `沒蹤跡`, `拂子`.

Notable false positives among the clean group:

- `寶鏡三昧` is consistently the title/name of Dongshan’s transmitted text; “what is the Complete Command of the Precious Mirror?” asks about that named item and does not by itself establish a second thing.
- `公案`’s government case-file is the corpus’s explicit source comparison/self-definition for the Chan public case, not necessarily a second live referent in the audited occurrences. Split only if independent allowlisted passages actually use `公案` for a contemporary secular lawsuit/file.
- `法嗣`, `嗣法`, and `付法` differ by grammatical construction across overlapping entries, but each audited entry is internally coherent; do not split merely to harmonize English.
- `一喝` remains one countable vocal act even where Linji says a shout is “not used as a shout.” That line classifies deployment and does not prove a different object.
- `正法眼` and `正法眼藏` are overlapping but distinct headwords. The longer term’s book-title defect does not automatically create a title sense for standalone `正法眼`.
- `阿難`, `釋迦老子`, `律師`, and `波羅提木叉` showed no word/title or precept-family hidden split in their current anchored uses.

## Rewrite order proposed

1. `佛`: confirm and anchor the shortened statue/image use, then split.
2. `正法眼藏`: identify the recut work precisely and split word vs work title.
3. `三關`: apply the same named-set policy already used for `三句`.
4. Targeted searches only for `一著` and `理事`; do not rewrite unless the different word-class survives allowlist and segmentation checks.
5. Resolve full-title policy before changing `尊宿`; likely add/link `古尊宿語錄` rather than overloading the component entry.
