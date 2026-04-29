using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using ReadZen.App.Models;
using ReadZen.App.Services;
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
    private GraphStatisticsPanel? _statsPanel;
    private GraphLegendPanel? _legendPanel;
    private List<TermDisplayItem>? _termData;

    public event EventHandler<NavigationRequest>? NavigationRequested;
    public event EventHandler<string>? OpenMasterRequested;
    public event EventHandler<string>? DictionaryRequested;

    public ResearchGraphWindow()
    {
        InitializeComponent();
    }

    public ResearchGraphWindow(ScholarCollection collection, List<ScholarCollection> allCollections,
        List<TermDisplayItem>? termData = null)
    {
        InitializeComponent();
        _termData = termData;
        _vm = new ResearchGraphViewModel(collection, allCollections);
        Title = $"Research Graph \u2014 {collection.Name ?? collection.Id}";
        DataContext = _vm;

        SetupToolbar();
        SetupCanvas();
        SetupLeftPanels();
        SetupKeyBindings();
        SetupEmptyState();
        UpdateStatusBar();
        UpdateEmptyState();
        UpdateLeftPanels();

        Closing += (_, _) => _vm?.SaveLayoutToCollection();
    }

    private void SetupToolbar()
    {
        var btnAddConcept = this.FindControl<Button>("BtnAddConcept");
        btnAddConcept!.Click += OnAddConcept;

        var btnRelayout = this.FindControl<Button>("BtnRelayout");
        btnRelayout!.Click += (_, _) =>
        {
            double w = _canvas?.Bounds.Width > 0 ? _canvas.Bounds.Width : 800;
            double h = _canvas?.Bounds.Height > 0 ? _canvas.Bounds.Height : 600;
            _vm?.RunForceDirectedLayout(w, h);
            _canvas?.InvalidateVisual();
        };

        var btnFitView = this.FindControl<Button>("BtnFitView");
        btnFitView!.Click += (_, _) => _canvas?.FitToView();

        var btnUndo = this.FindControl<Button>("BtnUndo");
        btnUndo!.Click += (_, _) => { _vm?.Undo(); _canvas?.InvalidateVisual(); UpdateStatusBar(); UpdateLeftPanels(); };

        var btnRedo = this.FindControl<Button>("BtnRedo");
        btnRedo!.Click += (_, _) => { _vm?.Redo(); _canvas?.InvalidateVisual(); UpdateStatusBar(); UpdateLeftPanels(); };

        var txtSearch = this.FindControl<TextBox>("TxtSearch");
        txtSearch!.TextChanged += (_, _) =>
        {
            if (_vm != null)
            {
                _vm.SearchText = txtSearch.Text ?? "";
                _vm.HighlightSearch();
                _canvas?.InvalidateVisual();
            }
        };

        // + Passage button
        var btnAddPassage = this.FindControl<Button>("BtnAddPassage");
        if (btnAddPassage != null)
            btnAddPassage.Click += async (_, _) =>
            {
                if (_vm?.GetCollection() == null) return;
                var dialog = new PassagePickerDialog(_vm.GetCollection().Passages);
                var result = await dialog.ShowDialog<ScholarPassage?>(this);
                if (result != null && !_vm.Nodes.Any(n => n.NodeId == result.Id))
                {
                    var node = new ResearchGraphNode
                    {
                        NodeId = result.Id, NodeType = ScholarNodeType.Passage,
                        Label = result.DisplayTitle, ColorHex = "#6EAFF8",
                        X = _vm.Nodes.Count > 0 ? _vm.Nodes.Average(n => n.X) + 30 : 400,
                        Y = _vm.Nodes.Count > 0 ? _vm.Nodes.Average(n => n.Y) + 30 : 300,
                        SourceData = result
                    };
                    _vm.Nodes.Add(node);
                    _vm.RestoreNodeToMap(node);
                    _canvas?.InvalidateVisual();
                    UpdateStatusBar(); UpdateEmptyState(); UpdateLeftPanels();
                }
            };

        // + Master button
        var btnAddMaster = this.FindControl<Button>("BtnAddMaster");
        if (btnAddMaster != null)
            btnAddMaster.Click += async (_, _) =>
            {
                if (_vm == null) return;
                var masterNames = _vm.GetCollection().Passages
                    .SelectMany(p => p.MasterNames ?? new List<string>())
                    .Distinct().OrderBy(n => n).ToList();
                if (masterNames.Count == 0) return;
                var dialog = new MasterPickerDialog(masterNames);
                var result = await dialog.ShowDialog<string?>(this);
                if (!string.IsNullOrEmpty(result) && !_vm.Nodes.Any(n => n.NodeId == $"master:{result}"))
                {
                    var node = new ResearchGraphNode
                    {
                        NodeId = $"master:{result}", NodeType = ScholarNodeType.ZenMaster,
                        Label = result, ColorHex = "#64B5F6",
                        X = _vm.Nodes.Count > 0 ? _vm.Nodes.Average(n => n.X) + 30 : 400,
                        Y = _vm.Nodes.Count > 0 ? _vm.Nodes.Average(n => n.Y) + 30 : 300
                    };
                    _vm.Nodes.Add(node);
                    _vm.RestoreNodeToMap(node);
                    _canvas?.InvalidateVisual();
                    UpdateStatusBar(); UpdateLeftPanels();
                }
            };

        // + Term button
        var btnAddTerm = this.FindControl<Button>("BtnAddTerm");
        if (btnAddTerm != null)
            btnAddTerm.Click += async (_, _) =>
            {
                if (_vm == null) return;

                string? termName = null;
                string? termLabel = null;

                if (_termData != null && _termData.Count > 0)
                {
                    var dialog = new TermPickerDialog(_termData);
                    var result = await dialog.ShowDialog<TermDisplayItem?>(this);
                    if (result == null) return;
                    termName = result.SourceTerm;
                    termLabel = result.Display;
                }
                else
                {
                    // Fallback: simple text input when no termbase loaded
                    var renameWindow = new Window
                    {
                        Title = "Add Term", Width = 350, Height = 150,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner, CanResize = false
                    };
                    var grid = new Grid { RowDefinitions = RowDefinitions.Parse("*,Auto"), Margin = new Avalonia.Thickness(12) };
                    var txt = new TextBox { Watermark = "Enter Chinese or English term..." };
                    var btnPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 8 };
                    var btnOk = new Button { Content = "Add Term", Padding = new Avalonia.Thickness(12, 6) };
                    var btnCancel = new Button { Content = "Cancel", Padding = new Avalonia.Thickness(12, 6) };
                    btnPanel.Children.Add(btnCancel);
                    btnPanel.Children.Add(btnOk);
                    Grid.SetRow(txt, 0); Grid.SetRow(btnPanel, 1);
                    grid.Children.Add(txt); grid.Children.Add(btnPanel);
                    renameWindow.Content = grid;
                    btnOk.Click += (_, _) => { termName = txt.Text?.Trim(); renameWindow.Close(); };
                    btnCancel.Click += (_, _) => renameWindow.Close();
                    renameWindow.KeyDown += (_, e) =>
                    {
                        if (e.Key == Key.Return) { termName = txt.Text?.Trim(); renameWindow.Close(); }
                        if (e.Key == Key.Escape) renameWindow.Close();
                    };
                    await renameWindow.ShowDialog(this);
                    termLabel = termName;
                }

                // Find the full TermDisplayItem for SourceData
                TermDisplayItem? termItem = _termData?.FirstOrDefault(t => t.SourceTerm == termName);

                if (!string.IsNullOrEmpty(termName))
                {
                    var nodeId = $"term:{termName}";
                    if (_vm.Nodes.Any(n => n.NodeId == nodeId)) return;
                    var node = new ResearchGraphNode
                    {
                        NodeId = nodeId, NodeType = ScholarNodeType.TermbaseEntry,
                        Label = termLabel ?? termName, ColorHex = "#81C784",
                        SecondaryLabel = termItem?.PreferredTarget ?? "",
                        SourceData = termItem,
                        X = _vm.Nodes.Count > 0 ? _vm.Nodes.Average(n => n.X) + 30 : 400,
                        Y = _vm.Nodes.Count > 0 ? _vm.Nodes.Average(n => n.Y) + 30 : 300
                    };
                    _vm.Nodes.Add(node);
                    _vm.RestoreNodeToMap(node);
                    _canvas?.InvalidateVisual();
                    UpdateStatusBar(); UpdateLeftPanels();
                }
            };

        // + Collection button
        var btnAddCollection = this.FindControl<Button>("BtnAddCollection");
        if (btnAddCollection != null)
            btnAddCollection.Click += async (_, _) =>
            {
                if (_vm == null) return;
                var others = _vm.GetAllCollections().Where(c => c.Id != _vm.GetCollection().Id).ToList();
                if (others.Count == 0) return;
                var dialog = new MasterPickerDialog(others.Select(c => c.Name ?? c.Id));
                dialog.Title = "Add Collection Reference";
                var result = await dialog.ShowDialog<string?>(this);
                if (!string.IsNullOrEmpty(result))
                {
                    var target = others.FirstOrDefault(c => (c.Name ?? c.Id) == result);
                    if (target == null) return;
                    var nodeId = $"collection:{target.Id}";
                    if (_vm.Nodes.Any(n => n.NodeId == nodeId)) return;
                    var node = new ResearchGraphNode
                    {
                        NodeId = nodeId, NodeType = ScholarNodeType.Collection,
                        Label = target.Name ?? target.Id, ColorHex = "#AB47BC",
                        X = _vm.Nodes.Count > 0 ? _vm.Nodes.Average(n => n.X) + 30 : 400,
                        Y = _vm.Nodes.Count > 0 ? _vm.Nodes.Average(n => n.Y) + 30 : 300
                    };
                    _vm.Nodes.Add(node);
                    _vm.RestoreNodeToMap(node);
                    _canvas?.InvalidateVisual();
                    UpdateStatusBar(); UpdateLeftPanels();
                }
            };

        // Wire collection switcher
        var cmbCollection = this.FindControl<ComboBox>("CmbCollection");
        if (cmbCollection != null && _vm != null)
        {
            cmbCollection.ItemsSource = _vm.GetAllCollections();
            cmbCollection.SelectedItem = _vm.GetCollection();
            cmbCollection.SelectionChanged += (_, _) =>
            {
                if (cmbCollection.SelectedItem is ScholarCollection selected && _vm != null)
                {
                    _vm.SwitchToCollection(selected.Id);
                    Title = $"Research Graph \u2014 {selected.Name ?? selected.Id}";
                    _canvas?.InvalidateVisual();
                    UpdateStatusBar();
                    UpdateLeftPanels();
                    UpdateEmptyState();
                }
            };
        }

        // Overflow menu
        var btnOverflow = this.FindControl<Button>("BtnOverflow");
        if (btnOverflow != null)
            btnOverflow.Click += (_, _) =>
            {
                var menu = new ContextMenu();
                menu.Items.Add(CreateMenuItem("Fit to View", () => _canvas?.FitToView()));
                menu.Items.Add(CreateMenuItem("Relayout", () =>
                {
                    double w = _canvas?.Bounds.Width > 0 ? _canvas.Bounds.Width : 800;
                    double h = _canvas?.Bounds.Height > 0 ? _canvas.Bounds.Height : 600;
                    _vm?.RunForceDirectedLayout(w, h);
                    _canvas?.InvalidateVisual();
                }));
                menu.Open(btnOverflow);
            };

    }

    private void SetupCanvas()
    {
        _canvas = new ResearchGraphCanvasControl();
        _canvas.SetViewModel(_vm!);

        var canvasHost = this.FindControl<Grid>("CanvasHost");
        canvasHost!.Children.Insert(0, _canvas);

        _canvas.AttachedToVisualTree += (_, _) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_canvas.Bounds.Width > 0 && _canvas.Bounds.Height > 0 && _vm != null)
                {
                    var saved = _vm.GetCollection().GraphLayout;
                    bool hasSaved = saved?.NodePositions != null && saved.NodePositions.Count > 0;
                    if (!hasSaved && _vm.Nodes.Count > 1)
                    {
                        _vm.RunForceDirectedLayout(_canvas.Bounds.Width, _canvas.Bounds.Height);
                        _canvas.InvalidateVisual();
                    }
                }
            }, Avalonia.Threading.DispatcherPriority.Loaded);
        };

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
            if (_vm != null)
            {
                _vm.SelectedNode = node;
                foreach (var n in _vm.Nodes) n.IsSelected = n == node;
                UpdateInspector();
                _canvas?.InvalidateVisual();

                // Navigate based on node type
                switch (node.NodeType)
                {
                    case ScholarNodeType.Passage:
                        var passage = node.SourceData as ScholarPassage
                            ?? _vm.GetCollection()?.Passages.FirstOrDefault(p => p.Id == node.NodeId);
                        if (passage != null && !string.IsNullOrEmpty(passage.SourceRelPath))
                        {
                            NavigationRequested?.Invoke(this, new NavigationRequest
                            {
                                RelPath = passage.SourceRelPath,
                                Side = passage.PreferredSide,
                                MatchText = passage.ZhText?.Length > 20 ? passage.ZhText[..20] : passage.ZhText
                            });
                        }
                        break;
                    case ScholarNodeType.ZenMaster:
                        OpenMasterRequested?.Invoke(this, node.Label);
                        break;
                    case ScholarNodeType.TermbaseEntry:
                        DictionaryRequested?.Invoke(this, node.Label);
                        break;
                    case ScholarNodeType.Collection:
                        // Switch to the referenced collection in-place
                        var collId = node.NodeId.StartsWith("collection:") ? node.NodeId["collection:".Length..] : node.NodeId;
                        _vm.SwitchToCollection(collId);
                        _canvas?.InvalidateVisual();
                        UpdateStatusBar(); UpdateLeftPanels(); UpdateEmptyState();
                        Title = $"Research Graph \u2014 {_vm.GetCollection().Name ?? _vm.GetCollection().Id}";
                        break;
                }
            }
        };

        _canvas.EdgeDropped += async (_, args) =>
        {
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
                UpdateLeftPanels();
            }
        };

        _canvas.EdgeClicked += (_, edge) =>
        {
            var menu = new ContextMenu();
            menu.Items.Add(new MenuItem
            {
                Header = $"Edge Info: {edge.RelationType}",
                IsEnabled = false
            });
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateMenuItem("Delete Edge", () =>
            {
                _vm!.ExecuteCommand(new RemoveEdgeCommand(_vm!, edge.EdgeId));
                _canvas.InvalidateVisual();
                UpdateStatusBar();
                UpdateLeftPanels();
            }));
            menu.Open(_canvas);
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

    private void SetupLeftPanels()
    {
        var filterHost = this.FindControl<StackPanel>("FilterPanel");
        if (filterHost != null)
        {
            var fp = new GraphFilterPanel();
            filterHost.Children.Add(fp);
            fp.FiltersChanged += (_, _) =>
            {
                if (_vm == null) return;
                _vm.ShowPassages = fp.ShowPassages;
                _vm.ShowConcepts = fp.ShowConcepts;
                _vm.ShowMasters = fp.ShowMasters;
                _vm.ShowTerms = fp.ShowTerms;
                _vm.ShowCollections = fp.ShowCollections;
                _canvas?.InvalidateVisual();
            };
        }

        var statsHost = this.FindControl<StackPanel>("StatsPanel");
        if (statsHost != null)
        {
            _statsPanel = new GraphStatisticsPanel();
            statsHost.Children.Add(_statsPanel);
        }

        var legendHost = this.FindControl<StackPanel>("LegendPanel");
        if (legendHost != null)
        {
            _legendPanel = new GraphLegendPanel();
            legendHost.Children.Add(_legendPanel);
        }
    }

    private void UpdateLeftPanels()
    {
        if (_vm == null) return;
        _statsPanel?.UpdateStats(
            _vm.OrphanPassageCount, _vm.OrphanConceptCount,
            _vm.OverloadedConceptCount, _vm.WeakConceptCount, _vm.QualityScore);

        var legendEntries = _vm.GetVisibleEdges()
            .GroupBy(e => e.RelationType)
            .Select(g => new EdgeLegendEntry(g.Key, g.First().ColorHex, g.First().Label ?? g.Key))
            .ToList();
        _legendPanel?.UpdateLegend(legendEntries);
    }

    private void SetupKeyBindings()
    {
        KeyDown += (_, e) =>
        {
            if (e.KeyModifiers == KeyModifiers.Control)
            {
                switch (e.Key)
                {
                    case Key.Z: _vm?.Undo(); _canvas?.InvalidateVisual(); UpdateStatusBar(); UpdateLeftPanels(); e.Handled = true; break;
                    case Key.Y: _vm?.Redo(); _canvas?.InvalidateVisual(); UpdateStatusBar(); UpdateLeftPanels(); e.Handled = true; break;
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

            // Type-specific actions first
            switch (node.NodeType)
            {
                case ScholarNodeType.Passage:
                    var passage = _vm.GetCollection()?.Passages.FirstOrDefault(p => p.Id == node.NodeId);
                    menu.Items.Add(CreateMenuItem("Open in Reader", () =>
                    {
                        if (passage != null && !string.IsNullOrEmpty(passage.SourceRelPath))
                            NavigationRequested?.Invoke(this, new NavigationRequest { RelPath = passage.SourceRelPath, Side = passage.PreferredSide });
                    }));
                    if (passage != null && !string.IsNullOrEmpty(passage.SourceRelPath))
                    {
                        menu.Items.Add(CreateMenuItem("Copy Web Link", async () =>
                        {
                            var url = ZenUriParser.BuildShareableUrl(passage.SourceRelPath, fromLb: passage.StartBlockNumber?.ToString());
                            var top = TopLevel.GetTopLevel(this);
                            if (top?.Clipboard != null) await top.Clipboard.SetTextAsync(url);
                        }));
                    }
                    break;
                case ScholarNodeType.ZenMaster:
                    menu.Items.Add(CreateMenuItem("Open Master Page", () => OpenMasterRequested?.Invoke(this, node.Label)));
                    menu.Items.Add(CreateMenuItem("Copy Web Link", async () =>
                    {
                        var url = ZenUriParser.BuildShareableMasterUrl(node.Label);
                        var top = TopLevel.GetTopLevel(this);
                        if (top?.Clipboard != null) await top.Clipboard.SetTextAsync(url);
                    }));
                    break;
                case ScholarNodeType.TermbaseEntry:
                    menu.Items.Add(CreateMenuItem("Open in Dictionary", () => DictionaryRequested?.Invoke(this, node.Label)));
                    break;
                case ScholarNodeType.Concept:
                    menu.Items.Add(CreateMenuItem("Rename (F2)", () => RenameSelected()));
                    break;
                case ScholarNodeType.Collection:
                    menu.Items.Add(CreateMenuItem("Switch to Collection", () =>
                    {
                        var collId = node.NodeId.StartsWith("collection:") ? node.NodeId["collection:".Length..] : node.NodeId;
                        _vm.SwitchToCollection(collId);
                        _canvas?.InvalidateVisual(); UpdateStatusBar(); UpdateLeftPanels();
                    }));
                    break;
            }

            // Common actions for all nodes
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateMenuItem("Focus (Ego Network)", () => _vm.SetEgoMode(node.NodeId)));
            menu.Items.Add(CreateMenuItem("Clear Focus", () => _vm.SetEgoMode(null)));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateMenuItem("Remove from Graph", () => DeleteNode(node.NodeId)));
        }
        else
        {
            // Canvas context menu (no node selected)
            menu.Items.Add(CreateMenuItem("Add Concept (Ctrl+Shift+C)", () => OnAddConcept(null, null)));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateMenuItem("Fit to View", () => _canvas?.FitToView()));
            menu.Items.Add(CreateMenuItem("Relayout", () =>
            {
                double w = _canvas?.Bounds.Width > 0 ? _canvas.Bounds.Width : 800;
                double h = _canvas?.Bounds.Height > 0 ? _canvas.Bounds.Height : 600;
                _vm?.RunForceDirectedLayout(w, h);
                _canvas?.InvalidateVisual();
            }));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateMenuItem("Copy Graph Web Link", async () =>
            {
                var col = _vm?.GetCollection();
                if (col == null) return;
                var url = ZenUriParser.BuildShareableGraphUrl(col.Id ?? col.Name ?? "");
                var top = TopLevel.GetTopLevel(this);
                if (top?.Clipboard != null) await top.Clipboard.SetTextAsync(url);
            }));
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
            UpdateLeftPanels();
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
        _vm?.ExecuteCommand(new RemoveNodeCommand(_vm, nodeId));
        _vm!.SelectedNode = null;
        UpdateInspector();
        UpdateStatusBar();
        UpdateEmptyState();
        UpdateLeftPanels();
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

    private async void RenameSelected()
    {
        if (_vm?.SelectedNode == null) return;
        var node = _vm.SelectedNode;

        var renameWindow = new Window
        {
            Title = "Rename Node", Width = 350, Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, CanResize = false
        };
        var grid = new Grid { RowDefinitions = RowDefinitions.Parse("*,Auto"), Margin = new Avalonia.Thickness(12) };
        var txt = new TextBox { Text = node.Label };
        var btnPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 8 };
        var btnOk = new Button { Content = "Rename", Padding = new Avalonia.Thickness(12, 6) };
        var btnCancel = new Button { Content = "Cancel", Padding = new Avalonia.Thickness(12, 6) };
        btnPanel.Children.Add(btnCancel);
        btnPanel.Children.Add(btnOk);
        Grid.SetRow(txt, 0); Grid.SetRow(btnPanel, 1);
        grid.Children.Add(txt); grid.Children.Add(btnPanel);
        renameWindow.Content = grid;
        string? newLabel = null;
        btnOk.Click += (_, _) => { newLabel = txt.Text?.Trim(); renameWindow.Close(); };
        btnCancel.Click += (_, _) => renameWindow.Close();
        renameWindow.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Return) { newLabel = txt.Text?.Trim(); renameWindow.Close(); }
            if (e.Key == Key.Escape) renameWindow.Close();
        };
        await renameWindow.ShowDialog(this);
        if (!string.IsNullOrEmpty(newLabel) && newLabel != node.Label)
        {
            node.Label = newLabel;
            UpdateInspector();
            _canvas?.InvalidateVisual();
        }
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
                // Source file
                if (!string.IsNullOrWhiteSpace(passage.SourceRelPath))
                    content.Children.Add(new TextBlock { Text = $"Source: {System.IO.Path.GetFileNameWithoutExtension(passage.SourceRelPath)}", FontSize = 10, Opacity = 0.5, Margin = new Avalonia.Thickness(0, 4, 0, 0) });
                // Date added
                if (passage.AddedUtc != default)
                    content.Children.Add(new TextBlock { Text = $"Added: {passage.AddedUtc.LocalDateTime:yyyy-MM-dd}", FontSize = 10, Opacity = 0.5 });
                // Tags
                if (passage.Tags?.Count > 0)
                    content.Children.Add(new TextBlock { Text = $"Tags: {string.Join(", ", passage.Tags)}", FontSize = 10, Opacity = 0.6, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 4, 0, 0) });
                // Masters
                if (passage.MasterNames?.Count > 0)
                    content.Children.Add(new TextBlock { Text = $"Masters: {string.Join(", ", passage.MasterNames)}", FontSize = 10, Opacity = 0.6, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
                // Doctrinal topic
                if (!string.IsNullOrWhiteSpace(passage.DoctrinalTopic))
                    content.Children.Add(new TextBlock { Text = $"Topic: {passage.DoctrinalTopic}", FontSize = 10, Opacity = 0.6 });
                // Translator
                if (!string.IsNullOrWhiteSpace(passage.TranslationUser))
                    content.Children.Add(new TextBlock { Text = $"Translator: {passage.TranslationUser}", FontSize = 10, Opacity = 0.6 });
                // Chinese text
                if (!string.IsNullOrWhiteSpace(passage.ZhText))
                    content.Children.Add(new TextBlock { Text = passage.ZhText.Length > 200 ? passage.ZhText[..200] + "\u2026" : passage.ZhText, FontSize = 12, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 8, 0, 0) });
                // English text
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
        else if (node.NodeType == ScholarNodeType.ZenMaster)
        {
            if (node.SourceData is Dictionary<string, object> masterData)
            {
                if (masterData.TryGetValue("PassageCount", out var countObj))
                    content.Children.Add(new TextBlock
                    {
                        Text = $"Referenced in {countObj} passage(s)",
                        FontSize = 11, Opacity = 0.8,
                        Margin = new Avalonia.Thickness(0, 8, 0, 0)
                    });
                if (masterData.TryGetValue("Passages", out var listObj) && listObj is List<string> titles)
                {
                    foreach (var t in titles.Take(5))
                        content.Children.Add(new TextBlock
                        {
                            Text = $"\u2022 {t}",
                            FontSize = 11, Opacity = 0.7, TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            Margin = new Avalonia.Thickness(8, 2, 0, 0)
                        });
                    if (titles.Count > 5)
                        content.Children.Add(new TextBlock
                        {
                            Text = $"... and {titles.Count - 5} more",
                            FontSize = 10, Opacity = 0.5, Margin = new Avalonia.Thickness(8, 2, 0, 0)
                        });
                }
            }
        }
        else if (node.NodeType == ScholarNodeType.TermbaseEntry)
        {
            content.Children.Add(new TextBlock
            {
                Text = $"Term: {node.Label}",
                FontSize = 12, Opacity = 0.9,
                Margin = new Avalonia.Thickness(0, 8, 0, 0)
            });
            if (!string.IsNullOrEmpty(node.SecondaryLabel))
                content.Children.Add(new TextBlock
                {
                    Text = $"Preferred: {node.SecondaryLabel}",
                    FontSize = 11, Opacity = 0.8, TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(0, 4, 0, 0)
                });
            // Show alternate targets if SourceData is TermDisplayItem
            if (node.SourceData is TermDisplayItem termItem && termItem.AlternateTargets.Count > 0)
            {
                content.Children.Add(new TextBlock
                {
                    Text = "Alternates: " + string.Join(", ", termItem.AlternateTargets),
                    FontSize = 10, Opacity = 0.6, TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(0, 2, 0, 0)
                });
            }
        }

        // Inspector action buttons
        var actions = this.FindControl<StackPanel>("InspectorActions");
        if (actions != null)
        {
            actions.Children.Clear();

            if (node.NodeType == ScholarNodeType.Passage)
            {
                var passage = _vm.GetCollection()?.Passages.FirstOrDefault(p => p.Id == node.NodeId);
                var openBtn = new Button { Content = "Open in Reader", Padding = new Thickness(8, 4), FontSize = 11 };
                openBtn.Click += (_, _) =>
                {
                    if (passage != null && !string.IsNullOrEmpty(passage.SourceRelPath))
                        NavigationRequested?.Invoke(this, new NavigationRequest { RelPath = passage.SourceRelPath, Side = passage.PreferredSide });
                };
                actions.Children.Add(openBtn);

                if (passage != null && !string.IsNullOrEmpty(passage.SourceRelPath))
                {
                    var copyLinkBtn = new Button { Content = "Copy Web Link", Padding = new Thickness(8, 4), FontSize = 11 };
                    copyLinkBtn.Click += async (_, _) =>
                    {
                        var url = ZenUriParser.BuildShareableUrl(passage.SourceRelPath, fromLb: passage.StartBlockNumber?.ToString());
                        var top = TopLevel.GetTopLevel(this);
                        if (top?.Clipboard != null) await top.Clipboard.SetTextAsync(url);
                    };
                    actions.Children.Add(copyLinkBtn);
                }
            }

            if (node.NodeType == ScholarNodeType.ZenMaster)
            {
                var profileBtn = new Button { Content = "Master Profile", Padding = new Thickness(8, 4), FontSize = 11 };
                profileBtn.Click += (_, _) => OpenMasterRequested?.Invoke(this, node.Label);
                actions.Children.Add(profileBtn);

                var copyLinkBtn = new Button { Content = "Copy Web Link", Padding = new Thickness(8, 4), FontSize = 11 };
                copyLinkBtn.Click += async (_, _) =>
                {
                    var url = ZenUriParser.BuildShareableMasterUrl(node.Label);
                    var top = TopLevel.GetTopLevel(this);
                    if (top?.Clipboard != null) await top.Clipboard.SetTextAsync(url);
                };
                actions.Children.Add(copyLinkBtn);
            }
        }
    }

    private void SetupEmptyState()
    {
        var btnAddPassages = this.FindControl<Button>("BtnEmptyAddPassages");
        btnAddPassages?.AddHandler(Button.ClickEvent, (_, _) => OnAddPassageFromEmpty());

        var btnAddConcept = this.FindControl<Button>("BtnEmptyAddConcept");
        btnAddConcept?.AddHandler(Button.ClickEvent, (_, _) => OnAddConcept(null, null));
    }

    private void UpdateEmptyState()
    {
        var overlay = this.FindControl<Border>("EmptyStateOverlay");
        if (overlay == null) return;
        overlay.IsVisible = _vm == null || _vm.Nodes.Count == 0;
    }

    private async void OnAddPassageFromEmpty()
    {
        if (_vm?.GetCollection() == null) return;
        var dialog = new PassagePickerDialog(_vm.GetCollection().Passages);
        var result = await dialog.ShowDialog<ScholarPassage?>(this);
        if (result != null && !_vm.Nodes.Any(n => n.NodeId == result.Id))
        {
            var node = new ResearchGraphNode
            {
                NodeId = result.Id, NodeType = ScholarNodeType.Passage,
                Label = result.DisplayTitle, ColorHex = "#6EAFF8",
                X = _vm.Nodes.Count > 0 ? _vm.Nodes.Average(n => n.X) + 30 : 400,
                Y = _vm.Nodes.Count > 0 ? _vm.Nodes.Average(n => n.Y) + 30 : 300,
                SourceData = result
            };
            _vm.Nodes.Add(node);
            _vm.RestoreNodeToMap(node);
            _canvas?.InvalidateVisual();
            UpdateStatusBar(); UpdateEmptyState(); UpdateLeftPanels();
        }
    }

    private void UpdateStatusBar()
    {
        if (_vm == null) return;
        var nodeCount = this.FindControl<TextBlock>("TxtNodeCount");
        if (nodeCount != null)
            nodeCount.Text = $"{_vm.NodeCount} nodes, {_vm.EdgeCount} edges";
    }
}
