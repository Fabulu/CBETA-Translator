# ACTOR AUDIT — what the new attribution standard gets right, and the four defects

**Audited:** 2026-07-14, all 641 entries on disk (217 new-standard, 424 old-standard).
**Read `DICTIONARY_ENTRY_GUIDE.md` and `ATTRIBUTION_FIX.md` first.** This document does not replace them.
It reports what the attribution rework actually produced, and fixes the one thing that was never defined.

---

## First: the mechanics are flawless

80 random new-standard occurrences verified against the corpus with `zc.verify`:
**80 found · 80 with the correct `FromLb` · zero drift.** Nothing below is about the plumbing.

## And the new standard is genuinely better than what was asked for

`ActorAttribution` appears **only on occurrences with no named master** — it is the *record of the
investigation that concluded "unnamed"*, and it logs the six-rung ladder literally:

```json
"ActorAttribution": {
  "Status": "impersonal",
  "Kind": "editorial heading",
  "ActorLabel": "an impersonal discourse heading",
  "ActorRole": "event-label",
  "GrammarEvidence": "解制上堂 labels the occasion of the following address; it is
                      narrator-governed metadata rather than a quoted turn by Zhengfa Ximing.",
  "RungsChecked": ["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],
  "ReviewedBy": "Codex hard-bundle six-rung exact-turn review"
}
```

