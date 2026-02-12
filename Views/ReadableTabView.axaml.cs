// Views/ReadableTabView.axaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;

namespace CbetaTranslator.App.Views;

public partial class ReadableTabView : UserControl
{
    private TextBox? _editorOriginal;
    private TextBox? _editorTranslated;

    private HoverDictionaryBehavior? _hoverDict;
    private readonly ICedictDictionary _cedict = new CedictDictionaryService();

    private ScrollViewer? _svOriginal;
    private ScrollViewer? _svTranslated;

    // Cached template parts for better scroll/geometry
    private Visual? _presOriginal;
    private Visual? _presTranslated;
    private ScrollContentPresenter? _scpOriginal;
    private ScrollContentPresenter? _scpTranslated;

    private readonly ISelectionSyncService _selectionSync = new SelectionSyncService();

    private RenderedDocument _renderOrig = RenderedDocument.Empty;
    private RenderedDocument _renderTran = RenderedDocument.Empty;

    private bool _syncingSelection;

    private DateTime _ignoreProgrammaticUntilUtc = DateTime.MinValue;
    private const int IgnoreProgrammaticWindowMs = 180;

    private DateTime _suppressPollingUntilUtc = DateTime.MinValue;
    private const int SuppressPollingAfterUserActionMs = 220;

    private DispatcherTimer? _selTimer;
    private int _lastOrigSelStart = -1, _lastOrigSelEnd = -1;
    private int _lastTranSelStart = -1, _lastTranSelEnd = -1;
    private int _lastOrigCaret = -1, _lastTranCaret = -1;

    private DateTime _lastUserInputUtc = DateTime.MinValue;
    private TextBox? _lastUserInputEditor;
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

    private SearchHighlightOverlay? _hlOriginal;
    private SearchHighlightOverlay? _hlTranslated;

    private TextBox? _findTarget;
    private readonly List<int> _matchStarts = new();
    private int _matchLen = 0;
    private int _matchIndex = -1;

    private static readonly TimeSpan FindRecomputeDebounce = TimeSpan.FromMilliseconds(140);
    private DispatcherTimer? _findDebounceTimer;

    // When Find scrolls, never let mirroring/polling fight it
    private DateTime _suppressMirrorUntilUtc = DateTime.MinValue;
    private const int SuppressMirrorAfterFindMs = 900;

    // -------------------------
    // Notes: markers + bottom panel
    // -------------------------
    private AnnotationMarkerOverlay? _annMarksOriginal;
    private AnnotationMarkerOverlay? _annMarksTranslated;

    private Border? _notesPanel;
    private TextBlock? _notesHeader;
    private TextBox? _notesBody;
    private Button? _btnCloseNotes;

    // Notes actions
    private Button? _btnAddCommunityNote;
    private Button? _btnDeleteCommunityNote;

    // Track what note is currently shown
    private DocAnnotation? _currentAnn;

    public event EventHandler<DocAnnotation>? NoteClicked;

    public event EventHandler<(int XmlIndex, string NoteText, string? Resp)>? CommunityNoteInsertRequested;
    public event EventHandler<(int XmlStart, int XmlEndExclusive)>? CommunityNoteDeleteRequested;

    // NEW: status channel so MainWindow can show what's happening
    public event EventHandler<string>? Status;
    private void Say(string msg) => Status?.Invoke(this, msg);

