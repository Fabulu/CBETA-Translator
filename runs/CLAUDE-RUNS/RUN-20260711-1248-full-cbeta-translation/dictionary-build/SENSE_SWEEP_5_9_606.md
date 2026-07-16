# Item-8 sense sweep: `t_5*` through `t_9*` (606-entry state)

Date: 2026-07-13  
Mode: read-only candidate report; no `entry.v2.json` was edited  
Rule applied: `DICTIONARY_ENTRY_GUIDE.md` §5 #0f.7–8

## Result

- Assigned and examined: **153/153** STATUS=done, single-sense entries.
- Prefix distribution: `t_5*` 32; `t_6*` 30; `t_7*` 29; `t_8*` 31; `t_9*` 31.
- Entries present in `maintenance/depth-sense-gate.json`: **153/153**.
- Detector review flags in the assignment: **80** entries (78 broad-concordance flags; 3 semicolon flags, with one entry carrying both).
- Confirmed split candidates: **5**.
- No-split dispositions: **148**. One of these (`紫芝`) needs a family-boundary repair rather than a split.
- Candidate anchors tested below: **10/10 exact, allowlisted, `zc.verify(...).ok == true`** with `PYTHONIOENCODING=utf-8`.

The split count is deliberately conservative. A different rhetorical use, English rendering, predicate, speaker, or Zen deployment was not treated as a second sense unless two passages made the exact headword denote different things and one precise gloss could not cover both.

## Confirmed split candidates (report before rewrite)

### 1. `t_734eadab549a` 本心

Current single sense: **original mind**.

Required distinction:

1. **original mind** — the Buddhist/Chan mind term.
2. **original intention / what one originally meant to do** — an intention or purpose, not a kind of mind taught in the first sense.

Verified anchors:

- Sense 1: `T/T48/T48n2008.xml` 0349a21–0349a22: `不識本心，學法無益；若識自本心，見自本性` — “If one does not recognize original mind, studying the teaching is useless; if one recognizes one's own original mind, one sees one's own original nature.”
- Sense 2: `X/X81/X81n1568.xml` 0007c14–0007c15: `大眾，此日之事，故非本心。實謂祇箇住山寧有意，向來成佛亦無心。` — the speaker says that the day's event was not his original intention and that he had had no intention even of residing on a mountain or becoming a buddha.

Why one gloss fails: “the day's affair was not original mind” is wrong English and wrong referent. The second occurrence concerns a speaker's intention.

### 2. `t_62044e7bbb87` 本分事

Current single sense: **one's own fundamental matter**. Its own Note already calls the ordinary usage a “mundane sense” but excludes it.

Required distinction:

1. **one's own fundamental matter** — the Chan interview term, including receiving people with it.
2. **one's proper duty / one's assigned business** — an obligation belonging to one's role.

Verified anchors:

- Sense 1: `J/J24/J24nB137.xml` 0358b16: `若是宗師，須以本分事接人始得。` — “If one is a lineage master, one must receive people with the fundamental matter.”
- Sense 2: `J/J33/J33nB294.xml` 0737c19–0737c21: `既稱比丘持戒是本分事，忽略繩趨，放蕩規矩，取玷法門，自招罪過。` — “Having said that keeping the precepts is a bhikṣu's proper duty, [they nevertheless] neglect discipline and cast off the rules...”

Why one gloss fails: keeping the precepts is a role-duty here, whereas the public-interview term is the matter a lineage master uses to receive someone. This is the guide's literal/ordinary versus Zen-loaded family.

### 3. `t_7182bedf65d1` 下語

Current single sense: **to lay down a word**. Its Note identifies but excludes a second allowlisted usage.

Required distinction:

1. **to offer a response phrase / capping phrase** — the live Chan testing routine.
2. **wording / phrasing** — the wording of a composition, coordinated with word choice.

Verified anchors:

- Sense 1: `B/B27/B27n0152.xml` 0558a15–0558a16: `師令眾下語眾下語畢師復舉` — “The master ordered the assembly to offer phrases; when the assembly had finished offering phrases, the master raised [it] again.”
- Sense 2: `B/B25/B25n0143.xml` 0276a03–0276a05: `雖然「音律不差」，而「下語用字，全不可讀」，也沒有文學價值` — although the prosody is not bad, “the phrasing and word choice are altogether unreadable,” and it has no literary value.

Why one gloss fails: the second occurrence evaluates diction; nobody is being tested by being ordered to submit a capping phrase. Caveat for adjudication: the second anchor is modern scholarly prose inside an allowlisted CBETA file. Under the stated corpus rule it is still attested and must split. If editorial/secondary prose is to be excluded, that needs an explicit scope rule rather than silent deletion from this entry.

### 4. `t_8f4ef1246821` 無位

Current single sense: **without rank**. The entry currently combines the ordinary administrative predicate and the Chan-loaded rankless figure.

Required distinction:

1. **rankless / outside ordered rank** — the Chan use, including Linji's “true person without rank,” bare interview answers, and rank-system questions.
2. **without office or position** — ordinary institutional/administrative status.

