# Patterns from the QDA Tool Build

Notes on code patterns, architecture decisions, and agent workflows used during the
QDA tool build (2026-03-28/30). Many of these transfer directly to CBETA Translator.

## Agent Workflow Pattern

The most effective pattern for large feature batches:

```
Recon agents (explore codebase, web search)
  ↓
Opus Architect (consolidates findings → implementation plan)
  ↓
Implementer agents (parallel where possible, waves when dependencies exist)
  ↓
Opus Reviewer (code review, finds bugs)
  ↓
Opus QA (traces scenarios through actual code paths)
  ↓
Fix agent (addresses reviewer + QA findings)
  ↓
Test writer (writes xUnit tests against the implemented code)
  ↓
Commit
```

**Key learnings:**
- Parallel implementers work when they touch different files. Conflicts happen when
  two agents modify the same file — merge manually after.
- Recons before architecture prevent wasted implementation effort.
- The reviewer + QA combo catches different things: reviewer finds code-level bugs,
  QA finds workflow-level issues.
- Always run `dotnet build` and `dotnet test` after agents land — they sometimes
  produce code that compiles individually but conflicts with other agents' changes.

## Architecture: MVVM with Code-Behind

The QDA app uses CommunityToolkit.Mvvm with a pragmatic approach:

- **ViewModels** own all state and business logic (ObservableObject, ObservableProperty, RelayCommand)
- **Views** (code-behind) handle platform-specific things: file dialogs, AvaloniaEdit
  pointer events, drag-drop, media player
- **Services** are behind interfaces for testability (IProjectDatabase)
- **DI** via Microsoft.Extensions.DependencyInjection, registered in App.axaml.cs

**Why not strict MVVM?** AvaloniaEdit requires direct manipulation (Editor.Text,
Editor.Select, BackgroundRenderers). Media player needs LibVLC lifecycle management.
File dialogs need StorageProvider. These don't fit cleanly in a ViewModel.

**The compromise:** ViewModels handle logic, Views handle platform. Views call into
VM methods, VM fires events that Views handle. Works well in practice.

## Partial Classes for Large Code-Behind

When MainWindow.axaml.cs hit 2,852 lines, we split it:

```
MainWindow.axaml.cs           — constructor, lifecycle, file dialogs
MainWindow.MediaPlayer.cs     — LibVLC, timeline, subtitles, media keys
MainWindow.Coding.cs           — QWEASD selection, suggestions, coding
MainWindow.CodeTree.cs         — code bar, drag-drop, tree context menus
```

All declare `partial class MainWindow`. C# merges them at compile time. Fields
defined in any file are accessible from all others. This is the correct approach
for Avalonia code-behind — don't fight it, split it.

## SQLite Schema Migration Pattern

```csharp
private void MigrateSchema()
{
    // New columns: try/catch for "duplicate column"
    try { Exec("ALTER TABLE codes ADD COLUMN weight REAL NOT NULL DEFAULT 1.0;"); }
    catch (SqliteException ex) when (ex.Message.Contains("duplicate column",
        StringComparison.OrdinalIgnoreCase)) { }

    // New tables: CREATE TABLE IF NOT EXISTS (safe to re-run)
    Exec("CREATE TABLE IF NOT EXISTS audit_log (...)");

    // New indexes: CREATE INDEX IF NOT EXISTS
    Exec("CREATE INDEX IF NOT EXISTS idx_codes_parent ON codes(parent_id);");
}
```

Called from both `Create()` and `Open()`. Idempotent. No version tracking needed
for a single-dev app.

## TF-IDF Suggestion Service

Local AI that gets smarter as you code, with zero cloud/model dependencies:

1. Tokenize all coded segments (strip stopwords, lowercase, split on non-letter)
2. Compute IDF across all passages
3. Build sparse TF-IDF vectors per passage
4. On query: compute query vector, cosine similarity against all passages
5. Group by code, return top 3 codes above 40% threshold

**Incremental updates** (added in performance sprint):
- `AddPassage()`: tokenize one segment, update IDF, add vector — O(T) not O(N×T)
- `RemovePassage()`: remove vector, update IDF
- Accept slightly stale IDF (fine for ranking)

**Thread safety:** `lock (_indexLock)` around both mutations and rebuilds.
SuggestAsync runs on ThreadPool via Task.Run. InvalidateIndex called from UI thread.

## QDPX (REFI-QDA) Import/Export

The universal QDA interchange format. A ZIP containing:
- `project.qde` — XML in namespace `urn:QDA-XML:project:1.0`
- `Sources/*.txt` — plain text source documents

Key mappings:
- Our `Code` → `<Code guid name color isCodable>`
- Our `CodedSegment` → `<PlainTextSelection startPosition endPosition>` + `<Coding><CodeRef><SelectionRef>`
- Our `Memo` → `<Note>` with `<PlainTextContent>`
- Code hierarchy → nested `<Code>` elements
- Colors: our `#RRGGBB` → QDPX `#AARRGGBB` (prepend FF)

