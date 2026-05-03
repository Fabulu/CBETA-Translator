using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
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
    public Func<IReadOnlyList<FileNavItem>>? FileItems { get; set; }
    public Func<string, TextLicenseInfo?>? TextMetadataLookup { get; set; }
    public Func<string, (string? En, string? EnShort, string? Zh)>? TitleLookup { get; set; }
    private GraphStatisticsPanel? _statsPanel;
    private GraphLegendPanel? _legendPanel;
    private List<TermDisplayItem>? _termData;

    public event EventHandler<NavigationRequest>? NavigationRequested;
    public event EventHandler<string>? OpenMasterRequested;
    public event EventHandler<string>? DictionaryRequested;
    public event EventHandler<ScholarPassage>? AdoptPassageRequested;
    public event Action? SaveRequested;

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
        _vm.MasterLookup = BuildMasterLookup();
        _vm.RebuildGraph(); // Re-run with master lookup now available
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

        Closing += (_, _) =>
        {
            if (_vm != null && _canvas != null)
            {
                _vm.SavedZoom = _canvas.CurrentZoom;
                _vm.SavedOffsetX = _canvas.CurrentOffsetX;
                _vm.SavedOffsetY = _canvas.CurrentOffsetY;
                _vm.SaveLayoutToCollection();
                SaveRequested?.Invoke();
            }
        };
    }

    private void SetupToolbar()
    {
        var btnAddConcept = this.FindControl<Button>("BtnAddConcept");
        btnAddConcept!.Click += OnAddConcept;

        var btnRelayout = this.FindControl<Button>("BtnRelayout");
        btnRelayout!.Click += (_, _) =>
        {
            if (_vm != null)
            {
                foreach (var n in _vm.Nodes) n.IsPinned = false;
            }
            double w = _canvas?.Bounds.Width > 0 ? _canvas.Bounds.Width : 800;
            double h = _canvas?.Bounds.Height > 0 ? _canvas.Bounds.Height : 600;
            _vm?.RunForceDirectedLayout(w, h);
            _canvas?.InvalidateVisual();
        };

        var btnPhysics = this.FindControl<ToggleButton>("BtnPhysics");
        btnPhysics!.IsCheckedChanged += (_, _) =>
        {
            if (_canvas != null)
                _canvas.IsPhysicsEnabled = btnPhysics.IsChecked == true;
        };

        var btnLabels = this.FindControl<ToggleButton>("BtnLabels");
        btnLabels!.IsCheckedChanged += (_, _) =>
        {
            if (_canvas != null)
            {
                _canvas.ShowLabels = btnLabels.IsChecked == true;
                _canvas.InvalidateVisual();
            }
        };

        var btnMinimap = this.FindControl<ToggleButton>("BtnMinimap");
        btnMinimap!.IsCheckedChanged += (_, _) =>
        {
            if (_canvas != null)
            {
                _canvas.ShowMinimap = btnMinimap.IsChecked == true;
                _canvas.InvalidateVisual();
            }
        };

        var btnClusters = this.FindControl<ToggleButton>("BtnClusters");
        btnClusters!.IsCheckedChanged += (_, _) =>
        {
            if (_canvas != null)
            {
                _canvas.ShowClusters = btnClusters.IsChecked == true;
                _canvas.InvalidateVisual();
            }
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
                // Get ALL known masters from the master-dates.json catalog
                var masterNames = new List<string>();
                try
                {
                    var path = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "master-dates.json");
                    if (System.IO.File.Exists(path))
                    {
                        var json = System.IO.File.ReadAllText(path);
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("masters", out var mastersEl))
                        {
                            foreach (var m in mastersEl.EnumerateArray())
                            {
                                if (m.TryGetProperty("names", out var names) && names.GetArrayLength() > 0)
                                {
                                    var name = names[0].GetString();
                                    if (!string.IsNullOrEmpty(name)) masterNames.Add(name);
                                }
                            }
                        }
                    }
                }
                catch { }
                // Fallback to passage-derived masters if catalog unavailable
                if (masterNames.Count == 0)
                    masterNames = _vm.GetCollection().Passages
                        .SelectMany(p => p.MasterNames ?? new List<string>())
                        .Distinct().ToList();
                masterNames = masterNames.OrderBy(n => n).ToList();
                if (masterNames.Count == 0) return;
                var dialog = new MasterPickerDialog(masterNames);
                dialog.Title = "Add Master to Graph";
                var result = await dialog.ShowDialog<string?>(this);
                if (!string.IsNullOrEmpty(result) && !_vm.Nodes.Any(n => n.NodeId == $"master:{result}"))
                {
                    var record = _vm.MasterLookup?.Invoke(result);
                    var label = record?.CanonicalName ?? result;
                    var sourceData = new Dictionary<string, object>
                    {
                        ["PassageCount"] = 0,
                        ["Passages"] = new List<string>()
                    };
                    if (record != null) sourceData["MasterRecord"] = record;
                    var node = new ResearchGraphNode
                    {
                        NodeId = $"master:{result}", NodeType = ScholarNodeType.ZenMaster,
                        Label = label, SecondaryLabel = record?.DatesSummary,
                        ColorHex = "#FFB74D", SourceData = sourceData,
                        X = _vm.Nodes.Count > 0 ? _vm.Nodes.Average(n => n.X) + 30 : 400,
                        Y = _vm.Nodes.Count > 0 ? _vm.Nodes.Average(n => n.Y) + 30 : 300
                    };
                    _vm.Nodes.Add(node);
                    _vm.RestoreNodeToMap(node);
                    // Persist so the master survives reload
                    var coll = _vm.GetCollection();
                    coll.ExtraMasters ??= new List<string>();
                    if (!coll.ExtraMasters.Contains(result))
                        coll.ExtraMasters.Add(result);
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
                var dialog = new MasterPickerDialog(
                    others.Select(c => c.Name ?? c.Id),
                    searchWatermark: "Search collections...",
                    okButtonText: "Add Collection");
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
                    // Persist to backing collection data
                    _vm.GetCollection().CollectionRefs ??= new();
                    if (!_vm.GetCollection().CollectionRefs.Any(r => r.CollectionId == target.Id))
                    {
                        _vm.GetCollection().CollectionRefs.Add(new CollectionRefNode
                        {
                            CollectionId = target.Id,
                            CollectionName = target.Name,
                            IsShared = false,
                            OwnerUsername = target.CreatedBy
                        });
                    }
                    _canvas?.InvalidateVisual();
                    UpdateStatusBar(); UpdateLeftPanels();
                }
            };

        // + Book button
        var btnAddBook = this.FindControl<Button>("BtnAddBook");
        if (btnAddBook != null)
            btnAddBook.Click += async (_, _) =>
            {
                var fileItems = FileItems?.Invoke();
                if (_vm == null || fileItems == null || fileItems.Count == 0) return;
                var picker = new BookPickerDialog(fileItems);
                var result = await picker.ShowDialog<BookPickerDialog.BookEntry?>(this);
                if (result == null) return;
                // Check for duplicate
                if (_vm.Nodes.Any(n => n.NodeType == ScholarNodeType.Book
                    && (n.SourceData as ScholarPassage)?.SourceRelPath == result.RelPath))
                    return;
                var passage = new ScholarPassage
                {
                    Id = Guid.NewGuid().ToString("N"),
                    AnnotationType = "Book",
                    SourceRelPath = result.RelPath,
                    Summary = result.DisplayShort,
                    AddedUtc = DateTimeOffset.UtcNow,
                };
                var zhPart = result.Subtitle;
                if (!string.IsNullOrWhiteSpace(zhPart))
                    passage.Tags.Add("zh:" + zhPart);
                _vm.GetCollection().Passages.Add(passage);
                var node = new ResearchGraphNode
                {
                    NodeId = passage.Id,
                    NodeType = ScholarNodeType.Book,
                    Label = result.DisplayShort,
                    SecondaryLabel = result.RelPath,
                    ColorHex = "#D4A574",
                    SourceData = passage,
                    X = _vm.Nodes.Count > 0 ? _vm.Nodes.Average(n => n.X) + 30 : 400,
                    Y = _vm.Nodes.Count > 0 ? _vm.Nodes.Average(n => n.Y) + 30 : 300
                };
                _vm.Nodes.Add(node);
                _vm.RestoreNodeToMap(node);
                _canvas?.InvalidateVisual();
                UpdateStatusBar(); UpdateLeftPanels();
            };

        var btnAddLink = this.FindControl<Button>("BtnAddLink");
        if (btnAddLink != null)
            btnAddLink.Click += async (_, _) =>
            {
                if (_vm == null) return;
                var dialog = new LinkCreationDialog();
                var result = await dialog.ShowDialog<LinkNode?>(this);
                if (result == null) return;

                var collection = _vm.GetCollection();
                collection.LinkNodes ??= new List<LinkNode>();
                collection.LinkNodes.Add(result);

                var node = new ResearchGraphNode
                {
                    NodeId = $"link:{result.Id}",
                    NodeType = ScholarNodeType.Link,
                    Label = result.Name,
                    SecondaryLabel = result.Url,
                    ColorHex = "#78909C",
                    SourceData = result,
                    X = _vm.Nodes.Count > 0 ? _vm.Nodes.Average(n => n.X) + 30 : 400,
                    Y = _vm.Nodes.Count > 0 ? _vm.Nodes.Average(n => n.Y) + 30 : 300
                };
                _vm.Nodes.Add(node);
                _vm.RestoreNodeToMap(node);
                _canvas?.InvalidateVisual();
                UpdateStatusBar(); UpdateLeftPanels();
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
                    var btnBackCmb = this.FindControl<Button>("BtnBack");
                    if (btnBackCmb != null) btnBackCmb.IsVisible = _vm.CanGoBack;
                }
            };
        }

        // Back button
        var btnBack = this.FindControl<Button>("BtnBack");
        if (btnBack != null)
            btnBack.Click += (_, _) =>
            {
                if (_vm == null || !_vm.CanGoBack) return;
                _vm.GoBack();
                var viewport = _vm.GetSavedViewport();
                if (viewport.HasValue)
                    _canvas?.SetViewport(viewport.Value.zoom, viewport.Value.offsetX, viewport.Value.offsetY);
                else
                    _canvas?.FitToView();
                _canvas?.InvalidateVisual();
                Title = $"Research Graph \u2014 {_vm.GetCollection().Name ?? _vm.GetCollection().Id}";
                UpdateStatusBar(); UpdateLeftPanels(); UpdateEmptyState();
                btnBack.IsVisible = _vm.CanGoBack;
            };

        // Share button — copies graph web link
        var btnShare = this.FindControl<Button>("BtnShareGraph");
        if (btnShare != null)
            btnShare.Click += async (_, _) =>
            {
                var col = _vm?.GetCollection();
                if (col == null) return;
                var url = ZenUriParser.BuildShareableGraphUrl(col.Name ?? col.Id ?? "", col.CreatedBy);
                var top = TopLevel.GetTopLevel(this);
                if (top?.Clipboard != null) await top.Clipboard.SetTextAsync(url);
                var status = this.FindControl<TextBlock>("TxtStatus");
                if (status != null) status.Text = "Graph link copied!";
            };

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
                menu.Items.Add(new Separator());
                menu.Items.Add(CreateMenuItem("Export Nodes CSV", () => { _ = ExportNodesCsvAsync(); }));
                menu.Items.Add(CreateMenuItem("Export Edges CSV", () => { _ = ExportEdgesCsvAsync(); }));
                menu.Items.Add(CreateMenuItem("Copy Summary", () => { _ = CopySummaryAsync(); }));
                menu.Items.Add(new Separator());
                menu.Items.Add(CreateMenuItem("Copy Screenshot", () => { _ = CopyScreenshotAsync(); }));
                menu.Items.Add(new Separator());
                menu.Items.Add(CreateMenuItem("Export GEXF", () => { _ = ExportGexfAsync(); }));
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
                    var savedViewport = _vm.GetSavedViewport();
                    if (savedViewport.HasValue)
                    {
                        // Restore saved zoom/pan exactly as user left it
                        _canvas.SetViewport(savedViewport.Value.zoom,
                            savedViewport.Value.offsetX, savedViewport.Value.offsetY);
                    }
                    else if (_vm.Nodes.Count > 1)
                    {
                        // No saved layout — run force layout then FitToView
                        _vm.RunForceDirectedLayout(_canvas.Bounds.Width, _canvas.Bounds.Height);
                        _canvas.FitToView();
                    }
                    else if (_vm.Nodes.Count == 1)
                    {
                        _canvas.FitToView();
                    }
                }
            }, Avalonia.Threading.DispatcherPriority.Loaded);
        };

        _canvas.NodeClicked += (_, args) =>
        {
            var node = args.Node;
            if (args.IsCtrlHeld)
            {
                // Toggle selection without clearing others
                node.IsSelected = !node.IsSelected;
                _vm!.SelectedNode = node.IsSelected ? node : _vm.Nodes.FirstOrDefault(n => n.IsSelected);
            }
            else
            {
                // Single select: clear all, select clicked
                _vm!.SelectedNode = node;
                node.IsSelected = true;
                foreach (var n in _vm.Nodes.Where(n => n != node)) n.IsSelected = false;
            }
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
                            NavigationRequested?.Invoke(this, BuildPassageNavRequest(passage));
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
                        var btnBackDblClick = this.FindControl<Button>("BtnBack");
                        if (btnBackDblClick != null) btnBackDblClick.IsVisible = _vm.CanGoBack;
                        break;
                    case ScholarNodeType.Book:
                        var bookPassage = node.SourceData as ScholarPassage;
                        if (bookPassage != null && !string.IsNullOrEmpty(bookPassage.SourceRelPath))
                            NavigationRequested?.Invoke(this, BuildPassageNavRequest(bookPassage));
                        break;
                    case ScholarNodeType.Link:
                        if (node.SourceData is LinkNode dblLink && !string.IsNullOrEmpty(dblLink.Url))
                        {
                            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dblLink.Url) { UseShellExecute = true }); }
                            catch { }
                        }
                        break;
                }
            }
        };

        _canvas.EdgeDropped += async (_, args) =>
        {
            var customTypes = _vm?.GetCollection()?.CustomEdgeTypes;
            var picker = new EdgeTypePickerPopup(args.From.NodeType, args.To.NodeType, customTypes,
                fromTypeName: args.From.Label, toTypeName: args.To.Label);
            var result = await picker.ShowDialog<object?>(this);
            if (result is EdgeTypeDefinition edgeType)
            {
                // If user created a custom type, persist it to the collection
                if (!edgeType.IsBuiltIn && _vm != null)
                {
                    var collection = _vm.GetCollection();
                    if (!collection.CustomEdgeTypes.Any(t => t.Id == edgeType.Id))
                        collection.CustomEdgeTypes.Add(edgeType);
                }

                var fromId = args.From.NodeId;
                var toId = args.To.NodeId;
                var fromType = args.From.NodeType;
                var toType = args.To.NodeType;

                switch (picker.SelectedDirection)
                {
                    case EdgeDirection.Reverse:
                        (fromId, toId) = (toId, fromId);
                        (fromType, toType) = (toType, fromType);
                        break;
                    case EdgeDirection.Bidirectional:
                        // Create second (reverse) edge after the forward one
                        break;
                    case EdgeDirection.Undirected:
                        // Single edge, but not directional
                        break;
                }

                var edge = new ScholarGraphEdge
                {
                    Id = Guid.NewGuid().ToString("N")[..8],
                    FromNodeId = fromId,
                    FromNodeType = fromType,
                    ToNodeId = toId,
                    ToNodeType = toType,
                    RelationType = edgeType.Id,
                    CreatedUtc = DateTimeOffset.UtcNow
                };
                _vm!.ExecuteCommand(new AddEdgeCommand(_vm!, edge));

                // For bidirectional: add the reverse edge too
                if (picker.SelectedDirection == EdgeDirection.Bidirectional)
                {
                    var reverseEdge = new ScholarGraphEdge
                    {
                        Id = Guid.NewGuid().ToString("N")[..8],
                        FromNodeId = toId,
                        FromNodeType = toType,
                        ToNodeId = fromId,
                        ToNodeType = fromType,
                        RelationType = edgeType.Id,
                        CreatedUtc = DateTimeOffset.UtcNow
                    };
                    _vm.ExecuteCommand(new AddEdgeCommand(_vm, reverseEdge));
                }

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
                Header = $"Edge: {edge.Label ?? edge.RelationType}",
                IsEnabled = false
            });
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateMenuItem("Change Edge Type", async () =>
            {
                var customTypes = _vm?.GetCollection()?.CustomEdgeTypes;
                var picker = new EdgeTypePickerPopup(edge.From.NodeType, edge.To.NodeType, customTypes,
                    fromTypeName: edge.From.Label, toTypeName: edge.To.Label);
                var result = await picker.ShowDialog<object?>(this);
                if (result is EdgeTypeDefinition edgeType)
                {
                    // Update backing model
                    var collEdge = _vm!.GetCollection().Edges.FirstOrDefault(e => e.Id == edge.EdgeId);
                    if (collEdge != null) collEdge.RelationType = edgeType.Id;
                    // Update VM edge
                    edge.RelationType = edgeType.Id;
                    var def = EdgeTypeRegistry.GetById(edgeType.Id, _vm!.GetCollection().CustomEdgeTypes);
                    edge.Label = def?.DisplayName ?? edgeType.DisplayName ?? edgeType.Id;
                    edge.IsDirectional = def?.IsDirectional ?? edgeType.IsDirectional;
                    edge.ColorHex = def?.ColorHex ?? edgeType.ColorHex ?? "#9E9E9E";
                    _canvas.InvalidateVisual();
                    UpdateStatusBar(); UpdateLeftPanels();
                }
            }));
            menu.Items.Add(CreateMenuItem("Reverse Direction", () =>
            {
                // Swap on backing model
                var collEdge = _vm!.GetCollection().Edges.FirstOrDefault(e => e.Id == edge.EdgeId);
                if (collEdge != null)
                {
                    (collEdge.FromNodeId, collEdge.ToNodeId) = (collEdge.ToNodeId, collEdge.FromNodeId);
                    (collEdge.FromNodeType, collEdge.ToNodeType) = (collEdge.ToNodeType, collEdge.FromNodeType);
                }
                // Swap on VM edge
                (edge.From, edge.To) = (edge.To, edge.From);
                _canvas.InvalidateVisual();
            }));
            menu.Items.Add(CreateMenuItem("Edit Note", async () =>
            {
                var collEdge = _vm!.GetCollection().Edges.FirstOrDefault(e => e.Id == edge.EdgeId);
                var noteWin = new Window
                {
                    Title = "Edit Edge Note", Width = 350, Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner, CanResize = false
                };
                var grid = new Grid { RowDefinitions = RowDefinitions.Parse("*,Auto"), Margin = new Avalonia.Thickness(12) };
                var txt = new TextBox { Text = collEdge?.Note ?? "", Watermark = "Enter note..." };
                var btnPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 8 };
                var btnOk = new Button { Content = "Save", Padding = new Avalonia.Thickness(12, 6) };
                var btnCancel = new Button { Content = "Cancel", Padding = new Avalonia.Thickness(12, 6) };
                btnPanel.Children.Add(btnCancel); btnPanel.Children.Add(btnOk);
                Grid.SetRow(txt, 0); Grid.SetRow(btnPanel, 1);
                grid.Children.Add(txt); grid.Children.Add(btnPanel);
                noteWin.Content = grid;
                string? newNote = null;
                btnOk.Click += (_, _) => { newNote = txt.Text?.Trim(); noteWin.Close(); };
                btnCancel.Click += (_, _) => noteWin.Close();
                noteWin.KeyDown += (_, e2) =>
                {
                    if (e2.Key == Key.Return) { newNote = txt.Text?.Trim(); noteWin.Close(); }
                    if (e2.Key == Key.Escape) noteWin.Close();
                };
                await noteWin.ShowDialog(this);
                if (newNote != null && collEdge != null)
                {
                    collEdge.Note = newNote;
                    edge.Label = string.IsNullOrEmpty(newNote) ? (EdgeTypeRegistry.GetById(edge.RelationType, _vm!.GetCollection().CustomEdgeTypes)?.DisplayName ?? edge.RelationType) : newNote;
                    _canvas.InvalidateVisual();
                }
            }));
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
                _vm.ShowLinks = fp.ShowLinks;
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
                    // Ctrl+L was Link Mode (removed — edges are created via node handles)
                    case Key.F: FocusSearch(); e.Handled = true; break;
                    case Key.A:
                        if (_vm != null) foreach (var n in _vm.Nodes) n.IsSelected = true;
                        _canvas?.InvalidateVisual(); e.Handled = true; break;
                    case Key.D:
                        if (_vm != null) foreach (var n in _vm.Nodes) n.IsSelected = false;
                        _canvas?.InvalidateVisual(); e.Handled = true; break;
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

        // Check for multi-selection
        var selectedNodes = _canvas?.GetSelectedNodes();
        if (selectedNodes != null && selectedNodes.Count > 1)
        {
            int count = selectedNodes.Count;
            menu.Items.Add(CreateMenuItem($"Delete Selected ({count})", () =>
            {
                foreach (var n in selectedNodes.ToList())
                    DeleteNode(n.NodeId);
            }));
            menu.Items.Add(CreateMenuItem("Pin Selected", () =>
            {
                foreach (var n in selectedNodes) n.IsPinned = true;
                _canvas?.InvalidateVisual();
            }));
            menu.Items.Add(CreateMenuItem("Unpin Selected", () =>
            {
                foreach (var n in selectedNodes) n.IsPinned = false;
                _canvas?.InvalidateVisual();
            }));

            // Compare selected passages (2-4 passage nodes)
            var selectedPassages = selectedNodes
                .Where(n => n.NodeType == ScholarNodeType.Passage)
                .Select(n => n.SourceData as ScholarPassage
                    ?? _vm?.GetCollection()?.Passages.FirstOrDefault(p => p.Id == n.NodeId))
                .Where(p => p != null)
                .ToList();
            if (selectedPassages.Count >= 2 && selectedPassages.Count <= 4)
            {
                menu.Items.Add(new Separator());
                menu.Items.Add(CreateMenuItem($"Compare {selectedPassages.Count} Passages", async () =>
                {
                    var window = new ComparePassagesWindow(selectedPassages!);
                    var owner = TopLevel.GetTopLevel(this) as Window ?? this;
                    await window.ShowDialog(owner);
                }));
            }

            menu.Open(target);
            return;
        }

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
                            NavigationRequested?.Invoke(this, BuildPassageNavRequest(passage));
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
                    if (passage != null)
                    {
                        menu.Items.Add(new Separator());
                        menu.Items.Add(CreateMenuItem("Add to My Collection", () =>
                            AdoptPassageRequested?.Invoke(this, passage)));
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
                    menu.Items.Add(CreateMenuItem("Merge with Concept...", async () =>
                    {
                        var otherConcepts = _vm.Nodes
                            .Where(n => n.NodeType == ScholarNodeType.Concept && n.NodeId != node.NodeId)
                            .Select(n => n.Label)
                            .ToList();
                        if (otherConcepts.Count == 0) return;
                        var picker = new MasterPickerDialog(otherConcepts, okButtonText: "Merge");
                        picker.Title = "Select Concept to Merge Into This One";
                        var result = await picker.ShowDialog<string?>(this);
                        if (string.IsNullOrEmpty(result)) return;
                        var mergedNode = _vm.Nodes.FirstOrDefault(n => n.NodeType == ScholarNodeType.Concept && n.Label == result);
                        if (mergedNode == null) return;
                        // Move all edges from merged to surviving
                        var edgesToMove = _vm.Edges.Where(e => e.From.NodeId == mergedNode.NodeId || e.To.NodeId == mergedNode.NodeId).ToList();
                        foreach (var e in edgesToMove)
                        {
                            var collEdge = _vm.GetCollection().Edges.FirstOrDefault(ce => ce.Id == e.EdgeId);
                            if (e.From.NodeId == mergedNode.NodeId) { e.From.Degree--; e.From = node; node.Degree++; if (collEdge != null) collEdge.FromNodeId = node.NodeId; }
                            if (e.To.NodeId == mergedNode.NodeId) { e.To.Degree--; e.To = node; node.Degree++; if (collEdge != null) collEdge.ToNodeId = node.NodeId; }
                            // Remove self-edges
                            if (e.From.NodeId == e.To.NodeId)
                            {
                                e.From.Degree--; e.To.Degree--;
                                _vm.Edges.Remove(e);
                                _vm.GetCollection().Edges.RemoveAll(ce => ce.Id == e.EdgeId);
                            }
                        }
                        // Remove merged node
                        _vm.Nodes.Remove(mergedNode);
                        _vm.GetCollection().Concepts.RemoveAll(c => c.Id == mergedNode.NodeId);
                        _canvas?.InvalidateVisual();
                        UpdateStatusBar(); UpdateLeftPanels();
                    }));
                    break;
                case ScholarNodeType.Collection:
                    menu.Items.Add(CreateMenuItem("Switch to Collection", () =>
                    {
                        var collId = node.NodeId.StartsWith("collection:") ? node.NodeId["collection:".Length..] : node.NodeId;
                        _vm.SwitchToCollection(collId);
                        _canvas?.InvalidateVisual(); UpdateStatusBar(); UpdateLeftPanels();
                    }));
                    break;
                case ScholarNodeType.Link:
                    if (node.SourceData is LinkNode ctxLink && !string.IsNullOrEmpty(ctxLink.Url))
                    {
                        menu.Items.Add(CreateMenuItem("Open in Browser", () =>
                        {
                            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ctxLink.Url) { UseShellExecute = true }); }
                            catch { }
                        }));
                        menu.Items.Add(CreateMenuItem("Copy URL", async () =>
                        {
                            var top = TopLevel.GetTopLevel(this);
                            if (top?.Clipboard != null) await top.Clipboard.SetTextAsync(ctxLink.Url);
                        }));
                    }
                    break;
            }

            // Common actions for all nodes
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateMenuItem("Add Note", async () =>
            {
                var collection = _vm.GetCollection();
                if (collection == null) return;
                var existing = collection.NodeAnnotations.TryGetValue(node.NodeId, out var note) ? note : "";
                var noteWindow = new Window
                {
                    Title = "Node Note", Width = 400, Height = 180,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner, CanResize = false
                };
                var grid = new Grid { RowDefinitions = RowDefinitions.Parse("*,Auto"), Margin = new Avalonia.Thickness(12) };
                var txt = new TextBox { Text = existing, AcceptsReturn = true, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
                var btnPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 8 };
                var btnOk = new Button { Content = "Save", Padding = new Avalonia.Thickness(12, 6) };
                var btnCancel = new Button { Content = "Cancel", Padding = new Avalonia.Thickness(12, 6) };
                btnPanel.Children.Add(btnCancel);
                btnPanel.Children.Add(btnOk);
                Grid.SetRow(txt, 0); Grid.SetRow(btnPanel, 1);
                grid.Children.Add(txt); grid.Children.Add(btnPanel);
                noteWindow.Content = grid;
                string? result = null;
                btnOk.Click += (_, _) => { result = txt.Text?.Trim(); noteWindow.Close(); };
                btnCancel.Click += (_, _) => noteWindow.Close();
                await noteWindow.ShowDialog(this);
                if (result != null)
                {
                    if (string.IsNullOrWhiteSpace(result))
                        collection.NodeAnnotations.Remove(node.NodeId);
                    else
                        collection.NodeAnnotations[node.NodeId] = result;
                    UpdateInspector();
                }
            }));
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
            menu.Items.Add(CreateMenuItem("Unpin All Nodes", () =>
            {
                if (_vm != null)
                    foreach (var n in _vm.Nodes) n.IsPinned = false;
                _canvas?.InvalidateVisual();
            }));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateMenuItem("Select All (Ctrl+A)", () =>
            {
                if (_vm != null)
                    foreach (var n in _vm.Nodes) n.IsSelected = true;
                _canvas?.InvalidateVisual();
            }));
            menu.Items.Add(CreateMenuItem("Deselect All (Ctrl+D)", () =>
            {
                if (_vm != null)
                    foreach (var n in _vm.Nodes) n.IsSelected = false;
                _canvas?.InvalidateVisual();
            }));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateMenuItem("Zoom to 100%", () =>
            {
                if (_canvas != null && _vm != null)
                {
                    var visible = _vm.GetVisibleNodes();
                    double cx = visible.Count > 0 ? visible.Average(n => n.X) : 0;
                    double cy = visible.Count > 0 ? visible.Average(n => n.Y) : 0;
                    double viewW = _canvas.Bounds.Width > 0 ? _canvas.Bounds.Width : 800;
                    double viewH = _canvas.Bounds.Height > 0 ? _canvas.Bounds.Height : 600;
                    _canvas.SetViewport(1.0, viewW / 2.0 - cx, viewH / 2.0 - cy);
                }
            }));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateMenuItem("Copy Graph Web Link", async () =>
            {
                var col = _vm?.GetCollection();
                if (col == null) return;
                var url = ZenUriParser.BuildShareableGraphUrl(col.Name ?? col.Id ?? "", col.CreatedBy);
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
        if (_vm == null) return;
        // Remove from ExtraMasters if it was manually added
        if (nodeId.StartsWith("master:"))
        {
            var masterName = nodeId["master:".Length..];
            _vm.GetCollection()?.ExtraMasters?.Remove(masterName);
        }
        // Remove from LinkNodes if it was a link
        if (nodeId.StartsWith("link:"))
        {
            var linkId = nodeId["link:".Length..];
            _vm.GetCollection()?.LinkNodes?.RemoveAll(l => l.Id == linkId);
        }
        _vm.ExecuteCommand(new RemoveNodeCommand(_vm, nodeId));
        _vm.SelectedNode = null;
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

    private static Func<string, ZenMasterRecord?>? BuildMasterLookup()
    {
        try
        {
            // Load base master-dates.json synchronously (no community data needed here).
            // Avoid LoadAsync().GetAwaiter().GetResult() which can deadlock on UI thread.
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "master-dates.json");
            if (!System.IO.File.Exists(path)) return null;

            var svc = new ZenMasterManagerService(new MasterDatesService());
            var records = new List<ZenMasterRecord>();
            using var doc = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("masters", out var mastersEl)) return null;

            foreach (var m in mastersEl.EnumerateArray())
            {
                var names = new List<string>();
                if (m.TryGetProperty("names", out var namesEl))
                    foreach (var n in namesEl.EnumerateArray())
                    {
                        var s = n.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(s)) names.Add(s);
                    }
                if (names.Count == 0) continue;

                var floruit = m.TryGetProperty("floruit", out var fv) ? fv.GetInt32() : 0;
                var death = m.TryGetProperty("death", out var dv) ? dv.GetInt32() : 0;
                var school = m.TryGetProperty("school", out var sv) ? sv.GetString() : null;
                var teacher = m.TryGetProperty("teacher", out var tv) ? tv.GetString() : null;
                var notes = m.TryGetProperty("notes", out var nv) ? nv.GetString() : null;
                var students = new List<string>();
                if (m.TryGetProperty("students", out var stEl))
                    foreach (var st in stEl.EnumerateArray())
                    {
                        var s = st.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(s)) students.Add(s);
                    }
                var links = new List<MasterLink>();
                if (m.TryGetProperty("links", out var lnEl))
                    foreach (var ln in lnEl.EnumerateArray())
                        links.Add(new MasterLink
                        {
                            Label = ln.TryGetProperty("label", out var lbl) ? lbl.GetString() : null,
                            Url = ln.TryGetProperty("url", out var url) ? url.GetString() ?? "" : ""
                        });

                var variant = new ZenMasterVariant
                {
                    Names = names, Floruit = floruit, Death = death, IsBase = true,
                    School = school, Teacher = teacher, Notes = notes,
                    Students = students, Links = links
                };
                var rec = records.FirstOrDefault(r =>
                    r.Aliases.Any(a => names.Any(n => string.Equals(a, n, StringComparison.OrdinalIgnoreCase))));
                if (rec == null)
                {
                    rec = new ZenMasterRecord { CanonicalName = names[0], Aliases = new List<string>(names) };
                    records.Add(rec);
                }
                rec.Variants.Add(variant);
            }

            return name => svc.FindByName(records, name);
        }
        catch { return null; }
    }

    private static NavigationRequest BuildPassageNavRequest(ScholarPassage p)
    {
        var side = p.PreferredSide;
        var text = side == SearchSide.Translated ? p.EnText : p.ZhText;
        if (string.IsNullOrEmpty(text)) text = p.ZhText;
        var matchText = text?.Length > 80 ? text[..80] : text;
        return new NavigationRequest
        {
            RelPath = p.SourceRelPath,
            Side = side,
            User = side == SearchSide.Translated ? p.TranslationUser : null,
            MatchText = matchText,
            FromLb = p.FromLb,
            ToLb = p.ToLb,
            AnchorStartHint = p.StartBlockNumber
        };
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
            if (node.NodeType == ScholarNodeType.Concept)
            {
                _vm.ExecuteCommand(new RenameConceptCommand(_vm, node.NodeId, node.Label, newLabel));
            }
            else if (node.NodeType == ScholarNodeType.Link && node.SourceData is LinkNode linkData)
            {
                linkData.Name = newLabel;
                node.Label = newLabel;
            }
            else
            {
                node.Label = newLabel;
            }
            UpdateInspector();
            _canvas?.InvalidateVisual();
        }
    }

    private void FocusSearch()
    {
        var txt = this.FindControl<TextBox>("TxtSearch");
        txt?.Focus();
    }

    private static readonly Avalonia.Media.FontFamily CjkFontFamily = new("'Noto Serif CJK SC', 'Source Han Serif SC', SimSun, 'Songti SC', serif");

    private (string? en, string? enShort, string? zh) LookupTitle(string? relPath)
    {
        if (string.IsNullOrWhiteSpace(relPath)) return (null, null, null);
        return TitleLookup?.Invoke(relPath) ?? (null, null, null);
    }

    /// <summary>
    /// Adds source text info to the inspector: English title, Chinese title (from TEI
    /// if not in titles.jsonl), author, dynasty, composed date. No clutter.
    /// </summary>
    private void AddSourceTextInfo(StackPanel content, string? relPath)
    {
        if (string.IsNullOrWhiteSpace(relPath)) return;
        var (en, enShort, zh) = LookupTitle(relPath);

        // English title prominently
        if (!string.IsNullOrWhiteSpace(en))
            content.Children.Add(new SelectableTextBlock { Text = en, FontSize = 11, Opacity = 0.8, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 4, 0, 0) });
        else if (!string.IsNullOrWhiteSpace(enShort))
            content.Children.Add(new SelectableTextBlock { Text = enShort, FontSize = 11, Opacity = 0.8, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 4, 0, 0) });

        // Chinese title: from titles.jsonl, or fallback to TEI header
        if (!string.IsNullOrWhiteSpace(zh))
            content.Children.Add(new SelectableTextBlock { Text = zh, FontSize = 11, Opacity = 0.7, FontFamily = CjkFontFamily, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 2, 0, 0) });
        else if (TextMetadataLookup != null)
        {
            var info = TextMetadataLookup(relPath);
            if (info != null && !string.IsNullOrWhiteSpace(info.TitleZh))
                content.Children.Add(new SelectableTextBlock { Text = info.TitleZh, FontSize = 11, Opacity = 0.7, FontFamily = CjkFontFamily, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 2, 0, 0) });
        }

        // Author, dynasty, date from TEI header
        if (TextMetadataLookup != null)
        {
            var info = TextMetadataLookup(relPath);
            if (info != null)
            {
                if (!string.IsNullOrWhiteSpace(info.Author))
                    content.Children.Add(new SelectableTextBlock { Text = $"Author: {info.Author}", FontSize = 10, Opacity = 0.7, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
                if (!string.IsNullOrWhiteSpace(info.Dynasty))
                    content.Children.Add(new SelectableTextBlock { Text = $"Dynasty: {info.Dynasty}", FontSize = 10, Opacity = 0.7 });
                if (!string.IsNullOrWhiteSpace(info.YearComposed))
                    content.Children.Add(new SelectableTextBlock { Text = $"Composed: {info.YearComposed}", FontSize = 10, Opacity = 0.7 });
            }
        }
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
        var typeNames = new[] { "Passage", "Concept", "Master", "Term", "Collection", "Text", "Link" };
        var typeName = typeNames[(int)node.NodeType];
        content.Children.Add(new SelectableTextBlock { Text = typeName, FontSize = 11, Opacity = 0.6 });

        // Node label
        content.Children.Add(new SelectableTextBlock { Text = node.Label, FontSize = 16, FontWeight = Avalonia.Media.FontWeight.Bold, TextWrapping = Avalonia.Media.TextWrapping.Wrap });

        // Degree
        content.Children.Add(new SelectableTextBlock { Text = $"Connections: {node.Degree}", FontSize = 11, Opacity = 0.7, Margin = new Avalonia.Thickness(0, 8, 0, 0) });

        // Node annotation
        var collection = _vm.GetCollection();
        if (collection?.NodeAnnotations != null && collection.NodeAnnotations.TryGetValue(node.NodeId, out var annotation) && !string.IsNullOrWhiteSpace(annotation))
        {
            content.Children.Add(new SelectableTextBlock
            {
                Text = $"Note: {annotation}",
                FontSize = 11, FontStyle = Avalonia.Media.FontStyle.Italic,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Opacity = 0.8, Margin = new Avalonia.Thickness(0, 4, 0, 0)
            });
        }

        // Type-specific content from collection data
        if (node.NodeType == ScholarNodeType.Passage)
        {
            var passage = _vm.GetCollection()?.Passages.FirstOrDefault(p => p.Id == node.NodeId);
            if (passage != null)
            {
                // Source text info (EN title, ZH title, author, dynasty, date)
                AddSourceTextInfo(content, passage.SourceRelPath);
                // Date added
                if (passage.AddedUtc != default)
                    content.Children.Add(new SelectableTextBlock { Text = $"Added: {passage.AddedUtc.LocalDateTime:yyyy-MM-dd}", FontSize = 10, Opacity = 0.5 });
                // Tags
                if (passage.Tags?.Count > 0)
                    content.Children.Add(new SelectableTextBlock { Text = $"Tags: {string.Join(", ", passage.Tags)}", FontSize = 10, Opacity = 0.6, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 4, 0, 0) });
                // Masters
                if (passage.MasterNames?.Count > 0)
                    content.Children.Add(new SelectableTextBlock { Text = $"Masters: {string.Join(", ", passage.MasterNames)}", FontSize = 10, Opacity = 0.6, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
                // Doctrinal topic
                if (!string.IsNullOrWhiteSpace(passage.DoctrinalTopic))
                    content.Children.Add(new SelectableTextBlock { Text = $"Topic: {passage.DoctrinalTopic}", FontSize = 10, Opacity = 0.6 });
                // Translator
                if (!string.IsNullOrWhiteSpace(passage.TranslationUser))
                    content.Children.Add(new SelectableTextBlock { Text = $"Translator: {passage.TranslationUser}", FontSize = 10, Opacity = 0.6 });
                // Chinese text (full, no truncation)
                if (!string.IsNullOrWhiteSpace(passage.ZhText))
                    content.Children.Add(new SelectableTextBlock { Text = passage.ZhText, FontSize = 12, FontFamily = CjkFontFamily, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 8, 0, 0) });
                // English text (full, no truncation)
                if (!string.IsNullOrWhiteSpace(passage.EnText))
                    content.Children.Add(new SelectableTextBlock { Text = passage.EnText, FontSize = 11, Opacity = 0.8, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 4, 0, 0) });
            }
        }
        else if (node.NodeType == ScholarNodeType.Concept)
        {
            var concept = _vm.GetCollection()?.Concepts.FirstOrDefault(c => c.Id == node.NodeId);
            if (concept != null && !string.IsNullOrWhiteSpace(concept.Description))
                content.Children.Add(new SelectableTextBlock { Text = concept.Description, FontSize = 11, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 8, 0, 0) });
            if (concept?.Tags?.Count > 0)
                content.Children.Add(new SelectableTextBlock { Text = $"Tags: {string.Join(", ", concept.Tags)}", FontSize = 10, Opacity = 0.6, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 4, 0, 0) });
        }
        else if (node.NodeType == ScholarNodeType.ZenMaster)
        {
            if (node.SourceData is Dictionary<string, object> masterData)
            {
                // Rich master info from ZenMasterRecord
                if (masterData.TryGetValue("MasterRecord", out var recObj) && recObj is ZenMasterRecord rec)
                {
                    if (rec.Aliases.Count > 1)
                        content.Children.Add(new SelectableTextBlock
                        {
                            Text = string.Join(" \u00B7 ", rec.Aliases.Take(4)),
                            FontSize = 11, Opacity = 0.6, TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            Margin = new Avalonia.Thickness(0, 4, 0, 0)
                        });
                    content.Children.Add(new SelectableTextBlock
                    {
                        Text = rec.DatesSummary,
                        FontSize = 11, Opacity = 0.8,
                        Margin = new Avalonia.Thickness(0, 4, 0, 0)
                    });
                    if (!string.IsNullOrWhiteSpace(rec.School))
                        content.Children.Add(new SelectableTextBlock { Text = $"School: {rec.School}", FontSize = 11, Opacity = 0.8 });
                    if (!string.IsNullOrWhiteSpace(rec.Teacher))
                        content.Children.Add(new SelectableTextBlock { Text = $"Teacher: {rec.Teacher}", FontSize = 11, Opacity = 0.8 });
                    if (rec.Students.Count > 0)
                        content.Children.Add(new SelectableTextBlock
                        {
                            Text = $"Students: {string.Join(", ", rec.Students.Take(8))}{(rec.Students.Count > 8 ? "..." : "")}",
                            FontSize = 11, Opacity = 0.8, TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        });
                    if (!string.IsNullOrWhiteSpace(rec.Notes))
                    {
                        var bio = rec.Notes.Length > 300 ? rec.Notes[..300] + "..." : rec.Notes;
                        content.Children.Add(new SelectableTextBlock
                        {
                            Text = bio, FontSize = 10, Opacity = 0.6,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            Margin = new Avalonia.Thickness(0, 6, 0, 0)
                        });
                    }
                    foreach (var link in rec.Links.Take(3))
                    {
                        var linkTb = new TextBlock
                        {
                            Text = $"\uD83D\uDD17 {link.Label ?? link.Url}",
                            FontSize = 10, Opacity = 0.7,
                            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                            Margin = new Avalonia.Thickness(0, 2, 0, 0)
                        };
                        var url = link.Url;
                        linkTb.PointerPressed += (_, _) =>
                        {
                            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
                            catch { }
                        };
                        content.Children.Add(linkTb);
                    }
                }

                // Passage references
                if (masterData.TryGetValue("PassageCount", out var countObj))
                    content.Children.Add(new SelectableTextBlock
                    {
                        Text = $"Referenced in {countObj} passage(s)",
                        FontSize = 11, Opacity = 0.8,
                        Margin = new Avalonia.Thickness(0, 8, 0, 0)
                    });
                if (masterData.TryGetValue("Passages", out var listObj) && listObj is List<string> titles)
                {
                    foreach (var t in titles.Take(5))
                        content.Children.Add(new SelectableTextBlock
                        {
                            Text = $"\u2022 {t}",
                            FontSize = 11, Opacity = 0.7, TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            Margin = new Avalonia.Thickness(8, 2, 0, 0)
                        });
                    if (titles.Count > 5)
                        content.Children.Add(new SelectableTextBlock
                        {
                            Text = $"... and {titles.Count - 5} more",
                            FontSize = 10, Opacity = 0.5, Margin = new Avalonia.Thickness(8, 2, 0, 0)
                        });
                }
            }
        }
        else if (node.NodeType == ScholarNodeType.TermbaseEntry)
        {
            content.Children.Add(new SelectableTextBlock
            {
                Text = $"Term: {node.Label}",
                FontSize = 12, Opacity = 0.9,
                Margin = new Avalonia.Thickness(0, 8, 0, 0)
            });
            if (!string.IsNullOrEmpty(node.SecondaryLabel))
                content.Children.Add(new SelectableTextBlock
                {
                    Text = $"Preferred: {node.SecondaryLabel}",
                    FontSize = 11, Opacity = 0.8, TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(0, 4, 0, 0)
                });
            // Show alternate targets if SourceData is TermDisplayItem
            if (node.SourceData is TermDisplayItem termItem && termItem.AlternateTargets.Count > 0)
            {
                content.Children.Add(new SelectableTextBlock
                {
                    Text = "Alternates: " + string.Join(", ", termItem.AlternateTargets),
                    FontSize = 10, Opacity = 0.6, TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(0, 2, 0, 0)
                });
            }
        }
        else if (node.NodeType == ScholarNodeType.Book)
        {
            var bookPassage = node.SourceData as ScholarPassage;
            var relPath = bookPassage?.SourceRelPath ?? node.SecondaryLabel;
            AddSourceTextInfo(content, relPath);
            if (bookPassage != null)
            {
                if (bookPassage.AddedUtc != default)
                    content.Children.Add(new SelectableTextBlock { Text = $"Added: {bookPassage.AddedUtc.LocalDateTime:yyyy-MM-dd}", FontSize = 10, Opacity = 0.5 });
                if (bookPassage.Tags?.Count > 0)
                    content.Children.Add(new SelectableTextBlock { Text = $"Tags: {string.Join(", ", bookPassage.Tags)}", FontSize = 10, Opacity = 0.6, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 4, 0, 0) });
                if (bookPassage.MasterNames?.Count > 0)
                    content.Children.Add(new SelectableTextBlock { Text = $"Masters: {string.Join(", ", bookPassage.MasterNames)}", FontSize = 10, Opacity = 0.6, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
                if (!string.IsNullOrWhiteSpace(bookPassage.Notes))
                    content.Children.Add(new SelectableTextBlock { Text = bookPassage.Notes, FontSize = 10, Opacity = 0.6, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 4, 0, 0) });
            }
        }

        else if (node.NodeType == ScholarNodeType.Link && node.SourceData is LinkNode inspLink)
        {
            if (!string.IsNullOrEmpty(inspLink.Url))
            {
                var urlBlock = new SelectableTextBlock
                {
                    Text = inspLink.Url, FontSize = 12,
                    Foreground = Avalonia.Media.Brushes.CornflowerBlue,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(0, 4, 0, 0)
                };
                content.Children.Add(urlBlock);
            }
            if (!string.IsNullOrEmpty(inspLink.Description))
                content.Children.Add(new SelectableTextBlock { Text = inspLink.Description, FontSize = 11, Opacity = 0.7, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 4, 0, 0) });
            if (inspLink.CreatedUtc != default)
                content.Children.Add(new SelectableTextBlock { Text = $"Added: {inspLink.CreatedUtc.LocalDateTime:yyyy-MM-dd}", FontSize = 10, Opacity = 0.5 });
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
                        NavigationRequested?.Invoke(this, BuildPassageNavRequest(passage));
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

            if (node.NodeType == ScholarNodeType.Link && node.SourceData is LinkNode actionLink && !string.IsNullOrEmpty(actionLink.Url))
            {
                var openBtn = new Button { Content = "Open in Browser", Padding = new Thickness(8, 4), FontSize = 11 };
                openBtn.Click += (_, _) =>
                {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(actionLink.Url) { UseShellExecute = true }); }
                    catch { }
                };
                actions.Children.Add(openBtn);

                var copyBtn = new Button { Content = "Copy URL", Padding = new Thickness(8, 4), FontSize = 11 };
                copyBtn.Click += async (_, _) =>
                {
                    var top = TopLevel.GetTopLevel(this);
                    if (top?.Clipboard != null) await top.Clipboard.SetTextAsync(actionLink.Url);
                };
                actions.Children.Add(copyBtn);
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

    private async System.Threading.Tasks.Task ExportNodesCsvAsync()
    {
        if (_vm == null) return;
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage == null) return;

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Nodes CSV",
            DefaultExtension = "csv",
            FileTypeChoices = new[] { new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } } },
            SuggestedFileName = $"{_vm.GetCollection().Name ?? "graph"}-nodes.csv"
        });
        if (file == null) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("NodeId,Type,Label,X,Y,Degree");
        foreach (var n in _vm.Nodes)
            sb.AppendLine($"\"{EscapeCsv(n.NodeId)}\",\"{n.NodeType}\",\"{EscapeCsv(n.Label)}\",{n.X:F1},{n.Y:F1},{n.Degree}");

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new System.IO.StreamWriter(stream);
        await writer.WriteAsync(sb.ToString());
    }

    private async System.Threading.Tasks.Task ExportEdgesCsvAsync()
    {
        if (_vm == null) return;
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage == null) return;

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Edges CSV",
            DefaultExtension = "csv",
            FileTypeChoices = new[] { new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } } },
            SuggestedFileName = $"{_vm.GetCollection().Name ?? "graph"}-edges.csv"
        });
        if (file == null) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("From,To,RelationType,IsDirectional");
        foreach (var e in _vm.Edges)
            sb.AppendLine($"\"{EscapeCsv(e.From.NodeId)}\",\"{EscapeCsv(e.To.NodeId)}\",\"{EscapeCsv(e.RelationType)}\",{e.IsDirectional}");

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new System.IO.StreamWriter(stream);
        await writer.WriteAsync(sb.ToString());
    }

    private async System.Threading.Tasks.Task ExportGexfAsync()
    {
        if (_vm == null) return;
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage == null) return;

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export GEXF",
            DefaultExtension = "gexf",
            FileTypeChoices = new[] { new FilePickerFileType("GEXF") { Patterns = new[] { "*.gexf" } } },
            SuggestedFileName = $"{_vm.GetCollection().Name ?? "graph"}.gexf"
        });
        if (file == null) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<gexf xmlns=\"http://gexf.net/1.3\" version=\"1.3\">");
        sb.AppendLine("  <graph defaultedgetype=\"directed\">");
        sb.AppendLine("    <attributes class=\"node\"><attribute id=\"0\" title=\"type\" type=\"string\"/><attribute id=\"1\" title=\"color\" type=\"string\"/></attributes>");
        sb.AppendLine("    <attributes class=\"edge\"><attribute id=\"0\" title=\"weight\" type=\"float\"/><attribute id=\"1\" title=\"relation\" type=\"string\"/></attributes>");
        sb.AppendLine("    <nodes>");
        foreach (var n in _vm.Nodes)
        {
            var label = System.Security.SecurityElement.Escape(n.Label) ?? "";
            sb.AppendLine($"      <node id=\"{System.Security.SecurityElement.Escape(n.NodeId)}\" label=\"{label}\">");
            sb.AppendLine($"        <attvalues><attvalue for=\"0\" value=\"{n.NodeType}\"/><attvalue for=\"1\" value=\"{n.ColorHex}\"/></attvalues>");
            sb.AppendLine("      </node>");
        }
        sb.AppendLine("    </nodes>");
        sb.AppendLine("    <edges>");
        int edgeIdx = 0;
        foreach (var e in _vm.Edges)
        {
            var rel = System.Security.SecurityElement.Escape(e.RelationType) ?? "";
            sb.AppendLine($"      <edge id=\"{edgeIdx++}\" source=\"{System.Security.SecurityElement.Escape(e.From.NodeId)}\" target=\"{System.Security.SecurityElement.Escape(e.To.NodeId)}\">");
            sb.AppendLine($"        <attvalues><attvalue for=\"0\" value=\"{e.Weight:F2}\"/><attvalue for=\"1\" value=\"{rel}\"/></attvalues>");
            sb.AppendLine("      </edge>");
        }
        sb.AppendLine("    </edges>");
        sb.AppendLine("  </graph>");
        sb.AppendLine("</gexf>");

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new System.IO.StreamWriter(stream);
        await writer.WriteAsync(sb.ToString());
    }

    private async System.Threading.Tasks.Task CopySummaryAsync()
    {
        if (_vm == null) return;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Research Graph: {_vm.GetCollection().Name ?? _vm.GetCollection().Id}");
        sb.AppendLine($"Nodes: {_vm.NodeCount}  Edges: {_vm.EdgeCount}  Quality: {_vm.QualityScore:F0}%");
        sb.AppendLine();
        sb.AppendLine("--- Nodes ---");
        foreach (var n in _vm.Nodes)
            sb.AppendLine($"  [{n.NodeType}] {n.Label} (degree {n.Degree})");
        sb.AppendLine();
        sb.AppendLine("--- Edges ---");
        foreach (var e in _vm.Edges)
            sb.AppendLine($"  {e.From.Label} --[{e.RelationType}]--> {e.To.Label}");

        await clipboard.SetTextAsync(sb.ToString());
    }

    private async System.Threading.Tasks.Task CopyScreenshotAsync()
    {
        if (_canvas == null) return;
        int w = (int)_canvas.Bounds.Width;
        int h = (int)_canvas.Bounds.Height;
        if (w < 1 || h < 1) return;

        try
        {
            var rtb = new RenderTargetBitmap(new PixelSize(w, h));
            rtb.Render(_canvas);

            // Save to temp file and copy the file path (Avalonia clipboard bitmap support is limited)
            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"research-graph-{DateTime.Now:yyyyMMdd-HHmmss}.png");
            rtb.Save(tempPath);

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(tempPath);

            var status = this.FindControl<TextBlock>("TxtStatus");
            if (status != null) status.Text = $"Screenshot saved: {tempPath}";
        }
        catch (Exception ex)
        {
            var status = this.FindControl<TextBlock>("TxtStatus");
            if (status != null) status.Text = $"Screenshot failed: {ex.Message}";
        }
    }

    private static string EscapeCsv(string s) => s.Replace("\"", "\"\"");
}
