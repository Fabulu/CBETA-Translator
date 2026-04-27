using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
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
    private ResearchGraphCanvasControl? _canvas;

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
        SetupCanvas();
        SetupKeyBindings();
        SetupEmptyState();
        UpdateStatusBar();
        UpdateEmptyState();
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

        var cmbCollection = this.FindControl<ComboBox>("CmbCollection");
        if (cmbCollection != null && _vm != null)
        {
            cmbCollection.ItemsSource = _vm.GetAllCollections();
            cmbCollection.SelectedItem = _vm.GetCollection();
            cmbCollection.SelectionChanged += (_, e) =>
            {
                if (cmbCollection.SelectedItem is ScholarCollection selected)
                {
                    _vm.SwitchToCollection(selected.Id);
                    _canvas?.InvalidateVisual();
                    UpdateStatusBar();
                    UpdateEmptyState();
                    UpdateInspector();
                    Title = $"Research Graph \u2014 {selected.Name}";
                }
            };
        }

        var txtSearch = this.FindControl<TextBox>("TxtSearch");
        txtSearch!.TextChanged += (_, _) =>
        {
            if (_vm != null) _vm.SearchText = txtSearch.Text ?? "";
        };
    }

    private void SetupCanvas()
    {
        _canvas = new ResearchGraphCanvasControl();
        _canvas.SetViewModel(_vm!);

        var canvasHost = this.FindControl<Grid>("CanvasHost");
        canvasHost!.Children.Insert(0, _canvas);

        _canvas.NodeClicked += (_, node) =>
        {
            _vm!.SelectedNode = node;
            node.IsSelected = true;
            foreach (var n in _vm.Nodes.Where(n => n != node)) n.IsSelected = false;
            UpdateInspector();
            _canvas.InvalidateVisual();
        };

        _canvas.NodeDoubleClicked += (_, node) =>
        {
            if (node.NodeType == ScholarNodeType.Collection)
            {
                var collId = node.NodeId.StartsWith("collection:") ? node.NodeId[11..] : node.NodeId;
                _vm?.SwitchToCollection(collId);
                _canvas?.InvalidateVisual();
                UpdateStatusBar();
                UpdateEmptyState();
                UpdateInspector();

                // Sync the collection dropdown
                var cmb = this.FindControl<ComboBox>("CmbCollection");
                if (cmb != null && _vm != null)
                {
                    cmb.SelectedItem = _vm.GetCollection();
                    Title = $"Research Graph \u2014 {_vm.GetCollection().Name}";
                }
            }
            else
            {
                // Default: select and inspect
                if (_vm != null)
                {
                    _vm.SelectedNode = node;
                    foreach (var n in _vm.Nodes) n.IsSelected = n == node;
                    UpdateInspector();
                    _canvas?.InvalidateVisual();
                }
            }
        };

        _canvas.EdgeDropped += async (_, args) =>
        {
            var validTypes = EdgeTypeRegistry.GetValidTypes(args.From.NodeType, args.To.NodeType);
            if (validTypes.Count == 0) return; // No valid edge types for this node pair

            var picker = new EdgeTypePickerPopup(args.From.NodeType, args.To.NodeType);
            var result = await picker.ShowDialog<object?>(this);
            if (result is EdgeTypeDefinition edgeType)
            {
                var edge = new ScholarGraphEdge
                {
                    Id = Guid.NewGuid().ToString("N")[..8],
                    FromNodeId = args.From.NodeId,
                    FromNodeType = args.From.NodeType,
                    ToNodeId = args.To.NodeId,
                    ToNodeType = args.To.NodeType,
                    RelationType = edgeType.Id,
                    CreatedUtc = DateTimeOffset.UtcNow
                };
                _vm!.ExecuteCommand(new AddEdgeCommand(_vm!, edge));
                _canvas.InvalidateVisual();
                UpdateStatusBar();
            }
        };

        // Right-click context menu
        _canvas.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(_canvas).Properties.IsRightButtonPressed)
            {
                var pos = e.GetPosition(_canvas);
                ShowContextMenu(_canvas, pos);
            }
        };
    }

    private void SetupKeyBindings()
    {
        KeyDown += (_, e) =>
        {
            if (e.KeyModifiers == KeyModifiers.Control)
            {
                switch (e.Key)
                {
                    case Key.Z: _vm?.Undo(); e.Handled = true; break;
                    case Key.Y: _vm?.Redo(); e.Handled = true; break;
                    case Key.L: ToggleLinkMode(); e.Handled = true; break;
                    case Key.F: FocusSearch(); e.Handled = true; break;
                }
            }
            else if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
            {
                switch (e.Key)
                {
                    case Key.C: OnAddConcept(null, null); e.Handled = true; break;
                }
            }
            else if (e.KeyModifiers == KeyModifiers.None)
            {
                switch (e.Key)
                {
                    case Key.Delete: DeleteSelected(); e.Handled = true; break;
                    case Key.Escape: CancelCurrentAction(); e.Handled = true; break;
                    case Key.F2: RenameSelected(); e.Handled = true; break;
                }
            }
        };
    }

    private void ShowContextMenu(Control target, Point position)
    {
        var menu = new ContextMenu();

        if (_vm?.SelectedNode != null)
        {
            var node = _vm.SelectedNode;
            switch (node.NodeType)
            {
                case ScholarNodeType.Passage:
                    menu.Items.Add(new MenuItem { Header = "Open in Reader" });
                    menu.Items.Add(new Separator());
                    menu.Items.Add(CreateMenuItem("Focus (Ego Network)", () => _vm.SetEgoMode(node.NodeId)));
                    menu.Items.Add(CreateMenuItem("Clear Focus", () => _vm.SetEgoMode(null)));
                    menu.Items.Add(new Separator());
                    menu.Items.Add(CreateMenuItem("Remove from Graph", () => DeleteNode(node.NodeId)));
                    break;
                case ScholarNodeType.Concept:
                    menu.Items.Add(CreateMenuItem("Rename (F2)", () => RenameSelected()));
                    menu.Items.Add(new Separator());
                    menu.Items.Add(CreateMenuItem("Focus (Ego Network)", () => _vm.SetEgoMode(node.NodeId)));
                    menu.Items.Add(CreateMenuItem("Clear Focus", () => _vm.SetEgoMode(null)));
                    menu.Items.Add(new Separator());
                    menu.Items.Add(CreateMenuItem("Remove from Graph", () => DeleteNode(node.NodeId)));
                    break;
                default:
                    menu.Items.Add(CreateMenuItem("Focus (Ego Network)", () => _vm.SetEgoMode(node.NodeId)));
                    menu.Items.Add(CreateMenuItem("Clear Focus", () => _vm.SetEgoMode(null)));
                    menu.Items.Add(new Separator());
                    menu.Items.Add(CreateMenuItem("Remove from Graph", () => DeleteNode(node.NodeId)));
                    break;
            }
        }
        else
        {
            // Canvas context menu (no node selected)
            menu.Items.Add(CreateMenuItem("Add Concept (Ctrl+Shift+C)", () => OnAddConcept(null, null)));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateMenuItem("Fit to View", () => { /* TODO */ }));
            menu.Items.Add(CreateMenuItem("Relayout", () => _vm?.RunForceDirectedLayout(800, 600)));
        }

        menu.Open(target);
    }

    private MenuItem CreateMenuItem(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    private async void OnAddConcept(object? sender, RoutedEventArgs? e)
    {
        if (_vm == null) return;
        var dialog = new ConceptCreationDialog();
        var result = await dialog.ShowDialog<ConceptNode?>(this);
        if (result != null)
        {
            _vm.ExecuteCommand(new AddConceptCommand(_vm, result));
            UpdateStatusBar();
            UpdateEmptyState();
            _canvas?.InvalidateVisual();
        }
    }

    private void DeleteSelected()
    {
        if (_vm?.SelectedNode == null) return;
        DeleteNode(_vm.SelectedNode.NodeId);
    }

    private void DeleteNode(string nodeId)
    {
        if (_vm == null) return;
        _vm.ExecuteCommand(new RemoveNodeCommand(_vm, nodeId));
        _vm.SelectedNode = null;
        UpdateInspector();
        UpdateStatusBar();
        UpdateEmptyState();
        _canvas?.InvalidateVisual();
    }

    private void CancelCurrentAction()
    {
        if (_vm != null)
        {
            _vm.SetEgoMode(null);
            _vm.SelectedNode = null;
            foreach (var n in _vm.Nodes) { n.IsSelected = false; n.IsDimmed = false; }
        }
        UpdateInspector();
        _canvas?.InvalidateVisual();
    }

    private void RenameSelected()
    {
        // TODO: inline rename for concepts
    }

    private void ToggleLinkMode()
    {
        var btn = this.FindControl<ToggleButton>("BtnLinkMode");
        if (btn != null) btn.IsChecked = !btn.IsChecked;
    }

    private void FocusSearch()
    {
        var txt = this.FindControl<TextBox>("TxtSearch");
        txt?.Focus();
    }

    private void UpdateInspector()
    {
        var content = this.FindControl<StackPanel>("InspectorContent");
        var empty = this.FindControl<TextBlock>("InspectorEmpty");
        var title = this.FindControl<TextBlock>("InspectorTitle");
        if (content == null) return;

        content.Children.Clear();

        if (_vm?.SelectedNode == null)
        {
            if (empty != null) { empty.IsVisible = true; content.Children.Add(empty); }
            if (title != null) title.Text = "Inspector";
            return;
        }
        if (empty != null) empty.IsVisible = false;

        var node = _vm.SelectedNode;
        if (title != null) title.Text = node.Label;

        // Type badge
        var typeNames = new[] { "Passage", "Concept", "Master", "Term", "Collection" };
        var typeName = typeNames[(int)node.NodeType];
        content.Children.Add(new TextBlock { Text = typeName, FontSize = 11, Opacity = 0.6 });

        // Node label
        content.Children.Add(new TextBlock { Text = node.Label, FontSize = 16, FontWeight = Avalonia.Media.FontWeight.Bold, TextWrapping = Avalonia.Media.TextWrapping.Wrap });

        // Degree
        content.Children.Add(new TextBlock { Text = $"Connections: {node.Degree}", FontSize = 11, Opacity = 0.7, Margin = new Avalonia.Thickness(0, 8, 0, 0) });

        // Type-specific content from collection data
        if (node.NodeType == ScholarNodeType.Passage)
        {
            var passage = _vm.GetCollection()?.Passages.FirstOrDefault(p => p.Id == node.NodeId);
            if (passage != null)
            {
                if (!string.IsNullOrWhiteSpace(passage.ZhText))
                    content.Children.Add(new TextBlock { Text = passage.ZhText.Length > 200 ? passage.ZhText[..200] + "\u2026" : passage.ZhText, FontSize = 12, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 8, 0, 0) });
                if (!string.IsNullOrWhiteSpace(passage.EnText))
                    content.Children.Add(new TextBlock { Text = passage.EnText.Length > 200 ? passage.EnText[..200] + "\u2026" : passage.EnText, FontSize = 11, Opacity = 0.8, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 4, 0, 0) });
            }
        }
        else if (node.NodeType == ScholarNodeType.Concept)
        {
            var concept = _vm.GetCollection()?.Concepts.FirstOrDefault(c => c.Id == node.NodeId);
            if (concept != null && !string.IsNullOrWhiteSpace(concept.Description))
                content.Children.Add(new TextBlock { Text = concept.Description, FontSize = 11, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 8, 0, 0) });
        }
    }

    private void SetupEmptyState()
    {
        var btnAddPassages = this.FindControl<Button>("BtnEmptyAddPassages");
        btnAddPassages?.AddHandler(Button.ClickEvent, (_, _) => OnAddPassage());

        var btnAddConcept = this.FindControl<Button>("BtnEmptyAddConcept");
        btnAddConcept?.AddHandler(Button.ClickEvent, (_, _) => OnAddConcept(null, null));
    }

    private void UpdateEmptyState()
    {
        var overlay = this.FindControl<Border>("EmptyStateOverlay");
        if (overlay == null) return;
        overlay.IsVisible = _vm == null || _vm.Nodes.Count == 0;
    }

    private void OnAddPassage()
    {
        // TODO: open passage picker dialog
    }

    private void UpdateStatusBar()
    {
        if (_vm == null) return;
        var nodeCount = this.FindControl<TextBlock>("TxtNodeCount");
        if (nodeCount != null)
            nodeCount.Text = $"{_vm.NodeCount} nodes, {_vm.EdgeCount} edges";
    }
}
