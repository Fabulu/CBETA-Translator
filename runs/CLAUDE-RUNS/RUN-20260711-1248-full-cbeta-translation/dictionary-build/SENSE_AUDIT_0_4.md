# Sense audit: done entries `t_0…` through `t_4…`

Report-only audit under `DICTIONARY_ENTRY_GUIDE.md` §5 #0f.8. No entry was edited. I scanned all 129 `STATUS=done` directories whose ID suffix begins with hexadecimal 0–4, inspected every sense/target/explanation/curated witness, ran the semicolon-target detector, and checked likely title/person and overlapping-family forms against the allowlisted corpus. Counts below are exact-string leads from the 462-file allowlist; because raw XML markup can interrupt a phrase, they are conservative rather than inflated whole-canon counts.

## High-confidence split candidates

### `善知識` — role noun vs. direct address

The present single sense explicitly joins two different grammatical/referential uses: “a good teacher” and the vocative “good friends.” These are not competing readings of the same occurrence.

- Role/person: Dahui defines the office, `夫稱善知識者。引導一切眾生。令見佛性` (“those called good teachers guide all beings and cause them to see buddha-nature”), and the entry also anchors `善知識本分合做底事` (“the proper work a good teacher ought to do”) and `參善知識` (“visit good teachers”).
- Address: Huineng says `善知識！一行三昧者…` (“Good friends! As for complete command of the single conduct…”). Exact `善知識！` occurs at least 156 times in the allowlisted XML. Here the addressees are the assembly, not 156 assertions that each listener occupies the teacher role.
- The entry itself correctly observes that the vocative “does not identify every listener as an officeholder”; that sentence is the decisive proof that one bucket currently contains two referents/functions.

Recommended structure: primary corpus-wide “good teacher / good guide”; second corpus-wide vocative “good friends!” Anchor the definition/work/visiting evidence under the first and a Huineng assembly address under the second.

### `三昧` — word vs. named person/title

The existing sense treats the phonetic word in compounds such as `一行三昧`, `海印三昧`, `法性三昧`, and `自受用三昧`. The corpus also contains `三昧和尚` (“Master Sanmei”) at least 7 times, including a section heading `<head>三昧和尚</head>` in `J/J28/J28nB206.xml` at line 801. In those occurrences `三昧` is the identifying name/title of a person, not “complete command” predicated of a state or domain.

This is the guide's explicit word-vs-title/person family. Add a provisional person/name sense only after checking whether the seven strings resolve to one named master and anchoring the biographical/heading witness; do not distribute that proper-name sense across ordinary compounds.

### `師子` — lion vs. the Twenty-fourth Patriarch Siṃha

The existing single sense is the animal “lion,” with lion cub (`師子兒`), lion's roar (`師子吼`), and the lion exerting its strength against rabbit and elephant. Separately, exact `師子尊者` (“Venerable Siṃha”) occurs at least 178 times in the allowlisted XML. Examples are unambiguous person records:

