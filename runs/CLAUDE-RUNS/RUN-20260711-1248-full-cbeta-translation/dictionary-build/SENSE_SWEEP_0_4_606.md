# Item-8 sense sweep: IDs `t_0*` through `t_4*` (606-entry snapshot)

Date: 2026-07-13  
Mode: read-only; candidates reported before any rewrite  
Rule applied: `DICTIONARY_ENTRY_GUIDE.md` §5 #0f.7–8

## Scope and result

- Ground-truth population: every `STATUS=done` entry whose ID begins `t_0` through `t_4`.
- Assigned done entries: 398.
- Assigned single-sense entries inspected: **180**.
- Actionable split candidates: **7**.
- Single-sense entries not presently supported for a split: **173** (including the watchlist and detector false positives below).
- No `entry.v2.json`, `STATUS`, termbase, or integration file was changed.

The sweep used `maintenance/depth-sense-gate.json` as a detector, then independently inspected each assigned single-sense entry's target, explanation, note, occurrence grammar, and term family. Broad-frequency flags were treated as review prompts, not split evidence. Candidate passages below were independently checked with `zc.verify` under `PYTHONIOENCODING=utf-8`; every quoted string returned `ok: true` and the anchors shown.

## Actionable candidates

### 1. 血脈 (`t_21f09b3726e7`) — confirmed

The semicolon currently hides two referents: an inherited family bloodline and the connective/transmissive line in Chan speech and succession. The latter is built from the former but is not the same thing.

- Family bloodline: `J/J34/J34nB311.xml`, `0594c08–0594c09`: `父子血脈相承、本無斷絕`.
- Chan connective line: `T/T48/T48n2003.xml`, `0150a02–0150a03`: `古人自是血脈不斷。所以道。問在答處。答在問處。`

Rewrite requirement: split and anchor both. A bodily blood-vessel sense should be added only if a separate allowlisted witness is harvested; the current article mentions medical prose but does not anchor it.

### 2. 上堂 (`t_4f7bd98ad40f`) — confirmed

The entry explicitly merges the physical/institutional act of ascending the hall or taking the teaching seat with the resulting formal address/observance. Item 8 treats the act and the discourse event/product as different things, not different readings of one thing.

- Act of ascending/taking the seat: `T/T47/T47n1985.xml`, `0496b14`: `府主王常侍與諸官請師升座，師上堂，云：`
- Completed formal address/observance: `J/J10/J10nA158.xml`, `0021b02–0021b03`: `師舉手揖云：「已為大眾上堂了也。」便歸方丈。`

Rewrite requirement: split “ascend/take the teaching-hall seat” from “formal teaching-hall address/observance.”

### 3. 評唱 (`t_10ca0857a11b`) — confirmed

The explanation already states both a verb and a resulting genre noun. Appraising/commenting is an activity; an appraisal-commentary or commentary collection is its product.

- Activity: `T/T48/T48n2003.xml`, `0140a10–0140a11`: `師住澧州夾山靈泉禪院評唱雪竇顯和尚頌古語要`.
- Resulting collection: `X/X73/X73n1449.xml`, `0071b18–0071b19`: `由機緣而頌古作，由頌古而評唱集`.

Rewrite requirement: split the verb from the commentary/collection noun and retain book-title evidence under the product sense.

### 4. 著語 (`t_0a686fa27769`) — confirmed

The single article presently uses the noun target “attached words” while its evidence alternates between the operation of attaching a comment and the attached comment itself.

- Commenting operation: `T/T48/T48n2003.xml`, `0144a07`: `雪竇著語云。勘破了也。`
- Resulting comment/device: `T/T48/T48n2003.xml`, `0144a08`: `眾中謂之著語。`

Rewrite requirement: split “attach a comment” from “attached comment/capping comment.” This is not a split among differing English renderings of the same comment.

### 5. 示眾 (`t_1a7e251bda53`) — confirmed

The current entry expressly merges two different public acts: physically showing an object to the assembly and verbally addressing/instructing the assembly.

- Physical display: `X/X79/X79n1557.xml`, `0014a06`: `世尊在靈山會上，拈花示眾，眾皆默然`.
- Verbal public address: `X/X79/X79n1557.xml`, `0024c11`: `示眾云：諸善知識，汝等各各靜心，聽吾說法。`

Rewrite requirement: split “show/display to the assembly” from “address/instruct the assembly.” The latter may also function as a discourse heading, but heading and spoken address need not be split again unless the corpus establishes a separate textual product.

### 6. 君臣 (`t_2069b9c33315`) — confirmed

The entry defines only the Caodong technical deployment, but the allowlisted corpus also uses the same word for ordinary political/ethical roles. A sovereign and minister are persons in a social relation; the Caodong lord/minister configuration is a technical rank schema.

