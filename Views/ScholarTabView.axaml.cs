using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CbetaTranslator.App.Views;

public partial class ScholarTabView : UserControl
{
    private readonly ScholarTabViewModel _vm;

    public event EventHandler<string>? Status;
    public event EventHandler<NavigationRequest>? NavigationRequested;

    public ScholarTabView()
    {
        InitializeComponent();

        _vm = new ScholarTabViewModel(App.Services.GetRequiredService<IScholarCollectionsService>());
        DataContext = _vm;

        _vm.StatusChanged += (_, msg) => Status?.Invoke(this, msg);
        _vm.NavigationRequested += (_, req) => NavigationRequested?.Invoke(this, req);

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
            if (e.PropertyName == nameof(ScholarTabViewModel.SelectedPassage))
            {
                UpdateDetailFields();
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

    // ----- Public API -----

    public void SetRoot(string root) => _vm.SetRoot(root);

    public void Clear() => _vm.Clear();

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
