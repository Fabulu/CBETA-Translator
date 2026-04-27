using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;

namespace ReadZen.App.Views;

/// <summary>
/// Window for building and exploring research knowledge graphs with passage, concept,
/// master, term, and collection node types.
/// </summary>
public partial class ResearchGraphWindow : Window
{
    private ResearchGraphViewModel? _vm;

    public ResearchGraphWindow()
    {
        InitializeComponent();
    }

    public ResearchGraphWindow(ScholarCollection collection, List<ScholarCollection> allCollections)
    {
        InitializeComponent();
        _vm = new ResearchGraphViewModel(collection, allCollections);
        DataContext = _vm;

        SetupToolbar();
        UpdateStatusBar();
    }

    private void SetupToolbar()
    {
        var btnAddConcept = this.FindControl<Button>("BtnAddConcept");
        btnAddConcept!.Click += OnAddConcept;

        var btnRelayout = this.FindControl<Button>("BtnRelayout");
        btnRelayout!.Click += (_, _) => _vm?.RunForceDirectedLayout(800, 600);

        var btnFitView = this.FindControl<Button>("BtnFitView");
        btnFitView!.Click += (_, _) => { /* TODO: fit view */ };

        var btnUndo = this.FindControl<Button>("BtnUndo");
        btnUndo!.Click += (_, _) => _vm?.Undo();

        var btnRedo = this.FindControl<Button>("BtnRedo");
        btnRedo!.Click += (_, _) => _vm?.Redo();

        var txtSearch = this.FindControl<TextBox>("TxtSearch");
        txtSearch!.TextChanged += (_, _) =>
        {
            if (_vm != null) _vm.SearchText = txtSearch.Text ?? "";
        };
    }

    private void OnAddConcept(object? sender, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var concept = new ConceptNode
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Name = "New Concept",
            CreatedUtc = DateTimeOffset.UtcNow
        };
        _vm.AddConcept(concept);
        UpdateStatusBar();
    }

    private void UpdateStatusBar()
    {
        if (_vm == null) return;
        var nodeCount = this.FindControl<TextBlock>("TxtNodeCount");
        if (nodeCount != null)
            nodeCount.Text = $"{_vm.NodeCount} nodes, {_vm.EdgeCount} edges";
    }
}
