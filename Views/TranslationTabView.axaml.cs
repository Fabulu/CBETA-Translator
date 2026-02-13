// Views/TranslationTabView.axaml.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Services;

namespace CbetaTranslator.App.Views;

public partial class TranslationTabView : UserControl
{
    // -------------------------
    // DEBUG LOGGING
    // -------------------------
    private static void Log(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [TranslationTabView] {msg}";
        Console.WriteLine(line);
        Debug.WriteLine(line);
    }

    private static string LenStr(string? s) => s == null ? "null" : s.Length.ToString();

    private static string BrushStr(IBrush? b)
    {
        if (b == null) return "null";
        if (b is SolidColorBrush scb) return $"Solid({scb.Color})";
        return b.GetType().Name;
    }

    private static bool IsTransparentBrush(IBrush? b)
    {
        if (b is SolidColorBrush scb)
            return scb.Color.A == 0;
        return false;
    }

    private static IBrush SafeBrushOrFallback(IBrush? current, IBrush fallback)
        => (current == null || IsTransparentBrush(current)) ? fallback : current;

    private static string Sha1Short(string s)
    {
        try
        {
            using var sha1 = SHA1.Create();
            var bytes = Encoding.UTF8.GetBytes(s);
            var hash = sha1.ComputeHash(bytes);
            return Convert.ToHexString(hash).Substring(0, 12);
        }
        catch { return "sha1_err"; }
    }

    // -------------------------
    // FILE PATHS (MUST BE SET BY PARENT WHEN A FILE IS LOADED)
    // -------------------------
    private string? _currentOrigPath;
    private string? _currentTranPath;

    /// <summary>
    /// Parent must call this when selecting/loading a file.
    /// These are the DISK paths that add/delete MUST modify.
    /// </summary>
    public void SetCurrentFilePaths(string originalXmlPath, string translatedXmlPath)
    {
        _currentOrigPath = originalXmlPath;
        _currentTranPath = translatedXmlPath;

        Log($"SetCurrentFilePaths: orig='{_currentOrigPath}' (exists={File.Exists(_currentOrigPath)}) " +
            $"tran='{_currentTranPath}' (exists={File.Exists(_currentTranPath)})");
    }

    // Gate: prevent overlapping save+reload storms (your logs show double SetXml bursts)
    private readonly SemaphoreSlim _saveReloadGate = new(1, 1);

    // -------------------------
    // UI controls
    // -------------------------
    private Button? _btnCopyPrompt;
    private Button? _btnPasteReplace;
    private Button? _btnSaveTranslated;
    private Button? _btnSelectNext50Tags;
    private Button? _btnCheckXml;

    // IMPORTANT: these are AvaloniaEdit TextEditor, not TextBox
    private TextEditor? _orig;
    private TextEditor? _tran;

    private TextBlock? _txtHint;

    // Hover dictionary (AvaloniaEdit)
    private HoverDictionaryBehaviorEdit? _hoverDictOrig;
    private HoverDictionaryBehaviorEdit? _hoverDictTran;
    private readonly ICedictDictionary _cedict = new CedictDictionaryService();

    // Remember last "copy selection" range
    private int _lastCopyStart = -1;
    private int _lastCopyEnd = -1;

    public event EventHandler? SaveRequested;
    public event EventHandler<string>? Status;

    // -------------------------
    // Ctrl+F Find state
    // -------------------------
    private Border? _findBar;
    private TextBox? _findQuery;
    private TextBlock? _findCount;
    private TextBlock? _findScope;
    private Button? _btnPrev;
    private Button? _btnNext;
    private Button? _btnCloseFind;

    // Find highlight renderers
    private SearchHighlightRenderer? _hlOrig;
    private SearchHighlightRenderer? _hlTran;

    private TextEditor? _findTarget;

    private readonly List<int> _matchStarts = new();
    private int _matchLen = 0;
    private int _matchIndex = -1;

    private static readonly TimeSpan FindRecomputeDebounce = TimeSpan.FromMilliseconds(140);
    private DispatcherTimer? _findDebounceTimer;

    // Track last user input editor for sane scope selection
    private DateTime _lastUserInputUtc = DateTime.MinValue;
    private TextEditor? _lastUserInputEditor;
    private const int UserInputPriorityWindowMs = 250;

    // cached text (so we can re-apply on attach)
    private string _cachedOrigXml = "";
    private string _cachedTranXml = "";

    public TranslationTabView()
    {
        Log("CTOR start");

        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            // If XAML fails to load, you otherwise get "blank UI" and suffer.
            Log("InitializeComponent ERROR: " + ex);

            Content = new Border
            {
                Background = Brushes.Black,
                Padding = new Thickness(12),
                Child = new TextBlock
                {
                    Foreground = Brushes.OrangeRed,
                    Text = "TranslationTabView failed to load XAML.\n\n" + ex.ToString()
                }
            };
            return;
        }

        FindControls();
        WireEvents();

        AttachedToVisualTree += (_, _) =>
        {
            Log("AttachedToVisualTree");

            ApplyEditorDefaults(_orig, "orig (AttachedToVisualTree)");
            ApplyEditorDefaults(_tran, "tran (AttachedToVisualTree)");

            SetupHoverDictionary();

            Dispatcher.UIThread.Post(() =>
            {
                Log("AttachedToVisualTree -> EnsureFindRenderersAttached (Background)");
                EnsureFindRenderersAttached();
            }, DispatcherPriority.Background);

            Dispatcher.UIThread.Post(() =>
            {
                Log("AttachedToVisualTree -> ReApplyEditorsText (Background)");
                ReApplyEditorsText("AttachedToVisualTree post");
            }, DispatcherPriority.Background);
        };

        DetachedFromVisualTree += (_, _) =>
        {
            Log("DetachedFromVisualTree");
            DisposeHoverDictionary();
            DetachFindRenderers();
        };

