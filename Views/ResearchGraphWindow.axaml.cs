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

        // Type-specific content using SourceData
        if (node.NodeType == ScholarNodeType.Passage && node.SourceData is ScholarPassage passage)
        {
            // Summary highlight box
            if (!string.IsNullOrWhiteSpace(passage.Summary))
            {
                var summaryBorder = new Avalonia.Controls.Border
                {
                    BorderThickness = new Avalonia.Thickness(2, 0, 0, 0),
                    BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#d4ab58")),
                    Padding = new Avalonia.Thickness(8, 4),
                    Margin = new Avalonia.Thickness(0, 8, 0, 0)
                };
                summaryBorder.Child = new TextBlock { Text = passage.Summary, TextWrapping = Avalonia.Media.TextWrapping.Wrap, FontSize = 12 };
                content.Children.Add(summaryBorder);
            }

            // Chinese text
            if (!string.IsNullOrWhiteSpace(passage.ZhText))
                AddSection(content, "Chinese", passage.ZhText.Length > 300 ? passage.ZhText[..300] + "\u2026" : passage.ZhText);

            // English text
            if (!string.IsNullOrWhiteSpace(passage.EnText))
                AddSection(content, "English", passage.EnText.Length > 300 ? passage.EnText[..300] + "\u2026" : passage.EnText);

            // Tags
            if (passage.Tags?.Count > 0)
                AddChips(content, "Tags", passage.Tags);

            // Masters
            if (passage.MasterNames?.Count > 0)
                AddChips(content, "Masters", passage.MasterNames);

            // Reading status + importance
            if (!string.IsNullOrWhiteSpace(passage.ReadingStatus))
                AddSection(content, "Status", passage.ReadingStatus);
            if (passage.Importance.HasValue && passage.Importance > 0)
                AddSection(content, "Importance", new string('\u2605', passage.Importance.Value) + new string('\u2606', 5 - passage.Importance.Value));

            // Facets
            if (!string.IsNullOrWhiteSpace(passage.DoctrinalTopic))
                AddSection(content, "Doctrine", passage.DoctrinalTopic);
            if (!string.IsNullOrWhiteSpace(passage.Lineage))
                AddSection(content, "Lineage", passage.Lineage);

            // Notes
            if (!string.IsNullOrWhiteSpace(passage.Notes))
                AddSection(content, "Notes", passage.Notes.Length > 200 ? passage.Notes[..200] + "\u2026" : passage.Notes);

            // Source reference
            if (!string.IsNullOrWhiteSpace(passage.SourceRelPath))
                AddSection(content, "Source", passage.SourceRelPath);
        }
        else if (node.NodeType == ScholarNodeType.Concept && node.SourceData is ConceptNode concept)
        {
            if (!string.IsNullOrWhiteSpace(concept.Description))
                AddSection(content, "Description", concept.Description);

            if (concept.Tags?.Count > 0)
                AddChips(content, "Tags", concept.Tags);

            if (concept.Status != ConceptStatus.Active)
                AddSection(content, "Status", concept.Status.ToString());

            // Count linked edges
            var linkedCount = _vm.Edges.Count(e =>
                e.From.NodeId == concept.Id || e.To.NodeId == concept.Id);
            AddSection(content, "Connections", $"{linkedCount} edges");
        }
        else
        {
            // Master/Term/Collection -- show what we have
            AddSection(content, "Type", node.NodeType.ToString());
            AddSection(content, "Connections", $"{node.Degree} edges");
        }
    }

    private static void AddSection(StackPanel parent, string label, string value)
    {
        var section = new StackPanel { Spacing = 2, Margin = new Avalonia.Thickness(0, 4, 0, 0) };
        section.Children.Add(new TextBlock { Text = label, FontSize = 11, FontWeight = Avalonia.Media.FontWeight.SemiBold, Opacity = 0.6 });
        section.Children.Add(new TextBlock { Text = value, FontSize = 12, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        parent.Children.Add(section);
    }

    private static void AddChips(StackPanel parent, string label, List<string> items)
    {
        var section = new StackPanel { Spacing = 4, Margin = new Avalonia.Thickness(0, 4, 0, 0) };
        section.Children.Add(new TextBlock { Text = label, FontSize = 11, FontWeight = Avalonia.Media.FontWeight.SemiBold, Opacity = 0.6 });
        var wrap = new Avalonia.Controls.WrapPanel();
        foreach (var item in items.Take(8))
        {
            var chip = new Avalonia.Controls.Border
            {
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(40, 255, 255, 255)),
                CornerRadius = new Avalonia.CornerRadius(3),
                Padding = new Avalonia.Thickness(6, 2),
                Margin = new Avalonia.Thickness(0, 0, 4, 4)
            };
            chip.Child = new TextBlock { Text = item, FontSize = 10 };
            wrap.Children.Add(chip);
        }
        section.Children.Add(wrap);
        parent.Children.Add(section);
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
