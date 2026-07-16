# ATTRIBUTION FIX — name the speaker, anchor the quote

**Status:** REQUIRED. This is a correction pass over all 606 existing entries, not new work.
**Read `DICTIONARY_ENTRY_GUIDE.md` first.** Everything in it still applies (#0 describe-don't-interpret,
#0b Zen-only, #0c English, #0g the deviation, the depth gate). This document fixes a defect the guide
did not catch.

---

## The defect

An explanation says **"a master said…"** — and does not say which master. Across the termbase,
**357 of 715 senses (50%)** use a vague attributor: *a master, the master, a monk, the text says*.

The excuse for this was that the corpus often doesn't name the speaker. **That excuse is false.**
The speaker was not in the KWIC line — but the KWIC line sits **inside a book**, and the book names him.

Measured over all 3,666 occurrences:

| the occurrence sits in… | occurrences | already carry a `MasterName` |
|---|---|---|
| **a single master's own record** (語錄 / 廣錄; the TEI `<author>` names him) | **2,045** | **513 (25%)** |
| an anthology or lamp record (speaker changes per section) | 1,602 | 245 (15%) |
| neither — genuinely container-less | **19** | — |

**1,532 occurrences sit in a named master's own record and we failed to name him.** In Linji's own
`鎮州臨濟慧照禪師語錄` (T47n1985) we quote 51 passages and name him in 36. In `御選語錄` (X68n1319) we
quote 55 and name 2. The genuinely unattributable set is **19 occurrences**, not the majority.

Saying "a master said" when the book is *that master's own recorded sayings* is not caution. It is a
failure to read the container. **Fix it.**

---

## Task 1 — resolve the exact actor for every occurrence

For each occurrence, determine the exact actor of the headword turn/action. Set `MasterName` whenever
that actor is a master (every master must be named) or another personally named actor. Use the reviewed
exception branch only for a source-anonymous non-master participant or an impersonal construction.

### 1-FAST. Title-first candidate, complete-case proof (user, 2026-07-13)

Read the title first. In a verified single-master record it identifies the resident master and will eliminate most
container searches. **It does not by itself identify the speaker of the exact line.** Before assigning the title owner:

1. classify the book as a genuine single-master record rather than a multi-master compilation whose title merely contains `語錄`;
2. locate and read the **complete encounter/case unit**, from its opening attribution through its closing structural boundary;
3. reconstruct every turn and confirm that the exact stored headword utterance belongs to the title owner;
4. reject the shortcut if the unit contains another master, a visitor, an embedded old case, a quotation/citation, a preface or other contributor, an uncertain boundary, or any title/header/turn conflict.

The title owner may be the addressee, person discussed, host, or respondent while someone else speaks the headword.
Examples found during validation include an unnamed monk addressing the record owner and a sermon owner quoting an older
master's line before giving his own reply. In those cases the title owner is **not** the quoted line's speaker. Fail closed
to the ladder below. Store the title, section, whole-case evidence, exact-turn decision, method, and any exception in the
review record. A likely 95% shortcut is useful candidate generation; it is not permission to accept a 5% error rate.

**Validation result (2026-07-13): automatic Tier A is disabled.** In a fixed stratified 24-case test, one of only
two proposed Tier-A cases was a false accept: `雪竇石奇禪師語錄` explicitly says `應庵祖云`, so Yingan Tanhua—not
the title parser's proposed Xuedou—owns the quoted turn. `attribution_packet.py` therefore remains a review packet,
never an automatic writer or approval oracle. Inline speaker markers, a title alias that matches only part of a fuller
personal title, or failure of the extracted structural unit to contain its own normalized stored KWIC must force
review. The durable evidence is `maintenance/attribution-shortcut-validation.{json,md}`.

### 1a. Single-master records (2,045 occurrences)

The container names him. Get it from the TEI header and the title:

```python
import re
s = open(f'C:/temp/NewTranslationrepos/CbetaZenTexts/xml-p5/{relPath}', encoding='utf-8').read(40000)
author = re.sub(r'<[^>]+>', '', re.search(r'<author>(.*?)</author>', s, re.S).group(1)).strip()
# e.g. "宋 蘊聞編"  -> compiler, NOT the master; the TITLE names the master
# e.g. "明 通賢說　行浚等編" -> 通賢 SPOKE it (說) -- he is the master
```

⚠ **`編`/`集` = compiler, not speaker.** `宋 蘊聞編` on 大慧普覺禪師語錄 means Yunwen *compiled* it;
the master is **大慧** (Dahui), from the title. `說 / 撰 / 述 / 語 / 著` = the master himself.
**Read the title AND the author line; the title is usually the reliable one.**

### 1b. Anthologies and lamp records (1,602 occurrences)

Titles containing 傳燈錄 / 五燈 / 會元 / 續燈 / 古尊宿 / 指月錄 / 頌古 / 碧巖 / 從容 / 無門關 /
人天眼目 / 聯燈會要 / 祖堂集 / 拈古彙集 / 列祖提綱錄 / 御選語錄 …

The speaker changes per section. Find the **nearest preceding entry header** above the occurrence's
`FromLb`. Entry headers look like:

    常州府龍池萬如通微禪師      (prefecture + monastery + sobriquet + dharma name + 禪師)
    趙州從諗禪師
    鎮州臨濟義玄禪師

Walk **backwards** from the occurrence offset to the closest `…禪師` / `…和尚` entry header and take
that master. **Verify by proximity** — a header 40,000 characters away is not this passage's speaker;
if nothing plausible is within range, treat it as 1c.

### 1c. "Unattributable" — almost none of it is real

I originally reported 19 unattributable occurrences. **I checked all 19 by hand. Every one names the
master in the TITLE.** They fell through only because I trusted the TEI `<author>` field —

> ⚠ **`<author>` IS EMPTY FOR THE ENTIRE X (卍續藏 / Manji) CANON.** 7 of the 307 texts we quote have no
> `<author>` at all. Trusting that field is how we came to quote **馬祖道一禪師廣錄 — Mazu Daoyi's own
> record** — and **百丈懷海禪師廣錄 — Baizhang Huaihai's own record** — and write *"a master said."*
> **The TITLE is the reliable field.** Read the title first, always.

The last supposed hold-out — `十牛圖和頌` (X64n1271) — fell to **rung 2 of the ladder**. Its own preface
says: 而**普明**復一一係之以頌：**普明，未詳何許人** — "**Puming** attached a verse to each; Puming, it is
not recorded where he was from." The verses are Puming's; the *harmonising* verses (和頌) after them are
by a named crowd (破山, 萬如, 浮石, 玉林, 箬菴, 山茨, 天隱, 玄微, 真寂). Attribute the occurrence to
whichever verse it sits in.

> **CORRECTION AFTER EXACT-TURN REVIEW:** every item in that original 19-item container audit was
> nameable, but that does not prove every grammatical actor is named. Later complete-case review found
> genuine anonymous questioners and narrator-governed intervals. They require reviewed exception records;
> they never license assigning the respondent or title owner as the exact actor.

Note what that source does: it names the man **and then says his origins are unknown**. The tradition
will not leave a verse unsigned even when it cannot place the author. If you think a passage is
anonymous, you have not finished looking.

### THE RULE: AN UNNAMED MASTER IS NOT A MASTER

Anonymity is not a neutral state in this tradition. Zen is **obsessive** about attribution: a saying
with nobody behind it is not Zen evidence. So an occurrence you cannot attribute is not "a modest
finding" — it is a **weak occurrence**, and you should prefer to replace it with an attributable one
showing the same usage.

But you may only *declare* a speaker unnamed **after exhausting the search.** After the guarded title-first candidate
check above, THE FALLBACK LADDER — work it in order and stop at the first rung that names the exact speaker:

1. **The quoted line.**
2. **Widen the context.** ±500 → ±2,000 → ±10,000 characters. **This is where the name usually is.** An
   exchange opens by naming the master and then says 師 ("the master") for pages afterwards. If you only
   read the KWIC window, you will see 師 and conclude "anonymous" — wrongly.
3. **The section / 卷 header** above the passage (`…禪師` / `…和尚` entry headers in anthologies).
4. **The book title.** A 語錄 / 廣錄 / 雜錄 **is** a named master's record.
5. **The TEI header** — remembering it is empty across the X canon.
6. **The same passage in another text.** Cases travel. A lamp record or case collection quoting the same
   exchange will usually name him. Search the corpus for the KWIC string and read the parallel witness.

Only when **all six** fail is a real **non-master** actor genuinely unnamed. Then say so **explicitly** — *"the record
does not name the questioning monk"* — never a bare "a master said". `MasterName` stays `null` and a
complete `ActorAttribution` object records the `reviewed-unnamed` finding and all six rungs. When grammar
shows that the headword is narrator-governed duration, scene state, or group nonresponse, use the
`impersonal` status with concrete `GrammarEvidence`. Never put a respondent, title owner, or following
speaker in `MasterName`; put those people and their roles in `ContextMasters`. A bare null remains an
audit failure.

**Every master must be named.** The reviewed-unnamed branch is only for non-master participants whom the
source calls a monk, questioner, person, or group without a personal name. It must never carry `Kind:
master` (or a synonym) and must never excuse an unnamed teacher. If the exact actor is a master, resolve
his name or reject that occurrence.

### The name to write

`MasterName` **must exactly match `names[0]`** of the master's entry in the roster
(`https://raw.githubusercontent.com/Fabulu/CbetaZenTranslations/main/masters.json`), e.g.
`"Zhaozhou Congshen"`, `"Linji Yixuan"`, `"Dahui Zonggao"`. This is what makes the website's
`#/master/{name}` link resolve. A master not in the roster: use the pinyin `Sobriquet Dharma` form
and note it — the roster is being expanded separately.

---

## Task 2 — rewrite the prose to name him

Every `Explanation` and `Note` that says *a master / the master / a monk / the text says* must name
the man **wherever Task 1 resolved him**:

- ❌ "A master answers a monk's question with 'three pounds of flax'."
- ✅ "**Dongshan Shouchu** answers a monk's question with 'three pounds of flax' (麻三斤)."

Where he is genuinely unnamed (1c), say so plainly:
- ✅ "The record does not name the speaker; the exchange is given as a hall address."

Also name the **text**, not just the man, when the prose leans on a passage — the reader needs to
know it comes from the *Blue Cliff Record* and not from a Qing anthology.

**Do not invent.** If Task 1 could not resolve the speaker, do not guess him into the prose.

---

## Task 3 — anchor the quotes, or drop them

**1,201 of 3,883 Chinese quotes in the explanations (31%) match no stored occurrence.** The prose
quotes the corpus and does not show where it got it. Worst: `五位` quotes 45 passages and anchors **3**.
`公案` 46/12. `佛性` 48/15. `無心` 38/9.

**ANCHOR THEM. Do not delete them (user, 2026-07-13).** Deleting a dangling quote destroys evidence and
hides the reason it was dangling. Every Chinese string quoted in an `Explanation` or `Note` gets an
occurrence:

1. **Find it.** `zc.find(relPath, term)` / a corpus-wide search for the string. It is quoted because a
   researcher read it somewhere — go find where.
2. **Anchor it.** Add an occurrence: correct `RelPath` + `FromLb` (**verify with `zc.verify`**), a real
   `Kwic`, a `MasterName` (rule 10 — the container names him), and an `AttributionNote` naming the text
   and the speaker.
3. **⚠ IF THE QUOTE IS NOT IN THE CORPUS AT ALL — STOP AND FLAG IT LOUDLY.** Do not quietly delete it.
   A Chinese string in our prose that does not exist in the Zen corpus is one of:
   - a **paraphrase presented as a quotation** (we invented the wording), or
   - a quote from **outside the allowlist** (a non-Zen text — a sutra, a Qing encyclopedia), or
   - a **transcription error** (wrong character, dropped character).

   All three are serious. **List every one of them in your report with the entry, the string, and what
   you think happened.** These are the highest-value findings in this whole pass: an unfindable quotation
   is the signature of fabrication, and we would never have seen it if the quotes had just been deleted.

Only after a string has been searched for and genuinely does not exist may it be removed — and even then
it must be **reported**, never silently dropped.

No quote may remain in the prose with nothing behind it, and **no quote may be deleted merely because
anchoring it is work.**

---

## Task 4 — `AttributionNote` must name the source

97% of occurrences already have one, and they are good. Keep that standard, and make sure every one
of them names **the text** and **the speaker** (or states plainly that the speaker is unnamed):

> "Old Recorded Sayings of Venerable Masters (古尊宿語錄). In a hall act, the master lifts the whisk…"

becomes

> "Old Recorded Sayings of Venerable Masters (古尊宿語錄), section on **Zhaozhou Congshen**. In a hall
> act he lifts the whisk…"

---

## Why this matters — the website is already built and it is showing your work

The public site is **https://readzen.pages.dev** (source: `C:\programmieren\ZenLinkPage`, repo
`github.com/Fabulu/readzen-page`, deployed to Cloudflare Pages). **The dictionary is live on it right
now.** It reads the data you produce, live, from the data repo:

