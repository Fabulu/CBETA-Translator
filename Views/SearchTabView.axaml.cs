// Views/SearchTabView.axaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ReadZen.App.Views;

public partial class SearchTabView : UserControl
{
    private readonly SearchTabViewModel _vm;
    private readonly ICedictDictionary _cedict;
    private TextBlock? _activeHoverTextBlock;
    private CancellationTokenSource? _hoverLookupCts;

    public SearchTabViewModel ViewModel => _vm;

    public event EventHandler<string>? Status;
    public event EventHandler<NavigationRequest>? NavigationRequested;
    public event EventHandler<ScholarPassage>? AddToScholarRequested;

    /// <summary>Returns the currently active translation user (null = community).</summary>
    public Func<string?>? GetTranslationUser { get; set; }

    /// <summary>Returns the active translation source key for search-state deep links.</summary>
    public Func<string?>? GetTranslationSourceKey { get; set; }
    public Func<string?>? GetShareableTranslationSourceKey { get; set; }

    public SearchTabView()
    {
        InitializeComponent();

        _cedict = App.Services.GetRequiredService<ICedictDictionary>();
        _vm = new SearchTabViewModel(
            App.Services.GetRequiredService<ISearchIndexService>(),
            App.Services.GetRequiredService<ISearchExportService>());
        DataContext = _vm;

        _vm.StatusChanged += (_, msg) => Status?.Invoke(this, msg);
        _vm.NavigationRequested += (_, req) => NavigationRequested?.Invoke(this, req);

        WireViewEvents();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void WireViewEvents()
    {
        var txtQuery = this.FindControl<TextBox>("TxtQuery");
        if (txtQuery != null)
        {
            txtQuery.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    _vm.SearchCommand.Execute(null);
                    e.Handled = true;
                }
            };
        }

        var btnBuildIndex = this.FindControl<Button>("BtnBuildIndex");
        if (btnBuildIndex != null)
        {
            btnBuildIndex.PointerPressed += (_, e) =>
            {
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    _vm.SetForceRebuild();
            };
        }

        var resultsTree = this.FindControl<TreeView>("ResultsTree");
        if (resultsTree != null)
        {
            resultsTree.DoubleTapped += (_, _) =>
            {
                _vm.HandleResultDoubleTap(resultsTree.SelectedItem);
            };
            resultsTree.PointerMoved += ResultsTree_PointerMoved;
            resultsTree.PointerExited += (_, _) => ClearResultsHoverTooltip();
            resultsTree.PointerPressed += (_, _) => ClearResultsHoverTooltip();

            var addToScholarItem = new MenuItem { Header = "Add to Scholar Collection" };
            addToScholarItem.Click += (_, _) =>
            {
                if (resultsTree.SelectedItem is not SearchResultChild child) return;

                var passage = child.ToScholarPassage();
                AddToScholarRequested?.Invoke(this, passage);
            };

            var copyPassageLinkItem = new MenuItem { Header = "Copy Passage Link" };
            copyPassageLinkItem.Click += async (_, _) =>
            {
                if (resultsTree.SelectedItem is not SearchResultChild child) return;
                await CopyPassageLinkAsync(child, shareable: false);
            };

            var copyPassageRedditLink = new MenuItem { Header = "Copy Shareable Passage Link" };
            copyPassageRedditLink.Click += async (_, _) =>
            {
                if (resultsTree.SelectedItem is not SearchResultChild child) return;
                await CopyPassageLinkAsync(child, shareable: true);
            };

            var copySearchLinkItem = new MenuItem { Header = "Copy Search Link" };
            copySearchLinkItem.Click += async (_, _) =>
            {
                await CopySearchLinkAsync(shareable: true);
            };

            var copyShareableSearchLinkItem = new MenuItem { Header = "Copy Shareable Search Link" };
            copyShareableSearchLinkItem.Click += async (_, _) =>
            {
                await CopySearchLinkAsync(shareable: true);
            };

            resultsTree.ContextMenu = new ContextMenu
            {
                Items =
                {
                    addToScholarItem,
                    new Separator(),
                    copyPassageLinkItem,
                    copyPassageRedditLink,
                    new Separator(),
                    copySearchLinkItem,
                    copyShareableSearchLinkItem
                }
            };
        }

        _vm.PickExportFormatAsync = PickExportFormatAsyncCore;
        _vm.PickExportFileAsync = PickExportFileAsyncCore;

        var btnExport = this.FindControl<Button>("BtnExport");
        if (btnExport != null)
        {
            btnExport.Click += async (_, _) =>
            {
                await _vm.ExportCommand.ExecuteAsync(null);
            };
        }

