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
    private ComboBox? _cmbTagUser;
    private Dictionary<string, List<DocumentTag>>? _communityTags;
    private Dictionary<string, TagVocabulary>? _communityVocabularies;
    private string? _selectedTagUser; // null = "Me" (own tags)

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

            var uri = CbetaUriParser.BuildUri(relPath, fromLb, toLb, highlight, side);
            var top = TopLevel.GetTopLevel(this);
            if (top?.Clipboard != null)
                await top.Clipboard.SetTextAsync(uri);
            Say("Link copied to clipboard.");
        };
        menu.Items.Add(copyLinkItem);

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
                return;
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

        textView.EnsureVisualLines();

        try
        {
            if (ed.TextArea != null)
                ed.TextArea.Caret.Offset = Math.Clamp(caretOffset, 0, (ed.Text ?? "").Length);
        }
        catch { }

        var caretPos = ed.TextArea?.Caret.Position;
        if (caretPos == null) return;

        var loc = textView.GetVisualPosition(caretPos.Value, VisualYPosition.LineTop);
        var p = textView.TranslatePoint(loc, sv);
        if (p == null) return;

        double caretY = p.Value.Y;

        bool looksLikeViewportCoords =
            caretY >= -viewportH * 0.25 &&
            caretY <= viewportH * 1.25;

        double desiredY = looksLikeViewportCoords
            ? sv.Offset.Y + (caretY - (viewportH / 2.0))
            : caretY - (viewportH / 2.0);

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

            for (int i = 0; i < markers.Count; i++)
            {
                var m = markers[i];
                if (m.EndExclusive <= lineStart) continue;
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
                    AddTermOccurrencesInSegment(ranges, docText, zhStart, zhLength, hit.SourceTerm);
                }
            }
        }

        _termHighlighter.SetRanges(ranges);
        editor.TextArea.TextView.Redraw();
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

        var items = new List<string> { "My Tags" };
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
        if (selected == "My Tags" || selected == null)
        {
            // Show own tags
            _selectedTagUser = null;
            if (_codeBarSlots != null) _codeBarSlots.Opacity = 1.0;
            RefreshTagHighlights();
            RefreshCodeBarStatus();
        }
        else
        {
            // Show another user's tags
            _selectedTagUser = selected;
            ShowCommunityUserTags(selected);
        }
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

        var editor = _aeOrig;
        var doc = _vm.RenderOrig;
        if (editor?.TextArea?.TextView == null || doc == null || doc.IsEmpty)
        {
            ClearTagHighlights();
            return;
        }

        if (_tagHighlighter == null)
        {
            _tagHighlighter = new TagHighlightTransformer();
            editor.TextArea.TextView.LineTransformers.Add(_tagHighlighter);
        }

        var ranges = new List<(int Start, int Length, Color TagColor)>();
        foreach (var tag in forFile)
        {
            if (string.IsNullOrEmpty(tag.FromLb)) continue;

            Color color = Color.FromRgb(52, 152, 219);
            if (userVocab != null)
            {
                var def = userVocab.Tags.Find(d => d.Id == tag.TagId);
                if (def != null)
                {
                    try { color = Color.Parse(def.Color); } catch { }
                }
            }

            if (!TryFindSegmentByLb(doc, tag.FromLb, out var startSeg)) continue;
            int rangeStart = startSeg.Start;
            int rangeEnd = startSeg.EndExclusive;

            if (!string.IsNullOrEmpty(tag.ToLb) && tag.ToLb != tag.FromLb)
            {
                if (TryFindSegmentByLb(doc, tag.ToLb, out var endSeg))
                    rangeEnd = endSeg.EndExclusive;
            }

            if (rangeEnd > rangeStart)
                ranges.Add((rangeStart, rangeEnd - rangeStart, color));
        }

        _tagHighlighter.SetRanges(ranges);
        editor.TextArea.TextView.Redraw();

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

    private int FindSegmentIndex(int offset)
    {
        var doc = _vm.RenderOrig;
        if (doc == null || doc.IsEmpty) return -1;
        var seg = doc.FindSegmentAtOrBefore(offset);
        if (seg == null) return -1;
        return doc.Segments.IndexOf(seg.Value);
    }

    private void CodingSelectCurrentBlock()
    {
        var editor = _aeOrig;
        var doc = _vm.RenderOrig;
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
        var editor = _aeOrig;
        var doc = _vm.RenderOrig;
        if (editor?.TextArea == null || doc == null || doc.IsEmpty) return;

        int selEnd = GetSelectionEndSafe(editor);
        int selStart = GetSelectionStartSafe(editor);

        // Find the segment at or after current selection end
        int idx = FindSegmentIndex(selEnd);
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
        var editor = _aeOrig;
        var doc = _vm.RenderOrig;
        if (editor?.TextArea == null || doc == null || doc.IsEmpty) return;

        int selEnd = GetSelectionEndSafe(editor);
        int selStart = GetSelectionStartSafe(editor);

        // Find segment before current selection end
        int idx = FindSegmentIndex(Math.Max(0, selEnd - 1));
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
        var editor = _aeOrig;
        var doc = _vm.RenderOrig;
        if (editor?.TextArea == null || doc == null || doc.IsEmpty) return;

        int selStart = GetSelectionStartSafe(editor);
        int selEnd = GetSelectionEndSafe(editor);

        // Find segment at or before selection start
        int idx = FindSegmentIndex(selStart);
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
        var editor = _aeOrig;
        var doc = _vm.RenderOrig;
        if (editor?.TextArea == null || doc == null || doc.IsEmpty) return;

        int selStart = GetSelectionStartSafe(editor);
        int selEnd = GetSelectionEndSafe(editor);

        // Find segment at selection start, move start to next segment
        int idx = FindSegmentIndex(selStart);
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

        var editor = _aeOrig;
        var doc = _vm.RenderOrig;
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

    // --- Tab: skip to next untagged ---

    private void CodingSkipToNextUntagged()
    {
        var editor = _aeOrig;
        var doc = _vm.RenderOrig;
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

        var editor = _aeOrig;
        if (editor?.TextArea != null)
        {
            int selStart = GetSelectionStartSafe(editor);
            int selEnd = GetSelectionEndSafe(editor);
            if (selEnd > selStart)
            {
                var fromLb = LbHelper.FindNearestLbNValue(doc, selStart);
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

        var editor = _aeOrig;
        if (editor?.TextArea?.TextView == null) return;
        var doc = _vm.RenderOrig;
        if (doc == null || doc.IsEmpty) { ClearTagHighlights(); return; }

        if (_tagHighlighter == null)
        {
            _tagHighlighter = new TagHighlightTransformer();
            editor.TextArea.TextView.LineTransformers.Add(_tagHighlighter);
        }

        var ranges = new List<(int Start, int Length, Color TagColor)>();
        foreach (var tag in _appliedTags)
        {
            if (string.IsNullOrEmpty(tag.FromLb)) continue;

            // Find the tag definition color
            Color color = Color.FromRgb(52, 152, 219); // default blue
            if (_tagVocabulary != null)
            {
                var def = _tagVocabulary.Tags.Find(d => d.Id == tag.TagId);
                if (def != null)
                {
                    try { color = Color.Parse(def.Color); } catch { }
                }
            }

            // Find rendered range for this lb range
            if (!TryFindSegmentByLb(doc, tag.FromLb, out var startSeg)) continue;
            int rangeStart = startSeg.Start;
            int rangeEnd = startSeg.EndExclusive;

            if (!string.IsNullOrEmpty(tag.ToLb) && tag.ToLb != tag.FromLb)
            {
                if (TryFindSegmentByLb(doc, tag.ToLb, out var endSeg))
                    rangeEnd = endSeg.EndExclusive;
            }

            if (rangeEnd > rangeStart)
                ranges.Add((rangeStart, rangeEnd - rangeStart, color));
        }

        _tagHighlighter.SetRanges(ranges);
        editor.TextArea.TextView.Redraw();
    }

    private void ClearTagHighlights()
    {
        if (_tagHighlighter == null) return;
        _tagHighlighter.SetRanges(new List<(int, int, Color)>());
        try
        {
            _aeOrig?.TextArea?.TextView?.LineTransformers.Remove(_tagHighlighter);
        }
        catch { }
        _aeOrig?.TextArea?.TextView?.Redraw();
        _tagHighlighter = null;
    }

    private sealed class TagHighlightTransformer : DocumentColorizingTransformer
    {
        private List<(int Start, int Length, IBrush Brush)> _ranges = new();

        public void SetRanges(IEnumerable<(int Start, int Length, Color TagColor)> ranges)
        {
            // Pre-compute brushes once when ranges change, not on every ColorizeLine call.
            _ranges = ranges
                .Select(r => (r.Start, r.Length,
                    (IBrush)new SolidColorBrush(Color.FromArgb(77, r.TagColor.R, r.TagColor.G, r.TagColor.B))))
                .ToList();
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            foreach (var (start, length, brush) in _ranges)
            {
                int s = Math.Max(start, line.Offset);
                int e = Math.Min(start + length, line.Offset + line.Length);
                if (s >= e) continue;

                ChangeLinePart(s, e, el =>
                    el.TextRunProperties.SetBackgroundBrush(brush));
            }
        }
    }

    private sealed class TermbaseHighlightTransformer : DocumentColorizingTransformer
    {
        private List<(int Start, int Length)> _ranges = new();

        public void SetRanges(IEnumerable<(int Start, int Length)> ranges)
        {
            _ranges = ranges.ToList();
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            foreach (var (start, length) in _ranges)
            {
                int s = Math.Max(start, line.Offset);
                int e = Math.Min(start + length, line.Offset + line.Length);
                if (s >= e) continue;
                ChangeLinePart(s, e, el =>
                    el.TextRunProperties.SetBackgroundBrush(
                        new SolidColorBrush(Color.FromArgb(90, 255, 185, 0))));
            }
        }
    }
}
