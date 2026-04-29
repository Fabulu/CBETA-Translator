using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ReadZen.App.Views;

public partial class ScholarTabView : UserControl
{
    private readonly ScholarTabViewModel _vm;
    private bool _suppressSelectionSync;

    // Hover dictionary
    private readonly ICedictDictionary _cedict = App.Services.GetRequiredService<ICedictDictionary>();
    private readonly IGrammarReferenceService _grammar = App.Services.GetRequiredService<IGrammarReferenceService>();
    private readonly ICitationService _citationService = new CitationService();
    private HoverDictionaryBehaviorTextBox? _hoverDict;
    private DispatcherTimer? _scholarSelDebounce;
    private bool _scholarSelWired;

    // Parallel passage finder
    private readonly IParallelPassageFinderService _parallelFinder = App.Services.GetRequiredService<IParallelPassageFinderService>();
    private CancellationTokenSource? _parallelCts;

    // Tree drag-and-drop
    private static readonly DataFormat<string> PassageDragFormat = DataFormat.CreateStringApplicationFormat("scholar/passage-id");
    private CollectionTreeNode? _dragCandidate;
    private Point? _dragStartPoint;

    // Assistant panel
    private readonly ITranslationAssistantService _assistantService = App.Services.GetRequiredService<ITranslationAssistantService>();
    private string? _originalDir;
    private string? _translatedDir;
    private string? _currentUsername;
    private string? _lastRenderedPassageId;
    private CancellationTokenSource? _assistantCts;

    private StackPanel? _scholarQaHost;
    private StackPanel? _scholarTermHost;
    private StackPanel? _scholarApprovedTmHost;
    private StackPanel? _scholarReferenceTmHost;

    private Button? _btnAddLink;

    // Autosave debounce
    private CancellationTokenSource? _autosaveCts;

    public event EventHandler<string>? Status;
    public Func<string?, string>? SourceTitleResolver { get; set; }
    public event EventHandler<NavigationRequest>? NavigationRequested;
    public event EventHandler? DictionaryRequested;
    public event EventHandler<int>? DictionarySourceChanged;
    public event EventHandler? ZenMastersRequested;
    public event EventHandler<string>? OpenMasterRequested;
    public event EventHandler<string>? OpenDictionaryTermRequested;

    public ScholarTabView()
    {
        InitializeComponent();

        _vm = new ScholarTabViewModel(
            App.Services.GetRequiredService<IScholarCollectionsService>(),
            App.Services.GetRequiredService<IAppConfigService>());
        DataContext = _vm;

        _vm.StatusChanged += (_, msg) => Status?.Invoke(this, msg);
        _vm.NavigationRequested += (_, req) => NavigationRequested?.Invoke(this, req);

        _vm.PickExportFileAsync = PickExportFileAsync;
        _vm.PickImportFileAsync = PickImportFileAsync;
        _vm.ConfirmAsync = ShowYesNoAsync;
        _vm.PickExportFormatAsync = PickExportFormatAsync;

        _scholarQaHost = this.FindControl<StackPanel>("PnlQualityChecks");
        _scholarTermHost = this.FindControl<StackPanel>("PnlGlossary");
        _scholarApprovedTmHost = this.FindControl<StackPanel>("PnlApprovedTm");
        _scholarReferenceTmHost = this.FindControl<StackPanel>("PnlRefTm");

        // Re-render assistant when any sub-expander opens (AvaloniaEdit needs visual tree for text layout)
        foreach (var expName in new[] { "ExpanderQA", "ExpanderGlossary", "ExpanderApprovedTm", "ExpanderRefTm" })
        {
            var exp = this.FindControl<Expander>(expName);
            if (exp != null)
            {
                exp.PropertyChanged += (_, e) =>
                {
                    if (e.Property.Name == "IsExpanded" && exp.IsExpanded)
                    {
                        _lastRenderedPassageId = null;
                        _ = RefreshAssistantAsync();
                    }
                };
            }
        }

        _btnAddLink = this.FindControl<Button>("BtnAddLink");
        if (_btnAddLink != null)
            _btnAddLink.Click += async (_, _) => await ShowLinkDialogAsync();

        // Tree panel toggle
        var btnToggle = this.FindControl<Button>("BtnToggleTree");
        if (btnToggle != null)
        {
            btnToggle.Click += (_, _) =>
            {
                var panel = this.FindControl<Border>("TreePanel");
                if (panel != null)
                    panel.Width = panel.Width > 32 ? 32 : 240;
            };
        }

        // Tree selection -> passage loading
        var tree = this.FindControl<TreeView>("CollectionsTree");
        if (tree != null)
        {
            tree.SelectionChanged += (_, e) =>
            {
                if (tree.SelectedItem is CollectionTreeNode node)
                {
                    if (node.Kind == TreeNodeKind.Passage && node.Tag is ScholarPassage passage)
                        SelectPassage(passage);
                    else if (node.Kind == TreeNodeKind.Collection && node.Tag is ScholarCollection col)
                        _vm.SelectedCollection = col;
                }
                else if (tree.SelectedItem is ScholarPassage passage)
                    SelectPassage(passage);
                else if (tree.SelectedItem is ScholarCollection col)
                    _vm.SelectedCollection = col;
            };

            // Context menus on tree items
            tree.ContextRequested += (_, e) =>
            {
                if (tree.SelectedItem is not CollectionTreeNode node) return;

                var menu = new ContextMenu();

                if (node.Kind == TreeNodeKind.Passage && node.Tag is ScholarPassage passage)
                {
                    var openItem = new MenuItem { Header = "Open in Reader" };
                    openItem.Click += (_, _) =>
                    {
                        NavigationRequested?.Invoke(this, new NavigationRequest
                        {
                            RelPath = passage.SourceRelPath ?? "",
                            Side = SearchSide.Original
                        });
                    };
                    menu.Items.Add(openItem);

                    // Copy Web Link (readzen.pages.dev)
                    var copyWebLink = new MenuItem { Header = "Copy Web Link" };
                    copyWebLink.Click += async (_, _) =>
                    {
                        var url = ZenUriParser.BuildShareableUrl(
                            passage.SourceRelPath ?? "",
                            fromLb: passage.StartBlockNumber?.ToString(),
                            toLb: passage.EndBlockNumber?.ToString());
                        var top = TopLevel.GetTopLevel(this);
                        if (top?.Clipboard != null)
                            await top.Clipboard.SetTextAsync(url);
                        Status?.Invoke(this, "Web link copied.");
                    };
                    menu.Items.Add(copyWebLink);

                    // Copy Deep Link (zen://)
                    var copyLinkItem = new MenuItem { Header = "Copy Deep Link" };
                    copyLinkItem.Click += async (_, _) =>
                    {
                        var link = $"zen://scholar/{passage.Id}";
                        var top = TopLevel.GetTopLevel(this);
                        if (top?.Clipboard != null)
                            await top.Clipboard.SetTextAsync(link);
                        Status?.Invoke(this, "Deep link copied.");
                    };
                    menu.Items.Add(copyLinkItem);

                    // Citation flyout
                    menu.Items.Add(new Separator());
                    var citeFlyout = CitationMenuHelper.BuildCiteAsFlyout(
                        _citationService,
                        CitationMenuHelper.GetPreferredStyle(),
                        buildMetadata: () =>
                        {
                            string? lbValue = passage.StartBlockNumber?.ToString();
                            TextLicenseInfo? licInfo = null;
                            if (!string.IsNullOrEmpty(_originalDir) && !string.IsNullOrEmpty(passage.SourceRelPath))
                            {
                                try
                                {
                                    var lSvc = App.Services.GetRequiredService<ILicenseMetadataService>();
                                    lSvc.TryGet(Path.Combine(_originalDir, passage.SourceRelPath), out licInfo);
                                }
                                catch { }
                            }
                            return _citationService.BuildMetadata(
                                licInfo, fromLb: lbValue,
                                quotedText: passage.ZhText?.Length > 80
                                    ? passage.ZhText[..80] + "..."
                                    : passage.ZhText,
                                translatorName: passage.TranslationUser);
                        },
                        copyToClipboard: async text =>
                        {
                            var top = TopLevel.GetTopLevel(this);
                            if (top?.Clipboard != null)
                                await top.Clipboard.SetTextAsync(text);
                        },
                        onCopied: msg => Status?.Invoke(this, msg));
                    menu.Items.Add(citeFlyout);

                    menu.Items.Add(new Separator());

                    var deleteItem = new MenuItem { Header = "Delete Passage" };
                    deleteItem.Click += (_, _) => _vm.DeletePassageCommand.Execute(null);
                    menu.Items.Add(deleteItem);
                }
                else if (node.Kind == TreeNodeKind.Collection && node.Tag is ScholarCollection col)
                {
                    var renameItem = new MenuItem { Header = "Rename Collection" };
                    renameItem.Click += async (_, _) =>
                    {
                        _vm.SelectedCollection = col;
                        await RenameSelectedCollectionAsync();
                    };
                    menu.Items.Add(renameItem);

                    var exportItem = new MenuItem { Header = "Export Collection" };
                    exportItem.Click += (_, _) => _vm.ExportCollectionsCommand.Execute(null);
                    menu.Items.Add(exportItem);

                    menu.Items.Add(new Separator());

                    var deleteItem = new MenuItem { Header = "Delete Collection" };
                    deleteItem.Click += (_, _) => _vm.DeleteCollectionCommand.Execute(null);
                    menu.Items.Add(deleteItem);
                }

                if (menu.Items.Count > 0)
                {
                    tree.ContextMenu = menu;
                    menu.Open(tree);
                }
            };

            // Drag-and-drop reorder for passages
            DragDrop.SetAllowDrop(tree, true);

            tree.PointerPressed += (_, e) =>
            {
                if (e.GetCurrentPoint(tree).Properties.IsLeftButtonPressed &&
                    tree.SelectedItem is CollectionTreeNode node &&
                    node.Kind == TreeNodeKind.Passage)
                {
                    _dragCandidate = node;
                    _dragStartPoint = e.GetPosition(tree);
                }
            };

            tree.PointerMoved += async (_, e) =>
            {
                if (_dragStartPoint == null || _dragCandidate == null) return;
                var pos = e.GetPosition(tree);
                var delta = pos - _dragStartPoint.Value;
                if (Math.Abs(delta.X) < 5 && Math.Abs(delta.Y) < 5) return;

                _dragStartPoint = null;
                var passage = _dragCandidate.Tag as ScholarPassage;
                if (passage == null) { _dragCandidate = null; return; }

                var data = new DataTransfer();
                data.Add(DataTransferItem.Create(PassageDragFormat, passage.Id));
                await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);
                _dragCandidate = null;
            };

            tree.AddHandler(DragDrop.DropEvent, async (_, args) =>
            {
                if (!args.DataTransfer.Contains(PassageDragFormat)) return;
                var sourceId = args.DataTransfer.TryGetValue(PassageDragFormat);
                if (string.IsNullOrEmpty(sourceId)) return;

                var col = _vm.SelectedCollection;
                if (col == null) return;

                var sourcePassage = col.Passages.FirstOrDefault(p => p.Id == sourceId);
                if (sourcePassage == null) return;

                int targetIndex = col.Passages.Count;
                if (args.Source is Control ctrl)
                {
                    var tvi = ctrl as TreeViewItem ?? ctrl.FindAncestorOfType<TreeViewItem>();
                    if (tvi?.DataContext is CollectionTreeNode targetNode &&
                        targetNode.Kind == TreeNodeKind.Passage &&
                        targetNode.Tag is ScholarPassage targetPassage)
                    {
                        targetIndex = col.Passages.IndexOf(targetPassage);
                    }
                }

                await _vm.MovePassageToIndexAsync(sourcePassage, targetIndex);
                Status?.Invoke(this, "Passage reordered.");
            });

            tree.PointerReleased += (_, _) =>
            {
                _dragCandidate = null;
                _dragStartPoint = null;
            };
        }

