// Views/SearchTabView.axaml.cs
using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CbetaTranslator.App.Views;

public partial class SearchTabView : UserControl
{
    private readonly SearchTabViewModel _vm;

    public SearchTabViewModel ViewModel => _vm;

    public event EventHandler<string>? Status;
    public event EventHandler<NavigationRequest>? NavigationRequested;
    public event EventHandler<ScholarPassage>? AddToScholarRequested;

    public SearchTabView()
    {
        InitializeComponent();

        _vm = new SearchTabViewModel(App.Services.GetRequiredService<ISearchIndexService>());
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

            var addToScholarItem = new MenuItem { Header = "Add to Scholar Collection" };
            addToScholarItem.Click += (_, _) =>
            {
                if (resultsTree.SelectedItem is not SearchResultChild child) return;

                var passage = new ScholarPassage
                {
                    Id = Guid.NewGuid().ToString("N"),
                    SourceRelPath = child.RelPath,
                    ZhText = child.Side == SearchSide.Original ? child.MatchText : "",
                    EnText = child.Side == SearchSide.Translated ? child.MatchText : "",
                    AddedUtc = DateTimeOffset.UtcNow
                };

                AddToScholarRequested?.Invoke(this, passage);
            };

            var copyLinkItem = new MenuItem { Header = "Copy Link" };
            copyLinkItem.Click += async (_, _) =>
            {
                if (resultsTree.SelectedItem is not SearchResultChild child) return;

                var uri = CbetaUriParser.BuildUri(
                    child.RelPath, highlightText: child.MatchText, side: child.Side,
                    leftContext: child.LeftText, rightContext: child.RightText);
                var top = TopLevel.GetTopLevel(this);
                if (top?.Clipboard != null)
                    await top.Clipboard.SetTextAsync(uri);
                Status?.Invoke(this, "Link copied to clipboard.");
            };

            var copyRedditLink = new MenuItem { Header = "Copy Reddit Link" };
            copyRedditLink.Click += async (_, _) =>
            {
                if (resultsTree.SelectedItem is not SearchResultChild child) return;

                var url = CbetaUriParser.BuildShareableUrl(
                    child.RelPath, highlightText: child.MatchText, side: child.Side);
                var top = TopLevel.GetTopLevel(this);
                if (top?.Clipboard != null)
                    await top.Clipboard.SetTextAsync(url);
                Status?.Invoke(this, "Reddit link copied to clipboard.");
            };

            resultsTree.ContextMenu = new ContextMenu
            {
                Items = { addToScholarItem, copyLinkItem, copyRedditLink }
            };
        }

        // Wire the save-file picker delegate so the VM never touches Window/StorageProvider
        _vm.PickSaveFileAsync = async () =>
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner?.StorageProvider == null) return null;

            var file = await owner.StorageProvider.SaveFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "Export search results (TSV)",
                    SuggestedFileName = "search-results.tsv"
                });

            return file?.TryGetLocalPath();
        };

        var btnExport = this.FindControl<Button>("BtnExportTsv");
        if (btnExport != null)
        {
            btnExport.Click += async (_, _) =>
            {
                await _vm.ExportTsvCommand.ExecuteAsync(null);
            };
        }
    }

    // ----- Public forwarding methods (called by MainWindow) -----

    public void SetRootContext(string root, string originalDir, string translatedDir)
        => _vm.SetRootContext(root, originalDir, translatedDir);

    public void SetFileIndex(List<FileNavItem> items)
        => _vm.SetFileIndex(items);

    public void SetContext(
        string root,
        string originalDir,
        string translatedDir,
        Func<string, (string display, string tooltip, TranslationStatus? status)> fileMeta)
        => _vm.SetContext(root, originalDir, translatedDir, fileMeta);

    public void SetZenResolver(Func<string, bool> isZenResolver)
        => _vm.SetZenResolver(isZenResolver);

    public void SetTagFilterData(List<DocumentTag>? tags, TagVocabulary? vocab)
        => _vm.SetTagFilterData(tags, vocab);

    public void Clear()
        => _vm.Clear();
}