        UpdateHint("Select XML in Translated XML → Copy selection + prompt.");
        Log("CTOR end");
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void FindControls()
    {
        Log("FindControls start");

        _btnCopyPrompt = this.FindControl<Button>("BtnCopyPrompt");
        _btnPasteReplace = this.FindControl<Button>("BtnPasteReplace");
        _btnSaveTranslated = this.FindControl<Button>("BtnSaveTranslated");
        _btnSelectNext50Tags = this.FindControl<Button>("BtnSelectNext50Tags");
        _btnCheckXml = this.FindControl<Button>("BtnCheckXml");

        _orig = this.FindControl<TextEditor>("EditorOrigXml");
        _tran = this.FindControl<TextEditor>("EditorTranXml");

        _txtHint = this.FindControl<TextBlock>("TxtHint");

        Log($"Controls: orig={_orig != null}, tran={_tran != null}, hint={_txtHint != null}");

        if (_orig != null)
        {
            _orig.IsReadOnly = true;
            ApplyEditorDefaults(_orig, "orig (FindControls)");
        }
        else Log("ERROR: Could not find EditorOrigXml (TextEditor). Check XAML Name=EditorOrigXml.");

        if (_tran != null)
        {
            _tran.IsReadOnly = false;
            ApplyEditorDefaults(_tran, "tran (FindControls)");
        }
        else Log("ERROR: Could not find EditorTranXml (TextEditor). Check XAML Name=EditorTranXml.");

        // Find UI
        _findBar = this.FindControl<Border>("FindBar");
        _findQuery = this.FindControl<TextBox>("FindQuery");
        _findCount = this.FindControl<TextBlock>("FindCount");
        _findScope = this.FindControl<TextBlock>("FindScope");
        _btnPrev = this.FindControl<Button>("BtnPrev");
        _btnNext = this.FindControl<Button>("BtnNext");
        _btnCloseFind = this.FindControl<Button>("BtnCloseFind");

        Log($"Find UI: bar={_findBar != null}, query={_findQuery != null}, count={_findCount != null}");

        Log("FindControls end");
    }

    /// <summary>
    /// Make AvaloniaEdit editors always visible + interactive.
    /// </summary>
    private void ApplyEditorDefaults(TextEditor? ed, string tag)
    {
        if (ed == null) return;

        try
        {
            ed.Focusable = true;
            ed.IsHitTestVisible = true;
            ed.IsEnabled = true;

            ed.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            ed.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            ed.Background ??= Brushes.Transparent;
            ed.TextArea.Background ??= Brushes.Transparent;

            ed.Foreground = SafeBrushOrFallback(ed.Foreground, Brushes.White);
            ed.TextArea.Caret.CaretBrush = SafeBrushOrFallback(ed.TextArea.Caret.CaretBrush, Brushes.White);
            ed.TextArea.SelectionBrush = SafeBrushOrFallback(
                ed.TextArea.SelectionBrush,
                new SolidColorBrush(Color.FromArgb(80, 120, 160, 255)));

            Log($"ApplyEditorDefaults {tag}: Bg={BrushStr(ed.Background)} TA.Bg={BrushStr(ed.TextArea.Background)} Fg={BrushStr(ed.Foreground)} Bounds={ed.Bounds.Width}x{ed.Bounds.Height}");
        }
        catch (Exception ex)
        {
            Log($"ApplyEditorDefaults {tag} ERROR: {ex}");
        }
    }

    private void WireEvents()
    {
        Log("WireEvents start");

        if (_btnCopyPrompt != null) _btnCopyPrompt.Click += async (_, _) => await CopySelectionWithPromptAsync();
        if (_btnPasteReplace != null) _btnPasteReplace.Click += async (_, _) => await PasteReplaceSelectionAsync();

        if (_btnSaveTranslated != null) _btnSaveTranslated.Click += async (_, _) => await SaveIfValidAsync();
        if (_btnSelectNext50Tags != null) _btnSelectNext50Tags.Click += async (_, _) => await SelectNextTagsAsync(100);
        if (_btnCheckXml != null) _btnCheckXml.Click += async (_, _) => await CheckXmlWithPopupAsync();

        HookEditorDebugInput(_orig, "orig");
        HookEditorDebugInput(_tran, "tran");

        if (_tran != null)
        {
            _tran.TextArea.SelectionChanged += (_, _) => RememberSelectionIfAny();
            _tran.TextArea.Caret.PositionChanged += (_, _) => { _lastUserInputUtc = DateTime.UtcNow; _lastUserInputEditor = _tran; };
        }

        if (_orig != null)
            _orig.TextArea.Caret.PositionChanged += (_, _) => { _lastUserInputUtc = DateTime.UtcNow; _lastUserInputEditor = _orig; };

        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

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

        if (_orig != null)
        {
            _orig.GotFocus += (_, _) =>
            {
                Log("orig GotFocus (switch find target if open)");
                if (_findBar?.IsVisible == true)
                    SetFindTarget(_orig, preserveIndex: true);
            };
        }

        if (_tran != null)
        {
            _tran.GotFocus += (_, _) =>
            {
                Log("tran GotFocus (switch find target if open)");
                if (_findBar?.IsVisible == true)
                    SetFindTarget(_tran, preserveIndex: true);
            };
        }

        Log("WireEvents end");
    }

    private void HookEditorDebugInput(TextEditor? ed, string tag)
    {
        if (ed == null) return;

        ed.PointerPressed += (_, e) =>
        {
            Log($"{tag} PointerPressed: pos={e.GetPosition(ed)} HitTest={ed.IsHitTestVisible} Opacity={ed.Opacity}");
            ApplyEditorDefaults(ed, $"{tag} (PointerPressed)");
        };

        ed.PointerReleased += (_, _) => Log($"{tag} PointerReleased");
        ed.KeyDown += (_, e) => Log($"{tag} KeyDown: {e.Key} mods={e.KeyModifiers}");
        ed.TextArea.TextEntered += (_, e) => Log($"{tag} TextEntered: '{e.Text}' caretOffset={ed.TextArea.Caret.Offset}");
        ed.GotFocus += (_, _) =>
        {
            Log($"{tag} GotFocus: Bounds={ed.Bounds.Width}x{ed.Bounds.Height}");
            ApplyEditorDefaults(ed, $"{tag} (GotFocus)");
        };
    }

