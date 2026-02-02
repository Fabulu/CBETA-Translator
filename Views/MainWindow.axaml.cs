using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.Text;

namespace CbetaTranslator.App.Views;

public partial class MainWindow : Window
{
    // Named controls (match MainWindow.axaml)
    private Button? _btnToggleNav;
    private Button? _btnOpenRoot;
    private Button? _btnSave;

    private Border? _navPanel;
    private ListBox? _filesList;

    private TextBox? _editorOriginal;
    private TextBox? _editorTranslated;

    private TextBlock? _txtRoot;
    private TextBlock? _txtCurrentFile;
    private TextBlock? _txtStatus;

    // Internal scroll viewers
    private ScrollViewer? _svOriginal;
    private ScrollViewer? _svTranslated;

    // Services
    private readonly IFileService _fileService = new FileService();
    private readonly ISelectionSyncService _selectionSync = new SelectionSyncService();

    // State
    private string? _root;
    private string? _originalDir;
    private string? _translatedDir;
    private List<string> _relativeFiles = new();
    private string? _currentRelPath;

    // Raw XML
    private string _rawOrigXml = "";
    private string _rawTranXml = "";

    // Rendered readable text + segment maps
    private RenderedDocument _renderOrig = RenderedDocument.Empty;
    private RenderedDocument _renderTran = RenderedDocument.Empty;

    // Selection sync guards
    private bool _syncingSelection;

    // Ignore programmatic selection churn (ONLY for timer polling)
    private DateTime _ignoreProgrammaticUntilUtc = DateTime.MinValue;
    private const int IgnoreProgrammaticWindowMs = 180;

    // Suppress polling right after explicit user-action mirroring
    private DateTime _suppressPollingUntilUtc = DateTime.MinValue;
    private const int SuppressPollingAfterUserActionMs = 220;

    // Selection polling
    private DispatcherTimer? _selTimer;
    private int _lastOrigSelStart = -1, _lastOrigSelEnd = -1;
    private int _lastTranSelStart = -1, _lastTranSelEnd = -1;
    private int _lastOrigCaret = -1, _lastTranCaret = -1;

    // Source determination
    private DateTime _lastUserInputUtc = DateTime.MinValue;
    private TextBox? _lastUserInputEditor;
    private const int UserInputPriorityWindowMs = 250;

    // Fallback heuristic
    private const int JumpFallbackExtraLines = 8;

    public MainWindow()
    {
        InitializeComponent();

        FindControls();
        WireEvents();

        if (_editorOriginal != null) _editorOriginal.IsReadOnly = true;
        if (_editorTranslated != null) _editorTranslated.IsReadOnly = true;

        SetStatus("Ready. Click Open Root.");

        Opened += (_, _) =>
        {
            _svOriginal = FindScrollViewer(_editorOriginal);
            _svTranslated = FindScrollViewer(_editorTranslated);
            StartSelectionTimer();
        };

        Closed += (_, _) => StopSelectionTimer();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void FindControls()
    {
        _btnToggleNav = this.FindControl<Button>("BtnToggleNav");
        _btnOpenRoot = this.FindControl<Button>("BtnOpenRoot");
        _btnSave = this.FindControl<Button>("BtnSave");

        _navPanel = this.FindControl<Border>("NavPanel");
        _filesList = this.FindControl<ListBox>("FilesList");

        _editorOriginal = this.FindControl<TextBox>("EditorOriginal");
        _editorTranslated = this.FindControl<TextBox>("EditorTranslated");

        _txtRoot = this.FindControl<TextBlock>("TxtRoot");
        _txtCurrentFile = this.FindControl<TextBlock>("TxtCurrentFile");
        _txtStatus = this.FindControl<TextBlock>("TxtStatus");
    }

    private void WireEvents()
    {
        if (_btnToggleNav != null) _btnToggleNav.Click += ToggleNav_Click;
        if (_btnOpenRoot != null) _btnOpenRoot.Click += OpenRoot_Click;
        if (_btnSave != null) _btnSave.Click += Save_Click_Stub;

        if (_filesList != null) _filesList.SelectionChanged += FilesList_SelectionChanged;

        HookUserInputTracking(_editorOriginal);
        HookUserInputTracking(_editorTranslated);
    }

    private void HookUserInputTracking(TextBox? tb)
    {
        if (tb == null) return;

        tb.PointerPressed += OnEditorUserInput;
        tb.PointerReleased += OnEditorPointerReleased;
        tb.KeyDown += OnEditorUserInput;
        tb.KeyUp += OnEditorKeyUp;
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

    private void ToggleNav_Click(object? sender, RoutedEventArgs e)
    {
        if (_navPanel == null) return;
        _navPanel.IsVisible = !_navPanel.IsVisible;
    }

    private async void OpenRoot_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (StorageProvider is null)
            {
                SetStatus("StorageProvider not available.");
                return;
            }

            var picked = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select CBETA root folder (contains xml-p5; xml-p5t will be created if missing)"
            });

