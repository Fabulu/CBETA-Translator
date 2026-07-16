# Does Chan/Zen teach karma and rebirth? — a corpus investigation

**A brief for a second opinion.** Self-contained. Please read adversarially and tell me where I'm wrong.

---

## 0. Context

I (Fabian) am building **ReadZen**, an open-source environment for Chinese Chan/Zen texts, and within
it a **"Zen-to-Zen" dictionary**: every term is defined *only* from how Chan masters actually use it
across the Chan corpus — never from a general Chinese or Buddhist dictionary.

Rules relevant to this dispute:

- **#0 — DESCRIBE, DO NOT INTERPRET.** An entry reports (a) the literal sense of the graphs, (b) the
  term's attested deployment, quoting what masters actually said, (c) structural facts. It must NOT
  assert the master's intent or the doctrinal "point."
- **Multi-source gate.** A sense is established only if it holds across ≥2 independent texts/masters.
  One source ⇒ `provisional`.
- **Corpus scope.** A 462-text allowlist from CBETA: records of masters (語錄/廣錄), lamp records
  (燈錄), koan collections + verse commentary (頌古). Excludes sutras, Pure Land, Vinaya, Tiantai.

All evidence below was gathered with my own concordance tool: **allowlist-scoped, tag-stripped,
apparatus (`<note>/<app>`) excluded, `<lb>`-anchored**. Every quotation is a verified verbatim
contiguous substring of the source TEI.

---

## 1. The dispute

**My claim** (it evolved — stating that honestly):

1. *"Zen doesn't deal in karma."* → 2. *"It's absent."* → 3. *"They literally say there is no
   causation."* → 4. **Best version:** *"**Conceptual entanglement IS Zen karma** — not literal
   rebirth as in Buddhism. Suffering is self-made, self-binding, one's own responsibility."*

**Claude's counter:** the corpus contains and asserts karma/rebirth in its preaching register; the
"it's really only psychology" reading is interpretation, not description.

**Outcome: we were BOTH partly wrong.** Details below.

---

## 2. What the corpus shows (measured)

### 2a. Counts (462 texts) — with the caveat that *word occurrence ≠ concept occurrence*

業 11,997 (432 texts) · 生死 11,223 (429) · 三世 4,760 (391) · 因緣 5,711 (394) · 三界 3,002 (333) ·
因果 1,534 (243) · 輪迴 923 +輪回 210 (207) · 六道 686 (197) · 業識 993 (239) · 野狐 1,721 (261)

Counts are sense-blind and settle nothing on their own. Deployment analysis follows.

### 2b. REGISTER 1 — causation ASSERTED (exhortation / preaching)

- **永覺元賢**: 便可當下超出三界，**永斷輪迴**，更無別法。
- **御選語錄**: 一切眾生…千生千死，**六道輪迴，無有休息，茫茫業海**，痛莫可喻。善男信女，能生一念淨信…
- **宗鏡錄**: 以**過去**善惡為因，**現今**苦樂為果。**絲毫匪濫，孰能免之**。
- **天隱**: **因果分明**罪福招，不遵佛律**入地獄如箭**。
- **定業難逃** 13 hits/11 texts · **因果歷然** 36/25
- **Huangbo** does not deny the six destinies: 諸佛體圓，更無增減，**流入六道，處處皆圓**。
- **Wumenguan case 2**: Baizhang **cremates the fox with monastic funeral rites** (乞依亡僧事例…乃依火葬).

### 2c. REGISTER 2 — causation DENIED, apophatically, of the awakened  ← **my position, and it IS attested**

**少室六門** (T48n2009 @0374a17–20; attributed to Bodhidharma, scholarly consensus = later Tang):

> 成佛須是見性。若不見性，**因果等語是外道法**。若是佛，不習外道法。**佛是無業人，無因果**。…佛無持犯，
> 心性本空，亦非垢淨，諸法無修無證，**無因無果**。佛不持戒，佛不修善，佛不造惡…**佛是無作人**。

> *"Talk of cause and effect is the dharma of outsiders (外道法). If you are buddha, you don't practise
> outsider dharma. **The buddha is a person without karma, without cause and effect.** … no cultivation,
> no attainment, **no cause and no effect.** Buddha keeps no precepts, cultivates no good, creates no
> evil… Buddha is a person of non-doing."*

Also **千山剩人**: 無善無惡、**無因無果**、無煩惱可斷、無菩提可求、無生死可了、無涅槃可證，上無諸佛可成、
下無眾生可度。 — note it negates **buddha and nirvana in the same breath.**

**Counts:** 無因果 = 128 hits / 82 texts — **BUT 97 of those are the string 撥無因果** (see 2d), so
standalone 無因果 ≈ **31 hits**. 無因無果 = 8 hits / 8 texts.

### 2d. REGISTER 3 — DENYING causation is a NAMED, CONDEMNED ERROR

**撥無因果** ("to dismiss/deny cause and effect") — **97 hits / 71 texts**, always condemnatory:

