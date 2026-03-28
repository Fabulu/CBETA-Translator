using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CbetaTranslator.App.Views;

public partial class ScholarTabView : UserControl
{
    private readonly ScholarTabViewModel _vm;
    private bool _suppressSelectionSync;

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
        var txtZhText = this.FindControl<TextBlock>("TxtZhText");
        var txtEnText = this.FindControl<TextBlock>("TxtEnText");

        if (txtSourcePath != null) txtSourcePath.Text = passage?.SourceRelPath ?? "";
        if (txtZhText != null) txtZhText.Text = passage?.ZhText ?? "";
        if (txtEnText != null) txtEnText.Text = passage?.EnText ?? "";
    }

    private void UpdateCommunityDetailFields()
    {
        var passage = _vm.SelectedCommunityPassage;

        var txtSourcePath = this.FindControl<TextBlock>("TxtSourcePath");
        var txtZhText = this.FindControl<TextBlock>("TxtZhText");
        var txtEnText = this.FindControl<TextBlock>("TxtEnText");

        if (txtSourcePath != null) txtSourcePath.Text = passage?.SourceRelPath ?? "";
        if (txtZhText != null) txtZhText.Text = passage?.ZhText ?? "";
        if (txtEnText != null) txtEnText.Text = passage?.EnText ?? "";

        // Update editor fields to show community passage metadata (read-only context)
        _vm.PassageNotes = passage?.Notes ?? "";
        _vm.PassageTags = passage != null ? string.Join(", ", passage.Tags) : "";
        _vm.PassageMasterNames = passage != null ? string.Join(", ", passage.MasterNames) : "";
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

    public void SetRoot(string root) => _vm.SetRoot(root);
    public void SetUsername(string? username) => _vm.SetUsername(username);

    public void Clear() => _vm.Clear();
    public void ReloadCommunity() => _vm.LoadCommunityCommand.Execute(null);

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