Verified anchors:

- Sense 1: `T/T47/T47n1985.xml` 0496c10–0496c11: `赤肉團上有一無位真人，常從汝等諸人面門出入` — “On the lump of red flesh is a true person without rank, constantly going in and out through your face-gates.”
- Sense 2: `X/X68/X68n1319.xml` 0571b11–0571b12: `有德無位。一人之言，無徵不信。` — “[He] had virtue but no position; the word of one person, without corroboration, is not believed.”

Why one gloss fails: the second is lack of recognized institutional position; the first is a named Chan figure whose “ranklessness” is deployed in the public interview and cannot be reduced to unemployment or lack of office.

### 5. `t_7cddddb76d37` 任運

Current single sense: **proceed of itself**. It also treats the exact headword as the proper title of Puming's seventh oxherding section.

Required distinction:

1. **proceed of itself / run its course** — a predicate of conduct or functioning.
2. **Spontaneous Course** — the title/name of the seventh section in Puming's oxherding sequence.

Verified anchors:

- Sense 1: `J/J28/J28nB202.xml` 0005b14–0005b15: `渴飲饑餐隨緣任運，更說甚佛法玄妙、菩提涅槃？` — “Drink when thirsty, eat when hungry, follow conditions and proceed of itself; what more is there to say of subtle buddhadharma or awakening and nirvana?”
- Sense 2: `J/J23/J23nB128.xml` 0348a10–0348a13: `任運任運普明禪師頌任運第七` — repeated section heading followed by “Chan Master Puming's verse: Spontaneous Course, the seventh.”

Why one gloss fails: in the latter passage `任運` names a numbered textual section; it is not the predicate of a grammatical subject. This is the item-8 word-versus-title family. The title is transparent, so its explanation should explicitly connect it to sense 1 rather than invent an interpretive distinction.

## Family-boundary repair, not a split

### `t_75a477117870` 紫芝

Keep the headword's lexical sense **purple fungus**. The current article makes the longer title `紫芝歌` (“Purple Fungus Song”) dominate the explanation, but the exact headword does not independently denote the song in the harvested evidence.

Verified family anchors:

- `J/J36/J36nB366.xml` 0843c23: `興來獨步玲瓏石，懶去溪邊採紫芝。` — the plant is gathered beside a stream.
- `X/X78/X78n1553.xml` 0556c05–0556c06: `如何是紫芝歌？師撫掌對之。` — the different, longer term `紫芝歌` is asked about as a song title.

Disposition: do **not** create a song sense under bare `紫芝` merely because those graphs occur inside the longer compound. Create or enrich a separate `紫芝歌` entry and move the title/public-interview evidence there. Zizhi-named lineage masters remain roster entities, as the current Note correctly says.

## Required semicolon adjudications

All three semicolon detector hits in this prefix range are false positives:

- `t_8a016f49e5b8` 思量 — `to ponder; think over`: synonyms for one act of mental reckoning, not different things.
- `t_90435e47b008` 休去歇去 — `rest; cease`: English for the two coordinated imperatives in one fixed doubled command, not two senses of the headword.
- `t_9571d06dd1c7` 張公喫酒李公醉 — the semicolon separates the two clauses of one fixed saying (“Mr. Zhang drinks; Mr. Li gets drunk”); it is not a gloss menu.

## High-risk no-split adjudications

These entries received extra scrutiny because their prose already combines ordinary and Zen-loaded deployment, title language, grammatical alternation, or a concrete object with an action. None established a second thing for the exact headword:

