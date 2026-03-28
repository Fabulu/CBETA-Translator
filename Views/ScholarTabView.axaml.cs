using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CbetaTranslator.App.Views;

public partial class ScholarTabView : UserControl
{
    private readonly ScholarTabViewModel _vm;
    private bool _suppressSelectionSync;

    // Hover dictionary
    private readonly ICedictDictionary _cedict = App.Services.GetRequiredService<ICedictDictionary>();
    private HoverDictionaryBehaviorTextBox? _hoverDict;

    // Termbase highlighting
    private readonly ITermbaseStorageService _termbaseStorage = App.Services.GetRequiredService<ITermbaseStorageService>();
    private List<TermbaseEntry>? _cachedTermbaseEntries;
    private string? _termbaseCacheRoot;

    public event EventHandler<string>? Status;
    public event EventHandler<NavigationRequest>? NavigationRequested;

    public ScholarTabView()
    {
        InitializeComponent();

        _vm = new ScholarTabViewModel(App.Services.GetRequiredService<IScholarCollectionsService>());
        DataContext = _vm;

        _vm.StatusChanged += (_, msg) => Status?.Invoke(this, msg);
        _vm.NavigationRequested += (_, req) => NavigationRequested?.Invoke(this, req);

        _vm.PickExportFileAsync = PickExportFileAsync;
        _vm.PickImportFileAsync = PickImportFileAsync;

        WireViewEvents();
        SetupHoverDictionary();

        DetachedFromVisualTree += (_, _) => DisposeHoverDictionary();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void WireViewEvents()
    {
        var passagesList = this.FindControl<ListBox>("PassagesList");
        if (passagesList != null)
        {
            passagesList.DoubleTapped += (_, _) =>
            {
                _vm.NavigateToPassageCommand.Execute(null);
            };
        }

        // Update detail text fields when selected passage changes
        _vm.PropertyChanged += (_, e) =>
        {
            if (_suppressSelectionSync) return;

            if (e.PropertyName == nameof(ScholarTabViewModel.SelectedPassage))
            {
                UpdateDetailFields();
                if (_vm.SelectedPassage != null)
                {
                    _suppressSelectionSync = true;
                    _vm.SelectedCommunityPassage = null;
                    _suppressSelectionSync = false;
                }
            }
            else if (e.PropertyName == nameof(ScholarTabViewModel.SelectedCommunityPassage))
            {
                UpdateCommunityDetailFields();
                if (_vm.SelectedCommunityPassage != null)
                {
                    _suppressSelectionSync = true;
                    _vm.SelectedPassage = null;
                    _suppressSelectionSync = false;
                }
            }
            else if (e.PropertyName == nameof(ScholarTabViewModel.SelectedCommunityCollection))
            {
                if (_vm.SelectedCommunityCollection != null)
                {
                    _suppressSelectionSync = true;
                    _vm.SelectedPassage = null;
                    _suppressSelectionSync = false;
                }
            }
        };
    }

    private void UpdateDetailFields()
    {
        var passage = _vm.SelectedPassage;
        var txtSourcePath = this.FindControl<TextBlock>("TxtSourcePath");
        var txtZhText = this.FindControl<TextBox>("TxtZhText");
        var txtEnText = this.FindControl<TextBlock>("TxtEnText");

        if (txtSourcePath != null) txtSourcePath.Text = passage?.SourceRelPath ?? "";
        if (txtZhText != null) txtZhText.Text = passage?.ZhText ?? "";
        if (txtEnText != null) txtEnText.Text = passage?.EnText ?? "";

        SetupHoverDictionary();
        _ = UpdateTermbaseHitsAsync(passage?.ZhText);
    }

    private void UpdateCommunityDetailFields()
    {
        var passage = _vm.SelectedCommunityPassage;

        var txtSourcePath = this.FindControl<TextBlock>("TxtSourcePath");
        var txtZhText = this.FindControl<TextBox>("TxtZhText");
        var txtEnText = this.FindControl<TextBlock>("TxtEnText");

        if (txtSourcePath != null) txtSourcePath.Text = passage?.SourceRelPath ?? "";
        if (txtZhText != null) txtZhText.Text = passage?.ZhText ?? "";
        if (txtEnText != null) txtEnText.Text = passage?.EnText ?? "";

        // Update editor fields to show community passage metadata (read-only context)
        _vm.PassageNotes = passage?.Notes ?? "";
        _vm.PassageTags = passage != null ? string.Join(", ", passage.Tags) : "";
        _vm.PassageMasterNames = passage != null ? string.Join(", ", passage.MasterNames) : "";

        SetupHoverDictionary();
        _ = UpdateTermbaseHitsAsync(passage?.ZhText);
    }

    // ----- Hover dictionary -----

    private void SetupHoverDictionary()
    {
        DisposeHoverDictionary();

        var txtZhText = this.FindControl<TextBox>("TxtZhText");
        if (txtZhText == null) return;

        try { _hoverDict = new HoverDictionaryBehaviorTextBox(txtZhText, _cedict); }
        catch { /* dictionary not available */ }
    }

    private void DisposeHoverDictionary()
    {
        try { _hoverDict?.Dispose(); } catch { }
        _hoverDict = null;
    }

    // ----- Termbase highlighting -----

    private static readonly IBrush TermbaseGoldBg = new SolidColorBrush(Color.FromArgb(90, 255, 185, 0));

    private async Task UpdateTermbaseHitsAsync(string? zhText)
    {
        var panel = this.FindControl<ItemsControl>("PnlTermbaseHits");
        if (panel == null) return;

        if (string.IsNullOrWhiteSpace(zhText))
        {
            panel.ItemsSource = null;
            panel.IsVisible = false;
            return;
        }

        var root = _vm.GetRoot();
        if (string.IsNullOrWhiteSpace(root))
        {
            panel.ItemsSource = null;
            panel.IsVisible = false;
            return;
        }

        try
        {
            // Cache termbase entries per root
            if (_cachedTermbaseEntries == null || _termbaseCacheRoot != root)
            {
                _cachedTermbaseEntries = await _termbaseStorage.LoadAsync(root);
                _termbaseCacheRoot = root;
            }

            var hits = FindTermbaseHitsInText(zhText, _cachedTermbaseEntries);
            if (hits.Count == 0)
            {
                panel.ItemsSource = null;
                panel.IsVisible = false;
                return;
            }

            var controls = new List<Control>();
            foreach (var hit in hits)
            {
                var label = new TextBlock
                {
                    Text = $"{hit.SourceTerm} \u2192 {hit.PreferredTarget}",
                    FontSize = 11,
                    Padding = new Thickness(4, 2),
                };
                var border = new Border
                {
                    CornerRadius = new CornerRadius(3),
                    Background = TermbaseGoldBg,
                    Child = label,
                    Margin = new Thickness(0, 0, 4, 2)
                };
                if (!string.IsNullOrEmpty(hit.Note))
                    ToolTip.SetTip(border, hit.Note);
                controls.Add(border);
            }

            panel.ItemsSource = controls;
            panel.IsVisible = true;
        }
        catch
        {
            panel.ItemsSource = null;
            panel.IsVisible = false;
        }
    }

    private static List<TermbaseEntry> FindTermbaseHitsInText(string zhText, IReadOnlyList<TermbaseEntry> entries)
    {
        var hits = new List<TermbaseEntry>();
        foreach (var entry in entries)
        {
            if (string.IsNullOrEmpty(entry.SourceTerm)) continue;
            if (zhText.Contains(entry.SourceTerm, StringComparison.Ordinal))
                hits.Add(entry);
        }
        return hits;
    }

    // ----- File pickers -----

    private async Task<string?> PickExportFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Scholar Collections",
            SuggestedFileName = "scholar-collections.json",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } }
            }
        });

        return file?.Path.LocalPath;
    }

    private async Task<string?> PickImportFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Scholar Collections",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } }
            }
        });

        return files.FirstOrDefault()?.Path.LocalPath;
    }

    // ----- Public API -----

    public void SetRoot(string root)
    {
        _cachedTermbaseEntries = null;
        _termbaseCacheRoot = null;
        _vm.SetRoot(root);
    }
    public void SetUsername(string? username) => _vm.SetUsername(username);

    public void Clear() => _vm.Clear();
    public void ReloadCommunity() => _vm.LoadCommunityCommand.Execute(null);

    public void InvalidateTermbaseCache()
    {
        _cachedTermbaseEntries = null;
        _termbaseCacheRoot = null;
    }

    public void AddPassage(ScholarPassage passage)
    {
        // If no collections, create a default one first
        if (_vm.Collections.Count == 0)
        {
            _vm.AddCollectionCommand.Execute(null);
        }

        // Add to selected collection (or first one)
        var target = _vm.SelectedCollection ?? (_vm.Collections.Count > 0 ? _vm.Collections[0] : null);
        if (target == null) return;

        _ = _vm.AddPassageToCollectionAsync(target.Id, passage);
    }
}
