// Views/TranslationTabView.axaml.cs
// Projection editor for IndexedTranslationService (Head / Body / Notes)

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Models;
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
    private Button? _btnCopyChunkPrompt, _btnPasteByNumber, _btnNextUntranslated, _btnFindChineseInEn, _btnSave, _btnRevert;
    private Button? _btnApproveSegment, _btnNeedsWorkSegment, _btnRejectSegment;
    private CheckBox? _chkWrap;
    private ComboBox? _cmbChunkSize;
    private TextBlock? _txtModeInfo;
    private TextBlock? _txtQuickInfo;
    private TextBlock? _txtReviewState;
    private TextEditor? _editor;

    private TranslationEditMode _currentMode = TranslationEditMode.Body;
    private string _currentProjection = "";

    private string? _origPath;
    private string? _tranPath;

    private bool _hoverDictionaryEnabled = true;
    private HoverDictionaryBehaviorEdit? _hoverDictionaryBehavior;
    private readonly ICedictDictionary _cedict = new CedictDictionaryService();

    public event EventHandler<TranslationEditMode>? ModeChanged;
    public event EventHandler? SaveRequested;
    public event EventHandler? RevertRequested;
    public event EventHandler<string>? Status;

    private CheckBox? _chkAssistantVisible;
    private Border? _assistantPane;
    private GridSplitter? _assistantSplitter;
    private Grid? _editorAssistantGrid;

    private Button? _btnBuildReferenceTm;
    public event EventHandler? BuildReferenceTmRequested;

    private Button? _btnManageTerms;
    public event EventHandler? ManageTermsRequested;

    private StackPanel? _approvedTmHost;
    private StackPanel? _referenceTmHost;
    private StackPanel? _termHost;
    private StackPanel? _qaHost;

    private readonly List<IDisposable> _assistantHoverDisposables = new();
    private Func<string, string>? _assistantTitleResolver;
    private TranslationAssistantSnapshot? _lastAssistantSnapshot;

    public event EventHandler<string>? ReviewActionRequested;

    public TranslationTabView()
    {
        AvaloniaXamlLoader.Load(this);
        FindControls();
        WireEvents();
        ApplyWrap();
        UpdateAssistantVisibility();
        UpdateModeInfo();
        ApplyHoverDictionarySetting();
        SetCurrentReviewState(null, null, null);
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
        _btnFindChineseInEn = this.FindControl<Button>("BtnFindChineseInEn");
        _btnSave = this.FindControl<Button>("BtnSave");
        _btnRevert = this.FindControl<Button>("BtnRevert");
        _btnBuildReferenceTm = this.FindControl<Button>("BtnBuildReferenceTm");
        _btnManageTerms = this.FindControl<Button>("BtnManageTerms");

        _btnApproveSegment = this.FindControl<Button>("BtnApproveSegment");
        _btnNeedsWorkSegment = this.FindControl<Button>("BtnNeedsWorkSegment");
        _btnRejectSegment = this.FindControl<Button>("BtnRejectSegment");

        _cmbChunkSize = this.FindControl<ComboBox>("CmbChunkSize");
        _chkWrap = this.FindControl<CheckBox>("ChkWrap");
        _chkAssistantVisible = this.FindControl<CheckBox>("ChkAssistantVisible");

        _txtModeInfo = this.FindControl<TextBlock>("TxtModeInfo");
        _txtQuickInfo = this.FindControl<TextBlock>("TxtQuickInfo");
        _txtReviewState = this.FindControl<TextBlock>("TxtReviewState");

        _editor = this.FindControl<TextEditor>("EditorProjection");

        _assistantPane = this.FindControl<Border>("AssistantPane");
        _assistantSplitter = this.FindControl<GridSplitter>("AssistantSplitter");
        _editorAssistantGrid = this.FindControl<Grid>("EditorAssistantGrid");

        _approvedTmHost = this.FindControl<StackPanel>("ApprovedTmHost");
        _referenceTmHost = this.FindControl<StackPanel>("ReferenceTmHost");
        _termHost = this.FindControl<StackPanel>("TermHost");
        _qaHost = this.FindControl<StackPanel>("QaHost");

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
                _editor.TextArea.Caret.PositionChanged += (_, _) => PublishCurrentSegment();
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
        if (_btnFindChineseInEn != null) _btnFindChineseInEn.Click += (_, _) => JumpToChineseInEnglishLine();

        if (_btnSave != null)
            _btnSave.Click += (_, _) => SaveRequested?.Invoke(this, EventArgs.Empty);

        if (_btnRevert != null)
            _btnRevert.Click += (_, _) => RevertRequested?.Invoke(this, EventArgs.Empty);

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

        if (_chkWrap != null)
        {
            _chkWrap.Checked += (_, _) => ApplyWrap();
            _chkWrap.Unchecked += (_, _) => ApplyWrap();
        }

        if (_chkAssistantVisible != null)
        {
            _chkAssistantVisible.Checked += (_, _) => UpdateAssistantVisibility();
            _chkAssistantVisible.Unchecked += (_, _) => UpdateAssistantVisibility();
        }

        AddHandler(KeyDownEvent, OnKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    public void SetAssistantTitleResolver(Func<string, string>? resolver)
    {
        _assistantTitleResolver = resolver;
        if (_lastAssistantSnapshot != null)
            RenderAssistantSnapshot(_lastAssistantSnapshot);
    }

    public void SetCurrentReviewState(string? status, string? reviewer, DateTime? reviewedUtc)
    {
        if (_txtReviewState == null)
            return;

        status = (status ?? "").Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(status))
        {
            _txtReviewState.Text = "Unreviewed";
            return;
        }

        string stateLabel = status switch
        {
            TranslationReviewStatuses.Approved => "Approved",
            TranslationReviewStatuses.NeedsWork => "Needs work",
            TranslationReviewStatuses.Rejected => "Rejected",
            _ => status
        };

        if (!string.IsNullOrWhiteSpace(reviewer) && reviewedUtc.HasValue)
        {
            _txtReviewState.Text = $"{stateLabel} — {reviewer} — {reviewedUtc.Value.ToLocalTime():yyyy-MM-dd HH:mm}";
        }
        else if (!string.IsNullOrWhiteSpace(reviewer))
        {
            _txtReviewState.Text = $"{stateLabel} — {reviewer}";
        }
        else
        {
            _txtReviewState.Text = stateLabel;
        }
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
        ReattachAssistantHoverBehaviors();
    }

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

        SetAssistantSnapshot(null);
        SetCurrentReviewState(null, null, null);
        UpdateModeInfo();
        UpdateModeButtons();
        UpdateQuickInfo();
    }

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
            int emptyEn = blocks.Count(b => string.IsNullOrWhiteSpace(b.En));
            int untranslated = blocks.Count(b => ShouldJumpToUntranslated(b));

            _txtQuickInfo.Text = total > 0
                ? $"Blocks: {total}  Empty EN: {emptyEn}  Untranslated: {untranslated}"
                : "";
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
            return;
        }

        if (e.Key == Key.F9)
        {
            ReviewActionRequested?.Invoke(this, TranslationReviewStatuses.Approved);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F10)
        {
            ReviewActionRequested?.Invoke(this, TranslationReviewStatuses.NeedsWork);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F11)
        {
            ReviewActionRequested?.Invoke(this, TranslationReviewStatuses.Rejected);
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
- Translate common Zen honorifics/titles like 「和尚」 as “the master” (or “Venerable”) in EN, not left as Chinese.

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

    private static bool IsChineseChar(char ch)
    {
        return (ch >= '\u3400' && ch <= '\u4DBF')
            || (ch >= '\u4E00' && ch <= '\u9FFF')
            || (ch >= '\uF900' && ch <= '\uFAFF');
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

    private static List<ProjectionBlockInfo> ParseProjectionBlocksWithOffsets(string text)
    {
        text ??= "";

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
            var blockEnd = m.Index + m.Length;

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

    public sealed class CurrentProjectionSegmentChangedEventArgs : EventArgs
    {
        public int BlockNumber { get; init; }
        public string Zh { get; init; } = "";
        public string En { get; init; } = "";
        public int BlockStartOffset { get; init; }
        public int BlockEndOffsetExclusive { get; init; }
        public TranslationEditMode Mode { get; init; }
    }

    public event EventHandler<CurrentProjectionSegmentChangedEventArgs>? CurrentSegmentChanged;

    private void PublishCurrentSegment()
    {
        try
        {
            if (_editor == null) return;

            var blocks = ParseProjectionBlocksWithOffsets(_editor.Text ?? "");
            if (blocks.Count == 0) return;

            int caret = _editor.CaretOffset;
            int ix = FindBlockIndexAtOrAfterCaret(blocks, caret);
            if (ix < 0 || ix >= blocks.Count) return;

            var b = blocks[ix];

            CurrentSegmentChanged?.Invoke(this, new CurrentProjectionSegmentChangedEventArgs
            {
                BlockNumber = b.BlockNumber,
                Zh = b.Zh ?? "",
                En = b.En ?? "",
                BlockStartOffset = b.BlockStartOffset,
                BlockEndOffsetExclusive = b.BlockEndOffsetExclusive,
                Mode = _currentMode
            });
        }
        catch
        {
        }
    }

    public void SetAssistantSnapshot(TranslationAssistantSnapshot? snapshot)
    {
        _lastAssistantSnapshot = snapshot;
        RenderAssistantSnapshot(snapshot);
    }

    private void RenderAssistantSnapshot(TranslationAssistantSnapshot? snapshot)
    {
        ClearAssistantHoverBehaviors();

        if (_approvedTmHost != null) _approvedTmHost.Children.Clear();
        if (_referenceTmHost != null) _referenceTmHost.Children.Clear();
        if (_termHost != null) _termHost.Children.Clear();
        if (_qaHost != null) _qaHost.Children.Clear();

        if (snapshot == null)
            return;

        if (_approvedTmHost != null)
        {
            foreach (var m in snapshot.ApprovedMatches ?? new List<TranslationTmMatch>())
                _approvedTmHost.Children.Add(BuildTmEntry(snapshot, m));
        }

        if (_referenceTmHost != null)
        {
            foreach (var m in snapshot.ReferenceMatches ?? new List<TranslationTmMatch>())
                _referenceTmHost.Children.Add(BuildTmEntry(snapshot, m));
        }

        if (_termHost != null)
        {
            foreach (var t in snapshot.Terms ?? new List<TermHit>())
                _termHost.Children.Add(BuildTermEntry(snapshot, t));
        }

        if (_qaHost != null)
        {
            foreach (var q in snapshot.QaIssues ?? new List<QaIssue>())
                _qaHost.Children.Add(BuildQaEntry(q));
        }
    }

    private Control BuildTmEntry(TranslationAssistantSnapshot snapshot, TranslationTmMatch match)
    {
        string title = ResolveAssistantTitle(match.RelPath);
        string currentZh = snapshot.Segment?.ZhText ?? "";
        string editorText = BuildTmEditorText(match, title);
        var ranges = BuildTmHighlightRanges(editorText, match.SourceText ?? "", currentZh);

        var editor = BuildAssistantEditor(editorText, ranges, minHeight: 90, maxHeight: 220);

        return new Border
        {
            BorderBrush = GetResourceBrush("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6),
            Child = editor
        };
    }

    private Control BuildTermEntry(TranslationAssistantSnapshot snapshot, TermHit term)
    {
        string currentZh = snapshot.Segment?.ZhText ?? "";
        string editorText = BuildTermEditorText(term);
        var ranges = BuildSingleLineChineseHighlightRanges(editorText, term.SourceTerm ?? "", currentZh);

        var editor = BuildAssistantEditor(editorText, ranges, minHeight: 70, maxHeight: 180);

        return new Border
        {
            BorderBrush = GetResourceBrush("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6),
            Child = editor
        };
    }

    private Control BuildQaEntry(QaIssue issue)
    {
        var editor = BuildAssistantEditor(
            $"[{issue.Severity}] {issue.Message}",
            Array.Empty<TextRange>(),
            minHeight: 56,
            maxHeight: 140);

        return new Border
        {
            BorderBrush = GetResourceBrush("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6),
            Child = editor
        };
    }

    private TextEditor BuildAssistantEditor(
        string text,
        IReadOnlyList<TextRange> highlightRanges,
        double minHeight,
        double maxHeight)
    {
        var editor = new TextEditor
        {
            Text = text ?? "",
            IsReadOnly = true,
            ShowLineNumbers = false,
            WordWrap = true,
            FontFamily = new FontFamily("Consolas"),
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Background = GetResourceBrush("XmlViewerBg"),
            Foreground = GetResourceBrush("TextFg"),
            MinHeight = minHeight,
            MaxHeight = maxHeight
        };

        if (editor.TextArea?.TextView != null && highlightRanges.Count > 0)
        {
            var colorizer = new SharedChineseColorizer(highlightRanges);
            editor.TextArea.TextView.LineTransformers.Add(colorizer);
            editor.TextArea.TextView.Redraw();
        }

        if (editor.TextArea != null)
        {
            editor.TextArea.Caret.Offset = 0;
            editor.TextArea.Selection = Selection.Create(editor.TextArea, 0, 0);
        }

        AttachAssistantHover(editor);
        return editor;
    }

    private static string BuildTmEditorText(TranslationTmMatch match, string title)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"{Math.Round(match.Score)}%  [{match.ReviewStatus}]");

        if (!string.IsNullOrWhiteSpace(title))
            sb.AppendLine(title);

        sb.AppendLine($"ZH: {match.SourceText}");
        sb.AppendLine($"EN: {match.TargetText}");

        if (!string.IsNullOrWhiteSpace(match.Translator))
            sb.AppendLine($"Translator: {match.Translator}");

        return sb.ToString().TrimEnd();
    }

    private static string BuildTermEditorText(TermHit t)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Source: {t.SourceTerm}");
        sb.AppendLine($"Preferred: {t.PreferredTarget}");

        if (t.AlternateTargets != null && t.AlternateTargets.Count > 0)
            sb.AppendLine($"Alternates: {string.Join(", ", t.AlternateTargets)}");

        if (!string.IsNullOrWhiteSpace(t.Status))
            sb.AppendLine($"Status: {t.Status}");

        if (!string.IsNullOrWhiteSpace(t.Note))
            sb.AppendLine($"Note: {t.Note}");

        return sb.ToString().TrimEnd();
    }

    private sealed record TextRange(int Start, int Length);

    private static IReadOnlyList<TextRange> BuildTmHighlightRanges(string wholeText, string suggestionZh, string currentZh)
    {
        int zhLineStart = wholeText.IndexOf("ZH: ", StringComparison.Ordinal);
        if (zhLineStart < 0)
            return Array.Empty<TextRange>();

        zhLineStart += 4;
        int zhLineEnd = wholeText.IndexOf('\n', zhLineStart);
        if (zhLineEnd < 0)
            zhLineEnd = wholeText.Length;

        return BuildSharedChineseRangesInWholeText(
            wholeText,
            zhLineStart,
            zhLineEnd - zhLineStart,
            suggestionZh,
            currentZh);
    }

    private static IReadOnlyList<TextRange> BuildSingleLineChineseHighlightRanges(string wholeText, string lineText, string currentZh)
    {
        int lineEnd = wholeText.IndexOf('\n');
        if (lineEnd < 0)
            lineEnd = wholeText.Length;

        int colon = wholeText.IndexOf(": ", StringComparison.Ordinal);
        int contentStart = colon >= 0 ? colon + 2 : 0;
        int contentLength = Math.Max(0, lineEnd - contentStart);

        return BuildSharedChineseRangesInWholeText(
            wholeText,
            contentStart,
            contentLength,
            lineText,
            currentZh);
    }

    private static IReadOnlyList<TextRange> BuildSharedChineseRangesInWholeText(
        string wholeText,
        int targetStart,
        int targetLength,
        string suggestionZh,
        string currentZh)
    {
        var result = new List<TextRange>();

        if (string.IsNullOrWhiteSpace(wholeText) ||
            string.IsNullOrWhiteSpace(suggestionZh) ||
            string.IsNullOrWhiteSpace(currentZh) ||
            targetLength <= 0)
            return result;

        var localRanges = FindSharedChineseRanges(suggestionZh, currentZh);
        foreach (var r in localRanges)
        {
            int absStart = targetStart + r.Start;
            if (absStart < 0 || absStart >= wholeText.Length)
                continue;

            int len = Math.Min(r.Length, wholeText.Length - absStart);
            if (len > 0)
                result.Add(new TextRange(absStart, len));
        }

        return MergeRanges(result);
    }

    private static List<TextRange> FindSharedChineseRanges(string suggestionZh, string currentZh)
    {
        var result = new List<TextRange>();
        if (string.IsNullOrWhiteSpace(suggestionZh) || string.IsNullOrWhiteSpace(currentZh))
            return result;

        var candidates = ExtractChinesePhrases(currentZh)
            .Where(x => x.Length >= 2)
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(x => x.Length)
            .ToList();

        var used = new bool[suggestionZh.Length];

        foreach (var phrase in candidates)
        {
            int searchAt = 0;
            while (searchAt < suggestionZh.Length)
            {
                int ix = suggestionZh.IndexOf(phrase, searchAt, StringComparison.Ordinal);
                if (ix < 0)
                    break;

                bool overlaps = false;
                for (int i = ix; i < ix + phrase.Length && i < used.Length; i++)
                {
                    if (used[i])
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    for (int i = ix; i < ix + phrase.Length && i < used.Length; i++)
                        used[i] = true;

                    result.Add(new TextRange(ix, phrase.Length));
                }

                searchAt = ix + Math.Max(1, phrase.Length);
            }
        }

        return result.OrderBy(x => x.Start).ToList();
    }

    private static List<string> ExtractChinesePhrases(string text)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
            return result;

        var runs = new List<string>();
        var sb = new StringBuilder();

        foreach (char ch in text)
        {
            if (IsChineseChar(ch))
            {
                sb.Append(ch);
            }
            else if (sb.Length > 0)
            {
                runs.Add(sb.ToString());
                sb.Clear();
            }
        }

        if (sb.Length > 0)
            runs.Add(sb.ToString());

        foreach (var run in runs)
        {
            for (int len = run.Length; len >= 2; len--)
            {
                for (int i = 0; i + len <= run.Length; i++)
                {
                    result.Add(run.Substring(i, len));
                }
            }
        }

        return result;
    }

    private static IReadOnlyList<TextRange> MergeRanges(List<TextRange> ranges)
    {
        if (ranges.Count == 0)
            return ranges;

        var ordered = ranges.OrderBy(r => r.Start).ThenBy(r => r.Length).ToList();
        var merged = new List<TextRange> { ordered[0] };

        for (int i = 1; i < ordered.Count; i++)
        {
            var last = merged[^1];
            var cur = ordered[i];

            int lastEnd = last.Start + last.Length;
            int curEnd = cur.Start + cur.Length;

            if (cur.Start <= lastEnd)
            {
                merged[^1] = new TextRange(last.Start, Math.Max(lastEnd, curEnd) - last.Start);
            }
            else
            {
                merged.Add(cur);
            }
        }

        return merged;
    }

    private string ResolveAssistantTitle(string? relPath)
    {
        if (string.IsNullOrWhiteSpace(relPath))
            return "";

        if (_assistantTitleResolver != null)
        {
            var resolved = _assistantTitleResolver(relPath);
            if (!string.IsNullOrWhiteSpace(resolved))
                return resolved;
        }

        return relPath ?? "";
    }

    private void AttachAssistantHover(TextEditor editor)
    {
        if (!_hoverDictionaryEnabled)
            return;

        try
        {
            var behavior = new HoverDictionaryBehaviorEdit(editor, _cedict);
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

    private void ReattachAssistantHoverBehaviors()
    {
        if (_lastAssistantSnapshot != null)
            RenderAssistantSnapshot(_lastAssistantSnapshot);
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
        _currentMode = mode;
        _currentProjection = projectionText ?? "";

        if (_editor != null)
            _editor.Text = _currentProjection;

        UpdateModeInfo();
        UpdateModeButtons();
        UpdateQuickInfo();
        PublishCurrentSegment();
    }

    private sealed class SharedChineseColorizer : DocumentColorizingTransformer
    {
        private readonly IReadOnlyList<TextRange> _ranges;

        public SharedChineseColorizer(IReadOnlyList<TextRange> ranges)
        {
            _ranges = ranges ?? Array.Empty<TextRange>();
        }

        private static IBrush Brush(string key, IBrush fallback)
        {
            var app = Application.Current;
            if (app != null && app.TryFindResource(key, out var res) && res is IBrush b)
                return b;
            return fallback;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (_ranges.Count == 0)
                return;

            int lineStart = line.Offset;
            int lineEnd = line.EndOffset;

            var fg = Brush("NoteMarkerCommunityFg", Brushes.DodgerBlue);

            for (int i = 0; i < _ranges.Count; i++)
            {
                var r = _ranges[i];
                int rStart = r.Start;
                int rEnd = r.Start + r.Length;

                if (rEnd <= lineStart)
                    continue;

                if (rStart >= lineEnd)
                    break;

                int s = Math.Max(rStart, lineStart);
                int e = Math.Min(rEnd, lineEnd);

                if (e <= s)
                    continue;

                ChangeLinePart(s, e, el =>
                {
                    el.TextRunProperties.SetForegroundBrush(fg);
                    el.TextRunProperties.SetTypeface(
                        new Typeface(
                            el.TextRunProperties.Typeface.FontFamily,
                            el.TextRunProperties.Typeface.Style,
                            FontWeight.SemiBold,
                            el.TextRunProperties.Typeface.Stretch));
                });
            }
        }
    }
}