> **古雪哲**: 古人一時**抑揚之說、對機之談**，便乃**撥無因果，自招殃禍**。
> *"[People take] the ancients' rhetorical flourishes and situation-adapted sayings — and thereby deny
> cause and effect, and bring calamity upon themselves."*

> **古庭**: 懶墮至則狂念起，狂念起則**撥無因果**，無所不為。
> **天界覺浪盛**: 父有父業，子有子業…**何可撥無因果乎？**

### 2e. THE CORPUS GLOSSES ITS OWN NEGATION — 遮詮 (apophatic predication)

**禪源諸詮集都序** and **宗鏡錄** both classify these negations explicitly:

> 如諸經所說真妙理性，每云：不生不滅，不垢不淨，**無因無果**，無相無為，非凡非聖…**皆是遮詮**，遣非蕩跡。
> *"…these are all **apophatic predications** (遮詮), sweeping away traces."*

i.e. the tradition itself files 無因無果 with 不生不滅 and 非凡非聖 — the *via negativa*, not a positive
doctrinal denial. **This is an in-corpus self-definition of how to read the negation.**

### 2f. The fox koan is ironized — MULTI-SOURCE (a finding I earned)

Wumenguan case 2: the old teacher says **不落因果** ("does not fall into cause and effect") → 500 lives
as a fox; freed by **不昧因果** ("not blind to cause and effect"). But the commentators refuse to let
不昧 stand as the pious answer:

- **Wumen**: 前百丈**贏得風流五百生** ("the former Baizhang **WON** 500 lives of free-spirited elegance")
- **Wumen's verse**: **不落不昧，兩采一賽** ("two throws, one game") — he flattens the distinction
- **楚石梵琦**: 不落因果，不昧因果，**總未脫野狐身**。若要脫野狐身，**更過五百生始得**。
- **敏樹**: 今日新百丈**也不落、也不昧**… · **大慧**: 却喚甚麼作因果…

And one master does *both at once*:
> **天界覺浪盛**: 直饒你**因果歷然，不落不昧**，亦未脫得野狐身。

### 2g. 無繩自縛 ("binding yourself with no rope") — real, but not a karma phrase

**257 hits / 134 texts.** But its deployment is a reproach for self-created **conceptual** entanglement
(getting stuck on 兩邊, on a phrase; even **Bodhidharma** is accused of it when Emperor Wu pressed him).
**Only 2 of 257 (0.8%)** have karma vocabulary within ±60 chars — and the one that does uses it to
condemn *rigid position-taking on the fox dilemma* while **affirming 不昧因果**:
> 豈可**撥去不落，守箇不昧，無繩自縛**？…豈不以**明不昧因果**…？

---

## 3. Where it lands

**Refuted:** "karma vocabulary is absent from Chan."

**VINDICATED (I was right, Claude conceded):** Chan texts DO flatly deny causation — 佛是無業人無因果;
因果等語是外道法. Claude searched trying to falsify me and found it himself.

**Also established:** the fox koan is systematically ironized, multi-source; 不昧因果 is not permitted to
settle as a doctrinal right answer.

**The honest descriptive finding — three registers, all attested, held together deliberately:**

> The Chan records **(1) assert** karmic causation in the exhortation register, **(2) deny** it
> apophatically of the awakened (and the corpus itself labels this 遮詮), and **(3) condemn** its denial
> as a doctrinal thesis (撥無因果, 71 texts). Sometimes two registers appear in a single sentence.

Nobody appears to have counted this.

**Still open:** my strong claim — *"Chan karma = conceptual entanglement, NOT literal rebirth"* —
requires reading register (1) as non-literal. The literal cosmology is stubborn (入地獄如箭; 定業難逃;
past-life cause → present-life effect; a fox corpse given monastic cremation), and 撥無因果 explicitly
condemns inferring "no karma" from the masters' rhetoric.

---

## 4. Questions for you

1. **Can register (1) be read as non-literal** without importing modern psychologized-Zen framing
   (cf. Sharf, "The Zen of Japanese Nationalism")? Or is that exactly the move 撥無因果 forbids?
2. **Is 遮詮 the key?** Does the corpus's own labelling of 無因無果 as apophatic settle how register (2)
   should be read — i.e. as *via negativa*, not doctrinal denial?
3. **The untried test:** are there **in-corpus self-definitions of 業** (業者…也 / 所謂業者)? If masters
   gloss 業 as deluded discrimination, my reading is corpus-grounded, not projected. **Not yet run.**
   (業識 = 993 hits is where to look.)
4. **Lexicography:** is "three registers" a **three-way SENSE split**, or one sense with three
   deployment stances? (I think the latter — the *meaning* of 因果 doesn't change; the *stance* does.)
5. **Where should a dictionary stop?** My rule says describe, never interpret. Is that restraint what
   makes it trustworthy — or is it the sterile "objective facts only" tendency Ogawa Takashi criticises
   in the Yanagida legacy?

Push back hard. I would rather be corrected now than in front of an audience.
