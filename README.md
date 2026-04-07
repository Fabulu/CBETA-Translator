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

Read Zen expects a CBETA text root with the standard source and translation structure. The important folders are:

```text
root/
  xml-p5/                         original Chinese XML
  xml-p5t/                        shared/canonical translated XML
  community/
    translations/{user}/          personal translations
    termbases/{user}.jsonl        personal terminology share files
    collections/{user}.jsonl      personal Scholar collections share files
    reviews/{user}.jsonl          personal review share files
    tags/{user}.jsonl             personal tagging share files
    tag-vocabularies/{user}.json  personal tag vocabulary share files
```

In normal use:
- your writable personal translation source lives in `community/translations/{user}/...`
- community/canonical translations live in `xml-p5t/...`
- sync treats personal share files differently from shared canonical translation updates

## Reader

The Reader is the main side-by-side reading view.

What it does:
- Chinese on the left, selected English source on the right
- click text to highlight matching text on the other side
- switch translation source between community, your own work, and other users' work
- open the full Zen Dictionary with `Dict`
- use the `Study` panel for dictionary hits, recognized terminology, and translation memory support
- add and read community notes inline
- create deep links or add passages to Scholar from right-click menus

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
- assistant support with termbase hits, translation memory hits, and warnings
- per-block review controls
- a `Fresh Start` option to reset the current writable translation back to untranslated state with confirmation
- personal-vs-other-user translation source switching

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

The Community tab handles downloading texts, updating your local clone, and syncing shareable work through GitHub.

Important model:
- first-run text download is not the same thing as GitHub sharing
- personal share files can auto-merge through the community data flow
- canonical/shared translation updates are handled separately from personal translation storage
- recovery actions exist, but the normal `Sync` path is the intended workflow

Read Zen tries to protect local work during updates, but this is still a Git-backed workflow. If something feels destructive, stop and inspect before proceeding.

## Deep Links

Read Zen supports a broad `zen://` deep-link surface, plus shareable web links through the launcher site.

Supported families include:
- passages
- searches
- dictionary / termbase entries
- Scholar collections and passages
- tags
- Zen masters
- compare views

These links are used throughout the app from Reader, Translate, Search, Scholar, and the dictionary/master tooling.

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
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

### Linux
```bash
./eng/build-linux.sh Release true linux-x64
./run-cbeta-selfcontained.sh linux-x64
```

### macOS Intel
```bash
dotnet publish -c Release -r osx-x64 --self-contained true /p:PublishSingleFile=true
```

### macOS Apple Silicon
```bash
dotnet publish -c Release -r osx-arm64 --self-contained true /p:PublishSingleFile=true
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
