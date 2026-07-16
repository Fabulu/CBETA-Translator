# CODEX HANDOFF — Continue the Zen-to-Zen Dictionary Build

> **NEWEST LIVE POINTER (2026-07-14):** Read `LIVE_RESUME_20260714.md` first.
> It supersedes all historical progress counts below and contains the active
> semantic-retrospective worker queues and deterministic restart sequence.

## ⛔ CURRENT LIVE POINTER — 2026-07-13

- **621 entries are merged; b001–b036 and r001–r002 are complete. Current wave: r003.** Older progress numbers below
  are historical and must not be used to rebuild finished work.
- **Current work is the rule-10 attribution remediation; r003 is held unmerged.** Read
  `ATTRIBUTION_FIX.md` and `ATTRIBUTION_REMEDIATION_PLAN.md`. The latter freezes separate baselines for
  the 606-entry snapshot and the 15 later r002 entries. Dangling Chinese evidence must be anchored, not
  deleted, and every newly anchored witness triggers definition/sense re-testing.
- Current remediation checkpoint: r002 15/15 complete; original-606 has 41 mechanically complete and
  independently semantic-reviewed. Resume from `ATTRIBUTION_PROGRESS.md`, not by rediscovering finished cohorts.
  Roster membership checking remains deferred while a separate agent expands the roster.
- Finish requested r003–r009 from `REQUESTED_BUILD_PLAN.md` (r009 adds 心如牆壁 and 不動尊). Then build the curated, already-derived
  `NEXT500_BUILD_PLAN.md` (500 unique terms, n001–n034), followed by `NEXT100_BUILD_PLAN.md` (100 sayings/trivia
  entries, s001–s007). Then process all 720 leads in `RELATED_INVESTIGATION_BACKLOG.md` in frequency order,
  adjudicate each explicitly, and build every independent Zen-specific headword that survives. Merge after every
  wave. Do not replace these with a new raw-frequency derivation.
- `NEXT500_TERMS.md`, `NEXT500_CANDIDATES_A.md`, `NEXT500_CANDIDATES_B.md`, and
  `NEXT500_RELATED_POOL.tsv` preserve frequency, Chan-deployment rationale, and provenance. The sayings' exact
  anchors and inherited explanations are in `NEXT100_SAYINGS_CANDIDATES.md`.
- Guide §5 item 9 is mandatory: begin from inherited research, then record keep/revise/reject after testing the
  candidate's own concordance. Existing interpretations are leads, not authority, and may not silently disappear.

## ⛔⛔ LATEST PHASE DIRECTIVE (user, 2026-07-12) — READ THIS FIRST
- This supersedes the earlier cancellation of the "next 500" plan. Complete the newly prioritized requested
  families, finish b023–b036, finish every remaining term in `REQUESTED_TERMS.md`, and run the required #0g
  deviation pass over existing entries. Then derive the next curated 500 dictionary-worthy terms from the
  allowlisted Zen corpus, deduplicated against the termbase, WAVE_PLAN, roster, and requested list, and begin
  building them under the same #0–#0g gates. Do not generate a raw-frequency dump.
- **Follow the NEW process:** the guide now runs §5 **#0 through #0g**. #0g (the "flyswatter test") is REQUIRED:
  every entry must surface WHERE ZEN BENDS THE WORD (e.g. 拂子 = the teaching-seat implement / emblem of
  authority, not "fly-whisk" flat). Buddhist/pre-Zen FIGURES the masters invoke ARE in scope, defined by the
  ZEN deployment (the Zen Buddha = flower sermon + mounting/leaving the seat, not the Buddhist hagiography).
  Lineage patriarchs/masters stay on the master roster, not in the dictionary.
- **Apply the #0g deviation lens to existing entries too** — the ~278+ entries built before #0g may state usage
  without naming the deviation; a read-only deviation audit + targeted reframe (see `DEVIATION_AUDIT_A/B/C.md`
  for the method) should be part of Phase 2.

---

You (Codex) are taking over an in-progress dictionary build from another agent. This file is your
**complete, self-contained** instruction set. Read it fully before doing anything. You do NOT have the
prior conversation — everything you need is here or in the files this points to.

**Read order / what to do:** (1) read this whole file; (2) read `STATUS.md` (live state) and skim
`DICTIONARY_ENTRY_GUIDE.md`; (3) do the **§6.5 CALIBRATION pass** and get the user's approval — this GATES
everything; (4) only then run the wave loop (§7) autonomously. Sections: §1 the rule · §2 state · §3 paths ·
§4 schema · §5 search & tools · §6 models · **§6.5 calibration (do first)** · §7 pipeline · §8–9 prompts ·
§10 gotchas · §11 done-criteria.

---

## 0. MISSION & PROJECT CONTEXT

**Build a rigorous Zen-to-Zen dictionary**: one entry per Zen technical term, defined **only from the
primary Chan/Zen corpus**, wave by wave (15 terms per wave), verified, and merged into a community termbase.
The dictionary is the gate for eventually translating the Chan corpus, so **accuracy is everything**.

