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

    public event EventHandler<TourStep>? StepChanged;
    public event EventHandler? TourCompleted;
    public event EventHandler? TourSkipped;

    public OnboardingTourService()
    {
        BuildSteps();
    }

    public void Start(int startIndex = 0)
    {
        IsActive = true;
        CurrentIndex = Math.Max(0, Math.Min(startIndex, Steps.Count - 1));
        StepChanged?.Invoke(this, CurrentStep!);
    }

    public void Next()
    {
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
        if (CurrentIndex > 0)
        {
            CurrentIndex--;
            StepChanged?.Invoke(this, CurrentStep!);
        }
    }

    public void Skip()
    {
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
            Body = "This tool helps you read, translate, and study classical Chinese Zen texts from the CBETA corpus.\n\nLet's get you set up \u2014 it only takes a minute.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "git-check",
            Title = "Checking for Git...",
            Body = "We need Git to download texts. Checking your system...",
            Type = TourStepType.Wait,
            Placement = TourPlacement.Center,
            WaitForEvent = "git-check-complete"
        });

        Steps.Add(new TourStep
        {
            Id = "download-texts",
            Title = "Downloading the Text Collection",
            Body = "Choose where to store the Read Zen data, then download it. This downloads both the original text corpus and the translation workspace.\n\nAlready have texts on disk? Click 'Skip' to choose an existing folder instead.",
            Type = TourStepType.Wait,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnGitSync",
            SwitchToTabIndex = 3,
            WaitForEvent = "root-cloned",
            ActionButtonLabel = "Choose Folder + Download",
            CanSkipWait = true
        });

        Steps.Add(new TourStep
        {
            Id = "building-index",
            Title = "Building Search Index...",
            Body = "Building a search index across the entire corpus. This runs automatically and takes a moment.",
            Type = TourStepType.Wait,
            Placement = TourPlacement.Center,
            WaitForEvent = "index-built"
        });

        Steps.Add(new TourStep
        {
            Id = "sidebar",
            Title = "Your Text Library",
            Body = "Here are all the texts in the collection. Use the search box above the list to filter titles quickly. You can also collapse the sidebar or the top command bar later if you want more reading space.\n\nRed = not yet translated\nYellow = partially translated\nGreen = fully translated\n\nClick any text to start reading.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Right,
            TargetControlName = "FilesList"
        });

        // ===== Phase 2: Reading (steps 6-11) =====

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
            Placement = TourPlacement.Center
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
            Body = "Check the \'Study\' checkbox in the Reader toolbar to open the Study Panel.\nIt updates as you move through the original text and shows dictionary help, recognized terms from your dictionary, and translation memory matches.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Right,
            TargetControlName = "ChkStudyPanel",
            SwitchToTabIndex = 0
        });

        Steps.Add(new TourStep
        {
            Id = "reader-dictionary-button",
            Title = "Open the Zen Dictionary",
            Body = "Reader also has a Dict button when you want the full Zen Dictionary window, not just hover lookups. Use it to manage terminology and inspect corpus usage directly while reading.",
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
            Id = "right-click",
            Title = "Right-Click to Collect",
            Body = "Right-click selected text to add it to Scholar, create zen:// deep links, or copy shareable URLs.\nThis works in Reader, Translate, and Search.",
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
            Body = "Press 1-9 to apply the corresponding tag. Shift+1-9 switches pages (up to 18 pages = 162 tags).\nW selects a block, E/Q expand or shrink the selection, Tab skips to the next untagged block.",
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
            Body = "Switching to the Translate tab.\n\nThis is where translations happen. The editor shows numbered blocks: Chinese on top, English below. Use the source selector to switch between Community, your own translation, and other users\' views. Navigate between blocks with Alt+\u2190 and Alt+\u2192.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 1,
            TargetControlName = "TranslationView"
        });

        Steps.Add(new TourStep
        {
            Id = "mode-buttons",
            Title = "Text Sections",
            Body = "Translate works in two sections: Body (main text) and Notes (footnotes).\nSwitch between them with these buttons or Ctrl+2 / Ctrl+3.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnModeBody"
        });

        Steps.Add(new TourStep
        {
            Id = "copy-for-ai",
            Title = "Copy for AI Translation",
            Body = "Select any block, or just click 'Copy for AI' to automatically grab up to 100 untranslated blocks with instructions.\n\nPaste into ChatGPT, Claude, or DeepSeek. The AI returns numbered translations.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnCopyChunkPrompt"
        });

        Steps.Add(new TourStep
        {
            Id = "paste-from-ai",
            Title = "Paste AI Translations",
            Body = "Copy the AI's output and click 'Paste from AI'. No need to select anything \u2014 the app matches block numbers automatically, in any order.\n\nIt catches errors like skipped or combined lines.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnPasteByNumber"
        });

        Steps.Add(new TourStep
        {
            Id = "review-system",
            Title = "Review and Approve",
            Body = "After translating, review each block with the visible toolbar controls: Approve, Reject, and Next ? for the next unresolved block. Other users can see review status, so this is how you clean up AI drafts into a usable translation.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnApproveSegment"
        });

        Steps.Add(new TourStep
        {
            Id = "assistant-panel",
            Title = "Translation Assistant",
            Body = "The assistant panel shows recognized terminology, translation memory matches, and quality warnings. Use the Asst toggle to show or hide it; its highlights follow along with the panel. In Reader, the Study panel gives you a lighter reading-focused version of this workflow.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Left,
            TargetControlName = "AssistantPane",
            AutoJumpToBlock = 4
        });

        Steps.Add(new TourStep
        {
            Id = "save-translation",
            Title = "Save Your Work",
            Body = "Ctrl+S saves your current translation source. Keep exactly one EN line per block in the editor; multiline EN inside a single block is not supported. Large numbered batch pastes across many blocks still work normally. Other users\' translation sources are read-only.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnSave"
        });

        // ===== Phase 4: Research (steps 23-30) =====

        Steps.Add(new TourStep
        {
            Id = "search-tab",
            Title = "Search the Corpus",
            Body = "Switching to the Search tab.\n\nSearch the corpus for Chinese text or English phrases. Use the Original and Translated toggles to choose which side to search, then refine with Zen only, Status, Tag, and KWIC width. When both sides are available, results can show paired bilingual context.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 2,
            TargetControlName = "TxtQuery"
        });

        Steps.Add(new TourStep
        {
            Id = "search-results",
            Title = "Working with Results",
            Body = "Results show KWIC context around the match. When both sides are available, each row can show paired Chinese and English context. Double-click a result to open it. Right-click a result row for passage links, shareable links, search-state links, and Add to Scholar.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "ResultsTree"
        });

        Steps.Add(new TourStep
        {
            Id = "scholar-tab",
            Title = "Your Research Workspace",
            Body = "This is the Scholar tab \u2014 your research workspace.\n\nIt\u2019s empty right now because you haven\u2019t collected any passages yet. As you read and translate, you can right-click text to add passages here. They\u2019ll appear in your collections for comparison, annotation, and export.",
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
            Body = "If you have no local collections yet but shared ones exist, Scholar can open on Shared first. Browse another user, inspect their collection, choose a local target collection, and use Adopt to copy passages into your own workspace.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center,
            SwitchToTabIndex = 4,
            TargetControlName = "ScholarView"
        });

        Steps.Add(new TourStep
        {
            Id = "adding-passages",
            Title = "Adding Passages",
            Body = "Right-click text in Reader, Translate, or Search to add it to Scholar. If you do not have a writable local collection yet, Scholar creates one on your first successful add or adopt flow. Multi-block selections capture longer snippets automatically.",
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
            Body = "Each passage has: tags, master names, notes, doctrinal categorization (Topic, Form, Lineage), and cross-reference links to other passages.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "scholar-tools",
            Title = "Compare, Graph, and Exports",
            Body = "Use Compare to enter checkbox mode, then pick 2-4 passages and continue into the compare window. Scholar exports include readable formats and academic ones like CSV, BibTeX, CSL-JSON, and paper-draft output.",
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
            Id = "zen-master-manager",
            Title = "Zen Master Manager",
            Body = "Use the Zen Master Manager to browse master records, aliases, dates, and community variants in one place. It centralizes data that used to be scattered across different dialogs, and zen:// master links can open a master directly there.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center,
            SwitchToTabIndex = 4,
            TargetControlName = "ScholarView"
        });

        Steps.Add(new TourStep
        {
            Id = "deep-links",
            Title = "Share Links to Any Passage",
            Body = "Right-click text selections, search results, or files to copy zen:// deep links. Link types now include dictionary terms, Scholar resources, corpus searches, tags, Zen masters, and compare views. Share with colleagues — clicking opens the exact resource in the app.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        // ===== Phase 5: Community =====

        Steps.Add(new TourStep
        {
            Id = "git-tab",
            Title = "Community Sync",
            Body = "Switching to the Community tab.\n\nUse Sync when you want GitHub-backed sharing and updates. Community materials and selected translated text are handled as separate sync/share flows. Sharing requires GitHub login, but the first-run text download does not.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 3,
            TargetControlName = "BtnGitSync"
        });

        Steps.Add(new TourStep
        {
            Id = "git-advanced",
            Title = "Advanced Recovery",
            Body = "The advanced area is mainly for recovery actions like discarding local changes or fixing a confused sync state. Most users should stay with the main Sync workflow.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnGitSync"
        });

        Steps.Add(new TourStep
        {
            Id = "tour-complete",
            Title = "You're Ready!",
            Body = "You now know everything you need to read, translate, research, and share classical Chinese Zen texts.\n\nTo restart this tour later, go to Settings.\n\nHappy studying! \ud83d\udcda",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });
    }
}