- Ordinary social relation: `J/J37/J37nB392.xml`, `0580c26–0580c27`: `父子有親，君臣有義，夫婦有別，長幼有序，朋友有信。`
- Caodong technical schema: `T/T47/T47n1987A.xml`, `0527a10–0527a12`: `君為正位。臣為偏位。臣向君是偏中正。君視臣是正中偏。君臣道合是兼帶語。`

Rewrite requirement: split corpus-wide lord-and-minister usage from the Caodong technical configuration; the technical sense is house-specific, not the ordinary sense.

### 7. 鐵牛 (`t_20a56b9c1026`) — confirmed

The article fuses a named cast landmark/object with a figurative “iron-ox mechanism” used to describe the patriarchal mind-seal. Impossible actions predicated of the Shaanfu iron ox remain deployments of the landmark, but the explicitly comparative mechanism is a different referent.

- Shaanfu cast landmark: `X/X78/X78n1556.xml`, `0646c20–0646c21`: `問：如何是學人轉身處？師云：陝府灌鐵牛。`
- Figurative mechanism: `X/X80/X80n1565.xml`, `0230b06–0230b07`: `祖師心印。狀似鐵牛之機。去即印住。住即印破。`

Rewrite requirement: split the Shaanfu iron ox from the iron-ox mechanism/comparison. Do not proliferate further senses merely because the same landmark is made to move, swallow, or receive water.

## Detector adjudication and false positives

### Semicolon detector

Five assigned single-sense targets contained semicolons.

- **血脈** — true hidden split; see candidate 1.
- **蹉過** — false positive. “Slip past,” “miss it,” and “let it slip by” describe the same failure-to-catch/pass event; no second object or act was established.
- **粥飯** — false positive. “Gruel and rice” names the foods; “the monastery's regular meals” describes those foods in their institutional setting, not another thing.
- **粥飯僧** — false positive. The literal compound and its English unpacking denote the same monk.
- **現成** — false positive. “Ready-made,” “already there,” and “already complete” are competing readings of present availability/completeness, not distinct referents.

### High-risk terms inspected but not supported for a split now

- **下座 / 便下座 / 歸方丈**: formal closure, voluntary departure, and being pulled down preserve the same seat/departure action.
- **代語**: the harvested headword constructions establish supplying a response; the submitted response itself is normally named `下語`, so an act/product split for bare `代語` was not proved.
- **分別**: positive “distinguish things well” and negative “do not arouse discrimination” evaluate the same distinguishing operation differently.
- **主人公**: the etymological house-master image and Ruiyan's master-in-charge language do not by themselves establish a separately used literal headword sense in the harvested witnesses.
- **落處**: physical source imagery and “where a saying/case lands” remain a watch item, but the current evidence set did not independently anchor bare `落處` as a concrete landing site.
- **門庭**: architectural source image versus teaching-house frontage is a serious watch item, but all curated witnesses are already institutional/figurative; no separate literal occurrence is anchored yet.
- **鳥道**: landscape imagery versus Dongshan's road is a serious watch item, but the current entry's witnesses do not prove a separately used ordinary route sense.
- **五家**: reviewed for “five households” versus the five Chan houses; the harvested family is consistently the Chan grouping.
- **出身處**: geographic answers and puns occur inside questions about a student's or buddhas' place to emerge; they do not alone create a separately attested birthplace/provenance sense.
- **解制**: calendar event, heading, and personal extension all remain explicitly tied to releasing the restriction period; no independent generic “remove a rule” sense was established.
- **經行**: all valid occurrences inspected denote walking about; apparent “sutra circulation” parses are segmentation noise, not a second sense.
- **燈錄**: individual titles and the compilation family are instances of the same record genre.
- **杜撰**: the staff's “surname Du, given name Zhuan” is a pun inside a dialogue, not a stable person/title sense.
- **法身 / 無住 / 剎那**: apparent master-name collisions belong on the master roster or are string collisions; they are not dictionary senses of the common word.
- **木人 / 燈籠 / 木佛 / 貓兒**: animation, case deployment, or use as an answer does not replace the underlying wooden figure, lantern, wooden buddha-image, or cat with another referent.
- **粥飯 / 粥飯僧 / 貓兒 / 木佛**: institutional or public-case salience is Zen deployment under #0g, but salience alone is not item-8 polysemy.

## Sweep conclusion

The actionable defect is concentrated rather than universal: **7 of 180** assigned single-sense articles have two independently anchored things collapsed into one sense. The strongest mechanical follow-up detector is broader than semicolons: search explanations for explicit constructions such as “as a verb … as a noun,” “the act … and by extension the address/result,” and “the literal object … forms a comparison.” Those phrases exposed four of the seven candidates. Rewrites should still be held until this report is reviewed, as requested.