        var btnCopySearchLink = this.FindControl<Button>("BtnCopySearchLink");
        if (btnCopySearchLink != null)
        {
            btnCopySearchLink.Click += async (_, _) =>
            {
                await CopySearchLinkAsync(shareable: true);
            };
        }
    }

    private async void ResultsTree_PointerMoved(object? sender, PointerEventArgs e)
    {
        var textBlock = TryGetHoveredSearchTextBlock(e.Source);
        if (textBlock == null)
        {
            ClearResultsHoverTooltip();
            return;
        }

        var text = textBlock.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text) || !ContainsCjk(text))
        {
            ClearResultsHoverTooltip();
            return;
        }

        if (ReferenceEquals(textBlock, _activeHoverTextBlock))
            return;

        _hoverLookupCts?.Cancel();
        _hoverLookupCts?.Dispose();
        _hoverLookupCts = new CancellationTokenSource();
        var ct = _hoverLookupCts.Token;

        try
        {
            await _cedict.EnsureLoadedAsync(ct);
            if (ct.IsCancellationRequested)
                return;

            if (!TryLookupSearchSegment(text, out var match))
            {
                ClearResultsHoverTooltip();
                return;
            }

            ClearResultsHoverTooltip();

            ToolTip.SetShowDelay(textBlock, 0);
            ToolTip.SetTip(textBlock, BuildDictionaryTooltip(match));
            ToolTip.SetIsOpen(textBlock, true);
            _activeHoverTextBlock = textBlock;
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            ClearResultsHoverTooltip();
        }
    }

    private void ClearResultsHoverTooltip()
    {
        try { _hoverLookupCts?.Cancel(); } catch { }
        try { _hoverLookupCts?.Dispose(); } catch { }
        _hoverLookupCts = null;

        if (_activeHoverTextBlock != null)
        {
            try { ToolTip.SetIsOpen(_activeHoverTextBlock, false); } catch { }
            try { ToolTip.SetTip(_activeHoverTextBlock, null); } catch { }
            _activeHoverTextBlock = null;
        }
    }

    private static TextBlock? TryGetHoveredSearchTextBlock(object? source)
    {
        var textBlock = source as TextBlock;
        if (textBlock == null && source is Visual visual)
            textBlock = visual.FindAncestorOfType<TextBlock>();

        if (textBlock?.DataContext is not SearchResultChild child)
            return null;

        var text = textBlock.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        bool isPrimaryMatch = string.Equals(text, child.MatchText, StringComparison.Ordinal);
        bool isSecondaryMatch = string.Equals(text, child.SecondaryMatchText, StringComparison.Ordinal);
        return isPrimaryMatch || isSecondaryMatch ? textBlock : null;
    }

    private static bool ContainsCjk(string text)
        => text.Any(ch => ch >= 0x3400 && ch <= 0x9fff);

    private bool TryLookupSearchSegment(string text, out CedictMatch match)
    {
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (ch < 0x3400 || ch > 0x9fff)
                continue;

            if (_cedict.TryLookupLongest(text, i, out match, maxLen: Math.Min(12, text.Length - i)))
                return true;

            if (_cedict.TryLookupChar(ch, out var entries) && entries.Count > 0)
            {
                match = new CedictMatch(ch.ToString(), i, 1, entries);
                return true;
            }
        }

        match = default!;
        return false;
    }

    private static Border BuildDictionaryTooltip(CedictMatch match)
    {
        var panel = new StackPanel { Spacing = 4, MaxWidth = 420 };
        panel.Children.Add(new TextBlock
        {
            Text = match.Headword,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });

        foreach (var entry in match.Entries.Take(3))
        {
            string senses = entry.Senses == null ? string.Empty : string.Join("; ", entry.Senses.Take(3));
            panel.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(senses) ? entry.Pinyin : $"{entry.Pinyin} - {senses}",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                FontSize = 11
            });
        }

        return new Border
        {
            Background = App.Current?.FindResource("PanelBg") as Avalonia.Media.IBrush,
            BorderBrush = App.Current?.FindResource("PanelBorder") as Avalonia.Media.IBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Child = panel,
            MaxWidth = 440
        };
    }

    private async Task CopyPassageLinkAsync(SearchResultChild child, bool shareable)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top?.Clipboard == null)
            return;

        // Use the active translation user regardless of which side the
        // search hit is on. The previous logic dropped the translator for
        // ZH-side hits, which meant a link made while reading your own
        // translation would silently fall back to community when followed.
        // Edge case: if the active translator hasn't translated this
        // particular file, the followed link lands on an empty translation
        // pane — but that's still the correct behavior, because it
        // truthfully reflects the source the user was browsing in.
        var user = GetTranslationUser?.Invoke();
        var highlightText = string.IsNullOrWhiteSpace(child.MatchText) ? child.PrimarySnippetText : child.MatchText;
        var leftContext = string.IsNullOrWhiteSpace(child.MatchText) ? null : child.LeftText;
        var rightContext = string.IsNullOrWhiteSpace(child.MatchText) ? null : child.RightText;
        var text = shareable
            ? ZenUriParser.BuildShareableUrl(child.RelPath, highlightText: highlightText, side: child.Side, user: user)
            : ZenUriParser.BuildUri(child.RelPath, highlightText: highlightText, side: child.Side, leftContext: leftContext, rightContext: rightContext, user: user);

        await top.Clipboard.SetTextAsync(text);
        Status?.Invoke(this, shareable ? "Shareable passage link copied to clipboard." : "Passage link copied to clipboard.");
    }

    private async Task CopySearchLinkAsync(bool shareable)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top?.Clipboard == null)
            return;

        var state = _vm.ExportUiState();
        var sourceKey = shareable
            ? (GetShareableTranslationSourceKey?.Invoke() ?? GetTranslationSourceKey?.Invoke())
            : GetTranslationSourceKey?.Invoke();
        var link = shareable
            ? ZenUriParser.BuildShareableSearchUrl(
                state.Query,
                searchOriginal: state.SearchOriginal,
                searchTranslated: state.SearchTranslated,
                zenOnly: state.ZenOnly,
                statusIndex: state.SelectedStatusIndex,
                tagId: state.SelectedTagFilterId,
                contextIndex: state.SelectedContextIndex,
                translationSource: sourceKey)
            : ZenUriParser.BuildSearchUri(
                state.Query,
                searchOriginal: state.SearchOriginal,
                searchTranslated: state.SearchTranslated,
                zenOnly: state.ZenOnly,
                statusIndex: state.SelectedStatusIndex,
                tagId: state.SelectedTagFilterId,
                contextIndex: state.SelectedContextIndex,
                translationSource: sourceKey);

        await top.Clipboard.SetTextAsync(link);
        Status?.Invoke(this, shareable ? "Shareable search link copied to clipboard." : "Search link copied to clipboard.");
    }

    private async Task<SearchExportFormat?> PickExportFormatAsyncCore()
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null)
            return null;

        var dlg = new SearchExportFormatDialog
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        return await dlg.ShowDialog<SearchExportFormat?>(owner);
    }

    private async Task<string?> PickExportFileAsyncCore(SearchExportFormat format, string? suggestedBaseName)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner?.StorageProvider == null)
            return null;

        var baseName = string.IsNullOrWhiteSpace(suggestedBaseName) ? "search-results" : suggestedBaseName;
        var options = format switch
        {
            SearchExportFormat.Html => new FilePickerSaveOptions
            {
                Title = "Export search results as HTML",
                SuggestedFileName = baseName + ".html",
                DefaultExtension = "html",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("HTML") { Patterns = new[] { "*.html", "*.htm" } }
                }
            },
            SearchExportFormat.Markdown => new FilePickerSaveOptions
            {
                Title = "Export search results as Markdown",
                SuggestedFileName = baseName + ".md",
                DefaultExtension = "md",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Markdown") { Patterns = new[] { "*.md", "*.markdown" } }
                }
            },
            SearchExportFormat.PlainText => new FilePickerSaveOptions
            {
                Title = "Export search results as Plain Text",
                SuggestedFileName = baseName + ".txt",
                DefaultExtension = "txt",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Plain Text") { Patterns = new[] { "*.txt" } }
                }
            },
            SearchExportFormat.Csv => new FilePickerSaveOptions
            {
                Title = "Export search results as CSV",
                SuggestedFileName = baseName + ".csv",
                DefaultExtension = "csv",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } }
                }
            },
            SearchExportFormat.Tsv => new FilePickerSaveOptions
            {
                Title = "Export search results as TSV",
                SuggestedFileName = baseName + ".tsv",
                DefaultExtension = "tsv",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("TSV") { Patterns = new[] { "*.tsv" } }
                }
            },
            SearchExportFormat.Json => new FilePickerSaveOptions
            {
                Title = "Export search results as JSON",
                SuggestedFileName = baseName + ".json",
                DefaultExtension = "json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } }
                }
            },
            _ => new FilePickerSaveOptions
            {
                Title = "Export search results",
                SuggestedFileName = baseName + ".txt",
                DefaultExtension = "txt"
            }
        };

        var file = await owner.StorageProvider.SaveFilePickerAsync(options);
        return file?.TryGetLocalPath();
    }

    public void SetRootContext(string root, string originalDir, IReadOnlyList<string> translatedDirs)
        => _vm.SetRootContext(root, originalDir, translatedDirs);

    public void SetFileIndex(List<FileNavItem> items)
        => _vm.SetFileIndex(items);

    public void SetContext(
        string root,
        string originalDir,
        IReadOnlyList<string> translatedDirs,
        Func<string, (string display, string tooltip, TranslationStatus? status)> fileMeta)
        => _vm.SetContext(root, originalDir, translatedDirs, fileMeta);

    public void SetZenResolver(Func<string, bool> isZenResolver)
        => _vm.SetZenResolver(isZenResolver);

    public void SetTagFilterData(List<DocumentTag>? tags, TagVocabulary? vocab)
        => _vm.SetTagFilterData(tags, vocab);

    public void Clear()
        => _vm.Clear();

    public SearchTabViewModel.SearchUiState ExportUiState()
        => _vm.ExportUiState();

    public Task ApplyUiStateAsync(SearchTabViewModel.SearchUiState? state, bool executeSearch = false)
        => _vm.ApplyUiStateAsync(state, executeSearch);

    public void SetSearchTextAndExecute(string query)
    {
        _ = _vm.ApplyUiStateAsync(new SearchTabViewModel.SearchUiState { Query = query }, executeSearch: true);
    }
}

