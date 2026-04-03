using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Views;

public partial class ReadableTabView : UserControl
{
    // -------------------------
    // ViewModel
    // -------------------------
    private readonly ReadableTabViewModel _vm = new();

    // -------------------------
    // Inner editors (read-only panes)
    // -------------------------
    private AnnotatedTextEditor? _editorOriginal;
    private AnnotatedTextEditor? _editorTranslated;
    private TextEditor? _aeOrig;
    private TextEditor? _aeTran;

    // Hover dictionary (orig pane only)
    private HoverDictionaryBehaviorEdit? _hoverDictOrig;
    private Canvas? _dictOverlayCanvas;
    private readonly ICedictDictionary _cedict = App.Services.GetRequiredService<ICedictDictionary>();
    private readonly IGrammarReferenceService _grammar = App.Services.GetRequiredService<IGrammarReferenceService>();

    // -------------------------
    // Selection mirroring
    // -------------------------
    private readonly ISelectionSyncService _selectionSync = App.Services.GetRequiredService<ISelectionSyncService>();
    private DispatcherTimer? _selTimer;
    private bool _syncingSelection;

    private DateTime _ignoreProgrammaticUntilUtc = DateTime.MinValue;
    private const int IgnoreProgrammaticWindowMs = 180;

    private DateTime _suppressPollingUntilUtc = DateTime.MinValue;
    private const int SuppressPollingAfterUserActionMs = 220;

    private DateTime _suppressMirrorUntilUtc = DateTime.MinValue;

    private DateTime _suppressMirrorForMarkerClickUntilUtc = DateTime.MinValue;
    private const int SuppressMirrorAfterMarkerClickMs = 260;

    private int _lastOrigSelStart = -1, _lastOrigSelEnd = -1;
    private int _lastTranSelStart = -1, _lastTranSelEnd = -1;
    private int _lastOrigCaret = -1, _lastTranCaret = -1;

    private DateTime _lastUserInputUtc = DateTime.MinValue;
    private object? _lastUserInputEditor;
    private const int UserInputPriorityWindowMs = 250;

    private bool _mirrorQueued;
    private bool _mirrorSourceIsTranslated;

    // -------------------------
    // Zen toggle
    // -------------------------
    private CheckBox? _chkZenText;

    // -------------------------
    // Notes panel + buttons
    // -------------------------
    private Border? _notesPanel;
    private TextBlock? _notesHeader;
    private TextBox? _notesBody;
    private Button? _btnCloseNotes;
    private Button? _btnAddCommunityNote;
    private Button? _btnDeleteCommunityNote;
    private Button? _btnMoveFootnote;

    private Border? _readableEmptyState;

    // Navigation highlight: cleared on next user click
    private TextEditor? _navHighlightEditor;

    private MarkerColorizer? _markerColorizerOrig;
    private MarkerColorizer? _markerColorizerTran;

    // -------------------------
    // Coding mode
    // -------------------------
    private Border? _codeBarPanel;
    private StackPanel? _codingToggleRow;
    private StackPanel? _codeBarSlots;
    private TextBlock? _txtCodeBarPage;
    private TextBlock? _txtCodeBarStatus;
    private ToggleButton? _btnCodingMode;
    private ToggleButton? _btnCodingModeCompact;

    private bool _codingModeActive;
    private bool _spaceHeld;
    private bool _codingHintShown;
    private int _codeBarPage = 1;
    private TagVocabulary? _tagVocabulary;
    private readonly List<DocumentTag> _appliedTags = new();
    private TagHighlightTransformer? _tagHighlighter;
    private TagHighlightTransformer? _tagHighlighterTran;
    private string? _lastAppliedTagId;
    private ComboBox? _cmbTagUser;
    private Dictionary<string, List<DocumentTag>>? _communityTags;
    private Dictionary<string, TagVocabulary>? _communityVocabularies;
    private string? _selectedTagUser; // null = "Me" (own tags)

    // -------------------------
    // Study panel
    // -------------------------
    private Grid? _readerOuterGrid;
    private Border? _studyPanel;
    private GridSplitter? _studyPanelSplitter;
    private CheckBox? _chkStudyPanel;
    private StackPanel? _studyTermHost;
    private StackPanel? _studyTmHost;
    private TextBlock? _txtStudySegmentZh;
    private TextBlock? _txtStudySegmentEn;
    private string? _lastStudySegmentKey;
    private TextBlock? _txtStudyDictHeadword;
    private TextBlock? _txtStudyDictPinyin;
    private StackPanel? _studyDictSenses;
    private string? _lastStudyDictKey;
    private readonly List<IDisposable> _studyHoverDisposables = new();
    private List<(int Start, int Length, TermHit Hit)>? _termHitRanges;

    // Local state kept in code-behind (UI suppression flags / hot-path counters)
    private bool _suppressZenEvents;
    private long _seq;

    // -------------------------
    // Events to host (forwarded from VM)
    // -------------------------
    public event EventHandler<DocAnnotation>? NoteClicked;
    public event EventHandler<(int XmlIndex, string NoteText, string? Resp)>? CommunityNoteInsertRequested;
    public event EventHandler<(int XmlStart, int XmlEndExclusive)>? CommunityNoteDeleteRequested;
    public event EventHandler<(string RelPath, bool IsZen)>? ZenFlagChanged;

    /// <summary>Returns the currently active translation user (null = community).</summary>
    public Func<string?>? GetTranslationUser { get; set; }

    /// <summary>Pre-filled value for the Resp field in the "Add community note" dialog.</summary>
    public string DefaultResp
    {
        get => _vm.DefaultResp;
        set => _vm.DefaultResp = value;
    }

    public event EventHandler<ReadableTabViewModel.MoveFootnoteRequest>? FootnoteMoveRequested;

    /// <summary>Fired when user requests adding selected text to a Scholar collection.</summary>
    public event EventHandler<ScholarPassage>? AddToScholarRequested;

    // Coding mode events
    public event EventHandler<DocumentTag>? TagApplied;
    public event EventHandler<DocumentTag>? TagRemoved;
    public event EventHandler? CodingModeToggled;
    public event EventHandler? TagEditorRequested;
    public event EventHandler<TagVocabulary>? VocabularyChanged;

    /// <summary>Fired when user clicks Compare to open a 3-pane tag comparison window.</summary>
    public event EventHandler<CompareTagsRequestData>? CompareTagsRequested;
    public event EventHandler<CompareTranslationsRequestData>? CompareTranslationsRequested;

    /// <summary>Fired when the study panel's segment context changes (caret moved to new segment).</summary>
    public event EventHandler<CurrentSegmentContext>? StudyPanelContextChanged;

    /// <summary>Fired when study panel visibility changes (for config persistence).</summary>
    public event EventHandler<bool>? StudyPanelVisibilityChanged;

    // -------------------------
    // Status/log
    // -------------------------
    public event EventHandler<string>? Status;
    private void Say(string msg) => Status?.Invoke(this, msg);

    public ReadableTabView()
    {
        DataContext = _vm;
        InitializeComponent();
        FindControls();
        WireEvents();
        WireVmEvents();

        AttachedToVisualTree += (_, _) =>
        {
            FindControls();
            ResolveInnerEditors();
            RewireButtons();

            SetupHoverDictionary();
            StartSelectionTimer();

            Dispatcher.UIThread.Post(() => _vm.UpdateButtonsState(), DispatcherPriority.Background);
            _vm.Log("ReadableTabView attached");
        };

        DetachedFromVisualTree += (_, _) =>
        {
            StopSelectionTimer();
            DisposeHoverDictionary();
            _vm.Log("ReadableTabView detached");
        };
    }

    private void WireVmEvents()
    {
        // Forward VM events to host-facing events
        _vm.ZenFlagChanged += (_, e) => ZenFlagChanged?.Invoke(this, e);
        _vm.CommunityNoteInsertRequested += (_, e) => CommunityNoteInsertRequested?.Invoke(this, e);
        _vm.CommunityNoteDeleteRequested += (_, e) => CommunityNoteDeleteRequested?.Invoke(this, e);
        _vm.FootnoteMoveRequested += (_, e) =>
            FootnoteMoveRequested?.Invoke(this, e);
        _vm.StatusChanged += (_, msg) => Status?.Invoke(this, msg);

        // Sync VM observable state to UI controls
        _vm.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(ReadableTabViewModel.NotesPanelVisible):
                    if (_notesPanel != null) _notesPanel.IsVisible = _vm.NotesPanelVisible;
                    break;
                case nameof(ReadableTabViewModel.NotesHeaderText):
                    if (_notesHeader != null) _notesHeader.Text = _vm.NotesHeaderText;
                    break;
                case nameof(ReadableTabViewModel.NotesBodyText):
                    if (_notesBody != null) _notesBody.Text = _vm.NotesBodyText;
                    break;
                case nameof(ReadableTabViewModel.CanDeleteCommunityNote):
                    if (_btnDeleteCommunityNote != null)
                    {
                        _btnDeleteCommunityNote.IsEnabled = _vm.CanDeleteCommunityNote;
                        _btnDeleteCommunityNote.IsVisible = _vm.CanDeleteCommunityNote;
                    }
                    break;
                case nameof(ReadableTabViewModel.CanMoveFootnote):
                    if (_btnMoveFootnote != null)
                        _btnMoveFootnote.IsVisible = _vm.CanMoveFootnote;
                    break;
                case nameof(ReadableTabViewModel.IsMoveFootnoteEnabled):
                    if (_btnMoveFootnote != null)
                        _btnMoveFootnote.IsEnabled = _vm.IsMoveFootnoteEnabled;
                    break;
                case nameof(ReadableTabViewModel.CanAddCommunityNote):
                    if (_btnAddCommunityNote != null)
                        _btnAddCommunityNote.IsEnabled = _vm.CanAddCommunityNote;
                    break;
                case nameof(ReadableTabViewModel.IsZenText):
                    if (_chkZenText != null)
                    {
                        _chkZenText.IsChecked = _vm.IsZenText;
                    }
                    break;
                case nameof(ReadableTabViewModel.IsZenEnabled):
                    if (_chkZenText != null)
                        _chkZenText.IsEnabled = _vm.IsZenEnabled;
                    break;
                case nameof(ReadableTabViewModel.IsEmptyState):
                    if (_readableEmptyState != null)
                        _readableEmptyState.IsVisible = _vm.IsEmptyState;
                    break;
            }
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    // =========================
    // Setup / wiring
    // =========================
    private void FindControls()
    {
        _editorOriginal = this.FindControl<AnnotatedTextEditor>("EditorOriginal");
        _editorTranslated = this.FindControl<AnnotatedTextEditor>("EditorTranslated");

        _notesPanel = this.FindControl<Border>("NotesPanel");
        _notesHeader = this.FindControl<TextBlock>("NotesHeader");
        _notesBody = this.FindControl<TextBox>("NotesBody");
        _btnCloseNotes = this.FindControl<Button>("BtnCloseNotes");

        _btnAddCommunityNote = this.FindControl<Button>("BtnAddCommunityNote");
        _btnDeleteCommunityNote = this.FindControl<Button>("BtnDeleteCommunityNote");
        _btnMoveFootnote = this.FindControl<Button>("BtnMoveFootnote");

        _chkZenText = this.FindControl<CheckBox>("ChkZenText");
        _readableEmptyState = this.FindControl<Border>("ReadableEmptyState");
        _dictOverlayCanvas = this.FindControl<Canvas>("DictOverlayCanvas");

        _codeBarPanel = this.FindControl<Border>("CodeBarPanel");
        _codingToggleRow = this.FindControl<StackPanel>("CodingToggleRow");
        _codeBarSlots = this.FindControl<StackPanel>("CodeBarSlots");
        _txtCodeBarPage = this.FindControl<TextBlock>("TxtCodeBarPage");
        _txtCodeBarStatus = this.FindControl<TextBlock>("TxtCodeBarStatus");
        _btnCodingMode = this.FindControl<ToggleButton>("BtnCodingMode");
        _btnCodingModeCompact = this.FindControl<ToggleButton>("BtnCodingModeCompact");
        _cmbTagUser = this.FindControl<ComboBox>("CmbTagUser");

        _readerOuterGrid = this.FindControl<Grid>("ReaderOuterGrid");
        _studyPanel = this.FindControl<Border>("StudyPanel");
        _studyPanelSplitter = this.FindControl<GridSplitter>("StudyPanelSplitter");
        _chkStudyPanel = this.FindControl<CheckBox>("ChkStudyPanel");
        _studyTermHost = this.FindControl<StackPanel>("StudyTermHost");
        _studyTmHost = this.FindControl<StackPanel>("StudyTmHost");
        _txtStudySegmentZh = this.FindControl<TextBlock>("TxtStudySegmentZh");
        _txtStudySegmentEn = this.FindControl<TextBlock>("TxtStudySegmentEn");
        _txtStudyDictHeadword = this.FindControl<TextBlock>("TxtStudyDictHeadword");
        _txtStudyDictPinyin = this.FindControl<TextBlock>("TxtStudyDictPinyin");
        _studyDictSenses = this.FindControl<StackPanel>("StudyDictSenses");

        if (_notesPanel != null) _notesPanel.IsVisible = false;
    }

    private void ResolveInnerEditors()
    {
        _aeOrig = FindInnerTextEditor(_editorOriginal);
        _aeTran = FindInnerTextEditor(_editorTranslated);

        if (_aeOrig != null)
        {
            _aeOrig.IsReadOnly = true;
            _aeOrig.ContextMenu = BuildScholarContextMenu(isTranslated: false);
        }
        if (_aeTran != null)
        {
            _aeTran.IsReadOnly = true;
            _aeTran.ContextMenu = BuildScholarContextMenu(isTranslated: true);
        }
    }

    private void RebuildContextMenus()
    {
        if (_aeOrig != null)
            _aeOrig.ContextMenu = BuildScholarContextMenu(isTranslated: false);
        if (_aeTran != null)
            _aeTran.ContextMenu = BuildScholarContextMenu(isTranslated: true);
    }

    private ContextMenu BuildScholarContextMenu(bool isTranslated)
    {
        var menu = new ContextMenu();
        var addItem = new MenuItem { Header = "Add to Scholar Collection..." };
        addItem.Click += async (_, _) => await OnAddToScholarCollectionAsync(isTranslated);
        menu.Items.Add(addItem);

        var copyLinkItem = new MenuItem { Header = "Copy Link" };
        copyLinkItem.Click += async (_, _) =>
        {
            var relPath = _vm.CurrentRelPathForZen;
            if (string.IsNullOrWhiteSpace(relPath)) return;

            var editor = isTranslated ? _aeTran : _aeOrig;
            var doc = isTranslated ? _vm.RenderTran : _vm.RenderOrig;
            var side = isTranslated ? SearchSide.Translated : SearchSide.Original;

            string? fromLb = null;
            string? toLb = null;
            string? highlight = null;

            if (editor != null && doc != null && !doc.IsEmpty)
            {
                int selStart = GetSelectionStartSafe(editor);
                int selEnd = GetSelectionEndSafe(editor);
                bool hasSelection = selEnd > selStart;

                if (hasSelection)
                {
                    fromLb = LbHelper.FindNearestLbNValue(doc, selStart);
                    toLb = LbHelper.FindNearestLbNValue(doc, Math.Max(selStart, selEnd - 1));

                    // Fall back to highlight text if lb extraction fails
                    if (fromLb == null)
                    {
                        highlight = editor.SelectedText;
                        if (string.IsNullOrWhiteSpace(highlight)) highlight = null;
                    }
                }
            }

            var user = isTranslated ? GetTranslationUser?.Invoke() : null;
            var uri = CbetaUriParser.BuildUri(relPath, fromLb, toLb, highlight, side, user: user);
            var top = TopLevel.GetTopLevel(this);
            if (top?.Clipboard != null)
                await top.Clipboard.SetTextAsync(uri);
            Say("Link copied to clipboard.");
        };
        menu.Items.Add(copyLinkItem);

        var copyRedditLink = new MenuItem { Header = "Copy Reddit Link" };
        copyRedditLink.Click += async (_, _) =>
        {
            var relPath = _vm.CurrentRelPathForZen;
            if (string.IsNullOrWhiteSpace(relPath)) return;

            var editor = isTranslated ? _aeTran : _aeOrig;
            var doc = isTranslated ? _vm.RenderTran : _vm.RenderOrig;
            var side = isTranslated ? SearchSide.Translated : SearchSide.Original;

            string? fromLb = null;
            string? toLb = null;
            string? highlight = null;

            if (editor != null && doc != null && !doc.IsEmpty)
            {
                int selStart = GetSelectionStartSafe(editor);
                int selEnd = GetSelectionEndSafe(editor);
                bool hasSelection = selEnd > selStart;

                if (hasSelection)
                {
                    fromLb = LbHelper.FindNearestLbNValue(doc, selStart);
                    toLb = LbHelper.FindNearestLbNValue(doc, Math.Max(selStart, selEnd - 1));

                    if (fromLb == null)
                    {
                        highlight = editor.SelectedText;
                        if (string.IsNullOrWhiteSpace(highlight)) highlight = null;
                    }
                }
            }

            var userR = isTranslated ? GetTranslationUser?.Invoke() : null;
            var url = CbetaUriParser.BuildShareableUrl(relPath, fromLb, toLb, highlight, side, user: userR);
            var top = TopLevel.GetTopLevel(this);
            if (top?.Clipboard != null)
                await top.Clipboard.SetTextAsync(url);
            Say("Reddit link copied to clipboard.");
        };
        menu.Items.Add(copyRedditLink);

        if (_codingModeActive)
        {
            var addTaggedItem = new MenuItem { Header = "Add Tagged Segment to Scholar" };
            addTaggedItem.Click += (_, _) => OnAddTaggedSegmentToScholar();
            menu.Items.Add(addTaggedItem);

            // When viewing another user's tags, offer to adopt the tag under caret
            if (_selectedTagUser != null)
            {
                var adoptItem = new MenuItem { Header = "Adopt This Tag to My Tags" };
                adoptItem.Click += (_, _) => OnAdoptCommunityTag(isTranslated);
                menu.Items.Add(adoptItem);
            }
        }

        return menu;
    }

