# Renovation Prompt

Copy everything below this line and paste it as your prompt to Claude in the CBETA-Translator directory.

---

Read `RENOVATION-FINDINGS.md` in this directory. It contains detailed findings from a dry-run renovation performed against a stale copy of this codebase (64 commits behind). The architectural patterns are sound but the code has changed significantly since then. Everything must be re-verified.

## Your mission: Full UI/UX + MVVM renovation of this app

### Phase 0: Deep Recon (DO THIS FIRST)

The app has grown a lot. Before doing anything, spawn **parallel recon agents** to map the CURRENT state:

1. **Agent 1 - View inventory**: Glob for all `*.axaml` and `*.axaml.cs` files. For each view, report: filename, line count of code-behind, whether it has a ViewModel, whether it uses data binding or FindControl. List any NEW views not mentioned in RENOVATION-FINDINGS.md.

2. **Agent 2 - Service inventory**: Glob for all files in `Services/`. For each service, report: filename, line count, whether it has an interface, whether it's registered in DI (check if ServiceCollectionExtensions.cs or similar exists). List any NEW services not in the findings.

3. **Agent 3 - Model inventory**: Glob for all files in `Models/`. Report any new models.

4. **Agent 4 - Infrastructure audit**: Check for: ViewModels/ folder, DI setup (App.axaml.cs), NuGet packages (CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection), Messages/ folder, test project. Report what already exists vs what's missing.

5. **Agent 5 - Git delta**: Run `git log --oneline -70` and `git diff --stat` against the oldest common ancestor. Identify the major areas of change since the findings were written.

6. **Agent 6 - CLAUDE.md and docs**: Read CLAUDE.md, BUILD.md, CONTRIBUTING.md, and any other documentation. Report constraints, conventions, or instructions that must be followed.

Wait for ALL 6 agents to complete before proceeding.

### Phase 1: Architect (use Opus model)

Based on the recon results AND the renovation findings, create a detailed implementation plan. The findings document has a proven 7-wave approach that worked on the old code:

1. Infrastructure (DI, CommunityToolkit.Mvvm, ViewModelBase, service interfaces)
2. Simplest ViewModel extraction (proof of concept)
3. Next simplest ViewModel
4. MainWindow orchestrator (most complex)
5. AvaloniaEdit-heavy views (ReadableTab, TranslationTab)
6. Remaining views (anything NEW that wasn't in the old app)
7. God service splits

But the plan must be adapted to the CURRENT codebase. Some things from the findings may already be done. New views and services may need to be included.

Key architectural decisions from the dry-run that worked:
- CommunityToolkit.Mvvm with [ObservableProperty] and [RelayCommand]
- Microsoft.Extensions.DependencyInjection with singleton services
- ViewModelBase : ObservableObject
- Code-behind bridge pattern for AvaloniaEdit (Func<>/Action<> delegates on ViewModel, wired by code-behind)
- Messages/AppMessages.cs with typed records for cross-VM communication
- Service interfaces for ALL services (enables mocking)
- Compiled bindings with x:DataType on every view

### Phase 2: UI/UX Improvements

The findings document has detailed per-view UX issues. Key ones to address:
- Tooltips on ALL interactive controls
- Empty state overlays with onboarding guidance on every tab
- Color-coded status bar (green success, red error, yellow warning)
- Git tab: terminal needs to be bigger on small screens, step progress indicators
- Search tab: inline validation errors, progress bars, fix Grid.Column bug
- Translation tab: make the ChatGPT workflow visible (it's completely hidden)
- Readable tab: annotation markers are disabled (Opacity=0), find scope indicator

### Phase 3: Implementation

Execute the plan wave by wave. After each wave:
- `dotnet build` must pass with 0 errors
- Commit the wave
- Move to the next

### Phase 4: Tests (use Opus model)

Write comprehensive unit tests for all ViewModels and split services. The dry-run produced 182 tests covering:
- ViewModel state transitions and PropertyChanged
- Command CanExecute logic
- Filter/search logic
- Static business logic methods
- Service splits

### Rules
- Always build and verify between waves
- Commit after each wave with a descriptive message
- If a view already has a ViewModel, skip it or enhance it
- If a service already has an interface, skip it
- Don't break existing functionality -- the app must run identically after each wave
- AvaloniaEdit views (ReadableTab, TranslationTab) will always have substantial code-behinds -- that's OK, just move LOGIC out
- Use the bridge pattern (Func/Action delegates) for views that can't fully bind
