// Views/TranslationTabView.axaml.cs
// Projection editor for IndexedTranslationService (Head / Body / Notes)

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using CbetaTranslator.App.Services;

namespace CbetaTranslator.App.Views;

public partial class TranslationTabView : UserControl
{
    private Button? _btnModeHead, _btnModeBody, _btnModeNotes;
    private Button? _btnCopyPrompt, _btnPasteReplace, _btnSave, _btnRevert;
    private CheckBox? _chkWrap;
    private TextBlock? _txtModeInfo;
    private TextEditor? _editor;

    private TranslationEditMode _currentMode = TranslationEditMode.Body;
    private string _currentProjection = "";

    // Optional file path display context (used by MainWindow)
    private string? _origPath;
    private string? _tranPath;

    public event EventHandler<TranslationEditMode>? ModeChanged;
    public event EventHandler? SaveRequested;
    public event EventHandler? RevertRequested;
    public event EventHandler<string>? Status;

    public TranslationTabView()
    {
        AvaloniaXamlLoader.Load(this);
        FindControls();
        WireEvents();
        ApplyWrap();
        UpdateModeInfo();
    }

    private void FindControls()
    {
        _btnModeHead = this.FindControl<Button>("BtnModeHead");
        _btnModeBody = this.FindControl<Button>("BtnModeBody");
        _btnModeNotes = this.FindControl<Button>("BtnModeNotes");

        _btnCopyPrompt = this.FindControl<Button>("BtnCopyPrompt");
        _btnPasteReplace = this.FindControl<Button>("BtnPasteReplace");
        _btnSave = this.FindControl<Button>("BtnSave");
        _btnRevert = this.FindControl<Button>("BtnRevert");

        _chkWrap = this.FindControl<CheckBox>("ChkWrap");
        _txtModeInfo = this.FindControl<TextBlock>("TxtModeInfo");
        _editor = this.FindControl<TextEditor>("EditorProjection");

        if (_editor != null)
        {
            _editor.Background ??= Brushes.Transparent;
            _editor.IsReadOnly = false;
            _editor.WordWrap = false;
            _editor.ShowLineNumbers = true;
        }
    }