    private void SetupHoverDictionary()
    {
        if (_orig == null || _tran == null)
        {
            Log("SetupHoverDictionary: editors null");
            return;
        }

        try
        {
            _hoverDictOrig?.Dispose();
            _hoverDictTran?.Dispose();

            _hoverDictOrig = new HoverDictionaryBehaviorEdit(_orig, _cedict);
            _hoverDictTran = new HoverDictionaryBehaviorEdit(_tran, _cedict);

            Log("SetupHoverDictionary: attached to orig+tran");
        }
        catch (Exception ex)
        {
            Log("SetupHoverDictionary failed: " + ex);
        }
    }

    private void DisposeHoverDictionary()
    {
        Log("DisposeHoverDictionary");
        _hoverDictOrig?.Dispose();
        _hoverDictOrig = null;
        _hoverDictTran?.Dispose();
        _hoverDictTran = null;
    }

    private void RememberSelectionIfAny()
    {
        if (_tran == null) return;

        try
        {
            var sel = _tran.TextArea.Selection;
            if (sel != null && !sel.IsEmpty)
            {
                int s = sel.SurroundingSegment.Offset;
                int e = s + sel.SurroundingSegment.Length;

                _lastCopyStart = s;
                _lastCopyEnd = e;

                Log($"RememberSelectionIfAny: {s}..{e} (len={e - s})");
            }
        }
        catch (Exception ex)
        {
            Log("RememberSelectionIfAny error: " + ex);
        }
    }

    // --------------------------
    // Find highlight renderer attach/detach
    // --------------------------

    private void EnsureFindRenderersAttached()
    {
        AttachRendererIfMissing(_orig, ref _hlOrig, "orig");
        AttachRendererIfMissing(_tran, ref _hlTran, "tran");
    }

    private void AttachRendererIfMissing(TextEditor? ed, ref SearchHighlightRenderer? renderer, string tag)
    {
        if (ed == null) return;

        try
        {
            var tv = ed.TextArea?.TextView;
            if (tv == null)
            {
                Log($"AttachRendererIfMissing({tag}): TextView null (too early).");
                return;
            }

            renderer ??= new SearchHighlightRenderer(tv);

            if (!tv.BackgroundRenderers.Contains(renderer))
            {
                tv.BackgroundRenderers.Add(renderer);
                Log($"AttachRendererIfMissing({tag}): attached SearchHighlightRenderer.");
            }
        }
        catch (Exception ex)
        {
            Log($"AttachRendererIfMissing({tag}) ERROR: {ex}");
        }
    }

    private void DetachFindRenderers()
    {
        DetachRenderer(_orig, ref _hlOrig, "orig");
        DetachRenderer(_tran, ref _hlTran, "tran");
    }

    private void DetachRenderer(TextEditor? ed, ref SearchHighlightRenderer? renderer, string tag)
    {
        if (ed == null || renderer == null) return;

        try
        {
            var tv = ed.TextArea?.TextView;
            if (tv != null && tv.BackgroundRenderers.Contains(renderer))
            {
                tv.BackgroundRenderers.Remove(renderer);
                Log($"DetachRenderer({tag}): removed SearchHighlightRenderer.");
            }
        }
        catch (Exception ex)
        {
            Log($"DetachRenderer({tag}) ERROR: {ex}");
        }
        finally
        {
            renderer = null;
        }
    }

    // --------------------------
    // Public API
    // --------------------------

    public void Clear()
    {
        Log("Clear() called");

        _cachedOrigXml = "";
        _cachedTranXml = "";

        if (_orig != null) SetEditorText(_orig, "", "Clear(orig)");
        if (_tran != null) SetEditorText(_tran, "", "Clear(tran)");

        _lastCopyStart = -1;
        _lastCopyEnd = -1;

        ResetNavigationState();
        UpdateHint("Select a file to edit XML.");

        ClearFindState();
        CloseFind();
    }