**339 occurrences carry all six rungs.** Nobody says "unnamed" any more without showing the work. And
`impersonal` is a distinction nobody asked for and everybody needed: *the passage has no human actor at
all* (a section heading, a compiler's stage direction) is a different fact from *a man spoke and we cannot
name him*.

New-standard occurrences: **1,055 named · 331 reviewed-unnamed · 66 impersonal.**

---

# THE ROOT CAUSE: `MasterName` was never defined

Every defect below traces to one thing. **`MasterName` has been carrying two incompatible ideas** and
nothing in the schema forced a choice:

- *"the man who uttered the headword"*, and
- *"the master whose section this passage sits in"*.

## ⛔ THE RULING (user, 2026-07-14)

> ### `MasterName` = **THE UTTERER OF THE HEADWORD.** Nothing else.

If the master did not say the headword, **his name does not go in `MasterName`.** He goes in
`ContextMasters` with a role, and `MasterName` is `null` with an `ActorAttribution` saying who did utter it
(or that nobody did).

---

# DEFECT 1 — the standard audits its failures but not its successes

**835 of the 1,055 named occurrences (79%) carry nothing but the name.** No `ContextMasters`, no roles, no
speaker/subject distinction. For those, the new standard is doing **exactly what the old one did**: assert a
master and move on. Only 220 carry the role distinction.

The rigour is entirely on the *negative* claim ("unnamed" → six rungs logged) and absent on the *positive*
one ("named" → just a name).

**Ten named new-standard occurrences were read in context. Three are wrong, in the same way the old ones
are wrong:**

| term | stored `MasterName` | what the passage actually says |
|---|---|---|
| **五位** | Caoshan Benji | 「**僧問**：『**五位**對賓時如何？』山云…」 — the headword is uttered by **the monk asking**. Caoshan *answers*. He is the section subject, not the utterer. |
| **大疑** | Xiuyun Wei | 「**師大疑**，猛力參究」 — the **compiler narrating**: "the master had great doubt." Xiuyun never said 大疑; someone wrote it *about* him. |
| **逍遙** | Danxia Tianran | 「師放曠情懷…去住**逍遙**」 — a biographer describing his manner. Not his speech. |

## DEFECT 2 — the old entries are worse, and their 99% is the alarming number

| | occurrences with a `MasterName` |
|---|---|
| new-standard (ladder logged) | **73%** |
| old-standard (no ladder) | **99%** |

The stricter pass demotes ~27% of its occurrences to `reviewed-unnamed` / `impersonal`. **The 424 old
entries were filled by a pass that never had to show its work and are sitting at 99% confidence.** That is
not a better result; it is an unearned one. Read in context, they fail the same way — and worse:

| term | stored `MasterName` | what the passage actually is |
|---|---|---|
| **勘辨** | Linji Yixuan | 「**勘辨勘辨**黃檗因入厨次…」 — 勘辨 is the **compiler's SECTION TITLE** (doubled, as CBETA repeats headings). Linji did not say it. This is `impersonal` / `editorial heading`. |
| **壁立萬仞** | Dahui Zonggao | 「和尚為甚麼一向**壁立萬仞**？**師曰**：…」 — the headword is spoken by **the questioner**; Dahui replies. |
| **頓悟** | Dazhu Huihai | 「自撰**頓悟**入道要門論一卷」 — the **compiler narrating**, and the headword occurs only inside a **BOOK TITLE**. Not a usage at all. |
| **斬葛藤** | Zhang Zhu | a florid literary **preface by a layman** — not a Zen master's speech. |

## DEFECT 3 — the role vocabulary is uncontrolled

**150+ distinct role strings**, almost all singletons: `sounding-block-declarer`, `actor standing in the
snow`, `walking-companion`, `sole-smiling-responder`, `preceding-departing-participant`,
`gavel-officiant`. That is **free-text prose in a structured field** — unqueryable, unfilterable,
unrenderable.

A real taxonomy is already visible in the ones that recur:
`respondent` 58 · `record-owner` 37 · `later-raiser` 26 · `section-subject` 20 · `person-discussed` 20 ·
`teacher` 18 · `recipient` 12 · `questioner` 12 · `addressee` 11 · `interlocutor` 10 · `commentator` 8.

**Close the vocabulary.** Free text belongs in `GrammarEvidence`, which exists for exactly that.

## DEFECT 4 — 176 occurrences whose KWIC does not contain the headword

`old: 153 (5.4%) · new: 23 (1.6%)` — improved, not fixed. An occurrence whose quote does not contain the
word it is evidence for is not evidence. Examples: `本來面目` has **four**; `老婆禪` has three; `著語`,
`庭前柏樹子`, `立處皆真`, `犯戒`, `當下`, `頭上安頭` each have one or more.

## DEFECT 5 — the prose still mumbles

Explanations say "**a master** answers…" even where the occurrence behind them is `reviewed-unnamed`. If
the record genuinely does not name him, the prose must **say so** — «the record does not name the
speaker» — not shrug. A reader cannot currently tell *"we looked and the record is silent"* from *"we
didn't look."* ~117 senses affected.

(Not every hit is a bug: "a master proclaims the sect's purport" in `舉揚` describes a *deployment shape*
across 1,627 hits, not one passage. That is fine.)

---

# THE FIX — read every occurrence in its passage

**4,338 occurrences across 641 entries.** This is not infinite, and every mechanical shortcut this project
has tried has produced *confident errors*: the TEI `<author>` field (empty across the whole X canon), the
五燈全書 tag position (labels the entry it CLOSES), the raw XML grep (inline `<lb/>` tags split names
mid-string), a speaker-classifier regex (48% false positives). **The thing that has worked every single
time is someone reading the passage.**

For each occurrence, answer ONE question — **who utters the headword here?** — and all five defects fall
out of that single act of reading.

### The four outcomes

1. **A named master utters it** → `MasterName` = him. Also give `ContextMasters` with his role, and the
   roles of anyone else in the passage.
2. **Someone else utters it** (a monk asking, a layman, a named third party) → `MasterName` = **that
   person** if he is a master; otherwise `null` + `ActorAttribution` naming who did speak. The section
   master goes to `ContextMasters` as `section-subject`. **This is the 五位 and 壁立萬仞 case.**
3. **The compiler narrates it** (「師大疑」, 「自撰頓悟入道要門論」) → `MasterName` = `null`,
   `ActorAttribution.Status` = `narrated`, `Kind` = `compiler narrative`, and the master goes to
   `ContextMasters` as `person-described`. **This is the 大疑 / 逍遙 / 頓悟 case.**
4. **No human actor at all** (a section heading, a stage direction) → `MasterName` = `null`,
   `Status` = `impersonal`. **This is the 勘辨 case.**

And in every case: **if the KWIC does not contain the headword, re-cut it** around the actual headword,
re-derive `FromLb`, and **verify with `zc.verify`**. If the headword is not in that passage at all, the
occurrence was wrong — replace it with one that is.

### The closed role vocabulary — use ONLY these

`utterer` · `respondent` · `questioner` · `interlocutor` · `addressee` · `section-subject` ·
`record-owner` · `person-described` · `person-discussed` · `commentator` · `later-raiser` · `later-quoter` ·
`teacher` · `student` · `compiler` · `verse-author` · `case-figure`

Anything you want to say beyond these goes in **`GrammarEvidence`** as prose.

### Non-negotiable

- **`zc.verify` is the gate.** Reading decides *who spoke*; the machine decides *whether the quote exists*.
  Both, always. That is what produced 3,499 verified occurrences with zero drift.
- **Never invent a speaker.** `reviewed-unnamed` with six rungs logged is a real, respected answer.
- **Never delete a quote to make a problem go away** (see `ATTRIBUTION_FIX.md` Task 3). Re-cut, or replace.
- **The old 424 entries get the same treatment.** Their 99% is the most suspicious number in the dataset.

### Definition of done

1. Every occurrence read in context; `MasterName` = **the utterer**, per the ruling.
2. Every occurrence carries `ContextMasters` with roles from the **closed vocabulary** — the named ones too,
   not just the failures.
3. Zero occurrences whose KWIC lacks the headword.
4. No explanation says "a master" where a name is recoverable, and where it is not, the prose **says the
   record does not name him**.
5. `node eng/tools/merge-dict-entries.js` run. Report before/after counts, and every occurrence where the
   utterer turned out **not** to be the master previously named — those are the findings.
