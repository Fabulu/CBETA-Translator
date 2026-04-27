// Views/TranslationTabView.axaml.cs
// Projection editor for IndexedTranslationService (Body / Notes)

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ReadZen.App.Views;

public partial class TranslationTabView : UserControl
{
    private const int AdjacentContextChars = 4;

    private Button? _btnModeBody, _btnModeNotes;
    private Button? _btnUndo, _btnRedo;
    private Button? _btnCopyChunkPrompt, _btnPasteByNumber, _btnNextUntranslated, _btnFindChineseInEn, _btnSave, _btnFreshStart, _btnRevert;
    private Button? _btnApproveSegment, _btnNeedsWorkSegment, _btnRejectSegment, _btnNextUnapproved;
    private CheckBox? _chkWrap;
    private ComboBox? _cmbChunkSize;
    private ComboBox? _cmbTranslationSource;
    private Button? _btnStarTranslation;
    private TextBlock? _txtModeInfo;
    private TextBlock? _txtQuickInfo;
    private TextBlock? _txtReviewState;
    private TextBlock? _txtProgress;
    private TextEditor? _editor;
    private Border? _emptyState;

    private readonly TranslationTabViewModel _vm;

    private CancellationTokenSource? _publishDebounce;

    private HoverDictionaryBehaviorEdit? _hoverDictionaryBehavior;
    private Canvas? _dictOverlayCanvas;
    private readonly ICedictDictionary _cedict = App.Services.GetRequiredService<ICedictDictionary>();
    private readonly IGrammarReferenceService _grammar = App.Services.GetRequiredService<IGrammarReferenceService>();

    public event EventHandler<TranslationEditMode>? ModeChanged;
    public event EventHandler? SaveRequested;
    public event EventHandler? FreshStartRequested;
    public event EventHandler? RevertRequested;
    public event EventHandler? HistoryRequested;
    public event EventHandler<string>? Status;

    private CheckBox? _chkAssistantVisible;
    private Border? _assistantPane;
    private GridSplitter? _assistantSplitter;
    private Grid? _editorAssistantGrid;

    private Button? _btnBuildReferenceTm;
    public event EventHandler? BuildReferenceTmRequested;

    private Button? _btnManageTerms;
    public event EventHandler? ManageTermsRequested;

    /// <summary>
    /// Fired when the user double-clicks a TM match in the assistant panel.
    /// Carries the source file and the Chinese source text to locate.
    /// </summary>
    public event EventHandler<NavigationRequest>? NavigationRequested;

    private StackPanel? _approvedTmHost;
    private StackPanel? _referenceTmHost;
    private StackPanel? _termHost;
    private StackPanel? _qaHost;

    private readonly List<IDisposable> _assistantHoverDisposables = new();

    public event EventHandler<string>? ReviewActionRequested;
    public event EventHandler? NextUnapprovedRequested;

    /// <summary>Fired when user requests adding selected text to a Scholar collection.</summary>
    public event EventHandler<ScholarPassage>? AddToScholarRequested;

    /// <summary>Fired when the user selects a different translation source.</summary>
    public event EventHandler<int>? TranslationSourceChanged;

    /// <summary>Fired when user clicks the star/unstar button for the current translation source.</summary>
    public event EventHandler? StarToggleRequested;

    /// <summary>Fired when user selects "Search corpus for selection" in the editor context menu.</summary>
    public event EventHandler<string>? SearchCorpusRequested;

    /// <summary>Fired when user selects "Create Termbase Entry" in the editor context menu.</summary>
    public event EventHandler<string>? CreateTermbaseEntryRequested;

    /// <summary>
    /// Delegate that resolves the lb n-value for a given block number.
    /// Wired by MainWindow from the current IndexedTranslationDocument.
    /// </summary>
    public Func<int, string?>? ResolveLbForBlock { get; set; }

    /// <summary>Returns the currently active translation user (null = community).</summary>
    public Func<string?>? GetTranslationUser { get; set; }

    private TextLicenseInfo? _currentLicense;
    private readonly ICitationService _citationService = new CitationService();

    /// <summary>Set the license metadata for the currently loaded file (wired by MainWindow).</summary>
    public void SetLicense(TextLicenseInfo? license) => _currentLicense = license;

    public TranslationTabView()
    {
        _vm = new TranslationTabViewModel();
        DataContext = _vm;

        // Forward VM events to code-behind events (MainWindow subscribes to these)
        _vm.ModeChanged += (_, mode) => ModeChanged?.Invoke(this, mode);
        _vm.SaveRequested += (_, e) => SaveRequested?.Invoke(this, e);
        _vm.RevertRequested += (_, e) => RevertRequested?.Invoke(this, e);
        _vm.StatusChanged += (_, msg) => Status?.Invoke(this, msg);
        _vm.BuildReferenceTmRequested += (_, e) => BuildReferenceTmRequested?.Invoke(this, e);
        _vm.ManageTermsRequested += (_, e) => ManageTermsRequested?.Invoke(this, e);
        _vm.NavigationRequested += (_, req) => NavigationRequested?.Invoke(this, req);
        _vm.ReviewActionRequested += (_, action) => ReviewActionRequested?.Invoke(this, action);
        _vm.NextUnapprovedRequested += (_, e) => NextUnapprovedRequested?.Invoke(this, e);

        AvaloniaXamlLoader.Load(this);
        FindControls();
        WireEvents();
        ApplyWrap();
        UpdateAssistantVisibility();
        UpdateModeInfo();
        ApplyHoverDictionarySetting();
        SetCurrentReviewState(null, null, null, null);

        DetachedFromVisualTree += (_, _) =>
        {
            _hoverDictionaryBehavior?.Dispose();
            _hoverDictionaryBehavior = null;
            ClearAssistantHoverBehaviors();
        };
    }

    private void FindControls()
    {
        _btnModeBody = this.FindControl<Button>("BtnModeBody");
        _btnModeNotes = this.FindControl<Button>("BtnModeNotes");

        _btnUndo = this.FindControl<Button>("BtnUndo");
        _btnRedo = this.FindControl<Button>("BtnRedo");

        _btnCopyChunkPrompt = this.FindControl<Button>("BtnCopyChunkPrompt");
        _btnPasteByNumber = this.FindControl<Button>("BtnPasteByNumber");
        _btnNextUntranslated = this.FindControl<Button>("BtnNextUntranslated");
        _btnFindChineseInEn = this.FindControl<Button>("BtnFindChineseInEn");
        _btnSave = this.FindControl<Button>("BtnSave");
        _btnFreshStart = this.FindControl<Button>("BtnFreshStart");
        _btnRevert = this.FindControl<Button>("BtnRevert");
        _btnBuildReferenceTm = this.FindControl<Button>("BtnBuildReferenceTm");
        _btnManageTerms = this.FindControl<Button>("BtnManageTerms");

        _btnApproveSegment = this.FindControl<Button>("BtnApproveSegment");
        _btnNeedsWorkSegment = this.FindControl<Button>("BtnNeedsWorkSegment");
        _btnRejectSegment = this.FindControl<Button>("BtnRejectSegment");
        _btnNextUnapproved = this.FindControl<Button>("BtnNextUnapproved");

        _cmbChunkSize = this.FindControl<ComboBox>("CmbChunkSize");
        _cmbTranslationSource = this.FindControl<ComboBox>("CmbTranslationSource");
        _btnStarTranslation = this.FindControl<Button>("BtnStarTranslation");
        _chkWrap = this.FindControl<CheckBox>("ChkWrap");
        _chkAssistantVisible = this.FindControl<CheckBox>("ChkAssistantVisible");

        _txtModeInfo = this.FindControl<TextBlock>("TxtModeInfo");
        _txtQuickInfo = this.FindControl<TextBlock>("TxtQuickInfo");
        _txtReviewState = this.FindControl<TextBlock>("TxtReviewState");
        _txtProgress = this.FindControl<TextBlock>("TxtProgress");

        _editor = this.FindControl<TextEditor>("EditorProjection");

        _assistantPane = this.FindControl<Border>("AssistantPane");
        _assistantSplitter = this.FindControl<GridSplitter>("AssistantSplitter");
        _editorAssistantGrid = this.FindControl<Grid>("EditorAssistantGrid");

        _approvedTmHost = this.FindControl<StackPanel>("ApprovedTmHost");
        _referenceTmHost = this.FindControl<StackPanel>("ReferenceTmHost");
        _termHost = this.FindControl<StackPanel>("TermHost");
        _qaHost = this.FindControl<StackPanel>("QaHost");

        _emptyState = this.FindControl<Border>("TranslationEmptyState");
        _dictOverlayCanvas = this.FindControl<Canvas>("DictOverlayCanvas");

        // Color-code review buttons
        if (_btnApproveSegment != null)
        {
            _btnApproveSegment.Background = new SolidColorBrush(Color.Parse("#1F3A1F"));
            _btnApproveSegment.Foreground = new SolidColorBrush(Color.Parse("#4CAF50"));
        }
        if (_btnNeedsWorkSegment != null)
        {
            _btnNeedsWorkSegment.Background = new SolidColorBrush(Color.Parse("#3A3418"));
            _btnNeedsWorkSegment.Foreground = new SolidColorBrush(Color.Parse("#FFC107"));
        }
        if (_btnRejectSegment != null)
        {
            _btnRejectSegment.Background = new SolidColorBrush(Color.Parse("#3A1F1F"));
            _btnRejectSegment.Foreground = new SolidColorBrush(Color.Parse("#E05555"));
        }

        if (_editor != null)
        {
            _editor.Background ??= Brushes.Transparent;
            _editor.IsReadOnly = false;
            _editor.WordWrap = _chkWrap?.IsChecked == true;
            _editor.ShowLineNumbers = true;

            _editor.TextChanged += (_, _) =>
            {
                UpdateQuickInfo();
                PublishCurrentSegment();
            };

            if (_editor.TextArea?.Caret != null)
                _editor.TextArea.Caret.PositionChanged += (_, _) => PublishCurrentSegmentDebounced();

            // Also refresh the assistant when the user changes the selection
            // (for selection-based TM lookup: highlight Chinese text → get
            // TM matches for just that phrase).
            if (_editor.TextArea != null)
                _editor.TextArea.SelectionChanged += (_, _) => PublishCurrentSegmentDebounced();

            _editor.ContextMenu = BuildScholarContextMenu();

            // Install AvaloniaEdit's built-in SearchPanel (gives Ctrl+F
            // find-in-text for free). The visible "Find" toolbar button
            // calls Open() on it so users who don't know the shortcut can
            // still discover the feature.
            if (_editor.TextArea != null)
            {
                var searchPanel = AvaloniaEdit.Search.SearchPanel.Install(_editor);
                var btnFind = this.FindControl<Button>("BtnFind");
                if (btnFind != null)
                    btnFind.Click += (_, _) => searchPanel.Open();
            }
        }
    }