    public void SetXml(string originalXml, string translatedXml)
    {
        Log($"SetXml called: origLen={originalXml?.Length ?? 0}, tranLen={translatedXml?.Length ?? 0}");

        _cachedOrigXml = originalXml ?? "";
        _cachedTranXml = translatedXml ?? "";

        if (_orig == null || _tran == null)
        {
            Log("SetXml: ERROR - editors are null.");
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            ApplyEditorDefaults(_orig, "orig (SetXml before set)");
            ApplyEditorDefaults(_tran, "tran (SetXml before set)");

            EnsureFindRenderersAttached();

            SetEditorText(_orig, _cachedOrigXml, "SetXml(orig)");
            SetEditorText(_tran, _cachedTranXml, "SetXml(tran)");

            _lastCopyStart = -1;
            _lastCopyEnd = -1;

            ResetNavigationState();
            UpdateHint("Tip: select a chunk in Translated XML → Copy selection + prompt.");

            if (_findBar?.IsVisible == true)
                RecomputeMatches(resetToFirst: false);
        }, DispatcherPriority.Normal);
    }

    public string GetTranslatedXml()
    {
        var t = _tran?.Text ?? "";
        Log($"GetTranslatedXml -> len={t.Length}");
        return t;
    }

    private void ReApplyEditorsText(string reason)
    {
        Log($"ReApplyEditorsText: {reason} cachedOrigLen={_cachedOrigXml.Length} cachedTranLen={_cachedTranXml.Length}");

        EnsureFindRenderersAttached();

        if (_orig != null)
        {
            ApplyEditorDefaults(_orig, "orig (ReApply)");
            SetEditorText(_orig, _cachedOrigXml, "ReApply(orig)");
        }

        if (_tran != null)
        {
            ApplyEditorDefaults(_tran, "tran (ReApply)");
            SetEditorText(_tran, _cachedTranXml, "ReApply(tran)");
        }
    }

    private void SetEditorText(TextEditor editor, string value, string which)
    {
        value ??= "";

        Log($"{which}: SetEditorText start. target={editor.Name} currentLen={LenStr(editor.Text)} newLen={value.Length} IsVisible={editor.IsVisible} IsReadOnly={editor.IsReadOnly} Bounds={editor.Bounds.Width}x{editor.Bounds.Height}");
        Log($"{which}: Brushes before set: Fg={BrushStr(editor.Foreground)} Bg={BrushStr(editor.Background)} TA.Bg={BrushStr(editor.TextArea.Background)}");

        editor.Text = value;

        Log($"{which}: SetEditorText after set. editor.TextLen={LenStr(editor.Text)}");

        Dispatcher.UIThread.Post(() =>
        {
            Log($"{which}: Post-check (Background). editor.TextLen={LenStr(editor.Text)} IsVisible={editor.IsVisible} Bounds={editor.Bounds.Width}x{editor.Bounds.Height}");
        }, DispatcherPriority.Background);
    }

    // ============================================================
    // COMMUNITY NOTES (FIX)
    // ============================================================
    //
    // ReadableTabView raises:
    //   CommunityNoteInsertRequested(xmlIndex, noteText, resp)
    //   CommunityNoteDeleteRequested(xmlStart, xmlEndExclusive)
    //
    // These MUST:
    //   1) modify the TRANSLATED FILE ON DISK (the one you reload from)
    //   2) verify disk contains the modified text
    //   3) reload BOTH orig+tran from disk and call SetXml(orig, tran)
    //
    // Parent should call these from the events.
    // ============================================================

    public async Task HandleCommunityNoteInsertAsync(int xmlIndex, string noteText, string? resp)
    {
        await _saveReloadGate.WaitAsync();
        try
        {
            if (!TryValidatePaths(out var origPath, out var tranPath))
                return;

            Log($"COMM-INSERT start xmlIndex={xmlIndex} textLen={(noteText ?? "").Length} resp='{resp ?? ""}'");

            var beforeDisk = await ReadAllTextUtf8Async(tranPath);
            Log($"COMM-INSERT disk BEFORE len={beforeDisk.Length} sha1={Sha1Short(beforeDisk)} mtime={SafeMTime(tranPath)}");

            var updated = InsertCommunityNote(beforeDisk, xmlIndex, noteText, resp, out var why);
            if (updated == null)
            {
                Status?.Invoke(this, "Add note failed: " + why);
                Log("COMM-INSERT FAILED: " + why);
                return;
            }

            await AtomicWriteUtf8Async(tranPath, updated);

            var afterDisk = await ReadAllTextUtf8Async(tranPath);
            Log($"COMM-INSERT disk AFTER  len={afterDisk.Length} sha1={Sha1Short(afterDisk)} mtime={SafeMTime(tranPath)} matchLen={(afterDisk.Length == updated.Length)}");

            if (afterDisk.Length != updated.Length)
            {
                Status?.Invoke(this, "Add note FAILED: disk write mismatch (wrong path or overwritten). Check logs.");
                Log("COMM-INSERT HARD FAIL: disk length mismatch after write.");
                return;
            }

            // Reload both from disk, always.
            var origDisk = await ReadAllTextUtf8Async(origPath);
            var tranDisk = afterDisk;

            Log($"COMM-INSERT reload: origLen={origDisk.Length} tranLen={tranDisk.Length}");
            SetXml(origDisk, tranDisk);

            Status?.Invoke(this, "Community note added (saved to file).");
        }
        catch (Exception ex)
        {
            Log("COMM-INSERT EXCEPTION: " + ex);
            Status?.Invoke(this, "Add note failed (exception). See debug log.");
        }
        finally
        {
            _saveReloadGate.Release();
        }
    }

    public async Task HandleCommunityNoteDeleteAsync(int xmlStart, int xmlEndExclusive)
    {
        await _saveReloadGate.WaitAsync();
        try
        {
            if (!TryValidatePaths(out var origPath, out var tranPath))
                return;

            Log($"COMM-DELETE start xmlStart={xmlStart} xmlEndEx={xmlEndExclusive}");

            var beforeDisk = await ReadAllTextUtf8Async(tranPath);
            Log($"COMM-DELETE disk BEFORE len={beforeDisk.Length} sha1={Sha1Short(beforeDisk)} mtime={SafeMTime(tranPath)}");

            var updated = DeleteRange(beforeDisk, xmlStart, xmlEndExclusive, out var why);
            if (updated == null)
            {
                Status?.Invoke(this, "Delete note failed: " + why);
                Log("COMM-DELETE FAILED: " + why);
                return;
            }

            await AtomicWriteUtf8Async(tranPath, updated);

            var afterDisk = await ReadAllTextUtf8Async(tranPath);
            Log($"COMM-DELETE disk AFTER  len={afterDisk.Length} sha1={Sha1Short(afterDisk)} mtime={SafeMTime(tranPath)} matchLen={(afterDisk.Length == updated.Length)}");

            if (afterDisk.Length != updated.Length)
            {
                Status?.Invoke(this, "Delete note FAILED: disk write mismatch (wrong path or overwritten). Check logs.");
                Log("COMM-DELETE HARD FAIL: disk length mismatch after write.");
                return;
            }

            // Reload both from disk, always.
            var origDisk = await ReadAllTextUtf8Async(origPath);
            var tranDisk = afterDisk;

            Log($"COMM-DELETE reload: origLen={origDisk.Length} tranLen={tranDisk.Length}");
            SetXml(origDisk, tranDisk);

            Status?.Invoke(this, "Community note deleted (saved to file).");
        }
        catch (Exception ex)
        {
            Log("COMM-DELETE EXCEPTION: " + ex);
            Status?.Invoke(this, "Delete note failed (exception). See debug log.");
        }
        finally
        {
            _saveReloadGate.Release();
        }
    }

    private bool TryValidatePaths(out string origPath, out string tranPath)
    {
        origPath = _currentOrigPath ?? "";
        tranPath = _currentTranPath ?? "";

        if (string.IsNullOrWhiteSpace(origPath) || string.IsNullOrWhiteSpace(tranPath))
        {
            Status?.Invoke(this, "Paths not set. Call SetCurrentFilePaths(...) when loading a file.");
            Log("PATHS INVALID: SetCurrentFilePaths was not called.");
            return false;
        }

        bool o = File.Exists(origPath);
        bool t = File.Exists(tranPath);

        Log($"PATHS: orig='{origPath}' exists={o} | tran='{tranPath}' exists={t}");

        if (!o || !t)
        {
            Status?.Invoke(this, "File not found on disk (orig or tran). Check logs.");
            return false;
        }

        return true;
    }

    private static string SafeMTime(string path)
    {
        try { return File.GetLastWriteTimeUtc(path).ToString("O"); }
        catch { return "mtime_err"; }
    }

    private static async Task<string> ReadAllTextUtf8Async(string path)
    {
        // Explicit UTF-8 (no BOM) behavior consistent across platforms
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), detectEncodingFromByteOrderMarks: true);
        return await sr.ReadToEndAsync();
    }

    private static async Task AtomicWriteUtf8Async(string path, string content)
    {
        // Write to temp in same dir, then replace. Avoid partial writes and races.
        var dir = Path.GetDirectoryName(path) ?? "";
        var file = Path.GetFileName(path);
        var tmp = Path.Combine(dir, file + ".tmp_" + Guid.NewGuid().ToString("N"));

        var enc = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        await File.WriteAllTextAsync(tmp, content, enc);

        // Prefer Replace when possible, fall back to Move overwrite.
        try
        {
            // If no backup needed, still safe.
            File.Replace(tmp, path, destinationBackupFileName: null, ignoreMetadataErrors: true);
        }
        catch
        {
            try
            {
#if NET8_0_OR_GREATER
                File.Move(tmp, path, overwrite: true);
#else
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
#endif
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            }
        }
    }

    private static string EscapeXmlText(string s)
        => (s ?? "")
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");

    private static string EscapeXmlAttr(string s)
        => EscapeXmlText(s).Replace("\"", "&quot;").Replace("'", "&apos;");

    private static string? InsertCommunityNote(string xml, int index, string noteText, string? resp, out string why)
    {
        why = "";
        if (xml == null) { why = "xml is null"; return null; }
        if (index < 0 || index > xml.Length) { why = $"index out of range: {index} (len={xml.Length})"; return null; }

        noteText = (noteText ?? "").Trim();
        if (noteText.Length == 0) { why = "note text empty"; return null; }

        // Minimal TEI-ish note:
        // <note type="community" resp="...">TEXT</note>
        var attrs = " type=\"community\"";
        if (!string.IsNullOrWhiteSpace(resp))
            attrs += $" resp=\"{EscapeXmlAttr(resp.Trim())}\"";

        var note = $"<note{attrs}>{EscapeXmlText(noteText)}</note>";

        // Insert at exact character index.
        // This assumes the provided xmlIndex is meant to be a position in the translated XML string.
        // If upstream gives tag-boundary-safe indices, great. If not, you’ll see broken XML and your checker will scream.
        var sb = new StringBuilder(xml.Length + note.Length);
        sb.Append(xml, 0, index);
        sb.Append(note);
        sb.Append(xml, index, xml.Length - index);

        return sb.ToString();
    }

    private static string? DeleteRange(string xml, int start, int endExclusive, out string why)
    {
        why = "";
        if (xml == null) { why = "xml is null"; return null; }
        if (start < 0 || endExclusive < 0) { why = $"negative range: {start}..{endExclusive}"; return null; }
        if (endExclusive < start) { why = $"endExclusive < start: {start}..{endExclusive}"; return null; }
        if (start > xml.Length || endExclusive > xml.Length) { why = $"range out of bounds for len={xml.Length}: {start}..{endExclusive}"; return null; }
        if (endExclusive == start) { why = "empty range"; return null; }

        // Delete exactly the range we were told.
        // ReadableTabView’s IsCommunityAnnotation() expects xmlStart/xmlEndExclusive to cover the <note ...>...</note>.
        return xml.Remove(start, endExclusive - start);
    }

    // --------------------------
    // Clipboard workflow
    // --------------------------

    private async Task CopySelectionWithPromptAsync()
    {
        Log("CopySelectionWithPromptAsync start");

        if (_tran == null)
        {
            Status?.Invoke(this, "Translated XML editor not available.");
            return;
        }

        var text = _tran.Text ?? "";
        if (text.Length == 0)
        {
            Status?.Invoke(this, "Translated XML is empty.");
            return;
        }

        int start = -1;
        int end = -1;

        try
        {
            var sel = _tran.TextArea.Selection;
            if (sel != null && !sel.IsEmpty)
            {
                start = sel.SurroundingSegment.Offset;
                end = start + sel.SurroundingSegment.Length;
            }
        }
        catch { }

        if (end <= start && _lastCopyEnd > _lastCopyStart)
        {
            start = _lastCopyStart;
            end = _lastCopyEnd;
        }

        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, 0, text.Length);

        if (end <= start)
        {
            Status?.Invoke(this, "No selection. Select an XML fragment first.");
            return;
        }

        string selectionXml = text.Substring(start, end - start);

        _lastCopyStart = start;
        _lastCopyEnd = end;

        string clipboardPayload = BuildChatGptPrompt(selectionXml);

        var clipboard = GetClipboard();
        if (clipboard == null)
        {
            Status?.Invoke(this, "Clipboard not available (TopLevel.Clipboard is null).");
            return;
        }

        await clipboard.SetTextAsync(clipboardPayload);
        Status?.Invoke(this, $"Copied selection + prompt ({selectionXml.Length:n0} chars) to clipboard.");
    }

    private async Task PasteReplaceSelectionAsync()
    {
        if (_tran == null)
        {
            Status?.Invoke(this, "Translated XML editor not available.");
            return;
        }

        var clipboard = GetClipboard();
        if (clipboard == null)
        {
            Status?.Invoke(this, "Clipboard not available (TopLevel.Clipboard is null).");
            return;
        }

        var clipText = await clipboard.TryGetTextAsync() ?? "";
        clipText = clipText.Trim();

        if (clipText.Length == 0)
        {
            Status?.Invoke(this, "Clipboard is empty.");
            return;
        }

        string pastedXml = ExtractXmlFromClipboard(clipText);
        if (string.IsNullOrWhiteSpace(pastedXml))
        {
            Status?.Invoke(this, "Could not find XML in clipboard text (no ```xml``` block, and content wasn't raw XML).");
            return;
        }

        int start = -1;
        int end = -1;

        try
        {
            var sel = _tran.TextArea.Selection;
            if (sel != null && !sel.IsEmpty)
            {
                start = sel.SurroundingSegment.Offset;
                end = start + sel.SurroundingSegment.Length;
            }
        }
        catch { }

        if (end <= start)
        {
            if (_lastCopyEnd > _lastCopyStart)
            {
                start = _lastCopyStart;
                end = _lastCopyEnd;
            }
            else
            {
                Status?.Invoke(this, "No active selection (and no remembered copy range). Select where to paste.");
                return;
            }
        }

        var all = _tran.Text ?? "";
        start = Math.Clamp(start, 0, all.Length);
        end = Math.Clamp(end, 0, all.Length);
        if (end < start) (start, end) = (end, start);

        var sb = new StringBuilder(all.Length - (end - start) + pastedXml.Length);
        sb.Append(all, 0, start);
        sb.Append(pastedXml);
        sb.Append(all, end, all.Length - end);

        _tran.Text = sb.ToString();

        try
        {
            _tran.TextArea.Selection = Selection.Create(_tran.TextArea, start, start + pastedXml.Length);
            _tran.TextArea.Caret.Offset = start;
        }
        catch { }

        _lastCopyStart = start;
        _lastCopyEnd = start + pastedXml.Length;

        Status?.Invoke(this, $"Pasted & replaced selection with {pastedXml.Length:n0} chars.");
    }

    // --------------------------
    // Ctrl+F Find UI
    // --------------------------

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

        EnsureFindRenderersAttached();

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
        if (_orig == null || _tran == null)
            return _tran;

        bool recentInput = (DateTime.UtcNow - _lastUserInputUtc).TotalMilliseconds <= UserInputPriorityWindowMs;
        if (recentInput && _lastUserInputEditor != null)
            return _lastUserInputEditor;

        if (_tran.IsFocused || _tran.IsKeyboardFocusWithin) return _tran;
        if (_orig.IsFocused || _orig.IsKeyboardFocusWithin) return _orig;

        return _tran;
    }

    private void SetFindTarget(TextEditor? ed, bool preserveIndex)
    {
        if (ed == null) return;

        _findTarget = ed;

        if (_findScope != null)
            _findScope.Text = ReferenceEquals(ed, _orig) ? "Find (Original):" : "Find (Translated):";

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
            int caret = 0;
            try { caret = ed.TextArea.Caret.Offset; } catch { }
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
            _findTarget.TextArea.Caret.Offset = Math.Clamp(start, 0, (_findTarget.Text ?? "").Length);

            Dispatcher.UIThread.Post(() =>
            {
                try { CenterByCaret(_findTarget); } catch { }
                ApplyHighlight(_findTarget, start, len);
            }, DispatcherPriority.Background);
        }
        catch { }
    }

    private void ApplyHighlight(TextEditor target, int start, int len)
    {
        EnsureFindRenderersAttached();

        try
        {
            if (ReferenceEquals(target, _orig))
            {
                _hlTran?.Clear();
                _hlOrig?.SetRange(start, len);
            }
            else
            {
                _hlOrig?.Clear();
                _hlTran?.SetRange(start, len);
            }

            target.TextArea?.TextView?.InvalidateVisual();
        }
        catch (Exception ex)
        {
            Log("ApplyHighlight renderer error: " + ex.Message);
        }
    }

    private void ClearHighlight()
    {
        try { _hlOrig?.Clear(); } catch { }
        try { _hlTran?.Clear(); } catch { }
        try
        {
            _orig?.TextArea?.TextView?.InvalidateVisual();
            _tran?.TextArea?.TextView?.InvalidateVisual();
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

    // --------------------------
    // Helpers
    // --------------------------

    private IClipboard? GetClipboard()
    {
        var top = TopLevel.GetTopLevel(this);
        return top?.Clipboard;
    }

    private void UpdateHint(string s)
    {
        if (_txtHint != null) _txtHint.Text = s;
    }

    private static string BuildChatGptPrompt(string selectionXml)
    {
        return
$@"You are an XML-preserving translator.

ABSOLUTE TARGET LANGUAGE:
- Translate into ENGLISH ONLY.
- Do NOT rewrite or paraphrase text that is already in English; keep existing English EXACTLY as-is.

NON-NEGOTIABLE RULES (STRICT SPEC):

1) XML STRUCTURE MUST BE PRESERVED EXACTLY
   - Every start tag, end tag, self-closing tag, attribute name/value, namespace prefix,
     entity reference, CDATA marker, processing instruction, and tag order must remain IDENTICAL.
   - Do NOT add, remove, reorder, rename, or reformat any tags or attributes.
   - Do NOT move text across tag boundaries.
   - Do NOT duplicate the fragment or append a second copy.
   - Output must be well-formed XML.

2) TRANSLATE TEXT NODES ONLY (HUMAN-READABLE NATURAL LANGUAGE)
   - Translate ONLY natural-language text inside text nodes.
   - NEVER translate or modify:
     • tag names
     • attribute names or values
     • IDs, codes, catalog numbers, dates/times, line numbers, refs, witness marks (e.g., 【CB】), or other non-prose tokens
     • any text that is already English (leave it exactly unchanged)
   - You MUST translate ALL non-empty Chinese/Japanese/Korean (CJK) natural-language text that appears in text nodes.
   - You MAY smooth sentence flow across line-break tags (e.g. <lb/>, <pb/>) ONLY by choosing English wording that reads naturally,
     but you may NOT move text across tags. Punctuation is up to you as long as you make the text remain close to the original meaning.

3) WHITESPACE / PUNCTUATION PRESERVATION
   - Keep whitespace/newlines as close as possible to the input (do NOT rewrap or normalize).
   - Preserve the existing punctuation structure as long as you output readable English.
   - Do NOT add explanatory parentheses, glosses, or extra words like ""(i.e.)"".