        // Bottom drawer toggle
        // "Research Graph" button opens the graph window directly
        var btnShowGraph = this.FindControl<Button>("BtnShowGraph");
        if (btnShowGraph != null)
        {
            btnShowGraph.Click += async (_, _) =>
            {
                if (_vm.SelectedCollection == null) return;

                List<TermDisplayItem>? termData = null;
                try
                {
                    var termService = App.Services.GetRequiredService<ITermbaseService>();
                    var root = _vm.GetRoot();
                    if (!string.IsNullOrEmpty(root))
                    {
                        var hits = await termService.GetAllTermsAsync(root);
                        termData = hits.Select(h => new TermDisplayItem
                        {
                            SourceTerm = h.SourceTerm,
                            PreferredTarget = h.PreferredTarget,
                            AlternateTargets = h.AlternateTargets ?? new()
                        }).ToList();
                    }
                }
                catch { }

                var graphWindow = new ResearchGraphWindow(
                    _vm.SelectedCollection, _vm.Collections.ToList(), termData);
                graphWindow.NavigationRequested += (_, req) => NavigationRequested?.Invoke(this, req);
                graphWindow.OpenMasterRequested += (_, name) => OpenMasterRequested?.Invoke(this, name);
                graphWindow.DictionaryRequested += (_, term) => OpenDictionaryTermRequested?.Invoke(this, term);
                graphWindow.AdoptPassageRequested += async (_, passage) =>
                {
                    var target = _vm.SelectedCollection ?? _vm.Collections.FirstOrDefault();
                    if (target != null)
                    {
                        await _vm.AdoptPassageToCollectionAsync(passage, target);
                        Status?.Invoke(this, $"Passage adopted to '{target.Name}'.");
                    }
                };
                graphWindow.Show();
            };
        }
        // Summary field: sync to VM on every keystroke, autosave on lost focus
        var txtSummary = this.FindControl<TextBox>("TxtSummary");
        if (txtSummary != null)
        {
            txtSummary.TextChanged += (_, _) =>
            {
                if (_vm.SelectedPassage != null)
                    _vm.PassageSummary = txtSummary.Text ?? "";
            };
            txtSummary.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter && _vm.SelectedPassage != null)
                {
                    _vm.SyncAndSave();
                    _vm.RebuildTree();
                    e.Handled = true;
                }
            };
            txtSummary.LostFocus += (_, _) =>
            {
                if (_vm.SelectedPassage != null)
                {
                    _vm.SyncAndSave();
                    _vm.RebuildTree();
                }
            };
        }

        // Research Notes field autosave on lost focus
        // Research Notes: sync on every keystroke, save on blur
        var txtPassageNotes = this.FindControl<TextBox>("TxtPassageNotes");
        if (txtPassageNotes != null)
        {
            txtPassageNotes.TextChanged += (_, _) =>
            {
                if (_vm.SelectedPassage != null)
                    _vm.PassageNotes = txtPassageNotes.Text ?? "";
            };
            txtPassageNotes.LostFocus += (_, _) =>
            {
                if (_vm.SelectedPassage != null)
                    _vm.SyncAndSave();
            };
        }

        // Categorization dropdowns: save on selection change
        foreach (var cmbName in new[] { "CmbDoctrinalTopic", "CmbLiteraryForm", "CmbLineage", "CmbRhetoricalFunction" })
        {
            var cmb = this.FindControl<ComboBox>(cmbName);
            if (cmb != null)
                cmb.SelectionChanged += (_, _) => { if (_vm.SelectedPassage != null) _vm.SyncAndSave(); };
        }

        // Export button
        var btnExport = this.FindControl<Button>("BtnExport");
        if (btnExport != null)
            btnExport.Click += (_, _) => _vm.ExportCollectionsCommand.Execute(null);

        // Open in Reader button (guarded — only works when a passage is selected)
        var btnOpenInReader = this.FindControl<Button>("BtnOpenInReader");
        if (btnOpenInReader != null)
            btnOpenInReader.Click += (_, _) => { if (_vm.SelectedPassage != null) _vm.NavigateToPassageCommand.Execute(null); };

        // Overflow menu
        var btnOverflow = this.FindControl<Button>("BtnOverflow");
        if (btnOverflow != null)
        {
            btnOverflow.Click += (_, _) =>
            {
                var menu = new ContextMenu();
                menu.Items.Add(CreateScholarMenuItem("Rename Collection", async () => await RenameSelectedCollectionAsync()));
                menu.Items.Add(CreateScholarMenuItem("Import Collections", () => _vm.ImportCollectionsCommand.Execute(null)));
                menu.Items.Add(CreateScholarMenuItem("Rebuild Tree", () => _vm.RebuildTree()));
                menu.Items.Add(new Separator());
                menu.Items.Add(CreateScholarMenuItem("Delete Collection", () => _vm.DeleteCollectionCommand.Execute(null)));
                menu.Open(btnOverflow);
            };
        }

        // Delete passage button
        var btnDeletePassage = this.FindControl<Button>("BtnDeletePassage");
        if (btnDeletePassage != null)
            btnDeletePassage.Click += (_, _) => _vm.DeletePassageCommand.Execute(null);

        var btnMoveUp = this.FindControl<Button>("BtnMoveUp");
        if (btnMoveUp != null)
            btnMoveUp.Click += async (_, _) => await _vm.MovePassageUpCommand.ExecuteAsync(null);

        var btnMoveDown = this.FindControl<Button>("BtnMoveDown");
        if (btnMoveDown != null)
            btnMoveDown.Click += async (_, _) => await _vm.MovePassageDownCommand.ExecuteAsync(null);

        // Wire collection creation buttons
        var btnAddCollection = this.FindControl<Button>("BtnAddCollection");
        if (btnAddCollection != null)
            btnAddCollection.Click += async (_, _) => await _vm.AddCollectionCommand.ExecuteAsync(null);

        var btnCreateFirst = this.FindControl<Button>("BtnCreateFirst");
        if (btnCreateFirst != null)
            btnCreateFirst.Click += async (_, _) => await _vm.AddCollectionCommand.ExecuteAsync(null);

        // Copy/Cite buttons for Chinese and English text
        WireCopyCiteButton("BtnCopyZh", "TxtZhText", () => _vm.SelectedPassage?.ZhText, "Chinese text copied.");
        WireCopyCiteButton("BtnCopyEn", "TxtEnText", () => _vm.SelectedPassage?.EnText, "English text copied.");
        WireCiteButton("BtnCiteZh", () => _vm.SelectedPassage?.ZhText);
        WireCiteButton("BtnCiteEn", () => _vm.SelectedPassage?.EnText);

        WireViewEvents();
        SetupHoverDictionary();

        DetachedFromVisualTree += (_, _) =>
        {
            DisposeHoverDictionary();
            try { _assistantCts?.Cancel(); } catch (ObjectDisposedException) { }
            try { _assistantCts?.Dispose(); } catch (ObjectDisposedException) { }
            try { _parallelCts?.Cancel(); } catch (ObjectDisposedException) { }
            try { _parallelCts?.Dispose(); } catch (ObjectDisposedException) { }
            try { _autosaveCts?.Cancel(); } catch (ObjectDisposedException) { }
            try { _autosaveCts?.Dispose(); } catch (ObjectDisposedException) { }
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void WireViewEvents()
    {
        // Keyboard shortcuts
        KeyDown += (_, e) =>
        {
            if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
            {
                if (e.Key == Key.P)
                {
                    _ = OnFindParallelsClickedAsync(null);
                    e.Handled = true;
                }
            }
        };

        // Insert reference: show a passage picker flyout
        var btnInsertRef = this.FindControl<Button>("BtnInsertReference");
        if (btnInsertRef != null)
        {
            btnInsertRef.Click += (_, _) =>
            {
                if (_vm.SelectedCollection == null || _vm.SelectedPassage == null) return;

                var otherPassages = _vm.SelectedCollection.Passages
                    .Where(p => p.Id != _vm.SelectedPassage.Id)
                    .ToList();
                if (otherPassages.Count == 0) return;

                var insertFlyout = new Flyout();
                var listBox = new ListBox
                {
                    MaxHeight = 300,
                    MinWidth = 250,
                    ItemsSource = otherPassages
                };
                listBox.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<ScholarPassage>((p, _) =>
                {
                    return new TextBlock { Text = p.DisplayTitle, FontSize = 12 };
                });
                listBox.SelectionChanged += (_, _) =>
                {
                    if (listBox.SelectedItem is ScholarPassage selected)
                    {
                        InsertPassageReference(selected);
                        insertFlyout.Hide();
                    }
                };
                insertFlyout.Content = listBox;
                insertFlyout.ShowAt(btnInsertRef);
            };
        }

        // Update detail text fields when selected passage changes
        _vm.PropertyChanged += (_, e) =>
        {
            if (_suppressSelectionSync) return;

            if (e.PropertyName == nameof(ScholarTabViewModel.SelectedPassage))
            {
                UpdateDetailFields();
                _ = RefreshAssistantAsync();
                if (_vm.SelectedPassage != null)
                {
                    _suppressSelectionSync = true;
                    _vm.SelectedCommunityPassage = null;
                    _suppressSelectionSync = false;
                }
            }
            else if (e.PropertyName == nameof(ScholarTabViewModel.SelectedCollection)
                  || e.PropertyName == nameof(ScholarTabViewModel.IsEmptyState))
            {
                UpdateDetailVisibility();
            }
            else if (e.PropertyName == nameof(ScholarTabViewModel.SelectedCommunityPassage))
            {
                if (_vm.SelectedCommunityPassage != null)
                {
                    _suppressSelectionSync = true;
                    _vm.SelectedPassage = null;
                    _suppressSelectionSync = false;
                }
                UpdateCommunityDetailFields();
                _ = RefreshAssistantAsync();
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

        // Tag bubble: Enter to add
        var txtAddTag = this.FindControl<TextBox>("TxtAddTag");
        if (txtAddTag != null)
        {
            txtAddTag.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(txtAddTag.Text))
                {
                    _vm.AddTag(txtAddTag.Text);
                    txtAddTag.Text = "";
                    e.Handled = true;
                }
            };
        }

        // Refresh tag/master chip UI whenever the observable collections change
        _vm.TagBubbles.CollectionChanged += (_, _) => PopulatePassageMetadata();
        _vm.MasterBubbles.CollectionChanged += (_, _) => PopulatePassageMetadata();

        // Master autocomplete: populate items, add on selection or Enter
        var acbMaster = this.FindControl<AutoCompleteBox>("AcbAddMaster");
        if (acbMaster != null)
        {
            acbMaster.ItemsSource = _vm.AllMasterDisplayNames;
            acbMaster.SelectionChanged += (_, _) =>
            {
                if (acbMaster.SelectedItem is string name && !string.IsNullOrWhiteSpace(name))
                {
                    _vm.AddMaster(name);
                    acbMaster.Text = "";
                    acbMaster.SelectedItem = null;
                }
            };
            acbMaster.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(acbMaster.Text))
                {
                    _vm.AddMaster(acbMaster.Text);
                    acbMaster.Text = "";
                    e.Handled = true;
                }
            };
        }

    }

    private void SelectPassage(ScholarPassage passage)
    {
        _vm.SelectedPassage = passage;
        UpdateDetailVisibility();
        PopulatePassageMetadata();
        RefreshBacklinks();
    }

    /// <summary>Populates tag and master chip panels from the currently selected passage.</summary>
    private void PopulatePassageMetadata()
    {
        var passage = _vm?.SelectedPassage;
        if (passage == null) return;

        // Populate tag chips in PnlTags (with × remove buttons)
        var tagsPanel = this.FindControl<WrapPanel>("PnlTags");
        if (tagsPanel != null)
        {
            tagsPanel.Children.Clear();
            foreach (var tag in passage.Tags ?? new())
            {
                var capturedTag = tag;
                var removeBtn = new Button
                {
                    Content = "\u00d7", Padding = new Thickness(2, 0),
                    FontSize = 9, MinWidth = 14, MinHeight = 14,
                    Background = Brushes.Transparent, Foreground = Brushes.LightGray,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                removeBtn.Click += (_, _) => _vm?.RemoveTag(capturedTag);
                var chip = new Border
                {
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(6, 2),
                    Margin = new Thickness(0, 0, 4, 4),
                    Background = Brushes.DarkSlateGray,
                    Child = new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        Spacing = 4,
                        Children = { new TextBlock { Text = tag, FontSize = 10, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center }, removeBtn }
                    }
                };
                tagsPanel.Children.Add(chip);
            }
        }

        // Populate master chips in PnlMasters (with × remove buttons)
        var mastersPanel = this.FindControl<WrapPanel>("PnlMasters");
        if (mastersPanel != null)
        {
            mastersPanel.Children.Clear();
            foreach (var master in passage.MasterNames ?? new())
            {
                var capturedMaster = master;
                var removeBtn = new Button
                {
                    Content = "\u00d7", Padding = new Thickness(2, 0),
                    FontSize = 9, MinWidth = 14, MinHeight = 14,
                    Background = Brushes.Transparent, Foreground = Brushes.LightGray,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                removeBtn.Click += (_, _) => _vm?.RemoveMaster(capturedMaster);
                var chip = new Border
                {
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(6, 2),
                    Margin = new Thickness(0, 0, 4, 4),
                    Background = Brushes.DarkGoldenrod,
                    Child = new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        Spacing = 4,
                        Children = { new TextBlock { Text = master, FontSize = 10, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center }, removeBtn }
                    }
                };
                mastersPanel.Children.Add(chip);
            }
        }
    }

    private void UpdateDetailVisibility()
    {
        var empty = this.FindControl<Border>("EmptyState");
        var detail = this.FindControl<StackPanel>("PassageDetail");
        var noPassage = this.FindControl<Border>("NoPassageState");
        bool isEmptyState = _vm.IsEmptyState;
        bool hasPassage = _vm.SelectedPassage != null;
        // Show empty state only when there are no collections at all (true empty),
        // not just when no passage is selected.
        if (empty != null) empty.IsVisible = isEmptyState;
        if (detail != null) detail.IsVisible = hasPassage;
        if (noPassage != null) noPassage.IsVisible = !isEmptyState && !hasPassage;
    }

    private void ScheduleAutosave()
    {
        _autosaveCts?.Cancel();
        _autosaveCts = new CancellationTokenSource();
        var token = _autosaveCts.Token;
        _ = Task.Delay(2000, token).ContinueWith(async _ =>
        {
            if (!token.IsCancellationRequested)
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () => await _vm.SaveCurrentStateAsync());
        }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
    }

    private void RefreshBacklinks()
    {
        var panel = this.FindControl<ItemsControl>("PnlBacklinks");
        var noBacklinks = this.FindControl<TextBlock>("TxtNoBacklinks");
        if (panel == null || _vm.SelectedPassage == null || _vm.SelectedCollection == null) return;

        var chips = new List<Control>();
        var links = _vm.SelectedCollection.Links
            .Where(l => l.ToPassageId == _vm.SelectedPassage.Id)
            .ToList();

        foreach (var link in links)
        {
            var source = _vm.SelectedCollection.Passages.FirstOrDefault(p => p.Id == link.FromPassageId);
            if (source == null) continue;

            var chip = new Border
            {
                Padding = new Thickness(8, 4),
                Margin = new Thickness(0, 0, 4, 4),
                CornerRadius = new CornerRadius(4),
                Background = Avalonia.Media.Brushes.DimGray,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Child = new TextBlock
                {
                    Text = $"{link.RelationType}: {source.DisplayTitle}",
                    FontSize = 12,
                    Foreground = Avalonia.Media.Brushes.White
                }
            };
            var capturedSource = source;
            chip.PointerPressed += (_, _) =>
            {
                _vm.SelectPassageById(capturedSource.Id);
            };
            chips.Add(chip);
        }

        panel.ItemsSource = chips;
        if (noBacklinks != null) noBacklinks.IsVisible = chips.Count == 0;
    }

    private void UpdateDetailFields()
    {
        var passage = _vm.SelectedPassage;
        var txtSourcePath = this.FindControl<TextBlock>("TxtSourcePath");
        var txtZhText = this.FindControl<TextBox>("TxtZhText");
        var txtEnText = this.FindControl<TextBox>("TxtEnText");
        var txtSummary = this.FindControl<TextBox>("TxtSummary");
        var txtPassageNotes = this.FindControl<TextBox>("TxtPassageNotes");

        if (txtSourcePath != null) txtSourcePath.Text = ResolveSourceDisplay(passage);
        if (txtZhText != null) txtZhText.Text = passage?.ZhText ?? "";
        if (txtEnText != null) txtEnText.Text = passage?.EnText ?? "";
        if (txtSummary != null) txtSummary.Text = _vm.PassageSummary ?? "";
        if (txtPassageNotes != null) txtPassageNotes.Text = _vm.PassageNotes ?? "";

        SetupHoverDictionary();
        RefreshLinksPanel();
        RefreshLinkedTextsPanel();
        UpdateDetailVisibility();
        PopulatePassageMetadata();
        RefreshBacklinks();
    }

    private void UpdateCommunityDetailFields()
    {
        var passage = _vm.SelectedCommunityPassage;

        var txtSourcePath = this.FindControl<TextBlock>("TxtSourcePath");
        var txtZhText = this.FindControl<TextBox>("TxtZhText");
        var txtEnText = this.FindControl<TextBox>("TxtEnText");

        if (txtSourcePath != null) txtSourcePath.Text = ResolveSourceDisplay(passage);
        if (txtZhText != null) txtZhText.Text = passage?.ZhText ?? "";
        if (txtEnText != null) txtEnText.Text = passage?.EnText ?? "";

        // Disable editor fields for community passages (read-only)
        _vm.IsEditorEnabled = false;

        // Update editor fields to show community passage metadata (read-only context)
        _vm.PassageNotes = passage?.Notes ?? "";
        _vm.PassageTags = passage != null ? string.Join(", ", passage.Tags) : "";
        _vm.PassageMasterNames = passage != null ? string.Join(", ", passage.MasterNames) : "";
        _vm.DoctrinalTopic = passage?.DoctrinalTopic ?? "";
        _vm.LiteraryForm = passage?.LiteraryForm ?? "";
        _vm.Lineage = passage?.Lineage ?? "";
        _vm.RhetoricalFunction = passage?.RhetoricalFunction ?? "";

        SetupHoverDictionary();
        RefreshLinksPanel();
        RefreshLinkedTextsPanel();
    }

    // ----- Hover dictionary -----

    private void SetupHoverDictionary()
    {
        DisposeHoverDictionary();

        var txtZhText = this.FindControl<TextBox>("TxtZhText");
        var dictCanvas = this.FindControl<Canvas>("DictOverlayCanvas");
        if (txtZhText == null || dictCanvas == null) return;

        try { _hoverDict = new HoverDictionaryBehaviorTextBox(txtZhText, _cedict, _grammar, dictCanvas); }
        catch { /* dictionary not available */ }

        // Selection-based TM: wire ONCE (not per passage change). The handler
        // reads txtZhText.SelectedText at fire time, so it naturally reflects
        // whichever passage is currently displayed. Previous versions leaked
        // handlers + timers on every SetupHoverDictionary call.
        if (!_scholarSelWired)
        {
            _scholarSelWired = true;
            _scholarSelDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _scholarSelDebounce.Tick += (_, _) =>
            {
                _scholarSelDebounce!.Stop();
                var box = this.FindControl<TextBox>("TxtZhText");
                var sel = box?.SelectedText ?? "";
                int cjkCount = 0;
                foreach (var c in sel)
                {
                    if (c >= '\u4E00' && c <= '\u9FFF' || c >= '\u3400' && c <= '\u4DBF' || c >= '\uF900' && c <= '\uFAFF')
                        cjkCount++;
                }
                if (cjkCount >= 2)
                {
                    _lastRenderedPassageId = null;
                    _ = RefreshAssistantAsync(zhOverride: sel);
                }
                else
                {
                    _lastRenderedPassageId = null;
                    _ = RefreshAssistantAsync();
                }
            };

            txtZhText.AddHandler(InputElement.PointerReleasedEvent, (_, _) =>
            {
                _scholarSelDebounce!.Stop();
                _scholarSelDebounce.Start();
            });
        }
    }

    /// <summary>Opens the Research Graph window for the currently selected collection. Called by deep links.</summary>
    public async void OpenGraphForCurrentCollection()
    {
        if (_vm.SelectedCollection == null) return;

        List<TermDisplayItem>? termData = null;
        try
        {
            var termService = App.Services.GetRequiredService<ITermbaseService>();
            var root = _vm.GetRoot();
            if (!string.IsNullOrEmpty(root))
            {
                var hits = await termService.GetAllTermsAsync(root);
                termData = hits.Select(h => new TermDisplayItem
                {
                    SourceTerm = h.SourceTerm,
                    PreferredTarget = h.PreferredTarget,
                    AlternateTargets = h.AlternateTargets ?? new()
                }).ToList();
            }
        }
        catch { }

        var graphWindow = new ResearchGraphWindow(
            _vm.SelectedCollection, _vm.Collections.ToList(), termData);
        graphWindow.NavigationRequested += (_, req) => NavigationRequested?.Invoke(this, req);
        graphWindow.OpenMasterRequested += (_, name) => OpenMasterRequested?.Invoke(this, name);
        graphWindow.DictionaryRequested += (_, term) => OpenDictionaryTermRequested?.Invoke(this, term);
        graphWindow.AdoptPassageRequested += async (_, passage) =>
        {
            var target = _vm.SelectedCollection ?? _vm.Collections.FirstOrDefault();
            if (target != null)
            {
                await _vm.AdoptPassageToCollectionAsync(passage, target);
                Status?.Invoke(this, $"Passage adopted to '{target.Name}'.");
            }
        };
        graphWindow.Show();
    }

    private async Task RenameSelectedCollectionAsync()
    {
        var col = _vm.SelectedCollection;
        if (col == null) return;
        var dlg = new Window
        {
            Title = "Rename Collection", Width = 350, Height = 130,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, CanResize = false
        };
        var grid = new Grid { RowDefinitions = RowDefinitions.Parse("*,Auto"), Margin = new Thickness(12) };
        var txt = new TextBox { Text = col.Name ?? "" };
        var btnPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 8 };
        var btnOk = new Button { Content = "Rename", Padding = new Thickness(12, 6) };
        var btnCancel = new Button { Content = "Cancel", Padding = new Thickness(12, 6) };
        btnPanel.Children.Add(btnCancel); btnPanel.Children.Add(btnOk);
        Grid.SetRow(txt, 0); Grid.SetRow(btnPanel, 1);
        grid.Children.Add(txt); grid.Children.Add(btnPanel);
        dlg.Content = grid;
        string? newName = null;
        btnOk.Click += (_, _) => { newName = txt.Text?.Trim(); dlg.Close(); };
        btnCancel.Click += (_, _) => dlg.Close();
        txt.KeyDown += (_, ke) => { if (ke.Key == Key.Enter) { newName = txt.Text?.Trim(); dlg.Close(); } };
        var top = TopLevel.GetTopLevel(this) as Window;
        if (top != null) await dlg.ShowDialog(top);
        if (!string.IsNullOrEmpty(newName) && newName != col.Name)
        {
            col.Name = newName;
            _vm.SyncAndSave();
            _vm.RebuildTree();
            Status?.Invoke(this, $"Collection renamed to '{newName}'.");
        }
    }

    private static MenuItem CreateScholarMenuItem(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    private void WireCopyCiteButton(string btnName, string textBoxName, Func<string?> getFullText, string statusMsg)
    {
        var btn = this.FindControl<Button>(btnName);
        if (btn != null)
        {
            btn.Click += async (_, _) =>
            {
                // Prefer selected text, fall back to full text
                var box = this.FindControl<TextBox>(textBoxName);
                var text = !string.IsNullOrEmpty(box?.SelectedText) ? box.SelectedText : getFullText();
                if (string.IsNullOrEmpty(text)) return;
                var top = TopLevel.GetTopLevel(this);
                if (top?.Clipboard != null)
                    await top.Clipboard.SetTextAsync(text);
                var what = !string.IsNullOrEmpty(box?.SelectedText) ? "Selection copied." : statusMsg;
                Status?.Invoke(this, what);
            };
        }
    }

    private void WireCiteButton(string btnName, Func<string?> getQuotedText)
    {
        var btn = this.FindControl<Button>(btnName);
        if (btn != null)
        {
            btn.Click += async (_, _) =>
            {
                var passage = _vm.SelectedPassage;
                if (passage == null) return;
                // Check for selection in the matching text box
                var zhBox = this.FindControl<TextBox>("TxtZhText");
                var enBox = this.FindControl<TextBox>("TxtEnText");
                var selectedText = !string.IsNullOrEmpty(zhBox?.SelectedText) ? zhBox.SelectedText
                    : !string.IsNullOrEmpty(enBox?.SelectedText) ? enBox.SelectedText : null;
                var quoted = selectedText ?? getQuotedText();
                if (string.IsNullOrEmpty(quoted)) quoted = passage.ZhText;
                TextLicenseInfo? licenseInfo = null;
                if (!string.IsNullOrEmpty(_originalDir) && !string.IsNullOrEmpty(passage.SourceRelPath))
                {
                    var absPath = Path.Combine(_originalDir, passage.SourceRelPath);
                    try
                    {
                        var licenseSvc = App.Services.GetRequiredService<ILicenseMetadataService>();
                        licenseSvc.TryGet(absPath, out licenseInfo);
                    }
                    catch { }
                }
                var metadata = _citationService.BuildMetadata(
                    licenseInfo,
                    fromLb: passage.StartBlockNumber?.ToString(),
                    quotedText: quoted?.Length > 200 ? quoted[..200] + "..." : quoted,
                    translatorName: passage.TranslationUser);
                var style = CitationMenuHelper.GetPreferredStyle();
                var citation = _citationService.Generate(metadata, style);
                var top = TopLevel.GetTopLevel(this);
                if (top?.Clipboard != null)
                    await top.Clipboard.SetTextAsync(citation);
                Status?.Invoke(this, $"{style} citation copied.");
            };
        }
    }

    private void DisposeHoverDictionary()
    {
        try { _hoverDict?.Dispose(); } catch { }
        _hoverDict = null;
    }

    // ----- Termbase highlighting -----

    private static IBrush TermbaseGoldBg =>
        Application.Current?.FindResource("TermbaseHighlightBg") as IBrush
        ?? new SolidColorBrush(Color.FromArgb(90, 255, 185, 0));

    private void UpdateTermbaseHits(IReadOnlyList<TermHit>? hits)
    {
        var panel = this.FindControl<ItemsControl>("PnlTermbaseHits");
        if (panel == null) return;

        if (hits == null || hits.Count == 0)
        {
            panel.ItemsSource = null;
            panel.IsVisible = false;
            return;
        }

        var controls = new List<Control>();
        foreach (var hit in hits)
        {
            if (string.IsNullOrWhiteSpace(hit.SourceTerm))
                continue;

            var label = new TextBlock
            {
                Text = $"{hit.SourceTerm} -> {hit.PreferredTarget}",
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

            var tool = string.IsNullOrWhiteSpace(hit.Note)
                ? (string.IsNullOrWhiteSpace(hit.CreatedBy) ? null : $"By: {hit.CreatedBy}")
                : (string.IsNullOrWhiteSpace(hit.CreatedBy) ? hit.Note : $"{hit.Note}\nBy: {hit.CreatedBy}");
            if (!string.IsNullOrWhiteSpace(tool))
                ToolTip.SetTip(border, tool);
            controls.Add(border);
        }

        panel.ItemsSource = controls;
        panel.IsVisible = controls.Count > 0;
    }

    // ----- Confirmation dialog -----

    private async Task<bool> ShowYesNoAsync(string title, string message)
    {
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel == null) return true; // no window => allow action

        var dlg = new Window
        {
            Title = title,
            Width = 380,
            Height = 160,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Application.Current?.FindResource("AppBg") as IBrush
        };

        var result = false;
        var btnYes = new Button { Content = "Yes", Width = 80, Height = 32 };
        var btnNo = new Button { Content = "No", Width = 80, Height = 32 };
        btnYes.Click += (_, _) => { result = true; dlg.Close(); };
        btnNo.Click += (_, _) => { result = false; dlg.Close(); };

        dlg.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { btnNo, btnYes }
                }
            }
        };

        await dlg.ShowDialog(topLevel);
        return result;
    }

    // ----- File pickers -----

    private async Task<string?> PickExportFileAsync(ScholarExportFormat format, string? collectionName)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var baseName = string.IsNullOrWhiteSpace(collectionName)
            ? "scholar-collections"
            : string.Concat(collectionName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "scholar-collections";

        var options = format switch
        {
            ScholarExportFormat.Html => new FilePickerSaveOptions
            {
                Title = "Export Scholar Collection as HTML",
                SuggestedFileName = baseName + ".html",
                DefaultExtension = "html",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("HTML") { Patterns = new[] { "*.html", "*.htm" } }
                }
            },
            ScholarExportFormat.Markdown => new FilePickerSaveOptions
            {
                Title = "Export Scholar Collection as Markdown",
                SuggestedFileName = baseName + ".md",
                DefaultExtension = "md",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Markdown") { Patterns = new[] { "*.md", "*.markdown" } }
                }
            },
            ScholarExportFormat.PlainText => new FilePickerSaveOptions
            {
                Title = "Export Scholar Collection as Plain Text",
                SuggestedFileName = baseName + ".txt",
                DefaultExtension = "txt",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Plain Text") { Patterns = new[] { "*.txt" } }
                }
            },
            ScholarExportFormat.Csv => new FilePickerSaveOptions
            {
                Title = "Export Scholar Collection as CSV",
                SuggestedFileName = baseName + ".csv",
                DefaultExtension = "csv",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } }
                }
            },
            ScholarExportFormat.Tsv => new FilePickerSaveOptions
            {
                Title = "Export Scholar Collection as TSV",
                SuggestedFileName = baseName + ".tsv",
                DefaultExtension = "tsv",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("TSV") { Patterns = new[] { "*.tsv", "*.tab" } }
                }
            },
            ScholarExportFormat.ReaderTagBundle => new FilePickerSaveOptions
            {
                Title = "Export Reader Tag Bundle",
                SuggestedFileName = baseName + ".reader-tags.json",
                DefaultExtension = "json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Reader Tag Bundle JSON") { Patterns = new[] { "*.reader-tags.json", "*.json" } }
                }
            },
            ScholarExportFormat.ReaderTagTsv => new FilePickerSaveOptions
            {
                Title = "Export Reader Tags TSV",
                SuggestedFileName = baseName + ".reader-tags.tsv",
                DefaultExtension = "tsv",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Reader Tags TSV") { Patterns = new[] { "*.reader-tags.tsv", "*.tsv" } }
                }
            },
            ScholarExportFormat.BibTex => new FilePickerSaveOptions
            {
                Title = "Export Scholar Collection as BibTeX",
                SuggestedFileName = baseName + ".bib",
                DefaultExtension = "bib",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("BibTeX") { Patterns = new[] { "*.bib" } }
                }
            },
            ScholarExportFormat.CslJson => new FilePickerSaveOptions
            {
                Title = "Export Scholar Collection as CSL-JSON",
                SuggestedFileName = baseName + ".csl.json",
                DefaultExtension = "json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("CSL-JSON") { Patterns = new[] { "*.csl.json", "*.json" } }
                }
            },
            ScholarExportFormat.PaperDraft => new FilePickerSaveOptions
            {
                Title = "Export Scholar Collection as Paper Draft",
                SuggestedFileName = baseName + "-draft.md",
                DefaultExtension = "md",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Markdown") { Patterns = new[] { "*.md", "*.markdown" } }
                }
            },
            _ => new FilePickerSaveOptions
            {
                Title = "Export Scholar Collections as JSON",
                SuggestedFileName = "scholar-collections.json",
                DefaultExtension = "json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } }
                }
            }
        };

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);
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

    // ----- Export format picker -----

    private async Task<ExportDialogResult?> PickExportFormatAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel == null) return null;

        var dlg = new ExportFormatDialog
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var result = await dlg.ShowDialog<ExportDialogResult?>(topLevel);
        return result;
    }

    // ----- Compare -----

    private bool _compareMode;

    private async Task OnCompareClickedAsync()
    {
        if (!_compareMode)
        {
            // Enter compare mode
            _compareMode = true;
            Status?.Invoke(this, "Check 2-4 passages, then trigger compare again.");
            return;
        }

        // Collect checked passages from the bound models
        var checked_ = _vm.Passages.Where(p => p.IsSelectedForCompare).ToList();
        _compareMode = false;
        foreach (var passage in _vm.Passages)
            passage.IsSelectedForCompare = false;

        if (checked_.Count < 2 || checked_.Count > 4)
        {
            Status?.Invoke(this, "Check 2-4 passages to compare.");
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel == null) return;

        var compareWindow = new ComparePassagesWindow(checked_);
        compareWindow.Topmost = false;
        await compareWindow.ShowDialog(topLevel);
    }

    private void RefreshLinksPanel()
    {
        var panel = this.FindControl<ItemsControl>("PnlOutgoingLinks");
        if (panel == null) return;

        var passage = _vm.SelectedPassage;
        if (passage == null || _vm.SelectedCollection == null)
        {
            panel.ItemsSource = null;
            return;
        }

        var links = _vm.GetLinksForPassage(passage.Id);

        if (links.Count == 0)
        {
            panel.ItemsSource = null;
            return;
        }

        var controls = new List<Control>();
        foreach (var link in links)
        {
            var otherPassageId = link.FromPassageId == passage.Id
                ? link.ToPassageId
                : link.FromPassageId;
            var otherPassage = _vm.FindPassageById(otherPassageId);
            var otherPreview = otherPassage != null
                ? (otherPassage.ZhText.Length > 30 ? otherPassage.ZhText[..30] + "..." : otherPassage.ZhText)
                : "(deleted)";

            var relationChip = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(60, 100, 180, 255)),
                Padding = new Thickness(6, 1),
                Margin = new Thickness(0, 0, 4, 0),
                Child = new TextBlock
                {
                    Text = link.RelationType,
                    FontSize = 11,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                }
            };

            var previewText = new TextBlock
            {
                Text = otherPreview,
                FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };

            // Click to select the linked passage
            if (otherPassage != null)
            {
                var captured = otherPassage;
                previewText.PointerPressed += (_, _) =>
                {
                    _vm.SelectedPassage = captured;
                };
                // Double-click to navigate to source
                previewText.DoubleTapped += (_, _) =>
                {
                    _vm.SelectedPassage = captured;
                    _vm.NavigateToPassageCommand.Execute(null);
                };
            }

            var deleteBtn = new Button
            {
                Content = "\u00d7",
                Padding = new Thickness(4, 0),
                MinWidth = 20,
                MinHeight = 20,
                FontSize = 12,
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            var capturedLinkId = link.Id;
            deleteBtn.Click += async (_, _) =>
            {
                await _vm.RemoveLinkAsync(capturedLinkId);
                RefreshLinksPanel();
            };

            var row = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Vertical,
                Spacing = 2,
                Margin = new Thickness(0, 2)
            };

            var topRow = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 4
            };
            topRow.Children.Add(relationChip);
            topRow.Children.Add(previewText);
            topRow.Children.Add(deleteBtn);
            row.Children.Add(topRow);

            // Show note if present
            if (!string.IsNullOrWhiteSpace(link.Note))
            {
                var noteText = new TextBlock
                {
                    Text = link.Note,
                    FontSize = 11,
                    Opacity = 0.7,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 300,
                    Margin = new Thickness(14, 0, 0, 0)
                };
                row.Children.Add(noteText);
            }

            controls.Add(row);
        }

        panel.ItemsSource = controls;
    }

    private async Task SaveAllCurrentAsync(string? statusMessage = "Saved current Scholar state.")
    {
        await _vm.SaveCurrentStateAsync();
        _lastRenderedPassageId = null;
        _ = RefreshAssistantAsync();
        if (!string.IsNullOrWhiteSpace(statusMessage))
            Status?.Invoke(this, statusMessage);
    }

    // ----- Linked Texts -----

    private void RefreshLinkedTextsPanel()
    {
        var panel = this.FindControl<StackPanel>("PnlLinkedTexts");
        if (panel == null) return;

        var passage = _vm.SelectedPassage;
        // Keep the header TextBlock (first child), clear the rest
        while (panel.Children.Count > 1) panel.Children.RemoveAt(panel.Children.Count - 1);
        if (passage == null || passage.LinkedTexts.Count == 0)
        {
            panel.IsVisible = false;
            return;
        }

        var controls = new List<Control>();
        foreach (var relPath in passage.LinkedTexts)
        {
            var capturedPath = relPath;
            var fileName = System.IO.Path.GetFileNameWithoutExtension(relPath);

            var nameText = new TextBlock
            {
                Text = fileName,
                FontSize = 11,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Foreground = new SolidColorBrush(Color.FromRgb(100, 180, 255)),
                TextDecorations = Avalonia.Media.TextDecorations.Underline
            };
            nameText.PointerPressed += (_, _) =>
            {
                NavigationRequested?.Invoke(this, new NavigationRequest
                {
                    RelPath = capturedPath,
                    Side = SearchSide.Original
                });
            };

            var deleteBtn = new Button
            {
                Content = "\u00d7",
                Padding = new Thickness(2, 0),
                MinWidth = 16,
                MinHeight = 16,
                FontSize = 10,
                Margin = new Thickness(2, 0, 6, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            deleteBtn.Click += async (_, _) =>
            {
                passage.LinkedTexts.Remove(capturedPath);
                passage.ModifiedUtc = DateTimeOffset.UtcNow;
                await _vm.SaveCurrentStateAsync();
                RefreshLinkedTextsPanel();
                Status?.Invoke(this, $"Removed link to '{fileName}'.");
            };

            var chip = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 2,
                Margin = new Thickness(0, 1)
            };
            chip.Children.Add(nameText);
            chip.Children.Add(deleteBtn);
            controls.Add(chip);
        }

        foreach (var c in controls) panel.Children.Add(c);
        panel.IsVisible = true;
    }

    private async Task ShowLinkDialogAsync()
    {
        var fromPassage = _vm.SelectedPassage;
        if (fromPassage == null || _vm.SelectedCollection == null)
        {
            Status?.Invoke(this, "Select a passage first.");
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel == null) return;

        var otherPassages = _vm.SelectedCollection.Passages
            .Where(p => p.Id != fromPassage.Id)
            .ToList();

        if (otherPassages.Count == 0)
        {
            Status?.Invoke(this, "Need at least two passages to create a link.");
            return;
        }

        var dlg = new LinkPassageDialog(otherPassages)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var result = await dlg.ShowDialog<(string PassageId, string RelationType, string? Note)?>(topLevel);
        if (result == null) return;

        await _vm.CreateLinkAsync(fromPassage.Id, result.Value.PassageId, result.Value.RelationType, result.Value.Note);
        RefreshLinksPanel();
        Status?.Invoke(this, $"Link created: {result.Value.RelationType}");
    }

    // ----- Link dialog (kept in code-behind as it's pure UI) -----

    private sealed class LinkPassageDialog : Window
    {
        private readonly ListBox _passageListBox;
        private readonly ComboBox _relationCombo;
        private readonly TextBox _noteBox;

        public LinkPassageDialog(List<ScholarPassage> passages)
        {
            Title = "Link to Passage";
            Width = 400;
            Height = 430;
            Topmost = false;
            CanResize = false;

            var root = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto,Auto"),
                Margin = new Thickness(16),
                RowSpacing = 10
            };

            var header = new TextBlock
            {
                Text = "1. Click a passage below\n2. Choose the relationship type\n3. Click Link",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                FontWeight = FontWeight.SemiBold
            };

            _passageListBox = new ListBox
            {
                ItemsSource = passages,
                SelectedIndex = 0
            };
            _passageListBox.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<ScholarPassage>((p, _) =>
            {
                var sp = new StackPanel { Margin = new Thickness(2) };
                sp.Children.Add(new TextBlock
                {
                    Text = p.ZhText.Length > 40 ? p.ZhText[..40] + "..." : p.ZhText,
                    FontWeight = FontWeight.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                sp.Children.Add(new TextBlock
                {
                    Text = p.SourceRelPath ?? "",
                    FontSize = 10,
                    Opacity = 0.5
                });
                return sp;
            });

            var relationPanel = new DockPanel { Margin = new Thickness(0, 4) };
            var relationLabel = new TextBlock
            {
                Text = "Relation:",
                VerticalAlignment = VerticalAlignment.Center,
                Width = 70
            };
            _relationCombo = new ComboBox
            {
                ItemsSource = PassageLink.RelationTypes,
                SelectedIndex = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            DockPanel.SetDock(relationLabel, Dock.Left);
            relationPanel.Children.Add(relationLabel);
            relationPanel.Children.Add(_relationCombo);

            _noteBox = new TextBox
            {
                Watermark = "Note (optional)",
                AcceptsReturn = true,
                MaxHeight = 60,
                TextWrapping = TextWrapping.Wrap
            };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8
            };

            var btnCancel = new Button { Content = "Cancel", MinWidth = 80 };
            btnCancel.Click += (_, _) => Close(null);

            var btnOk = new Button { Content = "Link", MinWidth = 80 };
            btnOk.Click += (_, _) =>
            {
                var selected = _passageListBox.SelectedItem as ScholarPassage;
                var relation = _relationCombo.SelectedItem as string;
                if (selected != null && !string.IsNullOrEmpty(relation))
                    Close((selected.Id, relation, string.IsNullOrWhiteSpace(_noteBox.Text) ? (string?)null : _noteBox.Text?.Trim()));
                else
                    Close(null);
            };

            buttons.Children.Add(btnCancel);
            buttons.Children.Add(btnOk);

            root.Children.Add(header);
            Grid.SetRow(header, 0);

            root.Children.Add(_passageListBox);
            Grid.SetRow(_passageListBox, 1);

            root.Children.Add(relationPanel);
            Grid.SetRow(relationPanel, 2);

            root.Children.Add(_noteBox);
            Grid.SetRow(_noteBox, 3);

            root.Children.Add(buttons);
            Grid.SetRow(buttons, 4);

            Content = root;
        }
    }

    // ----- Find Parallels -----

    private async Task OnFindParallelsClickedAsync(Button? anchorButton)
    {
        var passage = _vm.SelectedPassage ?? _vm.SelectedCommunityPassage;
        if (passage == null || string.IsNullOrWhiteSpace(passage.ZhText))
        {
            Status?.Invoke(this, "Select a passage with Chinese text first.");
            return;
        }

        var root = _vm.GetRoot();
        if (string.IsNullOrWhiteSpace(root))
        {
            Status?.Invoke(this, "No corpus root loaded.");
            return;
        }

        var oldParallelCts = _parallelCts;
        oldParallelCts?.Cancel();
        _parallelCts = new CancellationTokenSource();
        var ct = _parallelCts.Token;
        try { oldParallelCts?.Dispose(); } catch (ObjectDisposedException) { }

        if (anchorButton != null) anchorButton.IsEnabled = false;
        Status?.Invoke(this, "Searching for parallel passages...");

        try
        {
            // root is the translations repo root; AppPaths expects the parent folder
            var parentRoot = Path.GetDirectoryName(root) ?? root;
            var origDir = _originalDir ?? Infrastructure.AppPaths.GetOriginalDir(parentRoot);
            var tranDir = _translatedDir ?? Infrastructure.AppPaths.GetTranslatedDir(parentRoot);

            var results = await _parallelFinder.FindParallelsAsync(
                passage.ZhText, root, origDir, tranDir, ct);

            if (ct.IsCancellationRequested) return;

            if (results.Count == 0)
            {
                Status?.Invoke(this, "No parallel passages found.");
                return;
            }

            Status?.Invoke(this, $"Found {results.Count} parallel passage(s).");

            // Show results in a flyout if we have an anchor
            if (anchorButton != null)
                ShowParallelResultsFlyout(anchorButton, results);
        }
        catch (Exception ex)
        {
            if (!ct.IsCancellationRequested)
                Status?.Invoke(this, "Parallel search failed: " + ex.Message);
        }
        finally
        {
            if (anchorButton != null) anchorButton.IsEnabled = true;
        }
    }

    private void ShowParallelResultsFlyout(Button anchor, List<ParallelPassageResult> results)
    {
        var listBox = new ListBox
        {
            MaxHeight = 400,
            MinWidth = 350,
            ItemsSource = results
        };

        listBox.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<ParallelPassageResult>((r, _) =>
        {
            var sp = new StackPanel { Margin = new Thickness(2) };
            var snippet = r.Snippet.Length > 50 ? r.Snippet[..50] + "..." : r.Snippet;
            sp.Children.Add(new TextBlock
            {
                Text = snippet,
                FontWeight = FontWeight.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            sp.Children.Add(new TextBlock
            {
                Text = $"{r.RelPath}  (overlap: {r.OverlapScore}%)",
                FontSize = 10,
                Opacity = 0.6
            });
            return sp;
        });

        listBox.SelectionChanged += (_, _) =>
        {
            if (listBox.SelectedItem is ParallelPassageResult r)
            {
                NavigationRequested?.Invoke(this, new NavigationRequest
                {
                    RelPath = r.RelPath,
                    MatchText = r.Snippet.Length > 30 ? r.Snippet[..30] : r.Snippet
                });
            }
        };

        var flyout = new Flyout
        {
            Content = listBox,
            Placement = PlacementMode.BottomEdgeAlignedLeft
        };

        flyout.ShowAt(anchor);
    }

    // ----- Vocabulary Analysis -----

    private async Task OnVocabularyClickedAsync(Button? vocabButton = null)
    {
        // Collect passages from selected collection, or all collections
        var passages = new List<ScholarPassage>();
        if (_vm.SelectedCollection != null)
        {
            passages.AddRange(_vm.SelectedCollection.Passages);
        }
        else
        {
            foreach (var c in _vm.Collections)
                passages.AddRange(c.Passages);
        }

        if (passages.Count == 0)
        {
            Status?.Invoke(this, "No passages to analyze.");
            return;
        }

        if (vocabButton != null) vocabButton.IsEnabled = false;
        try
        {
            var items = VocabularyAnalysisService.Analyze(passages);
            if (items.Count == 0)
            {
                Status?.Invoke(this, "No vocabulary patterns found.");
                return;
            }

            var topLevel = TopLevel.GetTopLevel(this) as Window;
            if (topLevel == null) return;

            var dlg = new VocabularyAnalysisDialog(items)
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            await dlg.ShowDialog(topLevel);
        }
        finally
        {
            if (vocabButton != null) vocabButton.IsEnabled = true;
        }
    }

    // ----- Edit Master Dates -----

    private async Task OnEditMasterDatesClickedAsync()
    {
        var owner = GetOwnerWindow();
        if (owner == null) return;

        var filePath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "master-dates.json");
        var repoRoot = _vm.GetRoot();
        if (await ShowMasterDatesEditorDialogAsync(owner, filePath, repoRoot))
        {
            _vm.InvalidateMasterDatesCache();
            ReloadScholarData(repoRoot);
            Status?.Invoke(this, "Master dates updated.");
        }
    }


    protected virtual Window? GetOwnerWindow() => TopLevel.GetTopLevel(this) as Window;

    protected virtual async Task<bool> ShowMasterDatesEditorDialogAsync(Window owner, string filePath, string? repoRoot)
    {
        var dlg = new MasterDatesEditorDialog(filePath, repoRoot)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            RequestedThemeVariant = ActualThemeVariant
        };
        await dlg.ShowDialog(owner);
        return dlg.Saved;
    }


    protected virtual void ReloadScholarData(string? root)
    {
        if (!string.IsNullOrWhiteSpace(root))
        {
            _vm.SetRoot(root);
            _ = RefreshAssistantAsync();
        }
    }

    // ----- Insert Reference -----

    private void InsertPassageReference(ScholarPassage passage)
    {
        var txtNotes = this.FindControl<TextBox>("TxtPassageNotes");
        if (txtNotes == null) return;

        var reference = $"[[{passage.Id}]]";
        var caretIndex = txtNotes.CaretIndex;
        var currentText = txtNotes.Text ?? "";

        if (caretIndex < 0 || caretIndex > currentText.Length)
            caretIndex = currentText.Length;

        var newText = currentText.Insert(caretIndex, reference);
        txtNotes.Text = newText;
        txtNotes.CaretIndex = caretIndex + reference.Length;
    }

    // ----- Assistant panel -----

    /// <summary>
    /// Refreshes the Scholar assistant panel. When <paramref name="zhOverride"/>
    /// is non-null, uses that text for TM lookup instead of the full passage
    /// ZhText — this powers selection-based TM: highlight a phrase in the
    /// passage text box → get TM matches for just that phrase.
    /// </summary>
    private async Task RefreshAssistantAsync(string? zhOverride = null)
    {
        var passage = _vm.SelectedPassage ?? _vm.SelectedCommunityPassage;
        if (passage == null || string.IsNullOrWhiteSpace(passage.ZhText))
        {
            if (_scholarQaHost != null)
                AssistantPanelRenderer.RenderEmptyGuidance(_scholarQaHost,
                    "Select a passage to see assistant results.");
            _scholarTermHost?.Children.Clear();
            _scholarApprovedTmHost?.Children.Clear();
            _scholarReferenceTmHost?.Children.Clear();
            UpdateTermbaseHits(null);
            _lastRenderedPassageId = null;
            return;
        }

        // Skip if same passage AND no selection override (avoids redundant calls).
        // When a selection override is present we always re-query because the
        // user wants matches for a different text.
        if (zhOverride == null && passage.Id == _lastRenderedPassageId) return;

        try
        {
            var oldCts = _assistantCts;
            oldCts?.Cancel();
            _assistantCts = new CancellationTokenSource();
            var ct = _assistantCts.Token;
            // Dispose old CTS after cancellation propagates (avoids ObjectDisposedException)
            try { oldCts?.Dispose(); } catch (ObjectDisposedException) { }

            var zhText = zhOverride ?? passage.ZhText ?? "";
            var ctx = new CurrentSegmentContext
            {
                RelPath = passage.SourceRelPath ?? "",
                ZhText = zhText,
                EnText = passage.EnText ?? "",
                ZhContextText = passage.ZhText ?? "", // always the full passage for context
                BlockNumber = passage.StartBlockNumber ?? passage.EndBlockNumber ?? 0,
                Mode = TranslationEditMode.Body
            };

            var root = _vm.GetRoot();
            string? assistantTranslatedDir = _translatedDir;
            if (!string.IsNullOrWhiteSpace(root) && !string.IsNullOrWhiteSpace(passage.TranslationUser))
            {
                // root here is the translations repo root; GetUserTranslatedDir expects the parent folder.
                var parentRoot = Path.GetDirectoryName(root) ?? root;
                assistantTranslatedDir = AppPaths.GetUserTranslatedDir(parentRoot, passage.TranslationUser);
            }

            try { _assistantService.SetUsername(_currentUsername); } catch { }
            var configSvc = App.Services.GetService<IAppConfigService>() as AppConfigService;
            int maxResults = 8;
            if (configSvc != null)
            {
                try { var cfg = await configSvc.TryLoadAsync(); maxResults = cfg?.TmMaxResults ?? 8; }
                catch { }
            }
            var snapshot = await _assistantService.BuildSnapshotAsync(
                ctx, root, _originalDir, assistantTranslatedDir, ct, maxResults);

            if (ct.IsCancellationRequested) return;

            _lastRenderedPassageId = passage.Id;
            UpdateTermbaseHits(snapshot?.Terms);

            AssistantPanelRenderer.RenderSnapshot(
                snapshot,
                _scholarQaHost, _scholarTermHost,
                _scholarApprovedTmHost, _scholarReferenceTmHost,
                titleResolver: rel => ResolveSourceTitle(rel),
                brushResolver: GetAssistantBrush,
                navigationHandler: (_, req) => NavigationRequested?.Invoke(this, req),
                addToScholarHandler: passage => AddPassage(passage));
        }
        catch (Exception ex)
        {
            Status?.Invoke(this, "Scholar assistant unavailable: " + ex.Message);
            UpdateTermbaseHits(null);
            AssistantPanelRenderer.RenderSnapshot(null,
                _scholarQaHost, _scholarTermHost,
                _scholarApprovedTmHost, _scholarReferenceTmHost);
        }
    }

    private string ResolveSourceDisplay(ScholarPassage? passage)
    {
        if (passage == null) return "";
        return ResolveSourceTitle(passage.SourceRelPath);
    }

    private string ResolveSourceTitle(string? relPath)
    {
        if (string.IsNullOrWhiteSpace(relPath))
            return "";

        try
        {
            if (SourceTitleResolver != null)
            {
                var resolved = SourceTitleResolver(relPath);
                if (!string.IsNullOrWhiteSpace(resolved))
                    return resolved;
            }
        }
        catch { }

        try
        {
            var fileName = Path.GetFileNameWithoutExtension(relPath);
            return string.IsNullOrWhiteSpace(fileName) ? relPath ?? "" : fileName;
        }
        catch
        {
            return relPath ?? "";
        }
    }
    private static IBrush? GetAssistantBrush(string key)
    {
        if (Avalonia.Application.Current?.TryFindResource(key, out var obj) == true && obj is IBrush brush)
            return brush;
        return null;
    }

    // ----- Public API -----

    public void SetScholarLoading(bool isLoading)
    {
        // ScholarLoadingBar was removed in Phase 4 overhaul; nothing to update.
    }

    public void SetTranslationDirs(string? origDir, string? tranDir)
    {
        _originalDir = origDir;
        _translatedDir = tranDir;
        _lastRenderedPassageId = null; // Force re-render with new dirs
        _ = RefreshAssistantAsync();
    }

    public async Task SaveCurrentStateAsync() => await SaveAllCurrentAsync(statusMessage: null);

    public void SetRoot(string root)
    {
        if (string.Equals(_vm.GetRoot(), root, StringComparison.OrdinalIgnoreCase))
            return;

        _lastRenderedPassageId = null;
        _vm.SetRoot(root);
        _ = RefreshAssistantAsync();
    }
    public void SetAssistantUsername(string? username)
    {
        _currentUsername = string.IsNullOrWhiteSpace(username) ? null : username.Trim();
        try { _assistantService.SetUsername(_currentUsername); } catch { }
        _lastRenderedPassageId = null;
        _ = RefreshAssistantAsync();
    }

    public void SetDictionarySourceOptions(List<string> options)
    {
        // Dictionary source combo was removed from the new layout (moved to overflow menu)
    }

    public void SetDictionarySourceIndex(int index)
    {
        // Dictionary source combo was removed from the new layout (moved to overflow menu)
    }
    public void SetUsername(string? username)
    {
        _vm.SetUsername(username);
    }

    public void Clear()
    {
        try { _assistantCts?.Cancel(); } catch (ObjectDisposedException) { }
        try { _assistantCts?.Dispose(); } catch (ObjectDisposedException) { }
        _assistantCts = null;
        _lastRenderedPassageId = null;
        _vm.Clear();
    }
    public void ReloadCommunity() => _vm.LoadCommunityCommand.Execute(null);

    public void InvalidateTermbaseCache()
    {
        _lastRenderedPassageId = null;
        _ = RefreshAssistantAsync();
    }

    /// <summary>Returns the currently selected scholar passage, or null if none.</summary>
    public ScholarPassage? GetSelectedPassage() => _vm.SelectedPassage;

    /// <summary>Adds a linked text RelPath to the selected passage and saves.</summary>
    public async Task AddLinkedTextAsync(string relPath)
    {
        var passage = _vm.SelectedPassage;
        if (passage == null) return;

        if (!passage.LinkedTexts.Contains(relPath))
        {
            passage.LinkedTexts.Add(relPath);
            passage.ModifiedUtc = DateTimeOffset.UtcNow;
            await _vm.SaveCurrentStateAsync();
            RefreshLinkedTextsPanel();
            Status?.Invoke(this, $"Linked '{System.IO.Path.GetFileNameWithoutExtension(relPath)}' to passage.");
        }
    }

    /// <summary>Fires on ANY ScholarTabView instance after a passage is added and saved.
    /// Other windows can subscribe to reload their scholar data.</summary>
    public static event EventHandler? ScholarDataChanged;

    public void AddPassage(ScholarPassage passage)
    {
        _ = TryAddPassageAsync(passage);
    }

    public async Task<bool> TryAddPassageAsync(ScholarPassage passage)
    {
        if (!await _vm.EnsureStorageContextAsync())
        {
            Status?.Invoke(this, _vm.StatusMessage);
            return false;
        }

        var target = await _vm.EnsureWritableCollectionAsync();
        if (target == null)
        {
            Status?.Invoke(this, string.IsNullOrWhiteSpace(_vm.StatusMessage) ? "Add to Scholar failed." : _vm.StatusMessage);
            return false;
        }

        return await AddPassageAndNotifyAsync(target.Id, passage);
    }

    private async Task<bool> AddPassageAndNotifyAsync(string collectionId, ScholarPassage passage)
    {
        int before = _vm.SelectedCollection?.Passages.Count ?? _vm.Collections.FirstOrDefault(c => c.Id == collectionId)?.Passages.Count ?? 0;
        await _vm.AddPassageToCollectionAsync(collectionId, passage);
        int after = _vm.SelectedCollection?.Passages.Count ?? _vm.Collections.FirstOrDefault(c => c.Id == collectionId)?.Passages.Count ?? 0;
        if (after > before)
        {
            ScholarDataChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        if (!string.IsNullOrWhiteSpace(_vm.StatusMessage))
            Status?.Invoke(this, _vm.StatusMessage);
        return false;
    }
}