    private async Task OnAddToScholarCollectionAsync(bool isTranslated)
    {
        var editor = isTranslated ? _aeTran : _aeOrig;
        if (editor == null) return;

        string selectedText = editor.SelectedText ?? "";
        if (string.IsNullOrWhiteSpace(selectedText))
        {
            Say("Select some text first, then right-click to add to Scholar.");
            return;
        }

        // Get text from the other pane (selection sync may have mirrored it)
        var otherEditor = isTranslated ? _aeOrig : _aeTran;
        string otherText = otherEditor?.SelectedText ?? "";

        // If the other pane has no selection, try multi-segment mapping
        if (string.IsNullOrWhiteSpace(otherText) && otherEditor != null)
        {
            var srcDoc = isTranslated ? _vm.RenderTran : _vm.RenderOrig;
            var dstDoc = isTranslated ? _vm.RenderOrig : _vm.RenderTran;

            int selStart = GetSelectionStartSafe(editor);
            int selEnd = GetSelectionEndSafe(editor);
            bool hasSelection = selEnd > selStart;

            if (hasSelection && !srcDoc.IsEmpty && !dstDoc.IsEmpty)
            {
                // Find all source segments overlapping the selection
                var mappedParts = new List<string>();
                foreach (var seg in srcDoc.Segments)
                {
                    if (seg.Start >= selEnd || seg.EndExclusive <= selStart)
                        continue; // no overlap

                    // This segment overlaps — find corresponding destination segment
                    if (_selectionSync.TryGetDestinationSegment(srcDoc, dstDoc, seg.Start, out var dstSeg))
                    {
                        int dstLen = otherEditor.Text?.Length ?? 0;
                        int s = Math.Clamp(dstSeg.Start, 0, dstLen);
                        int e = Math.Clamp(dstSeg.EndExclusive, 0, dstLen);
                        if (e > s)
                        {
                            string part = otherEditor.Text!.Substring(s, e - s);
                            if (!string.IsNullOrWhiteSpace(part))
                                mappedParts.Add(part);
                        }
                    }
                }

                if (mappedParts.Count > 0)
                    otherText = string.Join("\n", mappedParts);
            }

            // Single-segment fallback (original behavior)
            if (string.IsNullOrWhiteSpace(otherText))
            {
                int caret = GetCaretOffsetSafe(editor);
                if (caret >= 0 && !srcDoc.IsEmpty && !dstDoc.IsEmpty
                    && _selectionSync.TryGetDestinationSegment(srcDoc, dstDoc, caret, out var dstSeg2))
                {
                    int len = otherEditor.Text?.Length ?? 0;
                    int s = Math.Clamp(dstSeg2.Start, 0, len);
                    int e = Math.Clamp(dstSeg2.EndExclusive, 0, len);
                    if (e > s)
                        otherText = otherEditor.Text?.Substring(s, e - s) ?? "";
                }
            }
        }

        var passage = new ScholarPassage
        {
            ZhText = isTranslated ? otherText : selectedText,
            EnText = isTranslated ? selectedText : otherText,
            SourceRelPath = _vm.CurrentRelPathForZen ?? ""
        };

        // Capture lb values for zen:// link generation
        var lbDoc = isTranslated ? _vm.RenderTran : _vm.RenderOrig;
        if (lbDoc != null && !lbDoc.IsEmpty)
        {
            int lbSelStart = GetSelectionStartSafe(editor);
            int lbSelEnd = GetSelectionEndSafe(editor);
            passage.FromLb = LbHelper.FindNearestLbNValue(lbDoc, lbSelStart);
            passage.ToLb = LbHelper.FindNearestLbNValue(lbDoc, Math.Max(lbSelStart, lbSelEnd - 1));
        }

        AddToScholarRequested?.Invoke(this, passage);
        await Task.CompletedTask;
    }

    private static TextEditor? FindInnerTextEditor(Control? root)
    {
        if (root == null) return null;
        if (root is TextEditor te) return te;

        var found = root.GetVisualDescendants().OfType<TextEditor>().FirstOrDefault();
        if (found != null) return found;

        try
        {
            var t = root.GetType();
            foreach (var name in new[] { "Editor", "TextEditor", "InnerEditor", "InnerTextEditor" })
            {
                var pi = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi?.GetValue(root) is TextEditor te2) return te2;
            }
        }
        catch { }