4) SILENT INTERNAL SELF-CHECK (MANDATORY)
   - Check that the sequence and count of '<...>' XML tokens in your output EXACTLY matches the input.
   - Check that all attributes, namespaces, and processing instructions are unchanged.
   - Check that NO CJK characters remain in text nodes that should have been translated.
   - Check that you did NOT translate any existing English text.
   - If ANY check fails, output the ORIGINAL XML UNCHANGED (verbatim).

OUTPUT REQUIREMENTS:
- Output ONLY the XML fragment (exactly one copy).
- Put the entire output in ONE single ```xml code block``` and NOTHING ELSE.

XML fragment to translate:
```xml
{selectionXml}
```";
    }

    private static string ExtractXmlFromClipboard(string clipboardText)
    {
        var m = Regex.Match(
            clipboardText,
            @"```(?:xml)?\s*(?<xml>[\s\S]*?)\s*```",
            RegexOptions.IgnoreCase);

        if (m.Success)
            return m.Groups["xml"].Value.Trim();

        return clipboardText.Trim();
    }

    private static readonly Regex XmlTagRegex = new Regex(@"<[^>]+>", RegexOptions.Compiled);

    private async Task SelectNextTagsAsync(int tagCount)
    {
        if (_tran == null)
        {
            Status?.Invoke(this, "Translated XML editor not available.");
            return;
        }

        var text = _tran.Text ?? "";
        if (text.Length == 0)
        {
            Status?.Invoke(this, "Translated XML is empty.");
            return;
        }

        int start;

        try
        {
            var sel = _tran.TextArea.Selection;
            if (sel != null && !sel.IsEmpty)
            {
                start = sel.SurroundingSegment.Offset + sel.SurroundingSegment.Length;
                _lastCopyStart = sel.SurroundingSegment.Offset;
                _lastCopyEnd = start;
            }
            else if (_lastCopyEnd > _lastCopyStart) start = _lastCopyEnd;
            else start = _tran.TextArea.Caret.Offset;
        }
        catch
        {
            start = _lastCopyEnd > _lastCopyStart ? _lastCopyEnd : 0;
        }

        start = Math.Clamp(start, 0, text.Length);

        var matches = XmlTagRegex.Matches(text, start);
        if (matches.Count == 0)
        {
            await ShowInfoPopupAsync("End reached", "No more XML tags found after the current position.");
            return;
        }

        int take = Math.Min(tagCount, matches.Count);
        var last = matches[take - 1];

        int end = last.Index + last.Length;
        end = Math.Clamp(end, 0, text.Length);

        end = ExtendToNextNewline(text, end);

        if (end <= start)
        {
            await ShowInfoPopupAsync("Selection failed", "Could not compute a valid selection range.");
            return;
        }

        try
        {
            _tran.Focus();
            _tran.TextArea.Selection = Selection.Create(_tran.TextArea, start, end);
            _tran.TextArea.Caret.Offset = start;
        }
        catch { }

        _lastCopyStart = start;
        _lastCopyEnd = end;

        Status?.Invoke(this, $"Selected {take} tag(s) + newline boundary ({end - start:n0} chars).");
    }

    private static int ExtendToNextNewline(string text, int end)
    {
        if (string.IsNullOrEmpty(text)) return end;
        end = Math.Clamp(end, 0, text.Length);
        if (end >= text.Length) return text.Length;

        const int MaxScan = 4000;
        int scanLimit = Math.Min(text.Length, end + MaxScan);

        for (int i = end; i < scanLimit; i++)
        {
            if (text[i] == '\n')
                return i + 1;
        }

        int j = end;
        while (j < text.Length && (text[j] == ' ' || text[j] == '\t' || text[j] == '\r'))
            j++;

        return j;
    }

    // --------------------------
    // Hacky XML check (no parser)
    // --------------------------

    private static readonly Regex CommunityNoteBlockRegex = new Regex(
        @"<note\b(?<attrs>[^>]*)\btype\s*=\s*""community""(?<attrs2>[^>]*)>(?<inner>[\s\S]*?)</note>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static string StripCommunityNotes(string xml)
    {
        if (string.IsNullOrEmpty(xml)) return xml ?? string.Empty;
        return CommunityNoteBlockRegex.Replace(xml, "");
    }

    private static readonly Regex LbTagRegex = new Regex(
        @"<lb\b(?<attrs>[^>]*)\/?>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AttrRegex = new Regex(
        @"\b(?<name>n|ed)\s*=\s*""(?<val>[^""]*)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static (int totalLb, Dictionary<string, int> sigCounts) CollectLbSignatures(string xml)
    {
        int total = 0;
        var dict = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (Match m in LbTagRegex.Matches(xml))
        {
            total++;

            string attrs = m.Groups["attrs"].Value;

            string? nVal = null;
            string? edVal = null;

            foreach (Match am in AttrRegex.Matches(attrs))
            {
                var name = am.Groups["name"].Value;
                var val = am.Groups["val"].Value;

                if (name.Equals("n", StringComparison.OrdinalIgnoreCase)) nVal = val;
                else if (name.Equals("ed", StringComparison.OrdinalIgnoreCase)) edVal = val;
            }

            string sig = $"n={nVal ?? "<missing>"}|ed={edVal ?? "<missing>"}";

            if (dict.TryGetValue(sig, out int c)) dict[sig] = c + 1;
            else dict[sig] = 1;
        }

        return (total, dict);
    }

    private static (bool ok, string message, int origTags, int tranTags, int origLb, int tranLb) VerifyXmlHacky(string orig, string tran)
    {
        if (string.IsNullOrEmpty(orig))
            return (false, "Original XML is empty. Nothing to compare.", 0, 0, 0, 0);

        tran ??= "";

        string tranStripped = StripCommunityNotes(tran);

        int origTagCount = XmlTagRegex.Matches(orig).Count;
        int tranTagCount = XmlTagRegex.Matches(tran).Count;
        int tranTagCountStripped = XmlTagRegex.Matches(tranStripped).Count;

        var (origLbTotal, origSigs) = CollectLbSignatures(orig);
        var (tranLbTotal, tranSigs) = CollectLbSignatures(tranStripped);

        var missing = origSigs.Keys.Where(k => !tranSigs.ContainsKey(k)).ToList();
        var extra = tranSigs.Keys.Where(k => !origSigs.ContainsKey(k)).ToList();

        var countDiffs = new List<string>();
        foreach (var k in origSigs.Keys.Intersect(tranSigs.Keys))
        {
            int a = origSigs[k];
            int b = tranSigs[k];
            if (a != b)
                countDiffs.Add($"{k}  original={a}  translated={b}");
        }

        var problems = new List<string>();

        if (origTagCount != tranTagCountStripped)
            problems.Add(
                $"TAG COUNT MISMATCH (ignoring community notes):\n" +
                $"  original={origTagCount:n0}\n" +
                $"  translated_stripped={tranTagCountStripped:n0}\n" +
                $"  translated_raw={tranTagCount:n0}");

        if (origLbTotal != tranLbTotal)
            problems.Add($"LB TOTAL MISMATCH:\n  original={origLbTotal:n0}\n  translated={tranLbTotal:n0}");

        if (missing.Count > 0)
            problems.Add($"MISSING <lb> SIGNATURES in translated: {missing.Count:n0}\n(showing up to 15)\n- {string.Join("\n- ", missing.Take(15))}");

        if (extra.Count > 0)
            problems.Add($"EXTRA <lb> SIGNATURES in translated: {extra.Count:n0}\n(showing up to 15)\n- {string.Join("\n- ", extra.Take(15))}");

        if (countDiffs.Count > 0)
            problems.Add($"<lb> SIGNATURE COUNT DIFFERENCES: {countDiffs.Count:n0}\n(showing up to 15)\n- {string.Join("\n- ", countDiffs.Take(15))}");

        if (problems.Count == 0)
        {
            int removed = tranTagCount - tranTagCountStripped;

            string okMsg =
                $"OK ✅\n\n" +
                $"Tag count matches (ignoring community notes): {tranTagCountStripped:n0}\n" +
                $"<lb> count matches: {tranLbTotal:n0}\n" +
                $"All <lb n=... ed=...> signatures match.\n" +
                (removed > 0 ? $"\nCommunity-note tags ignored during check: {removed:n0}\n" : "\n") +
                $"(Hacky structural check only; not a full XML validator.)";

            return (true, okMsg, origTagCount, tranTagCount, origLbTotal, tranLbTotal);
        }

        return (false, string.Join("\n\n", problems), origTagCount, tranTagCount, origLbTotal, tranLbTotal);
    }

    private async Task<bool> EnsureXmlOkOrWarnAsync(bool showOkPopup)
    {
        if (_orig == null || _tran == null)
        {
            Status?.Invoke(this, "Editors not available.");
            if (showOkPopup)
                await ShowInfoPopupAsync("Check XML", "Editors not available.");
            return false;
        }

        var orig = _orig.Text ?? "";
        var tran = _tran.Text ?? "";

        var (ok, msg, _, tranTags, _, tranLb) = VerifyXmlHacky(orig, tran);

        if (ok)
        {
            Status?.Invoke(this, $"XML check OK: tags={tranTags:n0}, lb={tranLb:n0} (n/ed preserved).");
            if (showOkPopup)
                await ShowInfoPopupAsync("Check XML", msg);
            return true;
        }

        Status?.Invoke(this, "XML check failed (see popup).");
        await ShowInfoPopupAsync("Check XML (hacky)", msg);
        return false;
    }

    private Task CheckXmlWithPopupAsync() => EnsureXmlOkOrWarnAsync(showOkPopup: true);

    private async Task SaveIfValidAsync()
    {
        if (!await EnsureXmlOkOrWarnAsync(showOkPopup: false))
            return;

        SaveRequested?.Invoke(this, EventArgs.Empty);
    }

    // --------------------------
    // Popup (awaited)
    // --------------------------

    private async Task ShowInfoPopupAsync(string title, string message)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;

        var ok = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 80
        };

        var text = new TextBox
        {
            Text = message,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            Height = 240
        };

        ScrollViewer.SetVerticalScrollBarVisibility(text, ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(text, ScrollBarVisibility.Disabled);

        var panel = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 10
        };

        panel.Children.Add(text);
        panel.Children.Add(ok);

        var win = new Window
        {
            Title = title,
            Width = 700,
            Height = 380,
            Content = panel,
            WindowStartupLocation = owner != null
                ? WindowStartupLocation.CenterOwner
                : WindowStartupLocation.CenterScreen
        };

        if (owner != null)
        {
            ok.Click += (_, _) => win.Close();
            await win.ShowDialog(owner);
            return;
        }

        var tcs = new TaskCompletionSource<bool>();
        ok.Click += (_, _) => { win.Close(); tcs.TrySetResult(true); };
        win.Show();
        await tcs.Task;
    }

    private void ResetNavigationState()
    {
        if (_tran == null) return;

        _lastCopyStart = -1;
        _lastCopyEnd = -1;

        try
        {
            _tran.TextArea.Selection = Selection.Create(_tran.TextArea, 0, 0);
            _tran.TextArea.Caret.Offset = 0;
        }
        catch { }

        try { _tran.Focus(); } catch { }

        Log("ResetNavigationState done");
    }

    // --------------------------
    // Scroll helper for AvaloniaEdit
    // --------------------------
    private static void CenterByCaret(TextEditor ed)
    {
        var sv = ed.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (sv == null) return;

        double viewportH = sv.Viewport.Height;
        double extentH = sv.Extent.Height;
        if (double.IsNaN(viewportH) || double.IsInfinity(viewportH) || viewportH <= 0) return;

        var textView = ed.TextArea.TextView;
        if (textView == null) return;

        textView.EnsureVisualLines();

        var caretPos = ed.TextArea.Caret.Position;

        var loc = textView.GetVisualPosition(caretPos, AvaloniaEdit.Rendering.VisualYPosition.LineTop);
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
}