- `一著`: an ordinary/game-like move and a named Chan move remain the same countable move; no second object was found.
- `全體`: noun-like “whole body/whole” and extent syntax denote the same complete whole; word order is not a sense.
- `狗子無佛性`, `百丈野狐`, `德山托鉢`, `香嚴上樹`, `倩女離魂`, `歸宗斬蛇`: wording/event used as a transparent case label does not by itself establish a lexical homograph. The title points to the same recorded wording/event.
- `寶鏡三昧`: “the book” and the transmitted Precious-Mirror Complete Command may be text versus teaching, but the current anchors do not prove two independently denoted things; transmission of a text can cover both. Keep on the audit watchlist if non-textual uses are found.
- `拈古`: the evidence is nominal throughout—genre, record section, anthology, body of work, or countable prose comments. The existing evidence does not establish an exact-headword verb separate from those pieces.
- `拈提`: verbal and nominalized syntax names the same act/commentarial treatment; no separate title or object emerged.
- `十牛`: all harvested occurrences denote the Ten-Ox verse/picture series; no allowlisted occurrence of ten literal cattle was established.
- `石女`: “barren woman” explains the impossible stock figure; the corpus did not establish a separate stone statue/woman referent. Different impossible predicates are readings/deployments of one figure.
- `破草鞋`: literal sandals, including sandals worn out by walking, remain the object even when used as an answer or explicit comparison. One cannot split the object from what a master compares it to.
- `象王`: animal narratives, gait, and teaching-seat comparison preserve the same elephant-king referent. The loaded deployment is mandatory #0g prose, not a second elephant.
- `秉拂`: every retained occurrence is institutionally loaded presiding with the whisk. `秉拂人` is the officeholder derived from that act, not evidence for a separate bare-headword sense; no merely casual whisk-holding occurrence was established.
- `公案`: the corpus explicitly explains the government-case metaphor, but in the retained headword passages `公案` denotes Chan public cases; an etymological comparison is not an occurrence denoting a secular lawsuit.
- `正令`: the governmental/military image is transferred to the teaching seat, but all retained headword occurrences denote the command exercised there; no actual state order was established.
- `未在`: ordinary temporal “not yet” and the master's verdict “not there yet” preserve the same uncompleted/not-yet predicate. They are different deployments, not different things.
- `目前`: visual “before the eyes/right before you” remains locative; no independently attested modern temporal “currently” sense was established in the inspected evidence.
- `體露`: concrete-looking “body exposed” and “substance exposed” are contextual renderings of the same compound family in the harvested Chan formulas; no passage established an unrelated physical-undress sense.
- `本性`: the allowlisted article evidence supports original nature. Searches found ordinary “innate disposition” uses elsewhere in CBETA, but no exact allowlisted anchor in the assigned corpus, so a second corpus sense was not established.
- `宗師`: one allowlisted line says Huangbo is not a `儒教宗師` (a master of Confucian teaching). This suggests the current Chan-only prose could be widened to acknowledge the corpus-wide title “master of a tradition,” but it does not establish a different role; this is a definition-breadth review, not a split candidate.
- `行腳`, `面壁`, `觸目`, `拄杖子`, `拂子`: ordinary action/object and Zen institutional deployment retain the same feet-facing-wall/visual-field/staff/whisk referent in the evidence inspected. The institutional bend must remain central under #0g, but a bend alone is not automatically a second thing.

## Complete assigned inventory

A dagger marks the five split candidates above. Every other item has a no-split disposition in this pass (including the `紫芝` family repair).

### `t_5*` (32)

依樣畫葫蘆, 作麼生, 三玄三要, 行腳, 百丈野狐, 秉拂, 百尺竿頭, 一著, 參話頭, 孤明, 全體, 放行, 擬對, 山河大地, 客塵, 斷臂, 擊禪床, 懸崖撒手, 死中得活, 寸絲不掛, 狗子無佛性, 德山托鉢, 銀碗裏盛雪, 本性, 露地白牛, 須彌, 竿頭進步, 寶鏡三昧, 金鎖玄路, 維摩詰, 野狐禪, 情知.

### `t_6*` (30)

異類, 兼中到, 本分事†, 莫錯會, 波羅提木叉, 昏沈, 漸修, 單傳, 全機, 非心非佛, 香嚴上樹, 冷暖自知, 拈古, 僧問, 正令, 面壁, 騰騰任運, 良久, 渠今正是我, 沒巴鼻, 照用, 破草鞋, 淨裸裸, 銀山鐵壁, 一行三昧, 一鏃破三關, 情識, 迷頭認影, 知解, 不落因果.

### `t_7*` (29)

恁麼則, 恁麼, 下語†, 咄, 本心†, 明暗, 韓獹逐塊, 喪身失命, 紫芝, 石女, 丹霞燒佛, 付法, 釋迦老子, 省悟, 尊宿, 大疑, 生死事大, 動念即乖, 十牛, 罔措, 觸目, 格外, 宗風, 嗣法, 任運†, 公案, 看話, 冷灰豆爆, 父母未生前.

### `t_8*` (31)

綱宗, 拈提, 解會, 四料揀, 倩女離魂, 大徹大悟, 擒縱, 塵塵剎剎, 未在, 本地風光, 具眼, 象王, 撫掌, 淨瓶, 兼中至, 拄杖子, 惺惺, 便打, 一莖草, 漏逗, 思量, 法嗣, 逐塊, 一喝, 把定, 隨他去, 一口吸盡西江水, 意旨如何, 無位†, 水泄不通, 粥.

### `t_9*` (31)

休去歇去, 參請, 盡大地, 悟道, 參堂, 喫粥, 目前, 本來無一物, 顧視, 體露, 一物, 張公喫酒李公醉, 言前, 沒滋味, 心外無法, 持戒, 大用, 律師, 正法眼, 阿難, 宗師, 拈華微笑, 眉毛墮落, 平常心是道, 一歸何處, 理事, 歸宗斬蛇, 沒蹤跡, 猫兒, 還會麼, 拂子.

## Rewrite-order recommendation

1. Rewrite the four strongest lexical/institutional splits first: `本心`, `本分事`, `下語`, `無位`.
2. Adjudicate and, under the guide's word-versus-title rule, split `任運` with the Puming-specific title sense second.
3. Repair `紫芝`/`紫芝歌` as an overlapping-family boundary, not by adding a blurry second sense to `紫芝`.
4. Re-run family checks and the depth gate after every rewrite; every resulting sense needs at least one occurrence of its own, and every existing occurrence must be re-assigned rather than mechanically retained.