        return null;
    }

    private void WireEvents()
    {
        HookUserInputTracking(_editorOriginal, isTranslated: false);
        HookUserInputTracking(_editorTranslated, isTranslated: true);

        // Global tunnel click for marker clicks + move clicks
        AddHandler(InputElement.PointerPressedEvent, OnPointerPressed_Tunnel, RoutingStrategies.Tunnel, handledEventsToo: true);

        if (_chkZenText != null)
            _chkZenText.IsCheckedChanged += ChkZenText_IsCheckedChanged;

        if (_chkStudyPanel != null)
        {
            _chkStudyPanel.Checked += (_, _) => UpdateStudyPanelVisibility();
            _chkStudyPanel.Unchecked += (_, _) => UpdateStudyPanelVisibility();
        }

        if (_btnCloseNotes != null)
            _btnCloseNotes.Click += (_, _) => CancelMoveModeAndHideNotes();

        // Coding mode toggle buttons
        if (_btnCodingMode != null)
            _btnCodingMode.IsCheckedChanged += (_, _) => SetCodingModeActive(_btnCodingMode.IsChecked == true);
        if (_btnCodingModeCompact != null)
            _btnCodingModeCompact.IsCheckedChanged += (_, _) => SetCodingModeActive(_btnCodingModeCompact.IsChecked == true);
        if (_cmbTagUser != null)
            _cmbTagUser.SelectionChanged += OnTagUserSelectionChanged;

        var btnEditTags = this.FindControl<Button>("BtnEditTags");
        if (btnEditTags != null)
            btnEditTags.Click += (_, _) => TagEditorRequested?.Invoke(this, EventArgs.Empty);

        var btnCompareTags = this.FindControl<Button>("BtnCompareTags");
        if (btnCompareTags != null)
            btnCompareTags.Click += OnCompareTagsClicked;

        var btnCompareTranslations = this.FindControl<Button>("BtnCompareTranslations");
        if (btnCompareTranslations != null)
            btnCompareTranslations.Click += (_, _) => CompareTranslationsRequested?.Invoke(this, null!);

        // Tunnel key handlers for coding mode (Space tracking + F2 + coding keys)
        AddHandler(InputElement.KeyDownEvent, OnCodingKeyDown_Tunnel, RoutingStrategies.Tunnel, handledEventsToo: false);
        AddHandler(InputElement.KeyUpEvent, OnCodingKeyUp_Tunnel, RoutingStrategies.Tunnel, handledEventsToo: false);

        RewireButtons();
    }

    private void RewireButtons()
    {
        if (_btnAddCommunityNote != null)
        {
            _btnAddCommunityNote.Click -= BtnAddCommunityNote_Click;
            _btnAddCommunityNote.Click += BtnAddCommunityNote_Click;
        }

        if (_btnDeleteCommunityNote != null)
        {
            _btnDeleteCommunityNote.Click -= BtnDeleteCommunityNote_Click;
            _btnDeleteCommunityNote.Click += BtnDeleteCommunityNote_Click;
        }

        if (_btnMoveFootnote != null)
        {
            _btnMoveFootnote.Click -= BtnMoveFootnote_Click;
            _btnMoveFootnote.Click += BtnMoveFootnote_Click;
        }

        UpdateButtonsState();
    }

    // =========================
    // Public API (called by host)
    // =========================
    public void Clear()
    {
        _vm.RenderOrig = RenderedDocument.Empty;
        _vm.RenderTran = RenderedDocument.Empty;
        _vm.IsEmptyState = true;

        try { UninstallMarkerColorizers(); } catch { }

        // Clear tag highlights (stale from previous file)
        ClearTagHighlights();

        if (_aeOrig != null) _aeOrig.Text = "";
        if (_aeTran != null) _aeTran.Text = "";

        _lastOrigSelStart = _lastOrigSelEnd = -1;
        _lastTranSelStart = _lastTranSelEnd = -1;
        _lastOrigCaret = -1;
        _lastTranCaret = -1;

        SetZenContext(null, isZen: false);

        CancelMoveModeAndHideNotes();

        _lastStudySegmentKey = null;
        _lastStudyDictKey = null;
        _vm.LastStudySnapshot = null;
        _termHitRanges = null;
        ClearStudyHoverBehaviors();

        _vm.PendingRefresh = false;
        UpdateButtonsState();
    }

    public void SetRendered(RenderedDocument orig, RenderedDocument tran)
    {
        _vm.RenderOrig = orig ?? RenderedDocument.Empty;
        _vm.RenderTran = tran ?? RenderedDocument.Empty;
        _vm.IsEmptyState = false;

        FindControls();
        ResolveInnerEditors();
        RewireButtons();

        if (_aeOrig == null || _aeTran == null) return;

        var (origSv, origOff) = GetScrollOffsetSafe(_aeOrig);
        var (tranSv, tranOff) = GetScrollOffsetSafe(_aeTran);

        int origCaret = GetCaretOffsetSafe(_aeOrig);
        int tranCaret = GetCaretOffsetSafe(_aeTran);

        int origSelS = GetSelectionStartSafe(_aeOrig);
        int origSelE = GetSelectionEndSafe(_aeOrig);
        int tranSelS = GetSelectionStartSafe(_aeTran);
        int tranSelE = GetSelectionEndSafe(_aeTran);

        _syncingSelection = true;
        _suppressPollingUntilUtc = DateTime.UtcNow.AddMilliseconds(700);
        _ignoreProgrammaticUntilUtc = DateTime.UtcNow.AddMilliseconds(700);
        _suppressMirrorUntilUtc = DateTime.UtcNow.AddMilliseconds(900);
        _suppressMirrorForMarkerClickUntilUtc = DateTime.UtcNow.AddMilliseconds(700);

        try
        {
            // remove any old transformers before swapping docs/text
            try { UninstallMarkerColorizers(); } catch { }

            _aeOrig.Text = _vm.RenderOrig.Text ?? "";
            _aeTran.Text = _vm.RenderTran.Text ?? "";

            // install colorizers for the NEW marker spans
            InstallMarkerColorizers();

            SetupHoverDictionary();
            CancelMoveModeAndHideNotes();

            ExitPending("SetRendered");

            try
            {
                if (_aeOrig.TextArea != null)
                {
                    int len = (_aeOrig.Text ?? "").Length;
                    _aeOrig.TextArea.Caret.Offset = Math.Clamp(origCaret < 0 ? 0 : origCaret, 0, len);

                    if (origSelE != origSelS)
                    {
                        int s = Math.Clamp(Math.Min(origSelS, origSelE), 0, len);
                        int e = Math.Clamp(Math.Max(origSelS, origSelE), 0, len);
                        _aeOrig.TextArea.Selection = Selection.Create(_aeOrig.TextArea, s, e);
                    }
                }

                if (_aeTran.TextArea != null)
                {
                    int len = (_aeTran.Text ?? "").Length;
                    _aeTran.TextArea.Caret.Offset = Math.Clamp(tranCaret < 0 ? 0 : tranCaret, 0, len);

                    if (tranSelE != tranSelS)
                    {
                        int s = Math.Clamp(Math.Min(tranSelS, tranSelE), 0, len);
                        int e = Math.Clamp(Math.Max(tranSelS, tranSelE), 0, len);
                        _aeTran.TextArea.Selection = Selection.Create(_aeTran.TextArea, s, e);
                    }
                }
            }
            catch { }

            // Restore scroll offsets twice: once on Background, then again on Render
            // (AvaloniaEdit may adjust scroll to keep caret visible during layout/render.)
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    SetScrollOffsetSafe(origSv, origOff);
                    SetScrollOffsetSafe(tranSv, tranOff);
                }
                catch { }
            }, DispatcherPriority.Background);

            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    SetScrollOffsetSafe(origSv, origOff);
                    SetScrollOffsetSafe(tranSv, tranOff);
                }
                catch { }
            }, DispatcherPriority.Render);
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    public void SetHoverDictionaryEnabled(bool enabled)
    {
        _vm.HoverDictionaryEnabled = enabled;
        if (enabled) SetupHoverDictionary();
        else DisposeHoverDictionary();
    }

    public bool GetHoverDictionaryEnabled() => _vm.HoverDictionaryEnabled;

    public void SetZenContext(string? relPath, bool isZen)
    {
        _vm.CurrentRelPathForZen = relPath;

        if (_chkZenText == null) return;

        _suppressZenEvents = true;
        try
        {
            _chkZenText.IsChecked = isZen;
            _chkZenText.IsEnabled = !string.IsNullOrWhiteSpace(relPath);
        }
        finally
        {
            _suppressZenEvents = false;
        }
    }

    // =========================
    // Navigate-to (called from secondary windows after SetRendered)
    // =========================

    /// <summary>
    /// Scrolls the appropriate pane to the location described by <paramref name="request"/>
    /// and briefly highlights the matched span.
    /// Must be called after <see cref="SetRendered"/> has completed and the layout has settled.
    /// </summary>
    public async Task NavigateToAsync(NavigationRequest request)
    {
        // Give the layout engine one full pass so the text is measured and scrollable.
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await Task.Delay(150);

        var doc = request.Side == SearchSide.Original ? _vm.RenderOrig : _vm.RenderTran;
        var editor = request.Side == SearchSide.Original ? _aeOrig : _aeTran;

        if (doc == null || doc.IsEmpty || editor?.Document == null)
            return;

        // --- lb-based navigation (preferred for deep links with from/to params) ---
        if (!string.IsNullOrEmpty(request.FromLb))
        {
            System.Diagnostics.Debug.WriteLine($"[DeepLink] FromLb={request.FromLb}, ToLb={request.ToLb}");
            System.Diagnostics.Debug.WriteLine($"[DeepLink] Doc has {doc.Segments.Count} segments, text length={doc.Text?.Length ?? 0}");
            // Dump first 10 segment keys for debugging
            for (int i = 0; i < Math.Min(10, doc.Segments.Count); i++)
                System.Diagnostics.Debug.WriteLine($"[DeepLink]   Segment[{i}]: Key={doc.Segments[i].Key} Start={doc.Segments[i].Start}");

            var (lbStart, lbLength) = ResolveLbRange(doc, request.FromLb, request.ToLb);
            System.Diagnostics.Debug.WriteLine($"[DeepLink] ResolveLbRange result: start={lbStart}, length={lbLength}");
            if (lbStart >= 0 && lbLength > 0)
            {
                _ignoreProgrammaticUntilUtc = DateTime.UtcNow.AddMilliseconds(IgnoreProgrammaticWindowMs + 500);
                _suppressMirrorUntilUtc = DateTime.UtcNow.AddMilliseconds(700);

                int lbDocLen = editor.Document.TextLength;
                int lbSafeStart = Math.Clamp(lbStart, 0, Math.Max(0, lbDocLen - 1));
                int lbSafeEnd = Math.Clamp(lbSafeStart + lbLength, 0, lbDocLen);

                // Trim trailing newlines from selection range
                string lbDocText = editor.Document.Text ?? "";
                while (lbSafeEnd > lbSafeStart && lbSafeEnd <= lbDocText.Length &&
                       (lbDocText[lbSafeEnd - 1] == '\n' || lbDocText[lbSafeEnd - 1] == '\r'))
                {
                    lbSafeEnd--;
                }

                editor.TextArea.Caret.Offset = lbSafeStart;
                editor.TextArea.Selection = Selection.Create(editor.TextArea, lbSafeStart, lbSafeEnd);

                var lbLine = editor.Document.GetLineByOffset(lbSafeStart);
                editor.ScrollToLine(lbLine.LineNumber);

                _navHighlightEditor = editor;
                return;
            }
            // lb keys not found — fall through to text-based matching if MatchText is available
        }

        // --- text-based navigation (fallback for search results, old URLs, etc.) ---
        if (string.IsNullOrEmpty(request.MatchText))
            return;

        var hit = FindBestMatchRange(
            doc.Text,
            request.MatchText,
            request.LeftContext,
            request.RightContext,
            request.AnchorStartHint,
            request.AnchorOccurrenceHint,
            request.AnchorTextSignal);
        if (hit.start < 0 || hit.length <= 0)
            return;

        // Suppress selection-mirror during our programmatic move
        _ignoreProgrammaticUntilUtc = DateTime.UtcNow.AddMilliseconds(IgnoreProgrammaticWindowMs + 500);
        _suppressMirrorUntilUtc = DateTime.UtcNow.AddMilliseconds(700);

        int docLen = editor.Document.TextLength;
        int safeStart = Math.Clamp(hit.start, 0, Math.Max(0, docLen - 1));
        int safeEnd = Math.Clamp(safeStart + hit.length, 0, docLen);

        // Trim trailing newlines from selection range
        string fbDocText = editor.Document.Text ?? "";
        while (safeEnd > safeStart && safeEnd <= fbDocText.Length &&
               (fbDocText[safeEnd - 1] == '\n' || fbDocText[safeEnd - 1] == '\r'))
        {
            safeEnd--;
        }

        editor.TextArea.Caret.Offset = safeStart;
        editor.TextArea.Selection = Selection.Create(editor.TextArea, safeStart, safeEnd);

        // Caret.Offset assignment does not always scroll — scroll explicitly too.
        var line = editor.Document.GetLineByOffset(safeStart);
        editor.ScrollToLine(line.LineNumber);

        // Keep the highlight visible indefinitely — it will be cleared on user click
        // (see OnPointerPressed_ClearNavHighlight).
        _navHighlightEditor = editor;
    }

    /// <summary>
    /// Finds the best-scoring match range in the rendered text.
    /// Strategy:
    /// 1) Try exact raw substring matching first (preserves existing behavior).
    /// <summary>
    /// Resolves an lb-based range to rendered text offsets.
    /// Looks up segments by key "lb|{fromLb}" and optionally "lb|{toLb}".
    /// Returns (start, length) in rendered text coordinates, or (-1, 0) if not found.
    /// </summary>
    private static (int start, int length) ResolveLbRange(
        RenderedDocument doc, string fromLb, string? toLb)
    {
        // Try finding the segment with and without edition suffix
        if (!TryFindSegmentByLb(doc, fromLb, out var startSeg))
            return (-1, 0);

        int rangeStart = startSeg.Start;
        int rangeEnd = startSeg.EndExclusive;

        if (!string.IsNullOrEmpty(toLb) && toLb != fromLb)
        {
            if (TryFindSegmentByLb(doc, toLb, out var endSeg))
                rangeEnd = endSeg.EndExclusive;
        }

        return (rangeStart, rangeEnd - rangeStart);
    }

    /// <summary>
    /// Attempts to find a segment by lb n-value, trying both bare key "lb|{nValue}"
    /// and common edition suffixes like "lb|{nValue}|CB".
    /// </summary>
    private static bool TryFindSegmentByLb(
        RenderedDocument doc, string nValue, out RenderSegment seg)
    {
        // Try bare key first
        if (doc.TryGetSegmentByKey("lb|" + nValue, out seg))
            return true;

        // Try with common edition suffixes
        foreach (var suffix in new[] { "CB", "CBETA", "T", "X", "J" })
        {
            if (doc.TryGetSegmentByKey("lb|" + nValue + "|" + suffix, out seg))
                return true;
        }

        // Brute-force: scan segments for any key containing this n-value
        foreach (var s in doc.Segments)
        {
            if (s.Key.StartsWith("lb|", StringComparison.Ordinal))
            {
                var parts = s.Key.Split('|');
                if (parts.Length >= 2 && parts[1] == nValue)
                {
                    seg = s;
                    return true;
                }
            }
        }

        seg = default;
        return false;
    }

    /// <summary>
    /// Extracts rendered text spanning from <paramref name="fromLb"/> to <paramref name="toLb"/> (inclusive).
    /// Returns empty string if the document is empty or the segments cannot be found.
    /// </summary>
    private static string ExtractTextBetweenLbs(RenderedDocument doc, string fromLb, string? toLb)
    {
        if (doc == null || doc.IsEmpty || string.IsNullOrEmpty(fromLb)) return "";

        if (!TryFindSegmentByLb(doc, fromLb, out var startSeg)) return "";
        int start = startSeg.Start;
        int end = startSeg.EndExclusive;

        if (!string.IsNullOrEmpty(toLb) && toLb != fromLb)
        {
            if (TryFindSegmentByLb(doc, toLb, out var endSeg))
                end = endSeg.EndExclusive;
        }

        var text = doc.Text ?? "";
        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, 0, text.Length);
        return end > start ? text.Substring(start, end - start) : "";
    }

    /// 2) If not found, use compact-CJK normalized matching and map back to raw offsets
    ///    so cross-tag / cross-line CJK hits can still be highlighted.
    /// </summary>
    private static (int start, int length) FindBestMatchRange(
        string docText,
        string match,
        string? left,
        string? right,
        int? anchorStartHint,
        int? anchorOccurrenceHint,
        string? anchorTextSignal)
    {
        if (string.IsNullOrEmpty(docText) || string.IsNullOrEmpty(match))
            return (-1, 0);

        static (int start, int length) FindExact(
            string text,
            string m,
            string? l,
            string? r,
            int? startHint,
            int? occurrenceHint,
            string? textSignal)
        {
            var candidates = new List<int>();
            int searchFrom = 0;
            while (searchFrom < text.Length)
            {
                int idx = text.IndexOf(m, searchFrom, StringComparison.Ordinal);
                if (idx < 0) break;
                candidates.Add(idx);
                searchFrom = idx + 1;
            }

            if (candidates.Count == 0)
                return (-1, 0);

            var ranked = candidates
                .Select((idx, occurrence) =>
                {
                    int contextScore = 0;

                    if (!string.IsNullOrEmpty(l))
                    {
                        int winStart = Math.Max(0, idx - l.Length * 2);
                        string pre = text.Substring(winStart, idx - winStart);
                        if (pre.Contains(l, StringComparison.Ordinal)) contextScore += 2;
                    }

                    if (!string.IsNullOrEmpty(r))
                    {
                        int matchEnd = idx + m.Length;
                        int winEnd = Math.Min(text.Length, matchEnd + r.Length * 2);
                        string post = text.Substring(matchEnd, winEnd - matchEnd);
                        if (post.Contains(r, StringComparison.Ordinal)) contextScore += 2;
                    }

                    // Additional soft signal for no-context TM navigation ties.
                    int signalScore = 0;
                    if (!string.IsNullOrWhiteSpace(textSignal))
                    {
                        string raw = text.Substring(idx, m.Length);
                        var shared = CjkMatchNormalizer.FindSharedRawRanges(raw, textSignal, minPhraseLen: 2);
                        foreach (var s in shared)
                            signalScore += Math.Max(1, s.Length);
                    }

                    int startDistance = startHint.HasValue ? Math.Abs(idx - startHint.Value) : int.MaxValue;
                    int occurrenceDistance = occurrenceHint.HasValue ? Math.Abs(occurrence - occurrenceHint.Value) : int.MaxValue;

                    return (idx, contextScore, signalScore, startDistance, occurrenceDistance);
                })
                .OrderByDescending(x => x.contextScore)
                .ThenByDescending(x => x.signalScore)
                .ThenBy(x => x.startDistance)
                .ThenBy(x => x.occurrenceDistance)
                .ThenBy(x => x.idx)
                .First();

            return (ranked.idx, m.Length);
        }

        var exact = FindExact(docText, match, left, right, anchorStartHint, anchorOccurrenceHint, anchorTextSignal);
        if (exact.start >= 0)
            return exact;

        bool anyCjk = CjkMatchNormalizer.ContainsCjk(match)
                      || CjkMatchNormalizer.ContainsCjk(left)
                      || CjkMatchNormalizer.ContainsCjk(right);
        if (!anyCjk)
            return (-1, 0);

        var nDoc = CjkMatchNormalizer.NormalizeWithMap(docText);
        string nMatch = CjkMatchNormalizer.Normalize(match);
        if (string.IsNullOrEmpty(nDoc.Normalized) || string.IsNullOrEmpty(nMatch))
            return (-1, 0);

        string? nLeft = string.IsNullOrEmpty(left) ? null : CjkMatchNormalizer.Normalize(left);
        string? nRight = string.IsNullOrEmpty(right) ? null : CjkMatchNormalizer.Normalize(right);

        var normCandidates = new List<(int normStart, int rawStart, int rawEnd)>();
        int from = 0;
        while (from < nDoc.Normalized.Length)
        {
            int idx = nDoc.Normalized.IndexOf(nMatch, from, StringComparison.Ordinal);
            if (idx < 0) break;

            int rawStart = CjkMatchNormalizer.RawIndexFromNormalizedPos(nDoc, idx);
            int rawEnd = CjkMatchNormalizer.RawIndexFromNormalizedPos(nDoc, idx + nMatch.Length);
            if (rawEnd > rawStart)
                normCandidates.Add((idx, rawStart, rawEnd));

            from = idx + 1;
        }

        if (normCandidates.Count == 0)
            return (-1, 0);

        var bestNorm = normCandidates
            .Select((c, occurrence) =>
            {
                int score = 0;
                if (!string.IsNullOrEmpty(nLeft))
                {
                    int winStart = Math.Max(0, c.normStart - nLeft.Length * 2);
                    string pre = nDoc.Normalized.Substring(winStart, c.normStart - winStart);
                    if (pre.Contains(nLeft, StringComparison.Ordinal)) score += 2;
                }

                if (!string.IsNullOrEmpty(nRight))
                {
                    int matchEnd = c.normStart + nMatch.Length;
                    int winEnd = Math.Min(nDoc.Normalized.Length, matchEnd + nRight.Length * 2);
                    string post = nDoc.Normalized.Substring(matchEnd, winEnd - matchEnd);
                    if (post.Contains(nRight, StringComparison.Ordinal)) score += 2;
                }

                int signalScore = 0;
                if (!string.IsNullOrWhiteSpace(anchorTextSignal))
                {
                    string raw = docText.Substring(c.rawStart, c.rawEnd - c.rawStart);
                    var shared = CjkMatchNormalizer.FindSharedRawRanges(raw, anchorTextSignal, minPhraseLen: 2);
                    foreach (var s in shared)
                        signalScore += Math.Max(1, s.Length);
                }

                int startDistance = anchorStartHint.HasValue ? Math.Abs(c.rawStart - anchorStartHint.Value) : int.MaxValue;
                int occurrenceDistance = anchorOccurrenceHint.HasValue ? Math.Abs(occurrence - anchorOccurrenceHint.Value) : int.MaxValue;

                return (c.rawStart, rawLength: c.rawEnd - c.rawStart, score, signalScore, startDistance, occurrenceDistance);
            })
            .OrderByDescending(x => x.score)
            .ThenByDescending(x => x.signalScore)
            .ThenBy(x => x.startDistance)
            .ThenBy(x => x.occurrenceDistance)
            .ThenBy(x => x.rawStart)
            .First();

        return (bestNorm.rawStart, bestNorm.rawLength);
    }

    // =========================
    // Notes UI
    // =========================
    private void ShowNotes(DocAnnotation ann, bool fromTranslatedPane)
    {
        if (_notesPanel == null || _notesBody == null || _notesHeader == null) return;

        _vm.CurrentAnnotation = ann;
        _vm.CurrentAnnotationFromTranslatedPane = fromTranslatedPane;

        CancelMoveMode(keepPanelOpen: true);

        var kind = GetAnnotationLabel(ann);
        var resp = GetAnnotationResp(ann);
        _notesHeader.Text = string.IsNullOrWhiteSpace(resp) ? kind : $"{kind} ({resp})";

        _notesBody.Text = ann.Text ?? "";
        _notesPanel.IsVisible = true;

        UpdateButtonsState();

        try { _notesBody.SelectionStart = 0; _notesBody.SelectionEnd = 0; } catch { }
    }

    private void HideNotes()
    {
        if (_notesPanel == null || _notesBody == null) return;

        _notesPanel.IsVisible = false;
        _notesBody.Text = "";
        _vm.CurrentAnnotation = null;
        _vm.CurrentAnnotationFromTranslatedPane = false;

        UpdateButtonsState();
    }

    private void UpdateButtonsState()
    {
        if (_vm.PendingRefresh)
        {
            if (_btnAddCommunityNote != null) _btnAddCommunityNote.IsEnabled = false;
            if (_btnDeleteCommunityNote != null) { _btnDeleteCommunityNote.IsEnabled = false; _btnDeleteCommunityNote.IsVisible = false; }
            if (_btnMoveFootnote != null) { _btnMoveFootnote.IsEnabled = false; _btnMoveFootnote.IsVisible = false; }
            return;
        }

        if (_btnAddCommunityNote != null)
            _btnAddCommunityNote.IsEnabled = !_vm.RenderTran.IsEmpty && _aeTran != null;

        if (_btnDeleteCommunityNote != null)
        {
            bool canDelete = _vm.CurrentAnnotation != null && TryGetXmlCommunitySpanStrict(_vm.CurrentAnnotation, out var xs, out var xe) && xe > xs;
            _btnDeleteCommunityNote.IsEnabled = canDelete;
            _btnDeleteCommunityNote.IsVisible = canDelete;
        }

        if (_btnMoveFootnote != null)
        {
            bool canMove = _vm.CurrentAnnotation != null && TryGetXmlSpanLoose(_vm.CurrentAnnotation, out var xs, out var xe) && xe > xs;
            _btnMoveFootnote.IsVisible = canMove;
            _btnMoveFootnote.IsEnabled = canMove && !_vm.AwaitingMoveTargetClick;
        }
    }

    // =========================
    // Community note actions
    // =========================
    private async void BtnAddCommunityNote_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_vm.PendingRefresh) return;
            if (_aeTran == null) ResolveInnerEditors();
            await TryAddCommunityNoteAtSelectionOrCaretAsync();
        }
        catch (Exception ex)
        {
            Log("ADD CLICK EXCEPTION: " + ex.Message);
        }
    }

    private void BtnDeleteCommunityNote_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_vm.PendingRefresh) return;
            DeleteCurrentCommunityNote();
        }
        catch (Exception ex)
        {
            Log("DELETE CLICK EXCEPTION: " + ex.Message);
        }
    }

    public async Task<(bool ok, string reason)> TryAddCommunityNoteAtSelectionOrCaretAsync()
    {
        if (_vm.PendingRefresh) return (false, "Pending refresh");
        if (_notesPanel?.IsVisible == true) return (false, "Notes panel open");
        if (_aeTran == null) return (false, "_aeTran is null");
        if (_vm.RenderTran.IsEmpty) return (false, "_vm.RenderTran empty");

        int renderedIndex = GetSelectionMidpointOrCaretSafe(_aeTran);
        if (renderedIndex < 0) renderedIndex = 0;

        if (!TryMapRenderedIndexToXmlIndex(_vm.RenderTran, renderedIndex, out int xmlIndex))
            return (false, $"Cannot map display index {renderedIndex} to XML index");

        await PromptAddCommunityNoteAsync(xmlIndex);
        return (true, $"Inserted at xmlIndex={xmlIndex}");
    }

    private void DeleteCurrentCommunityNote()
    {
        if (_vm.PendingRefresh) return;
        if (_vm.CurrentAnnotation == null) return;
        if (!TryGetXmlCommunitySpanStrict(_vm.CurrentAnnotation, out int xs, out int xe)) return;

        EnterPending($"delete xs={xs} xe={xe}");
        CommunityNoteDeleteRequested?.Invoke(this, (xs, xe));
        HideNotes();
    }

    private static int GetSelectionMidpointOrCaretSafe(TextEditor ed)
    {
        try
        {
            var sel = ed.TextArea?.Selection;
            if (sel == null || sel.IsEmpty)
                return ed.TextArea?.Caret.Offset ?? 0;

            var seg = sel.SurroundingSegment;
            int start = seg.Offset;
            int endEx = seg.Offset + seg.Length;
            if (endEx < start) (start, endEx) = (endEx, start);
            return start + Math.Max(0, (endEx - start) / 2);
        }
        catch
        {
            return ed.TextArea?.Caret.Offset ?? 0;
        }
    }

    private async Task PromptAddCommunityNoteAsync(int xmlIndex)
    {
        if (_vm.PendingRefresh) return;

        var owner = TopLevel.GetTopLevel(this) as Window;

        IBrush? R(string key)
        {
            try
            {
                if (Application.Current?.Resources?.TryGetValue(key, out var v) == true && v is IBrush b)
                    return b;
            }
            catch { }
            return null;
        }

        var txt = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 140,
            Background = R("ControlBg"),
            Foreground = R("TextFg"),
            BorderBrush = R("BorderBrush"),
            BorderThickness = new Thickness(1),
            CaretBrush = R("TextFg"),
            SelectionBrush = R("SelectionBg"),
        };
        ScrollViewer.SetVerticalScrollBarVisibility(txt, ScrollBarVisibility.Auto);

        var resp = new TextBox
        {
            Text = DefaultResp.Length > 0 ? DefaultResp : null,
            Watermark = "Optional resp (e.g., your initials)",
            Height = 32,
            Background = R("ControlBg"),
            Foreground = R("TextFg"),
            BorderBrush = R("BorderBrush"),
            BorderThickness = new Thickness(1),
            CaretBrush = R("TextFg"),
            SelectionBrush = R("SelectionBg"),
        };

        var btnOk = new Button { Content = "Add note", MinWidth = 110 };
        var btnCancel = new Button { Content = "Cancel", MinWidth = 90 };

        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        btnRow.Children.Add(btnCancel);
        btnRow.Children.Add(btnOk);

        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 10 };
        panel.Children.Add(new TextBlock { Text = "Community note text:" });
        panel.Children.Add(txt);
        panel.Children.Add(new TextBlock { Text = "Resp (optional):" });
        panel.Children.Add(resp);
        panel.Children.Add(btnRow);

        var chrome = new Border
        {
            Background = R("AppBg") ?? R("MenuBg"),
            BorderBrush = R("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Child = panel
        };

        var win = new Window
        {
            Title = "Add community note",
            Width = 520,
            Height = 360,
            Content = chrome,
            WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
            Topmost = false
        };

        win.RequestedThemeVariant =
            owner?.RequestedThemeVariant
            ?? owner?.ActualThemeVariant
            ?? Application.Current?.RequestedThemeVariant
            ?? ThemeVariant.Dark;

        win.Background = R("AppBg") ?? R("MenuBg");

        bool okRes;
        if (owner != null)
        {
            btnCancel.Click += (_, _) => win.Close(false);
            btnOk.Click += (_, _) => win.Close(true);
            okRes = await win.ShowDialog<bool>(owner);
        }
        else
        {
            var tcs = new TaskCompletionSource<bool>();
            void CloseOk(bool ok) { try { win.Close(); } catch { } tcs.TrySetResult(ok); }
            btnCancel.Click += (_, _) => CloseOk(false);
            btnOk.Click += (_, _) => CloseOk(true);
            win.Closed += (_, _) => { if (!tcs.Task.IsCompleted) tcs.TrySetResult(false); };
            win.Show();
            okRes = await tcs.Task;
        }

        if (!okRes) return;

        var noteText = (txt.Text ?? "").Trim();
        if (noteText.Length == 0) return;

        var respVal = (resp.Text ?? "").Trim();
        if (respVal.Length == 0) respVal = null;

        EnterPending($"insert xmlIndex={xmlIndex}");
        CommunityNoteInsertRequested?.Invoke(this, (xmlIndex, noteText, respVal));
    }

    // =========================
    // Move footnote (copy+delete in host)
    // =========================
    private void BtnMoveFootnote_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_vm.PendingRefresh) return;
            if (_vm.CurrentAnnotation == null) return;

            if (!TryGetXmlSpanLoose(_vm.CurrentAnnotation, out var xs, out var xe) || xe <= xs)
            {
                Say("This note cannot be moved (missing XML span).");
                return;
            }

            _vm.AwaitingMoveTargetClick = true;
            _vm.MoveSourceAnnotation = _vm.CurrentAnnotation;

            if (_notesHeader != null)
                _notesHeader.Text = "Note (click new location to move)";

            Say("Move mode: click in the reader where you want this footnote.");
            UpdateButtonsState();
        }
        catch (Exception ex)
        {
            Log("MOVE CLICK EXCEPTION: " + ex.Message);
        }
    }

    private void CancelMoveMode(bool keepPanelOpen)
    {
        _vm.AwaitingMoveTargetClick = false;
        _vm.MoveSourceAnnotation = null;

        if (keepPanelOpen && _notesHeader != null && _vm.CurrentAnnotation != null)
        {
            var kind = TryGetXmlCommunitySpanStrict(_vm.CurrentAnnotation, out _, out _) ? "Community" : "Note";
            var resp = GetAnnotationResp(_vm.CurrentAnnotation);
            _notesHeader.Text = string.IsNullOrWhiteSpace(resp) ? kind : $"{kind} ({resp})";
        }

        UpdateButtonsState();
    }

    private void CancelMoveModeAndHideNotes()
    {
        CancelMoveMode(keepPanelOpen: false);
        HideNotes();
    }

    // =========================
    // Click handling (marker click + move target click)
    // =========================
    private void OnPointerPressed_Tunnel(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            // Clear persistent navigation highlight on any click
            if (_navHighlightEditor != null)
            {
                try { _navHighlightEditor.TextArea.Selection = Selection.Create(_navHighlightEditor.TextArea, 0, 0); } catch { }
                _navHighlightEditor = null;
            }

            if (_vm.PendingRefresh) return;
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

            // ignore clicks inside notes panel
            if (IsInsideControl(e.Source, _notesPanel)) return;

            // close notes panel when clicking outside it (unless in move mode)
            if (_notesPanel?.IsVisible == true && !_vm.AwaitingMoveTargetClick)
            {
                CancelMoveModeAndHideNotes();
            }

            if (IsInsideScrollbarStuff(e.Source)) return;

            bool onOrig = IsInsideControl(e.Source, _editorOriginal);
            bool onTran = IsInsideControl(e.Source, _editorTranslated);
            if (!onOrig && !onTran) return;

            var te = onOrig ? _aeOrig : _aeTran;
            if (te == null) return;

            var doc = onOrig ? _vm.RenderOrig : _vm.RenderTran;
            if (doc.IsEmpty) return;

            _suppressMirrorForMarkerClickUntilUtc = DateTime.UtcNow.AddMilliseconds(SuppressMirrorAfterMarkerClickMs);
            _suppressMirrorUntilUtc = DateTime.UtcNow.AddMilliseconds(SuppressMirrorAfterMarkerClickMs);

            // MOVE MODE:
            if (_vm.AwaitingMoveTargetClick && _vm.MoveSourceAnnotation != null)
            {
                // DO NOT set e.Handled here.
                // We must allow AvaloniaEdit/TextEditor to process the click and move the caret.

                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        if (_vm.PendingRefresh) return;
                        if (_vm.MoveSourceAnnotation == null) return;

                        if (!TryGetXmlSpanLoose(_vm.MoveSourceAnnotation, out int oldXs, out int oldXe) || oldXe <= oldXs)
                        {
                            Say("Move failed: source note missing XML span.");
                            CancelMoveMode(keepPanelOpen: true);
                            return;
                        }

                        // Now caret should reflect the click location
                        int displayIndex = GetSelectionMidpointOrCaretSafe(te);
                        if (displayIndex < 0) displayIndex = 0;

                        if (!TryMapRenderedIndexToXmlIndex(doc, displayIndex, out int newXmlIndex) || newXmlIndex < 0)
                        {
                            Say("Move failed: could not map caret to XML index.");
                            CancelMoveMode(keepPanelOpen: true);
                            return;
                        }

                        var text = _vm.MoveSourceAnnotation.Text ?? "";
                        var resp = GetAnnotationResp(_vm.MoveSourceAnnotation);

                        EnterPending($"move old {oldXs}..{oldXe} -> new {newXmlIndex}");
                        FootnoteMoveRequested?.Invoke(this, new ReadableTabViewModel.MoveFootnoteRequest(
                            OldXmlStart: oldXs,
                            OldXmlEndExclusive: oldXe,
                            NewXmlIndex: newXmlIndex,
                            NoteText: text,
                            Resp: resp,
                            SourceWasTranslatedPane: _vm.CurrentAnnotationFromTranslatedPane
                        ));

                        CancelMoveModeAndHideNotes();
                    }
                    catch (Exception ex2)
                    {
                        Log("Move target click error: " + ex2.Message);
                        CancelMoveMode(keepPanelOpen: true);
                    }
                }, DispatcherPriority.Input);

                return;
            }

            // Normal note open: resolve marker near caret (Input post)
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    if (_vm.PendingRefresh) return;

                    int caret = GetCaretOffsetSafe(te);
                    if (caret < 0) return;

                    // Term click: if caret is inside a highlighted termbase range, show the term info
                    if (onOrig && TryResolveTermHitAtOffset(caret, out var termHit))
                    {
                        ShowTermInNotesPanel(termHit);
                        return;
                    }

                    if (!TryResolveAnnotationFromMarkerSpans(doc, caret, out var ann))
                        return;

                    ShowNotes(ann, fromTranslatedPane: onTran);
                    NoteClicked?.Invoke(this, ann);
                }
                catch (Exception ex2)
                {
                    Log("Marker click error: " + ex2.Message);
                }
            }, DispatcherPriority.Input);
        }
        catch (Exception ex)
        {
            Log("PointerPressed error: " + ex.Message);
        }
    }

    private static bool TryResolveAnnotationFromMarkerSpans(RenderedDocument doc, int idx, out DocAnnotation ann)
    {
        ann = default!;
        var markers = doc.AnnotationMarkers;
        var anns = doc.Annotations;

        if (markers == null || markers.Count == 0) return false;
        if (anns == null || anns.Count == 0) return false;

        const int radius = 8;

        int lo = 0, hi = markers.Count - 1, firstGreater = markers.Count;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) / 2);
            if (markers[mid].Start > idx) { firstGreater = mid; hi = mid - 1; }
            else lo = mid + 1;
        }

        int bestMarkerIndex = -1;
        int bestDist = int.MaxValue;

        int startScan = Math.Max(0, firstGreater - 6);
        int endScan = Math.Min(markers.Count - 1, firstGreater + 6);

        for (int i = startScan; i <= endScan; i++)
        {
            var m = markers[i];
            if (m.Start > idx + radius) break;
            if (m.EndExclusive < idx - radius) continue;

            int dist = idx < m.Start ? (m.Start - idx)
                     : idx > m.EndExclusive ? (idx - m.EndExclusive)
                     : 0;

            if (dist < bestDist)
            {
                bestDist = dist;
                bestMarkerIndex = i;
                if (dist == 0) break;
            }
        }

        if (bestMarkerIndex < 0 || bestDist > radius) return false;

        var best = markers[bestMarkerIndex];
        int annIndex = best.AnnotationIndex;
        if ((uint)annIndex >= (uint)anns.Count) return false;

        ann = anns[annIndex];
        return true;
    }

    // =========================
    // Selection mirroring plumbing
    // =========================
    private void HookUserInputTracking(AnnotatedTextEditor? host, bool isTranslated)
    {
        if (host == null) return;

        host.PointerPressed += (_, _) => MarkUserInput(host);
        host.PointerReleased += (_, _) => OnUserActionReleased(isTranslated, host);
        host.KeyDown += (_, _) => MarkUserInput(host);
        host.KeyUp += (_, _) => OnUserActionReleased(isTranslated, host);
    }

    private void MarkUserInput(object who)
    {
        _lastUserInputUtc = DateTime.UtcNow;
        _lastUserInputEditor = who;
    }

    private void OnUserActionReleased(bool sourceIsTranslated, object who)
    {
        MarkUserInput(who);
        _suppressPollingUntilUtc = DateTime.UtcNow.AddMilliseconds(SuppressPollingAfterUserActionMs);
        RequestMirrorFromUserAction(sourceIsTranslated);
    }

    private void StartSelectionTimer()
    {
        if (_selTimer != null) return;

        _selTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(240) };
        _selTimer.Tick += (_, _) => PollSelectionChanges();
        _selTimer.Start();
    }

    private void StopSelectionTimer()
    {
        if (_selTimer == null) return;
        _selTimer.Stop();
        _selTimer = null;
    }

    private void PollSelectionChanges()
    {
        if (_vm.PendingRefresh)
        {
            if ((DateTime.UtcNow - _vm.PendingSinceUtc).TotalMilliseconds > ReadableTabViewModel.PendingTimeoutMs)
            {
                _vm.PendingRefresh = false;
                UpdateButtonsState();
            }
            return;
        }

        if (DateTime.UtcNow <= _suppressPollingUntilUtc) return;
        if (_syncingSelection) return;
        if (DateTime.UtcNow <= _ignoreProgrammaticUntilUtc) return;
        if (DateTime.UtcNow <= _suppressMirrorUntilUtc) return;

        if (_aeOrig == null || _aeTran == null) return;
        if (_vm.RenderOrig.IsEmpty || _vm.RenderTran.IsEmpty) return;

        bool anyFocused =
            (_aeOrig.IsFocused || _aeOrig.IsKeyboardFocusWithin) ||
            (_aeTran.IsFocused || _aeTran.IsKeyboardFocusWithin);

        if (!anyFocused) return;

        int oS = GetSelectionStartSafe(_aeOrig);
        int oE = GetSelectionEndSafe(_aeOrig);
        int tS = GetSelectionStartSafe(_aeTran);
        int tE = GetSelectionEndSafe(_aeTran);
        int oC = GetCaretOffsetSafe(_aeOrig);
        int tC = GetCaretOffsetSafe(_aeTran);

        bool origSelChanged = (oS != _lastOrigSelStart) || (oE != _lastOrigSelEnd);
        bool tranSelChanged = (tS != _lastTranSelStart) || (tE != _lastTranSelEnd);
        bool origCaretChanged = (oC != _lastOrigCaret);
        bool tranCaretChanged = (tC != _lastTranCaret);

        if (!origSelChanged && !tranSelChanged && !origCaretChanged && !tranCaretChanged)
            return;

        _lastOrigSelStart = oS;
        _lastOrigSelEnd = oE;
        _lastTranSelStart = tS;
        _lastTranSelEnd = tE;
        _lastOrigCaret = oC;
        _lastTranCaret = tC;

        bool sourceIsTranslated = DetermineSourcePane(origSelChanged || origCaretChanged, tranSelChanged || tranCaretChanged);
        RequestMirrorFromUserAction(sourceIsTranslated);

        // Study panel: check if segment under caret changed
        if (_vm.StudyPanelVisible && (origCaretChanged || origSelChanged || tranCaretChanged))
            DeriveReaderSegmentContext();

        if (_vm.StudyPanelVisible)
            UpdateStudyDictionary();
    }

    private bool DetermineSourcePane(bool origChanged, bool tranChanged)
    {
        if (_aeOrig == null || _aeTran == null) return true;

        bool origFocused = _aeOrig.IsFocused || _aeOrig.IsKeyboardFocusWithin;
        bool tranFocused = _aeTran.IsFocused || _aeTran.IsKeyboardFocusWithin;
        bool recentInput = (DateTime.UtcNow - _lastUserInputUtc).TotalMilliseconds <= UserInputPriorityWindowMs;

        if (origFocused && !tranFocused) return false;
        if (tranFocused && !origFocused) return true;

        if (origChanged && !tranChanged) return false;
        if (tranChanged && !origChanged) return true;

        if (recentInput && _lastUserInputEditor != null)
        {
            if (ReferenceEquals(_lastUserInputEditor, _editorTranslated) || ReferenceEquals(_lastUserInputEditor, _aeTran))
                return true;
            if (ReferenceEquals(_lastUserInputEditor, _editorOriginal) || ReferenceEquals(_lastUserInputEditor, _aeOrig))
                return false;
        }

        if (tranFocused) return true;
        if (origFocused) return false;
        return true;
    }

    private void RequestMirrorFromUserAction(bool sourceIsTranslated)
    {
        if (_vm.PendingRefresh) return;
        if (DateTime.UtcNow <= _suppressMirrorUntilUtc) return;
        if (DateTime.UtcNow <= _suppressMirrorForMarkerClickUntilUtc) return;

        _mirrorSourceIsTranslated = sourceIsTranslated;
        if (_mirrorQueued) return;
        _mirrorQueued = true;

        Dispatcher.UIThread.Post(() =>
        {
            _mirrorQueued = false;

            if (_vm.PendingRefresh) return;
            if (_syncingSelection) return;
            if (_vm.RenderOrig.IsEmpty || _vm.RenderTran.IsEmpty) return;
            if (DateTime.UtcNow <= _suppressMirrorUntilUtc) return;
            if (DateTime.UtcNow <= _suppressMirrorForMarkerClickUntilUtc) return;

            MirrorSelectionOneWay(_mirrorSourceIsTranslated);
        }, DispatcherPriority.Background);
    }

    private void MirrorSelectionOneWay(bool sourceIsTranslated)
    {
        if (_aeOrig == null || _aeTran == null) return;
        if (_vm.RenderOrig.IsEmpty || _vm.RenderTran.IsEmpty) return;

        var srcEditor = sourceIsTranslated ? _aeTran : _aeOrig;
        var dstEditor = sourceIsTranslated ? _aeOrig : _aeTran;

        var srcDoc = sourceIsTranslated ? _vm.RenderTran : _vm.RenderOrig;
        var dstDoc = sourceIsTranslated ? _vm.RenderOrig : _vm.RenderTran;

        int caret = GetCaretOffsetSafe(srcEditor);
        if (caret < 0) caret = 0;

        if (!_selectionSync.TryGetDestinationSegment(srcDoc, dstDoc, caret, out var dstSeg))
            return;

        try
        {
            _syncingSelection = true;
            ApplyDestinationSelection(dstEditor, dstSeg.Start, dstSeg.EndExclusive, center: true);

            if (ReferenceEquals(dstEditor, _aeOrig))
            {
                _lastOrigSelStart = GetSelectionStartSafe(dstEditor);
                _lastOrigSelEnd = GetSelectionEndSafe(dstEditor);
                _lastOrigCaret = GetCaretOffsetSafe(dstEditor);
            }
            else
            {
                _lastTranSelStart = GetSelectionStartSafe(dstEditor);
                _lastTranSelEnd = GetSelectionEndSafe(dstEditor);
                _lastTranCaret = GetCaretOffsetSafe(dstEditor);
            }

            _ignoreProgrammaticUntilUtc = DateTime.UtcNow.AddMilliseconds(IgnoreProgrammaticWindowMs);
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private static void ApplyDestinationSelection(TextEditor dst, int start, int endExclusive, bool center)
    {
        int len = dst.Text?.Length ?? 0;
        start = Math.Clamp(start, 0, len);
        endExclusive = Math.Clamp(endExclusive, 0, len);
        if (endExclusive < start) (start, endExclusive) = (endExclusive, start);

        try
        {
            if (dst.TextArea != null)
            {
                dst.TextArea.Selection = Selection.Create(dst.TextArea, start, endExclusive);
                dst.TextArea.Caret.Offset = start;
            }
        }
        catch { }

        if (!center) return;

        int anchor = start + Math.Max(0, (endExclusive - start) / 2);
        CenterByCaret(dst, anchor);
    }

    // =========================
    // Hover dictionary
    // =========================
    private void SetupHoverDictionary()
    {
        _hoverDictOrig?.Dispose();
        _hoverDictOrig = null;

        if (!_vm.HoverDictionaryEnabled) return;
        if (_aeOrig == null) return;

        try { _hoverDictOrig = new HoverDictionaryBehaviorEdit(_aeOrig, _cedict, _grammar, _dictOverlayCanvas); }
        catch (Exception ex) { Log("Hover dictionary failed: " + ex.Message); }
    }

    private void DisposeHoverDictionary()
    {
        try { _hoverDictOrig?.Dispose(); } catch { }
        _hoverDictOrig = null;
    }

    // =========================
    // Zen toggle
    // =========================
    private void ChkZenText_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_suppressZenEvents) return;
            if (string.IsNullOrWhiteSpace(_vm.CurrentRelPathForZen)) return;

            bool isZen = _chkZenText?.IsChecked == true;
            ZenFlagChanged?.Invoke(this, (_vm.CurrentRelPathForZen!, isZen));
        }
        catch (Exception ex)
        {
            Log("ZEN TOGGLE EXCEPTION: " + ex.Message);
        }
    }

    // =========================
    // Mapping helpers (same mechanics as Add Note)
    // =========================
    private static bool TryMapRenderedIndexToXmlIndex(RenderedDocument doc, int displayIndex, out int xmlIndex)
    {
        xmlIndex = -1;
        if (doc == null || doc.IsEmpty) return false;
        if (doc.BaseToXmlIndex == null || doc.BaseToXmlIndex.Length == 0) return false;

        try
        {
            // RenderedDocument handles marker insertion + pos-map vs char-map
            xmlIndex = doc.DisplayIndexToXmlIndex(displayIndex);
            return xmlIndex >= 0;
        }
        catch
        {
            xmlIndex = -1;
            return false;
        }
    }

    private static int GetCaretOffsetSafe(TextEditor te)
    {
        try { return te.TextArea?.Caret.Offset ?? -1; }
        catch { return -1; }
    }

    private static int GetSelectionStartSafe(TextEditor ed)
    {
        try
        {
            var sel = ed.TextArea?.Selection;
            if (sel == null || sel.IsEmpty) return ed.TextArea?.Caret.Offset ?? 0;
            return sel.SurroundingSegment.Offset;
        }
        catch { return 0; }
    }

    private static int GetSelectionEndSafe(TextEditor ed)
    {
        try
        {
            var sel = ed.TextArea?.Selection;
            if (sel == null || sel.IsEmpty) return ed.TextArea?.Caret.Offset ?? 0;
            return sel.SurroundingSegment.Offset + sel.SurroundingSegment.Length;
        }
        catch { return 0; }
    }

    /// <summary>
    /// Extracts the lb n-value from a segment key like "lb|0001a01" or "lb|0001a01|CB".
    /// Returns null if the key is null or not an lb-type key.
    /// </summary>
    private static string? ExtractLbNValue(string? segmentKey)
        => LbHelper.ExtractLbNValue(segmentKey);

    // =========================
    // Scroll helpers
    // =========================
    private static (ScrollViewer? sv, Vector offset) GetScrollOffsetSafe(TextEditor ed)
    {
        try
        {
            var sv = ed.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
            return sv == null ? (null, default) : (sv, sv.Offset);
        }
        catch { return (null, default); }
    }

    private static void SetScrollOffsetSafe(ScrollViewer? sv, Vector offset)
    {
        if (sv == null) return;

        try
        {
            double viewportH = sv.Viewport.Height;
            double extentH = sv.Extent.Height;
            double y = offset.Y;

            if (!double.IsNaN(viewportH) && !double.IsInfinity(viewportH) && viewportH > 0 &&
                !double.IsNaN(extentH) && !double.IsInfinity(extentH) && extentH > 0)
            {
                double maxY = Math.Max(0, extentH - viewportH);
                y = Math.Clamp(y, 0, maxY);
            }
            else
            {
                y = Math.Max(0, y);
            }

            sv.Offset = new Vector(offset.X, y);
        }
        catch { }
    }

    private static void CenterByCaret(TextEditor ed, int caretOffset)
    {
        var sv = ed.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (sv == null) return;

        double viewportH = sv.Viewport.Height;
        double extentH = sv.Extent.Height;
        if (double.IsNaN(viewportH) || double.IsInfinity(viewportH) || viewportH <= 0) return;

        var textView = ed.TextArea?.TextView;
        if (textView == null) return;

        try
        {
            if (ed.TextArea != null)
                ed.TextArea.Caret.Offset = Math.Clamp(caretOffset, 0, (ed.Text ?? "").Length);
        }
        catch { }

        // Use ScrollTo first to ensure the target line's visual lines are materialized.
        // This avoids stale layout metrics from GetVisualPosition/TranslatePoint when
        // the caret is outside the currently rendered viewport (especially scrolling UP).
        var docLine = ed.Document?.GetLineByOffset(Math.Clamp(caretOffset, 0, ed.Document.TextLength));
        if (docLine == null) return;

        ed.ScrollTo(docLine.LineNumber, 0);

        // Now the target line is visible. Re-query the scroll viewer and use
        // GetVisualTopByDocumentLine for an absolute document-space Y coordinate
        // that doesn't depend on TranslatePoint viewport-relative translation.
        textView.EnsureVisualLines();

        double absoluteY;
        try
        {
            absoluteY = textView.GetVisualTopByDocumentLine(docLine.LineNumber);
        }
        catch
        {
            return; // line not yet laid out — ScrollTo already made it visible, good enough
        }

        double desiredY = absoluteY - (viewportH / 3.0); // bias toward upper third

        if (!double.IsNaN(extentH) && !double.IsInfinity(extentH) && extentH > 0)
        {
            double maxY = Math.Max(0, extentH - viewportH);
            desiredY = Math.Clamp(desiredY, 0, maxY);
        }
        else
        {
            desiredY = Math.Max(0, desiredY);
        }

        sv.Offset = new Vector(sv.Offset.X, desiredY);
    }

    // =========================
    // Click filtering helpers
    // =========================
    private static bool IsInsideScrollbarStuff(object? source)
    {
        var cur = source as StyledElement;
        while (cur != null)
        {
            if (cur is ScrollBar || cur is Thumb || cur is RepeatButton) return true;
            cur = cur.Parent as StyledElement;
        }
        return false;
    }

    private static bool IsInsideControl(object? source, Control? root)
    {
        if (root == null) return false;
        var cur = source as StyledElement;
        while (cur != null)
        {
            if (ReferenceEquals(cur, root)) return true;
            cur = cur.Parent as StyledElement;
        }
        return false;
    }

    // =========================
    // Annotation span detection (community strict vs note loose)
    // =========================
    private static bool TryGetXmlCommunitySpanStrict(DocAnnotation ann, out int xmlStart, out int xmlEndExclusive)
    {
        xmlStart = -1;
        xmlEndExclusive = -1;

        if (ann == null) return false;

        var kind = (ann.Kind ?? "").Trim();
        bool isCommunity = kind.Equals("community", StringComparison.OrdinalIgnoreCase);

        if (!isCommunity)
        {
            if (TryGetStringProp(ann, "Type", out var t) && !string.IsNullOrWhiteSpace(t) &&
                t.Trim().Equals("community", StringComparison.OrdinalIgnoreCase))
                isCommunity = true;

            if (!isCommunity &&
                TryGetStringProp(ann, "Source", out var s) && !string.IsNullOrWhiteSpace(s) &&
                s.Trim().Equals("community", StringComparison.OrdinalIgnoreCase))
                isCommunity = true;
        }

        if (!isCommunity) return false;

        bool gotStart =
            TryGetIntProp(ann, "XmlStart", out xmlStart) ||
            TryGetIntProp(ann, "XmlStartIndex", out xmlStart) ||
            TryGetIntProp(ann, "XmlFrom", out xmlStart);

        bool gotEnd =
            TryGetIntProp(ann, "XmlEndExclusive", out xmlEndExclusive) ||
            TryGetIntProp(ann, "XmlEnd", out xmlEndExclusive) ||
            TryGetIntProp(ann, "XmlTo", out xmlEndExclusive);

        if (!gotStart || !gotEnd) return false;
        if (xmlStart < 0) return false;
        if (xmlEndExclusive <= xmlStart) return false;

        const int MaxReasonableSpan = 20_000;
        if (xmlEndExclusive - xmlStart > MaxReasonableSpan) return false;

        return true;
    }

    private static bool TryGetXmlSpanLoose(DocAnnotation ann, out int xmlStart, out int xmlEndExclusive)
    {
        xmlStart = -1;
        xmlEndExclusive = -1;

        if (ann == null) return false;

        bool gotStart =
            TryGetIntProp(ann, "XmlStart", out xmlStart) ||
            TryGetIntProp(ann, "XmlStartIndex", out xmlStart) ||
            TryGetIntProp(ann, "XmlFrom", out xmlStart);

        bool gotEnd =
            TryGetIntProp(ann, "XmlEndExclusive", out xmlEndExclusive) ||
            TryGetIntProp(ann, "XmlEnd", out xmlEndExclusive) ||
            TryGetIntProp(ann, "XmlTo", out xmlEndExclusive);

        if (!gotStart || !gotEnd) return false;
        if (xmlStart < 0) return false;
        if (xmlEndExclusive <= xmlStart) return false;

        const int MaxReasonableSpan = 200_000;
        if (xmlEndExclusive - xmlStart > MaxReasonableSpan) return false;

        return true;
    }

    // =========================
    // Reflection helpers (resp + span props)
    // =========================
    private static bool TryGetIntProp(object obj, string name, out int value)
    {
        value = 0;
        try
        {
            var t = obj.GetType();

            var pi = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi != null && TryConvertNumber(pi.GetValue(obj), out value))
                return true;

            var fi = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi != null && TryConvertNumber(fi.GetValue(obj), out value))
                return true;
        }
        catch { }

        return false;
    }

    private static bool TryConvertNumber(object? raw, out int value)
    {
        value = 0;
        if (raw == null) return false;

        try
        {
            switch (raw)
            {
                case int i: value = i; return true;
                case long l: value = l > int.MaxValue ? int.MaxValue : l < int.MinValue ? int.MinValue : (int)l; return true;
                case short s: value = s; return true;
                case byte b: value = b; return true;
                case uint ui: value = ui > int.MaxValue ? int.MaxValue : (int)ui; return true;
                case ulong ul: value = ul > (ulong)int.MaxValue ? int.MaxValue : (int)ul; return true;
                case float f: value = (int)f; return true;
                case double d: value = (int)d; return true;
                case decimal m: value = (int)m; return true;
                default:
                    if (raw is IConvertible) { value = Convert.ToInt32(raw); return true; }
                    return false;
            }
        }
        catch { return false; }
    }

    private void UninstallMarkerColorizers()
    {
        try
        {
            if (_aeOrig?.TextArea?.TextView != null && _markerColorizerOrig != null)
            {
                _aeOrig.TextArea.TextView.LineTransformers.Remove(_markerColorizerOrig);
                _aeOrig.TextArea.TextView.Redraw();
            }
        }
        catch { }

        try
        {
            if (_aeTran?.TextArea?.TextView != null && _markerColorizerTran != null)
            {
                _aeTran.TextArea.TextView.LineTransformers.Remove(_markerColorizerTran);
                _aeTran.TextArea.TextView.Redraw();
            }
        }
        catch { }
    }
    private void InstallMarkerColorizers()
    {
        try
        {
            if (_aeOrig?.TextArea?.TextView != null)
            {
                _markerColorizerOrig ??= new MarkerColorizer(() =>
                    _vm.RenderOrig.AnnotationMarkers != null
                        ? (IReadOnlyList<AnnotationMarkerInserter.MarkerSpan>)_vm.RenderOrig.AnnotationMarkers
                        : Array.Empty<AnnotationMarkerInserter.MarkerSpan>());

                var list = _aeOrig.TextArea.TextView.LineTransformers;
                if (!list.Contains(_markerColorizerOrig))
                    list.Add(_markerColorizerOrig);

                _aeOrig.TextArea.TextView.Redraw();
            }
        }
        catch { }

        try
        {
            if (_aeTran?.TextArea?.TextView != null)
            {
                _markerColorizerTran ??= new MarkerColorizer(() =>
                    _vm.RenderTran.AnnotationMarkers != null
                        ? (IReadOnlyList<AnnotationMarkerInserter.MarkerSpan>)_vm.RenderTran.AnnotationMarkers
                        : Array.Empty<AnnotationMarkerInserter.MarkerSpan>());

                var list = _aeTran.TextArea.TextView.LineTransformers;
                if (!list.Contains(_markerColorizerTran))
                    list.Add(_markerColorizerTran);

                _aeTran.TextArea.TextView.Redraw();
            }
        }
        catch { }
    }

    private sealed class MarkerColorizer : DocumentColorizingTransformer
    {
        private readonly Func<IReadOnlyList<AnnotationMarkerInserter.MarkerSpan>> _getMarkers;

        public MarkerColorizer(Func<IReadOnlyList<AnnotationMarkerInserter.MarkerSpan>> getMarkers)
        {
            _getMarkers = getMarkers;
        }

        private static IBrush Brush(string key, IBrush fallback)
        {
            var app = Application.Current;
            if (app is null) return fallback;

            if (app.TryFindResource(key, theme: null, out var res) && res is IBrush b)
                return b;

            return fallback;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            var markers = _getMarkers();
            if (markers == null || markers.Count == 0) return;

            int lineStart = line.Offset;
            int lineEnd = line.EndOffset;

            // Binary search for first marker that could overlap this line
            int lo = 0, hi = markers.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (markers[mid].EndExclusive <= lineStart) lo = mid + 1;
                else hi = mid;
            }

            for (int i = lo; i < markers.Count; i++)
            {
                var m = markers[i];
                if (m.Start >= lineEnd) break;

                var fg = m.Kind switch
                {
                    AnnotationMarkerInserter.MarkerKind.Yuanwu =>
                        Brush("NoteMarkerYuanwuFg", Brushes.DarkOrange),

                    AnnotationMarkerInserter.MarkerKind.Community =>
                        Brush("NoteMarkerCommunityFg", Brushes.DodgerBlue),

                    _ =>
                        Brush("NoteMarkerNormalFg", Brushes.Gray),
                };

                int s = Math.Max(m.Start, lineStart);
                int e = Math.Min(m.EndExclusive, lineEnd);

                ChangeLinePart(s, e, el =>
                {
                    el.TextRunProperties.SetForegroundBrush(fg);
                });
            }
        }
    }




    private static string GetAnnotationLabel(DocAnnotation ann)
    {
        if (TryGetXmlCommunitySpanStrict(ann, out _, out _))
            return "Community Note";
        // Use the same classification as marker coloring
        var markerKind = AnnotationMarkerInserter.GetMarkerKind(ann);
        return markerKind switch
        {
            AnnotationMarkerInserter.MarkerKind.Yuanwu => "Footnote",
            AnnotationMarkerInserter.MarkerKind.Community => "Community Note",
            _ => "CBETA Note"
        };
    }

    private static string? GetAnnotationResp(DocAnnotation ann)
    {
        try
        {
            var pi = ann.GetType().GetProperty("Resp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi?.GetValue(ann) is string s && !string.IsNullOrWhiteSpace(s))
                return s.Trim();
        }
        catch { }

        if (TryGetStringProp(ann, "Author", out var a) && !string.IsNullOrWhiteSpace(a)) return a.Trim();
        if (TryGetStringProp(ann, "By", out var b) && !string.IsNullOrWhiteSpace(b)) return b.Trim();
        if (TryGetStringProp(ann, "Name", out var n) && !string.IsNullOrWhiteSpace(n)) return n.Trim();

        return null;
    }

    private static bool TryGetStringProp(object obj, string name, out string? value)
    {
        value = null;
        try
        {
            var pi = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi?.GetValue(obj) is string s) { value = s; return true; }
        }
        catch { }
        return false;
    }

    // =========================
    // Pending refresh gate
    // =========================
    private void EnterPending(string why)
    {
        _vm.PendingRefresh = true;
        _vm.PendingSinceUtc = DateTime.UtcNow;

        _suppressPollingUntilUtc = DateTime.UtcNow.AddMilliseconds(900);
        _ignoreProgrammaticUntilUtc = DateTime.UtcNow.AddMilliseconds(900);
        _suppressMirrorUntilUtc = DateTime.UtcNow.AddMilliseconds(900);

        if (_btnAddCommunityNote != null) _btnAddCommunityNote.IsEnabled = false;
        if (_btnDeleteCommunityNote != null) _btnDeleteCommunityNote.IsEnabled = false;
        if (_btnMoveFootnote != null) _btnMoveFootnote.IsEnabled = false;

        Log("Pending enter: " + why);
    }

    private void ExitPending(string why)
    {
        if (!_vm.PendingRefresh) return;
        _vm.PendingRefresh = false;
        UpdateButtonsState();
        Log("Pending exit: " + why);
    }

    // =========================
    // Logging
    // =========================
    private void Log(string msg)
    {
        var line = $"[ReadableTabView #{++_seq}] {msg}";
        try { Say(line); } catch { }
        try { Debug.WriteLine(line); } catch { }
    }

    // =========================
    // Termbase highlighting
    // =========================

    private TermbaseHighlightTransformer? _termHighlighter;

    /// <summary>
    /// Highlights all occurrences of recognized termbase source terms within
    /// the current segment's Chinese text in the original (left) pane.
    /// Pass null/empty to clear all highlights.
    /// </summary>
    public void UpdateTermbaseHighlights(
        IReadOnlyList<TermHit>? hits,
        string? currentZhText,
        int? preferredOccurrenceHint = null,
        string? anchorTextSignal = null)
    {
        var editor = _aeOrig;
        if (editor == null) return;

        if (_termHighlighter == null)
        {
            _termHighlighter = new TermbaseHighlightTransformer();
            editor.TextArea.TextView.LineTransformers.Add(_termHighlighter);
        }

        var ranges = new List<(int Start, int Length)>();
        var hitRanges = new List<(int Start, int Length, TermHit Hit)>();

        if (hits != null && !string.IsNullOrWhiteSpace(currentZhText))
        {
            string docText = editor.Document?.Text ?? "";
            var signalTerms = hits
                .Select(h => h.SourceTerm)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (TryFindSegmentRange(
                docText,
                currentZhText,
                signalTerms,
                tmSourceSignal: null,
                preferredOffset: editor.TextArea?.Caret?.Offset,
                preferredOccurrenceHint: preferredOccurrenceHint,
                anchorTextSignal: anchorTextSignal,
                out int zhStart,
                out int zhLength))
            {
                foreach (var hit in hits)
                {
                    if (string.IsNullOrWhiteSpace(hit.SourceTerm)) continue;
                    int countBefore = ranges.Count;
                    AddTermOccurrencesInSegment(ranges, docText, zhStart, zhLength, hit.SourceTerm);
                    for (int i = countBefore; i < ranges.Count; i++)
                        hitRanges.Add((ranges[i].Start, ranges[i].Length, hit));
                }
            }
        }

        _termHitRanges = hitRanges;
        _termHighlighter.SetRanges(ranges);
        editor.TextArea.TextView.Redraw();
    }

    private bool TryResolveTermHitAtOffset(int offset, out TermHit hit)
    {
        hit = null!;
        if (_termHitRanges == null || _termHitRanges.Count == 0) return false;

        foreach (var (start, length, h) in _termHitRanges)
        {
            if (offset >= start && offset < start + length)
            {
                hit = h;
                return true;
            }
        }
        return false;
    }

    private void ShowTermInNotesPanel(TermHit hit)
    {
        if (_notesPanel == null || _notesHeader == null || _notesBody == null) return;

        _notesHeader.Text = $"Term: {hit.SourceTerm}";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Preferred: {hit.PreferredTarget}");
        if (hit.AlternateTargets.Count > 0)
            sb.AppendLine($"Alternates: {string.Join(", ", hit.AlternateTargets)}");
        if (!string.IsNullOrWhiteSpace(hit.Status))
            sb.AppendLine($"Status: {hit.Status}");
        if (!string.IsNullOrWhiteSpace(hit.Note))
            sb.AppendLine($"Note: {hit.Note}");
        if (!string.IsNullOrWhiteSpace(hit.CreatedBy))
            sb.AppendLine($"By: {hit.CreatedBy}");

        _notesBody.Text = sb.ToString().TrimEnd();
        _vm.CanDeleteCommunityNote = false;
        _vm.CanMoveFootnote = false;
        _notesPanel.IsVisible = true;
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
        // then occurrence/proximity hints (if available), then raw start.
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

    // =========================
    // Coding Mode
    // =========================

    public bool IsCodingModeActive => _codingModeActive;

    public void SetTagVocabulary(TagVocabulary? vocab)
    {
        _tagVocabulary = vocab;

        // Clamp _codeBarPage so it never exceeds the vocabulary's max page.
        // This prevents "Page 3/1" after the user deletes pages in the editor.
        int maxPage = vocab?.Pages.Count > 0 ? vocab.Pages.Keys.Max() : 1;
        if (_codeBarPage > maxPage)
            _codeBarPage = 1;

        if (_codingModeActive)
            RefreshCodeBar();
    }

    public void SetAppliedTags(List<DocumentTag>? tags)
    {
        _appliedTags.Clear();
        if (tags != null)
            _appliedTags.AddRange(tags);
        RefreshTagHighlights();
        if (_codingModeActive)
            RefreshCodeBarStatus();
    }

    public void SetCommunityTags(Dictionary<string, List<DocumentTag>>? communityTags)
    {
        _communityTags = communityTags;
        RefreshTagUserComboBox();
    }

    public void SetCommunityVocabularies(Dictionary<string, TagVocabulary>? communityVocabs)
    {
        _communityVocabularies = communityVocabs;
    }

    private void RefreshTagUserComboBox()
    {
        if (_cmbTagUser == null) return;

        var username = DefaultResp;
        var myLabel = !string.IsNullOrWhiteSpace(username) ? $"My Tags ({username})" : "My Tags";

        var items = new List<string> { myLabel };
        if (_communityTags != null)
        {
            foreach (var user in _communityTags.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                items.Add(user);
        }

        _cmbTagUser.ItemsSource = items;

        // Preserve selection or default to "My Tags"
        if (_selectedTagUser != null && items.Contains(_selectedTagUser))
            _cmbTagUser.SelectedItem = _selectedTagUser;
        else
            _cmbTagUser.SelectedIndex = 0;
    }

    private void OnTagUserSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_cmbTagUser == null) return;

        var selected = _cmbTagUser.SelectedItem as string;
        if (selected == null || _cmbTagUser.SelectedIndex == 0)
        {
            // Show own tags
            _selectedTagUser = null;
            if (_codeBarSlots != null) _codeBarSlots.Opacity = 1.0;
            RefreshTagHighlights();
            RefreshCodeBarStatus();
            RebuildContextMenus();
        }
        else
        {
            // Show another user's tags
            _selectedTagUser = selected;
            ShowCommunityUserTags(selected);
            RebuildContextMenus();
        }
    }

    private void OnCompareTagsClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Determine the other user to compare against
        string? otherUser = _selectedTagUser;
        if (string.IsNullOrWhiteSpace(otherUser))
        {
            // If "My Tags" is selected, pick the first community user if available
            if (_communityTags != null && _communityTags.Count > 0)
                otherUser = _communityTags.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).First();
        }

        if (string.IsNullOrWhiteSpace(otherUser)) return;

        var doc = _vm.RenderOrig;
        if (doc.IsEmpty) return;

        var relPath = _vm.CurrentRelPathForZen;
        var myUsername = DefaultResp;

        // Filter my tags to current file
        var myTagsForFile = _appliedTags
            .Where(t => string.Equals(t.RelPath, relPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Get other user's tags for current file
        List<DocumentTag> otherTagsForFile = new();
        if (_communityTags != null && _communityTags.TryGetValue(otherUser, out var allOtherTags))
        {
            otherTagsForFile = allOtherTags
                .Where(t => string.Equals(t.RelPath, relPath, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Get vocabularies
        TagVocabulary? otherVocab = null;
        _communityVocabularies?.TryGetValue(otherUser, out otherVocab);

        var data = new CompareTagsRequestData(
            relPath ?? "Unknown",
            doc,
            string.IsNullOrWhiteSpace(myUsername) ? "Me" : myUsername,
            myTagsForFile,
            _tagVocabulary,
            otherUser,
            otherTagsForFile,
            otherVocab);

        CompareTagsRequested?.Invoke(this, data);
    }

    private void ShowCommunityUserTags(string username)
    {
        if (_communityTags == null || !_communityTags.TryGetValue(username, out var allUserTags))
        {
            ClearTagHighlights();
            if (_txtCodeBarStatus != null)
                _txtCodeBarStatus.Text = $"No tags from {username}";
            return;
        }

        // Filter to current file
        var relPath = _vm.CurrentRelPathForZen;
        var forFile = allUserTags
            .Where(t => string.Equals(t.RelPath, relPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Get the community user's vocabulary for colors
        TagVocabulary? userVocab = null;
        _communityVocabularies?.TryGetValue(username, out userVocab);

        // Build tag color map and apply to both editors
        var tagColorMap = BuildTagColorMap(forFile, userVocab);
        ApplyTagHighlightsToEditor(_aeOrig, _vm.RenderOrig, tagColorMap, ref _tagHighlighter);
        ApplyTagHighlightsToEditor(_aeTran, _vm.RenderTran, tagColorMap, ref _tagHighlighterTran);

        if (_txtCodeBarStatus != null)
            _txtCodeBarStatus.Text = $"Viewing {username}'s tags ({forFile.Count} on this file) \u2014 read-only";

        // Dim the code bar slots to visually signal read-only state
        if (_codeBarSlots != null)
            _codeBarSlots.Opacity = 0.35;
    }

    private void SetCodingModeActive(bool active)
    {
        _codingModeActive = active;

        if (_codeBarPanel != null)
            _codeBarPanel.IsVisible = active;
        if (_codingToggleRow != null)
            _codingToggleRow.IsVisible = !active;

        // Sync both toggle buttons
        if (_btnCodingMode != null && _btnCodingMode.IsChecked != active)
            _btnCodingMode.IsChecked = active;
        if (_btnCodingModeCompact != null && _btnCodingModeCompact.IsChecked != active)
            _btnCodingModeCompact.IsChecked = active;

        if (active)
        {
            RefreshCodeBar();
            RefreshTagHighlights();

            // Guide first-time users when no tags are configured
            bool hasSlots = _tagVocabulary?.Pages.Values.Any(p => p.Any(s => s != null)) == true;
            if (_tagVocabulary == null || _tagVocabulary.Tags.Count == 0)
            {
                if (_txtCodeBarStatus != null)
                    _txtCodeBarStatus.Text = "No tags defined. Click \"Edit Tags\" to create your tag vocabulary.";
            }
            else if (!hasSlots)
            {
                if (_txtCodeBarStatus != null)
                    _txtCodeBarStatus.Text = "Tags exist but none assigned to code bar. Click \"Edit Tags\" to assign slots.";
            }

            // Show keyboard hint once per session via main status bar
            if (!_codingHintShown)
            {
                _codingHintShown = true;
                Status?.Invoke(this, "Coding mode: W=select block, 1-9=apply tag, E/Q=expand/shrink, Tab=next untagged, hold Space+number=multi-tag");
            }
        }
        else
        {
            _spaceHeld = false;
            ClearTagHighlights();
        }

        CodingModeToggled?.Invoke(this, EventArgs.Empty);
    }

    private void OnCodingKeyDown_Tunnel(object? sender, KeyEventArgs e)
    {
        // F2 toggles coding mode regardless
        if (e.Key == Key.F2)
        {
            SetCodingModeActive(!_codingModeActive);
            e.Handled = true;
            return;
        }

        if (!_codingModeActive) return;

        // Track space held state (only in coding mode)
        if (e.Key == Key.Space)
            _spaceHeld = true;
        if (_vm.PendingRefresh) return;

        // Only handle coding keys when a text editor is focused (not buttons/textboxes)
        bool origFocused = _aeOrig != null && (_aeOrig.IsFocused || _aeOrig.IsKeyboardFocusWithin);
        bool tranFocused = _aeTran != null && (_aeTran.IsFocused || _aeTran.IsKeyboardFocusWithin);
        if (!origFocused && !tranFocused) return;

        var mods = e.KeyModifiers;

        switch (e.Key)
        {
            case Key.W:
                if (mods == KeyModifiers.None) { CodingSelectCurrentBlock(); e.Handled = true; }
                break;

            case Key.E:
                if (mods == KeyModifiers.None) { CodingExpandForward(); e.Handled = true; }
                else if (mods == KeyModifiers.Shift) { CodingExpandBackward(); e.Handled = true; }
                break;

            case Key.Q:
                if (mods == KeyModifiers.None) { CodingShrinkBackward(); e.Handled = true; }
                else if (mods == KeyModifiers.Shift) { CodingShrinkForward(); e.Handled = true; }
                break;

            case Key.Tab:
                if (mods == KeyModifiers.None) { CodingSkipToNextUntagged(); e.Handled = true; }
                break;

            case Key.N:
                if (mods == KeyModifiers.None)
                {
                    OnAddTaggedSegmentToScholar();
                    e.Handled = true;
                }
                break;

            case Key.D1: case Key.D2: case Key.D3: case Key.D4: case Key.D5:
            case Key.D6: case Key.D7: case Key.D8: case Key.D9:
                int slot = e.Key - Key.D1; // 0-8
                if (mods == KeyModifiers.Shift)
                {
                    int requestedPage = slot + 1;
                    int maxPage = _tagVocabulary?.Pages.Count > 0
                        ? _tagVocabulary.Pages.Keys.Max()
                        : 0;
                    if (maxPage <= 0)
                    {
                        if (_txtCodeBarStatus != null)
                            _txtCodeBarStatus.Text = "No pages defined yet";
                    }
                    else if (requestedPage > maxPage)
                    {
                        if (_txtCodeBarStatus != null)
                            _txtCodeBarStatus.Text = $"No page {requestedPage} (max {maxPage})";
                    }
                    else
                    {
                        _codeBarPage = requestedPage;
                        RefreshCodeBar();
                    }
                    e.Handled = true;
                }
                else if (mods == (KeyModifiers.Control | KeyModifiers.Shift))
                {
                    QuickAssignTagToSlot(slot);
                    e.Handled = true;
                }
                else if (mods == KeyModifiers.None)
                {
                    CodingApplyTag(slot);
                    e.Handled = true;
                }
                break;
        }
    }

    private void OnCodingKeyUp_Tunnel(object? sender, KeyEventArgs e)
    {
        if (_codingModeActive && _spaceHeld)
        {
            // Clear _spaceHeld on any KeyUp. This prevents a stuck state if the
            // Space KeyUp was missed (e.g. user released Space while another
            // window had focus, or while a non-editor control was focused).
            _spaceHeld = false;
        }
    }

    // --- Block selection operations ---

    private (TextEditor? editor, RenderedDocument? doc) GetActiveCodingEditorAndDoc()
    {
        bool origFocused = _aeOrig != null && (_aeOrig.IsFocused || _aeOrig.IsKeyboardFocusWithin);
        bool tranFocused = _aeTran != null && (_aeTran.IsFocused || _aeTran.IsKeyboardFocusWithin);

        if (tranFocused && !origFocused)
            return (_aeTran, _vm.RenderTran);

        return (_aeOrig, _vm.RenderOrig);
    }

    private int FindSegmentIndex(int offset, RenderedDocument? doc)
    {
        if (doc == null || doc.IsEmpty) return -1;
        var seg = doc.FindSegmentAtOrBefore(offset);
        if (seg == null) return -1;
        return doc.Segments.IndexOf(seg.Value);
    }

    private void CodingSelectCurrentBlock()
    {
        var (editor, doc) = GetActiveCodingEditorAndDoc();
        if (editor?.TextArea == null || doc == null || doc.IsEmpty)
        {
            if (_txtCodeBarStatus != null) _txtCodeBarStatus.Text = "No document loaded";
            return;
        }

        int caret = GetCaretOffsetSafe(editor);
        var seg = doc.FindSegmentAtOrBefore(caret);
        if (seg == null) return;

        int len = (editor.Text ?? "").Length;
        int s = Math.Clamp(seg.Value.Start, 0, len);
        int e = Math.Clamp(seg.Value.EndExclusive, 0, len);
        editor.TextArea.Selection = Selection.Create(editor.TextArea, s, e);
        editor.TextArea.Caret.Offset = s;

        RefreshCodeBarStatus();
    }

    private void CodingExpandForward()
    {
        var (editor, doc) = GetActiveCodingEditorAndDoc();
        if (editor?.TextArea == null || doc == null || doc.IsEmpty) return;

        int selEnd = GetSelectionEndSafe(editor);
        int selStart = GetSelectionStartSafe(editor);

        // Find the segment at or after current selection end
        int idx = FindSegmentIndex(selEnd, doc);
        if (idx < 0) return;

        // If selection end is at or past current segment end, go to next
        var curSeg = doc.Segments[idx];
        if (selEnd >= curSeg.EndExclusive && idx + 1 < doc.Segments.Count)
            idx++;

        if (idx >= doc.Segments.Count) return;

        int len = (editor.Text ?? "").Length;
        int newEnd = Math.Clamp(doc.Segments[idx].EndExclusive, 0, len);
        int s = Math.Clamp(selStart, 0, len);
        editor.TextArea.Selection = Selection.Create(editor.TextArea, s, newEnd);
        editor.TextArea.Caret.Offset = s;

        RefreshCodeBarStatus();
    }

    private void CodingShrinkBackward()
    {
        var (editor, doc) = GetActiveCodingEditorAndDoc();
        if (editor?.TextArea == null || doc == null || doc.IsEmpty) return;

        int selEnd = GetSelectionEndSafe(editor);
        int selStart = GetSelectionStartSafe(editor);

        // Find segment before current selection end
        int idx = FindSegmentIndex(Math.Max(0, selEnd - 1), doc);
        if (idx < 0) return;

        // Move end back to current segment's start
        var seg = doc.Segments[idx];
        int newEnd = seg.Start;
        if (newEnd <= selStart) return; // can't shrink below start

        int len = (editor.Text ?? "").Length;
        int s = Math.Clamp(selStart, 0, len);
        int e = Math.Clamp(newEnd, 0, len);
        if (e <= s) return;
        editor.TextArea.Selection = Selection.Create(editor.TextArea, s, e);
        editor.TextArea.Caret.Offset = s;

        RefreshCodeBarStatus();
    }

    private void CodingExpandBackward()
    {
        var (editor, doc) = GetActiveCodingEditorAndDoc();
        if (editor?.TextArea == null || doc == null || doc.IsEmpty) return;

        int selStart = GetSelectionStartSafe(editor);
        int selEnd = GetSelectionEndSafe(editor);

        // Find segment at or before selection start
        int idx = FindSegmentIndex(selStart, doc);
        if (idx < 0) return;

        // If at segment start, move to previous segment
        var curSeg = doc.Segments[idx];
        if (selStart <= curSeg.Start && idx > 0)
            idx--;

        int len = (editor.Text ?? "").Length;
        int newStart = Math.Clamp(doc.Segments[idx].Start, 0, len);
        int e = Math.Clamp(selEnd, 0, len);
        editor.TextArea.Selection = Selection.Create(editor.TextArea, newStart, e);
        editor.TextArea.Caret.Offset = newStart;

        RefreshCodeBarStatus();
    }

    private void CodingShrinkForward()
    {
        var (editor, doc) = GetActiveCodingEditorAndDoc();
        if (editor?.TextArea == null || doc == null || doc.IsEmpty) return;

        int selStart = GetSelectionStartSafe(editor);
        int selEnd = GetSelectionEndSafe(editor);

        // Find segment at selection start, move start to next segment
        int idx = FindSegmentIndex(selStart, doc);
        if (idx < 0 || idx + 1 >= doc.Segments.Count) return;

        var nextSeg = doc.Segments[idx + 1];
        int newStart = nextSeg.Start;
        if (newStart >= selEnd) return; // can't shrink past end

        int len = (editor.Text ?? "").Length;
        int s = Math.Clamp(newStart, 0, len);
        int e = Math.Clamp(selEnd, 0, len);
        if (e <= s) return;
        editor.TextArea.Selection = Selection.Create(editor.TextArea, s, e);
        editor.TextArea.Caret.Offset = s;

        RefreshCodeBarStatus();
    }

    // --- Tag application ---

    private void CodingApplyTag(int slotIndex)
    {
        if (_tagVocabulary == null)
        {
            if (_txtCodeBarStatus != null)
                _txtCodeBarStatus.Text = "No tag vocabulary loaded. Click \"Edit Tags\" to create one.";
            return;
        }
        if (_selectedTagUser != null)
        {
            if (_txtCodeBarStatus != null)
                _txtCodeBarStatus.Text = $"Viewing {_selectedTagUser}'s tags (read-only). Switch to \"My Tags\" to code.";
            return;
        }

        var tagDef = GetTagDefinitionForSlot(slotIndex);
        if (tagDef == null)
        {
            if (_txtCodeBarStatus != null) _txtCodeBarStatus.Text = $"Slot {slotIndex + 1}: empty";
            return;
        }

        var (editor, doc) = GetActiveCodingEditorAndDoc();
        if (editor?.TextArea == null || doc == null || doc.IsEmpty) return;

        int selStart = GetSelectionStartSafe(editor);
        int selEnd = GetSelectionEndSafe(editor);
        if (selEnd <= selStart)
        {
            if (_txtCodeBarStatus != null) _txtCodeBarStatus.Text = "Select a block first (W key)";
            return;
        }

        string? fromLb = LbHelper.FindNearestLbNValue(doc, selStart);
        string? toLb = LbHelper.FindNearestLbNValue(doc, Math.Max(selStart, selEnd - 1));
        if (fromLb == null)
        {
            if (_txtCodeBarStatus != null) _txtCodeBarStatus.Text = "No lb segment found";
            return;
        }

        // Snap outward to encompass partially overlapping existing tags
        (fromLb, toLb) = SnapToExistingTagBoundaries(doc, fromLb, toLb ?? fromLb);

        var tag = new DocumentTag
        {
            Id = Guid.NewGuid().ToString("N"),
            RelPath = _vm.CurrentRelPathForZen ?? "",
            FromLb = fromLb,
            ToLb = toLb ?? fromLb,
            TagId = tagDef.Id,
            CreatedUtc = DateTimeOffset.UtcNow
        };

        _appliedTags.Add(tag);
        _lastAppliedTagId = tagDef.Id;
        RefreshTagHighlights();
        RefreshCodeBarStatus();

        // Confirm application in the main status bar
        Status?.Invoke(this, $"Tagged \"{tagDef.DisplayName}\" at {fromLb}");

        TagApplied?.Invoke(this, tag);

        // Auto-advance to next untagged block unless Space is held (hold Space to multi-tag)
        if (!_spaceHeld)
        {
            Dispatcher.UIThread.Post(() => CodingSkipToNextUntagged(), DispatcherPriority.Background);
        }
    }

    /// <summary>
    /// Creates a ScholarPassage from the tag overlapping the caret and raises
    /// <see cref="AddToScholarRequested"/>. The passage is pre-populated with
    /// the tag's lb range, Chinese/English text, and the tag name.
    /// </summary>
    private void OnAddTaggedSegmentToScholar()
    {
        var (editor, doc) = GetActiveCodingEditorAndDoc();
        if (editor?.TextArea == null || doc == null || doc.IsEmpty) return;

        // Find which tag overlaps the current caret position
        int caret = GetCaretOffsetSafe(editor);
        DocumentTag? overlappingTag = null;

        foreach (var tag in _appliedTags)
        {
            if (string.IsNullOrEmpty(tag.FromLb)) continue;
            if (!TryFindSegmentByLb(doc, tag.FromLb, out var tagStart)) continue;

            int tagEnd = tagStart.EndExclusive;
            if (!string.IsNullOrEmpty(tag.ToLb) && tag.ToLb != tag.FromLb)
            {
                if (TryFindSegmentByLb(doc, tag.ToLb, out var tagEndSeg))
                    tagEnd = tagEndSeg.EndExclusive;
            }

            if (caret >= tagStart.Start && caret < tagEnd)
            {
                overlappingTag = tag;
                break;
            }
        }

        if (overlappingTag == null)
        {
            if (_txtCodeBarStatus != null)
                _txtCodeBarStatus.Text = "Place cursor inside a tagged segment first";
            return;
        }

        // Extract text from both panes using the tag's lb range
        string zhText = ExtractTextBetweenLbs(_vm.RenderOrig, overlappingTag.FromLb, overlappingTag.ToLb);
        string enText = ExtractTextBetweenLbs(_vm.RenderTran, overlappingTag.FromLb, overlappingTag.ToLb);

        // Resolve tag display name
        string tagName = "";
        if (_tagVocabulary != null)
        {
            var def = _tagVocabulary.Tags.Find(t => t.Id == overlappingTag.TagId);
            if (def != null) tagName = def.DisplayName;
        }

        var passage = new ScholarPassage
        {
            Id = Guid.NewGuid().ToString("N"),
            SourceRelPath = _vm.CurrentRelPathForZen ?? "",
            ZhText = zhText,
            EnText = enText,
            FromLb = overlappingTag.FromLb,
            ToLb = overlappingTag.ToLb,
            Tags = string.IsNullOrWhiteSpace(tagName) ? new() : new List<string> { tagName },
            AddedUtc = DateTimeOffset.UtcNow
        };

        AddToScholarRequested?.Invoke(this, passage);

        if (_txtCodeBarStatus != null)
            _txtCodeBarStatus.Text = $"Added tagged segment \"{tagName}\" to Scholar collection";
    }

    /// <summary>
    /// Adopts a community tag under the caret to the current user's own tags.
    /// Finds the overlapping tag from the selected community user, creates a
    /// copy with a new ID, and fires <see cref="TagApplied"/> so it gets persisted.
    /// If the tag definition doesn't exist in the user's vocabulary, it is added.
    /// </summary>
    private void OnAdoptCommunityTag(bool isTranslated)
    {
        if (_selectedTagUser == null || _communityTags == null) return;
        if (!_communityTags.TryGetValue(_selectedTagUser, out var allUserTags)) return;

        var editor = isTranslated ? _aeTran : _aeOrig;
        var doc = isTranslated ? _vm.RenderTran : _vm.RenderOrig;
        if (editor?.TextArea == null || doc == null || doc.IsEmpty) return;

        // Filter community tags to current file
        var relPath = _vm.CurrentRelPathForZen;
        var forFile = allUserTags
            .Where(t => string.Equals(t.RelPath, relPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (forFile.Count == 0)
        {
            Say("No community tags on this file to adopt.");
            return;
        }

        // Find which community tag overlaps the caret
        int caret = GetCaretOffsetSafe(editor);
        DocumentTag? overlapping = null;

        foreach (var tag in forFile)
        {
            if (string.IsNullOrEmpty(tag.FromLb)) continue;
            if (!TryFindSegmentByLb(doc, tag.FromLb, out var tagStart)) continue;

            int tagEnd = tagStart.EndExclusive;
            if (!string.IsNullOrEmpty(tag.ToLb) && tag.ToLb != tag.FromLb)
            {
                if (TryFindSegmentByLb(doc, tag.ToLb, out var tagEndSeg))
                    tagEnd = tagEndSeg.EndExclusive;
            }

            if (caret >= tagStart.Start && caret < tagEnd)
            {
                overlapping = tag;
                break;
            }
        }

        if (overlapping == null)
        {
            Say("Place cursor inside a highlighted tag to adopt it.");
            return;
        }

        // Resolve tag definition name from community vocabulary
        string tagName = overlapping.TagId;
        TagDefinition? communityDef = null;
        if (_communityVocabularies != null && _communityVocabularies.TryGetValue(_selectedTagUser, out var otherVocab))
        {
            communityDef = otherVocab.Tags.Find(t => t.Id == overlapping.TagId);
            if (communityDef != null) tagName = communityDef.DisplayName;
        }

        // Ensure the tag definition exists in the user's own vocabulary
        if (_tagVocabulary != null && communityDef != null)
        {
            var existing = _tagVocabulary.Tags.Find(t =>
                string.Equals(t.Name, communityDef.Name, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                // Copy the tag definition into user's vocabulary
                var newDef = new TagDefinition
                {
                    Id = communityDef.Id,
                    Name = communityDef.Name,
                    Color = communityDef.Color,
                    Description = communityDef.Description,
                    ParentId = communityDef.ParentId,
                    CreatedUtc = DateTimeOffset.UtcNow
                };
                _tagVocabulary.Tags.Add(newDef);
                VocabularyChanged?.Invoke(this, _tagVocabulary);
            }
        }

        // Check for duplicate: same file, same tag, same lb range already in user's tags
        bool alreadyExists = _appliedTags.Any(t =>
            string.Equals(t.RelPath, overlapping.RelPath, StringComparison.OrdinalIgnoreCase) &&
            t.TagId == overlapping.TagId &&
            t.FromLb == overlapping.FromLb &&
            t.ToLb == overlapping.ToLb);

        if (alreadyExists)
        {
            Say($"Tag \"{tagName}\" already exists in your tags for this range.");
            return;
        }

        // Create adopted tag with new ID
        var adopted = new DocumentTag
        {
            Id = Guid.NewGuid().ToString("N"),
            RelPath = overlapping.RelPath,
            FromLb = overlapping.FromLb,
            ToLb = overlapping.ToLb,
            TagId = overlapping.TagId,
            CreatedUtc = DateTimeOffset.UtcNow
        };

        _appliedTags.Add(adopted);
        TagApplied?.Invoke(this, adopted);

        Say($"Adopted tag \"{tagName}\" from {_selectedTagUser}.");
    }

    private void QuickAssignTagToSlot(int slotIndex)
    {
        if (_lastAppliedTagId == null || _tagVocabulary == null)
        {
            if (_txtCodeBarStatus != null)
                _txtCodeBarStatus.Text = "Apply a tag first, then Ctrl+Shift+N to assign it to slot N";
            return;
        }

        if (!_tagVocabulary.Tags.Any(t => t.Id == _lastAppliedTagId))
        {
            if (_txtCodeBarStatus != null) _txtCodeBarStatus.Text = "Last tag no longer exists";
            return;
        }

        if (!_tagVocabulary.Pages.TryGetValue(_codeBarPage, out var slots))
        {
            slots = new string?[9];
            _tagVocabulary.Pages[_codeBarPage] = slots;
        }
        if (slots.Length < 9)
        {
            var newSlots = new string?[9];
            Array.Copy(slots, newSlots, slots.Length);
            slots = newSlots;
            _tagVocabulary.Pages[_codeBarPage] = slots;
        }

        slots[slotIndex] = _lastAppliedTagId;

        var tagName = _tagVocabulary.Tags.Find(t => t.Id == _lastAppliedTagId)?.DisplayName ?? "?";
        if (_txtCodeBarStatus != null)
            _txtCodeBarStatus.Text = $"Assigned \"{tagName}\" to slot {slotIndex + 1} on page {_codeBarPage}";

        RefreshCodeBar();
        VocabularyChanged?.Invoke(this, _tagVocabulary);
    }

    private TagDefinition? GetTagDefinitionForSlot(int slotIndex)
    {
        if (_tagVocabulary == null) return null;
        if (!_tagVocabulary.Pages.TryGetValue(_codeBarPage, out var pageSlots))
            return null;
        if (slotIndex < 0 || slotIndex >= pageSlots.Length) return null;

        var tagId = pageSlots[slotIndex];
        if (string.IsNullOrEmpty(tagId)) return null;

        return _tagVocabulary.Tags.Find(t => t.Id == tagId);
    }

    /// <summary>
    /// Snap selection outward to encompass any existing tags that partially overlap.
    /// Prevents creating tags that straddle existing tag boundaries.
    /// </summary>
    private (string fromLb, string toLb) SnapToExistingTagBoundaries(
        RenderedDocument doc, string fromLb, string toLb)
    {
        if (_appliedTags.Count == 0) return (fromLb, toLb);

        // Build segment-index lookup for the selection
        if (!TryFindSegmentByLb(doc, fromLb, out var selStartSeg)) return (fromLb, toLb);
        if (!TryFindSegmentByLb(doc, toLb, out var selEndSeg)) return (fromLb, toLb);

        int selStartIdx = doc.Segments.IndexOf(selStartSeg);
        int selEndIdx = doc.Segments.IndexOf(selEndSeg);
        if (selStartIdx < 0 || selEndIdx < 0) return (fromLb, toLb);

        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var tag in _appliedTags)
            {
                if (string.IsNullOrEmpty(tag.FromLb)) continue;
                if (!TryFindSegmentByLb(doc, tag.FromLb, out var tagStartSeg)) continue;
                int tagStartIdx = doc.Segments.IndexOf(tagStartSeg);
                if (tagStartIdx < 0) continue;

                string tagToLb = !string.IsNullOrEmpty(tag.ToLb) ? tag.ToLb : tag.FromLb;
                int tagEndIdx = tagStartIdx;
                if (TryFindSegmentByLb(doc, tagToLb, out var tagEndSeg))
                {
                    int ei = doc.Segments.IndexOf(tagEndSeg);
                    if (ei >= 0) tagEndIdx = ei;
                }

                // Check overlap: does the tag range intersect the selection range?
                if (tagStartIdx <= selEndIdx && tagEndIdx >= selStartIdx)
                {
                    if (tagStartIdx < selStartIdx)
                    {
                        selStartIdx = tagStartIdx;
                        fromLb = LbHelper.ExtractLbNValue(doc.Segments[selStartIdx].Key) ?? fromLb;
                        changed = true;
                    }
                    if (tagEndIdx > selEndIdx)
                    {
                        selEndIdx = tagEndIdx;
                        toLb = LbHelper.ExtractLbNValue(doc.Segments[selEndIdx].Key) ?? toLb;
                        changed = true;
                    }
                }
            }
        }

        return (fromLb, toLb);
    }

    // --- Tab: skip to next untagged ---

    private void CodingSkipToNextUntagged()
    {
        var (editor, doc) = GetActiveCodingEditorAndDoc();
        if (editor?.TextArea == null || doc == null || doc.IsEmpty) return;

        int selEnd = GetSelectionEndSafe(editor);
        var taggedLbs = BuildTaggedLbSet(doc);

        // Scan segments starting after selection end
        for (int i = 0; i < doc.Segments.Count; i++)
        {
            var seg = doc.Segments[i];
            if (seg.Start < selEnd) continue;

            var nValue = LbHelper.ExtractLbNValue(seg.Key);
            if (nValue != null && !taggedLbs.Contains(nValue))
            {
                // Found an untagged block - select it
                int len = (editor.Text ?? "").Length;
                int s = Math.Clamp(seg.Start, 0, len);
                int e = Math.Clamp(seg.EndExclusive, 0, len);
                editor.TextArea.Selection = Selection.Create(editor.TextArea, s, e);
                editor.TextArea.Caret.Offset = s;

                // Scroll to it
                try
                {
                    var line = editor.Document.GetLineByOffset(s);
                    editor.ScrollToLine(line.LineNumber);
                }
                catch { }

                RefreshCodeBarStatus();
                return;
            }
        }

        // Wrap around from beginning
        for (int i = 0; i < doc.Segments.Count; i++)
        {
            var seg = doc.Segments[i];
            if (seg.Start >= selEnd) break; // we already scanned these

            var nValue = LbHelper.ExtractLbNValue(seg.Key);
            if (nValue != null && !taggedLbs.Contains(nValue))
            {
                int len = (editor.Text ?? "").Length;
                int s = Math.Clamp(seg.Start, 0, len);
                int e = Math.Clamp(seg.EndExclusive, 0, len);
                editor.TextArea.Selection = Selection.Create(editor.TextArea, s, e);
                editor.TextArea.Caret.Offset = s;

                try
                {
                    var line = editor.Document.GetLineByOffset(s);
                    editor.ScrollToLine(line.LineNumber);
                }
                catch { }

                RefreshCodeBarStatus();
                return;
            }
        }

        if (_txtCodeBarStatus != null) _txtCodeBarStatus.Text = "All blocks tagged!";
    }

    /// <summary>
    /// Collects all lb n-values covered by the given tags, including intermediate blocks
    /// for multi-block tag ranges.
    /// </summary>
    private HashSet<string> BuildTaggedLbSet(RenderedDocument? doc)
    {
        var tagged = new HashSet<string>(StringComparer.Ordinal);
        if (doc == null || doc.IsEmpty) return tagged;

        foreach (var t in _appliedTags)
        {
            if (string.IsNullOrEmpty(t.FromLb)) continue;

            // Find start and end segment indices for this tag range
            if (!TryFindSegmentByLb(doc, t.FromLb, out var startSeg)) continue;
            int startIdx = doc.Segments.IndexOf(startSeg);
            if (startIdx < 0) continue;

            int endIdx = startIdx;
            if (!string.IsNullOrEmpty(t.ToLb) && t.ToLb != t.FromLb)
            {
                if (TryFindSegmentByLb(doc, t.ToLb, out var endSeg))
                {
                    int ei = doc.Segments.IndexOf(endSeg);
                    if (ei >= 0) endIdx = ei;
                }
            }

            // Add all lb n-values from startIdx to endIdx inclusive
            for (int i = startIdx; i <= endIdx && i < doc.Segments.Count; i++)
            {
                var nVal = LbHelper.ExtractLbNValue(doc.Segments[i].Key);
                if (nVal != null) tagged.Add(nVal);
            }
        }

        return tagged;
    }

    // --- Code bar rendering ---

    private void RefreshCodeBar()
    {
        if (_codeBarSlots == null || _txtCodeBarPage == null) return;

        int maxPage = _tagVocabulary?.Pages.Count > 0
            ? _tagVocabulary.Pages.Keys.Max()
            : 1;

        // Clamp current page to valid range
        if (_codeBarPage > maxPage)
            _codeBarPage = 1;

        _txtCodeBarPage.Text = $"Page {_codeBarPage}/{maxPage}";

        _codeBarSlots.Children.Clear();

        for (int slot = 0; slot < 9; slot++)
        {
            var tagDef = GetTagDefinitionForSlot(slot);
            string label = tagDef != null ? $"{slot + 1}: {tagDef.DisplayName}" : $"{slot + 1}: \u2014";

            Color bgColor;
            if (tagDef != null)
            {
                try { bgColor = Color.Parse(tagDef.Color); }
                catch { bgColor = Color.FromRgb(52, 152, 219); } // default blue
            }
            else
            {
                bgColor = Color.FromArgb(40, 128, 128, 128); // dim gray for empty
            }

            var chipBg = Color.FromArgb(180, bgColor.R, bgColor.G, bgColor.B);
            // Use dark text on light backgrounds for readability
            var chipFg = (0.299 * chipBg.R + 0.587 * chipBg.G + 0.114 * chipBg.B) > 160
                ? Brushes.Black : Brushes.White;

            var chip = new Border
            {
                Background = new SolidColorBrush(chipBg),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2),
                Child = new TextBlock
                {
                    Text = label,
                    FontSize = 11,
                    Foreground = chipFg,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            _codeBarSlots.Children.Add(chip);
        }

        RefreshCodeBarStatus();
    }

    private void RefreshCodeBarStatus()
    {
        if (_txtCodeBarStatus == null) return;

        // When viewing another user's tags, don't overwrite their status line
        if (_selectedTagUser != null) return;

        var doc = _vm.RenderOrig;
        if (doc == null || doc.IsEmpty)
        {
            _txtCodeBarStatus.Text = "";
            return;
        }

        // Count total lb segments and tagged ones
        int totalBlocks = 0;
        int taggedBlocks = 0;

        var taggedLbs = BuildTaggedLbSet(doc);

        foreach (var seg in doc.Segments)
        {
            var nVal = LbHelper.ExtractLbNValue(seg.Key);
            if (nVal == null) continue;
            totalBlocks++;
            if (taggedLbs.Contains(nVal)) taggedBlocks++;
        }

        // Count orphaned tags (applied tags whose definition was deleted)
        int orphanCount = 0;
        if (_tagVocabulary != null)
        {
            var knownIds = new HashSet<string>(_tagVocabulary.Tags.Select(t => t.Id));
            orphanCount = _appliedTags.Count(t => !knownIds.Contains(t.TagId));
        }

        // Build a richer status: show progress + selection info + orphan warning
        string status = $"{taggedBlocks}/{totalBlocks} tagged";

        if (orphanCount > 0)
            status += $"  |  {orphanCount} orphaned tag(s)";

        var (activeEditor, activeDoc) = GetActiveCodingEditorAndDoc();
        if (activeEditor?.TextArea != null && activeDoc != null)
        {
            int selStart = GetSelectionStartSafe(activeEditor);
            int selEnd = GetSelectionEndSafe(activeEditor);
            if (selEnd > selStart)
            {
                var fromLb = LbHelper.FindNearestLbNValue(activeDoc, selStart);
                if (fromLb != null)
                    status += $"  |  Selected: {fromLb}";
            }
        }

        _txtCodeBarStatus.Text = status;
    }

    // --- Tag highlight rendering ---

    private void RefreshTagHighlights()
    {
        // If viewing another user's tags, don't overwrite with own
        if (_selectedTagUser != null)
        {
            ShowCommunityUserTags(_selectedTagUser);
            return;
        }

        // Compute tag color map from applied tags (lb-based, document-independent)
        var tagColorMap = BuildTagColorMap(_appliedTags, _tagVocabulary);

        // Apply to original (Chinese) editor
        ApplyTagHighlightsToEditor(_aeOrig, _vm.RenderOrig, tagColorMap, ref _tagHighlighter);

        // Apply to translated (English) editor
        ApplyTagHighlightsToEditor(_aeTran, _vm.RenderTran, tagColorMap, ref _tagHighlighterTran);
    }

    private static List<(string FromLb, string? ToLb, Color TagColor)> BuildTagColorMap(
        List<DocumentTag> tags, TagVocabulary? vocab)
    {
        var result = new List<(string, string?, Color)>();
        foreach (var tag in tags)
        {
            if (string.IsNullOrEmpty(tag.FromLb)) continue;

            Color color = Color.FromRgb(52, 152, 219); // default blue
            if (vocab != null)
            {
                var def = vocab.Tags.Find(d => d.Id == tag.TagId);
                if (def != null)
                {
                    try { color = Color.Parse(def.Color); } catch { }
                }
            }

            result.Add((tag.FromLb, tag.ToLb, color));
        }
        return result;
    }

    private void ApplyTagHighlightsToEditor(
        TextEditor? editor, RenderedDocument? doc,
        List<(string FromLb, string? ToLb, Color TagColor)> tagColorMap,
        ref TagHighlightTransformer? highlighter)
    {
        if (editor?.TextArea?.TextView == null || doc == null || doc.IsEmpty)
        {
            if (highlighter != null)
            {
                highlighter.SetRanges(Array.Empty<(int, int, Color)>());
                try { editor?.TextArea?.TextView?.Redraw(); } catch { }
            }
            return;
        }

        if (highlighter == null)
        {
            highlighter = new TagHighlightTransformer();
            editor.TextArea.TextView.LineTransformers.Add(highlighter);
        }

        var ranges = new List<(int Start, int Length, Color TagColor)>();
        foreach (var (fromLb, toLb, color) in tagColorMap)
        {
            if (!TryFindSegmentByLb(doc, fromLb, out var startSeg)) continue;
            int rangeStart = startSeg.Start;
            int rangeEnd = startSeg.EndExclusive;

            if (!string.IsNullOrEmpty(toLb) && toLb != fromLb)
            {
                if (TryFindSegmentByLb(doc, toLb, out var endSeg))
                    rangeEnd = endSeg.EndExclusive;
            }

            if (rangeEnd > rangeStart)
                ranges.Add((rangeStart, rangeEnd - rangeStart, color));
        }

        highlighter.SetRanges(ranges);
        editor.TextArea.TextView.Redraw();
    }

    private void ClearTagHighlights()
    {
        if (_tagHighlighter != null)
        {
            _tagHighlighter.SetRanges(new List<(int, int, Color)>());
            try { _aeOrig?.TextArea?.TextView?.LineTransformers.Remove(_tagHighlighter); } catch { }
            _aeOrig?.TextArea?.TextView?.Redraw();
            _tagHighlighter = null;
        }

        if (_tagHighlighterTran != null)
        {
            _tagHighlighterTran.SetRanges(new List<(int, int, Color)>());
            try { _aeTran?.TextArea?.TextView?.LineTransformers.Remove(_tagHighlighterTran); } catch { }
            _aeTran?.TextArea?.TextView?.Redraw();
            _tagHighlighterTran = null;
        }
    }

    private sealed class TagHighlightTransformer : DocumentColorizingTransformer
    {
        private List<(int Start, int Length, IBrush Brush)> _ranges = new();

        public void SetRanges(IEnumerable<(int Start, int Length, Color TagColor)> ranges)
        {
            _ranges = ranges
                .Select(r => (r.Start, r.Length,
                    (IBrush)new SolidColorBrush(Color.FromArgb(77, r.TagColor.R, r.TagColor.G, r.TagColor.B))))
                .OrderBy(r => r.Start)
                .ToList();
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            int lo = LowerBound(_ranges, line.Offset);
            for (int i = lo; i < _ranges.Count; i++)
            {
                var (start, length, brush) = _ranges[i];
                if (start >= line.Offset + line.Length) break;
                int s = Math.Max(start, line.Offset);
                int e = Math.Min(start + length, line.Offset + line.Length);
                if (s >= e) continue;
                ChangeLinePart(s, e, el =>
                    el.TextRunProperties.SetBackgroundBrush(brush));
            }
        }

        private static int LowerBound(List<(int Start, int Length, IBrush Brush)> ranges, int lineStart)
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

    private sealed class TermbaseHighlightTransformer : DocumentColorizingTransformer
    {
        private List<(int Start, int Length)> _ranges = new();
        private static readonly SolidColorBrush s_termBrush = new(Color.FromArgb(90, 255, 185, 0));

        public void SetRanges(IEnumerable<(int Start, int Length)> ranges)
        {
            _ranges = ranges.OrderBy(r => r.Start).ToList();
        }

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

    // =========================
    // Study panel
    // =========================

    private void UpdateStudyPanelVisibility()
    {
        bool visible = _chkStudyPanel?.IsChecked == true;
        _vm.StudyPanelVisible = visible;

        if (_studyPanel != null)
            _studyPanel.IsVisible = visible;
        if (_studyPanelSplitter != null)
            _studyPanelSplitter.IsVisible = visible;

        if (_readerOuterGrid != null && _readerOuterGrid.ColumnDefinitions.Count >= 3)
        {
            _readerOuterGrid.ColumnDefinitions[1].Width = visible ? new GridLength(8) : new GridLength(0);
            _readerOuterGrid.ColumnDefinitions[2].Width = visible ? new GridLength(320) : new GridLength(0);
        }

        StudyPanelVisibilityChanged?.Invoke(this, visible);

        // If just opened, derive context immediately (don't wait for poll timer)
        if (visible)
        {
            if (_vm.LastStudySnapshot != null)
                RenderStudyPanelSnapshot(_vm.LastStudySnapshot);
            else if (!_vm.RenderOrig.IsEmpty)
                DeriveReaderSegmentContext();

            UpdateStudyDictionary();
        }
    }

    /// <summary>Sets study panel visibility from config (called by host during init).</summary>
    public void SetStudyPanelVisible(bool visible)
    {
        if (_chkStudyPanel != null)
            _chkStudyPanel.IsChecked = visible;
        UpdateStudyPanelVisibility();
    }

    /// <summary>Called by host when a new study snapshot is ready.</summary>
    public void SetStudyPanelSnapshot(TranslationAssistantSnapshot? snapshot)
    {
        _vm.LastStudySnapshot = snapshot;
        if (_vm.StudyPanelVisible)
            RenderStudyPanelSnapshot(snapshot);
    }

    private void RenderStudyPanelSnapshot(TranslationAssistantSnapshot? snapshot)
    {
        ClearStudyHoverBehaviors();

        // Update segment preview
        if (_txtStudySegmentZh != null)
            _txtStudySegmentZh.Text = snapshot?.Segment?.ZhText ?? "";
        if (_txtStudySegmentEn != null)
            _txtStudySegmentEn.Text = !string.IsNullOrWhiteSpace(snapshot?.Segment?.EnText)
                ? snapshot!.Segment.EnText
                : "(no translation)";

        AssistantPanelRenderer.RenderSnapshot(
            snapshot,
            qaHost: null,
            termHost: _studyTermHost,
            approvedTmHost: _studyTmHost,
            referenceTmHost: _studyTmHost,
            brushResolver: key => GetResourceBrush(key),
            postProcessor: editor => AttachStudyHover(editor));
    }

    private void AttachStudyHover(TextEditor editor)
    {
        if (!_vm.HoverDictionaryEnabled) return;
        try
        {
            var behavior = new HoverDictionaryBehaviorEdit(editor, _cedict, _grammar, _dictOverlayCanvas);
            _studyHoverDisposables.Add(behavior);
        }
        catch { }
    }

    private void ClearStudyHoverBehaviors()
    {
        foreach (var d in _studyHoverDisposables)
        {
            try { d.Dispose(); } catch { }
        }
        _studyHoverDisposables.Clear();
    }

    private void UpdateStudyDictionary()
    {
        if (_aeOrig == null || !_vm.StudyPanelVisible) return;
        if (_txtStudyDictHeadword == null) return;

        string docText = _aeOrig.Document?.Text ?? "";
        if (string.IsNullOrEmpty(docText)) return;

        int caret = GetCaretOffsetSafe(_aeOrig);
        if (caret < 0 || caret >= docText.Length) return;

        // Check if character at caret is CJK
        char ch = docText[caret];
        if (!IsCjkChar(ch))
        {
            // Try one position back (caret might be after the character)
            if (caret > 0 && IsCjkChar(docText[caret - 1]))
                caret--;
            else
                return;
        }

        if (_cedict.TryLookupLongest(docText, caret, out var match))
        {
            string key = match.Headword + "|" + caret;
            if (key == _lastStudyDictKey) return;
            _lastStudyDictKey = key;

            _txtStudyDictHeadword.Text = match.Headword;

            // Build pinyin from all entries
            var pinyinSet = match.Entries.Select(e => e.Pinyin).Distinct().ToList();
            _txtStudyDictPinyin!.Text = string.Join(" / ", pinyinSet);

            // Build senses list
            _studyDictSenses!.Children.Clear();
            foreach (var entry in match.Entries.Take(8))
            {
                foreach (var sense in entry.Senses.Take(4))
                {
                    _studyDictSenses.Children.Add(new TextBlock
                    {
                        Text = "\u00b7 " + sense,
                        FontSize = 11,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        Opacity = 0.85
                    });
                }
            }
        }
    }

    private static bool IsCjkChar(char c)
    {
        return (c >= '\u4E00' && c <= '\u9FFF')   // CJK Unified
            || (c >= '\u3400' && c <= '\u4DBF')   // CJK Extension A
            || (c >= '\uF900' && c <= '\uFAFF');  // CJK Compat
    }

    private IBrush? GetResourceBrush(string key)
    {
        try
        {
            if (Application.Current?.TryFindResource(key, out var obj) == true && obj is IBrush brush)
                return brush;
        }
        catch { }
        return null;
    }

    private void DeriveReaderSegmentContext()
    {
        if (_aeOrig == null || _vm.RenderOrig.IsEmpty) return;

        int caret = GetCaretOffsetSafe(_aeOrig);
        if (caret < 0) caret = 0;
        var seg = _vm.RenderOrig.FindSegmentAtOrBefore(caret);

        // Fallback: if no segments, extract text around caret
        if (seg == null)
        {
            string docText = _vm.RenderOrig.Text ?? "";
            if (string.IsNullOrEmpty(docText)) return;
            int start = Math.Max(0, caret - 40);
            int end = Math.Min(docText.Length, caret + 40);
            string fallbackZh = docText[start..end];
            string fallbackKey = $"caret|{caret}";
            if (fallbackKey == _lastStudySegmentKey) return;
            _lastStudySegmentKey = fallbackKey;
            StudyPanelContextChanged?.Invoke(this, new CurrentSegmentContext
            {
                RelPath = _vm.CurrentRelPathForZen ?? "",
                ZhText = fallbackZh,
                ZhContextText = fallbackZh
            });
            return;
        }

        string segKey = seg.Value.Key;
        if (segKey == _lastStudySegmentKey) return; // still in same segment
        _lastStudySegmentKey = segKey;

        string zhText = ExtractSegmentText(_vm.RenderOrig, seg.Value);
        string enText = "";
        if (_vm.RenderTran.TryGetSegmentByKey(segKey, out var tranSeg))
            enText = ExtractSegmentText(_vm.RenderTran, tranSeg);

        // Build context: prev tail + current + next head
        var segs = _vm.RenderOrig.Segments;
        int idx = -1;
        for (int i = 0; i < segs.Count; i++)
        {
            if (segs[i].Key == segKey) { idx = i; break; }
        }
        if (idx < 0) idx = 0;

        string prevTail = idx > 0
            ? LastCharsStudy(ExtractSegmentText(_vm.RenderOrig, segs[idx - 1]), 4)
            : "";
        string nextHead = idx < segs.Count - 1
            ? FirstCharsStudy(ExtractSegmentText(_vm.RenderOrig, segs[idx + 1]), 4)
            : "";

        var ctx = new CurrentSegmentContext
        {
            RelPath = _vm.CurrentRelPathForZen ?? "",
            BlockNumber = idx,
            ZhText = zhText,
            EnText = enText,
            ZhContextText = prevTail + zhText + nextHead
        };

        StudyPanelContextChanged?.Invoke(this, ctx);
    }

    private static string ExtractSegmentText(RenderedDocument doc, RenderSegment seg)
    {
        if (doc.Text == null || doc.Text.Length < seg.EndExclusive) return "";
        return doc.Text[seg.Start..seg.EndExclusive];
    }

    private static string FirstCharsStudy(string s, int count)
    {
        if (string.IsNullOrEmpty(s) || count <= 0) return "";
        return s.Length <= count ? s : s[..count];
    }

    private static string LastCharsStudy(string s, int count)
    {
        if (string.IsNullOrEmpty(s) || count <= 0) return "";
        return s.Length <= count ? s : s[^count..];
    }
}