**Where this lives / why the schema looks like it does.** The dictionary is a feature of **ReadZen**, a
cross-platform desktop app (.NET 8 / Avalonia) for reading the classical Chan/Zen canon — a side-by-side
Chinese/English reader with corpus search, master (Zen-teacher) pages, and a lineage graph. The dictionary
entries are consumed by the app, which is why each entry:
- links **Occurrences** to exact corpus passages (`RelPath` + `FromLb` line-anchor) so the reader can jump to
  where a term is used in the actual text;
- carries **MasterName** on senses/occurrences so the app can link a usage to that teacher's page (names must
  match the roster in `master-dates.json` to link);
- ships as a shared **termbase** (`termbase.v2.json`) that all app users receive — so entries must be correct
  and non-editorializing, because thousands of readers will trust them.

**Zen-to-Zen** means: the definition of a Zen term is built *from how the Zen masters themselves use it in the
corpus* — not from modern scholarship, not from an AI's theory of what it "really means." (This is the origin
of the §1 rule.) The dictionary is descriptive lexicography over a fixed, authoritative corpus.

**Scope of the corpus.** The full CBETA canon is on disk, but **only the 462 texts in the allowlist
(`zen-corpus.json`) count as Zen** — the Chan lamp-records (傳燈錄/會元), master discourse-records (語錄/廣錄),
koan collections (碧巖錄/從容錄/無門關), and the like. Anything outside the allowlist is contamination.

**Progress model.** ~517 terms are planned across waves b001–b036 in `WAVE_PLAN.md` (value-ranked by corpus
frequency); after b036, generate and continue with the next ~500. 98 entries are merged so far (through b006).

---

## 1. ⛔ THE ONE RULE THAT MATTERS MOST — DESCRIBE, DO NOT INTERPRET