| file | what the site does with it |
|---|---|
| `termbase.index.json` | headwords only. The **reader underlines every word that has an entry.** |
| `termbase/NNN.json` | one shard, fetched **when a reader clicks an underlined word** |
| `termbase.v2.json` | the whole dictionary — the `#/dict` browse page full-text searches it |

Reading any Chinese text on the site, a word with an entry gets a dotted underline; **clicking it opens
the entry in a side panel** — senses, explanation, occurrences, related terms — and `#/dict/{term}` is a
shareable permalink to it.

**The card renders `MasterName` and links it to `#/master/{name}`.** Every occurrence you leave
unattributed is a missing link on a live public page, and every "a master said" is what a reader
actually reads. The site is also being upgraded to display the `Kwic` and the `AttributionNote` for each
occurrence directly on the card — so those fields stop being invisible metadata and become the visible
citation under each claim. **Write them for a reader, not for a database.**

---

## Publishing

After editing entries, **re-run the merge** — it regenerates every artifact the website reads:

```
node eng/tools/merge-dict-entries.js
```

It rewrites `termbase.v2.json`, `termbase.json`, `termbase.index.json` and the 202 `termbase/NNN.json`
shards in `C:\temp\NewTranslationrepos\CbetaZenTranslations`. It also normalises values (Validation to
the three legal states, `Curated: true`). **Do not hand-edit those artifacts — edit
`terms/<id>/entry.v2.json` and re-merge.** Do not commit or push; the user publishes.

