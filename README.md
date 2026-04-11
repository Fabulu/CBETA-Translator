![.NET 8](https://img.shields.io/badge/.NET-8-blue)
![Avalonia 11](https://img.shields.io/badge/Avalonia-11-purple)
![License: MIT](https://img.shields.io/badge/License-MIT-green)
![CBETA: Non-Commercial](https://img.shields.io/badge/CBETA-Non--Commercial-orange)

# Read Zen

Read Zen is a desktop app for reading, translating, searching, annotating, and sharing CBETA Zen texts without having to live in terminals, XML editors, or Git command lines.

It is built for actual text work:
- read Chinese and English side by side
- maintain personal translations and compare them with community ones
- search the corpus with bilingual context and exportable results
- build research collections in Scholar
- manage terminology, master metadata, tags, and reviews
- sync personal and shared work through GitHub-backed workflows

## What Read Zen Covers Now

Read Zen is no longer just a reader plus translation editor. The app now has five major work areas:
- `Reader`: side-by-side reading, hover dictionary, study assistant, notes, compare tools, coding/tagging
- `Translate`: projection-based translation workflow with AI copy/paste, review, assistant, and source switching
- `Search`: corpus search with KWIC, bilingual pairing, deep links, analytics, and exports
- `Community`: text download, updates, GitHub sync, recovery actions
- `Scholar`: collections, workspace, shared collections, passage comparison, exports, and research tooling

There is also a built-in onboarding tutorial that walks through the current workflow inside the app.

## Important Licensing Note

The app itself is MIT-licensed, but the CBETA corpus and derived translations remain non-commercial.

If you use or share CBETA-based texts:
- keep the original CBETA attribution/header
- do not use the corpus or derived translations commercially

## Text Folder Layout

Read Zen uses a two-repo model. You pick one parent folder; the app clones both repos into it and discovers them automatically:

```text
ReadZen/                              your chosen folder
  CbetaZenTexts/                      originals repo (read-only CBETA corpus)
    xml-p5/                           4990 original Chinese XML files
  CbetaZenTranslations/              translations repo (your work lives here)
    xml-p5t/                          shared/canonical translated XML
    xml-p5t-cache/                    auto-generated untranslated copies (local, gitignored)
    community/
      translations/{user}/            personal translations
      termbases/{user}.jsonl          personal terminology share files
      collections/{user}.jsonl        personal Scholar collections share files
      reviews/{user}.jsonl            personal review share files
      tags/{user}.jsonl               personal tagging share files
      tag-vocabularies/{user}.json    personal tag vocabulary share files
    termbase.json                     shared termbase
    translation-memory.approved.jsonl approved TM entries
    translation-review.jsonl          review ledger
    zen_texts.json                    zen text list
```

The split keeps the original CBETA corpus untouched in one repo and all translation work in another. Untranslated files are generated locally on demand and never distributed. Commits and PRs target the translations repo only.

Existing users on the old single-repo layout are migrated automatically on first launch.

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

Zen Master management now has its own manager window.
It centralizes:
- master names
- aliases
- dates
- community variants

Both dictionary terms and masters support deep links.

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

### Web preview (readzen.pages.dev)

The preview page is a zero-install fallback that fetches data directly from the public CBETA and translations repos:

- **Passage** links to a line range render side-by-side ZH/EN with the chosen translator
- **Passage** links without a line range render a bilingual body preview for translated works, or a navigable source TOC for untranslated ones
- **Compare** links render two translations side-by-side against the original
- **Tags / Scholar / Search** previews stream the relevant community files (tag lists, collection passages, title search) and link into ranged passage URLs
- **Dictionary / Termbase / Master** previews resolve a single lookup card with falls-back from per-user to shared sources

Every preview includes a download CTA for the desktop app, since the previews are deliberately limited to "proof of value" — the full reading, translation, search, and scholarship workflows live in the app.

When the desktop app is installed, the preview page silently hands the link off to it on load and the browser tab becomes a no-op. This auto-open behavior can be disabled via a subtle footer toggle on the preview page for users who prefer to stay in the browser; an explicit "Open in Read Zen" button reappears when auto-open is off.

**Example:** the Gateless Barrier (*Wumenguan* / *Mumonkan*, T48n2005) — [readzen.pages.dev/#/T48n2005/](https://readzen.pages.dev/#/T48n2005/)

## Onboarding Tutorial

The app includes an in-app tutorial covering the current workflow.
It now walks through:
- initial text download / setup
- Reader basics
- hover dictionary and the full dictionary window
- Study panel
- tagging/coding mode
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

## Legal

Read Zen: MIT License

Other important data sources:
- CBETA corpus: non-commercial terms
- CC-CEDICT: `CC BY-SA 4.0`

See `THIRD_PARTY_NOTICES.txt` for details.

## Short Version

Read Zen is now a full working environment for CBETA Zen study and translation:
- read side by side
- translate with structure-aware tools
- search with context and exports
- build Scholar collections
- manage terms, masters, notes, reviews, and tags
- sync personal and shared work without living in Git

Built for actual use, not demos.
