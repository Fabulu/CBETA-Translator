# Read Zen App - Renovation Findings & Blueprint

**Source:** Dry-run performed on a stale copy (64 commits behind) at `C:\programmieren\CbetaReaderMVVM\CbetaTranslator.App`. Everything below was validated against that older snapshot. The real app has evolved significantly -- many files, services, views, and features may have been added, renamed, or restructured. All findings must be re-verified against the current codebase before acting on them.

**Date:** 2026-03-26

---

## Part 1: UI/UX Recon Findings

Six parallel recon agents audited every view and the full service layer. These findings reflect the OLD codebase and need re-verification, but the categories of problems are likely still relevant.

### MainWindow Shell & Navigation

| Category | Status | Priority |
|----------|--------|----------|
| Layout | Strong 3-tier (topbar, workspace, statusbar) | Low |
| Discoverability | Moderate-Weak: hamburger has no tooltip, "Open Root" is vague, color legend missing, "Zen texts" is cryptic jargon | **High** |
| Guidance/Onboarding | Weak: no welcome screen, no get-started overlay, buttons lack tooltips | **High** |
| Responsiveness | Rigid: nav panel hardcoded 320px, no auto-collapse | Low |
| Theme System | Excellent: 26-brush night/light modes, but no UI toggle to switch | Medium |
| Status Bar | Excellent feedback, but no color-coded severity (green/red/yellow) | Medium |
| Accessibility | Basic: good contrast ratios, but no AutomationProperties, Unicode icons not screen-reader friendly | Medium |

### ReadableTabView (Side-by-side reading)

| Category | Status | Priority |
|----------|--------|----------|
| Layout | Clear 2-pane with splitter | Low |
| Discoverability | Poor: cryptic "Zen text", no tooltips, annotation markers INVISIBLE (disabled overlay) | **High** |
| Guidance | Minimal: no empty-state message, no keyboard shortcut hints | **High** |
| Find UX | Works but silent scope switches between editors | Medium |
| Selection sync | 240ms polling timer, 11 suppression flags | Medium |
| Error states | Silent failures, no user feedback | **High** |
| Annotations | Markers intentionally disabled (Opacity=0), users click blind | **Critical** |

### TranslationTabView (ChatGPT-assisted XML editing)

| Category | Status | Priority |
|----------|--------|----------|
| Layout | Clean dual-editor | Low |
| Discoverability | Very poor: the entire ChatGPT workflow is invisible, button labels assume familiarity | **Critical** |
| Guidance | Missing entirely: no onboarding, no workflow explanation | **Critical** |
| Naming/Jargon | "Check XML (hacky)" in user-facing UI, "Select next 100 tags" unexplained | **High** |
| Workflow | Clever (select -> copy prompt -> paste to LLM -> paste back -> validate -> save) but completely invisible | **High** |
| Error handling | Functional but user-unfriendly, status bar only | Medium |

### SearchTabView

| Category | Status | Priority |
|----------|--------|----------|
| Layout | Good but has Grid.Column="9" bug (out of range) | Medium |
| Discoverability | Poor: must build index first but not obvious, Shift+click rebuild hidden | **High** |
| Guidance | Minimal: "Index not loaded." is not instructional | **High** |
| Error states | Validation errors go to status bar only, not inline | **High** |
| Co-occurrence panel | Expander header is empty string "", analysis undiscoverable | Medium |
| Search UX | Functional but developer-oriented progress text | Medium |

### GitTabView (Contribution workflow)

| Category | Status | Priority |
|----------|--------|----------|
| Visual hierarchy | Poor: log dominates 40% of space, no step grouping | **High** |
| Discoverability | Poor: 3-step workflow implicit, Git terminology assumes knowledge | **High** |
| Onboarding | Missing: no welcome, no step-by-step, OAuth device code buried in log | **High** |
| Terminal size | Too small on laptops, users can't copy code from it | **High** |
| For non-technical users | Unsuitable without significant UX work | **High** |

---

## Part 2: Architecture Recon Findings

### Code-Behind Bloat (as of old snapshot)

| File | Lines | What's crammed in |
|------|-------|--------------------|
| MainWindow.axaml.cs | ~1,553 | File navigation, dirty tracking (SHA1), config persistence, tab orchestration, zen filtering |
| ReadableTabView.axaml.cs | ~1,829 | XML rendering, annotations, dictionary hover, selection sync, find/search highlighting |
| TranslationTabView.axaml.cs | ~1,758 | XML parsing/editing, clipboard ops, community note CRUD, ChatGPT prompt building |
| GitTabView.axaml.cs | ~1,325 | Git clone/fetch/push, GitHub OAuth, PR creation, process management |
| SearchTabView.axaml.cs | ~842 | Bloom filter index, search execution, co-occurrence metrics, TSV export |
| **Total** | **~7,997** | **All business logic, zero in ViewModels** |

### Missing Architectural Layers

1. **Zero ViewModels** -- no INotifyPropertyChanged, no Commands, no observable collections, no reactive binding
2. **Zero Dependency Injection** -- every service instantiated with `new` directly in code-behind (16+ places)
3. **4 services missing interfaces** -- AppConfigService, CedictDictionaryService, IndexCacheService, SearchIndexService
4. **2 god services** -- SearchIndexService (1,275 lines: bloom index + search + co-occurrence + KWIC + scoring) and IndexCacheService (476 lines: cache + status computation + XML parsing + CJK detection)
5. **No unit tests viable** -- can't mock anything, can't test business logic separate from UI

### Service Inventory (as of old snapshot -- MAY HAVE CHANGED)

