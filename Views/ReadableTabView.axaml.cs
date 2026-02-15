// Views/ReadableTabView.axaml.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;

namespace CbetaTranslator.App.Views;

public partial class ReadableTabView : UserControl
{
    // UI (custom controls)
    private AnnotatedTextEditor? _editorOriginal;
    private AnnotatedTextEditor? _editorTranslated;

    // Underlying AvaloniaEdit editors (fetched from inside AnnotatedTextEditor)
    private TextEditor? _aeOrig;
    private TextEditor? _aeTran;
    private ScrollViewer? _svOrig;
    private ScrollViewer? _svTran;

    // Hover dictionary (original only)
    private HoverDictionaryBehaviorEdit? _hoverDictOrig;
    private readonly ICedictDictionary _cedict = new CedictDictionaryService();

    // Selection sync
    private readonly ISelectionSyncService _selectionSync = new SelectionSyncService();

    private RenderedDocument _renderOrig = RenderedDocument.Empty;
    private RenderedDocument _renderTran = RenderedDocument.Empty;

    private bool _syncingSelection;

    private DateTime _ignoreProgrammaticUntilUtc = DateTime.MinValue;
    private const int IgnoreProgrammaticWindowMs = 180;

    private DateTime _suppressPollingUntilUtc = DateTime.MinValue;
    private const int SuppressPollingAfterUserActionMs = 220;
    private DispatcherTimer? _scrollSnapTimer;
    private const int ScrollSnapDebounceMs = 120;

    private DispatcherTimer? _selTimer;
    private int _lastOrigSelStart = -1, _lastOrigSelEnd = -1;
    private int _lastTranSelStart = -1, _lastTranSelEnd = -1;
    private int _lastOrigCaret = -1, _lastTranCaret = -1;

    private DateTime _lastUserInputUtc = DateTime.MinValue;
    private object? _lastUserInputEditor; // AnnotatedTextEditor or TextEditor
    private const int UserInputPriorityWindowMs = 250;

    // Coalesced mirroring: last request wins
    private bool _mirrorQueued;
    private bool _mirrorSourceIsTranslated;

    // -------------------------
    // Find (Ctrl+F) state
    // -------------------------
    private Border? _findBar;
    private TextBox? _findQuery;
    private TextBlock? _findCount;
    private TextBlock? _findScope;
    private Button? _btnPrev;
    private Button? _btnNext;
    private Button? _btnCloseFind;

    private SearchHighlightRenderer? _hlOrig;
    private SearchHighlightRenderer? _hlTran;

    private TextEditor? _findTarget;
    private readonly List<int> _matchStarts = new();
    private int _matchLen = 0;
    private int _matchIndex = -1;

    private static readonly TimeSpan FindRecomputeDebounce = TimeSpan.FromMilliseconds(140);
    private DispatcherTimer? _findDebounceTimer;

    // When Find scrolls, never let mirroring/polling fight it
    private DateTime _suppressMirrorUntilUtc = DateTime.MinValue;
    private const int SuppressMirrorAfterFindMs = 900;

    // -------------------------
    // Notes: bottom panel
    // -------------------------
    private Border? _notesPanel;
    private TextBlock? _notesHeader;
    private TextBox? _notesBody;
    private Button? _btnCloseNotes;

    private Button? _btnAddCommunityNote;
    private Button? _btnDeleteCommunityNote;

    private DocAnnotation? _currentAnn;

    public event EventHandler<DocAnnotation>? NoteClicked;
    public event EventHandler<(int XmlIndex, string NoteText, string? Resp)>? CommunityNoteInsertRequested;
    public event EventHandler<(int XmlStart, int XmlEndExclusive)>? CommunityNoteDeleteRequested;

    public event EventHandler<string>? Status;
    private void Say(string msg) => Status?.Invoke(this, msg);

    // -------------------------
    // HARD FIX: pending refresh gate for community note insert/delete
    // -------------------------
    private bool _pendingCommunityRefresh;
    private DateTime _pendingSinceUtc;
    private const int PendingCommunityTimeoutMs = 2500;

    private void EnterPendingCommunityRefresh(string why)
    {
        _pendingCommunityRefresh = true;
        _pendingSinceUtc = DateTime.UtcNow;

        // Freeze selection/mirroring while parent mutates XML + rebuilds render
        _suppressPollingUntilUtc = DateTime.UtcNow.AddMilliseconds(900);
        _ignoreProgrammaticUntilUtc = DateTime.UtcNow.AddMilliseconds(900);
        _suppressMirrorUntilUtc = DateTime.UtcNow.AddMilliseconds(900);

        // Disable buttons so you cannot double-delete/double-add on stale render
        if (_btnAddCommunityNote != null) _btnAddCommunityNote.IsEnabled = false;
        if (_btnDeleteCommunityNote != null) _btnDeleteCommunityNote.IsEnabled = false;

        Log($"PENDING REFRESH ENTER: {why}");
    }

    private void ExitPendingCommunityRefresh(string why)
    {
        if (!_pendingCommunityRefresh) return;
        _pendingCommunityRefresh = false;
        Log($"PENDING REFRESH EXIT: {why}");
        UpdateNotesButtonsState();
    }

    // -------------------------
    // Logging helpers
    // -------------------------
    private long _seq;

    private void Log(string msg)
    {
        var line = $"[ReadableTabView #{++_seq}] {msg}";
        try { Say(line); } catch { }
        try { Debug.WriteLine(line); } catch { }
    }

    private void DumpState(string tag)
    {
        Log($"{tag} | " +
            $"origCtrl={(_editorOriginal != null ? "OK" : "NULL")} tranCtrl={(_editorTranslated != null ? "OK" : "NULL")} | " +
            $"aeOrig={(_aeOrig != null ? "OK" : "NULL")} aeTran={(_aeTran != null ? "OK" : "NULL")} | " +
            $"renderOrigEmpty={_renderOrig.IsEmpty} renderTranEmpty={_renderTran.IsEmpty} | " +
            $"pendingRefresh={_pendingCommunityRefresh} | " +
            $"addBtn={(_btnAddCommunityNote != null ? "OK" : "NULL")} delBtn={(_btnDeleteCommunityNote != null ? "OK" : "NULL")} | " +
            $"addEnabled={(_btnAddCommunityNote?.IsEnabled.ToString() ?? "null")} delVisible={(_btnDeleteCommunityNote?.IsVisible.ToString() ?? "null")} delEnabled={(_btnDeleteCommunityNote?.IsEnabled.ToString() ?? "null")}");
    }

