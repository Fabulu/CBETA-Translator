![.NET 8](https://img.shields.io/badge/.NET-8-blue)
![Avalonia 11](https://img.shields.io/badge/Avalonia-11-purple)
![License: MIT](https://img.shields.io/badge/License-MIT-green)
![CBETA: Non-Commercial](https://img.shields.io/badge/CBETA-Non--Commercial-orange)
![OpenZenTexts: CC0 / Commercial OK](https://img.shields.io/badge/OpenZenTexts-CC0%20%2F%20Commercial%20OK-brightgreen)
[![Support on Ko-fi](https://img.shields.io/badge/Support_on-Ko--fi-ff5e5b?logo=ko-fi&logoColor=white)](https://ko-fi.com/readzen)

# Read Zen

Read Zen is a desktop app for reading, translating, searching, annotating, and sharing Chinese Zen texts across two corpora — **CBETA** (non-commercial, ~5000 texts) and **OpenZen** (commercial-OK, CC0/CC BY-SA, growing collection of freely-licensed witnesses) — without having to live in terminals, XML editors, or Git command lines.

It is built for actual text work:
- read Chinese and English side by side
- maintain personal translations and compare them with community ones
- search the corpus with bilingual context and exportable results
- build research collections in Scholar
- manage terminology, master metadata, tags, and reviews
- sync personal and shared work through GitHub-backed workflows

## What Read Zen Covers Now

Read Zen is no longer just a reader plus translation editor. The app now has six major work areas:
- `Reader`: side-by-side reading, hover dictionary, study assistant, notes, compare tools, coding/tagging
- `Translate`: projection-based translation workflow with AI copy/paste, review, assistant, and source switching
- `Search`: corpus search with KWIC, bilingual pairing, deep links, analytics, and exports
- `Community`: text download, updates, GitHub sync, recovery actions
- `Scholar`: collections, workspace, shared collections, passage comparison, exports, and research tooling
- `Masters`: database of 204 Chan/Zen masters with full lineage graph, corpus text appearances, biographical profiles, and 400+ reference links
- `Provenance Browser`: source witness tables, editorial documentation, license/attribution chips, and per-file manifest data from OpenZen

There is also a built-in onboarding tutorial that walks through the current workflow inside the app.

## Important Licensing Note

The app itself is MIT-licensed. The two corpora have different license terms:

**CBETA corpus** (non-commercial):
- keep the original CBETA attribution/header
- do not use the corpus or derived translations commercially

**OpenZen** (commercial-OK):
- each text declares its own license in the TEI header (typically CC0 or CC BY-SA)
- editorial reading editions (e.g. the 1632 NDL Wumenguan) are CC0 — public domain dedication, no attribution required
- Wikisource-derived texts carry CC BY-SA from Wikisource
- check the per-file license chip in the app or the TEI `<availability>` block for the specific terms

The two corpora are kept in separate repositories so their license terms never cross-contaminate.

## Text Folder Layout

Read Zen uses a multi-repo model. You pick one parent folder; the app clones all repos into it and discovers them automatically. Both corpora coexist under the same parent:

```text
ReadZen/                              your chosen folder
  CbetaZenTexts/                      CBETA originals repo (read-only, ~5000 texts)
    xml-p5/                           original Chinese XML files
  CbetaZenTranslations/              CBETA translations repo (your work lives here)
    xml-p5t/                          shared/canonical translated XML
    xml-p5t-cache/                    auto-generated untranslated copies (local, gitignored)
    community/                        per-user community contributions
      translations/{user}/            personal translations
      termbases/{user}.jsonl          personal terminology
      collections/{user}.jsonl        Scholar collections
      reviews/{user}.jsonl            review state
      tags/{user}.jsonl               tagging
      tag-vocabularies/{user}.json    tag vocabularies
    titles.jsonl                      title index
  OpenZen/                       OpenZen originals repo (free-licensed texts)
    xml-open/
      ws/                             Wikisource-derived texts
      pd/                             Public-domain scan-derived texts
      ce/                             Critical editions
      mit/                            MIT-licensed contributions
    provenance/
      {slug}/                         captured source witnesses + SHA-256 audit
    docs/curation/                    curation workflow documentation
      exemplars/{slug}/               worked examples of the transcription process
      PROCESS_LOG_TEMPLATE.md         journal template for future transcriptions
    tools/
      wikitext-to-tei/                converter for Wikisource witnesses
      woodblock-to-tei/               converter for woodblock-derived editions
  OpenZenTranslations/               OpenZen translations repo
    xml-open-t/                       shared/canonical translated XML
    community/                        same community structure as CBETA
    titles.jsonl                      title index
```

The split keeps each corpus's originals untouched in their own repo and all translation work in a separate translations repo. The OpenZen pair mirrors the CBETA pair's structure so the app can read both with the same code paths. File identifiers use different schemes (`T48n2005` for CBETA, `pd.wumenguan-1632` for OpenZen) so the two corpora can never be confused.

A corpus badge in the top-right corner of the app shows which corpus is active. Switching is one click.

Existing CBETA-only users get the OpenZen pair automatically on their next sync.

## Reader

The Reader is the main side-by-side reading view.

What it does:
- Chinese on the left, selected English source on the right
- click text to highlight matching text on the other side
- switch translation source between community, your own work, and other users' work
- open the full Zen Dictionary with `Dict`
- add and read community notes inline
- create deep links or add passages to Scholar from right-click menus

The Reader has a built-in **Study Assistant** panel that shows:
- hover dictionary lookups (CC-CEDICT) on any Chinese text
- recognized termbase entries highlighted in the text
- translation memory matches from approved and reference TM
- context from the active translation source

The Reader has a **Provenance Panel** (toggle via "Provenance" checkbox in the toolbar) that shows:
- source witnesses with SHA-256 hashes, capture dates, and vetting confidence
- edition kind and production method
- CBETA-independence verification
- expandable markdown documents from the provenance chain (witness verification, case audits, editorial notes)
- copy-citation button for quick attribution

A **license chip** in the top bar shows the current file's SPDX license at a glance (green = CC0/permissive, amber = CC BY-SA, orange = non-commercial). Click for full citation details.

Reader also contains the coding/tagging workflow:
- `F2` enters Coding Mode
- create and manage tag vocabularies
- apply tags by keyboard shortcuts
- switch tag user with the existing user picker
- compare your tag layer with another user's layer

## Translate

Translate uses a projection editor designed for safe structured translation work.

Key rules of the editor:
- edit only `EN:` lines
- do not edit `ZH:` lines or `<n>` block markers
- one `EN:` line per block
- multiline English inside a single block is intentionally rejected
- large numbered batch pastes across many blocks are supported

Translate supports:
- `Body` and `Notes` translation sections
- `Copy for AI` to export numbered blocks with strict instructions
- `Paste from AI` to reinsert numbered results safely
- per-block review controls (approve / needs-work with `Alt+A` / `Alt+N`)
- a `Fresh Start` option to reset the current writable translation back to untranslated state with confirmation
- personal-vs-other-user translation source switching

The Translate tab has a built-in **Translation Assistant** that shows:
- termbase hits highlighted in the Chinese source text
- translation memory matches (approved and reference) with shared-phrase highlighting
- QA warnings (same-as-source, Chinese in English, too-short)
- auto-fill from 100% TM matches

The editor is designed to preserve XML structure on save and reject unsafe projection states rather than silently mangling them.

## Search

Search is a full corpus workflow, not just a title filter.

Current features:
- search original Chinese, translated English, or both
- `Zen only`, status, tag, source, and KWIC controls
- paired bilingual result rows when counterpart text is available
- right-click result rows for passage links, shareable links, search-state links, and `Add to Scholar`
- corpus exports in multiple formats
- optional search analytics and a slower corpus-wide analytics mode
- deep-linkable search state

Search also supports:
- hover dictionary on Chinese result content
- incremental result population while search is running
- progress indication in the header
- shareable search links via the web launcher site

## Scholar

Scholar is the research workspace.

It is split into three concepts:
- `Collections`: your notebooks / containers
- `Workspace`: the active passage editing and comparison area
- `Shared`: other users' shared collections

What you can do there:
- collect passages from Reader, Translate, or Search
- adopt passages from shared collections into your own collection
- compare passages
- create typed links between passages
- attach notes, tags, doctrinal/topic metadata, and master metadata
- export in readable and research-friendly formats

Scholar has its own **Passage Assistant** that provides termbase hits and translation memory context for any selected passage, using the same TM and termbase data as the other tabs.

Scholar is intentionally not only for your own collections. If shared collections exist, new users can still browse and learn from them before building local collections.

## Zen Dictionary And Zen Masters

Read Zen includes a full terminology workflow.

The Zen Dictionary / termbase can:
- manage your own terminology entries
- inspect community/user terminology
- drive assistant highlights across Reader, Translate, and Scholar
- open directly from deep links

### Zen Master Database

Read Zen ships with a curated database of **204 Chan/Zen masters** from Bodhidharma through the late Ming, covering the full arc of Chinese Chan history. Each master carries:

- Dates (floruit + death), school, region
- Full lineage connections — teacher + students, cross-linked
- Biographical notes sourced from scholarly references
- Reference links per master (Wikipedia EN/ZH, Terebess, encyclopedias, academic papers, museum pages) — **400+ links total**

The **Masters tab** is a first-class tab in the main window with three sub-views:

- **List** — filterable master list with rich detail pane (clickable teacher/student names jump between profiles)
- **Corpus** — which texts in CBETA/OpenZen mention each master, split into primary (author/subject) vs secondary (quoted) with mention counts and snippets
- **Lineage Web** — interactive graph of master-student relationships with pan, zoom (mousewheel + slider), search, and temporal Y-axis ordering

The lineage taxonomy is rationalized per modern scholarship (McRae, Jia, Poceski) — Hongzhou, Caodong, Yunmen, Guiyang, Fayan, Linji, Heze, Early Chan. The polemical "Northern/Southern Chan" framing is avoided.

### Master Corpus Search

The app automatically scans all XML texts (~5,000 files) for zen master name mentions, building an index that:

- Classifies each appearance as **primary** (master is author/subject) or **secondary** (mentioned/quoted)
- Filters common Buddhist concept-names (法眼, 無門, 大慧, 國師, 六祖) that double as personal names, requiring longer-name corroboration
- Extracts clean context snippets (XML fragments stripped)
- Computes co-occurrence stats (who appears with whom in the same texts)

Right-click any master to **Copy Link** or **Copy Reddit Link** — the latter produces a clean URL like `https://readzen.pages.dev/master/Linji_Yixuan` that opens a full profile page on the web. Deep links work both ways: `zen://master/...` URIs open the master manager in the app.

In the Reader study panel, when a segment mentions a zen master the panel shows their bio with a **View Master →** button that jumps to their full profile.

## Community Sync

The Community tab handles downloading texts, updating your local repos, and syncing shareable work through GitHub.

Important model:
- first-run download clones both the originals repo and the translations repo
- personal share files can auto-merge through the community data flow
- canonical/shared translation updates are handled separately from personal translation storage
- commits and pull requests target the translations repo only
- recovery actions exist, but the normal `Sync` path is the intended workflow

Read Zen tries to protect local work during updates, but this is still a Git-backed workflow. If something feels destructive, stop and inspect before proceeding.

## Deep Links And Web Preview

Read Zen supports a broad `zen://` deep-link surface for in-app navigation, plus a companion web preview page at **[readzen.pages.dev](https://readzen.pages.dev)** so the same links can be shared with people who don't have the desktop app installed.

Supported link kinds:
- passages (with or without a line range)
- full-text searches (state and query)
- dictionary / termbase entries
- Zen masters
- Scholar collections and individual passages
- tags on a work
- compare views (two translations side by side)

These links are produced throughout the app — Reader, Translate, Search, Scholar, and the dictionary/master tooling — usually via right-click menus. They embed the same routing the app uses internally, so every link round-trips: open it on the web, click "open in Read Zen", and you land in the same place.

OpenZen files use synthetic line identifiers (e.g. `wm32.case01.l01`) that never collide with CBETA notation. Both formats work in deep links and the web preview.

### Web preview (readzen.pages.dev)

The preview page is a zero-install fallback that fetches data directly from the public CBETA and OpenZen repos. It recognizes both file-ID formats and dispatches to the correct repo automatically:

- **Passage** links to a line range render side-by-side ZH/EN with the chosen translator
- **Passage** links without a line range render a bilingual body preview for translated works, or a navigable source TOC for untranslated ones
- **Compare** links render two translations side-by-side against the original
- **Tags / Scholar / Search** previews stream the relevant community files (tag lists, collection passages, title search) and link into ranged passage URLs
- **Dictionary / Termbase / Master** previews resolve a single lookup card with falls-back from per-user to shared sources

Every preview includes a download CTA for the desktop app, since the previews are deliberately limited to "proof of value" — the full reading, translation, search, and scholarship workflows live in the app.

When the desktop app is installed, the preview page silently hands the link off to it on load and the browser tab becomes a no-op. This auto-open behavior can be disabled via a subtle footer toggle on the preview page for users who prefer to stay in the browser; an explicit "Open in Read Zen" button reappears when auto-open is off.

**Examples:**
- CBETA: the Gateless Barrier (*Wumenguan* / *Mumonkan*, T48n2005) — [readzen.pages.dev/T48n2005](https://readzen.pages.dev/T48n2005)
- OpenZen: the 1632 NDL Woodblock Reading Edition (*Wumenguan*, pd.wumenguan-1632) — [readzen.pages.dev/pd.wumenguan-1632](https://readzen.pages.dev/pd.wumenguan-1632)
- Zen Master: Linji Yixuan, founder of the Linji school — [readzen.pages.dev/master/Linji_Yixuan](https://readzen.pages.dev/master/Linji_Yixuan)
- Zen Master: Wansong Xingxiu, compiler of the Book of Serenity — [readzen.pages.dev/master/Wansong_Xingxiu](https://readzen.pages.dev/master/Wansong_Xingxiu)

## Onboarding Tutorial

The app includes an in-app tutorial with **43 guided steps** covering:
- initial text download / setup
- Reader basics
- hover dictionary and the full dictionary window
- Study panel
- tagging/coding mode
- corpus switching and provenance
- Translate workflow
- Search workflow
- Scholar collections/workspace/shared model
- Zen Dictionary and Zen Master Manager
- Community sync
- deep links and right-click actions

The tutorial is the quickest way to see how the current app is supposed to be used.

## Performance Philosophy

Read Zen is built around large text collections and tries to stay fast through indexing, caching, and deferred enrichment.

If something is slow, especially repeated work on the same corpus, that should generally be treated as a bug rather than as expected behavior.

## Platform Support

- Windows
- Linux
- macOS

Built with:
- `.NET 8`
- `Avalonia 11`

## Repositories

| Repository | Purpose |
|---|---|
| [Fabulu/ReadZen](https://github.com/Fabulu/ReadZen) | Desktop app source code |
| [Fabulu/CbetaZenTexts](https://github.com/Fabulu/CbetaZenTexts) | CBETA original Chinese texts (~5000 files) |
| [Fabulu/CbetaZenTranslations](https://github.com/Fabulu/CbetaZenTranslations) | CBETA translations + community data |
| [Fabulu/OpenZenTexts](https://github.com/Fabulu/OpenZenTexts) | OpenZenTexts originals + provenance + curation docs |
| [Fabulu/OpenZenTranslations](https://github.com/Fabulu/OpenZenTranslations) | OpenZenTexts translations + community data |
| [Fabulu/readzen-page](https://github.com/Fabulu/readzen-page) | Web preview SPA (readzen.pages.dev) |

## Building

If you just want to use Read Zen, use a release build.

### Windows
```bash
dotnet publish ReadZen.App.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

### Linux
```bash
dotnet publish ReadZen.App.csproj -c Release -r linux-x64 --self-contained true
./run-readzen-selfcontained.sh linux-x64
```

### macOS Apple Silicon
```bash
dotnet publish ReadZen.App.csproj -c Release -r osx-arm64 --self-contained true /p:PublishSingleFile=true
```

### Dictionary Asset
Make sure this file exists in the publish output:

```text
Assets/Dict/cedict_ts.u8
```

### Testing
```bash
dotnet test
```
882 automated tests covering URI parsing, index caching, search, translation status, corpus detection, scholar exports, and view model logic.

## Git / GitHub Requirements

For text download and sync, you need Git available.
For GitHub-backed sharing, you also need a GitHub account.

Useful links:
- Git: https://git-scm.com/downloads
- GitHub signup: https://github.com/signup

## Contributing

Contributions are welcome, but this app is built around corpus integrity and workflow safety.

Please avoid:
- XML rewrites that casually change structure
- "cleanup" passes that rewrite tags or semantics without need
- performance regressions without a strong reason
- UI changes that hide real working space for the text itself

If a change affects translation structure, sync, or search semantics, add tests.

## Support

ReadZen and OpenZen are free and open-source. If this work is useful to your practice, teaching, or research:

[![Support on Ko-fi](https://img.shields.io/badge/Support_on-Ko--fi-ff5e5b?logo=ko-fi&logoColor=white)](https://ko-fi.com/readzen)

Your support funds new woodblock transcriptions, translation tools, and a growing freely-licensed corpus.

## Legal

Read Zen: MIT License

Data sources and their terms:
- **CBETA corpus**: non-commercial terms — see CBETA's license
- **OpenZen**: per-file license declared in TEI headers (CC0, CC BY-SA, etc.) — commercial use permitted
- **CC-CEDICT**: `CC BY-SA 4.0`

See `THIRD_PARTY_NOTICES.txt` for details.

## Short Version

Read Zen is a full working environment for Chinese Zen study and translation across both the CBETA corpus (non-commercial, ~5000 texts) and the OpenZen collection (commercial-OK, freely-licensed witnesses with full provenance tracking):
- read side by side with provenance and license visibility
- translate with structure-aware tools
- search with context and exports
- build Scholar collections
- manage terms, masters, notes, reviews, and tags
- sync personal and shared work without living in Git
- share deep links that work in both the desktop app and on the web at [readzen.pages.dev](https://readzen.pages.dev)

Built for actual use, not demos.