**"The Zen texts are the measurement, not an AI's interpretation."** (This is the user's non-negotiable rule.)

An entry's `Explanation` reports:
- (a) the **literal sense of the graphs**,
- (b) the term's **attested deployment in the corpus** — quoting *what masters actually said*,
- (c) **structural facts** (which cases, which masters, don't-cross-attribute).

It must **NEVER** assert intent / point / spiritual force. **Banned** vocabulary unless a corpus text
literally says it: *"meant to / in order to / the point is / this smashes / deflationary / expresses /
symbolizes / represents / throws him back / underscores."* **A menu of readings is still interpreting** —
do not offer two or three "possible meanings." Where the force of a term is contested, state only that the
texts record it without gloss, and take no position.

Worked example — `乾屎橛` ("a dry shit-stick"): a draft said it was "meant to disgust." **Wrong** — a
shit-stick is the latrine wiping-tool; the user's test was *"Do you mean disgust when you say toilet paper?"*
You don't. Name the object, cite where masters say it (Linji on 無位真人; Yunmen on 如何是佛; Deshan on the
buddhas), and STOP. See the finished entry `terms/t_ba841f6e11c8/entry.v2.json` — **this is your gold
standard for style.** Its Explanation ends: *"The texts assign the word no gloss beyond its literal sense,
and neither does this entry."*

**BUT NOT THIN.** "Don't interpret" is not "say little." The definition must *emerge from accumulated
attested usage*. Get the MAXIMUM out of the text (all grep-verified, allowlist only):
1. **In-corpus self-definitions** — where a text literally defines the term (`X者…也` / `謂之X` / `名為X` /
   `喚作X`). Highest value; foreground these.
2. **Deployment range** — is it spoken as an answer, an epithet, a verdict, a test-question, a genre label,
   an imperative? Give a short attested example of each.
3. **Contrasts the texts themselves draw** (e.g. 殺人刀 vs 活人劍 by one author).
4. **Attested collocations & variants** with grep counts.

**DEPTH GATE:** these four categories are a required search checklist, not suggestions. Search every
self-definition formula and obvious word-order variant; harvest every distinct definition, deployment shape,
text-drawn contrast, morphological variant, and later comment on an earlier case. Record the inventory in
`WORK.md`. A perfect `zc.verify` pass does not rescue a thin entry. The usual 4–6 occurrences is not a cap when
another occurrence supplies unique lexicographic evidence.

Test every sentence: *"Is this in the text, or is it my conclusion about the text?"* Keep the first, cut the second.

**ZEN ONLY — purge imported framing (guide §5 #0b).** Zen = the Chinese Chan textual record (the allowlist),
NOT Japanese Zen-Buddhism, and **Zen has no "practice."** Cut all six families of imported framing, none may
appear in a gloss unless a Zen text literally says it:
1. **Buddhist doctrine** (emptiness/śūnyatā-as-doctrine, defilements, saṃsāra/nirvāṇa, pāramitās, Buddha-nature
   as metaphysical essence). The Chan masters mocked general-Buddhist piety.
2. **Meditation/mindfulness** — and **`禪`/dhyāna is NEVER "meditation"** (dhyāna is Sanskrit for something
   else): `禪床` = "Chan seat", `坐禪` = "sitting Chan", `參禪` = "investigate Chan". Never "meditation".
3. **Present-moment** ("be here now", "present-moment awareness", "the present scene"). `當下` = "on the spot";
   `目前` = "before your eyes".
4. **Dualism** ("dualistic thinking / non-dual / duality" — the original 凡情聖見 fakeout). `分別` = "to
   distinguish", not "dualistic parsing".
5. **Practice/method + Japanese overlay** ("practice, method, technique, training, cultivation, huatou,
   practice, zazen, koan practice, satori, kenshō"). The texts record sayings/encounters, not techniques.
   `看箇無字` = "look at the word 'no'": translate `無` as **no**, never Japanese “Mu,” a mantra, or a
   meditation exercise.
   `話頭` is never “huatou”: translate the occurrence as word/saying/remark/question/exchange as the case warrants.
   `坐禪` is neither Japanese zazen nor a meditation technique; derive it separately from Chinese Chan usage and
   verify any “seat of the mind-king” rendering against explicit corpus language (`心王`, `座`, collocations).
   Calibration found zero exact `心王座`/`心王之座`/`坐即心王`/`心王安坐`, so do not assert that equation.
   `禪床` is literal “Chan seat” furniture; report the Platform Record definitions and recorded critiques of
   `坐禪` without recasting them as a method.
6. **Chinese Chan only** — **no Dōgen (道元), no Japanese masters/sources/concepts. LITMUS: if you need
   Japanese to describe a concept, it is certified NOT Zen — drop it.**
If a term lives in that language in the wider tradition, either show how the CHAN corpus deploys it (often
CRITICALLY — quote the critique) or give only the literal graphs. Never adopt the imported frame as the meaning.

**DESCRIBE IN ENGLISH; TRANSLATE EVERYTHING (guide §5 #0c).** It's a dictionary: `PreferredTarget`/`Explanation`/
`Note` read in English and translate the Chinese. `看箇無字` = "look at the word 'no'" (無 = "no"), never left
bare. Every quoted Chinese phrase gets an English rendering beside it. Only the `Kwic` evidence field stays
verbatim Chinese. Do not reflexively retain `法` as “Dharma”: translate the corpus relation (for example,
`法嗣` = “lineage heir” in lamp headings), and derive `法眼`/`法身` from Chinese Chan occurrences rather than
copying a Buddhist-glossary definition. Likewise translate `三昧` as “complete command,” not an unexplained
“samādhi”; its named compounds must be described from their direct Chan definitions.

**THE ZEN RECORD (guide §5 #0d).** Zen is the roughly thousand-year Chinese Chan record of named masters and
students interacting, testing, answering, and commenting on earlier encounters. `公案` is a **public case / case**:
a historical record, not a paradox, riddle, parable, allegory, secret code, mystical anecdote, or thought-stopping
device. Zen is not tranquility, peace, quietism, reincarnation doctrine, New Age spirituality, or self-improvement.
The Four Statements are boundary conditions — outside teachings; not based on written words; point directly at
mind; see nature and become buddha — not a doctrine or instruction system.

**TRANSLATE ZEN AS CORPUS-SPECIFIC LANGUAGE (guide §5 #0e).** General dictionaries and existing translations
provide candidate glosses only. Resolve terms through the Chinese sentence, speaker, named case, genre, parallel
uses, later comments, and multi-master consistency. Shared English religious vocabulary can make unrelated
traditions look alike; re-derive the Zen referent. Literal graphs are the floor, then supply attested context in
plain English. AI and translations are leads, never evidence.

**SURFACE THE DEVIATION (guide §5 #0g — the flyswatter test).** A Zen entry earns its keep by noting WHERE ZEN
BENDS THE WORD away from ordinary-Chinese/Buddhist usage. Everyone's dictionary has 拂子 = "fly-whisk"; the Zen
dictionary notes it is the master's teaching-seat implement — the emblem of teaching authority (Baizhang raises
the whisk to answer "what do you use to show people?"). State that deviation up front, grounded in how the corpus
deploys the word. It must be ATTESTABLE (a cultural/institutional fact visible in the usage), not projected
mysticism — so #0's "no symbolizes" ban does NOT forbid it. Most words carry no deviation and need no entry;
prioritize objects Zen ritualizes (拂子/拄杖), ordinary words Zen loads (賊/老婆/屎), and Buddhist terms Zen
inverts/mocks (佛/坐禪/修行).

Full rules: `DICTIONARY_ENTRY_GUIDE.md`, especially **§5 #0, #0b, #0c, #0d, #0e, #0f, #0g** (read all seven before drafting).

---

## 2. CURRENT STATE (as of handoff)

- **Merged so far: 83 entries** in `termbase.v2.json` (waves b001–b005 + 7 legacy). All are describe-only + enriched.
- **b006 is in QA** (15 terms drafted; a QA pass may or may not have finished — CHECK, see §7).
- **Remaining: b007 … b036** (~15 terms each) per `WAVE_PLAN.md`, then a **NEXT 500** terms after b036.
- Every finished term lives in `terms/<id>/` with: `entry.v2.json`, `WORK.md`, `STATUS` (a file containing
  one word: `drafted` → `verified` → `done`).

**Resume by reading `STATUS.md` in this directory first** — it has the live per-wave state and the exact
next action. Then read `WAVE_PLAN.md` for the term list of the next wave.

---

## 3. EXACT PATHS (absolute)

| What | Path |
|---|---|
| This build dir | `C:\programmieren\MergeWorkCbeta\CBETA-Translator\runs\CLAUDE-RUNS\RUN-20260711-1248-full-cbeta-translation\dictionary-build` |
| Corpus (TEI XML) | `C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` |
| Allowlist (462 Zen texts — the ONLY texts that count) | `C:\programmieren\MergeWorkCbeta\CBETA-Translator\Assets\Data\zen-corpus.json` |
| Roster (valid MasterName values) | `C:\programmieren\MergeWorkCbeta\CBETA-Translator\Assets\Data\master-dates.json` |
| Merge target (rich) | `C:\temp\NewTranslationrepos\CbetaZenTranslations\termbase.v2.json` |
| Merge target (legacy) | `C:\temp\NewTranslationrepos\CbetaZenTranslations\termbase.json` |
| Merge script (node) | `C:\programmieren\MergeWorkCbeta\CBETA-Translator\eng\tools\merge-dict-entries.js` |
| Shared toolkit | `<build dir>\zc.py` |
| Entry guide | `<build dir>\DICTIONARY_ENTRY_GUIDE.md` |
| Live status | `<build dir>\STATUS.md` |
| Wave plan | `<build dir>\WAVE_PLAN.md` |
| Term dirs | `<build dir>\terms\<id>\` |
| Ledger | `<build dir>\MANIFEST.jsonl` (append-only) |

**Allowlist scoping is mandatory:** only the 462 relpaths in `zen-corpus.json` count as Zen. Citing any
other CBETA text is contamination and will be rejected.

---

## 4. THE ENTRY SCHEMA (PascalCase — this exact shape)

One `entry.v2.json` per term = ONE `DictionaryEntry`:

```json
{
  "Id": "t_<sha256(term)[:12]>",
  "SourceTerm": "話頭",
  "CreatedBy": "Codex",
  "WrittenUtc": null,
  "Senses": [
    {
      "SenseKey": null,
      "MasterName": null,
      "PreferredTarget": "the saying under examination (a huatou)",
      "AlternateTargets": ["the phrase to be worked on", "..."],
      "Status": "preferred",
      "Explanation": "<describe-only, per §1 — literal graphs + attested usage + self-definitions + counts>",
      "Validation": "multi-source",
      "Note": "<provenance, structural facts, don't-cross-attribute notes>",
      "Occurrences": [
        {
          "RelPath": "T/T48/T48n2005.xml",
          "FromLb": "0293c05",
          "ToLb": "0293c05",
          "Kwic": "<EXACT contiguous verbatim substring of the file, tags stripped>",
          "MasterName": "Wumen Huikai",
          "Curated": true,
          "AttributionNote": "<who said it, which section/cb:mulu head, why null if null>"
        }
      ],
      "SourceTexts": ["T/T48/T48n2005.xml", "..."],
      "RelatedMasters": ["..."],
      "RelatedTerms": ["..."]
    }
  ]
}
```

Rules:
- **`SenseKey = null` → corpus-wide sense; `SenseKey = <master CanonicalName>` → master-specific sense.**
  A term with two genuinely distinct *corpus-wide* meanings may carry **two null senses** — put the primary
  **Zen-technical** one at `Senses[0]`. (Accepted; e.g. 末後句, 隨波逐浪.) **Historical origin is not a
  master-specific meaning:** a usage introduced or popularized by one master but attested across later masters
  still gets a null key. Use a master key only for a genuinely distinct meaning.
- `Validation ∈ {provisional, multi-source, disputed}`. `multi-source` requires ≥2 independent texts/masters.
- `MasterName` must be an EXACT roster spelling from `master-dates.json` (a real master not in the roster may
  be named in the AttributionNote but leave `MasterName` null — it just won't link).
- **PascalCase field names only.** (The merge script now auto-normalizes camelCase, but write PascalCase.)
- `Id` MUST equal the directory name. Compute: `Id = "t_" + sha256(SourceTerm.trim())[:12]` (lowercase hex).

---

## 5. SEARCH & TOOLS — HOW TO FIND, VERIFY, AND ANCHOR EVERYTHING

Building an entry is a **corpus-search task**: concordance a term across the allowlist, read the contexts, pick
the strongest occurrences, verify each is a real exact quote, and anchor it to a line + a speaker. Do ALL of
this through the shared toolkit — **do not hand-roll grep scripts** (the previous run wasted enormous effort on
agents each re-writing buggy tag-strippers; `zc.py` is the vetted, tested version).

### 5.1 `zc.py` — the concordance toolkit (your primary search tool)
Tested against the 乾屎橛 exemplar. Import it; pass **Python strings** (avoids CJK command-line encoding issues):

```python
import sys
sys.path.insert(0, r"C:\programmieren\MergeWorkCbeta\CBETA-Translator\runs\CLAUDE-RUNS\RUN-20260711-1248-full-cbeta-translation\dictionary-build")
import zc

# 1) CONCORDANCE — how often / where a term occurs, allowlist-scoped:
zc.count("乾屎橛")
#   -> {"hits": 704, "files": 210, "per_file": [("X/X80/X80n1565.xml", 12), ...]}   sorted by count desc

# 2) READ CONTEXTS — see the term in situ with its governing line-anchor, to choose occurrences & attribution:
zc.find("X/X80/X80n1565.xml", "乾屎橛", ctx=16)
#   -> [{"window": "...無位真人是甚麼乾屎橛。巖頭...", "fromLb": "0227a10"}, ...]

# 3) VERIFY A KWIC — the ground truth for every Occurrence you write:
zc.verify("X/X80/X80n1565.xml", "無位真人是甚麼乾屎橛。巖頭不覺吐舌。雪峯曰。")
#   -> {"ok": True, "fromLb": "0227a10", "toLb": "0227a10", "count": 1}

# 4) TEXT TITLE — to name a text correctly in an AttributionNote:
zc.title("X/X80/X80n1565.xml")      # -> "五燈會元"

# 5) ALLOWLIST CHECK — is this text Zen (allowed as evidence)?
zc.is_allowed("X/X80/X80n1565.xml") # -> True

# 6) ATTRIBUTION POINTER (rough) — nearest <head>/cb:mulu before a line; CONFIRM by reading the file yourself:
zc.head("X/X80/X80n1565.xml", "0227a10")  # -> {"head": "...", "mulu": [...]}
```

Run python with env **`PYTHONIOENCODING=utf-8`** (Windows console needs it to print CJK). Everything `zc`
returns is already **allowlist-scoped, tag-stripped, apparatus-excluded, and primary-edition-lb aware**.

### 5.2 The search WORKFLOW for one term
1. `zc.count(term)` → get total hits, file count, and the top files. This tells you frequency and where to look.
2. For the top few files, `zc.find(rel, term)` → read the surrounding windows. Identify the distinct **senses**
   and **deployment shapes** (answer? epithet? verdict? test-question? self-definition `X者…也`?).
3. Pick ~4–6 strong occurrences per sense across **independent** texts/masters (for `multi-source`).
4. For each chosen occurrence, decide the exact `Kwic` span (one source line is safest) and run `zc.verify` —
   it must return `ok=True`; take `fromLb`/`toLb` from it.
5. For attribution, open the file and read the governing `cb:mulu`/`<head>` above the line (see §10.4). Set
   `MasterName` (roster spelling) or null.
6. Harvest self-definitions, contrasts, and collocation counts (`zc.count` on each collocation) for the prose.

### 5.3 Fallbacks & the raw corpus
- Raw files are UTF-8 TEI XML under the corpus dir (§3). You can `grep`/`rg` them directly to eyeball context,
  but **never take counts or KWIC boundaries from raw grep** — raw grep sees apparatus footnotes and misses
  phrases that cross `<lb/>` line breaks. `zc` is the source of truth; raw grep is only for a quick look.
- `zc.verify` returning `ok=False` on a string you *can* see in the raw file almost always means it sits inside
  an `<note>`/`<app>`/`<rdg>` apparatus block (a Taishō footnote or Ming/卍 edition variant) — that is **not
  valid main text**, do not cite it. (Real trap: Linji's 四賓主 compound is only in a Ming apparatus variant in
  T47n1985, absent from the main text.)

### 5.4 The app's own search (context only — you don't need it)
ReadZen ships a full-text corpus search (`Services/SearchIndexService` + `InvertedSearchIndex`, an inverted
index + bloom filter over the canon) that powers the reader's UI. **You do not use it for dictionary building** —
it indexes for interactive queries, not verbatim-quote verification. `zc.py` is purpose-built for this task
(exact substring + line anchor + allowlist scope + apparatus exclusion) and is what you should use.

`zc.verify` is your KWIC ground truth: **every `Kwic` in every entry MUST return `ok=True`.**

---

## 6. MODEL POLICY (per the user)

- **Default to GPT-5.6 generously — its rate limits are humane, so do NOT ration it.** Use GPT-5.6 for
  research/drafting, the quick-QA pass, hard attribution, and targeted fixes alike. Quality first; don't
  down-tier to save quota. Model id: try `gpt-5.6-sol` (the working id on a ChatGPT account); if rejected,
  fall back to `gpt-5.6`, then the strongest available.
- Optionally drop the *purely mechanical* KWIC-verify loop to a faster model if you want to save wall-clock —
  but GPT-5.6 everywhere is fine and preferred. When in doubt, use 5.6.
- **Concurrency cap: 4 subagents at once.** Design each wave to fit (see §7).

---

## 6.5 ⚠ FIRST — CALIBRATION PASS (do this BEFORE any autonomous work; the user gates on it)

**Do NOT go into the wave loop / autonomous "infinite queue" mode until the user has seen and approved a
head-to-head calibration.** The point is to check that your output matches the quality bar of the entries we
already built, on a term we can compare against.

Steps:
1. **Benchmark term: `話頭` (huatou), id `t_d190cf45c531`.** It already has a finished, merged entry at
   `terms/t_d190cf45c531/entry.v2.json` (2 senses, describe-only, enriched). **Do not open or read it yet** —
   build yours blind so the comparison is fair.
2. Run the FULL pipeline on 話頭 **from scratch** (research per §8 + quick-QA per §9, using GPT-5.6 and `zc.py`),
   but **WRITE YOUR RESULT TO A NEW FILE**: `terms/t_d190cf45c531/entry.codex.json`. **Do NOT overwrite
   `entry.v2.json` and do NOT touch its `STATUS`.**
3. Now read our `entry.v2.json` and write `CALIBRATION_COMPARE.md` in this build dir with: (a) a short summary
   of your entry (senses, #occurrences, self-definitions found), (b) a point-by-point diff vs ours (sense
   structure, occurrences/attributions, describe-only discipline, richness), (c) where you think yours is
   better or worse, (d) any rule ambiguity you hit.
4. **STOP and present both files + `CALIBRATION_COMPARE.md` to the user.** Ask: *which is better, and what
   should I adjust?* Wait for the user's verdict.
5. Only AFTER the user approves do you proceed to §7 and begin the wave loop (start at the next unbuilt wave per
   `STATUS.md`, i.e. b008+ — do NOT rebuild already-merged waves). Fold any adjustment the user asks for into
   your §8/§9 prompts first.

(If the user prefers a different or second benchmark term, use whichever they name — any already-`done` term in
`terms/` works the same way: build to `entry.codex.json`, compare, present.)

---

## 6.6 REFRESH / REBUILD POLICY — PRESERVE RESEARCH, FIX MECHANICS FIRST

Do not discard completed entries merely because the specification evolved. Existing verified KWICs, hard
attributions, self-definitions, contrasts, and rare variants are expensive research inventory.

1. Run a deterministic maintenance pass first (only when the user authorizes the corpus-wide write): verify every
   KWIC, rewrite `FromLb`/`ToLb` from current `zc.verify`, refresh all stated counts from `zc.count`, and flag any
   curated KWIC lacking the headword.
2. Refresh semantic content **informed by the old entry**, never blind: re-check its evidence against the Chinese,
   preserve valid depth, and rewrite stale glosses/sense structure under #0/#0b–#0f.
3. Treat old prose and keys as claims, not authority. Put the Zen-technical sense first and use a master key only
   for a distinct master-specific meaning, not historical origin.
4. Prefer targeted refresh of drifted waves plus deterministic repair over deleting all work. If the user chooses
   a full rebuild for uniformity, feed every old entry to the researcher as an evidence inventory and require the
   same final mechanical/depth QA.

---

## 7. THE PER-WAVE PIPELINE (fits your 4-agent cap)

For each wave `bNN` (15 terms), do these steps IN ORDER. **One wave in flight at a time.**

### Step A — SEED (you, the orchestrator; no subagent)
1. Get the 15 terms from `WAVE_PLAN.md` (the next `### Wave bNN` block).
2. For each term compute `Id = "t_" + sha256(term)[:12]` and `mkdir terms/<id>`.
   - Bash: `printf '%s' "話頭" | sha256sum | cut -c1-12`
3. Append 15 `todo` lines to `MANIFEST.jsonl` (one JSON object per line:
   `{"termId":"...","sourceTerm":"...","status":"todo","batchId":"bNN","agent":N}`). Write CJK via a file then
   `cat >> MANIFEST.jsonl` to avoid shell-encoding issues.

### Step B — RESEARCH (3 subagents, GPT-5.6, concurrent — 3 ≤ 4 cap)
Split the 15 terms into 3 groups of 5. Give each subagent the RESEARCH PROMPT (§8). Each writes
`terms/<id>/{entry.v2.json, WORK.md, STATUS=drafted}` for its 5 terms, self-verifying every KWIC with `zc`.

### Step C — QUICK-QA (1 subagent, GPT-5.6 — limits are humane, use it)
Give it the QUICK-QA PROMPT (§9) over all 15 terms. It uses `zc.verify` on EVERY KWIC (fast, comprehensive),
spot-checks attribution/counts/titles, scans for interpretation, fixes the obvious, and sets `STATUS=done`
(or leaves `verified` + flags a term). If terms are flagged, spawn ONE GPT-5.6 fix subagent for just those.

### Step D — MERGE (you; no subagent)
1. Verify all 15 `terms/<id>/STATUS` say `done` and every `entry.v2.json` parses.
2. Run: `cd C:\programmieren\MergeWorkCbeta\CBETA-Translator && node eng/tools/merge-dict-entries.js`
   - It collects every `terms/<id>/entry.v2.json` whose `STATUS=="done"`, preserves existing entries by `Id`,
     writes `termbase.v2.json` (rich) + `termbase.json` (legacy). Idempotent — safe to re-run.
3. Confirm the entry count grew by ~15. Update `STATUS.md` (progress + next wave) and `MANIFEST.jsonl`.

### Step E — NEXT WAVE
Go to Step A for `bN(N+1)`. After **b036**, generate a NEW 500-term plan (append `### Wave b037…` blocks to
`WAVE_PLAN.md`, value-ranked by corpus frequency of Zen-technical terms not yet in the dictionary) and continue.

**Agent budget per wave ≈ 4** (3 research + 1 QA), within your cap. Do NOT spin up a big adversarial gate army —
that is what made the previous approach too slow.

---

## 8. RESEARCH PROMPT (give verbatim to each of the 3 research subagents; swap the term list)

> You RESEARCH and DRAFT rigorous Zen dictionary entries from the primary corpus. READ FIRST:
> `<build dir>\DICTIONARY_ENTRY_GUIDE.md` §5 #0/#0b/#0c/#0d/#0e/#0f, and the exemplar
> `terms/t_ba841f6e11c8/entry.v2.json` (乾屎橛).
>
> ⛔ THREE RULES: (A) DESCRIBE, DO NOT INTERPRET — literal graph sense + attested usage (quote masters) +
> structural facts only; NEVER assert intent/point/force ("meant to/the point is/deflationary/symbolizes");
> no menu of readings; contested force → say the texts record it without gloss. (B) NOT THIN — MAXIMUM from
> text (grep-verified, allowlist only): in-corpus self-definitions (`X者…也`/`謂之X`/`名為X`), deployment range,
> contrasts the texts draw, collocations+variants with grep counts. Search ALL definition formulas and variants;
> inventory every distinct high-value finding in WORK.md, and include it or record why it was excluded. 4–6
> occurrences is not a cap on unique evidence. (C) ZEN RECORD + ENGLISH — cases are public historical records,
> never paradoxes/riddles/parables/codes; no meditation/practice/present-moment/New Age/Japanese framing; resolve
> Zen vocabulary from this corpus and translate every Chinese phrase in prose into plain English.
>
> USE THE TOOLKIT (don't hand-roll): `import sys; sys.path.insert(0, r"<build dir>"); import zc` — then
> `zc.count(term)`, `zc.find(rel,term)`, `zc.verify(rel,kwic)`, `zc.title(rel)`. Run python with
> `PYTHONIOENCODING=utf-8`. zc is allowlist-scoped, excludes apparatus, uses ed="X" lbs.
>
> CORPUS `C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` · ALLOWLIST `...\Assets\Data\zen-corpus.json` ·
> ROSTER `...\Assets\Data\master-dates.json`.
>
> YOUR 5 TERMS (write to `<build dir>\terms\<id>\`): <id · term · short gloss × 5>
>
> METHOD per term: (1) concordance across ALLOWLIST only via zc; derive real sense(s) → DictionarySense
> (SenseKey null=corpus-wide, else master; historical origin/popularization does NOT justify a master key; two
> null senses allowed if genuinely polysemous; primary Zen-technical sense at Senses[0]). (2) ~4–6 strong
> occurrences/sense plus every additional lexicographically unique witness; each Kwic MUST return
> `zc.verify(...).ok == True` (exact
> contiguous, main text NOT apparatus); take FromLb/ToLb from zc. (3) attribution ONLY after reading the
> governing cb:mulu head (zc.head is a rough pointer) — raised/two-speaker/stock-action/label lines → MasterName
> null; exact roster spelling; verify any text-title you name with `zc.title`. (4) Validation multi-source only
> if ≥2 independent; else provisional. (5) every Chinese phrase in Explanation/Note must be grep-confirmed in
> the allowlist (no fabricated collocations). (6) RelatedTerms genuine. (7) DEPTH AUDIT before writing: search
> `X者`/`所謂X`/`謂之X`/`名為X`/`喚作X`/`何謂X`/`如何是X`, deployment shapes, contrasts, variants, historical
> retrospectives, and later comments on cases; reconcile the inventory against the draft.
>
> WRITE per term: `entry.v2.json` (ONE DictionaryEntry, PascalCase, Id = dir id), `WORK.md`, `STATUS`=`drafted`.
> RETURN COMPACT per term: sense count, validation, #occurrences (all zc-verified), self-definitions searched/found/
> included-or-excluded, depth-audit result, files written.

---

## 9. QUICK-QA PROMPT (give verbatim to the 1 QA subagent, all 15 terms)

> You are the QUICK-QA-AND-FINALIZE gate for 15 drafted entries. FAST corpus-backed check + fix of the obvious,
> NOT an exhaustive re-derivation. USE THE TOOLKIT: `import sys; sys.path.insert(0, r"<build dir>"); import zc`
> (`PYTHONIOENCODING=utf-8`). Rules: `<build dir>\DICTIONARY_ENTRY_GUIDE.md` §5 #0/#0b/#0c/#0d/#0e/#0f. Exemplar
> `terms/t_ba841f6e11c8/entry.v2.json`. ROSTER `...\master-dates.json`.
>
> YOUR 15 TERMS: <id · term × 15>
>
> FOR EACH — read entry.v2.json, quick pass: (1) KWIC (MECHANICAL, ALL): `zc.verify` EVERY Kwic; ok=True → sync
> FromLb/ToLb from zc; ok=False → fix via `zc.find` to a true exact span, or split, or delete that occurrence.
> Flag any KWIC that lacks the headword; keep it only with an explicit reason that it is unique contextual evidence.
> (2) ATTRIBUTION (SPOT-CHECK): read the cb:mulu head of the 1–2 non-null-MasterName occurrences; null if
> raised/two-speaker/stock-action/label. (3) TITLES/COUNTS (SPOT-CHECK): `zc.title` any doubtful text-title;
> re-derive the 1–2 headline counts with `zc.count`; fix mismatches. (4) DESCRIBE-ONLY (SCAN): delete any
> banned interpretation word (meant to / the point is / deflationary / symbolizes / represents / smashes), plus
> imported meditation/practice/present-moment/New Age/Japanese framing and claims that cases are paradoxes,
> riddles, parables, codes, or mind-stopping devices. (5) SENSE STRUCTURE: primary Zen-technical sense first;
> null for corpus-wide meaning; never use a master key merely because that master introduced/popularized it.
> (6) DEPTH: read WORK.md's #0f inventory; confirm every found self-definition, distinct deployment, contrast,
> and variant is included or has a recorded exclusion reason. Missing depth is a flag, even when every KWIC passes.
> (7) set STATUS=`done` (or `verified` + flag a term you can't quickly fix).
>
> WRITE: overwrite entry.v2.json (PascalCase), set STATUS. Validate JSON parses.
> RETURN COMPACT per term: KWICs ok/fixed/headword flags, attribution/title/count/interp fixes, sense/depth audit,
> final STATUS, flags.

---

## 10. HARD-WON GOTCHAS (these caused real errors — respect them)

1. **KWIC must be an EXACT contiguous substring** of the cited file after tag-stripping. No ellipsis, no
   stitched fragments, no added/changed punctuation. `zc.verify` is the arbiter (`ok=True` required).
2. **Apparatus trap:** text inside `<note>` / `<app>` / `<rdg>` (Taishō footnotes, Ming/卍 edition variants)
   is NOT valid main text. `zc` excludes it; if `zc.verify` says `ok=False` but you see the string in the raw
   file, it's in apparatus — don't use it.
3. **X-canon dual lb editions:** X-canon files carry `ed="X"` and `ed="R"` (Manji reprint) lb systems. Use the
   **`ed="X"`** number. `zc` already returns the primary-edition lb.
4. **Attribution = read the cb:mulu head.** A quoted/raised case (`舉…`), a two-speaker Q&A, or a stock action
   line → `MasterName` null (name the person in the AttributionNote). Never write "speaker not identified"
   without actually reading the governing `cb:mulu` head. Watch reversed-name roster lookups (e.g. `圓通法秀`
   is on the roster; `法秀圓通` is the same person reversed).
5. **No fabricated collocations.** Every Chinese phrase you quote must occur in the allowlist (`zc.count > 0`).
6. **Don't cross-attribute** a case to the wrong master (e.g. verse = 洞山良价 vs prose definition = 曹山本寂;
   emblem-德山宣鑒 vs 第二代德山緣密; 一喝分賓主 = 谷隱 not Linji).
7. **Two null SenseKeys** are allowed for genuine corpus-wide polysemy (primary at Senses[0]) — not a defect.
8. **PascalCase field names.** Id must equal the dir name.
9. **Headword variants:** use the dominant corpus form as `SourceTerm` (e.g. 喫茶去 not 吃茶去), list the other
   as an AlternateTarget; keep the Id from whatever seeded the dir.
10. **Counts:** derive from `zc.count` (allowlist-scoped, tag-stripped). Don't trust line-based grep — phrases
    cross `<lb>` line breaks.

---

## 11. DEFINITION OF DONE (per term) & WHEN TO STOP

A term is `done` when: every KWIC `zc.verify` ok; attributions read at the mulu head; SourceTexts all attest
the headword; Explanation is describe-only + not-thin with grep-verified quotes; JSON parses; `Id` = dir name.

Merge after each wave. Keep `STATUS.md` current so the next session can resume. Continue b007→b036, then the
NEXT 500. Report to the user at each merge milestone or a structural blocker you cannot clear.

---

*Handoff authored by the prior agent (Claude). The lean pipeline (§7) and toolkit (§5) exist specifically so a
fresh orchestrator with a 4-agent cap can keep the same quality bar without the previous 16-agent-per-wave cost.*
