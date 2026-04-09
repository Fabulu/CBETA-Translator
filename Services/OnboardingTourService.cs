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

        // ===== Phase 4: Research (steps 23-30) =====

        Steps.Add(new TourStep
        {
            Id = "search-tab",
            Title = "Search the Corpus",
            Body = "Switching to the Search tab.\n\nSearch the entire text collection for Chinese or English phrases. Use the Original and Translated toggles to choose which side to search, then narrow results by Zen-only filter, translation status, tags, or how much surrounding context to show.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            SwitchToTabIndex = 2,
            TargetControlName = "TxtQuery"
        });

        Steps.Add(new TourStep
        {
            Id = "search-results",
            Title = "Working with Results",
            Body = "Results show your search match with surrounding text for context. When translations exist, each row shows both the Chinese and English side by side. Double-click a result to open it. Right-click for links, sharing options, or to add it to Scholar.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Bottom,
            TargetControlName = "ResultsTree"
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
            Id = "zen-master-manager",
            Title = "Zen Master Manager",
            Body = "Use the Zen Master Manager to browse information about Zen masters \u2014 names, aliases, dates, and community contributions \u2014 all in one place. You can also open a master directly from links shared by other users.",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center,
            SwitchToTabIndex = 4,
            TargetControlName = "ScholarView"
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
            Id = "tour-complete",
            Title = "You're Ready!",
            Body = "You now know everything you need to read, translate, research, and share classical Chinese Zen texts.\n\nTo restart this tour later, go to Settings.\n\nHappy studying! \ud83d\udcda",
            Type = TourStepType.Passive,
            Placement = TourPlacement.Center
        });
    }
}