| Service | Lines | Has Interface? | Assessment |
|---------|-------|----------------|------------|
| AppConfigService | 51 | No | OK - thin |
| BloomSearchIndexService | 164 | No | OK - focused |
| CedictDictionaryService | 351 | ICedictDictionary | OK |
| FileService | 52 | IFileService | OK |
| GitHubApiService | 177 | IGitHubApiService | OK |
| GitHubAuthService | 176 | IGitHubAuthService | OK |
| GitRepoService | 349 | IGitRepoService | OK |
| IndexCacheService | 476 | No | **GOD SERVICE** |
| RenderedDocumentCacheService | 89 | No | OK |
| SearchIndexService | 1,275 | No | **GOD SERVICE** |
| SearchIndexSettings | 15 | No | OK |
| SelectionSyncService | 21 | ISelectionSyncService | OK |
| ZenTextsService | 107 | No | OK |

### Cross-View Communication (as observed)

- MainWindow holds references to all 4 tab views via `FindControl`
- MainWindow calls public methods on tab views: `SetContext()`, `SetXml()`, `SetRendered()`, `Clear()`
- Tab views raise `EventHandler`-based events: `Status`, `OpenFileRequested`, `SaveRequested`, `RootCloned`, `ZenFlagChanged`, `CommunityNoteInsertRequested`, `CommunityNoteDeleteRequested`
- No messaging bus, no mediator pattern

### Model Layer (GOOD)

12 model files (~459 lines total): AppConfig, FileNavItem, SearchModels, RenderedDocument, RenderSegment, DocAnnotation, Filestamp, TranslationStatus, CoocMetric/CoocRow, CedictModels, IndexCache. All thin data carriers, no domain logic.

---

## Part 3: What We Built (in the dry-run) -- Reference Implementation

These are the architectural patterns we validated. The actual code was built against the OLD snapshot and is NOT directly portable, but the patterns and approach are sound.

### Wave 1: Infrastructure
- Added `CommunityToolkit.Mvvm` (ObservableObject, [ObservableProperty], [RelayCommand]) and `Microsoft.Extensions.DependencyInjection`
- Created `ViewModelBase : ObservableObject`
- Created interfaces for all services missing them
- Created `ServiceCollectionExtensions.cs` as DI composition root
- Created `Messages/AppMessages.cs` with typed message records for cross-VM communication

### Wave 2+3: Simple ViewModel Extractions (SearchTab, GitTab)
- SearchTabView code-behind: 842 -> ~130 lines
- GitTabView code-behind: 1,325 -> ~150 lines
- Pattern: ViewModel owns all state ([ObservableProperty]) and logic ([RelayCommand]), code-behind keeps only InitializeComponent, file picker delegation, and event wiring
- XAML converted to compiled bindings with `x:DataType`

### Wave 4: Complex Orchestrator (MainWindow)
- MainWindow code-behind: 1,553 -> ~561 lines
- Bridge pattern for tab views not yet on MVVM: ViewModel exposes `Func<>` and `Action<>` delegates that code-behind wires to tab view methods
- Dirty tracking timer stays in code-behind (DispatcherTimer), calls ViewModel.CheckDirty()

### Wave 5+6: AvaloniaEdit-Heavy Views (ReadableTab, TranslationTab)
- These views have HEAVY AvaloniaEdit integration that MUST stay in code-behind: TextEditor manipulation, caret/selection, hover dictionary, search highlight renderers, visual tree walking
- Business logic moves to ViewModel; editor interaction stays in code-behind
- Code-behind syncs ViewModel property changes to editor controls via PropertyChanged listener

### Wave 7: God Service Splits
- SearchIndexService (1,275 lines) -> ISearchIndexBuilder (build/load), ISearchEngine (search), ICooccurrenceService (metrics)
- IndexCacheService (476 lines) -> IIndexCacheService (cache I/O), ITranslationStatusService (status computation + CJK detection)
- Concrete classes kept as facades implementing multiple interfaces; DI forwards singleton instances

### Tests: 182 ViewModel + service tests
- Manual mocks/stubs (no Moq dependency needed)
- Tests cover: state transitions, command CanExecute, filter logic, static business methods, PropertyChanged notifications

---

## Part 4: Key Risks and Gotchas

1. **AvaloniaEdit does NOT support standard Avalonia bindings for `Text` property** -- code-behind bridge pattern required for ReadableTab and TranslationTab
2. **Compiled bindings require `x:DataType` on every view** -- omitting it causes silent runtime failures
3. **DispatcherTimer and Dispatcher.UIThread calls from ViewModel break MVVM purity** -- pragmatic impurity accepted for Avalonia-specific scenarios
4. **Community note insert/delete flow spans 3 views** -- needs message-based flow: ReadableTabVM -> message -> MainWindowVM -> TranslationTabVM -> refresh all
5. **CedictDictionaryService has TWO separate instances** in old code (ReadableTab and TranslationTab each `new` their own) -- DI singleton fixes this
6. **Selection sync uses 240ms polling with 11 suppression flags** -- fragile, consider event-driven approach
7. **Annotation markers are intentionally disabled** (Opacity=0) -- major UX gap
8. **The entire ChatGPT translation workflow is invisible** to users -- needs onboarding overlay

---

## Part 5: What the Real App Likely Has That We Didn't See

The real app is 64 commits ahead. Expect:
- New views or tabs
- New services
- New models
- Changed method signatures
- Possibly some of these issues already fixed
- New features that need the same treatment
- Different file organization

**Everything in this document must be re-verified against the current codebase before implementation.**