> **FRESH-REBUILD OVERRIDE (2026-07-16; hard gate).** The preceding historical publishing paragraph
> describes the retired pre-rebuild tree. During the current fresh rebuild, the only authoritative mutable
> entry is `fresh-build/entries/<id>/entry.v2.json`. The historical `terms/<id>` tree is reference-only: it
> may be absent, stale, or intentionally differ, and it must never be used for current-hash binding,
> collision detection, repair, review, promotion, or completion counts. A reported collision is valid only
> when the SHA of the same `fresh-build/entries/<id>/entry.v2.json` changes after the reader bound it.
> Review packets and ledgers must record this authoritative path explicitly. Publish through
> `publish_fresh_checkpoint.py`; do not run the historical merger against `terms/` during this phase.

---

## Definition of done

1. **Every occurrence has a complete exact-actor state.** Every master is named in `MasterName`. A null
   is permitted only with a complete `reviewed-unnamed` non-master record that survived all six rungs, or
   an `impersonal` record with concrete grammar evidence. Bare nulls and unnamed masters fail.
2. **No explanation says "a master" where the master is knowable.** Every vague attributor either
   becomes a name or becomes an explicit statement that the record does not name him.
3. **Zero dangling quotes — by ANCHORING, not deleting.** Every Chinese string quoted in the prose gets
   an occurrence. Any string that cannot be found in the corpus at all is **reported as a suspected
   fabrication / out-of-corpus quote / transcription error** — never silently removed.
4. Every `MasterName` matches the roster spelling exactly (so the site's master links resolve).
5. `node eng/tools/merge-dict-entries.js` run; report the before/after counts.

Report honestly: how many you named, how many you could not, and why.
