using System;
using System.Collections.Generic;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

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
            Body = "Choose where to store the Read Zen text collection, then download it. This only happens once.\n\nAlready have texts on disk? Click 'Skip' to choose an existing folder instead.",
            Type = TourStepType.Wait,
            Placement = TourPlacement.Bottom,
            TargetControlName = "GitView",
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
            Body = "Here are all the texts in the collection.\n\nRed = not yet translated\nYellow = partially translated\nGreen = fully translated\n\nClick any text to start reading.",
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
            TargetControlName = "ReadableView"
        });

        Steps.Add(new TourStep
        {
            Id = "reader-panes",
            Title = "Side-by-Side Reading",
            Body = "Left pane = original Chinese. Right pane = English translation.\nThe splitter between them can be dragged to adjust the ratio. Click any sentence to highlight its counterpart on the other side.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
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
            Body = "Check the 'Study' checkbox in the Reader toolbar to open the Study Panel.\nIt shows live dictionary lookups at your caret position, translation memory matches, and termbase hits \u2014 all updating as you read.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Right,
            TargetControlName = "ChkStudyPanel",
            SwitchToTabIndex = 0
        });

        Steps.Add(new TourStep
        {
            Id = "community-notes",
            Title = "Community Notes",
            Body = "Blue markers in the text are community notes \u2014 click one to read it.\nYou can add your own notes to any passage with this button.",
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
            Body = "Right-click selected text to add it to your Scholar collection, create deep links, or copy shareable URLs.\nThis works in the Reader, Translation, and Search tabs.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
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
            Body = "Click the pencil icon to open the Tag Editor. Create hierarchical tags, assign colors from a 15-color auto-palette, and drag tags into code bar slots for quick access.",
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
            Body = "Switching to the Translation tab.\n\nThis is where translations happen. The editor shows numbered blocks: Chinese on top, English below.\nNavigate between blocks with Alt+\u2190 and Alt+\u2192.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 1,
            TargetControlName = "TranslationView"
        });

        Steps.Add(new TourStep
        {
            Id = "mode-buttons",
            Title = "Text Sections",
            Body = "Texts have three parts: Header (title), Body (main text), and Notes (footnotes).\nSwitch between them with these buttons or Ctrl+1 / Ctrl+2 / Ctrl+3.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnModeHead"
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
            Body = "After translating, review each block:\n\u2022 Alt+A = Approve (moves to next unreviewed)\n\u2022 Alt+N = Needs Work\nOther users will see who approved what.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnApproveSegment"
        });

        Steps.Add(new TourStep
        {
            Id = "assistant-panel",
            Title = "Translation Assistant",
            Body = "The assistant panel shows: recognized terminology, similar translations from the translation memory, and quality warnings.\nIt updates automatically as you navigate blocks.\n\nIn the Reader tab, toggle the Study checkbox for similar lookups as you read, including live dictionary entries.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Left,
            TargetControlName = "AssistantPane"
        });

        Steps.Add(new TourStep
        {
            Id = "save-translation",
            Title = "Save Your Work",
            Body = "Ctrl+S saves your translation. The app writes clean TEI XML that preserves the original Chinese formatting.\nYour translator name is recorded automatically.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "BtnSave"
        });

        // ===== Phase 4: Research (steps 23-30) =====

        Steps.Add(new TourStep
        {
            Id = "search-tab",
            Title = "Search the Corpus",
            Body = "Switching to the Search tab.\n\nSearch the entire corpus for Chinese text or English phrases.\nThe search works across line breaks \u2014 CBETA often splits Chinese sentences mid-word.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 2,
            TargetControlName = "TxtQuery"
        });

        Steps.Add(new TourStep
        {
            Id = "search-results",
            Title = "Working with Results",
            Body = "Results show KWIC context (text before and after the match).\nDouble-click any result to open that text in a new reader window. Right-click to add to Scholar.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "ResultsTree"
        });

        Steps.Add(new TourStep
        {
            Id = "scholar-tab",
            Title = "Your Research Workspace",
            Body = "Switching to the Scholar tab.\n\nThe Scholar tab is your personal research workspace.\nCollect passages, organize by topic, compare texts, and export your findings.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 4,
            TargetControlName = "ScholarView"
        });

        Steps.Add(new TourStep
        {
            Id = "scholar-collections",
            Title = "Collections",
            Body = "Create named collections to organize your research.\nEach collection holds passages \u2014 snippets of Chinese + English text from anywhere in the corpus.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "PassagesList"
        });

        Steps.Add(new TourStep
        {
            Id = "graph-and-stats",
            Title = "Graph & Link Statistics",
            Body = "Switch to the Graph tab to see your collected passages as a network diagram \u2014 nodes are passages, edges are links colored by relation type.\nThe Links tab shows statistics: relation type distribution, coverage, and orphan passages.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center,
            SwitchToTabIndex = 4
        });

        Steps.Add(new TourStep
        {
            Id = "adding-passages",
            Title = "Adding Passages",
            Body = "Right-click text in the Reader, Translation, or Search tabs to add it to a collection.\nMulti-block selections capture entire paragraphs. Master names (like \u8d99\u5dde, \u5357\u6cc9) are auto-detected.\nUse the context menu to also create deep links or copy shareable URLs.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "context-menu-links",
            Title = "Context Menu & Inline Links",
            Body = "Right-click any passage to create cross-reference links, add to collections, or copy deep links.\nIn the Links tab, click '+ Link' to connect passages with semantic relations like 'quotes', 'parallels', or 'contradicts'.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "passage-detail",
            Title = "Passage Details",
            Body = "Each passage has: tags, master names, notes, doctrinal categorization (Topic, Form, Lineage), and cross-reference links to other passages.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Left,
            TargetControlName = "TxtZhText"
        });

        Steps.Add(new TourStep
        {
            Id = "scholar-tools",
            Title = "Compare, Graph, and Statistics",
            Body = "Compare passages side-by-side with shared character highlighting.\nSwitch to the Graph tab to visualize your passage network. The Links tab shows relation-type statistics and coverage.\n\nUse '+ Link' in the Links tab to connect related passages.",
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
            Id = "deep-links",
            Title = "Share Links to Any Passage",
            Body = "Right-click any text, search result, or file to copy a zen:// deep link.\nLink types: dictionary lookups, scholar collections, corpus searches, and tagged passages.\nShare with colleagues \u2014 clicking opens the exact resource in the app.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        // ===== Phase 5: Community =====

        Steps.Add(new TourStep
        {
            Id = "git-tab",
            Title = "Sync Your Work",
            Body = "Switching to the Git tab.\n\nClick Sync to share your work and get the latest texts and community data. That's it \u2014 one button does everything.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 3,
            TargetControlName = "GitView"
        });

        Steps.Add(new TourStep
        {
            Id = "git-advanced",
            Title = "Advanced Section",
            Body = "Advanced section has tools for submitting translation pull requests. Most users never need it.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "GitView"
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