~200 lines total for both export and import. Uses System.Xml.Linq + System.IO.Compression.

## Keyboard-Driven Coding (QWEASD)

The killer feature. All single keys, active when text is selected:

```
  Q  W  E     — shrink sentence / whole paragraph / expand sentence
  A  S  D     — shrink word / snap to sentence / expand word
  Shift+      — adjust START instead of END
  Tab         — skip to next uncoded sentence
  B           — bookmark + advance
  N           — annotate (create memo on selection)
  1-9         — apply code from current page
  Space hold  — suppress auto-advance for multi-coding
```

Implementation: tunnel KeyDown handler on the AvaloniaEdit editor, checks
KeyModifiers and Editor.SelectionLength before acting. All handlers in
MainWindow.Coding.cs.

**Media player reuses the same layout:**
```
  S           — play/pause
  A/D         — skip ±5 seconds
  Q/E         — previous/next subtitle
  W           — mark start/end
  1-9         — apply code to marked time range
  Shift+A/D   — nudge mark start ±1sec
  Ctrl+A/D    — nudge mark end ±1sec
```

Same hand, same position, same mental model — just operating on time instead of text.

## Code Pages (18 × 9 slots)

For projects with 50+ codes, 9 quick-access slots aren't enough:
- Shift+1-9 switches to code pages 1-9
- Ctrl+1-9 switches to pages 10-18
- Ctrl+Shift+1-9 assigns selected code to slot
- Drag-drop to reorder within a page
- "Auto-arrange by frequency" sorts by usage count

Stored in `code_page_assignments(page, slot, code_id)` with UNIQUE(page, slot).

## HTML Visualization "Cheat"

Instead of building charting into the app, generate self-contained HTML files
and open in the default browser:

```csharp
var html = GenerateWordFrequencyHtml(db);
var path = Path.Combine(Path.GetTempPath(), $"qda_viz_{Guid.NewGuid():N}.html");
File.WriteAllText(path, html, Encoding.UTF8);
Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
```

5 visualizations, ~200 lines total, zero NuGet packages. Works cross-platform.
The HTML files are also shareable.

## Inter-Coder Reliability (Cohen's Kappa)

Compare two coders' work:
1. Load second coder's .qdpx or .qda into a temp database
2. Match codes by name (case-insensitive)
3. Match documents by filename (extension-stripped)
4. For each matched doc × code: discretize into 10-char windows
5. Count agreement (both coded / neither coded / disagreement)
6. Calculate κ = (P_observed - P_expected) / (1 - P_expected)

~130 lines. The 10-char window approach is standard in QDA literature.

## Embedded Media Player (LibVLCSharp)

Audio/video coding without building a media engine:
- LibVLCSharp wraps VLC's native player
- `_vlcPlayer.Play(media)` / `.Pause()` / `.SeekTo(timespan)`
- TimeChanged event fires on background thread → dispatch to UI via Dispatcher.UIThread.Post
- SRT/VTT parsed on import, character-to-timestamp mappings stored in DB
- Bidirectional: click text → seek media, click timeline → highlight text

**Key gotcha:** Don't `using var media = new Media(...)` — VLC references it async
after Play(). Store as a field, dispose explicitly.

## Dark Mode via Theme Resources

Define light/dark color sets in App.axaml ThemeDictionaries:
```xml
<ResourceDictionary.ThemeDictionaries>
  <ResourceDictionary x:Key="Light">
    <SolidColorBrush x:Key="AppBg" Color="#F3F3F3"/>
  </ResourceDictionary>
  <ResourceDictionary x:Key="Dark">
    <SolidColorBrush x:Key="AppBg" Color="#1E1E1E"/>
  </ResourceDictionary>
</ResourceDictionary.ThemeDictionaries>
```

Then use `{DynamicResource AppBg}` everywhere. Toggle with:
```csharp
app.RequestedThemeVariant = isDark ? ThemeVariant.Light : ThemeVariant.Dark;
```

**Lesson learned:** Replace ALL hardcoded colors in one pass. Missing even one
makes dark mode look broken.

## Test Strategy

- **FakeProjectDatabase** — full in-memory IProjectDatabase implementation (~400 lines)
  with proper cascade deletes. Enables testing everything without SQLite.
- **Test per feature area** — DatabaseTests, ViewModelTests, ServiceTests, ModelTests,
  MediaPlayerTests, NewFeaturesTests, PolishSprintTests
- **249 tests, all passing in <1 second**
- **Can't test:** UI code-behind (AvaloniaEdit, dialogs, drag-drop). Accept this.
  Test the VM and services thoroughly instead.

## Numbers

Built in one conversation session:
- 24 commits
- ~10,000 lines production code
- ~4,000 lines tests (249 passing)
- 93 verified features
- 60+ source files
- 12 database tables
- 18 dialog windows
- 30+ keyboard shortcuts
- 13 NuGet dependencies

From empty directory to feature-competitive with $1,400/yr NVivo.