            var folder = picked.FirstOrDefault();
            if (folder is null) return;

            _root = folder.Path.LocalPath;
            _originalDir = AppPaths.GetOriginalDir(_root);
            _translatedDir = AppPaths.GetTranslatedDir(_root);

            if (_txtRoot != null) _txtRoot.Text = _root;

            if (!System.IO.Directory.Exists(_originalDir))
            {
                SetStatus($"Original folder missing: {_originalDir}");
                return;
            }

            AppPaths.EnsureTranslatedDirExists(_root);
            await LoadFileListAsync();
        }
        catch (Exception ex)
        {
            SetStatus("Open root failed: " + ex.Message);
        }
    }

    private async Task LoadFileListAsync()
    {
        if (_originalDir == null || _filesList == null)
            return;

        SetStatus("Scanning xml-p5…");

        _relativeFiles = await _fileService.EnumerateXmlRelativePathsAsync(_originalDir);
        _filesList.ItemsSource = _relativeFiles;

        _filesList.SelectedItem = null;
        _currentRelPath = null;

        ClearEditors();
        SetStatus($"Found {_relativeFiles.Count:n0} XML files. Click one to load.");
    }

    private void ClearEditors()
    {
        _rawOrigXml = "";
        _rawTranXml = "";
        _renderOrig = RenderedDocument.Empty;
        _renderTran = RenderedDocument.Empty;

        if (_editorOriginal != null) _editorOriginal.Text = "";
        if (_editorTranslated != null) _editorTranslated.Text = "";

        if (_txtCurrentFile != null) _txtCurrentFile.Text = "";

        _lastOrigSelStart = _lastOrigSelEnd = -1;
        _lastTranSelStart = _lastTranSelEnd = -1;
        _lastOrigCaret = _lastTranCaret = -1;

        ResetScroll(_svOriginal);
        ResetScroll(_svTranslated);
    }

    private async void FilesList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_filesList?.SelectedItem is not string rel)
            return;

        await LoadPairAsync(rel);
    }

    private async Task LoadPairAsync(string relPath)
    {
        if (_originalDir == null || _translatedDir == null || _editorOriginal == null || _editorTranslated == null)
            return;

        _currentRelPath = relPath;

        if (_txtCurrentFile != null)
            _txtCurrentFile.Text = relPath;

        SetStatus("Loading: " + relPath);

        var (orig, tran) = await _fileService.ReadPairAsync(_originalDir, _translatedDir, relPath);

        _rawOrigXml = orig ?? "";
        _rawTranXml = tran ?? "";

        SetStatus("Rendering readable view…");

        _renderOrig = CbetaTeiRenderer.Render(_rawOrigXml);
        _renderTran = CbetaTeiRenderer.Render(_rawTranXml);

        _editorOriginal.Text = _renderOrig.Text;
        _editorTranslated.Text = _renderTran.Text;

        ResetScroll(_svOriginal);
        ResetScroll(_svTranslated);

        _lastOrigSelStart = _editorOriginal.SelectionStart;
        _lastOrigSelEnd = _editorOriginal.SelectionEnd;
        _lastTranSelStart = _editorTranslated.SelectionStart;
        _lastTranSelEnd = _editorTranslated.SelectionEnd;
        _lastOrigCaret = _editorOriginal.CaretIndex;
        _lastTranCaret = _editorTranslated.CaretIndex;

        SetStatus($"Loaded readable text. Segments: O={_renderOrig.Segments.Count:n0}, T={_renderTran.Segments.Count:n0}. Mirroring active.");
    }

    private void Save_Click_Stub(object? sender, RoutedEventArgs e)
        => SetStatus("Save not implemented yet (readable mode is display-only).");

    private void SetStatus(string msg)
    {
        if (_txtStatus != null)
            _txtStatus.Text = msg;
    }

    // -------------------------
    // Polling (safety net) + mirroring
    // -------------------------

    private void StartSelectionTimer()
    {
        if (_selTimer != null) return;

        _selTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
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

        if (_editorOriginal == null || _editorTranslated == null) return;
        if (_renderOrig.IsEmpty || _renderTran.IsEmpty) return;

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
        MirrorSelectionOneWay(sourceIsTranslated);
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

        bool destinationIsChinese = ReferenceEquals(dstEditor, _editorOriginal);

        try
        {
            _syncingSelection = true;

            ApplyDestinationSelection(dstEditor, dstSeg.Start, dstSeg.EndExclusive, center: true, destinationIsChinese);

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

    private void ApplyDestinationSelection(TextBox dst, int start, int endExclusive, bool center, bool destinationIsChinese)
    {
        int len = dst.Text?.Length ?? 0;
        start = Math.Max(0, Math.Min(start, len));
        endExclusive = Math.Max(0, Math.Min(endExclusive, len));
        if (endExclusive < start) (start, endExclusive) = (endExclusive, start);

        dst.SelectionStart = start;
        dst.SelectionEnd = endExclusive;

        try { dst.CaretIndex = start; } catch { /* ignore */ }

        if (!center) return;

        // Key fix:
        // - If destination is Chinese: use newline-based centering (very stable because <lb/> => real '\n')
        // - If destination is English: use rect-based refinement (wrapping-heavy, newlines sparse)
        DispatcherTimer.RunOnce(() =>
        {
            try
            {
                if (destinationIsChinese)
                    CenterByNewlines(dst, start);
                else
                    CenterByCaretRect(dst, start);
            }
            catch { /* ignore */ }
        }, TimeSpan.FromMilliseconds(20));

        // One more pass after layout settles a bit more
        DispatcherTimer.RunOnce(() =>
        {
            try
            {
                if (destinationIsChinese)
                    CenterByNewlines(dst, start);
                else
                    CenterByCaretRect(dst, start);
            }
            catch { /* ignore */ }
        }, TimeSpan.FromMilliseconds(55));
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

    private static void CenterByNewlines(TextBox tb, int caretIndex)
    {
        var sv = FindScrollViewer(tb);
        if (sv == null) return;

        double viewportH = sv.Viewport.Height;
        double extentH = sv.Extent.Height;

        // Even if viewport metrics are flaky, this still produces a reasonable jump.
        double lineH = Math.Max(12.0, tb.FontSize * 1.35);

        string text = tb.Text ?? "";
        int lineIndex = CountNewlinesUpTo(text, caretIndex);

        if (!IsFinitePositive(viewportH))
        {
            // Minimal fallback
            double y = Math.Max(0, (lineIndex - JumpFallbackExtraLines) * lineH);
            sv.Offset = new Vector(sv.Offset.X, y);
            return;
        }

        double visibleLines = viewportH / lineH;
        double topLine = Math.Max(0, lineIndex - (visibleLines / 2.0));
        double desiredY = topLine * lineH;

        desiredY = ClampY(extentH, viewportH, desiredY);
        sv.Offset = new Vector(sv.Offset.X, desiredY);
    }

    private static void CenterByCaretRect(TextBox tb, int caretIndex)
    {
        var sv = FindScrollViewer(tb);
        if (sv == null) return;

        double viewportH = sv.Viewport.Height;
        double extentH = sv.Extent.Height;

        if (!IsFinitePositive(viewportH))
            return;

        if (!TryGetCaretYInScrollViewer(tb, sv, caretIndex, out double caretY))
            return;

        double targetY = viewportH / 2.0;
        double delta = caretY - targetY;

        if (Math.Abs(delta) <= Math.Max(6.0, viewportH * 0.03))
            return;

        double desiredY = sv.Offset.Y + delta;
        desiredY = ClampY(extentH, viewportH, desiredY);

        sv.Offset = new Vector(sv.Offset.X, desiredY);
    }

    private static int CountNewlinesUpTo(string text, int index)
    {
        if (string.IsNullOrEmpty(text) || index <= 0) return 0;
        if (index > text.Length) index = text.Length;

        int count = 0;
        for (int i = 0; i < index; i++)
        {
            if (text[i] == '\n')
                count++;
        }
        return count;
    }

    /// <summary>
    /// Returns caret Y in the ScrollViewer's coordinate space (viewport coords).
    /// Used for the English side where wrapping dominates.
    /// </summary>
    private static bool TryGetCaretYInScrollViewer(TextBox tb, ScrollViewer sv, int charIndex, out double caretY)
    {
        caretY = 0;

        int len = tb.Text?.Length ?? 0;
        if (len <= 0) return false;
        charIndex = Math.Clamp(charIndex, 0, len);

        // Prefer TextPresenter rect (reliable with wrapping)
        try
        {
            var presenter = tb.GetVisualDescendants().FirstOrDefault(v => v.GetType().Name == "TextPresenter");
            if (presenter != null)
            {
                var mPr = presenter.GetType().GetMethod("GetRectFromCharacterIndex", new[] { typeof(int) });
                if (mPr != null)
                {
                    var val = mPr.Invoke(presenter, new object[] { charIndex });
                    if (val is Rect r2)
                    {
                        var p = presenter.TranslatePoint(new Point(r2.X, r2.Y), sv);
                        if (p != null)
                        {
                            caretY = p.Value.Y;
                            return true;
                        }
                    }
                }
            }
        }
        catch { /* ignore */ }

        // Fallback to TextBox.GetRectFromCharacterIndex, translate to ScrollViewer coords
        var mTb = tb.GetType().GetMethod("GetRectFromCharacterIndex", new[] { typeof(int) });
        if (mTb != null)
        {
            try
            {
                var val = mTb.Invoke(tb, new object[] { charIndex });
                if (val is Rect r)
                {
                    var p = tb.TranslatePoint(new Point(r.X, r.Y), sv);
                    if (p != null)
                    {
                        caretY = p.Value.Y;
                        return true;
                    }
                }
            }
            catch { /* ignore */ }
        }

        return false;
    }

    // -------------------------
    // User-action mirror scheduling (one-click)
    // -------------------------

    private void RequestMirrorFromUserAction(bool sourceIsTranslated)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_syncingSelection) return;
            if (_renderOrig.IsEmpty || _renderTran.IsEmpty) return;
            MirrorSelectionOneWay(sourceIsTranslated);
        }, DispatcherPriority.Background);

        DispatcherTimer.RunOnce(() =>
        {
            if (_syncingSelection) return;
            if (_renderOrig.IsEmpty || _renderTran.IsEmpty) return;
            MirrorSelectionOneWay(sourceIsTranslated);
        }, TimeSpan.FromMilliseconds(25));
    }
}