- `第二十四祖師子尊者，中印土人` (“The Twenty-fourth Patriarch, Venerable Siṃha, was a man of central India”), `B/B25/B25n0144.xml`, line 2498.
- `罽賓國王斬師子尊者頭` (“the king of Kashmir cut off Venerable Siṃha's head”), `J/J32/J32nB273.xml`, line 316.

An animal cannot be the twenty-fourth patriarch, have a biography, or be beheaded as this named person. Split a proper-person sense from the animal/metaphor sense and anchor both. `師子座` (“lion seat”) and `師子吼` (“lion's roar”) by themselves remain deployments of the lion word, not additional things.

## Medium-confidence candidates requiring focused adjudication

### `法身` — teaching-body vs. `法身佛` title/entity

The current entry has one “teaching-body” sense and already cites divergent predicates. Exact `法身佛` occurs at least 82 times in the allowlisted XML, including `法身佛是名清淨法身毘盧遮那佛` (“the teaching-body buddha is named the pure teaching-body Vairocana Buddha”) in `C/C077/C077n1710.xml`, lines 5698–99, and enumerations contrasting `法身佛、報身佛、應身佛` (“teaching-body buddha, reward-body buddha, response-body buddha”). This may designate a buddha/entity rather than the body/standard itself.

Do not split merely because a compound exists: first sample standalone predication and the `法身佛` family to decide whether `法身` is independently the title/referent or only a modifier inside a separate compound. If the latter, keep one sense and create/cross-link a future `法身佛` article instead.

### `主人公` — personified self-address vs. ordinary one-in-charge

The draft concentrates on Ruiyan's self-call (“Master!”), Zhaozhou's “master-in-charge,” and definitions equating it with one's own self/mind. It also begins with the ordinary “host/master of a house or one in charge,” but provides no curated occurrence for an actual household/organizational owner. Exact `主人公` is common (at least 684 raw-XML matches), and `作主人公` (“act as/be the person in charge”) occurs at least 5 times.

This is a candidate only if focused sampling finds a concrete external proprietor/person-in-charge distinct from the personified self-address. Different modern interpretations of Ruiyan's call do not qualify. At present the second thing is asserted etymologically, not demonstrated by the saved evidence.

## Semicolon detector adjudication

- `血脈` — **not yet a split.** Target: “bloodline; connective thread.” All five curated witnesses concern continuity of question/answer, phrases, succession, or house style. Targeted allowlist checks found `血脈不斷` at least 28 times, `血脈流通` 3, and `血脈貫通` 5, but no exact `身中血脈` (“blood vessels in the body”) or `一身血脈` (“one body's blood vessels”). “Connective thread” currently paraphrases the corpus's figurative bloodline rather than naming a second attested object. Either simplify the target or find and anchor a genuinely anatomical witness before splitting.
- `蹉過` — **clean, one sense.** “to slip past; to miss it, to let it slip by” is a set of English renderings of one verb/action. The occurrences (`當面蹉過`, `早是蹉過`, `蹉過了`) do not change referent or word-class.
- `現成` — **clean, one sense.** “ready-made; already there, already complete” is paraphrase variation for the same adjectival/result state. `現成公案`, `觸處現成`, and `一切現成` change the modified noun/context, not the thing denoted by `現成`.

## Other inspected families: clean or already split

- Already correctly split under #0f.8: `傳燈` (action vs. lamp-record title), `老僧` (self-reference vs. an old monk), `向上` (Zen further-up vs. spatial upward), and `雲水` (itinerant monks vs. clouds/water). Their occurrences are assigned to different referents/usages rather than interpretive menus.
- `一句`: first phrase, last phrase, a phrase never told, and a tongue-stopping phrase are all one unit of utterance in different named/deictic contexts. The four exact `一句經` strings are a compound lead, not proof that `一句` changes thing.
- `宗門`: institution/lineage gate and its appearance inside a record title are not independently shown as two meanings of the headword. Exact `宗門錄` was found only once. The draft's decision not to split title context stands unless a work is actually called bare `宗門`.
- `燈錄`: named individual lamp records and the collective lamp-record family are instances of the same textual object class, not word vs. title in the `傳燈` sense.
- `五家`: the five named Chan houses, their lineages, purports, and records are the same five-part institutional grouping. `五家七宗` expands the enumeration but does not change what `五家` denotes.
- `棒喝`: object/action ambiguity belongs chiefly to standalone `棒`; within the fixed pair, the saved corpus consistently deploys blows/stick and shouts together. Audit standalone `棒` separately (already identified outside this ID range); do not manufacture parallel senses in `棒喝` without passage-level grammar.
- `木佛`: statue, the Danxia burning case, and “wooden buddha does not pass through fire” all denote a wooden buddha image. No person/title use was found in the entry.
- `善知識` is the only assigned-range entry where the prose itself expressly admits that its two uses have different referents; it should be the first rewrite after candidate review.

## Rewrite priority after approval

1. `善知識`, `師子`, `三昧` (directly proven different things).
2. Focused concordance adjudication for `法身`/`法身佛` and `主人公`.
3. Target cleanup, but no forced split, for the three semicolon false positives.

