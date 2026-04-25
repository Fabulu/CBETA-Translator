using System;
using System.Collections.Generic;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class OnboardingTourService
{
    public List<TourStep> Steps { get; } = new();
    public int CurrentIndex { get; private set; } = -1;
    public TourStep? CurrentStep => CurrentIndex >= 0 && CurrentIndex < Steps.Count ? Steps[CurrentIndex] : null;
    public bool IsActive { get; private set; }

    /// <summary>
    /// True when the service is running the mandatory setup phase (steps 0
    /// through <see cref="SetupStepCount"/>-1). False during the optional
    /// feature tour. Controls what progress text the tooltip shows.
    /// </summary>
    public bool IsInSetupPhase { get; private set; }

    /// <summary>Number of mandatory setup steps (welcome → git → download → index).</summary>
    public int SetupStepCount { get; private set; }

    /// <summary>Number of optional feature-tour steps (everything after setup).</summary>
    public int FeatureTourStepCount => Steps.Count - SetupStepCount;

    /// <summary>
    /// Returns the step index relative to the CURRENT PHASE so the tooltip
    /// shows "Step 1 of 4" during setup and "Step 1 of 52" during the tour,
    /// never "Step 1 of 56".
    /// </summary>
    public int PhaseRelativeIndex => IsInSetupPhase
        ? CurrentIndex
        : CurrentIndex - SetupStepCount;

    /// <summary>Total steps in the current phase.</summary>
    public int PhaseStepCount => IsInSetupPhase
        ? SetupStepCount
        : FeatureTourStepCount;

    public event EventHandler<TourStep>? StepChanged;
    public event EventHandler? TourCompleted;
    public event EventHandler? TourSkipped;

    /// <summary>
    /// Fires when the mandatory setup phase finishes (last mandatory step
    /// advances). The UI should show a "Take the tour?" prompt; if the user
    /// declines, call <see cref="Complete"/>. If they accept, call
    /// <see cref="StartFeatureTour"/>.
    /// </summary>
    public event EventHandler? SetupPhaseCompleted;

    private DateTime _lastAdvanceUtc;

    /// <summary>
    /// Minimum milliseconds between advances. Prevents rapid-fire clicks from skipping steps.
    /// </summary>
    internal int DebounceMs { get; set; } = 200;

    public OnboardingTourService()
    {
        BuildSteps();
        SetupStepCount = Steps.FindIndex(s => !s.IsMandatory);
        if (SetupStepCount < 0) SetupStepCount = Steps.Count; // all mandatory (shouldn't happen)
    }

    /// <summary>
    /// Starts the mandatory setup phase. Progress shows "Step 1 of N"
    /// where N is the number of mandatory steps — not the total.
    /// </summary>
    public void Start(int startIndex = 0)
    {
        IsActive = true;
        IsInSetupPhase = true;
        CurrentIndex = Math.Max(0, Math.Min(startIndex, SetupStepCount - 1));
        StepChanged?.Invoke(this, CurrentStep!);
    }

    /// <summary>
    /// Starts the optional feature tour from the first non-mandatory step.
    /// Called when the user clicks "Take the Tour" after setup completes.
    /// </summary>
    public void StartFeatureTour()
    {
        if (SetupStepCount >= Steps.Count) { Complete(); return; }
        IsActive = true;
        IsInSetupPhase = false;
        CurrentIndex = SetupStepCount;
        StepChanged?.Invoke(this, CurrentStep!);
    }

    public void Next()
    {
        var now = DateTime.UtcNow;
        if ((now - _lastAdvanceUtc).TotalMilliseconds < DebounceMs) return;
        _lastAdvanceUtc = now;

        // End of setup phase → fire SetupPhaseCompleted instead of advancing
        // into the feature tour. The UI decides whether to continue.
        if (IsInSetupPhase && CurrentIndex >= SetupStepCount - 1)
        {
            IsActive = false;
            SetupPhaseCompleted?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (CurrentIndex < Steps.Count - 1)
        {
            CurrentIndex++;
            StepChanged?.Invoke(this, CurrentStep!);
        }
        else
        {
            Complete();
        }
    }

    public void Previous()
    {
        // Don't go back past the phase boundary
        int lowerBound = IsInSetupPhase ? 0 : SetupStepCount;
        if (CurrentIndex > lowerBound)
        {
            CurrentIndex--;
            StepChanged?.Invoke(this, CurrentStep!);
        }
    }

    /// <summary>
    /// True when the active step is part of the mandatory setup (git, download, index build).
    /// Skip is refused while this is true — the app can't function without those steps.
    /// </summary>
    public bool IsCurrentStepMandatory =>
        CurrentStep is { IsMandatory: true };

    public void Skip()
    {
        if (IsCurrentStepMandatory) return; // refuse — mandatory setup must be completed
        IsActive = false;
        TourSkipped?.Invoke(this, EventArgs.Empty);
    }

    public void Complete()
    {
        IsActive = false;
        TourCompleted?.Invoke(this, EventArgs.Empty);
    }

    public void AdvanceIfWaitingFor(string eventId)
    {
        var now = DateTime.UtcNow;
        if ((now - _lastAdvanceUtc).TotalMilliseconds < DebounceMs) return;

        if (CurrentStep?.WaitForEvent == eventId)
            Next();
    }

    private void BuildSteps()
    {
        // ===== Phase 1: Setup (steps 1-5) =====

        Steps.Add(new TourStep
        {
            Id = "welcome",
            Title = "Welcome to Read Zen",
            Body = "This tool helps you read, translate, and study classical Chinese Zen texts from two collections: CBETA and OpenZen.\n\nWe just need to do three quick things: check for Git, pick a folder, and download the texts.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center,
            IsMandatory = true
        });

        Steps.Add(new TourStep
        {
            Id = "git-check",
            Title = "Checking for Git...",
            Body = "We need Git to download texts. Checking your system...",
            Type = TourStepType.Wait,
            Placement = TourPlacement.Center,
            WaitForEvent = "git-check-complete",
            IsMandatory = true
        });

        Steps.Add(new TourStep
        {
            Id = "download-texts",
            Title = "Downloading the Text Collection",
            Body = "Choose where to store the Read Zen data, then download it. This downloads both text collections (CBETA and OpenZen) plus the translation workspace.\n\nAlready have texts on disk? Click 'Skip' to choose an existing folder instead.",
            Type = TourStepType.Wait,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnGitSync",
            SwitchToTabIndex = 3,
            WaitForEvent = "root-cloned",
            ActionButtonLabel = "Choose Folder + Download",
            CanSkipWait = true,
            IsMandatory = true
        });

        Steps.Add(new TourStep
        {
            Id = "building-index",
            Title = "Building Search Index...",
            Body = "Building a search index across the entire corpus. This runs automatically and takes a moment.",
            Type = TourStepType.Wait,
            Placement = TourPlacement.Center,
            WaitForEvent = "index-built",
            IsMandatory = true
        });

        Steps.Add(new TourStep
        {
            Id = "sidebar",
            Title = "Your Text Library",
            Body = "Here's the text library. Use the search box above to filter by title.\n\n\u2022 Red = not yet translated\n\u2022 Yellow = partially translated\n\u2022 Green = fully translated\n\nClick any text to start reading.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Right,
            TargetControlName = "FilesList"
        });

        // ===== Phase 1b: Corpora =====

        Steps.Add(new TourStep
        {
            Id = "two-collections",
            Title = "Two Text Collections",
            Body = "Read Zen includes two text collections:\n\n\u2022 CBETA \u2014 the full Chinese Buddhist canon (non-commercial use)\n\u2022 OpenZen \u2014 a growing open-access Zen corpus (CC0 and CC BY-SA licensed)\n\nBoth are downloaded automatically. OpenZen files are freely shareable; CBETA files carry a non-commercial restriction. You\u2019ll see each file\u2019s license in the top bar when you open it.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "corpus-switcher",
            Title = "Switch Between Corpora",
            Body = "This badge shows which collection you\u2019re browsing. Click it to switch between CBETA and OpenZen \u2014 the sidebar, search, and file list all update to match.\n\nThe badge changes colour so you always know which corpus is active.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnCorpusBadge"
        });

        Steps.Add(new TourStep
        {
            Id = "license-chip",
            Title = "Per-File License",
            Body = "This chip shows the license for the currently open file. Colours tell you at a glance:\n\n\u2022 Green \u2014 public domain or very permissive (CC0, MIT)\n\u2022 Amber \u2014 attribution required (CC BY-SA)\n\u2022 Orange \u2014 non-commercial (CBETA)\n\nClick the chip for full citation details, source links, and the short attribution line you can copy for your own work.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnLicenseChipTopBar"
        });

        Steps.Add(new TourStep
        {
            Id = "provenance-panel",
            Title = "Source Documentation",
            Body = "Check the \u2018Provenance\u2019 checkbox to see where each text comes from: source witnesses, SHA-256 verification hashes, editorial notes, and the full documentation chain.\n\nFor OpenZen files like the 1632 Wumenguan, you can expand the witness verification ledger, case completeness audit, and reading edition notes right in the panel.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "ChkProvenance"
        });

        // ===== Phase 2: Reading =====

        Steps.Add(new TourStep
        {
            Id = "open-gateless-barrier",
            Title = "Opening Your First Text",
            Body = "We've opened the Gateless Barrier (\u7121\u9580\u95dc) by Wumen Huikai \u2014 one of the most famous Zen texts.\nYou'll see it in the Reader tab.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 0,
            TargetControlName = "ReadableView",
            AutoOpenRelPath = "T/T48/T48n2005.xml"
        });

        Steps.Add(new TourStep
        {
            Id = "reader-panes",
            Title = "Side-by-Side Reading",
            Body = "Left pane = original Chinese. Right pane = English translation.\n\nTry it now: click on any Chinese sentence on the left. The corresponding English text highlights on the right \u2014 and vice versa.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center,
            TargetControlName = "TwoPaneGrid"
        });

        Steps.Add(new TourStep
        {
            Id = "hover-dictionary",
            Title = "Built-in Dictionary",
            Body = "Hover over any Chinese character to see its CC-CEDICT dictionary definition.\nLiterary Chinese particles (\u4e4b, \u4e4e, \u8005, \u4e5f) also show grammar notes explaining their function.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "EditorOriginal"
        });

        Steps.Add(new TourStep
        {
            Id = "study-panel",
            Title = "Study Panel",
            Body = "Check the \'Study\' checkbox in the Reader toolbar to open the Study Panel.\nAs you move through the text, it shows dictionary definitions, relevant terms from your dictionary, and similar translations from other texts.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Right,
            TargetControlName = "ChkStudyPanel",
            SwitchToTabIndex = 0
        });

        Steps.Add(new TourStep
        {
            Id = "reader-dictionary-button",
            Title = "Open the Zen Dictionary",
            Body = "The Dict button opens the full Zen Dictionary window, where you can manage terminology and see how terms are used across all the texts \u2014 right while you're reading.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnDictionary",
            SwitchToTabIndex = 0
        });

        Steps.Add(new TourStep
        {
            Id = "community-notes",
            Title = "Community Notes",
            Body = "Blue Community markers in the text are shared notes from other readers — click one to read it. Use this button to add your own note to the current passage.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnAddCommunityNote"
        });

        Steps.Add(new TourStep
        {
            Id = "footnote-colors",
            Title = "Footnote Color Coding",
            Body = "Orange = original text footnotes from the source.\nGrey = CBETA editorial notes.\nBlue = community notes added by users like you.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "zoom-controls",
            Title = "Zoom In and Out",
            Body = "Hold Ctrl and scroll the mouse wheel to zoom in or out. Ctrl+0 resets to the default size. Works in Reader and Translate.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "right-click",
            Title = "Right-Click to Collect",
            Body = "Right-click selected text to add it to Scholar, create zen:// deep links, copy shareable URLs, or search the corpus for the selected phrase.\nThis works in Reader, Translate, and Search.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "reader-compare-tools",
            Title = "Compare in Reader",
            Body = "Reader has two separate compare tools. Use Compare Translations in the top bar to compare translation sources for the current text. In Coding Mode, use Compare next to the tag-user picker to compare tag layers.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center,
            SwitchToTabIndex = 0
        });

        // ===== Phase 2b: Coding / Tagging (steps 12-15) =====

        Steps.Add(new TourStep
        {
            Id = "coding-mode",
            Title = "Coding Mode",
            Body = "Press F2 to enter Coding Mode - a keyboard-driven QDA workflow for systematic Zen text analysis.\nTag passages, track themes, and build a structured reading of any text.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 0,
            TargetControlName = "BtnCodingModeCompact"
        });

        Steps.Add(new TourStep
        {
            Id = "tag-editor",
            Title = "Tag Vocabulary Editor",
            Body = "Click ✎ Edit Tags to open the Tag Editor. Create hierarchical tags, assign colors from a 15-color auto-palette, and drag tags into code bar slots for quick access.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnEditTags"
        });

        Steps.Add(new TourStep
        {
            Id = "code-bar",
            Title = "Code Bar Shortcuts",
            Body = "Press 1-9 to apply the corresponding tag. Shift+1-9 switches pages (up to 18 pages = 162 tags).\nW selects a passage, E/Q expand or shrink the selection, Tab skips to the next untagged passage.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "CodeBarSlots"
        });

        Steps.Add(new TourStep
        {
            Id = "community-tags",
            Title = "View Community Tags",
            Body = "Use the user picker to view other scholars' tagging work on the same text.\nSwitch back to \"My Tags\" to resume your own coding session.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "CmbTagUser"
        });

        // ===== Phase 3: Translation (steps 16-22) =====

        Steps.Add(new TourStep
        {
            Id = "translate-tab",
            Title = "The Translation Editor",
            Body = "Switching to the Translate tab.\n\nThis is where you translate. Each passage shows the Chinese original on top and your English translation below. Use the dropdown at the top to switch between the community translation, your own, or other users\' work. Move between passages with Alt+\u2190 / Alt+\u2192.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 1,
            TargetControlName = "TranslationView"
        });

        Steps.Add(new TourStep
        {
            Id = "mode-buttons",
            Title = "Text Sections",
            Body = "Translate works in two sections: Body (the main text) and Notes (footnotes).\nSwitch between them with these buttons, or press Ctrl+2 for Body and Ctrl+3 for Notes.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnModeBody"
        });

        Steps.Add(new TourStep
        {
            Id = "copy-for-ai",
            Title = "Copy for AI Translation",
            Body = "Click 'Copy for AI' to grab up to 100 untranslated lines with instructions, ready to paste into ChatGPT, Claude, or DeepSeek.\n\nThe AI will return numbered translations you can paste back in.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnCopyChunkPrompt"
        });

        Steps.Add(new TourStep
        {
            Id = "paste-from-ai",
            Title = "Paste AI Translations",
            Body = "Copy the AI's response and click 'Paste from AI'. The app reads the numbers and puts each translation in the right place automatically.\n\nIt will warn you if any lines were skipped or merged.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnPasteByNumber"
        });

        Steps.Add(new TourStep
        {
            Id = "auto-fill-tm",
            Title = "Auto-Fill from Translation Memory",
            Body = "When an identical Chinese passage exists in an approved translation elsewhere in the corpus, the editor can fill it in automatically.\n\nLook for the blue TM-match highlighting in the assistant panel \u2014 a 100\u0025 match means the translation is ready to use with one click.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "AssistantPane"
        });

        Steps.Add(new TourStep
        {
            Id = "review-system",
            Title = "Review and Approve",
            Body = "After translating, review each line using the toolbar: Approve to accept, Reject to flag for rework, and Next ? to jump to the next unreviewed line. This is how you polish AI drafts into a finished translation that others can use.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnApproveSegment"
        });

        Steps.Add(new TourStep
        {
            Id = "assistant-panel",
            Title = "Translation Assistant",
            Body = "The assistant panel shows relevant dictionary terms, similar translations from other texts, and quality warnings to help you translate accurately. Toggle it with the Asst button. In the Reader tab, the Study panel offers a lighter version of the same help.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Left,
            TargetControlName = "AssistantPane",
            AutoJumpToBlock = 4
        });

        Steps.Add(new TourStep
        {
            Id = "save-translation",
            Title = "Save Your Work",
            Body = "Ctrl+S saves your translation. Each line in the editor should have one English translation \u2014 don't split a single translation across multiple lines. Batch pastes from AI still work fine. Other users' translations are read-only; you can only save your own.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnSave"
        });

        Steps.Add(new TourStep
        {
            Id = "fresh-start",
            Title = "Start Over",
            Body = "Need to reset a translation? Fresh Start clears your draft and returns the file to its untranslated state.\n\nThis only affects the current file and translation source. Your approved work in other files is safe.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnFreshStart"
        });

        // ===== Phase 4: Research (steps 23-30) =====

        Steps.Add(new TourStep
        {
            Id = "search-tab",
            Title = "Search the Corpus",
            Body = "Switching to the Search tab.\n\nSearch the entire text collection for Chinese or English phrases. The typeahead popup shows matching masters, titles, and hit counts as you type. Your recent searches appear below for quick re-runs.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 2,
            TargetControlName = "TxtQuery"
        });

        Steps.Add(new TourStep
        {
            Id = "search-results",
            Title = "Working with Results",
            Body = "Results are grouped by text with hit count badges. Each group starts collapsed \u2014 click to expand, or use \u201cShow N more\u201d to load additional matches. Double-click a result to open it, or right-click for links, sharing, \u201cOpen in new window\u201d, or Add to Scholar.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "ResultsTree"
        });

        Steps.Add(new TourStep
        {
            Id = "search-post-filter",
            Title = "Filter After Searching",
            Body = "After a search completes, use the post-search filter to narrow results by master, text, or translation status without re-running the query. Combine multiple master chips for intersection filtering.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 2
        });

        Steps.Add(new TourStep
        {
            Id = "search-insights",
            Title = "Search Insights",
            Body = "Expand the Insights panel below search results to see bar charts and scatter plots showing how your search terms distribute across texts, masters, and time periods.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 2
        });

        Steps.Add(new TourStep
        {
            Id = "search-export",
            Title = "Export Search Results",
            Body = "Search results can be exported for use outside Read Zen: HTML for sharing, CSV for spreadsheets, or BibTeX and CSL-JSON for academic citation managers.\n\nRight-click any result row for more options including Add to Scholar and passage deep links.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnExport",
            SwitchToTabIndex = 2
        });

        Steps.Add(new TourStep
        {
            Id = "scholar-tab",
            Title = "Your Research Workspace",
            Body = "This is the Scholar tab \u2014 your research workspace.\n\nWe\u2019ve added a sample passage from the Gateless Barrier so you can see how it looks. You\u2019ll build your own collections over time by right-clicking text in Reader, Translate, or Search.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center,
            SwitchToTabIndex = 4
        });

        Steps.Add(new TourStep
        {
            Id = "scholar-collections",
            Title = "Collections and Workspace",
            Body = "Scholar has three sections:\n\n\u2022 Collections \u2014 your local notebooks\n\u2022 Workspace \u2014 where you edit and compare passages\n\u2022 Shared \u2014 other users\u2019 published collections\n\nYou build collections over time by adding passages from Reader, Translate, or Search.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "scholar-shared",
            Title = "Shared Collections",
            Body = "If you don't have any collections yet, Scholar shows shared ones from other users first. Browse their work, and use Adopt to copy passages you like into your own collection.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center,
            SwitchToTabIndex = 4,
            TargetControlName = "ScholarView"
        });

        Steps.Add(new TourStep
        {
            Id = "adding-passages",
            Title = "Adding Passages",
            Body = "Right-click text in Reader, Translate, or Search to add it to Scholar. If you don't have a collection yet, one is created automatically the first time you add something. Selecting multiple lines captures longer passages automatically.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "context-menu-links",
            Title = "Context Menu & Inline Links",
            Body = "Right-click passages and text selections to copy deep links, add to collections, or connect related passages. Scholar passages can also link to one another with relation types like quotes, parallels, or contradicts.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "passage-detail",
            Title = "Passage Details",
            Body = "Click the sample passage to see its details. Each passage can have tags, master names, notes, doctrinal categories, and links to other passages.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Left,
            TargetControlName = "ScholarView"
        });

        Steps.Add(new TourStep
        {
            Id = "scholar-tools",
            Title = "Compare, Graph, and Exports",
            Body = "Use Compare to select 2-4 passages and view them side by side. Scholar can export your collections in readable formats or academic ones (CSV, BibTeX, CSL-JSON) for use in papers.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center,
            TargetControlName = null
        });

        Steps.Add(new TourStep
        {
            Id = "zen-dictionary",
            Title = "Zen Dictionary",
            Body = "The Zen Dictionary (Ctrl+D) manages translation terminology.\nSelect any term to see everywhere it appears across the corpus, sorted by historical date.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnDictionary"
        });

        Steps.Add(new TourStep
        {
            Id = "masters-tab",
            Title = "Zen Masters Tab",
            Body = "The Masters tab is its own first-class workspace. Read Zen ships with **301 Chan/Zen masters** from Bodhidharma through the late Ming, with dates, schools, lineage connections, biographies, and 400+ reference links.\n\nIt has three sub-views: List, Corpus, and Lineage Web.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 5,
            TargetControlName = "BtnOpenMasters"
        });

        Steps.Add(new TourStep
        {
            Id = "masters-list",
            Title = "Browse Masters",
            Body = "The List view lets you filter through all 298 masters, see their bio, school affiliation, and lineage connections. Teacher and student names are clickable \u2014 jump between profiles to trace any lineage.\n\nRight-click a master for **Copy Link** / **Copy Reddit Link** to share their web profile, or **Edit Dates** to fix metadata in place.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center,
            SwitchToTabIndex = 5
        });

        Steps.Add(new TourStep
        {
            Id = "masters-corpus",
            Title = "Master Corpus Search",
            Body = "The Corpus view shows you which texts in CBETA and OpenZen mention each master \u2014 split into **primary** appearances (master is author/subject) and **secondary** (mentioned/quoted). It runs over all ~5,000 corpus files automatically and gives you snippets with context.\n\nA concept-name filter (\u6cd5\u773c, \u7121\u9580, \u5927\u6167, \u570b\u5e2b, \u516d\u7956) keeps Buddhist concept-words from being mistaken for personal names.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center,
            SwitchToTabIndex = 5
        });

        Steps.Add(new TourStep
        {
            Id = "masters-lineage",
            Title = "Lineage Web",
            Body = "The Lineage Web is an interactive graph of master\u2013student relationships. Pan with the mouse, zoom with the wheel or the **zoom slider**, search for a master, and click **Center** to recenter.\n\nThe Y-axis is temporal \u2014 death year drives vertical position \u2014 so chronological flow is visible at a glance. School colors follow modern scholarship (Hongzhou, Caodong, Yunmen, Linji, Heze, Early Chan).",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center,
            SwitchToTabIndex = 5
        });

        Steps.Add(new TourStep
        {
            Id = "masters-web-profile",
            Title = "Shareable Web Profiles",
            Body = "Every master has a public web profile at **readzen.pages.dev/master/{Name}** \u2014 underscore URLs, no hash routing, Reddit/Twitter/email friendly.\n\nExamples:\n\u2022 readzen.pages.dev/master/Linji_Yixuan\n\u2022 readzen.pages.dev/master/Wansong_Xingxiu\n\nIn the Reader study panel, when a passage mentions a master, a bio card appears with a **View Master \u2192** button that jumps to the full profile.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "zen-master-manager",
            Title = "Open from Anywhere",
            Body = "You can also open the Zen Master Manager directly from `zen://master/...` deep links shared by other users \u2014 the link routes straight to the right master's profile inside the app.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center,
            SwitchToTabIndex = 5
        });

        Steps.Add(new TourStep
        {
            Id = "deep-links",
            Title = "Share Links to Any Passage",
            Body = "Right-click any text, search result, or file to copy a shareable zen:// link. You can link to specific passages, dictionary terms, searches, tags, Zen masters, and more. Send links to colleagues \u2014 clicking one opens the exact spot in the app.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        // ===== Phase 5: Community =====

        Steps.Add(new TourStep
        {
            Id = "multi-corpus-sync",
            Title = "Syncing Both Corpora",
            Body = "When you click Sync, Read Zen updates both CBETA and OpenZen in one operation. Your personal translations, notes, tags, and terminology are preserved for both corpora.\n\nNew OpenZen content (like new woodblock transcriptions) arrives automatically on sync.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnGitSync",
            SwitchToTabIndex = 3
        });

        Steps.Add(new TourStep
        {
            Id = "git-tab",
            Title = "Community Sync",
            Body = "Switching to the Community tab.\n\nUse Sync to share your work and get updates from other translators. Translations and community materials (dictionary, notes) are shared separately. Sharing requires a free GitHub account, but downloading texts does not.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 3,
            TargetControlName = "BtnGitSync"
        });

        Steps.Add(new TourStep
        {
            Id = "git-advanced",
            Title = "Advanced Recovery",
            Body = "The advanced section is for troubleshooting \u2014 like undoing local changes or fixing sync problems. You probably won't need it. Stick with the main Sync button for everyday use.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnGitSync"
        });

        Steps.Add(new TourStep
        {
            Id = "translation-pr",
            Title = "Share Your Translation",
            Body = "When you\u2019re ready to share a translation with the community, Sync creates a pull request on GitHub automatically. The community can review it, and once approved, it becomes the shared translation that everyone sees.\n\nYou don\u2019t need to know Git \u2014 the app handles branches, commits, and PRs for you.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        // ===== Phase 5b: Critical Editions + Witnesses =====

        Steps.Add(new TourStep
        {
            Id = "witness-comparison",
            Title = "Compare Witnesses (Critical Editions)",
            Body = "Some OpenZen texts are critical editions \u2014 they reconstruct a reading from multiple historical witnesses. Open one and you'll find an **Edition Process** dialog with seven tabs: Sources / Timeline / Log / Process / Apparatus / Stats / Documents.\n\nIn the Apparatus tab, every disagreement between witnesses gets a **Compare witnesses** button. Click it to see all witness readings side by side, with differing readings shown first and identical ones collapsed by default.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "witness-text-viewer",
            Title = "Open a Witness's Full Text",
            Body = "Click any witness siglum (e.g. T1, A1, ndl-1632) in the comparison popup to open the **Witness Text Viewer** \u2014 a read-only window showing that witness's full delivered text, with copy + source-open + a status banner.\n\nWitness data lives in `witnesses.json` next to each edition. The reference implementation ships with the 1632 NDL Wumenguan (`pd.wumenguan-1632`).",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "command-palette",
            Title = "Command Palette",
            Body = "Press Ctrl+Shift+P to open the Command Palette \u2014 a quick-access overlay that lets you jump to any action, tab, or tool by typing a few letters. It works from anywhere in the app.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "toast-notifications",
            Title = "Toast Notifications",
            Body = "When background tasks finish \u2014 like building the search index or syncing \u2014 a small toast notification appears briefly in the corner so you know it\u2019s done without interrupting your work.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "tour-complete",
            Title = "You're Ready!",
            Body = "You now know everything you need to read, translate, research, and share classical Chinese Zen texts.\n\nTo restart this tour later, go to Settings \u2192 Onboarding Tour.\n\nHappy studying! \ud83d\udcda",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });
    }
}