    public ReadableTabView()
    {
        InitializeComponent();
        FindControls();
        WireEvents();

        AttachedToVisualTree += (_, _) =>
        {
            _svOriginal = FindScrollViewer(_editorOriginal);
            _svTranslated = FindScrollViewer(_editorTranslated);

            RefreshPresenterCache(_editorOriginal, isOriginal: true);
            RefreshPresenterCache(_editorTranslated, isOriginal: false);

            SetupHoverDictionary(); // hover dict MUST be on ORIGINAL
            StartSelectionTimer();

            Say("ReadableTabView attached.");
        };

        DetachedFromVisualTree += (_, _) =>
        {
            StopSelectionTimer();
            DisposeHoverDictionary();
            Say("ReadableTabView detached.");
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void FindControls()
    {
        _editorOriginal = this.FindControl<TextBox>("EditorOriginal");
        _editorTranslated = this.FindControl<TextBox>("EditorTranslated");

        if (_editorOriginal != null) _editorOriginal.IsReadOnly = true;
        if (_editorTranslated != null) _editorTranslated.IsReadOnly = true;

        if (_editorOriginal != null) _editorOriginal.TemplateApplied += (_, _) => RefreshPresenterCache(_editorOriginal, isOriginal: true);
        if (_editorTranslated != null) _editorTranslated.TemplateApplied += (_, _) => RefreshPresenterCache(_editorTranslated, isOriginal: false);

        _findBar = this.FindControl<Border>("FindBar");
        _findQuery = this.FindControl<TextBox>("FindQuery");
        _findCount = this.FindControl<TextBlock>("FindCount");
        _findScope = this.FindControl<TextBlock>("FindScope");
        _btnPrev = this.FindControl<Button>("BtnPrev");
        _btnNext = this.FindControl<Button>("BtnNext");
        _btnCloseFind = this.FindControl<Button>("BtnCloseFind");

        _hlOriginal = this.FindControl<SearchHighlightOverlay>("HlOriginal");
        _hlTranslated = this.FindControl<SearchHighlightOverlay>("HlTranslated");

        if (_hlOriginal != null) _hlOriginal.Target = _editorOriginal;
        if (_hlTranslated != null) _hlTranslated.Target = _editorTranslated;

        _annMarksOriginal = this.FindControl<AnnotationMarkerOverlay>("AnnMarksOriginal");
        _annMarksTranslated = this.FindControl<AnnotationMarkerOverlay>("AnnMarksTranslated");

        if (_annMarksOriginal != null) _annMarksOriginal.Target = _editorOriginal;
        if (_annMarksTranslated != null) _annMarksTranslated.Target = _editorTranslated;

        _notesPanel = this.FindControl<Border>("NotesPanel");
        _notesHeader = this.FindControl<TextBlock>("NotesHeader");
        _notesBody = this.FindControl<TextBox>("NotesBody");
        _btnCloseNotes = this.FindControl<Button>("BtnCloseNotes");

        _btnAddCommunityNote = this.FindControl<Button>("BtnAddCommunityNote");
        _btnDeleteCommunityNote = this.FindControl<Button>("BtnDeleteCommunityNote");

        // If XAML forgot to set it, make sure notes panel starts hidden.
        if (_notesPanel != null)
            _notesPanel.IsVisible = false;
    }

    private void WireEvents()
    {
        HookUserInputTracking(_editorOriginal);
        HookUserInputTracking(_editorTranslated);

        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

        // Capture note-clicks at UserControl level in Tunnel phase, and receive even if already handled
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

        // Notes actions (+ hard status reporting)
        if (_btnAddCommunityNote != null)
        {
            _btnAddCommunityNote.Click += async (_, _) =>
            {
                Say("ReadableTabView: Add community note clicked.");

                var (ok, reason) = await TryAddCommunityNoteAtSelectionOrCaretAsync();
                Say(ok ? "ReadableTabView: " + reason : "ReadableTabView: Add note blocked: " + reason);
            };
        }

        if (_btnDeleteCommunityNote != null)
        {
            _btnDeleteCommunityNote.Click += (_, _) =>
            {
                Say("ReadableTabView: Delete community note clicked.");
                DeleteCurrentCommunityNote();
            };
        }

        // If user focuses a pane while Find is open, switch scope like TranslationTabView
        if (_editorOriginal != null)
        {
            _editorOriginal.GotFocus += (_, _) =>
            {
                if (_findBar?.IsVisible == true)
                    SetFindTarget(_editorOriginal, preserveIndex: true);
            };
        }

        if (_editorTranslated != null)
        {
            _editorTranslated.GotFocus += (_, _) =>
            {
                if (_findBar?.IsVisible == true)
                    SetFindTarget(_editorTranslated, preserveIndex: true);
            };
        }
    }

    private void RefreshPresenterCache(TextBox? tb, bool isOriginal)
    {
        if (tb == null) return;

        var sv = FindScrollViewer(tb);
        var scp = sv != null ? FindScrollContentPresenter(sv) : null;

        var presenter = tb
            .GetVisualDescendants()
            .OfType<Visual>()
            .LastOrDefault(v => string.Equals(v.GetType().Name, "TextPresenter", StringComparison.Ordinal));

        presenter ??= tb
            .GetVisualDescendants()
            .OfType<Visual>()
            .LastOrDefault(v => (v.GetType().Name?.Contains("Text", StringComparison.OrdinalIgnoreCase) ?? false));

        if (isOriginal)
        {
            _svOriginal = sv ?? _svOriginal;
            _scpOriginal = scp;
            _presOriginal = presenter;
        }
        else
        {
            _svTranslated = sv ?? _svTranslated;
            _scpTranslated = scp;
            _presTranslated = presenter;
        }
    }

    private void SetupHoverDictionary()
    {
        if (_editorOriginal == null) return;

        _hoverDict?.Dispose();
        _hoverDict = new HoverDictionaryBehavior(_editorOriginal, _cedict);
    }

    private void DisposeHoverDictionary()
    {
        _hoverDict?.Dispose();
        _hoverDict = null;
    }

    // -------------------------
    // Notes click (robust)
    // -------------------------

    private void OnPointerPressed_TunnelForNotes(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        // If click is inside notes panel, ignore so copy/scroll works
        if (IsInsideControl(e.Source, _notesPanel))
            return;

        // If popup already open -> do NOT hijack clicks anymore
        if (_notesPanel?.IsVisible == true)
            return;

        // Ignore scrollbar parts only
        if (IsInsideScrollbarStuff(e.Source))
            return;

        var tb = FindAncestorTextBox(e.Source);
        if (tb == null) return;

        if (!ReferenceEquals(tb, _editorOriginal) && !ReferenceEquals(tb, _editorTranslated))
            return;

        var doc = ReferenceEquals(tb, _editorOriginal) ? _renderOrig : _renderTran;
        if (doc == null || doc.IsEmpty) return;

        // Resolve marker from marker spans (robust even when multiple markers are adjacent)
        if (!TryResolveAnnotationFromMarkerSpans(tb, doc, e, out var ann))
            return;


        ShowNotes(ann);
        NoteClicked?.Invoke(this, ann);
        e.Handled = true;
    }

    private bool TryResolveAnnotationFromMarkerSpans(
    TextBox tb,
    RenderedDocument doc,
    PointerPressedEventArgs e,
    out DocAnnotation ann)
    {
        ann = default!;

        int idx = GetCharIndexFromPointer(tb, e);
        if (idx < 0) return false;

        var markers = doc.AnnotationMarkers;
        var anns = doc.Annotations;

        if (markers == null || markers.Count == 0) return false;
        if (anns == null || anns.Count == 0) return false;

        // We’ll consider markers within this radius (in characters).
        const int radius = 8;

        // Binary search: first marker with Start > idx, so we can inspect neighbors.
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

        // Search around the insertion point for the closest span.
        int bestMarkerIndex = -1;
        int bestDist = int.MaxValue;

        // Check a small window around where the marker would be.
        int startScan = Math.Max(0, firstGreater - 6);
        int endScan = Math.Min(markers.Count - 1, firstGreater + 6);

        for (int i = startScan; i <= endScan; i++)
        {
            var m = markers[i];

            // Quick reject if clearly too far away
            if (m.Start > idx + radius) break;
            if (m.EndExclusive < idx - radius) continue;

            // IMPORTANT: treat EndExclusive as clickable (hit-testing often returns it).
            // So “inside” is [Start, EndExclusive] instead of [Start, EndExclusive).
            int dist;
            if (idx < m.Start) dist = m.Start - idx;
            else if (idx > m.EndExclusive) dist = idx - m.EndExclusive;
            else dist = 0;

            if (dist < bestDist)
            {
                bestDist = dist;
                bestMarkerIndex = i;
                if (dist == 0) break; // perfect hit
            }
        }

        if (bestMarkerIndex < 0 || bestDist > radius)
            return false;

        var best = markers[bestMarkerIndex];
        int annIndex = best.AnnotationIndex;

        if ((uint)annIndex >= (uint)anns.Count)
            return false;

        ann = anns[annIndex];
        return true;
    }


    private static bool IsSuperscriptMarkerChar(char c)
    {
        return c switch
        {
            '\u00B9' => true, // ¹
            '\u00B2' => true, // ²
            '\u00B3' => true, // ³
            '\u2070' => true, // ⁰
            '\u2071' => true, // ⁱ
            '\u2074' => true, // ⁴
            '\u2075' => true, // ⁵
            '\u2076' => true, // ⁶
            '\u2077' => true, // ⁷
            '\u2078' => true, // ⁸
            '\u2079' => true, // ⁹
            '\u207A' => true, // ⁺
            '\u207B' => true, // ⁻
            '\u207C' => true, // ⁼
            '\u207D' => true, // ⁽
            '\u207E' => true, // ⁾
            _ => false
        };
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

        // DO NOT steal focus
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
    }

    private void UpdateNotesButtonsState()
    {
        if (_btnAddCommunityNote != null)
            _btnAddCommunityNote.IsEnabled = !_renderTran.IsEmpty && _editorTranslated != null;

        if (_btnDeleteCommunityNote != null)
        {
            bool canDelete = false;
            if (_currentAnn != null && IsCommunityAnnotation(_currentAnn, out var xs, out var xe) && xe > xs)
                canDelete = true;

            _btnDeleteCommunityNote.IsEnabled = canDelete;
            _btnDeleteCommunityNote.IsVisible = canDelete; // hide unless it's deletable
        }
    }
    private static string? GetAnnotationResp(DocAnnotation ann)
    {
        // If your DocAnnotation has a Resp property, prefer that.
        try
        {
            // direct property access (most likely)
            var pi = ann.GetType().GetProperty("Resp");
            if (pi?.GetValue(ann) is string s && !string.IsNullOrWhiteSpace(s))
                return s.Trim();
        }
        catch { }

        // fallback: maybe it's called Author / By / Name
        if (TryGetStringProp(ann, "Author", out var a) && !string.IsNullOrWhiteSpace(a)) return a.Trim();
        if (TryGetStringProp(ann, "By", out var b) && !string.IsNullOrWhiteSpace(b)) return b.Trim();
        if (TryGetStringProp(ann, "Name", out var n) && !string.IsNullOrWhiteSpace(n)) return n.Trim();

        return null;
    }

    private static bool IsCommunityAnnotation(DocAnnotation ann, out int xmlStart, out int xmlEndExclusive)
    {
        xmlStart = -1;
        xmlEndExclusive = -1;

        // Heuristics: Kind/type/source contains "community"
        var kind = ann.Kind ?? "";
        var text = kind.ToLowerInvariant();
        bool looksCommunity = text.Contains("community") || text.Contains("comm");

        // Try to extract XmlStart/XmlEndExclusive from annotation (strong requirement for delete)
        if (TryGetIntProp(ann, "XmlStart", out var a) || TryGetIntProp(ann, "XmlStartIndex", out a) || TryGetIntProp(ann, "XmlFrom", out a))
            xmlStart = a;

        if (TryGetIntProp(ann, "XmlEndExclusive", out var b) || TryGetIntProp(ann, "XmlEnd", out b) || TryGetIntProp(ann, "XmlTo", out b))
            xmlEndExclusive = b;

        // If annotation has a "Type" property, use it
        if (TryGetStringProp(ann, "Type", out var t) && !string.IsNullOrWhiteSpace(t))
            looksCommunity = t.Trim().Equals("community", StringComparison.OrdinalIgnoreCase) || looksCommunity;

        // If annotation has a "Source" property, use it
        if (TryGetStringProp(ann, "Source", out var s) && !string.IsNullOrWhiteSpace(s))
            looksCommunity = s.Trim().Equals("community", StringComparison.OrdinalIgnoreCase) || looksCommunity;

        return looksCommunity && xmlStart >= 0 && xmlEndExclusive > xmlStart;
    }

    private async Task AddCommunityNoteFromCaretAsync()
    {
        // Community notes MUST be inserted into TRANSLATED XML, so we take caret from translated pane.
        if (_editorTranslated == null || _renderTran.IsEmpty)
            return;

        int caret = _editorTranslated.CaretIndex;
        if (caret < 0) caret = 0;

        if (!TryMapRenderedCaretToXmlIndex(_renderTran, caret, out int xmlIndex))
        {
            // If we cannot map, do nothing (can't safely insert).
            return;
        }

        await PromptAddCommunityNoteAsync(xmlIndex);
    }

    public async Task AddCommunityNoteAtCaretAsync()
    {
        await AddCommunityNoteFromCaretAsync();
    }

    // NEW: returns a reason so MainWindow (or your status bar) can show what's wrong.
    public async Task<(bool ok, string reason)> TryAddCommunityNoteAtSelectionOrCaretAsync()
    {
        if (_editorTranslated == null)
            return (false, "_editorTranslated is null (ReadableTabView not ready / not loaded yet).");

        if (_renderTran.IsEmpty)
            return (false, "_renderTran.IsEmpty (no rendered translated document set yet).");

        // Prefer selection midpoint
        int a = _editorTranslated.SelectionStart;
        int b = _editorTranslated.SelectionEnd;

        int renderedIndex =
            (a != b)
                ? Math.Min(a, b) + (Math.Abs(b - a) / 2)
                : _editorTranslated.CaretIndex;

        if (renderedIndex < 0) renderedIndex = 0;

        if (!TryMapRenderedCaretToXmlIndex(_renderTran, renderedIndex, out int xmlIndex))
        {
            var diag = ExplainMappingFailure(_renderTran, renderedIndex);
            return (false, $"Cannot map display index {renderedIndex} to XML index. BaseToXmlIndex is missing or out of range.");
        }

        await PromptAddCommunityNoteAsync(xmlIndex);
        return (true, $"Dialog opened. renderedIndex={renderedIndex} -> xmlIndex={xmlIndex}");
    }

    private static string ExplainMappingFailure(RenderedDocument doc, int renderedIndex)
    {
        try
        {
            var segs = doc.Segments;

            if (segs == null)
                return "doc.Segments is NULL (renderer did not populate segments).";

            if (segs.Count == 0)
                return "doc.Segments.Count == 0 (renderer produced no segments).";

            int nullCount = segs.Count(s => s == null);
            var firstNonNull = segs.FirstOrDefault(s => s != null);

            if (firstNonNull == null)
                return $"doc.Segments.Count == {segs.Count}, but ALL entries are null.";

            var t = firstNonNull.GetType();
            var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         .Select(p => p.Name)
                         .Distinct()
                         .OrderBy(n => n)
                         .ToList();

            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                          .Select(f => f.Name)
                          .Distinct()
                          .OrderBy(n => n)
                          .ToList();

            // Check how many segments we can parse ranges from
            int parsed = 0;
            int covers = 0;

            int minStart = int.MaxValue;
            int maxEnd = int.MinValue;

            for (int i = 0; i < segs.Count; i++)
            {
                var seg = segs[i];
                if (seg == null) continue;

                if (TryGetSegmentRanges(seg, out int rStart, out int rEndEx, out int xStart, out int xEndEx))
                {
                    parsed++;

                    if (rStart < minStart) minStart = rStart;
                    if (rEndEx > maxEnd) maxEnd = rEndEx;

                    // allow caret at endExclusive
                    if (renderedIndex >= rStart && renderedIndex <= rEndEx)
                        covers++;
                }
            }

            string rangeInfo =
                (parsed > 0 && minStart != int.MaxValue && maxEnd != int.MinValue)
                    ? $"ParsedSegments={parsed}/{segs.Count - nullCount} (non-null). RenderedRangeMinStart={minStart}, MaxEnd={maxEnd}. CaretCoveredBy={covers} segments."
                    : $"ParsedSegments=0/{segs.Count - nullCount} (non-null).";

            // Show “what we expected”
            string expected =
                "Expected seg members like: Start/EndExclusive (or End/Length) AND XmlStart/XmlEndExclusive (or XmlEnd).";

            // Don’t spam: only list some names
            string propPreview = props.Count == 0 ? "(none)" : string.Join(", ", props.Take(24)) + (props.Count > 24 ? ", …" : "");
            string fieldPreview = fields.Count == 0 ? "(none)" : string.Join(", ", fields.Take(24)) + (fields.Count > 24 ? ", …" : "");

            return $"Segments.Count={segs.Count}, NullEntries={nullCount}. FirstNonNullType={t.FullName}. {rangeInfo} {expected} Props[{props.Count}]: {propPreview}. Fields[{fields.Count}]: {fieldPreview}.";
        }
        catch (Exception ex)
        {
            return "ExplainMappingFailure threw: " + ex.GetType().Name + " " + ex.Message;
        }
    }


    private void DeleteCurrentCommunityNote()
    {
        if (_currentAnn == null)
            return;

        if (!IsCommunityAnnotation(_currentAnn, out int xs, out int xe))
            return;

        CommunityNoteDeleteRequested?.Invoke(this, (xs, xe));
        HideNotes();
    }

    private async Task PromptAddCommunityNoteAsync(int xmlIndex)
    {
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

        var tcs = new TaskCompletionSource<bool>();

        void CloseOk(bool ok)
        {
            try { win.Close(); } catch { }
            tcs.TrySetResult(ok);
        }

        btnCancel.Click += (_, _) => CloseOk(false);
        btnOk.Click += (_, _) => CloseOk(true);

        if (owner != null)
            _ = win.ShowDialog(owner);
        else
            win.Show();

        bool ok = await tcs.Task;
        if (!ok) return;

        var noteText = (txt.Text ?? "").Trim();
        if (noteText.Length == 0) return;

        var respVal = (resp.Text ?? "").Trim();
        if (respVal.Length == 0) respVal = null;

        CommunityNoteInsertRequested?.Invoke(this, (xmlIndex, noteText, respVal));
    }

    // Map rendered caret index -> XML index, using segment metadata if available.
    // Robust against long/int/fields/properties and different segment naming conventions.
    // Map DISPLAY caret index (with inserted markers) -> absolute XML index.
    // This MUST use the renderer-provided BaseToXmlIndex mapping.
    // Returns false if mapping isn't available.
    private static bool TryMapRenderedCaretToXmlIndex(RenderedDocument doc, int displayIndex, out int xmlIndex)
    {
        xmlIndex = -1;

        if (doc == null || doc.IsEmpty)
            return false;

        // This is the only valid mapping source.
        int mapped = doc.DisplayIndexToXmlIndex(displayIndex);

        if (mapped < 0)
            return false;

        xmlIndex = mapped;
        return true;
    }


    private static bool TryGetSegmentRanges(object seg, out int rStart, out int rEndEx, out int xStart, out int xEndEx)
    {
        rStart = rEndEx = xStart = xEndEx = 0;

        // Rendered start
        if (!TryGetIntProp(seg, "Start", out rStart) &&
            !TryGetIntProp(seg, "RenderedStart", out rStart) &&
            !TryGetIntProp(seg, "TextStart", out rStart))
            return false;

        // Rendered end exclusive (or end/length)
        if (TryGetIntProp(seg, "EndExclusive", out rEndEx) ||
            TryGetIntProp(seg, "RenderedEndExclusive", out rEndEx) ||
            TryGetIntProp(seg, "TextEndExclusive", out rEndEx))
        {
            // ok
        }
        else if (TryGetIntProp(seg, "End", out int rEnd))
        {
            // Some models use End as inclusive; assume it might be exclusive already.
            // We’ll treat it as exclusive if it looks like a typical exclusive value.
            // (Either way our containment allows caret==end.)
            rEndEx = rEnd;
        }
        else if (TryGetIntProp(seg, "Length", out int rLen))
        {
            rEndEx = rStart + Math.Max(0, rLen);
        }
        else
        {
            return false;
        }

        // XML start
        if (!TryGetIntProp(seg, "XmlStart", out xStart) &&
            !TryGetIntProp(seg, "XmlStartIndex", out xStart) &&
            !TryGetIntProp(seg, "XmlFrom", out xStart))
            return false;

        // XML end exclusive (or end/length)
        if (TryGetIntProp(seg, "XmlEndExclusive", out xEndEx) ||
            TryGetIntProp(seg, "XmlEnd", out xEndEx) ||
            TryGetIntProp(seg, "XmlTo", out xEndEx))
        {
            // ok
        }
        else if (TryGetIntProp(seg, "XmlLength", out int xLen))
        {
            xEndEx = xStart + Math.Max(0, xLen);
        }
        else
        {
            return false;
        }

        return true;
    }

    private static bool TryGetIntProp(object obj, string name, out int value)
    {
        value = 0;
        try
        {
            var t = obj.GetType();

            // property
            var pi = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi != null)
            {
                var raw = pi.GetValue(obj);
                if (TryConvertNumber(raw, out value))
                    return true;
            }

            // field
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

    private static TextBox? FindAncestorTextBox(object? source)
    {
        if (source is TextBox tb0) return tb0;

        if (source is Visual v)
            return v.GetVisualAncestors().OfType<TextBox>().FirstOrDefault();

        var cur = source as StyledElement;
        while (cur != null)
        {
            if (cur is TextBox tb) return tb;
            cur = cur.Parent as StyledElement;
        }

        return null;
    }

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

    private int GetCharIndexFromPointer(TextBox tb, PointerEventArgs e)
    {
        try
        {
            var pointInTb = e.GetPosition(tb);

            Visual? presenter = ReferenceEquals(tb, _editorOriginal) ? _presOriginal : _presTranslated;
            ScrollViewer? sv = ReferenceEquals(tb, _editorOriginal) ? _svOriginal : _svTranslated;

            presenter ??= tb
                .GetVisualDescendants()
                .OfType<Visual>()
                .LastOrDefault(v => string.Equals(v.GetType().Name, "TextPresenter", StringComparison.Ordinal))
                ?? tb.GetVisualDescendants()
                    .OfType<Visual>()
                    .LastOrDefault(v => (v.GetType().Name?.Contains("Text", StringComparison.OrdinalIgnoreCase) ?? false));

            if (presenter == null) return -1;

            Point pPresenter;

            var direct = tb.TranslatePoint(pointInTb, presenter);
            if (direct != null)
            {
                pPresenter = direct.Value;
            }
            else
            {
                sv ??= FindScrollViewer(tb);
                if (sv == null) return -1;

                var pSv = tb.TranslatePoint(pointInTb, sv);
                if (pSv == null) return -1;

                var corrected = new Point(pSv.Value.X + sv.Offset.X, pSv.Value.Y + sv.Offset.Y);
                var pPres2 = sv.TranslatePoint(corrected, presenter);
                if (pPres2 == null) return -1;

                pPresenter = pPres2.Value;
            }

            var tl = TryGetTextLayout(presenter);
            if (tl == null) return -1;

            var hit = tl.HitTestPoint(pPresenter);
            int idx = hit.TextPosition + (hit.IsTrailing ? 1 : 0);

            int len = tb.Text?.Length ?? 0;
            if (len <= 0) return -1;

            if (idx < 0) idx = 0;
            if (idx >= len) idx = len - 1;
            return idx;
        }
        catch
        {
            return -1;
        }
    }

    private static TextLayout? TryGetTextLayout(Visual presenter)
    {
        try
        {
            var prop = presenter.GetType().GetProperty(
                "TextLayout",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            return prop?.GetValue(presenter) as TextLayout;
        }
        catch
        {
            return null;
        }
    }

    // -------------------------
    // Public API
    // -------------------------

    public void Clear()
    {
        _renderOrig = RenderedDocument.Empty;
        _renderTran = RenderedDocument.Empty;

        if (_editorOriginal != null) _editorOriginal.Text = "";
        if (_editorTranslated != null) _editorTranslated.Text = "";

        _lastOrigSelStart = _lastOrigSelEnd = -1;
        _lastTranSelStart = _lastTranSelEnd = -1;
        _lastOrigCaret = _lastTranCaret = -1;

        ResetScroll(_svOriginal);
        ResetScroll(_svTranslated);

        if (_annMarksOriginal != null) _annMarksOriginal.Annotations = Array.Empty<DocAnnotation>();
        if (_annMarksTranslated != null) _annMarksTranslated.Annotations = Array.Empty<DocAnnotation>();

        HideNotes();

        ClearFindState();
        CloseFind();

        UpdateNotesButtonsState();
    }

    public void SetRendered(RenderedDocument orig, RenderedDocument tran)
    {
        _renderOrig = orig ?? RenderedDocument.Empty;
        _renderTran = tran ?? RenderedDocument.Empty;

        if (_editorOriginal != null) _editorOriginal.Text = _renderOrig.Text;
        if (_editorTranslated != null) _editorTranslated.Text = _renderTran.Text;

        if (_annMarksOriginal != null) _annMarksOriginal.Annotations = _renderOrig.Annotations ?? new List<DocAnnotation>();
        if (_annMarksTranslated != null) _annMarksTranslated.Annotations = _renderTran.Annotations ?? new List<DocAnnotation>();

        RefreshPresenterCache(_editorOriginal, isOriginal: true);
        RefreshPresenterCache(_editorTranslated, isOriginal: false);

        Dispatcher.UIThread.Post(() =>
        {
            RefreshPresenterCache(_editorOriginal, isOriginal: true);
            RefreshPresenterCache(_editorTranslated, isOriginal: false);
        }, DispatcherPriority.Loaded);

        DispatcherTimer.RunOnce(() =>
        {
            RefreshPresenterCache(_editorOriginal, isOriginal: true);
            RefreshPresenterCache(_editorTranslated, isOriginal: false);
        }, TimeSpan.FromMilliseconds(90));

        ResetScroll(_svOriginal);
        ResetScroll(_svTranslated);

        if (_editorOriginal != null)
        {
            _lastOrigSelStart = _editorOriginal.SelectionStart;
            _lastOrigSelEnd = _editorOriginal.SelectionEnd;
            _lastOrigCaret = _editorOriginal.CaretIndex;
        }

        if (_editorTranslated != null)
        {
            _lastTranSelStart = _editorTranslated.SelectionStart;
            _lastTranSelEnd = _editorTranslated.SelectionEnd;
            _lastTranCaret = _editorTranslated.CaretIndex;
        }

        HideNotes();
        UpdateNotesButtonsState();

        if (_findBar?.IsVisible == true)
            RecomputeMatches(resetToFirst: false);
    }

    // -------------------------
    // User input tracking (for mirroring + Find scope)
    // -------------------------

    private void HookUserInputTracking(TextBox? tb)
    {
        if (tb == null) return;

        tb.PointerPressed += OnEditorUserInput;
        tb.PointerReleased += OnEditorPointerReleased;
        tb.KeyDown += OnEditorUserInput;
        tb.KeyUp += OnEditorKeyUp;

        tb.GotFocus += (_, _) =>
        {
            if (_findBar?.IsVisible == true)
                SetFindTarget(tb, preserveIndex: true);
        };
    }

    private void OnEditorUserInput(object? sender, EventArgs e)
    {
        _lastUserInputUtc = DateTime.UtcNow;
        _lastUserInputEditor = sender as TextBox;
    }

    private void OnEditorPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _lastUserInputUtc = DateTime.UtcNow;
        _lastUserInputEditor = sender as TextBox;

        bool sourceIsTranslated = ReferenceEquals(_lastUserInputEditor, _editorTranslated);

        _suppressPollingUntilUtc = DateTime.UtcNow.AddMilliseconds(SuppressPollingAfterUserActionMs);
        RequestMirrorFromUserAction(sourceIsTranslated);
    }

    private void OnEditorKeyUp(object? sender, KeyEventArgs e)
    {
        _lastUserInputUtc = DateTime.UtcNow;
        _lastUserInputEditor = sender as TextBox;

        bool sourceIsTranslated = ReferenceEquals(_lastUserInputEditor, _editorTranslated);

        _suppressPollingUntilUtc = DateTime.UtcNow.AddMilliseconds(SuppressPollingAfterUserActionMs);
        RequestMirrorFromUserAction(sourceIsTranslated);
    }

    // -------------------------
    // Polling + mirroring
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
        if (DateTime.UtcNow <= _suppressPollingUntilUtc) return;
        if (_syncingSelection) return;
        if (DateTime.UtcNow <= _ignoreProgrammaticUntilUtc) return;
        if (DateTime.UtcNow <= _suppressMirrorUntilUtc) return;

        if (_editorOriginal == null || _editorTranslated == null) return;
        if (_renderOrig.IsEmpty || _renderTran.IsEmpty) return;

        bool anyFocused =
            _editorOriginal.IsFocused || _editorOriginal.IsKeyboardFocusWithin ||
            _editorTranslated.IsFocused || _editorTranslated.IsKeyboardFocusWithin;

        if (!anyFocused) return;

        int oS = _editorOriginal.SelectionStart;
        int oE = _editorOriginal.SelectionEnd;
        int tS = _editorTranslated.SelectionStart;
        int tE = _editorTranslated.SelectionEnd;
        int oC = _editorOriginal.CaretIndex;
        int tC = _editorTranslated.CaretIndex;

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
        if (_editorOriginal == null || _editorTranslated == null)
            return true;

        bool origFocused = _editorOriginal.IsFocused || _editorOriginal.IsKeyboardFocusWithin;
        bool tranFocused = _editorTranslated.IsFocused || _editorTranslated.IsKeyboardFocusWithin;

        bool recentInput = (DateTime.UtcNow - _lastUserInputUtc).TotalMilliseconds <= UserInputPriorityWindowMs;

        if (origFocused && !tranFocused) return false;
        if (tranFocused && !origFocused) return true;

        if (origChanged && !tranChanged) return false;
        if (tranChanged && !origChanged) return true;

        if (recentInput && _lastUserInputEditor != null)
            return ReferenceEquals(_lastUserInputEditor, _editorTranslated);

        if (tranFocused) return true;
        if (origFocused) return false;

        return true;
    }

    private void MirrorSelectionOneWay(bool sourceIsTranslated)
    {
        if (_editorOriginal == null || _editorTranslated == null) return;
        if (_renderOrig.IsEmpty || _renderTran.IsEmpty) return;

        var srcEditor = sourceIsTranslated ? _editorTranslated : _editorOriginal;
        var dstEditor = sourceIsTranslated ? _editorOriginal : _editorTranslated;

        var srcDoc = sourceIsTranslated ? _renderTran : _renderOrig;
        var dstDoc = sourceIsTranslated ? _renderOrig : _renderTran;

        int caret = srcEditor.CaretIndex;

        if (!_selectionSync.TryGetDestinationSegment(srcDoc, dstDoc, caret, out var dstSeg))
            return;

        try
        {
            _syncingSelection = true;

            ApplyDestinationSelection(dstEditor, dstSeg.Start, dstSeg.EndExclusive, center: true);

            if (ReferenceEquals(dstEditor, _editorOriginal))
            {
                _lastOrigSelStart = dstEditor.SelectionStart;
                _lastOrigSelEnd = dstEditor.SelectionEnd;
                _lastOrigCaret = dstEditor.CaretIndex;
            }
            else
            {
                _lastTranSelStart = dstEditor.SelectionStart;
                _lastTranSelEnd = dstEditor.SelectionEnd;
                _lastTranCaret = dstEditor.CaretIndex;
            }

            _ignoreProgrammaticUntilUtc = DateTime.UtcNow.AddMilliseconds(IgnoreProgrammaticWindowMs);
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private void ApplyDestinationSelection(TextBox dst, int start, int endExclusive, bool center)
    {
        int len = dst.Text?.Length ?? 0;
        start = Math.Max(0, Math.Min(start, len));
        endExclusive = Math.Max(0, Math.Min(endExclusive, len));
        if (endExclusive < start) (start, endExclusive) = (endExclusive, start);

        dst.SelectionStart = start;
        dst.SelectionEnd = endExclusive;

        try { dst.CaretIndex = start; } catch { }

        if (!center) return;

        int anchor = start + Math.Max(0, (endExclusive - start) / 2);
        CenterByTextLayoutReliable(dst, anchor);
    }

    private void RequestMirrorFromUserAction(bool sourceIsTranslated)
    {
        if (DateTime.UtcNow <= _suppressMirrorUntilUtc) return;

        _mirrorSourceIsTranslated = sourceIsTranslated;
        if (_mirrorQueued) return;
        _mirrorQueued = true;

        Dispatcher.UIThread.Post(() =>
        {
            _mirrorQueued = false;

            if (_syncingSelection) return;
            if (_renderOrig.IsEmpty || _renderTran.IsEmpty) return;
            if (DateTime.UtcNow <= _suppressMirrorUntilUtc) return;

            MirrorSelectionOneWay(_mirrorSourceIsTranslated);
        }, DispatcherPriority.Background);
    }

    // -------------------------
    // Ctrl+F Find UI (MATCH TranslationTabView behavior)
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

        // restore focus without messing with selections
        _findTarget?.Focus();
    }

    private TextBox? DetermineCurrentPaneForFind()
    {
        if (_editorOriginal == null || _editorTranslated == null)
            return _editorTranslated;

        bool recentInput = (DateTime.UtcNow - _lastUserInputUtc).TotalMilliseconds <= UserInputPriorityWindowMs;
        if (recentInput && _lastUserInputEditor != null)
            return _lastUserInputEditor;

        if (_editorTranslated.IsFocused || _editorTranslated.IsKeyboardFocusWithin) return _editorTranslated;
        if (_editorOriginal.IsFocused || _editorOriginal.IsKeyboardFocusWithin) return _editorOriginal;

        return _editorTranslated;
    }

    private void SetFindTarget(TextBox? tb, bool preserveIndex)
    {
        if (tb == null) return;

        _findTarget = tb;

        if (_findScope != null)
            _findScope.Text = ReferenceEquals(tb, _editorOriginal) ? "Find (Original):" : "Find (Translated):";

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

        var tb = _findTarget;
        if (tb == null) return;

        string hay = tb.Text ?? "";
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
            int caret = tb.CaretIndex;
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
            else
            {
                _matchIndex = 0;
            }
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

        // highlight immediately (do NOT touch selection)
        ApplyHighlight(_findTarget, start, len);

        if (!scroll)
            return;

        try
        {
            // hard suppress so Find never triggers mirroring fights
            _suppressPollingUntilUtc = DateTime.UtcNow.AddMilliseconds(420);
            _ignoreProgrammaticUntilUtc = DateTime.UtcNow.AddMilliseconds(420);
            _suppressMirrorUntilUtc = DateTime.UtcNow.AddMilliseconds(SuppressMirrorAfterFindMs);

            _findTarget.Focus();

            // scroll via caret only (no selection changes)
            _findTarget.CaretIndex = Math.Clamp(start, 0, (_findTarget.Text ?? "").Length);

            DispatcherTimer.RunOnce(() =>
            {
                try { CenterByTextLayoutReliable(_findTarget, start); } catch { }
                ApplyHighlight(_findTarget, start, len);
            }, TimeSpan.FromMilliseconds(25));

            DispatcherTimer.RunOnce(() =>
            {
                try { CenterByTextLayoutReliable(_findTarget, start); } catch { }
                ApplyHighlight(_findTarget, start, len);
            }, TimeSpan.FromMilliseconds(85));
        }
        catch { }
    }

    private void ApplyHighlight(TextBox target, int start, int len)
    {
        if (_hlOriginal == null || _hlTranslated == null) return;

        if (ReferenceEquals(target, _editorOriginal))
        {
            _hlTranslated.Clear();
            _hlOriginal.SetRange(start, len);
        }
        else
        {
            _hlOriginal.Clear();
            _hlTranslated.SetRange(start, len);
        }
    }

    private void ClearHighlight()
    {
        _hlOriginal?.Clear();
        _hlTranslated?.Clear();
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
    // Scroll helpers
    // -------------------------

    private static void ResetScroll(ScrollViewer? sv)
    {
        if (sv == null) return;
        sv.Offset = new Vector(0, 0);
    }

    private static ScrollViewer? FindScrollViewer(Control? c)
    {
        if (c == null) return null;
        return c.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
    }

    private static ScrollContentPresenter? FindScrollContentPresenter(ScrollViewer? sv)
    {
        if (sv == null) return null;
        return sv.GetVisualDescendants().OfType<ScrollContentPresenter>().FirstOrDefault();
    }

    private static bool IsFinitePositive(double v)
        => !double.IsNaN(v) && !double.IsInfinity(v) && v > 0;

    private static double ClampY(double extentH, double viewportH, double y)
    {
        if (IsFinitePositive(extentH) && IsFinitePositive(viewportH))
        {
            double maxY = Math.Max(0, extentH - viewportH);
            return Math.Max(0, Math.Min(y, maxY));
        }
        return Math.Max(0, y);
    }

    private void CenterByTextLayoutReliable(TextBox tb, int charIndex)
    {
        ScrollViewer? sv;
        ScrollContentPresenter? scp;
        Visual? presenter;

        if (ReferenceEquals(tb, _editorOriginal))
        {
            sv = _svOriginal ?? FindScrollViewer(tb);
            scp = _scpOriginal ?? FindScrollContentPresenter(sv);
            presenter = _presOriginal;
        }
        else
        {
            sv = _svTranslated ?? FindScrollViewer(tb);
            scp = _scpTranslated ?? FindScrollContentPresenter(sv);
            presenter = _presTranslated;
        }

        if (sv == null) return;

        int len = tb.Text?.Length ?? 0;
        if (len <= 0) return;
        charIndex = Math.Clamp(charIndex, 0, len);

        Dispatcher.UIThread.Post(() => TryCenterOnce_TextLayout(tb, sv, scp, presenter, charIndex), DispatcherPriority.Render);
        DispatcherTimer.RunOnce(() => TryCenterOnce_TextLayout(tb, sv, scp, presenter, charIndex), TimeSpan.FromMilliseconds(28));
        DispatcherTimer.RunOnce(() => TryCenterOnce_TextLayout(tb, sv, scp, presenter, charIndex), TimeSpan.FromMilliseconds(60));
        DispatcherTimer.RunOnce(() => TryCenterOnce_TextLayout(tb, sv, scp, presenter, charIndex), TimeSpan.FromMilliseconds(110));
    }

    private static void TryCenterOnce_TextLayout(TextBox tb, ScrollViewer sv, ScrollContentPresenter? scp, Visual? presenter, int charIndex)
    {
        double viewportH = sv.Viewport.Height;
        double extentH = sv.Extent.Height;
        if (!IsFinitePositive(viewportH) || !IsFinitePositive(extentH))
            return;

        Visual target = (Visual?)scp ?? (Visual)sv;

        presenter ??= tb
            .GetVisualDescendants()
            .OfType<Visual>()
            .LastOrDefault(v => string.Equals(v.GetType().Name, "TextPresenter", StringComparison.Ordinal));

        if (presenter == null) return;

        TextLayout? tl = null;
        try
        {
            var prop = presenter.GetType().GetProperty("TextLayout", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop?.GetValue(presenter) is TextLayout got)
                tl = got;
        }
        catch { }

        if (tl == null) return;

        Rect r;
        try { r = tl.HitTestTextPosition(charIndex); }
        catch { return; }

        var p = presenter.TranslatePoint(new Point(r.X, r.Y), target);
        if (p == null) return;

        double yInViewport = p.Value.Y;

        double targetY = viewportH * 0.40;
        double topBand = viewportH * 0.15;
        double bottomBand = viewportH * 0.85;

        if (yInViewport >= topBand && yInViewport <= bottomBand)
            return;

        double desiredY = sv.Offset.Y + (yInViewport - targetY);

        desiredY = ClampY(extentH, viewportH, desiredY);
        sv.Offset = new Vector(sv.Offset.X, desiredY);
    }
}
