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

    public void Start()
    {
        IsActive = true;
        CurrentIndex = 0;
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
            Title = "Welcome to CBETA Translator",
            Body = "This tool helps you read, translate, and study classical Chinese Buddhist texts from the CBETA corpus.\n\nLet's get you set up \u2014 it only takes a minute.",
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
            Body = "We're downloading the CBETA Zen text collection. This may take a few minutes \u2014 it's a comprehensive library of classical Chinese texts with existing translations.\n\nGrab a cup of tea. \u2615",
            Type = TourStepType.Wait,
            Placement = TourPlacement.Bottom,
            TargetControlName = "GitView",
            SwitchToTabIndex = 3,
            WaitForEvent = "root-cloned"
        });

        Steps.Add(new TourStep
        {
            Id = "building-index",
            Title = "Building Search Index...",
            Body = "The app is building a search index so you can search across the entire corpus. This takes a moment.\n\nDid you know? The CBETA corpus contains thousands of Buddhist texts digitized from woodblock prints.",
            Type = TourStepType.Wait,
            Placement = TourPlacement.Center,
            WaitForEvent = "index-built"
        });

        Steps.Add(new TourStep
        {
            Id = "sidebar",
            Title = "Your Text Library",
            Body = "Here are all the texts in the collection.\n\n\ud83d\udd34 Red = not yet translated\n\ud83d\udfe1 Yellow = partially translated\n\ud83d\udfe2 Green = fully translated\n\nClick any text to start reading.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Right,
            TargetControlName = "FilesList"
        });

        // ===== Phase 2: Reading (steps 6-11) =====

        Steps.Add(new TourStep
        {
            Id = "readable-tab",
            Title = "The Reader View",
            Body = "This is the Reader tab \u2014 it shows Chinese text on the left and English translation on the right, side by side.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 0,
            TargetControlName = "ReadableView"
        });

        Steps.Add(new TourStep
        {
            Id = "linked-scrolling",
            Title = "Linked Scrolling",
            Body = "The Chinese and English panes scroll together. Click any sentence in Chinese to highlight the corresponding English, and vice versa.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "hover-dictionary",
            Title = "Hover Dictionary",
            Body = "Hover over any Chinese character to see its dictionary definition. This uses the CC-CEDICT dictionary built into the app.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "community-notes",
            Title = "Community Notes",
            Body = "You can add footnotes to any passage. Right-click on text in the reader to add a community note.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "zen-flag",
            Title = "Zen Text Filter",
            Body = "Use the 'Zen texts' checkbox in the sidebar to filter for Chan/Zen Buddhist texts specifically.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Right,
            TargetControlName = "ChkZenOnly"
        });

        Steps.Add(new TourStep
        {
            Id = "status-colors",
            Title = "Translation Status",
            Body = "The colored indicators show translation progress at a glance. Use the status filter dropdown to find texts that need work.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Right,
            TargetControlName = "CmbStatusFilter"
        });

        // ===== Phase 3: Translation (steps 12-18) =====

        Steps.Add(new TourStep
        {
            Id = "translate-tab",
            Title = "The Translation Editor",
            Body = "Switch to the Translate XML tab to begin translating. This is where you write English translations for Chinese source text.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 1,
            TargetControlName = "TranslationView"
        });

        Steps.Add(new TourStep
        {
            Id = "translation-blocks",
            Title = "Translation Blocks",
            Body = "Each text is divided into blocks. The Chinese source appears on the left, and you type the English translation on the right.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "translation-memory",
            Title = "Translation Memory",
            Body = "The assistant panel shows translation memory matches \u2014 previously translated similar passages. High-scoring matches can be auto-filled.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "review-workflow",
            Title = "Review Workflow",
            Body = "After translating, use Alt+A to approve a block or Alt+N to mark it as needing work. This helps track quality across the corpus.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "termbase",
            Title = "Terminology Database",
            Body = "Press Ctrl+D to open the terminology database. Consistent terminology is key to quality translation.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "save-translation",
            Title = "Saving Your Work",
            Body = "Press F9 to save your translation. The app saves to markdown format \u2014 the XML is regenerated when needed.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "keyboard-shortcuts",
            Title = "Keyboard Shortcuts",
            Body = "Key shortcuts:\n\u2022 Alt+A \u2014 Approve block\n\u2022 Alt+N \u2014 Needs work\n\u2022 Alt+\u2190/\u2192 \u2014 Navigate blocks\n\u2022 F9 \u2014 Save\n\u2022 Ctrl+D \u2014 Dictionary",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        // ===== Phase 4: Research (steps 19-26) =====

        Steps.Add(new TourStep
        {
            Id = "search-tab",
            Title = "Corpus Search",
            Body = "The Search tab lets you search across the entire CBETA corpus. Find parallel passages, track terminology usage, and discover related texts.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 2,
            TargetControlName = "SearchView"
        });

        Steps.Add(new TourStep
        {
            Id = "search-results",
            Title = "Search Results",
            Body = "Results show keyword-in-context (KWIC) snippets. Double-click any result to open it in a new window.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "search-cross-lb",
            Title = "Cross-Line Matching",
            Body = "Search works across line boundaries \u2014 it can find phrases that span multiple lines in the original text.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "scholar-tab",
            Title = "Scholar Collections",
            Body = "The Scholar tab helps you organize research. Collect passages, link related texts, and build study collections.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 4,
            TargetControlName = "ScholarView"
        });

        Steps.Add(new TourStep
        {
            Id = "scholar-passages",
            Title = "Collecting Passages",
            Body = "Select text in the Reader or Search tab and use 'Add to Scholar' to collect passages for study.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "scholar-linked-texts",
            Title = "Linked Texts",
            Body = "Right-click a file in the sidebar to link it to a scholar passage. This helps track intertextual references.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "parallel-passages",
            Title = "Parallel Passages",
            Body = "The app can detect parallel passages across texts \u2014 common in Buddhist literature where sutras were translated multiple times.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "grammar-reference",
            Title = "Grammar Reference",
            Body = "Built-in classical Chinese grammar notes help with difficult constructions you encounter while reading.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        // ===== Phase 5: Community (steps 27-30) =====

        Steps.Add(new TourStep
        {
            Id = "git-tab",
            Title = "Git Integration",
            Body = "The Git tab lets you contribute translations back to the community. No terminal needed \u2014 everything is built in.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 3,
            TargetControlName = "GitView"
        });

        Steps.Add(new TourStep
        {
            Id = "github-auth",
            Title = "GitHub Authentication",
            Body = "Connect your GitHub account to push translations and open pull requests directly from the app.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "community-data",
            Title = "Community Data Sync",
            Body = "Share your approved translation memory and terminology with the community. Fetch others' contributions to improve your translations.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });

        Steps.Add(new TourStep
        {
            Id = "tour-complete",
            Title = "You're All Set!",
            Body = "That covers the essentials. You can restart this tour anytime from Settings.\n\nHappy translating!",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });
    }
}