    private void WireEvents()
    {
        if (_btnModeHead != null) _btnModeHead.Click += (_, _) => SwitchMode(TranslationEditMode.Head);
        if (_btnModeBody != null) _btnModeBody.Click += (_, _) => SwitchMode(TranslationEditMode.Body);
        if (_btnModeNotes != null) _btnModeNotes.Click += (_, _) => SwitchMode(TranslationEditMode.Notes);

        if (_btnCopyPrompt != null) _btnCopyPrompt.Click += async (_, _) => await CopySelectionWithPromptAsync();
        if (_btnPasteReplace != null) _btnPasteReplace.Click += async (_, _) => await PasteReplaceSelectionAsync();

        if (_btnSave != null)
            _btnSave.Click += (_, _) => SaveRequested?.Invoke(this, EventArgs.Empty);

        if (_btnRevert != null)
            _btnRevert.Click += (_, _) => RevertRequested?.Invoke(this, EventArgs.Empty);

        if (_chkWrap != null)
        {
            _chkWrap.Checked += (_, _) => ApplyWrap();
            _chkWrap.Unchecked += (_, _) => ApplyWrap();
        }

        AddHandler(KeyDownEvent, OnKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    // =========================
    // Public API used by MainWindow
    // =========================

    public void SetModeProjection(TranslationEditMode mode, string projectionText)
    {
        _currentMode = mode;
        _currentProjection = projectionText ?? "";

        if (_editor != null)
            _editor.Text = _currentProjection;

        UpdateModeInfo();
        UpdateModeButtons();
    }

    public string GetCurrentProjectionText()
        => _editor?.Text ?? _currentProjection ?? "";

    public void SetCurrentFilePaths(string originalPath, string translatedPath)
    {
        _origPath = originalPath;
        _tranPath = translatedPath;
        UpdateModeInfo();
    }

    // Compatibility helpers (so older MainWindow variants don't explode)
    public void SetXml(string originalXml, string translatedXml)
    {
        // If someone still calls SetXml, just dump translated text in editor.
        _currentProjection = translatedXml ?? "";
        if (_editor != null) _editor.Text = _currentProjection;
        UpdateModeInfo();
    }

    public string GetTranslatedXml() => GetCurrentProjectionText();
    public string GetTranslatedText() => GetCurrentProjectionText();
    public string GetTranslatedMarkdown() => GetCurrentProjectionText();

    public void Clear()
    {
        _currentProjection = "";
        _origPath = null;
        _tranPath = null;

        if (_editor != null)
            _editor.Text = "";

        UpdateModeInfo();
        UpdateModeButtons();
    }

    // =========================
    // UI helpers
    // =========================

    private void SwitchMode(TranslationEditMode mode)
    {
        if (_currentMode == mode) return;

        _currentMode = mode;
        UpdateModeInfo();
        UpdateModeButtons();

        // MainWindow will persist current mode text before switching modes.
        ModeChanged?.Invoke(this, mode);
    }

    private void UpdateModeButtons()
    {
        if (_btnModeHead != null) _btnModeHead.IsEnabled = _currentMode != TranslationEditMode.Head;
        if (_btnModeBody != null) _btnModeBody.IsEnabled = _currentMode != TranslationEditMode.Body;
        if (_btnModeNotes != null) _btnModeNotes.IsEnabled = _currentMode != TranslationEditMode.Notes;
    }

    private void UpdateModeInfo()
    {
        if (_txtModeInfo == null) return;

        var modeText = _currentMode switch
        {
            TranslationEditMode.Head => "Head of File",
            TranslationEditMode.Body => "Body of File",
            TranslationEditMode.Notes => "Notes",
            _ => "Translation Editor"
        };

        var fileLabel = string.IsNullOrWhiteSpace(_tranPath)
            ? ""
            : $" — {System.IO.Path.GetFileName(_tranPath)}";

        _txtModeInfo.Text = $"{modeText}{fileLabel}";
    }

    private void ApplyWrap()
    {
        if (_editor != null)
            _editor.WordWrap = _chkWrap?.IsChecked == true;
    }

    // =========================
    // Clipboard workflow
    // =========================

    private async Task CopySelectionWithPromptAsync()
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

        string selected = "";
        try
        {
            var sel = _editor.TextArea.Selection;
            if (sel != null && !sel.IsEmpty)
            {
                int s = sel.SurroundingSegment.Offset;
                int len = sel.SurroundingSegment.Length;
                if (s >= 0 && len >= 0 && s + len <= text.Length)
                    selected = text.Substring(s, len);
            }
        }
        catch { }

        if (string.IsNullOrWhiteSpace(selected))
        {
            Status?.Invoke(this, "No selection.");
            return;
        }

        var payload = BuildPrompt(selected);
        var cb = GetClipboard();
        if (cb == null)
        {
            Status?.Invoke(this, "Clipboard unavailable.");
            return;
        }

        await cb.SetTextAsync(payload);
        Status?.Invoke(this, "Copied selection + prompt.");
    }

    private async Task PasteReplaceSelectionAsync()
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

        var clip = (await cb.TryGetTextAsync())?.Trim() ?? "";
        if (clip.Length == 0)
        {
            Status?.Invoke(this, "Clipboard empty.");
            return;
        }

        var pasted = ExtractCodeBlockOrRaw(clip);

        try
        {
            var all = _editor.Text ?? "";
            var sel = _editor.TextArea.Selection;

            if (sel == null || sel.IsEmpty)
            {
                Status?.Invoke(this, "Select text to replace.");
                return;
            }

            int s = sel.SurroundingSegment.Offset;
            int e = s + sel.SurroundingSegment.Length;
            if (s < 0 || e < s || e > all.Length)
            {
                Status?.Invoke(this, "Invalid selection.");
                return;
            }

            var sb = new StringBuilder(all.Length - (e - s) + pasted.Length);
            sb.Append(all, 0, s);
            sb.Append(pasted);
            sb.Append(all, e, all.Length - e);

            _editor.Text = sb.ToString();
            _editor.TextArea.Selection = Selection.Create(_editor.TextArea, s, s + pasted.Length);

            Status?.Invoke(this, "Pasted over selection.");
        }
        catch (Exception ex)
        {
            Status?.Invoke(this, "Paste replace failed: " + ex.Message);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
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
        }
    }

    private IClipboard? GetClipboard()
    {
        var top = TopLevel.GetTopLevel(this);
        return top?.Clipboard;
    }

    private static string BuildPrompt(string selectedProjection)
    {
        return
$@"You are translating a CBETA projection block.

STRICT RULES:
- Edit ONLY EN: lines.
- Keep <n> and ZH: lines unchanged.
- Do not add commentary.
- Do not add, remove, or reorder blocks.
- Output only one markdown code block.

```markdown
{selectedProjection}
```";
    }

    private static string ExtractCodeBlockOrRaw(string text)
    {
        var m = Regex.Match(text, @"```(?:markdown|md|text)?\s*(?<x>[\s\S]*?)\s*```", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups["x"].Value.Trim() : text.Trim();
    }
}