    public ReadableTabView()
    {
        InitializeComponent();

        // First pass: try to find stuff in constructor
        FindControls();
        WireEvents();

        AttachedToVisualTree += (_, _) =>
        {
            // IMPORTANT: re-find & rewire AFTER visual tree is alive
            FindControls();
            ResolveInnerEditors();
            ResolveInnerScrollViewers();
            RewireNotesButtonsHard();

            SetupHoverDictionary(); // MUST be on original
            EnsureScrollSnapTimer();
            StartSelectionTimer();

            Dispatcher.UIThread.Post(() =>
            {
                EnsureFindRenderersAttached();
                UpdateNotesButtonsState();
                DumpState("AttachedToVisualTree (post)");
            }, DispatcherPriority.Background);

            Log("ReadableTabView attached.");
        };

        DetachedFromVisualTree += (_, _) =>
        {
            StopSelectionTimer();
            if (_svOrig != null) _svOrig.PropertyChanged -= OnOrigScrollViewerPropertyChanged;
            _svOrig = null;
            _svTran = null;
            DisposeHoverDictionary();
            DetachFindRenderers();
            Log("ReadableTabView detached.");
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void FindControls()
    {
        _editorOriginal = this.FindControl<AnnotatedTextEditor>("EditorOriginal");
        _editorTranslated = this.FindControl<AnnotatedTextEditor>("EditorTranslated");

        _findBar = this.FindControl<Border>("FindBar");
        _findQuery = this.FindControl<TextBox>("FindQuery");
        _findCount = this.FindControl<TextBlock>("FindCount");
        _findScope = this.FindControl<TextBlock>("FindScope");
        _btnPrev = this.FindControl<Button>("BtnPrev");
        _btnNext = this.FindControl<Button>("BtnNext");
        _btnCloseFind = this.FindControl<Button>("BtnCloseFind");

        _notesPanel = this.FindControl<Border>("NotesPanel");
        _notesHeader = this.FindControl<TextBlock>("NotesHeader");
        _notesBody = this.FindControl<TextBox>("NotesBody");
        _btnCloseNotes = this.FindControl<Button>("BtnCloseNotes");

        _btnAddCommunityNote = this.FindControl<Button>("BtnAddCommunityNote");
        _btnDeleteCommunityNote = this.FindControl<Button>("BtnDeleteCommunityNote");

        if (_notesPanel != null)
            _notesPanel.IsVisible = false;

        if (_btnAddCommunityNote == null) Log("FindControls: BtnAddCommunityNote NOT FOUND (null).");
        if (_btnDeleteCommunityNote == null) Log("FindControls: BtnDeleteCommunityNote NOT FOUND (null).");
    }

    private void ResolveInnerEditors()
    {
        _aeOrig = FindInnerTextEditor(_editorOriginal);
        _aeTran = FindInnerTextEditor(_editorTranslated);

        if (_aeOrig != null) _aeOrig.IsReadOnly = true;
        if (_aeTran != null) _aeTran.IsReadOnly = true;
    }

    private void ResolveInnerScrollViewers()
    {
        var newOrig = _aeOrig?.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        var newTran = _aeTran?.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

        if (!ReferenceEquals(_svOrig, newOrig))
        {
            if (_svOrig != null) _svOrig.PropertyChanged -= OnOrigScrollViewerPropertyChanged;
            _svOrig = newOrig;
            if (_svOrig != null) _svOrig.PropertyChanged += OnOrigScrollViewerPropertyChanged;
        }

        if (ReferenceEquals(_svOrig, null))
            _svOrig = newOrig;
        _svTran = newTran;
    }

    private void OnOrigScrollViewerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != ScrollViewer.OffsetProperty)
            return;

        object who = ((object?)_aeOrig ?? (object?)_editorOriginal) ?? this;
        MarkUserInput(who);
        ScheduleSnapFromOriginalScroll();
    }

