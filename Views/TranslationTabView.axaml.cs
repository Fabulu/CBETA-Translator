// Views/TranslationTabView.axaml.cs
// Projection editor for IndexedTranslationService (Head / Body / Notes)

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Views;

public partial class TranslationTabView : UserControl
{
    private Button? _btnModeHead, _btnModeBody, _btnModeNotes;
    private Button? _btnUndo, _btnRedo;
    private Button? _btnCopyChunkPrompt, _btnPasteByNumber, _btnNextUntranslated, _btnSave, _btnRevert;
    private CheckBox? _chkWrap;
    private ComboBox? _cmbChunkSize;
    private TextBlock? _txtModeInfo;
    private TextBlock? _txtQuickInfo;
    private TextEditor? _editor;

    private TranslationEditMode _currentMode = TranslationEditMode.Body;
    private string _currentProjection = "";

    // Optional file path display context (used by MainWindow)
    private string? _origPath;
    private string? _tranPath;

    // Hover dictionary
    private bool _hoverDictionaryEnabled = true;
    private HoverDictionaryBehaviorEdit? _hoverDictionaryBehavior;
    private readonly ICedictDictionary _cedict = new CedictDictionaryService();

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
        ApplyHoverDictionarySetting();
    }

    private void FindControls()
    {
        _btnModeHead = this.FindControl<Button>("BtnModeHead");
        _btnModeBody = this.FindControl<Button>("BtnModeBody");
        _btnModeNotes = this.FindControl<Button>("BtnModeNotes");

        _btnUndo = this.FindControl<Button>("BtnUndo");
        _btnRedo = this.FindControl<Button>("BtnRedo");

        _btnCopyChunkPrompt = this.FindControl<Button>("BtnCopyChunkPrompt");
        _btnPasteByNumber = this.FindControl<Button>("BtnPasteByNumber");
        _btnNextUntranslated = this.FindControl<Button>("BtnNextUntranslated");
        _btnSave = this.FindControl<Button>("BtnSave");
        _btnRevert = this.FindControl<Button>("BtnRevert");

        _cmbChunkSize = this.FindControl<ComboBox>("CmbChunkSize");
        _chkWrap = this.FindControl<CheckBox>("ChkWrap");
        _txtModeInfo = this.FindControl<TextBlock>("TxtModeInfo");
        _txtQuickInfo = this.FindControl<TextBlock>("TxtQuickInfo");
        _editor = this.FindControl<TextEditor>("EditorProjection");

        if (_editor != null)
        {
            _editor.Background ??= Brushes.Transparent;
            _editor.IsReadOnly = false;
            _editor.WordWrap = false;
            _editor.ShowLineNumbers = true;
            _editor.TextChanged += (_, _) => UpdateQuickInfo();
        }
    }

    private void WireEvents()
    {
        if (_btnModeHead != null) _btnModeHead.Click += (_, _) => SwitchMode(TranslationEditMode.Head);
        if (_btnModeBody != null) _btnModeBody.Click += (_, _) => SwitchMode(TranslationEditMode.Body);
        if (_btnModeNotes != null) _btnModeNotes.Click += (_, _) => SwitchMode(TranslationEditMode.Notes);

        if (_btnUndo != null) _btnUndo.Click += (_, _) => DoUndo();
        if (_btnRedo != null) _btnRedo.Click += (_, _) => DoRedo();

        if (_btnCopyChunkPrompt != null) _btnCopyChunkPrompt.Click += async (_, _) => await CopyChunkWithPromptAsync();
        if (_btnPasteByNumber != null) _btnPasteByNumber.Click += async (_, _) => await PasteByMatchingBlockNumberAsync();
        if (_btnNextUntranslated != null) _btnNextUntranslated.Click += (_, _) => JumpToNextUntranslated();

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
        UpdateQuickInfo();
    }

    public string GetCurrentProjectionText()
        => _editor?.Text ?? _currentProjection ?? "";

    public void SetCurrentFilePaths(string originalPath, string translatedPath)
    {
        _origPath = originalPath;
        _tranPath = translatedPath;
        UpdateModeInfo();
    }

    public void SetHoverDictionaryEnabled(bool enabled)
    {
        _hoverDictionaryEnabled = enabled;
        ApplyHoverDictionarySetting();
    }

    // Compatibility helpers (so older MainWindow variants don't explode)
    public void SetXml(string originalXml, string translatedXml)
    {
        _currentProjection = translatedXml ?? "";
        if (_editor != null) _editor.Text = _currentProjection;
        UpdateModeInfo();
        UpdateQuickInfo();
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
        UpdateQuickInfo();
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

    private void UpdateQuickInfo()
    {
        if (_txtQuickInfo == null)
            return;

        try
        {
            var blocks = ParseProjectionBlocksWithOffsets(_editor?.Text ?? "");
            int total = blocks.Count;
            int untranslated = blocks.Count(b => string.IsNullOrWhiteSpace(b.En));
            _txtQuickInfo.Text = total > 0 ? $"Blocks: {total}  Empty EN: {untranslated}" : "";
        }
        catch
        {
            _txtQuickInfo.Text = "";
        }
    }

    private void ApplyWrap()
    {
        if (_editor != null)
            _editor.WordWrap = _chkWrap?.IsChecked == true;
    }

    // =========================
    // Hover dictionary
    // =========================

    private void ApplyHoverDictionarySetting()
    {
        if (_editor == null)
            return;

        if (_hoverDictionaryEnabled)
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

            _hoverDictionaryBehavior = new HoverDictionaryBehaviorEdit(_editor, _cedict);
            Status?.Invoke(this, "Hover dictionary attached.");
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
            Status?.Invoke(this, "Hover dictionary disabled.");
        }
        catch
        {
            _hoverDictionaryBehavior = null;
        }
    }

    // =========================
    // Undo / Redo
    // =========================

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

    // =========================
    // Chunk copy / smart paste / navigation
    // =========================

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

        // Find block at/after caret
        int startIx = FindBlockIndexAtOrAfterCaret(blocks, caret);
        if (startIx < 0)
        {
            Status?.Invoke(this, "No block found near caret.");
            return;
        }

        // Move to first untranslated block from there
        while (startIx < blocks.Count && !string.IsNullOrWhiteSpace(blocks[startIx].En))
            startIx++;

        if (startIx >= blocks.Count)
        {
            Status?.Invoke(this, "No untranslated block found after caret.");
            return;
        }

        // Continuous untranslated span only (stop at first translated EN)
        int endIxExclusive = startIx;
        int copied = 0;
        while (endIxExclusive < blocks.Count && copied < maxCount)
        {
            if (!string.IsNullOrWhiteSpace(blocks[endIxExclusive].En))
                break;

            copied++;
            endIxExclusive++;
        }

        if (copied == 0)
        {
            Status?.Invoke(this, "Nothing to copy.");
            return;
        }

        var firstBlock = blocks[startIx];
        var lastBlock = blocks[endIxExclusive - 1];

        int copyStart = firstBlock.BlockStartOffset;
        int copyEndExclusive = lastBlock.BlockEndOffsetExclusive;

        if (copyStart < 0 || copyEndExclusive < copyStart || copyEndExclusive > text.Length)
        {
            Status?.Invoke(this, "Invalid chunk boundaries.");
            return;
        }

        var rawChunk = text.Substring(copyStart, copyEndExclusive - copyStart).TrimEnd('\r', '\n');
        var payload = BuildPrompt(rawChunk);

        var cb = GetClipboard();
        if (cb == null)
        {
            Status?.Invoke(this, "Clipboard unavailable.");
            return;
        }

        await cb.SetTextAsync(payload);

        // VISUAL FEEDBACK: select the entire copied chunk (all N blocks), not just the first one
        if (_editor.Document != null)
        {
            int start = Math.Clamp(copyStart, 0, _editor.Document.TextLength);
            int end = Math.Clamp(copyEndExclusive, start, _editor.Document.TextLength);

            _editor.CaretOffset = start;
            _editor.TextArea.Selection = Selection.Create(_editor.TextArea, start, end);

            try
            {
                var line = _editor.Document.GetLineByOffset(start).LineNumber;
                _editor.ScrollToLine(line);
            }
            catch
            {
                // ignore
            }

            _editor.Focus();
        }

        Status?.Invoke(this, $"Copied {copied} block(s): <{firstBlock.BlockNumber}>–<{lastBlock.BlockNumber}> + prompt.");
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

        // Apply replacements from back to front so offsets stay valid
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

        // Reselect / reveal first pasted block
        int minNum = pastedBlocks.Min(b => b.BlockNumber);
        int maxNum = pastedBlocks.Max(b => b.BlockNumber);

        var reparsed = ParseProjectionBlocksWithOffsets(_editor.Text ?? "");
        var firstReparsed = reparsed.FirstOrDefault(b => b.BlockNumber == pastedBlocks[0].BlockNumber)
            ?? reparsed.FirstOrDefault(b => b.BlockNumber == minNum);

        if (firstReparsed != null)
            SelectAndRevealBlock(firstReparsed);

        Status?.Invoke(this, $"Pasted {pastedBlocks.Count} block(s): <{minNum}>–<{maxNum}> (ZH validated).");
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

        // Prefer the next one after current
        int nextIx = -1;
        for (int i = Math.Min(curIx + 1, blocks.Count); i < blocks.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(blocks[i].En))
            {
                nextIx = i;
                break;
            }
        }

        // Wrap if needed
        bool wrapped = false;
        if (nextIx < 0)
        {
            for (int i = 0; i <= Math.Min(curIx, blocks.Count - 1); i++)
            {
                if (string.IsNullOrWhiteSpace(blocks[i].En))
                {
                    nextIx = i;
                    wrapped = true;
                    break;
                }
            }
        }

        if (nextIx < 0)
        {
            Status?.Invoke(this, "No untranslated blocks.");
            return;
        }

        SelectAndRevealBlock(blocks[nextIx]);
        Status?.Invoke(this, wrapped
            ? $"Jumped to untranslated block <{blocks[nextIx].BlockNumber}> (wrapped)."
            : $"Jumped to untranslated block <{blocks[nextIx].BlockNumber}>.");
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
        catch { }

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

        // If caret is before first block, start there
        if (caretOffset < blocks[0].BlockStartOffset)
            return 0;

        // If caret is after all blocks, return last
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
            // ignore
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

        if (e.Key == Key.D1 && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            SwitchMode(TranslationEditMode.Head);
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
- Keep <n> and all ZH: lines unchanged.
- Keep the same number of EN[n] lines as ZH[n] lines.
- Do NOT merge lines.
- Do NOT split lines.
- Do NOT add commentary.
- Do NOT add or remove blocks.
- Do NOT use angle brackets < or > in EN text.
- Output ONLY one markdown code block.

```markdown
{selectedProjection}
```";
    }

    private static string ExtractCodeBlockOrRaw(string text)
    {
        var m = Regex.Match(text, @"```(?:markdown|md|text)?\s*(?<x>[\s\S]*?)\s*```", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups["x"].Value.Trim() : text.Trim();
    }

    // =========================
    // Block parsing (editor text)
    // =========================

    private sealed class ProjectionBlockInfo
    {
        public int BlockNumber { get; set; }
        public string Zh { get; set; } = "";
        public string En { get; set; } = "";

        public int BlockStartOffset { get; set; }               // at '<'
        public int BlockEndOffsetExclusive { get; set; }        // end of block (may include trailing blank lines)
        public int EnValueStartOffset { get; set; }             // offset of EN value text
        public int EnValueLength { get; set; }                  // length of EN value text
    }

    private static List<ProjectionBlockInfo> ParseProjectionBlocksWithOffsets(string text)
    {
        text ??= "";

        // Strict single-line ZH / EN blocks:
        // <123>
        // ZH: ...
        // EN: ...
        //
        // Extra blank lines after block are allowed.
        var rx = new Regex(
            @"(?m)^(?<hdr><(?<num>\d+)>)\s*\r?\n" +
            @"ZH:\s?(?<zh>[^\r\n]*)\r?\n" +
            @"EN:\s?(?<en>[^\r\n]*)",
            RegexOptions.Compiled);

        var ms = rx.Matches(text);
        var list = new List<ProjectionBlockInfo>(ms.Count);

        foreach (Match m in ms)
        {
            if (!m.Success) continue;

            if (!int.TryParse(m.Groups["num"].Value, out int num))
                continue;

            var enGroup = m.Groups["en"];
            var blockStart = m.Index;
            var blockEnd = m.Index + m.Length; // extend later to include blank lines up to next header/start

            list.Add(new ProjectionBlockInfo
            {
                BlockNumber = num,
                Zh = m.Groups["zh"].Value,
                En = enGroup.Value,
                BlockStartOffset = blockStart,
                BlockEndOffsetExclusive = blockEnd,
                EnValueStartOffset = enGroup.Index,
                EnValueLength = enGroup.Length
            });
        }

        // Expand each block end to right before next block start (so copied chunks preserve blank separators)
        for (int i = 0; i < list.Count; i++)
        {
            int end = (i + 1 < list.Count) ? list[i + 1].BlockStartOffset : text.Length;
            list[i].BlockEndOffsetExclusive = end;
        }

        return list;
    }

    private static void ValidateEnglish(string en, int blockNumber)
    {
        en ??= "";

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
                ch == '\n' ||
                ch == '\r' ||
                (ch >= 0x20 && ch <= 0xD7FF) ||
                (ch >= 0xE000 && ch <= 0xFFFD);

            if (!ok)
            {
                throw new InvalidOperationException(
                    $"Block <{blockNumber}> EN contains invalid XML character (U+{((int)ch):X4}) at position {i + 1}.");
            }
        }
    }
}