// Views/SearchTabView.axaml.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.Controls.Primitives;
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
    private readonly ICitationService _citationService = new CitationService();
    private TextBlock? _activeHoverTextBlock;
    private CancellationTokenSource? _hoverLookupCts;

    // Typeahead
    private TypeaheadService? _typeahead;
    private DispatcherTimer? _typeaheadTimer;
    private Popup? _typeaheadPopup;
    private StackPanel? _typeaheadPanel;
    private Border? _typeaheadBorder;
    private TextBox? _txtQuery;
    private int _typeaheadActiveIndex = -1;
    private List<TypeaheadDisplayItem> _typeaheadItems = new();

    public SearchTabViewModel ViewModel => _vm;

    public event EventHandler<string>? Status;
    public event EventHandler<NavigationRequest>? NavigationRequested;
    public event EventHandler<string>? OpenMasterRequested;
    public event EventHandler<ScholarPassage>? AddToScholarRequested;

    /// <summary>Fired after each search so MainWindow can persist history to config.</summary>
    public event EventHandler? SearchHistoryChanged;

    /// <summary>
    /// Fired when the user selects "Open in new window" on a search result group.
    /// The argument is the RelPath of the result group.
    /// </summary>
    public event EventHandler<string>? OpenInNewWindowRequested;

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
        _vm.OpenMasterRequested += (_, name) => OpenMasterRequested?.Invoke(this, name);

        KeyDown += (s, e) =>
        {
            if (e.Key == Key.A &&
                e.KeyModifiers.HasFlag(KeyModifiers.Control | KeyModifiers.Shift))
            {
                _vm.SelectedSearchSubTabIndex = _vm.SelectedSearchSubTabIndex == 1 ? 0 : 1;
                e.Handled = true;
            }
        };

        WireViewEvents();
        WireTypeahead();
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

            // Update tooltip with recent search history after each search
            _vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SearchTabViewModel.SearchHistory) && _vm.SearchHistory.Count > 0)
                {
                    var tip = "Recent: " + string.Join(", ", _vm.SearchHistory.Take(5));
                    ToolTip.SetTip(txtQuery, tip);
                    // 4C: notify MainWindow so it can persist history to config
                    SearchHistoryChanged?.Invoke(this, EventArgs.Empty);
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
            resultsTree.SelectionChanged += (_, _) =>
            {
                if (resultsTree.SelectedItem is SearchResultGroup g
                    && g.RelPath == "__master__"
                    && _vm.HasResults)
                {
                    _vm.HandleMasterCardClick();
                }
            };
            resultsTree.PointerMoved += ResultsTree_PointerMoved;
            resultsTree.PointerExited += (_, _) => ClearResultsHoverTooltip();
            resultsTree.PointerPressed += (_, _) => ClearResultsHoverTooltip();

            // Plain-text copy of the hit snippet — the first menu entry for
            // discoverability. Many users reach for right-click and don't know
            // Ctrl+C will work here.
            var copySnippetItem = new MenuItem { Header = "Copy Snippet" };
            copySnippetItem.Click += async (_, _) =>
            {
                if (resultsTree.SelectedItem is not SearchResultChild child) return;
                var text = child.PrimarySnippetText;
                if (string.IsNullOrEmpty(text)) return;
                var top = TopLevel.GetTopLevel(this);
                if (top?.Clipboard != null)
                    await top.Clipboard.SetTextAsync(text);
            };

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

            // 4J: Open the selected result's document in a new independent reader window.
            var openInNewWindowItem = new MenuItem { Header = "Open in new window" };
            openInNewWindowItem.Click += (_, _) =>
            {
                string? relPath = null;
                if (resultsTree.SelectedItem is SearchResultGroup group && group.RelPath != "__master__")
                    relPath = group.RelPath;
                else if (resultsTree.SelectedItem is SearchResultChild child)
                    relPath = child.RelPath;

                if (!string.IsNullOrEmpty(relPath))
                    OpenInNewWindowRequested?.Invoke(this, relPath);
            };

            // 5D-2: Expand All / Collapse All
            var expandAllItem = new MenuItem { Header = "Expand All" };
            expandAllItem.Click += (_, _) =>
            {
                foreach (var g in _vm.ResultGroups)
                    g.IsExpanded = true;
            };

            var collapseAllItem = new MenuItem { Header = "Collapse All" };
            collapseAllItem.Click += (_, _) =>
            {
                foreach (var g in _vm.ResultGroups)
                    g.IsExpanded = false;
            };

            var copyCitationItem = CitationMenuHelper.BuildCiteAsFlyout(
                _citationService,
                CitationMenuHelper.GetPreferredStyle(),
                buildMetadata: () =>
                {
                    if (resultsTree.SelectedItem is not SearchResultChild child)
                        return new CitationMetadata();
                    var snippetText = child.PrimarySnippetText;
                    return new CitationMetadata
                    {
                        FileId = string.IsNullOrEmpty(child.RelPath) ? null : ZenUriParser.RelPathToFileId(child.RelPath),
                        QuotedText = snippetText,
                        ShareableUrl = string.IsNullOrEmpty(child.RelPath)
                            ? null
                            : ZenUriParser.ShareableBase + ZenUriParser.RelPathToFileId(child.RelPath),
                    };
                },
                copyToClipboard: async text =>
                {
                    var top = TopLevel.GetTopLevel(this);
                    if (top?.Clipboard != null)
                        await top.Clipboard.SetTextAsync(text);
                });

            resultsTree.ContextMenu = new ContextMenu
            {
                Items =
                {
                    copySnippetItem,
                    copyCitationItem,
                    new Separator(),
                    addToScholarItem,
                    new Separator(),
                    copyPassageLinkItem,
                    copyPassageRedditLink,
                    new Separator(),
                    copySearchLinkItem,
                    copyShareableSearchLinkItem,
                    new Separator(),
                    expandAllItem,
                    collapseAllItem,
                    new Separator(),
                    openInNewWindowItem
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
    {
        _vm.SetFileIndex(items);
        _typeahead ??= new TypeaheadService();
        _typeahead.Initialize(_vm.MasterCatalog, items);
    }

    public void SetContext(
        string root,
        string originalDir,
        IReadOnlyList<string> translatedDirs,
        Func<string, (string display, string tooltip, TranslationStatus? status)> fileMeta,
        IReadOnlyList<string>? additionalOriginalDirs = null,
        IReadOnlyList<string>? additionalTranslatedDirs = null)
        => _vm.SetContext(root, originalDir, translatedDirs, fileMeta, additionalOriginalDirs, additionalTranslatedDirs);

    public void SetZenResolver(Func<string, bool> isZenResolver)
        => _vm.SetZenResolver(isZenResolver);

    public void SetMasterCatalog(ZenMasterCatalog catalog)
        => _vm.SetMasterCatalog(catalog);

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

    /// <summary>
    /// Sets the query text and immediately executes a search.
    /// Called by MainWindow when the user selects "Search corpus for selection" in the reader.
    /// </summary>
    public void SetQueryAndSearch(string query) => SetSearchTextAndExecute(query);

    /// <summary>Focus the query text box and select all text so the user can type immediately.</summary>
    public void FocusQueryBox()
    {
        var txtQuery = this.FindControl<TextBox>("TxtQuery");
        if (txtQuery != null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                txtQuery.Focus();
                txtQuery.SelectAll();
            }, DispatcherPriority.Background);
        }
    }

    // ── Typeahead ──

    public void InitTypeahead(ZenMasterCatalog? catalog, IReadOnlyList<FileNavItem>? fileIndex)
    {
        _typeahead ??= new TypeaheadService();
        _typeahead.Initialize(catalog, fileIndex);
    }

    private void WireTypeahead()
    {
        _txtQuery = this.FindControl<TextBox>("TxtQuery");
        _typeaheadPopup = this.FindControl<Popup>("TypeaheadPopup");
        _typeaheadPanel = this.FindControl<StackPanel>("TypeaheadPanel");
        _typeaheadBorder = this.FindControl<Border>("TypeaheadBorder");
        if (_txtQuery == null || _typeaheadPopup == null || _typeaheadPanel == null) return;

        _typeaheadTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _typeaheadTimer.Tick += (_, _) =>
        {
            _typeaheadTimer.Stop();
            RenderTypeahead();
        };

        _txtQuery.TextChanged += (_, _) =>
        {
            _typeaheadTimer?.Stop();
            _typeaheadTimer?.Start();
        };

        _txtQuery.KeyDown += TypeaheadKeyDown;
    }

    private void TypeaheadKeyDown(object? sender, KeyEventArgs e)
    {
        if (_typeaheadPopup == null || !_typeaheadPopup.IsOpen) return;

        var selectableItems = _typeaheadItems
            .Where(i => i.Kind != TypeaheadItemKind.SectionHeader && i.Kind != TypeaheadItemKind.CountFooter).ToList();

        switch (e.Key)
        {
            case Key.Down:
                _typeaheadActiveIndex = Math.Min(_typeaheadActiveIndex + 1, selectableItems.Count - 1);
                HighlightTypeaheadItem();
                e.Handled = true;
                break;
            case Key.Up:
                _typeaheadActiveIndex = Math.Max(_typeaheadActiveIndex - 1, 0);
                HighlightTypeaheadItem();
                e.Handled = true;
                break;
            case Key.Enter when _typeaheadActiveIndex >= 0 && _typeaheadActiveIndex < selectableItems.Count:
                SelectTypeaheadItem(selectableItems[_typeaheadActiveIndex]);
                e.Handled = true;
                break;
            case Key.Escape:
                CloseTypeahead();
                e.Handled = true;
                break;
        }
    }

    private void SyncTypeaheadWidth()
    {
        if (_txtQuery == null || _typeaheadBorder == null) return;
        var w = _txtQuery.Bounds.Width;
        if (w > 100)
        {
            _typeaheadBorder.MinWidth = w;
            _typeaheadBorder.MaxWidth = w;
        }
    }

    private void RenderTypeahead()
    {
        if (_typeahead == null || _typeaheadPanel == null || _typeaheadPopup == null) return;

        var text = _txtQuery?.Text?.Trim() ?? "";
        if (text.Length < 1)
        {
            // 4C: show recent history when input is empty, otherwise close
            if (_vm.SearchHistory.Count > 0)
                RenderHistory();
            else
                CloseTypeahead();
            return;
        }

        _typeaheadItems = _typeahead.Query(text);
        if (_typeaheadItems.Count <= 1) // only fulltext action = no real suggestions
        {
            // Still show the fulltext action
        }

        // 4A: one inverted-index lookup for all title matches
        HashSet<string>? indexedPaths = _vm?.GetIndexedRelPaths(text);
        if (indexedPaths != null)
        {
            foreach (var it in _typeaheadItems)
            {
                if (it.Kind == TypeaheadItemKind.Title && it.FileItem != null)
                    it.InIndex = indexedPaths.Contains(it.FileItem.RelPath);
            }
        }

        // 4D: co-occurrence suggestions when Insights tab is active
        if (_vm.SelectedSearchSubTabIndex == 1)
        {
            var coocTerms = _vm.CoocChars
                .Take(5)
                .Select(r => r.Key)
                .Concat(_vm.CoocNgrams.Take(3).Select(r => r.Key))
                .Where(t => !string.IsNullOrWhiteSpace(t) && t != text)
                .Distinct()
                .Take(6)
                .ToList();

            if (coocTerms.Count > 0)
            {
                _typeaheadItems.Add(new TypeaheadDisplayItem
                {
                    Kind = TypeaheadItemKind.SectionHeader,
                    HeaderText = "Related"
                });
                foreach (var term in coocTerms)
                {
                    _typeaheadItems.Add(new TypeaheadDisplayItem
                    {
                        Kind = TypeaheadItemKind.CoocTerm,
                        Query = term,
                        DisplayName = term
                    });
                }
            }
        }

        // 4B: append trailing count footer for CJK queries
        if (HasTwoCjkChars(text))
        {
            var paths = indexedPaths ?? _vm?.GetIndexedRelPaths(text);
            if (paths != null)
            {
                _typeaheadItems.Add(new TypeaheadDisplayItem
                {
                    Kind = TypeaheadItemKind.CountFooter,
                    CountLabel = $"~{paths.Count} texts",
                });
            }
        }

        _typeaheadPanel.Children.Clear();
        _typeaheadActiveIndex = -1;

        var isDark = Avalonia.Application.Current?.ActualThemeVariant ==
                     Avalonia.Styling.ThemeVariant.Dark;
        int selectableIdx = 0;

        foreach (var item in _typeaheadItems)
        {
            switch (item.Kind)
            {
                case TypeaheadItemKind.SectionHeader:
                    _typeaheadPanel.Children.Add(new TextBlock
                    {
                        Text = item.HeaderText?.ToUpperInvariant() ?? "",
                        FontSize = 10,
                        FontWeight = Avalonia.Media.FontWeight.SemiBold,
                        Opacity = 0.5,
                        Padding = new Thickness(10, 8, 10, 2),
                    });
                    break;

                case TypeaheadItemKind.Master:
                {
                    var panel = new StackPanel { Spacing = 1 };
                    panel.Children.Add(new TextBlock
                    {
                        Text = item.DisplayName,
                        FontSize = 13,
                        FontWeight = Avalonia.Media.FontWeight.Medium,
                    });
                    if (!string.IsNullOrWhiteSpace(item.Meta))
                    {
                        panel.Children.Add(new TextBlock
                        {
                            Text = item.Meta,
                            FontSize = 11,
                            Opacity = 0.6,
                        });
                    }
                    var border = MakeTypeaheadRow(panel, item, selectableIdx++);
                    _typeaheadPanel.Children.Add(border);
                    break;
                }

                case TypeaheadItemKind.Title:
                {
                    var panel = new StackPanel { Spacing = 1 };
                    if (!string.IsNullOrWhiteSpace(item.ZhTitle))
                    {
                        panel.Children.Add(new TextBlock
                        {
                            Text = item.ZhTitle,
                            FontSize = 13,
                        });
                    }
                    if (!string.IsNullOrWhiteSpace(item.EnTitle))
                    {
                        panel.Children.Add(new TextBlock
                        {
                            Text = item.EnTitle,
                            FontSize = 11,
                            Opacity = 0.6,
                        });
                    }
                    if (item.InIndex)
                    {
                        panel.Children.Add(new Border
                        {
                            Background = new Avalonia.Media.SolidColorBrush(
                                Avalonia.Media.Color.FromArgb(60, 0, 200, 100)),
                            CornerRadius = new CornerRadius(3),
                            Padding = new Thickness(4, 1),
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                            Child = new TextBlock
                            {
                                Text = "in index",
                                FontSize = 9,
                                Opacity = 0.8,
                            },
                        });
                    }
                    var border = MakeTypeaheadRow(panel, item, selectableIdx++);
                    _typeaheadPanel.Children.Add(border);
                    break;
                }

                case TypeaheadItemKind.FullTextAction:
                {
                    var tb = new TextBlock
                    {
                        Text = $"Search full text for \u201c{item.Query}\u201d",
                        FontSize = 12,
                        FontStyle = Avalonia.Media.FontStyle.Italic,
                        Opacity = 0.6,
                    };
                    var border = MakeTypeaheadRow(tb, item, selectableIdx++);
                    _typeaheadPanel.Children.Add(border);
                    break;
                }

                case TypeaheadItemKind.CountFooter:
                {
                    _typeaheadPanel.Children.Add(new TextBlock
                    {
                        Text = item.CountLabel ?? "",
                        FontSize = 10,
                        FontStyle = Avalonia.Media.FontStyle.Italic,
                        Opacity = 0.45,
                        Padding = new Thickness(10, 4, 10, 6),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    });
                    // Not selectable — do NOT call MakeTypeaheadRow
                    break;
                }

                case TypeaheadItemKind.RecentSearch:
                {
                    var tb = new TextBlock
                    {
                        Text = item.DisplayName,
                        FontSize = 12,
                    };
                    var border = MakeTypeaheadRow(tb, item, selectableIdx++);
                    _typeaheadPanel.Children.Add(border);
                    break;
                }

                case TypeaheadItemKind.CoocTerm:
                {
                    var tb = new TextBlock
                    {
                        Text = item.DisplayName,
                        FontSize = 13,
                    };
                    var border = MakeTypeaheadRow(tb, item, selectableIdx++);
                    _typeaheadPanel.Children.Add(border);
                    break;
                }
            }
        }

        SyncTypeaheadWidth();
        _typeaheadPopup.IsOpen = true;
    }

    /// <summary>4C: Render recent search history when input box is empty.</summary>
    private void RenderHistory()
    {
        if (_typeaheadPanel == null || _typeaheadPopup == null) return;
        _typeaheadPanel.Children.Clear();
        _typeaheadActiveIndex = -1;
        _typeaheadItems = new List<TypeaheadDisplayItem>();

        // Header (not selectable)
        _typeaheadItems.Add(new TypeaheadDisplayItem
        {
            Kind = TypeaheadItemKind.SectionHeader,
            HeaderText = "Recent"
        });
        _typeaheadPanel.Children.Add(new TextBlock
        {
            Text = "RECENT",
            FontSize = 10,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            Opacity = 0.5,
            Padding = new Thickness(10, 8, 10, 2),
        });

        int selectableIdx = 0;
        foreach (var q in _vm.SearchHistory.Take(8))
        {
            var item = new TypeaheadDisplayItem
            {
                Kind = TypeaheadItemKind.RecentSearch,
                Query = q,
                DisplayName = q
            };
            _typeaheadItems.Add(item);

            var tb = new TextBlock { Text = q, FontSize = 12 };
            var row = MakeTypeaheadRow(tb, item, selectableIdx++);
            _typeaheadPanel.Children.Add(row);
        }

        SyncTypeaheadWidth();
        _typeaheadPopup.IsOpen = true;
    }

    private static bool HasTwoCjkChars(string text)
    {
        int count = 0;
        foreach (var ch in text)
            if (ch >= '\u4E00' && ch <= '\u9FFF' && ++count >= 2) return true;
        return false;
    }

    private Border MakeTypeaheadRow(Control content, TypeaheadDisplayItem item, int idx)
    {
        var border = new Border
        {
            Child = content,
            Padding = new Thickness(10, 6),
            CornerRadius = new CornerRadius(4),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            Tag = item,
        };
        border.PointerEntered += (_, _) =>
        {
            var selectableItems = _typeaheadItems
                .Where(i => i.Kind != TypeaheadItemKind.SectionHeader && i.Kind != TypeaheadItemKind.CountFooter).ToList();
            _typeaheadActiveIndex = selectableItems.IndexOf(item);
            HighlightTypeaheadItem();
        };
        border.PointerPressed += (_, _) => SelectTypeaheadItem(item);
        return border;
    }

    private void HighlightTypeaheadItem()
    {
        if (_typeaheadPanel == null) return;
        var selectableItems = _typeaheadItems
            .Where(i => i.Kind != TypeaheadItemKind.SectionHeader && i.Kind != TypeaheadItemKind.CountFooter).ToList();
        int si = 0;
        foreach (var child in _typeaheadPanel.Children)
        {
            if (child is Border b && b.Tag is TypeaheadDisplayItem)
            {
                var isActive = si == _typeaheadActiveIndex;
                var isDark = Avalonia.Application.Current?.ActualThemeVariant ==
                             Avalonia.Styling.ThemeVariant.Dark;
                b.Background = isActive
                    ? new Avalonia.Media.SolidColorBrush(isDark
                        ? Avalonia.Media.Color.FromArgb(80, 255, 255, 255)
                        : Avalonia.Media.Color.FromArgb(120, 50, 100, 180))
                    : null;
                si++;
            }
        }
    }

    private void SelectTypeaheadItem(TypeaheadDisplayItem item)
    {
        CloseTypeahead();
        switch (item.Kind)
        {
            case TypeaheadItemKind.Master when item.Master != null:
                OpenMasterRequested?.Invoke(this, item.Master.CanonicalName);
                break;
            case TypeaheadItemKind.Title when item.FileItem != null:
                NavigationRequested?.Invoke(this, new NavigationRequest { RelPath = item.FileItem.RelPath });
                break;
            case TypeaheadItemKind.FullTextAction:
                if (_txtQuery != null) _txtQuery.Text = item.Query;
                _vm.SearchCommand.Execute(null);
                break;
            case TypeaheadItemKind.RecentSearch:
                // 4C: populate the query box and execute search
                if (_txtQuery != null) _txtQuery.Text = item.Query;
                _vm.SearchCommand.Execute(null);
                break;
            case TypeaheadItemKind.CoocTerm:
                // 4D: use the co-occurrence term as the new search query
                if (_txtQuery != null) _txtQuery.Text = item.Query;
                _vm.SearchCommand.Execute(null);
                break;
        }
    }

    private void CloseTypeahead()
    {
        if (_typeaheadPopup != null) _typeaheadPopup.IsOpen = false;
        _typeaheadActiveIndex = -1;
    }
}