    private void EnsureScrollSnapTimer()
    {
        if (_scrollSnapTimer != null)
            return;

        _scrollSnapTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ScrollSnapDebounceMs) };
        _scrollSnapTimer.Tick += (_, _) =>
        {
            _scrollSnapTimer?.Stop();
            SnapTranslatedScrollToOriginal();
        };
    }

    private void ScheduleSnapFromOriginalScroll()
    {
        if (_pendingCommunityRefresh)
            return;

        EnsureScrollSnapTimer();
        _scrollSnapTimer?.Stop();
        _scrollSnapTimer?.Start();
    }

    private static TextEditor? FindInnerTextEditor(Control? root)
    {
        if (root == null) return null;
        if (root is TextEditor te) return te;

        // Search visual tree
        var found = root.GetVisualDescendants().OfType<TextEditor>().FirstOrDefault();
        if (found != null) return found;

        // Reflection fallback (if AnnotatedTextEditor exposes a property)
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
        // Track user input timing for mirroring + find scope
        HookUserInputTracking(_editorOriginal, isTranslated: false);
        HookUserInputTracking(_editorTranslated, isTranslated: true);

        // Ctrl+F, Escape, F3
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

        // Notes: capture clicks anywhere, decide if click was on marker
        AddHandler(
            InputElement.PointerPressedEvent,
            OnPointerPressed_TunnelForNotes,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);

        if (_findQuery != null)
        {
            _findQuery.KeyDown += FindQuery_KeyDown;
            _findQuery.PropertyChanged += (_, e) =>
            {
                if (e.Property == TextBox.TextProperty)
                    DebounceRecomputeMatches();
            };
        }

        if (_btnNext != null) _btnNext.Click += (_, _) => JumpNext();
        if (_btnPrev != null) _btnPrev.Click += (_, _) => JumpPrev();
        if (_btnCloseFind != null) _btnCloseFind.Click += (_, _) => CloseFind();

        if (_btnCloseNotes != null) _btnCloseNotes.Click += (_, _) => HideNotes();

        // Notes buttons are wired via named handlers (and re-wired after attach)
        RewireNotesButtonsHard();
    }

    private void RewireNotesButtonsHard()
    {
        if (_btnAddCommunityNote != null)
        {
            _btnAddCommunityNote.Click -= BtnAddCommunityNote_Click;
            _btnAddCommunityNote.Click += BtnAddCommunityNote_Click;

            _btnAddCommunityNote.PropertyChanged -= BtnAddCommunityNote_PropertyChanged;
            _btnAddCommunityNote.PropertyChanged += BtnAddCommunityNote_PropertyChanged;
        }

        if (_btnDeleteCommunityNote != null)
        {
            _btnDeleteCommunityNote.Click -= BtnDeleteCommunityNote_Click;
            _btnDeleteCommunityNote.Click += BtnDeleteCommunityNote_Click;

            _btnDeleteCommunityNote.PropertyChanged -= BtnDeleteCommunityNote_PropertyChanged;
            _btnDeleteCommunityNote.PropertyChanged += BtnDeleteCommunityNote_PropertyChanged;
        }

        UpdateNotesButtonsState();
    }

    private void BtnAddCommunityNote_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsEnabledProperty || e.Property == IsVisibleProperty)
            Log($"AddBtn property change: Enabled={_btnAddCommunityNote?.IsEnabled} Visible={_btnAddCommunityNote?.IsVisible}");
    }

    private void BtnDeleteCommunityNote_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsEnabledProperty || e.Property == IsVisibleProperty)
            Log($"DelBtn property change: Enabled={_btnDeleteCommunityNote?.IsEnabled} Visible={_btnDeleteCommunityNote?.IsVisible}");
    }

    private async void BtnAddCommunityNote_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_pendingCommunityRefresh)
            {
                Log("ADD CLICK ignored: pending refresh still active.");
                return;
            }

            Log("ADD CLICK fired.");
            DumpState("ADD CLICK state(before)");

            // Force-resolve inner editor at click time (prevents stale null)
            if (_aeTran == null)
            {
                Log("ADD CLICK: _aeTran was null; ResolveInnerEditors() now.");
                ResolveInnerEditors();
                DumpState("ADD CLICK state(after ResolveInnerEditors)");
            }

            var (ok, reason) = await TryAddCommunityNoteAtSelectionOrCaretAsync();
            Log(ok ? "ADD OK: " + reason : "ADD BLOCKED: " + reason);
        }
        catch (Exception ex)
        {
            Log("ADD CLICK EXCEPTION: " + ex);
        }
    }

    private void BtnDeleteCommunityNote_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_pendingCommunityRefresh)
            {
                Log("DELETE CLICK ignored: pending refresh still active.");
                return;
            }

            Log("DELETE CLICK fired.");
            DumpState("DELETE CLICK state(before)");
            DeleteCurrentCommunityNote();
            DumpState("DELETE CLICK state(after)");
        }
        catch (Exception ex)
        {
            Log("DELETE CLICK EXCEPTION: " + ex);
        }
    }

    private void HookUserInputTracking(AnnotatedTextEditor? host, bool isTranslated)
    {
        if (host == null) return;

        host.PointerPressed += (_, _) => MarkUserInput(host);
        host.PointerReleased += (_, _) => OnUserActionReleased(isTranslated, host);
        host.PointerWheelChanged += (_, _) =>
        {
            MarkUserInput(host);
            if (!isTranslated)
                ScheduleSnapFromOriginalScroll();
        };
        host.KeyDown += (_, _) => MarkUserInput(host);
        host.KeyUp += (_, _) => OnUserActionReleased(isTranslated, host);

        host.GotFocus += (_, _) =>
        {
            if (_findBar?.IsVisible == true)
                SetFindTarget(isTranslated ? _aeTran : _aeOrig, preserveIndex: true);
        };
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
        if (!sourceIsTranslated)
        {
            // ZH is the leader: on release, snap ENG scroll position to ZH by 0..1 ratio.
            SnapTranslatedScrollToOriginal();
            RequestMirrorFromUserAction(sourceIsTranslated: false);
        }
    }

    private void SnapTranslatedScrollToOriginal()
    {
        var src = _svOrig;
        var dst = _svTran;
        if (src == null || dst == null) return;

        double srcMax = Math.Max(0, src.Extent.Height - src.Viewport.Height);
        double dstMax = Math.Max(0, dst.Extent.Height - dst.Viewport.Height);
        if (srcMax <= 0 || dstMax <= 0) return;

        double ratio = src.Offset.Y / srcMax;
        if (double.IsNaN(ratio) || double.IsInfinity(ratio)) return;
        ratio = Math.Clamp(ratio, 0d, 1d);

        dst.Offset = new Vector(dst.Offset.X, ratio * dstMax);
    }

    private void SetupHoverDictionary()
    {
        if (_aeOrig == null) return;

        try
        {
            _hoverDictOrig?.Dispose();
            _hoverDictOrig = new HoverDictionaryBehaviorEdit(_aeOrig, _cedict);
        }
        catch (Exception ex)
        {
            Log("Hover dictionary failed: " + ex.Message);
        }
    }

    private void DisposeHoverDictionary()
    {
        _hoverDictOrig?.Dispose();
        _hoverDictOrig = null;
    }

    // -------------------------
    // Notes click detection
    // -------------------------
    private void OnPointerPressed_TunnelForNotes(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            if (_pendingCommunityRefresh) return;

            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                return;

            if (IsInsideControl(e.Source, _notesPanel))
                return;

            if (_notesPanel?.IsVisible == true)
                return;

            if (IsInsideScrollbarStuff(e.Source))
                return;

            bool onOrig = IsInsideControl(e.Source, _editorOriginal);
            bool onTran = IsInsideControl(e.Source, _editorTranslated);
            if (!onOrig && !onTran) return;

            var te = onOrig ? _aeOrig : _aeTran;
            if (te == null) return;

            var doc = onOrig ? _renderOrig : _renderTran;
            if (doc == null || doc.IsEmpty) return;

            int offset = GetDocumentOffsetFromPointer(te, e);

            if (offset >= 0)
            {
                if (TryResolveAnnotationFromMarkerSpans(doc, offset, out var ann))
                {
                    Log($"Marker click resolved immediately. pane={(onOrig ? "orig" : "tran")} offset={offset}");
                    ShowNotes(ann);
                    NoteClicked?.Invoke(this, ann);
                    e.Handled = true;
                }
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                int caret = GetCaretOffsetSafe(te);
                if (caret < 0) return;

                if (!TryResolveAnnotationFromMarkerSpans(doc, caret, out var ann))
                    return;

                Log($"Marker click resolved via caret fallback. pane={(onOrig ? "orig" : "tran")} caret={caret}");
                ShowNotes(ann);
                NoteClicked?.Invoke(this, ann);
            }, DispatcherPriority.Background);
        }
        catch (Exception ex)
        {
            Log("OnPointerPressed_TunnelForNotes exception: " + ex);
        }
    }

    private static int GetCaretOffsetSafe(TextEditor te)
    {
        try { return te.TextArea?.Caret.Offset ?? -1; }
        catch { return -1; }
    }

    private static int GetDocumentOffsetFromPointer(TextEditor ed, PointerEventArgs e)
    {
        try
        {
            var textView = ed.TextArea?.TextView;
            var document = ed.Document;
            if (textView == null || document == null) return -1;

            var p = e.GetPosition(textView);
            try { textView.EnsureVisualLines(); } catch { }

            object? posObj = null;
            var tvType = textView.GetType();

            var m1 = tvType.GetMethod("GetPositionFromPoint", new[] { typeof(Point), typeof(bool) });
            if (m1 != null)
                posObj = m1.Invoke(textView, new object[] { p, true });

            if (posObj == null)
            {
                var m2 = tvType.GetMethod("GetPositionFromPoint", new[] { typeof(Point) });
                if (m2 != null)
                    posObj = m2.Invoke(textView, new object[] { p });
            }

            if (posObj == null)
            {
                var m3 = tvType.GetMethod("GetPosition", new[] { typeof(Point) });
                if (m3 != null)
                    posObj = m3.Invoke(textView, new object[] { p });
            }

            if (posObj == null) return -1;

            var unwrapped = UnwrapNullable(posObj);
            if (unwrapped == null) return -1;

            int line = GetIntMember(unwrapped, "Line", fallback: -1);
            int column = GetIntMember(unwrapped, "Column", fallback: -1);

            if (line < 1 || column < 1)
            {
                var lineObj = GetMember(unwrapped, "Line");
                if (lineObj != null)
                {
                    int ln = GetIntMember(lineObj, "LineNumber", fallback: -1);
                    if (ln > 0) line = ln;
                }

                if (column < 1)
                    column = GetIntMember(unwrapped, "VisualColumn", fallback: column);
            }

            if (line < 1 || column < 1) return -1;

            int offset = document.GetOffset(line, column);
            offset = Math.Clamp(offset, 0, document.TextLength);
            return offset;
        }
        catch
        {
            return -1;
        }
    }

    private static object? UnwrapNullable(object obj)
    {
        try
        {
            var t = obj.GetType();
            var hasValue = t.GetProperty("HasValue");
            var valueProp = t.GetProperty("Value");
            if (hasValue != null && valueProp != null && hasValue.PropertyType == typeof(bool))
            {
                bool hv = (bool)(hasValue.GetValue(obj) ?? false);
                if (!hv) return null;
                return valueProp.GetValue(obj);
            }
        }
        catch { }
        return obj;
    }

    private static object? GetMember(object obj, string name)
    {
        try
        {
            var t = obj.GetType();
            var pi = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi != null) return pi.GetValue(obj);
            var fi = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi != null) return fi.GetValue(obj);
        }
        catch { }
        return null;
    }

    private static int GetIntMember(object obj, string name, int fallback)
    {
        try
        {
            var v = GetMember(obj, name);
            if (v == null) return fallback;
            if (v is int i) return i;
            if (v is long l) return (l > int.MaxValue) ? int.MaxValue : (int)l;
            if (v is IConvertible) return Convert.ToInt32(v);
        }
        catch { }
        return fallback;
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
            if (markers[mid].Start > idx)
            {
                firstGreater = mid;
                hi = mid - 1;
            }
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

            int dist;
            if (idx < m.Start) dist = m.Start - idx;
            else if (idx > m.EndExclusive) dist = idx - m.EndExclusive;
            else dist = 0;

            if (dist < bestDist)
            {
                bestDist = dist;
                bestMarkerIndex = i;
                if (dist == 0) break;
            }
        }

        if (bestMarkerIndex < 0 || bestDist > radius)
            return false;

        var best = markers[bestMarkerIndex];
        int annIndex = best.AnnotationIndex;
        if ((uint)annIndex >= (uint)anns.Count) return false;

        ann = anns[annIndex];
        return true;
    }

    private void ShowNotes(DocAnnotation ann)
    {
        if (_notesPanel == null || _notesBody == null || _notesHeader == null)
            return;

        _currentAnn = ann;

        var kind = string.IsNullOrWhiteSpace(ann.Kind) ? "Note" : ann.Kind!.Trim();
        var resp = GetAnnotationResp(ann);

        _notesHeader.Text = string.IsNullOrWhiteSpace(resp)
            ? kind
            : $"{kind} ({resp})";

        _notesBody.Text = ann.Text ?? "";
        _notesPanel.IsVisible = true;

        UpdateNotesButtonsState();
        DumpState("ShowNotes");

        try
        {
            _notesBody.SelectionStart = 0;
            _notesBody.SelectionEnd = 0;
        }
        catch { }
    }

    private void HideNotes()
    {
        if (_notesPanel == null || _notesBody == null) return;

        _notesPanel.IsVisible = false;
        _notesBody.Text = "";
        _currentAnn = null;

        UpdateNotesButtonsState();
        DumpState("HideNotes");
    }

    private void UpdateNotesButtonsState()
    {
        if (_pendingCommunityRefresh)
        {
            if (_btnAddCommunityNote != null) _btnAddCommunityNote.IsEnabled = false;
            if (_btnDeleteCommunityNote != null)
            {
                _btnDeleteCommunityNote.IsEnabled = false;
                _btnDeleteCommunityNote.IsVisible = false;
            }
            return;
        }

        if (_btnAddCommunityNote != null)
        {
            bool enabled = !_renderTran.IsEmpty && _aeTran != null;
            _btnAddCommunityNote.IsEnabled = enabled;
        }

        if (_btnDeleteCommunityNote != null)
        {
            bool canDelete = false;
            if (_currentAnn != null && IsCommunityAnnotation(_currentAnn, out var xs, out var xe) && xe > xs)
                canDelete = true;

            _btnDeleteCommunityNote.IsEnabled = canDelete;
            _btnDeleteCommunityNote.IsVisible = canDelete;
        }
    }

    private static string? GetAnnotationResp(DocAnnotation ann)
    {
        try
        {
            var pi = ann.GetType().GetProperty("Resp");
            if (pi?.GetValue(ann) is string s && !string.IsNullOrWhiteSpace(s))
                return s.Trim();
        }
        catch { }

        if (TryGetStringProp(ann, "Author", out var a) && !string.IsNullOrWhiteSpace(a)) return a.Trim();
        if (TryGetStringProp(ann, "By", out var b) && !string.IsNullOrWhiteSpace(b)) return b.Trim();
        if (TryGetStringProp(ann, "Name", out var n) && !string.IsNullOrWhiteSpace(n)) return n.Trim();

        return null;
    }

    private static bool IsCommunityAnnotation(DocAnnotation ann, out int xmlStart, out int xmlEndExclusive)
    {
        xmlStart = -1;
        xmlEndExclusive = -1;

        var kind = ann.Kind ?? "";
        var text = kind.ToLowerInvariant();
        bool looksCommunity = text.Contains("community") || text.Contains("comm");

        if (TryGetIntProp(ann, "XmlStart", out var a) || TryGetIntProp(ann, "XmlStartIndex", out a) || TryGetIntProp(ann, "XmlFrom", out a))
            xmlStart = a;

        if (TryGetIntProp(ann, "XmlEndExclusive", out var b) || TryGetIntProp(ann, "XmlEnd", out b) || TryGetIntProp(ann, "XmlTo", out b))
            xmlEndExclusive = b;

        if (TryGetStringProp(ann, "Type", out var t) && !string.IsNullOrWhiteSpace(t))
            looksCommunity = t.Trim().Equals("community", StringComparison.OrdinalIgnoreCase) || looksCommunity;

        if (TryGetStringProp(ann, "Source", out var s) && !string.IsNullOrWhiteSpace(s))
            looksCommunity = s.Trim().Equals("community", StringComparison.OrdinalIgnoreCase) || looksCommunity;

        return looksCommunity && xmlStart >= 0 && xmlEndExclusive > xmlStart;
    }

    // -------------------------
    // Community note add/delete
    // -------------------------
    public async Task AddCommunityNoteAtCaretAsync()
    {
        await AddCommunityNoteFromCaretAsync();
    }

    private async Task AddCommunityNoteFromCaretAsync()
    {
        if (_pendingCommunityRefresh)
        {
            Log("AddCommunityNoteFromCaretAsync blocked: pending refresh.");
            return;
        }

        if (_aeTran == null || _renderTran.IsEmpty)
        {
            Log("AddCommunityNoteFromCaretAsync blocked: _aeTran null or _renderTran empty.");
            return;
        }

        int caret = GetCaretOffsetSafe(_aeTran);
        if (caret < 0) caret = 0;

        if (!TryMapRenderedCaretToXmlIndex(_renderTran, caret, out int xmlIndex))
        {
            Log($"Add note blocked: cannot map caret displayIndex={caret} to XML index.");
            return;
        }

        await PromptAddCommunityNoteAsync(xmlIndex);
    }

    public async Task<(bool ok, string reason)> TryAddCommunityNoteAtSelectionOrCaretAsync()
    {
        if (_pendingCommunityRefresh)
            return (false, "Pending refresh: waiting for renderer to update after previous add/delete.");

        if (_aeTran == null)
            return (false, "_aeTran is null (inner editor not found).");

        if (_renderTran.IsEmpty)
            return (false, "_renderTran.IsEmpty (no rendered translated document set yet).");

        int renderedIndex = GetSelectionMidpointOrCaretSafe(_aeTran);
        if (renderedIndex < 0) renderedIndex = 0;

        if (!TryMapRenderedCaretToXmlIndex(_renderTran, renderedIndex, out int xmlIndex))
            return (false, $"Cannot map display index {renderedIndex} to XML index. DisplayIndexToXmlIndex returned < 0.");

        await PromptAddCommunityNoteAsync(xmlIndex);
        return (true, $"Dialog opened. renderedIndex={renderedIndex} -> xmlIndex={xmlIndex}");
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

    private void DeleteCurrentCommunityNote()
    {
        if (_pendingCommunityRefresh)
        {
            Log("Delete note blocked: pending refresh.");
            return;
        }

        if (_currentAnn == null)
        {
            Log("Delete note: no note is currently open.");
            return;
        }

        if (!IsCommunityAnnotation(_currentAnn, out int xs, out int xe))
        {
            Log("Delete note: current note is not a deletable community annotation (missing XmlStart/XmlEndExclusive or wrong type).");
            return;
        }

        // CRITICAL: gate UI NOW to prevent double-delete on stale render
        EnterPendingCommunityRefresh($"delete xs={xs} xe={xe}");

        Log($"Delete note: raising CommunityNoteDeleteRequested xs={xs} xe={xe}");
        CommunityNoteDeleteRequested?.Invoke(this, (xs, xe));

        HideNotes();
    }

    private async Task PromptAddCommunityNoteAsync(int xmlIndex)
    {
        if (_pendingCommunityRefresh)
        {
            Log("PromptAddCommunityNoteAsync blocked: pending refresh.");
            return;
        }

        Log($"PromptAddCommunityNoteAsync: opening dialog at xmlIndex={xmlIndex}");

        var owner = TopLevel.GetTopLevel(this) as Window;

        var txt = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 140
        };
        ScrollViewer.SetVerticalScrollBarVisibility(txt, ScrollBarVisibility.Auto);

        var resp = new TextBox
        {
            Watermark = "Optional resp (e.g., your initials)",
            Height = 32
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

        var panel = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 10
        };

        panel.Children.Add(new TextBlock { Text = "Community note text:" });
        panel.Children.Add(txt);
        panel.Children.Add(new TextBlock { Text = "Resp (optional):" });
        panel.Children.Add(resp);
        panel.Children.Add(btnRow);

        var win = new Window
        {
            Title = "Add community note",
            Width = 520,
            Height = 360,
            Content = panel,
            WindowStartupLocation = owner != null
                ? WindowStartupLocation.CenterOwner
                : WindowStartupLocation.CenterScreen
        };

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
            void CloseOk(bool ok)
            {
                try { win.Close(); } catch { }
                tcs.TrySetResult(ok);
            }

            btnCancel.Click += (_, _) => CloseOk(false);
            btnOk.Click += (_, _) => CloseOk(true);

            win.Closed += (_, _) =>
            {
                if (!tcs.Task.IsCompleted) tcs.TrySetResult(false);
            };

            win.Show();
            okRes = await tcs.Task;
        }

        Log("PromptAddCommunityNoteAsync: dialog closed ok=" + okRes);

        if (!okRes) return;

        var noteText = (txt.Text ?? "").Trim();
        if (noteText.Length == 0)
        {
            Log("PromptAddCommunityNoteAsync: empty note text, abort.");
            return;
        }

        var respVal = (resp.Text ?? "").Trim();
        if (respVal.Length == 0) respVal = null;

        // CRITICAL: gate UI NOW to prevent double-insert on stale render
        EnterPendingCommunityRefresh($"insert xmlIndex={xmlIndex}");

        Log($"PromptAddCommunityNoteAsync: raising CommunityNoteInsertRequested xmlIndex={xmlIndex} resp={(respVal ?? "(null)")} textLen={noteText.Length}");
        CommunityNoteInsertRequested?.Invoke(this, (xmlIndex, noteText, respVal));
    }

    private static bool TryMapRenderedCaretToXmlIndex(RenderedDocument doc, int displayIndex, out int xmlIndex)
    {
        xmlIndex = -1;

        if (doc == null || doc.IsEmpty)
            return false;

        int mapped = doc.DisplayIndexToXmlIndex(displayIndex);
        if (mapped < 0) return false;

        xmlIndex = mapped;
        return true;
    }

    // -------------------------
    // Public API
    // -------------------------
    public void Clear()
    {
        _renderOrig = RenderedDocument.Empty;
        _renderTran = RenderedDocument.Empty;

        if (_aeOrig != null) _aeOrig.Text = "";
        if (_aeTran != null) _aeTran.Text = "";

        _lastOrigSelStart = _lastOrigSelEnd = -1;
        _lastTranSelStart = _lastTranSelEnd = -1;
        _lastOrigCaret = -1;
        _lastTranCaret = -1;

        HideNotes();

        ClearFindState();
        CloseFind();

        _pendingCommunityRefresh = false;
        UpdateNotesButtonsState();
        DumpState("Clear()");
    }

    public void SetRendered(RenderedDocument orig, RenderedDocument tran)
    {
        _renderOrig = orig ?? RenderedDocument.Empty;
        _renderTran = tran ?? RenderedDocument.Empty;

        // Visual tree may not be stable yet; re-find + resolve here too
        FindControls();
        ResolveInnerEditors();
        ResolveInnerScrollViewers();
        RewireNotesButtonsHard();

        if (_aeOrig != null) _aeOrig.Text = _renderOrig.Text ?? "";
        if (_aeTran != null) _aeTran.Text = _renderTran.Text ?? "";

        SetupHoverDictionary();

        HideNotes();

        // This is the ONLY reliable signal that the UI is now up-to-date after add/delete.
        ExitPendingCommunityRefresh("SetRendered received new render");

        DumpState("SetRendered()");

        if (_findBar?.IsVisible == true)
            RecomputeMatches(resetToFirst: false);
    }

    // -------------------------
    // Polling + mirroring  (DO NOT TOUCH BEHAVIOR)
    // -------------------------
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
        // HARD FIX: while waiting for re-render after add/delete, do nothing.
        if (_pendingCommunityRefresh)
        {
            if ((DateTime.UtcNow - _pendingSinceUtc).TotalMilliseconds > PendingCommunityTimeoutMs)
            {
                // Safety: don't deadlock UI forever if parent forgot to call SetRendered.
                _pendingCommunityRefresh = false;
                Log("PENDING REFRESH TIMEOUT: releasing UI (no SetRendered received).");
                UpdateNotesButtonsState();
            }
            return;
        }

        if (DateTime.UtcNow <= _suppressPollingUntilUtc) return;
        if (_syncingSelection) return;
        if (DateTime.UtcNow <= _ignoreProgrammaticUntilUtc) return;
        if (DateTime.UtcNow <= _suppressMirrorUntilUtc) return;

        if (_aeOrig == null || _aeTran == null) return;
        if (_renderOrig.IsEmpty || _renderTran.IsEmpty) return;

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
        if (!sourceIsTranslated)
            RequestMirrorFromUserAction(sourceIsTranslated: false);
    }

    private bool DetermineSourcePane(bool origChanged, bool tranChanged)
    {
        if (_aeOrig == null || _aeTran == null)
            return true;

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

    private void MirrorSelectionOneWay(bool sourceIsTranslated)
    {
        if (_aeOrig == null || _aeTran == null) return;
        if (_renderOrig.IsEmpty || _renderTran.IsEmpty) return;

        var srcEditor = sourceIsTranslated ? _aeTran : _aeOrig;
        var dstEditor = sourceIsTranslated ? _aeOrig : _aeTran;

        var srcDoc = sourceIsTranslated ? _renderTran : _renderOrig;
        var dstDoc = sourceIsTranslated ? _renderOrig : _renderTran;

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

    private void ApplyDestinationSelection(TextEditor dst, int start, int endExclusive, bool center)
    {
        int len = dst.Text?.Length ?? 0;
        start = Math.Clamp(start, 0, Math.Max(0, len));
        endExclusive = Math.Clamp(endExclusive, 0, Math.Max(0, len));
        if (endExclusive < start) (start, endExclusive) = (endExclusive, start);

        try
        {
            dst.TextArea.Selection = Selection.Create(dst.TextArea, start, endExclusive);
            dst.TextArea.Caret.Offset = start;
        }
        catch
        {
            // ignore
        }

        if (!center) return;

        int anchor = start + Math.Max(0, (endExclusive - start) / 2);
        CenterByCaret(dst, anchor);
    }

    private void RequestMirrorFromUserAction(bool sourceIsTranslated)
    {
        if (_pendingCommunityRefresh) return;
        if (DateTime.UtcNow <= _suppressMirrorUntilUtc) return;

        _mirrorSourceIsTranslated = sourceIsTranslated;
        if (_mirrorQueued) return;
        _mirrorQueued = true;

        Dispatcher.UIThread.Post(() =>
        {
            _mirrorQueued = false;

            if (_pendingCommunityRefresh) return;
            if (_syncingSelection) return;
            if (_renderOrig.IsEmpty || _renderTran.IsEmpty) return;
            if (DateTime.UtcNow <= _suppressMirrorUntilUtc) return;

            MirrorSelectionOneWay(_mirrorSourceIsTranslated);
        }, DispatcherPriority.Background);
    }

    // -------------------------
    // Ctrl+F Find UI
    // -------------------------
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            OpenFind();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && _findBar?.IsVisible == true)
        {
            CloseFind();
            e.Handled = true;
            return;
        }

        if (_findBar?.IsVisible == true && e.Key == Key.F3)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) JumpPrev();
            else JumpNext();
            e.Handled = true;
            return;
        }
    }

    private void FindQuery_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_findBar?.IsVisible != true) return;

        if (e.Key == Key.Enter)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) JumpPrev();
            else JumpNext();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            CloseFind();
            e.Handled = true;
            return;
        }
    }

    private void OpenFind()
    {
        if (_findBar == null || _findQuery == null) return;

        ResolveInnerEditors();
        EnsureFindRenderersAttached();

        _findBar.IsVisible = true;

        var target = DetermineCurrentPaneForFind();
        SetFindTarget(target, preserveIndex: false);

        _findQuery.Focus();
        _findQuery.SelectionStart = 0;
        _findQuery.SelectionEnd = (_findQuery.Text ?? "").Length;

        RecomputeMatches(resetToFirst: false);
    }

    private void CloseFind()
    {
        if (_findBar != null)
            _findBar.IsVisible = false;

        ClearHighlight();

        try { _findTarget?.Focus(); } catch { }
    }

    private TextEditor? DetermineCurrentPaneForFind()
    {
        if (_aeOrig == null || _aeTran == null)
            return _aeTran;

        bool recentInput = (DateTime.UtcNow - _lastUserInputUtc).TotalMilliseconds <= UserInputPriorityWindowMs;
        if (recentInput && _lastUserInputEditor != null)
        {
            if (ReferenceEquals(_lastUserInputEditor, _editorOriginal) || ReferenceEquals(_lastUserInputEditor, _aeOrig)) return _aeOrig;
            if (ReferenceEquals(_lastUserInputEditor, _editorTranslated) || ReferenceEquals(_lastUserInputEditor, _aeTran)) return _aeTran;
        }

        if (_aeTran.IsFocused || _aeTran.IsKeyboardFocusWithin) return _aeTran;
        if (_aeOrig.IsFocused || _aeOrig.IsKeyboardFocusWithin) return _aeOrig;

        return _aeTran;
    }

    private void SetFindTarget(TextEditor? ed, bool preserveIndex)
    {
        if (ed == null) return;

        _findTarget = ed;

        if (_findScope != null)
            _findScope.Text = ReferenceEquals(ed, _aeOrig) ? "Find (Original):" : "Find (Translated):";

        RecomputeMatches(resetToFirst: !preserveIndex);
    }

    private void DebounceRecomputeMatches()
    {
        _findDebounceTimer ??= new DispatcherTimer { Interval = FindRecomputeDebounce };
        _findDebounceTimer.Stop();
        _findDebounceTimer.Tick -= FindDebounceTimer_Tick;
        _findDebounceTimer.Tick += FindDebounceTimer_Tick;
        _findDebounceTimer.Start();
    }

    private void FindDebounceTimer_Tick(object? sender, EventArgs e)
    {
        _findDebounceTimer?.Stop();
        RecomputeMatches(resetToFirst: true);
    }

    private void RecomputeMatches(bool resetToFirst)
    {
        if (_findBar?.IsVisible != true) return;

        var ed = _findTarget;
        if (ed == null) return;

        string hay = ed.Text ?? "";
        string q = (_findQuery?.Text ?? "").Trim();

        int oldSelectedStart = -1;
        if (!resetToFirst && _matchIndex >= 0 && _matchIndex < _matchStarts.Count)
            oldSelectedStart = _matchStarts[_matchIndex];

        _matchStarts.Clear();
        _matchLen = 0;
        _matchIndex = -1;

        if (q.Length == 0 || hay.Length == 0)
        {
            UpdateFindCount();
            ClearHighlight();
            return;
        }

        _matchLen = q.Length;

        int idx = 0;
        while (true)
        {
            idx = hay.IndexOf(q, idx, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) break;
            _matchStarts.Add(idx);
            idx = idx + Math.Max(1, q.Length);
        }

        if (_matchStarts.Count == 0)
        {
            UpdateFindCount();
            ClearHighlight();
            return;
        }

        if (resetToFirst)
        {
            int caret = GetCaretOffsetSafe(ed);
            int nearest = _matchStarts.FindIndex(s => s >= caret);
            _matchIndex = nearest >= 0 ? nearest : 0;
        }
        else
        {
            if (oldSelectedStart >= 0)
            {
                int exact = _matchStarts.IndexOf(oldSelectedStart);
                if (exact >= 0) _matchIndex = exact;
                else
                {
                    int nearest = _matchStarts.FindIndex(s => s >= oldSelectedStart);
                    _matchIndex = nearest >= 0 ? nearest : _matchStarts.Count - 1;
                }
            }
            else _matchIndex = 0;
        }

        UpdateFindCount();
        JumpToCurrentMatch(scroll: false);
    }

    private void UpdateFindCount()
    {
        if (_findCount == null) return;

        if (_matchStarts.Count == 0 || _matchIndex < 0)
            _findCount.Text = "0/0";
        else
            _findCount.Text = $"{_matchIndex + 1}/{_matchStarts.Count}";
    }

    private void JumpNext()
    {
        if (_matchStarts.Count == 0) return;
        _matchIndex = (_matchIndex + 1) % _matchStarts.Count;
        UpdateFindCount();
        JumpToCurrentMatch(scroll: true);
    }

    private void JumpPrev()
    {
        if (_matchStarts.Count == 0) return;
        _matchIndex = (_matchIndex - 1 + _matchStarts.Count) % _matchStarts.Count;
        UpdateFindCount();
        JumpToCurrentMatch(scroll: true);
    }

    private void JumpToCurrentMatch(bool scroll)
    {
        if (_findTarget == null) return;
        if (_matchIndex < 0 || _matchIndex >= _matchStarts.Count) return;

        int start = _matchStarts[_matchIndex];
        int len = _matchLen;

        ApplyHighlight(_findTarget, start, len);

        if (!scroll) return;

        try
        {
            _suppressPollingUntilUtc = DateTime.UtcNow.AddMilliseconds(420);
            _ignoreProgrammaticUntilUtc = DateTime.UtcNow.AddMilliseconds(420);
            _suppressMirrorUntilUtc = DateTime.UtcNow.AddMilliseconds(SuppressMirrorAfterFindMs);

            _findTarget.Focus();
            _findTarget.TextArea.Caret.Offset = Math.Clamp(start, 0, (_findTarget.Text ?? "").Length);

            DispatcherTimer.RunOnce(() =>
            {
                try { CenterByCaret(_findTarget, start); } catch { }
                ApplyHighlight(_findTarget, start, len);
            }, TimeSpan.FromMilliseconds(25));

            DispatcherTimer.RunOnce(() =>
            {
                try { CenterByCaret(_findTarget, start); } catch { }
                ApplyHighlight(_findTarget, start, len);
            }, TimeSpan.FromMilliseconds(85));
        }
        catch { }
    }

    private void EnsureFindRenderersAttached()
    {
        AttachRendererIfMissing(_aeOrig, ref _hlOrig);
        AttachRendererIfMissing(_aeTran, ref _hlTran);
    }

    private static void AttachRendererIfMissing(TextEditor? ed, ref SearchHighlightRenderer? renderer)
    {
        if (ed == null) return;

        var tv = ed.TextArea?.TextView;
        if (tv == null) return;

        renderer ??= new SearchHighlightRenderer(tv);

        if (!tv.BackgroundRenderers.Contains(renderer))
            tv.BackgroundRenderers.Add(renderer);
    }

    private void DetachFindRenderers()
    {
        DetachRenderer(_aeOrig, ref _hlOrig);
        DetachRenderer(_aeTran, ref _hlTran);
    }

    private static void DetachRenderer(TextEditor? ed, ref SearchHighlightRenderer? renderer)
    {
        if (ed == null || renderer == null) return;

        var tv = ed.TextArea?.TextView;
        if (tv != null && tv.BackgroundRenderers.Contains(renderer))
            tv.BackgroundRenderers.Remove(renderer);

        renderer = null;
    }

    private void ApplyHighlight(TextEditor target, int start, int len)
    {
        EnsureFindRenderersAttached();

        if (ReferenceEquals(target, _aeOrig))
        {
            _hlTran?.Clear();
            _hlOrig?.SetRange(start, len);
        }
        else
        {
            _hlOrig?.Clear();
            _hlTran?.SetRange(start, len);
        }

        try { target.TextArea?.TextView?.InvalidateVisual(); } catch { }
    }

    private void ClearHighlight()
    {
        try { _hlOrig?.Clear(); } catch { }
        try { _hlTran?.Clear(); } catch { }
        try
        {
            _aeOrig?.TextArea?.TextView?.InvalidateVisual();
            _aeTran?.TextArea?.TextView?.InvalidateVisual();
        }
        catch { }
    }

    private void ClearFindState()
    {
        _matchStarts.Clear();
        _matchLen = 0;
        _matchIndex = -1;
        UpdateFindCount();
        ClearHighlight();
    }

    // -------------------------
    // Selection helpers (AvaloniaEdit)
    // -------------------------
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

    // -------------------------
    // Scroll helper (AvaloniaEdit)
    // -------------------------
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

        try { ed.TextArea.Caret.Offset = Math.Clamp(caretOffset, 0, (ed.Text ?? "").Length); } catch { }

        var caretPos = ed.TextArea.Caret.Position;

        var loc = textView.GetVisualPosition(caretPos, VisualYPosition.LineTop);
        var p = textView.TranslatePoint(loc, sv);
        if (p == null) return;

        double caretY = p.Value.Y;

        bool looksLikeViewportCoords =
            caretY >= -viewportH * 0.25 &&
            caretY <= viewportH * 1.25;

        double desiredY;
        if (looksLikeViewportCoords)
            desiredY = sv.Offset.Y + (caretY - (viewportH / 2.0));
        else
            desiredY = caretY - (viewportH / 2.0);

        if (!double.IsNaN(extentH) && !double.IsInfinity(extentH) && extentH > 0)
        {
            double maxY = Math.Max(0, extentH - viewportH);
            desiredY = Math.Max(0, Math.Min(desiredY, maxY));
        }
        else desiredY = Math.Max(0, desiredY);

        sv.Offset = new Vector(sv.Offset.X, desiredY);
    }

    // -------------------------
    // Utility: visual ancestry checks
    // -------------------------
    private static bool IsInsideScrollbarStuff(object? source)
    {
        var cur = source as StyledElement;
        while (cur != null)
        {
            if (cur is ScrollBar || cur is Thumb || cur is RepeatButton)
                return true;

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
            if (ReferenceEquals(cur, root))
                return true;
            cur = cur.Parent as StyledElement;
        }
        return false;
    }

    private static bool TryGetIntProp(object obj, string name, out int value)
    {
        value = 0;
        try
        {
            var t = obj.GetType();

            var pi = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi != null)
            {
                var raw = pi.GetValue(obj);
                if (TryConvertNumber(raw, out value))
                    return true;
            }

            var fi = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi != null)
            {
                var raw = fi.GetValue(obj);
                if (TryConvertNumber(raw, out value))
                    return true;
            }
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
                case long l:
                    value = l > int.MaxValue ? int.MaxValue : (l < int.MinValue ? int.MinValue : (int)l);
                    return true;
                case short s: value = s; return true;
                case byte b: value = b; return true;
                case uint ui: value = ui > int.MaxValue ? int.MaxValue : (int)ui; return true;
                case ulong ul: value = ul > (ulong)int.MaxValue ? int.MaxValue : (int)ul; return true;
                case float f: value = (int)f; return true;
                case double d: value = (int)d; return true;
                case decimal m: value = (int)m; return true;
                default:
                    if (raw is IConvertible)
                    {
                        value = Convert.ToInt32(raw);
                        return true;
                    }
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetStringProp(object obj, string name, out string? value)
    {
        value = null;
        try
        {
            var pi = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi == null) return false;
            if (pi.GetValue(obj) is string s) { value = s; return true; }
        }
        catch { }
        return false;
    }
}