    private ContextMenu BuildScholarContextMenu()
    {
        var menu = new ContextMenu();
        var addItem = new MenuItem { Header = "Add to Scholar Collection..." };
        addItem.Click += (_, _) => OnAddToScholarCollection();
        menu.Items.Add(addItem);

        var copyLinkItem = new MenuItem { Header = "Copy Link" };
        copyLinkItem.Click += async (_, _) =>
        {
            var relPath = _vm.CurrentOriginalPath;
            if (string.IsNullOrWhiteSpace(relPath) || _editor == null) return;

            string? highlight = _editor.SelectedText;
            if (string.IsNullOrWhiteSpace(highlight)) highlight = null;

            int? blockNumber = null;
            string? fromLb = null;
            string? toLb = null;
            var blocks = ParseProjectionBlocksWithOffsets(_editor.Text ?? "");
            if (blocks.Count > 0)
            {
                int ix = FindBlockIndexAtOrAfterCaret(blocks, _editor.CaretOffset);
                if (ix >= 0 && ix < blocks.Count)
                {
                    blockNumber = blocks[ix].BlockNumber;
                    fromLb = ResolveLbForBlock?.Invoke(blocks[ix].BlockNumber);
                }

                // If there's a selection spanning multiple blocks, get toLb from last block
                int selStart = _editor.SelectionStart;
                int selEnd = selStart + _editor.SelectionLength;
                if (selEnd > selStart)
                {
                    int ixEnd = FindBlockIndexAtOrAfterCaret(blocks, selEnd - 1);
                    if (ixEnd >= 0 && ixEnd < blocks.Count && ixEnd != ix)
                        toLb = ResolveLbForBlock?.Invoke(blocks[ixEnd].BlockNumber);
                }
            }

            // Fall back to highlight text if lb extraction fails
            if (fromLb == null && highlight != null && highlight.Length > 60)
                highlight = highlight.Substring(0, 60);

            var user = GetTranslationUser?.Invoke();
            if (!string.IsNullOrWhiteSpace(fromLb))
                highlight = null;
            var uri = ZenUriParser.BuildUri(relPath, fromLb: fromLb, toLb: toLb, highlightText: highlight, side: SearchSide.Translated, blockNumber: blockNumber, user: user);
            var top = TopLevel.GetTopLevel(this);
            if (top?.Clipboard != null)
                await top.Clipboard.SetTextAsync(uri);
            Status?.Invoke(this, "Link copied to clipboard.");
        };
        menu.Items.Add(copyLinkItem);

        var copyRedditLink = new MenuItem { Header = "Copy Reddit Link" };
        copyRedditLink.Click += async (_, _) =>
        {
            var relPath = _vm.CurrentOriginalPath;
            if (string.IsNullOrWhiteSpace(relPath) || _editor == null) return;

            string? highlight = _editor.SelectedText;
            if (string.IsNullOrWhiteSpace(highlight)) highlight = null;

            string? fromLb = null;
            string? toLb = null;
            var blocks = ParseProjectionBlocksWithOffsets(_editor.Text ?? "");
            if (blocks.Count > 0)
            {
                int ix = FindBlockIndexAtOrAfterCaret(blocks, _editor.CaretOffset);
                if (ix >= 0 && ix < blocks.Count)
                    fromLb = ResolveLbForBlock?.Invoke(blocks[ix].BlockNumber);

                int selStart = _editor.SelectionStart;
                int selEnd = selStart + _editor.SelectionLength;
                if (selEnd > selStart)
                {
                    int ixEnd = FindBlockIndexAtOrAfterCaret(blocks, selEnd - 1);
                    if (ixEnd >= 0 && ixEnd < blocks.Count && ixEnd != ix)
                        toLb = ResolveLbForBlock?.Invoke(blocks[ixEnd].BlockNumber);
                }
            }

            if (fromLb == null && highlight != null && highlight.Length > 60)
                highlight = highlight.Substring(0, 60);

            var userR = GetTranslationUser?.Invoke();
            if (!string.IsNullOrWhiteSpace(fromLb))
                highlight = null;
            var url = ZenUriParser.BuildShareableUrl(relPath, fromLb: fromLb, toLb: toLb, highlightText: highlight, side: SearchSide.Translated, user: userR);
            var top = TopLevel.GetTopLevel(this);
            if (top?.Clipboard != null)
                await top.Clipboard.SetTextAsync(url);
            Status?.Invoke(this, "Reddit link copied to clipboard.");
        };
        menu.Items.Add(copyRedditLink);

        // Citation flyout
        menu.Items.Add(new Separator());
        var citeFlyout = CitationMenuHelper.BuildCiteAsFlyout(
            _citationService,
            CitationMenuHelper.GetPreferredStyle(),
            buildMetadata: () =>
            {
                string? lbValue = null;
                var blocks = ParseProjectionBlocksWithOffsets(_editor?.Text ?? "");
                if (blocks.Count > 0 && _editor != null)
                {
                    int ix = FindBlockIndexAtOrAfterCaret(blocks, _editor.CaretOffset);
                    if (ix >= 0 && ix < blocks.Count)
                        lbValue = ResolveLbForBlock?.Invoke(blocks[ix].BlockNumber);
                }
                return _citationService.BuildMetadata(
                    _currentLicense, fromLb: lbValue,
                    quotedText: _editor?.SelectedText?.Trim(),
                    translatorName: GetTranslationUser?.Invoke());
            },
            copyToClipboard: async text =>
            {
                var top = TopLevel.GetTopLevel(this);
                if (top?.Clipboard != null)
                    await top.Clipboard.SetTextAsync(text);
            },
            onCopied: msg => Status?.Invoke(this, msg));
        menu.Items.Add(citeFlyout);

        // 5D-1: Search corpus for selection
        menu.Items.Add(new Separator());
        var searchCorpusItem = new MenuItem { Header = "Search corpus for selection" };
        searchCorpusItem.Click += (_, _) =>
        {
            var selected = _editor?.SelectedText?.Trim();
            if (!string.IsNullOrEmpty(selected))
                SearchCorpusRequested?.Invoke(this, selected);
        };
        menu.Items.Add(searchCorpusItem);

        // Create Termbase Entry — opens the termbase editor with a new entry pre-filled.
        var createTermItem = new MenuItem { Header = "Create Termbase Entry" };
        createTermItem.Click += (_, _) =>
        {
            var selected = _editor?.SelectedText?.Trim();
            if (!string.IsNullOrEmpty(selected))
                CreateTermbaseEntryRequested?.Invoke(this, selected);
        };
        menu.Items.Add(createTermItem);

        return menu;
    }

    private void OnAddToScholarCollection()
    {
        if (_editor == null) return;

        string selectedText = _editor.SelectedText ?? "";
        if (string.IsNullOrWhiteSpace(selectedText))
        {
            Status?.Invoke(this, "Select some text first, then right-click to add to Scholar.");
            return;
        }

        var blocks = ParseProjectionBlocksWithOffsets(_editor.Text ?? "");
        if (blocks.Count == 0) return;

        // Find all blocks overlapping the selection
        int selStart = _editor.SelectionStart;
        int selEnd = selStart + _editor.SelectionLength;

        var overlapping = new List<ProjectionBlockInfo>();
        for (int i = 0; i < blocks.Count; i++)
        {
            var b = blocks[i];
            if (b.BlockStartOffset < selEnd && b.BlockEndOffsetExclusive > selStart)
                overlapping.Add(b);
        }

        string zh;
        string en;
        int? startBlock = null;
        int? endBlock = null;

        if (overlapping.Count > 0)
        {
            // Multi-block (or single block): concatenate all overlapping blocks
            zh = string.Join("\n", overlapping.Select(b => b.Zh ?? "").Where(s => !string.IsNullOrWhiteSpace(s)));
            en = string.Join("\n", overlapping.Select(b => b.En ?? "").Where(s => !string.IsNullOrWhiteSpace(s)));
            startBlock = overlapping[0].BlockNumber;
            endBlock = overlapping[overlapping.Count - 1].BlockNumber;
        }
        else
        {
            // Fallback: selection is outside any block (e.g. header text)
            int caret = _editor.CaretOffset;
            int ix = FindBlockIndexAtOrAfterCaret(blocks, caret);
            zh = ix >= 0 && ix < blocks.Count ? blocks[ix].Zh ?? "" : "";
            en = ix >= 0 && ix < blocks.Count ? blocks[ix].En ?? "" : "";

            if (ContainsChineseChar(selectedText))
                zh = selectedText;
            else
                en = selectedText;
        }

        // Resolve lb values from block numbers via delegate
        string? fromLb = startBlock.HasValue ? ResolveLbForBlock?.Invoke(startBlock.Value) : null;
        string? toLb = endBlock.HasValue ? ResolveLbForBlock?.Invoke(endBlock.Value) : null;

        var passage = new ScholarPassage
        {
            ZhText = zh,
            EnText = en,
            SourceRelPath = _vm.CurrentOriginalPath ?? "",
            StartBlockNumber = startBlock,
            EndBlockNumber = endBlock,
            FromLb = fromLb,
            ToLb = toLb,
            PreferredSide = SearchSide.Translated,
            TranslationUser = GetTranslationUser?.Invoke()
        };

        AddToScholarRequested?.Invoke(this, passage);
    }

    private void WireEvents()
    {
        if (_btnModeBody != null) _btnModeBody.Click += (_, _) => SwitchMode(TranslationEditMode.Body);
        if (_btnModeNotes != null) _btnModeNotes.Click += (_, _) => SwitchMode(TranslationEditMode.Notes);

        if (_btnUndo != null) _btnUndo.Click += (_, _) => DoUndo();
        if (_btnRedo != null) _btnRedo.Click += (_, _) => DoRedo();

        if (_btnCopyChunkPrompt != null) _btnCopyChunkPrompt.Click += async (_, _) => await CopyChunkWithPromptAsync();
        if (_btnPasteByNumber != null) _btnPasteByNumber.Click += async (_, _) => await PasteByMatchingBlockNumberAsync();
        if (_btnNextUntranslated != null) _btnNextUntranslated.Click += (_, _) => JumpToNextUntranslated();
        if (_btnFindChineseInEn != null) _btnFindChineseInEn.Click += (_, _) => JumpToChineseInEnglishLine();

        if (_btnSave != null)
            _btnSave.Click += (_, _) => SaveRequested?.Invoke(this, EventArgs.Empty);

        if (_btnFreshStart != null)
            _btnFreshStart.Click += (_, _) => FreshStartRequested?.Invoke(this, EventArgs.Empty);

        if (_btnRevert != null)
            _btnRevert.Click += (_, _) => RevertRequested?.Invoke(this, EventArgs.Empty);

        var btnHistory = this.FindControl<Button>("BtnHistory");
        if (btnHistory != null)
            btnHistory.Click += (_, _) => HistoryRequested?.Invoke(this, EventArgs.Empty);

        if (_btnBuildReferenceTm != null)
            _btnBuildReferenceTm.Click += (_, _) => BuildReferenceTmRequested?.Invoke(this, EventArgs.Empty);

        if (_btnManageTerms != null)
            _btnManageTerms.Click += (_, _) => ManageTermsRequested?.Invoke(this, EventArgs.Empty);

        if (_btnApproveSegment != null)
            _btnApproveSegment.Click += (_, _) => ReviewActionRequested?.Invoke(this, TranslationReviewStatuses.Approved);

        if (_btnNeedsWorkSegment != null)
            _btnNeedsWorkSegment.Click += (_, _) => ReviewActionRequested?.Invoke(this, TranslationReviewStatuses.NeedsWork);

        if (_btnRejectSegment != null)
            _btnRejectSegment.Click += (_, _) => ReviewActionRequested?.Invoke(this, TranslationReviewStatuses.Rejected);

        if (_btnNextUnapproved != null)
            _btnNextUnapproved.Click += (_, _) => NextUnapprovedRequested?.Invoke(this, EventArgs.Empty);

        if (_chkWrap != null)
        {
            _chkWrap.IsCheckedChanged += (_, _) => ApplyWrap();
        }

        if (_chkAssistantVisible != null)
        {
            _chkAssistantVisible.IsCheckedChanged += (_, _) => UpdateAssistantVisibility();
        }

        if (_cmbTranslationSource != null)
        {
            _cmbTranslationSource.SelectionChanged += (_, _) =>
            {
                if (_cmbTranslationSource.SelectedIndex >= 0)
                    TranslationSourceChanged?.Invoke(this, _cmbTranslationSource.SelectedIndex);
            };
        }

        if (_btnStarTranslation != null)
        {
            _btnStarTranslation.Click += (_, _) => StarToggleRequested?.Invoke(this, EventArgs.Empty);
        }

        AddHandler(KeyDownEvent, OnKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    public void SetAssistantTitleResolver(Func<string, string>? resolver)
    {
        _vm.SetAssistantTitleResolver(resolver);
        if (_vm.LastAssistantSnapshot != null)
            RenderAssistantSnapshot(_vm.LastAssistantSnapshot);
    }

    public void SetCurrentReviewState(string? status, string? reviewer, DateTime? reviewedUtc, SegmentReviewAggregation? agg = null)
    {
        _vm.SetCurrentReviewState(status, reviewer, reviewedUtc, agg);
        if (_txtReviewState != null)
            _txtReviewState.Text = _vm.ReviewStateText;
    }

    private void UpdateAssistantVisibility()
    {
        bool visible = _chkAssistantVisible?.IsChecked == true;

        if (_assistantPane != null)
            _assistantPane.IsVisible = visible;

        if (_assistantSplitter != null)
            _assistantSplitter.IsVisible = visible;

        if (_editorAssistantGrid != null && _editorAssistantGrid.ColumnDefinitions.Count >= 3)
        {
            _editorAssistantGrid.ColumnDefinitions[1].Width = visible ? new GridLength(8) : new GridLength(0);
            _editorAssistantGrid.ColumnDefinitions[2].Width = visible ? new GridLength(360) : new GridLength(0);
        }

        if (!visible)
        {
            ClearAssistantHoverBehaviors();
            UpdateTermbaseHighlights(null, null);
            UpdateTmSharedHighlights(null, null, null);
            return;
        }

        if (_vm.LastAssistantSnapshot != null)
        {
            RenderAssistantSnapshot(_vm.LastAssistantSnapshot);
            UpdateTermbaseHighlights(_vm.LastAssistantSnapshot.Terms, _vm.LastAssistantSnapshot.Segment?.ZhText);
            UpdateTmSharedHighlights(_vm.LastAssistantSnapshot.ApprovedMatches, _vm.LastAssistantSnapshot.ReferenceMatches, _vm.LastAssistantSnapshot.Segment?.ZhText);
        }
    }

    public string GetCurrentProjectionText()
        => _editor?.Text ?? _vm.CurrentProjection;

    public void SetCurrentFilePaths(string originalPath, string translatedPath)
    {
        _vm.SetCurrentFilePaths(originalPath, translatedPath);
        UpdateModeInfo();
    }

    public void SetHoverDictionaryEnabled(bool enabled)
    {
        _vm.HoverDictionaryEnabled = enabled;
        ApplyHoverDictionarySetting();
        if (_vm.LastAssistantSnapshot != null)
            RenderAssistantSnapshot(_vm.LastAssistantSnapshot);
    }

    public void SetXml(string originalXml, string translatedXml)
    {
        _vm.CurrentProjection = translatedXml ?? "";
        if (_editor != null) _editor.Text = _vm.CurrentProjection;
        if (_emptyState != null) _emptyState.IsVisible = false;
        UpdateModeInfo();
        UpdateQuickInfo();
    }


    public string GetTranslatedXml() => GetCurrentProjectionText();
    public string GetTranslatedText() => GetCurrentProjectionText();
    public string GetTranslatedMarkdown() => GetCurrentProjectionText();

    public void Clear()
    {
        _vm.Clear();

        if (_editor != null)
            _editor.Text = "";

        if (_emptyState != null) _emptyState.IsVisible = true;

        SetAssistantSnapshot(null);
        UpdateModeInfo();
        UpdateModeButtons();
        UpdateQuickInfo();
    }

    private void SwitchMode(TranslationEditMode mode)
    {
        if (_vm.CurrentMode == mode) return;

        _vm.SwitchMode(mode);
        // VM fires ModeChanged which is forwarded to our ModeChanged event
        UpdateModeInfo();
        UpdateModeButtons();
    }

    private void UpdateModeButtons()
    {
        if (_btnModeBody != null) _btnModeBody.IsEnabled = _vm.IsModeBodyEnabled;
        if (_btnModeNotes != null) _btnModeNotes.IsEnabled = _vm.IsModeNotesEnabled;
    }

    private void UpdateModeInfo()
    {
        _vm.UpdateModeInfo();
        if (_txtModeInfo != null)
            _txtModeInfo.Text = _vm.ModeInfoText;
    }

    private void UpdateQuickInfo()
    {
        _vm.UpdateQuickInfo(_editor?.Text ?? "");
        if (_txtQuickInfo != null)
            _txtQuickInfo.Text = _vm.QuickInfoText;
    }

    private void ApplyWrap()
    {
        if (_editor != null)
            _editor.WordWrap = _chkWrap?.IsChecked ?? true; // Default to wrap if checkbox not ready
    }

    private void ApplyHoverDictionarySetting()
    {
        if (_editor == null)
            return;

        if (_vm.HoverDictionaryEnabled)
            AttachHoverDictionary();
        else
            DetachHoverDictionary();
    }

    private void AttachHoverDictionary()
    {
        try
        {
            if (_editor == null)
                return;

            _hoverDictionaryBehavior?.Dispose();
            _hoverDictionaryBehavior = null;

            _hoverDictionaryBehavior = new HoverDictionaryBehaviorEdit(_editor, _cedict, _grammar, _dictOverlayCanvas);
        }
        catch (Exception ex)
        {
            Status?.Invoke(this, "Hover dictionary failed: " + ex.Message);
        }
    }

    private void DetachHoverDictionary()
    {
        try
        {
            _hoverDictionaryBehavior?.Dispose();
            _hoverDictionaryBehavior = null;
        }
        catch
        {
            _hoverDictionaryBehavior = null;
        }
    }

    private void DoUndo()
    {
        try
        {
            if (_editor?.CanUndo == true)
            {
                _editor.Undo();
                Status?.Invoke(this, "Undo");
            }
        }
        catch (Exception ex)
        {
            Status?.Invoke(this, "Undo failed: " + ex.Message);
        }
    }

    private void DoRedo()
    {
        try
        {
            if (_editor?.CanRedo == true)
            {
                _editor.Redo();
                Status?.Invoke(this, "Redo");
            }
        }
        catch (Exception ex)
        {
            Status?.Invoke(this, "Redo failed: " + ex.Message);
        }
    }

    private async Task CopyChunkWithPromptAsync()
    {
        if (_editor == null)
        {
            Status?.Invoke(this, "Editor not available.");
            return;
        }

        var text = _editor.Text ?? "";
        if (string.IsNullOrWhiteSpace(text))
        {
            Status?.Invoke(this, "Editor is empty.");
            return;
        }

        List<ProjectionBlockInfo> blocks;
        try
        {
            blocks = ParseProjectionBlocksWithOffsets(text);
        }
        catch (Exception ex)
        {
            Status?.Invoke(this, "Projection parse failed: " + ex.Message);
            return;
        }

        if (blocks.Count == 0)
        {
            Status?.Invoke(this, "No blocks found.");
            return;
        }

        int caret = _editor.CaretOffset;
        int maxCount = GetSelectedChunkSize();

        int startIx = FindBlockIndexAtOrAfterCaret(blocks, caret);
        if (startIx < 0)
        {
            Status?.Invoke(this, "No block found near caret.");
            return;
        }

        while (startIx < blocks.Count && !ShouldIncludeForCopy(blocks[startIx], requireUntranslated: true))
            startIx++;

        if (startIx >= blocks.Count)
        {
            Status?.Invoke(this, "No suitable untranslated block found after caret.");
            return;
        }

        int copied = 0;
        int firstIncludedIx = -1;
        int lastIncludedIx = -1;

        var selectedBlockTexts = new List<string>(maxCount);

        for (int i = startIx; i < blocks.Count && copied < maxCount; i++)
        {
            var b = blocks[i];

            if (!ShouldIncludeForCopy(b, requireUntranslated: true))
                continue;

            if (firstIncludedIx < 0) firstIncludedIx = i;
            lastIncludedIx = i;

            int bs = b.BlockStartOffset;
            int be = b.BlockEndOffsetExclusive;

            if (bs < 0 || be < bs || be > text.Length)
                continue;

            var blockText = text.Substring(bs, be - bs).TrimEnd('\r', '\n');
            if (blockText.Length == 0)
                continue;

            selectedBlockTexts.Add(blockText);
            copied++;
        }

        if (copied == 0 || firstIncludedIx < 0 || lastIncludedIx < 0)
        {
            Status?.Invoke(this, "Nothing to copy.");
            return;
        }

        var firstBlock = blocks[firstIncludedIx];
        var lastBlock = blocks[lastIncludedIx];

        var rawChunk = string.Join(Environment.NewLine + Environment.NewLine, selectedBlockTexts).TrimEnd('\r', '\n');
        if (string.IsNullOrWhiteSpace(rawChunk))
        {
            Status?.Invoke(this, "Nothing to copy after filtering.");
            return;
        }

        var payload = BuildPrompt(rawChunk);

        var cb = GetClipboard();
        if (cb == null)
        {
            Status?.Invoke(this, "Clipboard unavailable.");
            return;
        }

        await cb.SetTextAsync(payload);

        if (_editor.Document != null)
        {
            int selStart = Math.Clamp(firstBlock.BlockStartOffset, 0, _editor.Document.TextLength);
            int selEnd = Math.Clamp(lastBlock.BlockEndOffsetExclusive, selStart, _editor.Document.TextLength);

            _editor.TextArea.Selection = Selection.Create(_editor.TextArea, selStart, selEnd);
            _editor.CaretOffset = selEnd;

            try
            {
                var line = _editor.Document.GetLineByOffset(selStart).LineNumber;
                _editor.ScrollToLine(line);
            }
            catch
            {
            }

            _editor.Focus();
        }

        Status?.Invoke(this, $"Copied {copied} block(s): <{firstBlock.BlockNumber}>-<{lastBlock.BlockNumber}> + prompt.");
    }

    private async Task PasteByMatchingBlockNumberAsync()
    {
        if (_editor == null)
        {
            Status?.Invoke(this, "Editor not available.");
            return;
        }

        var cb = GetClipboard();
        if (cb == null)
        {
            Status?.Invoke(this, "Clipboard unavailable.");
            return;
        }

        var clip = (await cb.TryGetTextAsync()) ?? "";
        if (string.IsNullOrWhiteSpace(clip))
        {
            Status?.Invoke(this, "Clipboard empty.");
            return;
        }

        var pastedText = ExtractCodeBlockOrRaw(clip);

        List<ProjectionBlockInfo> pastedBlocks;
        try
        {
            pastedBlocks = ParseProjectionBlocksWithOffsets(pastedText);
        }
        catch (Exception ex)
        {
            Status?.Invoke(this, "Clipboard parse failed: " + ex.Message);
            return;
        }

        if (pastedBlocks.Count == 0)
        {
            Status?.Invoke(this, "No valid blocks found in clipboard.");
            return;
        }

        var editorText = _editor.Text ?? "";
        List<ProjectionBlockInfo> editorBlocks;
        try
        {
            editorBlocks = ParseProjectionBlocksWithOffsets(editorText);
        }
        catch (Exception ex)
        {
            Status?.Invoke(this, "Current editor parse failed: " + ex.Message);
            return;
        }

        var editorByNum = new Dictionary<int, ProjectionBlockInfo>();
        foreach (var b in editorBlocks)
        {
            if (editorByNum.ContainsKey(b.BlockNumber))
            {
                Status?.Invoke(this, $"Editor has duplicate block number <{b.BlockNumber}>.");
                return;
            }
            editorByNum[b.BlockNumber] = b;
        }

        var seenPasteNums = new HashSet<int>();
        foreach (var pb in pastedBlocks)
        {
            if (!seenPasteNums.Add(pb.BlockNumber))
            {
                Status?.Invoke(this, $"Clipboard contains duplicate block <{pb.BlockNumber}>.");
                return;
            }

            if (!editorByNum.TryGetValue(pb.BlockNumber, out var target))
            {
                Status?.Invoke(this, $"Reject: block <{pb.BlockNumber}> not found in current editor.");
                return;
            }

            if (!string.Equals(pb.Zh, target.Zh, StringComparison.Ordinal))
            {
                Status?.Invoke(this, $"Reject: ZH mismatch in block <{pb.BlockNumber}>.");
                return;
            }

            try
            {
                ValidateEnglish(pb.En, pb.BlockNumber);
            }
            catch (Exception ex)
            {
                Status?.Invoke(this, ex.Message);
                return;
            }
        }

        var orderedTargets = pastedBlocks
            .Select(pb => (Paste: pb, Target: editorByNum[pb.BlockNumber]))
            .OrderByDescending(x => x.Target.EnValueStartOffset)
            .ToList();

        var sb = new StringBuilder(editorText);

        foreach (var x in orderedTargets)
        {
            int start = x.Target.EnValueStartOffset;
            int len = x.Target.EnValueLength;

            if (start < 0 || len < 0 || start + len > sb.Length)
            {
                Status?.Invoke(this, $"Internal offset error while pasting block <{x.Paste.BlockNumber}>.");
                return;
            }

            sb.Remove(start, len);
            sb.Insert(start, x.Paste.En);
        }

        _editor.Text = sb.ToString();

        int minNum = pastedBlocks.Min(b => b.BlockNumber);
        int maxNum = pastedBlocks.Max(b => b.BlockNumber);

        var reparsed = ParseProjectionBlocksWithOffsets(_editor.Text ?? "");
        var firstReparsed = reparsed.FirstOrDefault(b => b.BlockNumber == pastedBlocks[0].BlockNumber)
            ?? reparsed.FirstOrDefault(b => b.BlockNumber == minNum);

        if (firstReparsed != null)
            SelectAndRevealBlock(firstReparsed);

        Status?.Invoke(this, $"Pasted {pastedBlocks.Count} block(s): <{minNum}>-<{maxNum}> (ZH validated).");
    }

    private void JumpToNextUntranslated()
    {
        if (_editor == null)
        {
            Status?.Invoke(this, "Editor not available.");
            return;
        }

        var text = _editor.Text ?? "";
        if (string.IsNullOrWhiteSpace(text))
        {
            Status?.Invoke(this, "Editor is empty.");
            return;
        }

        List<ProjectionBlockInfo> blocks;
        try
        {
            blocks = ParseProjectionBlocksWithOffsets(text);
        }
        catch (Exception ex)
        {
            Status?.Invoke(this, "Projection parse failed: " + ex.Message);
            return;
        }

        if (blocks.Count == 0)
        {
            Status?.Invoke(this, "No blocks found.");
            return;
        }

        int caret = _editor.CaretOffset;
        int curIx = FindBlockIndexAtOrAfterCaret(blocks, caret);
        if (curIx < 0) curIx = 0;

        int nextIx = -1;
        for (int i = Math.Min(curIx + 1, blocks.Count); i < blocks.Count; i++)
        {
            if (ShouldJumpToUntranslated(blocks[i]))
            {
                nextIx = i;
                break;
            }
        }

        bool wrapped = false;
        if (nextIx < 0)
        {
            for (int i = 0; i <= Math.Min(curIx, blocks.Count - 1); i++)
            {
                if (ShouldJumpToUntranslated(blocks[i]))
                {
                    nextIx = i;
                    wrapped = true;
                    break;
                }
            }
        }

        if (nextIx < 0)
        {
            Status?.Invoke(this, "No untranslated Chinese blocks.");
            return;
        }

        SelectAndRevealBlock(blocks[nextIx]);
        Status?.Invoke(this, wrapped
            ? $"Jumped to untranslated block <{blocks[nextIx].BlockNumber}> (wrapped)."
            : $"Jumped to untranslated block <{blocks[nextIx].BlockNumber}>.");
    }

    public void JumpToNextBlock()
    {
        if (_editor == null) return;
        var blocks = ParseProjectionBlocksWithOffsets(_editor.Text ?? "");
        if (blocks.Count == 0) return;
        int curIx = FindBlockIndexAtOrAfterCaret(blocks, _editor.CaretOffset);
        if (curIx < 0) curIx = 0;
        int nextIx = (curIx + 1) % blocks.Count;
        SelectAndRevealBlock(blocks[nextIx]);
    }

    public void JumpToPreviousBlock()
    {
        if (_editor == null) return;
        var blocks = ParseProjectionBlocksWithOffsets(_editor.Text ?? "");
        if (blocks.Count == 0) return;
        int curIx = FindBlockIndexAtOrAfterCaret(blocks, _editor.CaretOffset);
        if (curIx < 0) curIx = 0;
        int prevIx = (curIx - 1 + blocks.Count) % blocks.Count;
        SelectAndRevealBlock(blocks[prevIx]);
    }

    public void JumpToBlockNumber(int blockNumber)
    {
        if (_editor == null) return;
        var blocks = ParseProjectionBlocksWithOffsets(_editor.Text ?? "");
        var target = blocks.FirstOrDefault(b => b.BlockNumber == blockNumber);
        if (target != null)
            SelectAndRevealBlock(target);
    }

    public void JumpToNextUnapproved(IReadOnlySet<int> approvedBlockNumbers)
    {
        if (_editor == null) return;
        var blocks = ParseProjectionBlocksWithOffsets(_editor.Text ?? "");
        if (blocks.Count == 0) return;

        int curIx = FindBlockIndexAtOrAfterCaret(blocks, _editor.CaretOffset);
        if (curIx < 0) curIx = 0;

        for (int i = curIx + 1; i < blocks.Count; i++)
        {
            if (!approvedBlockNumbers.Contains(blocks[i].BlockNumber))
            {
                SelectAndRevealBlock(blocks[i]);
                return;
            }
        }

        for (int i = 0; i <= curIx && i < blocks.Count; i++)
        {
            if (!approvedBlockNumbers.Contains(blocks[i].BlockNumber))
            {
                SelectAndRevealBlock(blocks[i]);
                Status?.Invoke(this, $"Jumped to block <{blocks[i].BlockNumber}> (wrapped).");
                return;
            }
        }

        Status?.Invoke(this, "All segments in this file are approved.");
    }

    public IReadOnlyList<int> GetAllBlockNumbers()
    {
        var blocks = ParseProjectionBlocksWithOffsets(_editor?.Text ?? "");
        return blocks.Select(b => b.BlockNumber).ToList();
    }

    public void FillEnForCurrentBlock(string enText, int expectedBlockNumber = -1)
    {
        if (_editor == null) return;
        var blocks = ParseProjectionBlocksWithOffsets(_editor.Text ?? "");
        if (blocks.Count == 0) return;

        int curIx = FindBlockIndexAtOrAfterCaret(blocks, _editor.CaretOffset);
        if (curIx < 0 || curIx >= blocks.Count) return;

        var block = blocks[curIx];
        if (expectedBlockNumber >= 0 && block.BlockNumber != expectedBlockNumber) return;
        if (!string.IsNullOrWhiteSpace(block.En)) return;
        if (block.EnValueStartOffset < 0) return;

        _editor.Document.Replace(block.EnValueStartOffset, Math.Max(0, block.EnValueLength), enText);
        Status?.Invoke(this, "Auto-filled from 100% TM match.");
    }

    public void SetProgressStats(int approved, int needsWork, int total)
    {
        _vm.SetProgressStats(approved, needsWork, total);
        if (_txtProgress != null)
            _txtProgress.Text = _vm.ProgressText;
    }

    public bool IsEditorFocused()
        => _editor?.IsFocused == true || _editor?.TextArea?.IsFocused == true;

    /// <summary>
    /// Populates the translation source ComboBox with available options.
    /// </summary>
    public void SetTranslationSourceOptions(List<string> options)
    {
        if (_cmbTranslationSource == null) return;
        _cmbTranslationSource.ItemsSource = options;
        if (_cmbTranslationSource.SelectedIndex < 0)
            _cmbTranslationSource.SelectedIndex = 0;
    }

    /// <summary>
    /// Programmatically sets the selected translation source index.
    /// </summary>
    public void SetTranslationSourceIndex(int index)
    {
        if (_cmbTranslationSource != null && index >= 0)
            _cmbTranslationSource.SelectedIndex = index;
    }

    /// <summary>
    /// Updates the star button to reflect the current starred state.
    /// </summary>
    public void UpdateStarButton(bool? state)
    {
        if (_btnStarTranslation == null) return;
        if (state == null)
        {
            _btnStarTranslation.IsVisible = false;
            return;
        }
        _btnStarTranslation.IsVisible = true;
        _btnStarTranslation.Content = state == true ? "\u2605" : "\u2606";
        Avalonia.Controls.ToolTip.SetTip(_btnStarTranslation, state == true ? "Unstar this translation" : "Star this translation");
    }

    /// <summary>
    /// Sets the projection editor to read-only or editable.
    /// </summary>
    public void SetEditorReadOnly(bool readOnly)
    {
        if (_editor != null)
            _editor.IsReadOnly = readOnly;
    }

    // -------------------------
    // Termbase highlighting (projection editor)
    // -------------------------

    private TermbaseHighlightTransformer? _projectionTermHighlighter;

    public void UpdateTermbaseHighlights(IReadOnlyList<TermHit>? hits, string? zhText)
    {
        if (_editor == null) return;

        if (_chkAssistantVisible?.IsChecked != true)
        {
            _projectionTermHighlighter?.SetRanges(Array.Empty<(int Start, int Length)>());
            _editor.TextArea?.TextView?.Redraw();
            return;
        }

        if (_projectionTermHighlighter == null)
        {
            _projectionTermHighlighter = new TermbaseHighlightTransformer();
            _editor.TextArea.TextView.LineTransformers.Add(_projectionTermHighlighter);
        }

        var ranges = new List<(int Start, int Length)>();

        if (hits != null && !string.IsNullOrWhiteSpace(zhText))
        {
            string docText = _editor.Document?.Text ?? "";
            int? preferredOccurrenceHint =
                _vm.LastAssistantSnapshot != null && _vm.LastAssistantSnapshot.Segment.BlockNumber > 0
                    ? _vm.LastAssistantSnapshot.Segment.BlockNumber - 1
                    : null;
            var signalTerms = hits
                .Select(h => h.SourceTerm)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (TryFindSegmentRange(
                docText,
                zhText,
                signalTerms,
                tmSourceSignal: null,
                preferredOffset: _editor.TextArea?.Caret?.Offset,
                preferredOccurrenceHint: preferredOccurrenceHint,
                anchorTextSignal: _vm.LastAssistantSnapshot?.Segment?.ZhContextText,
                out int zhStart,
                out int zhLength))
            {
                foreach (var hit in hits)
                {
                    if (string.IsNullOrWhiteSpace(hit.SourceTerm)) continue;
                    AddTermOccurrencesInSegment(ranges, docText, zhStart, zhLength, hit.SourceTerm);
                }
            }
        }

        _projectionTermHighlighter.SetRanges(ranges);
        _editor.TextArea?.TextView?.Redraw();
    }

    private TmSharedHighlightTransformer? _tmSharedHighlighter;


    public void UpdateTmSharedHighlights(
        IReadOnlyList<TranslationTmMatch>? approvedMatches,
        IReadOnlyList<TranslationTmMatch>? referenceMatches,
        string? zhText)
    {
        if (_editor == null) return;

        if (_tmSharedHighlighter == null)
        {
            _tmSharedHighlighter = new TmSharedHighlightTransformer();
            _editor.TextArea.TextView.LineTransformers.Add(_tmSharedHighlighter);
        }

        if (_chkAssistantVisible?.IsChecked != true)
        {
            _tmSharedHighlighter?.SetRanges(Array.Empty<(int Start, int Length)>());
            _editor.TextArea?.TextView?.Redraw();
            return;
        }

        var ranges = new List<(int Start, int Length)>();

        if (!string.IsNullOrWhiteSpace(zhText))
        {
            var best = (approvedMatches ?? Enumerable.Empty<TranslationTmMatch>())
                .Concat(referenceMatches ?? Enumerable.Empty<TranslationTmMatch>())
                .OrderByDescending(m => m.Score)
                .FirstOrDefault();

            if (best != null && !string.IsNullOrWhiteSpace(best.SourceText))
            {
                string docText = _editor.Document?.Text ?? "";
                int? preferredOccurrenceHint =
                    _vm.LastAssistantSnapshot != null && _vm.LastAssistantSnapshot.Segment.BlockNumber > 0
                        ? _vm.LastAssistantSnapshot.Segment.BlockNumber - 1
                        : null;
                if (TryFindSegmentRange(
                    docText,
                    zhText,
                    signalTerms: null,
                    tmSourceSignal: best.SourceText,
                    preferredOffset: _editor.TextArea?.Caret?.Offset,
                    preferredOccurrenceHint: preferredOccurrenceHint,
                    anchorTextSignal: _vm.LastAssistantSnapshot?.Segment?.ZhContextText,
                    out int zhStart,
                    out int zhLength))
                {
                    // Anchor by the rendered ZH segment in the editor (not raw input ZH),
                    // then map shared ranges inside that anchored segment.
                    string anchoredZh = docText.Substring(zhStart, zhLength);
                    var sharedInZh = CjkMatchNormalizer.FindSharedRawRanges(
                        anchoredZh,
                        best.SourceText,
                        minPhraseLen: 2);
                    foreach (var r in sharedInZh)
                        ranges.Add((zhStart + r.Start, r.Length));
                }
            }
        }

        _tmSharedHighlighter.SetRanges(ranges);
        _editor.TextArea?.TextView?.Redraw();
    }

    private static bool TryFindSegmentRange(
        string docText,
        string segmentText,
        IReadOnlyList<string>? signalTerms,
        string? tmSourceSignal,
        int? preferredOffset,
        int? preferredOccurrenceHint,
        string? anchorTextSignal,
        out int start,
        out int length)
    {
        start = -1;
        length = 0;
        if (string.IsNullOrEmpty(docText) || string.IsNullOrEmpty(segmentText))
            return false;

        var candidates = new List<(int start, int length)>();
        var seen = new HashSet<(int start, int length)>();

        int from = 0;
        while (from < docText.Length)
        {
            int idx = docText.IndexOf(segmentText, from, StringComparison.Ordinal);
            if (idx < 0) break;
            var c = (idx, segmentText.Length);
            if (seen.Add(c)) candidates.Add(c);
            from = idx + 1;
        }

        bool cjkSeg = CjkMatchNormalizer.ContainsCjk(segmentText);
        var nDoc = cjkSeg ? CjkMatchNormalizer.NormalizeWithMap(docText) : null;
        string nSeg = cjkSeg ? CjkMatchNormalizer.Normalize(segmentText) : "";
        if (cjkSeg && !string.IsNullOrEmpty(nSeg) && !string.IsNullOrEmpty(nDoc!.Normalized))
        {
            int nFrom = 0;
            while (nFrom < nDoc.Normalized.Length)
            {
                int nIdx = nDoc.Normalized.IndexOf(nSeg, nFrom, StringComparison.Ordinal);
                if (nIdx < 0) break;

                int rawStart = CjkMatchNormalizer.RawIndexFromNormalizedPos(nDoc, nIdx);
                int rawEnd = CjkMatchNormalizer.RawIndexFromNormalizedPos(nDoc, nIdx + nSeg.Length);
                if (rawEnd > rawStart)
                {
                    var c = (rawStart, rawEnd - rawStart);
                    if (seen.Add(c)) candidates.Add(c);
                }

                nFrom = nIdx + 1;
            }
        }

        if (candidates.Count == 0)
            return false;

        int AnchorSharedScore(string segRaw, string? signal)
        {
            if (string.IsNullOrWhiteSpace(signal))
                return 0;

            if (CjkMatchNormalizer.ContainsCjk(segRaw) || CjkMatchNormalizer.ContainsCjk(signal))
            {
                int shared = 0;
                var ranges = CjkMatchNormalizer.FindSharedRawRanges(segRaw, signal, minPhraseLen: 2);
                for (int i = 0; i < ranges.Count; i++)
                    shared += Math.Max(1, ranges[i].Length);
                return shared;
            }

            return segRaw.Contains(signal, StringComparison.Ordinal) ? Math.Max(2, signal.Length) : 0;
        }

        (int score, int tmSharedTotal, int termSignalHits, int anchorSharedTotal, int proximity) Score((int start, int length) c)
        {
            int docLen = docText.Length;
            int segStart = Math.Clamp(c.start, 0, Math.Max(0, docLen - 1));
            int segEnd = Math.Clamp(segStart + c.length, 0, docLen);
            if (segEnd <= segStart) return (int.MinValue / 4, 0, 0, 0, int.MaxValue);

            string segRaw = docText.Substring(segStart, segEnd - segStart);
            int score = 0;
            int tmSharedTotal = 0;
            int termSignalHits = 0;

            if (signalTerms != null)
            {
                foreach (var term in signalTerms)
                {
                    if (string.IsNullOrWhiteSpace(term)) continue;
                    if (!CjkMatchNormalizer.ContainsCjk(term))
                    {
                        if (segRaw.Contains(term, StringComparison.Ordinal))
                        {
                            score += 4;
                            termSignalHits++;
                        }
                        continue;
                    }

                    string nTerm = CjkMatchNormalizer.Normalize(term);
                    if (string.IsNullOrEmpty(nTerm)) continue;
                    string nSegRaw = CjkMatchNormalizer.Normalize(segRaw);
                    if (nSegRaw.Contains(nTerm, StringComparison.Ordinal))
                    {
                        score += 6;
                        termSignalHits++;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(tmSourceSignal))
            {
                var shared = CjkMatchNormalizer.FindSharedRawRanges(segRaw, tmSourceSignal, minPhraseLen: 2);
                foreach (var r in shared)
                {
                    int s = Math.Max(1, r.Length);
                    score += s;
                    tmSharedTotal += s;
                }
            }

            int anchorSharedTotal = AnchorSharedScore(segRaw, anchorTextSignal);
            score += anchorSharedTotal;

            int proximity = preferredOffset.HasValue ? Math.Abs(segStart - preferredOffset.Value) : int.MaxValue;
            return (score, tmSharedTotal, termSignalHits, anchorSharedTotal, proximity);
        }

        var orderedByStart = candidates
            .OrderBy(c => c.start)
            .ToList();

        int OccurrenceProximity((int start, int length) c)
        {
            if (!preferredOccurrenceHint.HasValue)
                return int.MaxValue;

            for (int i = 0; i < orderedByStart.Count; i++)
            {
                if (orderedByStart[i].start == c.start && orderedByStart[i].length == c.length)
                    return Math.Abs(i - preferredOccurrenceHint.Value);
            }

            return int.MaxValue;
        }

        // Deterministic tie-break: score first, then stronger secondary signals,
        // then proximity hint (if available), then raw start.
        var best = candidates
            .Select(c => (candidate: c, key: Score(c)))
            .OrderByDescending(x => x.key.score)
            .ThenByDescending(x => x.key.tmSharedTotal)
            .ThenByDescending(x => x.key.termSignalHits)
            .ThenByDescending(x => x.key.anchorSharedTotal)
            .ThenBy(x => OccurrenceProximity(x.candidate))
            .ThenBy(x => x.key.proximity)
            .ThenBy(x => x.candidate.start)
            .First().candidate;

        start = best.start;
        length = best.length;
        return true;
    }

    private static void AddTermOccurrencesInSegment(
        List<(int Start, int Length)> ranges,
        string docText,
        int segmentStart,
        int segmentLength,
        string term)
    {
        if (string.IsNullOrWhiteSpace(term) || segmentLength <= 0)
            return;

        int segmentEnd = Math.Min(docText.Length, segmentStart + segmentLength);
        if (segmentStart < 0 || segmentStart >= segmentEnd)
            return;

        if (!CjkMatchNormalizer.ContainsCjk(term))
        {
            int from = segmentStart;
            while (from < segmentEnd)
            {
                int max = segmentEnd - from;
                if (max <= 0) break;
                int idx = docText.IndexOf(term, from, max, StringComparison.Ordinal);
                if (idx < 0) break;
                ranges.Add((idx, term.Length));
                from = idx + 1;
            }
            return;
        }

        string segmentRaw = docText.Substring(segmentStart, segmentEnd - segmentStart);
        var nSeg = CjkMatchNormalizer.NormalizeWithMap(segmentRaw);
        string nTerm = CjkMatchNormalizer.Normalize(term);
        if (string.IsNullOrEmpty(nSeg.Normalized) || string.IsNullOrEmpty(nTerm))
            return;

        int fromNorm = 0;
        while (fromNorm < nSeg.Normalized.Length)
        {
            int nIdx = nSeg.Normalized.IndexOf(nTerm, fromNorm, StringComparison.Ordinal);
            if (nIdx < 0) break;

            int rawStartLocal = CjkMatchNormalizer.RawIndexFromNormalizedPos(nSeg, nIdx);
            int rawEndLocal = CjkMatchNormalizer.RawIndexFromNormalizedPos(nSeg, nIdx + nTerm.Length);
            if (rawEndLocal > rawStartLocal)
                ranges.Add((segmentStart + rawStartLocal, rawEndLocal - rawStartLocal));

            fromNorm = nIdx + 1;
        }
    }

    private void JumpToChineseInEnglishLine()
    {
        if (_editor == null)
        {
            Status?.Invoke(this, "Editor not available.");
            return;
        }

        var text = _editor.Text ?? "";
        if (string.IsNullOrWhiteSpace(text))
        {
            Status?.Invoke(this, "Editor is empty.");
            return;
        }

        List<ProjectionBlockInfo> blocks;
        try
        {
            blocks = ParseProjectionBlocksWithOffsets(text);
        }
        catch (Exception ex)
        {
            Status?.Invoke(this, "Projection parse failed: " + ex.Message);
            return;
        }

        if (blocks.Count == 0)
        {
            Status?.Invoke(this, "No blocks found.");
            return;
        }

        int caret = _editor.CaretOffset;
        int curIx = FindBlockIndexAtOrAfterCaret(blocks, caret);
        if (curIx < 0) curIx = 0;

        int hitIx = -1;

        for (int i = Math.Min(curIx + 1, blocks.Count); i < blocks.Count; i++)
        {
            if (ContainsChineseChar(blocks[i].En))
            {
                hitIx = i;
                break;
            }
        }

        bool wrapped = false;
        if (hitIx < 0)
        {
            for (int i = 0; i <= Math.Min(curIx, blocks.Count - 1); i++)
            {
                if (ContainsChineseChar(blocks[i].En))
                {
                    hitIx = i;
                    wrapped = true;
                    break;
                }
            }
        }

        if (hitIx < 0)
        {
            Status?.Invoke(this, "No Chinese characters found in EN lines.");
            return;
        }

        SelectEnValueAndReveal(blocks[hitIx]);

        Status?.Invoke(this, wrapped
            ? $"Found Chinese in EN at block <{blocks[hitIx].BlockNumber}> (wrapped)."
            : $"Found Chinese in EN at block <{blocks[hitIx].BlockNumber}>.");
    }

    private int GetSelectedChunkSize()
    {
        try
        {
            if (_cmbChunkSize?.SelectedItem is ComboBoxItem cbi &&
                int.TryParse(cbi.Content?.ToString(), out var n) &&
                n > 0)
                return n;

            if (_cmbChunkSize?.SelectedItem != null &&
                int.TryParse(_cmbChunkSize.SelectedItem.ToString(), out n) &&
                n > 0)
                return n;
        }
        catch
        {
        }

        return 10;
    }

    private static int FindBlockIndexAtOrAfterCaret(List<ProjectionBlockInfo> blocks, int caretOffset)
    {
        if (blocks.Count == 0) return -1;

        for (int i = 0; i < blocks.Count; i++)
        {
            var b = blocks[i];
            if (caretOffset >= b.BlockStartOffset && caretOffset < b.BlockEndOffsetExclusive)
                return i;
        }

        if (caretOffset < blocks[0].BlockStartOffset)
            return 0;

        return blocks.Count - 1;
    }

    private void SelectAndRevealBlock(ProjectionBlockInfo block)
    {
        if (_editor?.Document == null)
            return;

        int start = Math.Clamp(block.BlockStartOffset, 0, _editor.Document.TextLength);
        int end = Math.Clamp(block.BlockEndOffsetExclusive, start, _editor.Document.TextLength);

        _editor.CaretOffset = start;
        _editor.TextArea.Selection = Selection.Create(_editor.TextArea, start, end);

        try
        {
            var line = _editor.Document.GetLineByOffset(start).LineNumber;
            _editor.ScrollToLine(line);
        }
        catch
        {
        }

        _editor.Focus();
    }

    private void SelectEnValueAndReveal(ProjectionBlockInfo block)
    {
        if (_editor?.Document == null)
            return;

        int start = Math.Clamp(block.EnValueStartOffset, 0, _editor.Document.TextLength);
        int end = Math.Clamp(block.EnValueStartOffset + block.EnValueLength, start, _editor.Document.TextLength);

        _editor.CaretOffset = start;
        _editor.TextArea.Selection = Selection.Create(_editor.TextArea, start, end);

        try
        {
            var line = _editor.Document.GetLineByOffset(start).LineNumber;
            _editor.ScrollToLine(line);
        }
        catch
        {
        }

        _editor.Focus();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Z && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            DoUndo();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Y && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            DoRedo();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.C &&
            e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            _ = CopyChunkWithPromptAsync();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.V &&
            e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            _ = PasteByMatchingBlockNumberAsync();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F8)
        {
            JumpToNextUntranslated();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F &&
            e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            JumpToChineseInEnglishLine();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.D2 && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            SwitchMode(TranslationEditMode.Body);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.D3 && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            SwitchMode(TranslationEditMode.Notes);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            SaveRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.R && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            RevertRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.A && e.KeyModifiers == KeyModifiers.Alt)
        {
            ReviewActionRequested?.Invoke(this, TranslationReviewStatuses.Approved);
            e.Handled = true;
            return;
        }

        // Alt+N (needs-work) removed - button hidden, shortcut disabled

        if (e.Key == Key.F9)
        {
            ReviewActionRequested?.Invoke(this, TranslationReviewStatuses.Approved);
            e.Handled = true;
            return;
        }

        // F10 (needs-work) removed - button hidden, shortcut disabled

        if (e.Key == Key.F11)
        {
            ReviewActionRequested?.Invoke(this, TranslationReviewStatuses.Rejected);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Right && e.KeyModifiers == KeyModifiers.Alt)
        {
            JumpToNextBlock();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Left && e.KeyModifiers == KeyModifiers.Alt)
        {
            JumpToPreviousBlock();
            e.Handled = true;
        }
    }

    private IClipboard? GetClipboard()
    {
        var top = TopLevel.GetTopLevel(this);
        return top?.Clipboard;
    }

    /// <summary>Delegate for finding masters mentioned in text. Set by MainWindow.</summary>
    public Func<string, List<ZenMasterRecord>>? FindMastersInText { get; set; }

    private string BuildPrompt(string selectedProjection)
    {
        var masterContext = "";

        // Scan for zen masters mentioned in the Chinese text
        if (FindMastersInText != null)
        {
            try
            {
                var masters = FindMastersInText(selectedProjection);
                if (masters.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine();
                    sb.AppendLine("CONTEXT — Zen masters mentioned in this passage:");
                    foreach (var m in masters.Take(5)) // cap at 5 to avoid prompt bloat
                    {
                        sb.Append($"- {m.CanonicalName} ({m.DatesSummary})");
                        if (!string.IsNullOrWhiteSpace(m.School)) sb.Append($", {m.School}");
                        if (!string.IsNullOrWhiteSpace(m.Teacher)) sb.Append($", student of {m.Teacher}");
                        if (!string.IsNullOrWhiteSpace(m.Notes)) sb.Append($". {m.Notes}");
                        sb.AppendLine();
                    }
                    masterContext = sb.ToString();
                }
            }
            catch { }
        }

        return
$@"You are translating a CBETA projection block.

STRICT RULES:
- Edit ONLY EN: lines.
- Keep <n> and all ZH: lines unchanged.
- Keep the same number of EN[n] lines as ZH[n] lines.
- Do NOT merge lines.
- Do NOT split lines.
- Do NOT add commentary.
- Do NOT add or remove blocks.
- Do NOT use angle brackets < or > in EN text.
- Output ONLY one markdown code block.
- Translate common Zen honorifics/titles like ""heshang"" as ""the master"" (or ""Venerable"") in EN, not left as Chinese.
{masterContext}
```markdown
{selectedProjection}
```";
    }

    private static string ExtractCodeBlockOrRaw(string text)
    {
        var m = Regex.Match(text, @"```(?:markdown|md|text)?\s*(?<x>[\s\S]*?)\s*```", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups["x"].Value.Trim() : text.Trim();
    }

    private static bool ShouldIncludeForCopy(ProjectionBlockInfo block, bool requireUntranslated)
    {
        if (block == null) return false;

        if (IsSkippableForCopyOrJump(block))
            return false;

        if (requireUntranslated && !string.IsNullOrWhiteSpace(block.En))
            return false;

        return true;
    }

    private static bool ShouldJumpToUntranslated(ProjectionBlockInfo block)
    {
        if (block == null) return false;

        if (IsSkippableForCopyOrJump(block))
            return false;

        return string.IsNullOrWhiteSpace(block.En);
    }

    private static bool IsSkippableForCopyOrJump(ProjectionBlockInfo block)
    {
        var zh = block.Zh ?? "";
        var en = block.En ?? "";

        if (string.IsNullOrWhiteSpace(zh) && string.IsNullOrWhiteSpace(en))
            return true;

        if (!ContainsChineseChar(zh))
            return true;

        return false;
    }

    private static bool ContainsChineseChar(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return false;

        foreach (char ch in s)
        {
            if ((ch >= '\u3400' && ch <= '\u4DBF') ||
                (ch >= '\u4E00' && ch <= '\u9FFF') ||
                (ch >= '\uF900' && ch <= '\uFAFF'))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class ProjectionBlockInfo
    {
        public int BlockNumber { get; set; }
        public string Zh { get; set; } = "";
        public string En { get; set; } = "";
        public int BlockStartOffset { get; set; }
        public int BlockEndOffsetExclusive { get; set; }
        public int EnValueStartOffset { get; set; }
        public int EnValueLength { get; set; }
    }

    private static string StripProjectionPrefixSpace(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text[0] == ' ' ? text.Substring(1) : text;
    }

    private sealed class ProjectionLineInfo
    {
        public string Text { get; init; } = "";
        public int StartOffset { get; init; }
    }

    private static List<ProjectionLineInfo> SplitProjectionLinesWithOffsets(string text)
    {
        var lines = new List<ProjectionLineInfo>();
        if (string.IsNullOrEmpty(text))
            return lines;

        int pos = 0;
        while (pos < text.Length)
        {
            int start = pos;
            while (pos < text.Length && text[pos] != '\r' && text[pos] != '\n')
                pos++;

            lines.Add(new ProjectionLineInfo
            {
                Text = text.Substring(start, pos - start),
                StartOffset = start
            });

            if (pos < text.Length)
            {
                if (text[pos] == '\r' && pos + 1 < text.Length && text[pos + 1] == '\n')
                    pos += 2;
                else
                    pos += 1;
            }
        }

        return lines;
    }

    private static List<ProjectionBlockInfo> ParseProjectionBlocksWithOffsets(string text)
    {
        text ??= "";
        var lines = SplitProjectionLinesWithOffsets(text);

        var list = new List<ProjectionBlockInfo>();
        int i = 0;
        while (i < lines.Count)
        {
            var headerTrim = lines[i].Text.Trim();
            if (!(headerTrim.StartsWith("<") && headerTrim.EndsWith(">")))
            {
                i++;
                continue;
            }

            var rawNum = headerTrim.Substring(1, headerTrim.Length - 2);
            if (!int.TryParse(rawNum, out int num))
                throw new InvalidOperationException($"Invalid block header: {headerTrim}");

            int blockStartOffset = lines[i].StartOffset;
            i++;

            string? zh = null;
            string? en = null;
            int enValueStartOffset = -1;
            int enValueLength = -1;

            while (i < lines.Count)
            {
                var cur = lines[i].Text;
                var curTrim = cur.Trim();
                if (curTrim.StartsWith("<") && curTrim.EndsWith(">"))
                    break;

                if (cur.StartsWith("ZH:", StringComparison.Ordinal))
                {
                    zh = StripProjectionPrefixSpace(cur.Substring(3));
                }
                else if (cur.StartsWith("EN:", StringComparison.Ordinal))
                {
                    var raw = cur.Substring(3);
                    en = StripProjectionPrefixSpace(raw);
                    enValueStartOffset = lines[i].StartOffset + 3 + ((raw.Length > 0 && raw[0] == ' ') ? 1 : 0);
                    enValueLength = en.Length;
                }
                else if (!string.IsNullOrWhiteSpace(cur))
                {
                    throw new InvalidOperationException(
                        $"Block <{num}> contains a continuation line. Multiline EN is not supported; keep one EN line per block and use numbered batch paste for many blocks.");
                }

                i++;
            }

            if (zh == null)
                throw new InvalidOperationException($"Block <{num}> missing ZH.");
            if (en == null)
                throw new InvalidOperationException($"Block <{num}> missing EN.");

            int blockEndOffsetExclusive = i < lines.Count ? lines[i].StartOffset : text.Length;
            list.Add(new ProjectionBlockInfo
            {
                BlockNumber = num,
                Zh = zh,
                En = en,
                BlockStartOffset = blockStartOffset,
                BlockEndOffsetExclusive = blockEndOffsetExclusive,
                EnValueStartOffset = enValueStartOffset,
                EnValueLength = enValueLength
            });
        }

        return list;
    }
    private static void ValidateEnglish(string en, int blockNumber)
    {
        en ??= "";

        if (en.Contains('\n') || en.Contains('\r'))
            throw new InvalidOperationException(
                $"Block <{blockNumber}> EN spans multiple lines. Multiline EN is not supported; keep one EN line per block.");

        if (en.Contains('<') || en.Contains('>'))
            throw new InvalidOperationException($"Block <{blockNumber}> EN contains '<' or '>' which is not allowed.");

        for (int i = 0; i < en.Length; i++)
        {
            char ch = en[i];

            if (char.IsHighSurrogate(ch))
            {
                if (i + 1 < en.Length && char.IsLowSurrogate(en[i + 1]))
                {
                    i++;
                    continue;
                }

                throw new InvalidOperationException(
                    $"Block <{blockNumber}> EN contains invalid XML character (unpaired high surrogate U+{((int)ch):X4}) at position {i + 1}.");
            }

            if (char.IsLowSurrogate(ch))
            {
                throw new InvalidOperationException(
                    $"Block <{blockNumber}> EN contains invalid XML character (unpaired low surrogate U+{((int)ch):X4}) at position {i + 1}.");
            }

            bool ok =
                ch == '\t' ||
                (ch >= 0x20 && ch <= 0xD7FF) ||
                (ch >= 0xE000 && ch <= 0xFFFD);

            if (!ok)
            {
                throw new InvalidOperationException(
                    $"Block <{blockNumber}> EN contains invalid XML character (U+{((int)ch):X4}) at position {i + 1}.");
            }
        }
    }

    public event EventHandler<CurrentProjectionSegmentChangedEventArgs>? CurrentSegmentChanged;

    private void PublishCurrentSegmentDebounced()
    {
        _publishDebounce?.Cancel();
        _publishDebounce = new CancellationTokenSource();
        var token = _publishDebounce.Token;
        _ = Task.Delay(300, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
                Avalonia.Threading.Dispatcher.UIThread.Post(() => PublishCurrentSegment());
        }, TaskScheduler.Default);
    }

    private void PublishCurrentSegment()
    {
        try
        {
            // Clear stale assistant content immediately for responsive visual feedback
            _vm.LastAssistantSnapshot = null;
            RenderAssistantSnapshot(null);

            if (_editor == null) return;

            var blocks = ParseProjectionBlocksWithOffsets(_editor.Text ?? "");
            if (blocks.Count == 0) return;

            int caret = _editor.CaretOffset;
            int ix = FindBlockIndexAtOrAfterCaret(blocks, caret);
            if (ix < 0 || ix >= blocks.Count) return;

            var b = blocks[ix];

            // ── Selection-based TM lookup ──
            // When the user highlights Chinese text, search TM for just that
            // phrase instead of the whole block. Minimum 2 CJK characters to
            // avoid matching everything. Falls back to per-block when no
            // meaningful CJK selection exists.
            string zhForLookup = b.Zh ?? "";
            var sel = _editor.SelectedText ?? "";
            if (sel.Length >= 2)
            {
                var cjkOnly = ExtractCjk(sel);
                if (cjkOnly.Length >= 2)
                    zhForLookup = cjkOnly;
            }

            // Build a wider ZH context (prev + current + next) for TM search.
            // Phrases often span <lb> boundaries so the wider window improves match quality.
            string prevZh = ix > 0 ? blocks[ix - 1].Zh ?? "" : "";
            string nextZh = ix + 1 < blocks.Count ? blocks[ix + 1].Zh ?? "" : "";
            string prevTail = LastChars(prevZh, AdjacentContextChars);
            string nextHead = FirstChars(nextZh, AdjacentContextChars);
            string zhContext = prevTail + (b.Zh ?? "") + nextHead;

            CurrentSegmentChanged?.Invoke(this, new CurrentProjectionSegmentChangedEventArgs
            {
                BlockNumber = b.BlockNumber,
                Zh = zhForLookup,
                En = b.En ?? "",
                ZhContext = zhContext,
                BlockStartOffset = b.BlockStartOffset,
                BlockEndOffsetExclusive = b.BlockEndOffsetExclusive,
                Mode = _vm.CurrentMode
            });
        }
        catch
        {
        }
    }

    /// <summary>
    /// Extracts CJK characters from a mixed selection. Handles the common case
    /// where the user selects across a ZH/EN boundary and gets some English
    /// text mixed in. Returns only the CJK codepoints, concatenated.
    /// </summary>
    private static string ExtractCjk(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c >= '\u4E00' && c <= '\u9FFF' ||
                c >= '\u3400' && c <= '\u4DBF' ||
                c >= '\uF900' && c <= '\uFAFF')
                sb.Append(c);
        }
        return sb.ToString();
    }

    public void SetAssistantLoading(bool isLoading)
    {
        var bar = this.FindControl<Avalonia.Controls.ProgressBar>("AssistantLoadingBar");
        if (bar != null) bar.IsVisible = isLoading;
    }

    public void SetAssistantSnapshot(TranslationAssistantSnapshot? snapshot)
    {
        _vm.LastAssistantSnapshot = snapshot;
        RenderAssistantSnapshot(snapshot);
    }

    private void RenderAssistantSnapshot(TranslationAssistantSnapshot? snapshot)
    {
        ClearAssistantHoverBehaviors();

        if (snapshot == null)
        {
            if (_qaHost != null)
                AssistantPanelRenderer.RenderEmptyGuidance(_qaHost,
                    "Select text to see glossary entries, translation memory matches, and quality checks.");
            _termHost?.Children.Clear();
            _approvedTmHost?.Children.Clear();
            _referenceTmHost?.Children.Clear();
            return;
        }

        AssistantPanelRenderer.RenderSnapshot(
            snapshot,
            _qaHost, _termHost, _approvedTmHost, _referenceTmHost,
            titleResolver: rel => _vm.ResolveAssistantTitle(rel),
            brushResolver: key => GetResourceBrush(key),
            postProcessor: null,
            navigationHandler: (_, req) => NavigationRequested?.Invoke(this, req),
            addToScholarHandler: passage => AddToScholarRequested?.Invoke(this, passage));
    }

    private static string FirstChars(string s, int count)
    {
        if (string.IsNullOrEmpty(s) || count <= 0)
            return "";
        return s.Length <= count ? s : s[..count];
    }

    private static string LastChars(string s, int count)
    {
        if (string.IsNullOrEmpty(s) || count <= 0)
            return "";
        return s.Length <= count ? s : s[^count..];
    }

    private void AttachAssistantHover(TextEditor editor)
    {
        if (!_vm.HoverDictionaryEnabled)
            return;

        try
        {
            var behavior = new HoverDictionaryBehaviorEdit(editor, _cedict, _grammar, _dictOverlayCanvas);
            _assistantHoverDisposables.Add(behavior);
        }
        catch
        {
            // assistant hover must never break rendering
        }
    }

    private void ClearAssistantHoverBehaviors()
    {
        foreach (var d in _assistantHoverDisposables)
        {
            try { d.Dispose(); } catch { }
        }
        _assistantHoverDisposables.Clear();
    }

    private IBrush? GetResourceBrush(string key)
    {
        try
        {
            if (Application.Current?.TryFindResource(key, out var obj) == true && obj is IBrush brush)
                return brush;
        }
        catch
        {
        }

        return null;
    }

    public void SetModeProjection(TranslationEditMode mode, string projectionText)
    {
        _vm.SetModeProjectionState(mode, projectionText);

        if (_editor != null)
            _editor.Text = _vm.CurrentProjection;

        if (_emptyState != null) _emptyState.IsVisible = false;

        UpdateModeInfo();
        UpdateModeButtons();
        UpdateQuickInfo();
        PublishCurrentSegment();
    }

    private sealed class TermbaseHighlightTransformer : DocumentColorizingTransformer
    {
        private List<(int Start, int Length)> _ranges = new();
        private static readonly SolidColorBrush s_termBrush = new(Color.FromArgb(90, 255, 185, 0));

        public void SetRanges(IEnumerable<(int Start, int Length)> ranges)
            => _ranges = ranges.OrderBy(r => r.Start).ToList();

        protected override void ColorizeLine(DocumentLine line)
        {
            int lo = LowerBound(_ranges, line.Offset);
            for (int i = lo; i < _ranges.Count; i++)
            {
                var (start, length) = _ranges[i];
                if (start >= line.Offset + line.Length) break;
                int s = Math.Max(start, line.Offset);
                int e = Math.Min(start + length, line.Offset + line.Length);
                if (s >= e) continue;
                ChangeLinePart(s, e, el =>
                    el.TextRunProperties.SetBackgroundBrush(s_termBrush));
            }
        }

        private static int LowerBound(List<(int Start, int Length)> ranges, int lineStart)
        {
            int lo = 0, hi = ranges.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (ranges[mid].Start + ranges[mid].Length <= lineStart) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }
    }

    private sealed class TmSharedHighlightTransformer : DocumentColorizingTransformer
    {
        private List<(int Start, int Length)> _ranges = new();
        private Typeface? _cachedSemiBold;

        public void SetRanges(IEnumerable<(int Start, int Length)> ranges)
            => _ranges = ranges.OrderBy(r => r.Start).ToList();

        private static IBrush GetBlueBrush()
        {
            var app = Application.Current;
            if (app != null && app.TryFindResource("NoteMarkerCommunityFg", out var res) && res is IBrush b)
                return b;
            return Brushes.DodgerBlue;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (_ranges.Count == 0) return;
            var fg = GetBlueBrush();
            int lo = LowerBound(_ranges, line.Offset);
            for (int i = lo; i < _ranges.Count; i++)
            {
                var (start, length) = _ranges[i];
                if (start >= line.Offset + line.Length) break;
                int s = Math.Max(start, line.Offset);
                int e = Math.Min(start + length, line.Offset + line.Length);
                if (s >= e) continue;
                ChangeLinePart(s, e, el =>
                {
                    el.TextRunProperties.SetForegroundBrush(fg);
                    _cachedSemiBold ??= new Typeface(
                        el.TextRunProperties.Typeface.FontFamily,
                        el.TextRunProperties.Typeface.Style,
                        FontWeight.SemiBold,
                        el.TextRunProperties.Typeface.Stretch);
                    el.TextRunProperties.SetTypeface(_cachedSemiBold.Value);
                });
            }
        }

        private static int LowerBound(List<(int Start, int Length)> ranges, int lineStart)
        {
            int lo = 0, hi = ranges.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (ranges[mid].Start + ranges[mid].Length <= lineStart) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }
    }

}


