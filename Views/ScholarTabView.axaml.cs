using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    private readonly IGrammarReferenceService _grammar = App.Services.GetRequiredService<IGrammarReferenceService>();
    private HoverDictionaryBehaviorTextBox? _hoverDict;
    private Canvas? _dictOverlayCanvas;

    // Termbase highlighting
    private readonly ITermbaseStorageService _termbaseStorage = App.Services.GetRequiredService<ITermbaseStorageService>();
    private List<TermbaseEntry>? _cachedTermbaseEntries;
    private string? _termbaseCacheRoot;

    // Parallel passage finder
    private readonly IParallelPassageFinderService _parallelFinder = App.Services.GetRequiredService<IParallelPassageFinderService>();
    private CancellationTokenSource? _parallelCts;

    // Assistant panel
    private readonly ITranslationAssistantService _assistantService = App.Services.GetRequiredService<ITranslationAssistantService>();
    private string? _originalDir;
    private string? _translatedDir;
    private string? _lastRenderedPassageId;
    private CancellationTokenSource? _assistantCts;

    private StackPanel? _scholarQaHost;
    private StackPanel? _scholarTermHost;
    private StackPanel? _scholarApprovedTmHost;
    private StackPanel? _scholarReferenceTmHost;

    // Graph + link stats controls
    private LinkNetworkGraphControl? _graphControl;
    private Button? _btnGraphRelayout;
    private TextBlock? _txtGraphInfo;
    private StackPanel? _pnlLinkStats;
    private TextBlock? _txtLinkCoverage;
    private Button? _btnAddLink;
    private readonly LinkGraphViewModel _graphVm = new();

    public event EventHandler<string>? Status;
    public event EventHandler<NavigationRequest>? NavigationRequested;
    public event EventHandler? DictionaryRequested;

    public ScholarTabView()
    {
        InitializeComponent();

        _vm = new ScholarTabViewModel(App.Services.GetRequiredService<IScholarCollectionsService>());
        DataContext = _vm;

        _vm.StatusChanged += (_, msg) => Status?.Invoke(this, msg);
        _vm.NavigationRequested += (_, req) => NavigationRequested?.Invoke(this, req);

        _vm.PickExportFileAsync = PickExportFileAsync;
        _vm.PickImportFileAsync = PickImportFileAsync;
        _vm.ConfirmAsync = ShowYesNoAsync;
        _vm.PickExportFormatAsync = PickExportFormatAsync;

        _scholarQaHost = this.FindControl<StackPanel>("ScholarQaHost");
        _scholarTermHost = this.FindControl<StackPanel>("ScholarTermHost");
        _scholarApprovedTmHost = this.FindControl<StackPanel>("ScholarApprovedTmHost");
        _scholarReferenceTmHost = this.FindControl<StackPanel>("ScholarReferenceTmHost");

        _dictOverlayCanvas = this.FindControl<Canvas>("DictOverlayCanvas");

        // Graph + link stats controls
        _graphControl = this.FindControl<LinkNetworkGraphControl>("GraphControl");
        _btnGraphRelayout = this.FindControl<Button>("BtnGraphRelayout");
        _txtGraphInfo = this.FindControl<TextBlock>("TxtGraphInfo");
        _pnlLinkStats = this.FindControl<StackPanel>("PnlLinkStats");
        _txtLinkCoverage = this.FindControl<TextBlock>("TxtLinkCoverage");
        _btnAddLink = this.FindControl<Button>("BtnAddLink");

        if (_graphControl != null)
        {
            _graphControl.NodeSelected += (_, passageId) => _vm.SelectPassageById(passageId);
            _graphControl.NodeDoubleClicked += (_, passageId) =>
            {
                _vm.SelectPassageById(passageId);
                _vm.NavigateToPassageCommand.Execute(null);
            };
        }

        if (_btnGraphRelayout != null)
            _btnGraphRelayout.Click += (_, _) => RefreshGraph();

        if (_btnAddLink != null)
            _btnAddLink.Click += async (_, _) => await ShowLinkDialogAsync();

        WireViewEvents();
        SetupHoverDictionary();

        DetachedFromVisualTree += (_, _) =>
        {
            DisposeHoverDictionary();
            try { _assistantCts?.Cancel(); } catch (ObjectDisposedException) { }
            try { _assistantCts?.Dispose(); } catch (ObjectDisposedException) { }
            try { _parallelCts?.Cancel(); } catch (ObjectDisposedException) { }
            try { _parallelCts?.Dispose(); } catch (ObjectDisposedException) { }
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
                if (e.Key == Key.C)
                {
                    var btnCompare = this.FindControl<Button>("BtnCompare");
                    if (btnCompare != null)
                    {
                        btnCompare.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.P)
                {
                    var btnParallels = this.FindControl<Button>("BtnFindParallels");
                    if (btnParallels != null)
                    {
                        btnParallels.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                        e.Handled = true;
                    }
                }
            }
        };

        var passagesList = this.FindControl<ListBox>("PassagesList");
        if (passagesList != null)
        {
            passagesList.DoubleTapped += (_, _) =>
            {
                _vm.NavigateToPassageCommand.Execute(null);
            };

            // Context menu with "Link to..." on passages list
            var ctxMenu = new ContextMenu();
            var linkMenuItem = new MenuItem { Header = "Link to..." };
            linkMenuItem.Click += async (_, _) => await ShowLinkDialogAsync();
            ctxMenu.Items.Add(linkMenuItem);

            var copyLinkItem = new MenuItem { Header = "Copy Link" };
            copyLinkItem.Click += async (_, _) =>
            {
                var passage = _vm.SelectedPassage;
                if (passage == null || string.IsNullOrWhiteSpace(passage.SourceRelPath)) return;

                string? highlight = null;
                // Prefer lb-based links; fall back to highlight text
                if (string.IsNullOrWhiteSpace(passage.FromLb))
                {
                    highlight = passage.ZhText;
                    if (!string.IsNullOrWhiteSpace(highlight) && highlight.Length > 80)
                        highlight = highlight.Substring(0, 80);
                    if (string.IsNullOrWhiteSpace(highlight)) highlight = null;
                }

                var uri = CbetaUriParser.BuildUri(
                    passage.SourceRelPath,
                    fromLb: passage.FromLb,
                    toLb: passage.ToLb,
                    highlightText: highlight,
                    blockNumber: passage.StartBlockNumber);
                var top = TopLevel.GetTopLevel(this);
                if (top?.Clipboard != null)
                    await top.Clipboard.SetTextAsync(uri);
                Status?.Invoke(this, "Link copied to clipboard.");
            };
            ctxMenu.Items.Add(copyLinkItem);

            var copyRedditLink = new MenuItem { Header = "Copy Reddit Link" };
            copyRedditLink.Click += async (_, _) =>
            {
                var passage = _vm.SelectedPassage;
                if (passage == null || string.IsNullOrWhiteSpace(passage.SourceRelPath)) return;

                string? highlight = null;
                if (string.IsNullOrWhiteSpace(passage.FromLb))
                {
                    highlight = passage.ZhText;
                    if (!string.IsNullOrWhiteSpace(highlight) && highlight.Length > 80)
                        highlight = highlight.Substring(0, 80);
                    if (string.IsNullOrWhiteSpace(highlight)) highlight = null;
                }

                var url = CbetaUriParser.BuildShareableUrl(
                    passage.SourceRelPath,
                    fromLb: passage.FromLb,
                    toLb: passage.ToLb,
                    highlightText: highlight,
                    side: SearchSide.Translated);
                var top = TopLevel.GetTopLevel(this);
                if (top?.Clipboard != null)
                    await top.Clipboard.SetTextAsync(url);
                Status?.Invoke(this, "Reddit link copied to clipboard.");
            };
            ctxMenu.Items.Add(copyRedditLink);

            passagesList.ContextMenu = ctxMenu;
        }

        // Compare button
        var btnCompare = this.FindControl<Button>("BtnCompare");
        if (btnCompare != null)
        {
            btnCompare.Click += async (_, _) => await OnCompareClickedAsync();
        }

        // Vocabulary button
        var btnVocab = this.FindControl<Button>("BtnVocabulary");
        if (btnVocab != null)
        {
            btnVocab.Click += async (_, _) => await OnVocabularyClickedAsync(btnVocab);
        }

        // Dictionary button
        var btnDict = this.FindControl<Button>("BtnDictionary");
        if (btnDict != null)
        {
            btnDict.Click += (_, _) => DictionaryRequested?.Invoke(this, EventArgs.Empty);
        }

        // Edit Master Dates button
        var btnEditMasterDates = this.FindControl<Button>("BtnEditMasterDates");
        if (btnEditMasterDates != null)
        {
            btnEditMasterDates.Click += async (_, _) => await OnEditMasterDatesClickedAsync();
        }

        // Find Parallels button
        var btnFindParallels = this.FindControl<Button>("BtnFindParallels");
        if (btnFindParallels != null)
        {
            btnFindParallels.Click += async (_, _) => await OnFindParallelsClickedAsync(btnFindParallels);
        }

        // Insert Reference — populate list when flyout opens, handle selection
        var refList = this.FindControl<ListBox>("ReferencePassageList");
        if (refList != null)
        {
            refList.SelectionChanged += (_, _) =>
            {
                if (refList.SelectedItem is ScholarPassage p)
                {
                    InsertPassageReference(p);
                    refList.SelectedItem = null;
                    // Close the flyout
                    var btn = this.FindControl<Button>("BtnInsertReference");
                    if (btn?.Flyout is Flyout fly) fly.Hide();
                }
            };
        }

        var btnInsertRef = this.FindControl<Button>("BtnInsertReference");
        if (btnInsertRef?.Flyout is Flyout flyout)
        {
            flyout.Opening += (_, _) =>
            {
                var list = this.FindControl<ListBox>("ReferencePassageList");
                if (list != null && _vm.SelectedCollection != null)
                {
                    list.ItemsSource = _vm.SelectedCollection.Passages;
                }
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
            else if (e.PropertyName == nameof(ScholarTabViewModel.SelectedCommunityPassage))
            {
                UpdateCommunityDetailFields();
                _ = RefreshAssistantAsync();
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

        // Tag bubble: Enter to add, X buttons to remove
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

        // Tag bubble remove buttons — use AddHandler on the ItemsControl
        var tagBubblesHost = this.FindControl<ItemsControl>("TagBubblesHost");
        if (tagBubblesHost != null)
        {
            tagBubblesHost.AddHandler(Button.ClickEvent, (sender, e) =>
            {
                if (e.Source is Button btn && btn.Name == "BtnRemoveTag" && btn.Tag is string tag)
                    _vm.RemoveTag(tag);
            });
        }

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

        // Master bubble remove buttons
        var masterBubblesHost = this.FindControl<ItemsControl>("MasterBubblesHost");
        if (masterBubblesHost != null)
        {
            masterBubblesHost.AddHandler(Button.ClickEvent, (sender, e) =>
            {
                if (e.Source is Button btn && btn.Name == "BtnRemoveMaster" && btn.Tag is string name)
                    _vm.RemoveMaster(name);
            });
        }
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
        RefreshLinksPanel();
        RefreshLinkedTextsPanel();

        var categoriesEmpty = this.FindControl<TextBlock>("TxtCategoriesEmpty");
        if (categoriesEmpty != null) categoriesEmpty.IsVisible = passage == null;
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
        _ = UpdateTermbaseHitsAsync(passage?.ZhText);
    }

    // ----- Hover dictionary -----

    private void SetupHoverDictionary()
    {
        DisposeHoverDictionary();

        var txtZhText = this.FindControl<TextBox>("TxtZhText");
        if (txtZhText == null) return;

        try { _hoverDict = new HoverDictionaryBehaviorTextBox(txtZhText, _cedict, _grammar, _dictOverlayCanvas); }
        catch { /* dictionary not available */ }
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

    // ----- Export format picker -----

    private async Task<ScholarExportFormat?> PickExportFormatAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel == null) return null;

        var dlg = new ExportFormatDialog
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var result = await dlg.ShowDialog<ScholarExportFormat?>(topLevel);
        return result;
    }

    // ----- Compare -----

    private bool _compareMode;

    private async Task OnCompareClickedAsync()
    {
        var passagesList = this.FindControl<ListBox>("PassagesList");
        if (passagesList == null)
        {
            _compareMode = false;
            return;
        }

        if (!_compareMode)
        {
            // Enter compare mode — show checkboxes
            _compareMode = true;
            passagesList.Tag = true; // Makes CheckBoxes visible via binding
            var btnCompare = this.FindControl<Button>("BtnCompare");
            if (btnCompare != null) btnCompare.Content = "Go Compare";
            Status?.Invoke(this, "Check 2-4 passages, then click 'Go Compare'.");
            return;
        }

        // Collect checked passages from the visual tree
        var checked_ = new List<ScholarPassage>();
        foreach (var container in passagesList.GetRealizedContainers())
        {
            var cb = FindCheckBox(container);
            if (cb?.IsChecked == true && container.DataContext is ScholarPassage p)
                checked_.Add(p);
        }

        // Exit compare mode
        _compareMode = false;
        passagesList.Tag = false;
        var btn = this.FindControl<Button>("BtnCompare");
        if (btn != null) btn.Content = "Compare";
        ClearCheckboxes(passagesList);

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

    private static CheckBox? FindCheckBox(Control container)
    {
        if (container is CheckBox cb) return cb;
        if (container is ContentPresenter cp && cp.Child is { } child)
            return FindCheckBoxInVisual(child);
        return FindCheckBoxInVisual(container);
    }

    private static CheckBox? FindCheckBoxInVisual(Control root)
    {
        if (root is CheckBox cb) return cb;
        if (root is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is CheckBox found) return found;
                if (child is Panel sub)
                {
                    var result = FindCheckBoxInVisual(sub);
                    if (result != null) return result;
                }
            }
        }
        return null;
    }

    private static void ClearCheckboxes(ListBox list)
    {
        foreach (var container in list.GetRealizedContainers())
        {
            var cb = FindCheckBox(container);
            if (cb != null) cb.IsChecked = false;
        }
    }

    // ----- Links -----

    private void RefreshLinksPanel()
    {
        var panel = this.FindControl<ItemsControl>("PnlLinks");
        var emptyText = this.FindControl<TextBlock>("TxtLinksEmpty");
        var tabHeader = this.FindControl<TextBlock>("TxtLinksTabHeader");
        if (panel == null) return;

        var passage = _vm.SelectedPassage;
        if (passage == null || _vm.SelectedCollection == null)
        {
            panel.ItemsSource = null;
            if (emptyText != null) emptyText.IsVisible = true;
            if (tabHeader != null) tabHeader.Text = "Links";
            return;
        }

        var links = _vm.GetLinksForPassage(passage.Id);

        if (emptyText != null) emptyText.IsVisible = links.Count == 0;
        if (tabHeader != null) tabHeader.Text = links.Count > 0 ? $"Links ({links.Count})" : "Links";

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
        RefreshLinkStats();
        RefreshGraph();
    }

    private void RefreshLinkStats()
    {
        if (_pnlLinkStats == null) return;
        _pnlLinkStats.Children.Clear();

        var collection = _vm.SelectedCollection;
        if (collection == null) return;

        var links = collection.Links ?? new();
        var passages = collection.Passages;

        if (links.Count == 0)
        {
            if (_txtLinkCoverage != null) _txtLinkCoverage.Text = "";
            return;
        }

        // By relation type
        var byType = links.GroupBy(l => l.RelationType ?? "unknown")
            .OrderByDescending(g => g.Count())
            .ToList();

        foreach (var group in byType)
        {
            var colorHex = LinkGraphViewModel.RelationColors.GetValueOrDefault(group.Key, "#9E9E9E");
            Color.TryParse(colorHex, out var color);
            var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6 };
            row.Children.Add(new Border
            {
                Width = Math.Max(20, group.Count() * 20),
                Height = 14,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(color)
            });
            row.Children.Add(new TextBlock
            {
                Text = $"{group.Key} ({group.Count()})",
                FontSize = 11,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            });
            _pnlLinkStats.Children.Add(row);
        }

        // Coverage
        var linkedIds = new HashSet<string>();
        foreach (var l in links) { linkedIds.Add(l.FromPassageId ?? ""); linkedIds.Add(l.ToPassageId ?? ""); }
        linkedIds.Remove("");
        int total = passages.Count;
        int linked = linkedIds.Count(id => passages.Any(p => p.Id == id));
        int orphans = total - linked;

        if (_txtLinkCoverage != null)
            _txtLinkCoverage.Text = $"{linked}/{total} passages linked ({(total > 0 ? linked * 100 / total : 0)}%) \u00b7 {orphans} orphans";
    }

    private void RefreshGraph()
    {
        if (_vm.SelectedCollection == null) return;
        _graphVm.BuildGraph(_vm.SelectedCollection.Passages, _vm.SelectedCollection.Links ?? new());
        _graphVm.RunLayout(80, _graphControl?.Bounds.Width > 0 ? _graphControl.Bounds.Width : 500,
                                _graphControl?.Bounds.Height > 0 ? _graphControl.Bounds.Height : 400);
        _graphControl?.SetViewModel(_graphVm);
        if (_txtGraphInfo != null)
            _txtGraphInfo.Text = $"{_graphVm.Nodes.Count} passages, {_graphVm.Edges.Count} links";
    }

    // ----- Linked Texts -----

    private void RefreshLinkedTextsPanel()
    {
        var panel = this.FindControl<ItemsControl>("PnlLinkedTexts");
        if (panel == null) return;

        var passage = _vm.SelectedPassage;
        if (passage == null || passage.LinkedTexts.Count == 0)
        {
            panel.ItemsSource = null;
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

        panel.ItemsSource = controls;
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
                Text = "Select target passage and relationship type",
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

    private async Task OnFindParallelsClickedAsync(Button anchorButton)
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

        anchorButton.IsEnabled = false;
        Status?.Invoke(this, "Searching for parallel passages...");

        try
        {
            var origDir = _originalDir ?? Infrastructure.AppPaths.GetOriginalDir(root);
            var tranDir = _translatedDir ?? Infrastructure.AppPaths.GetTranslatedDir(root);

            var results = await _parallelFinder.FindParallelsAsync(
                passage.ZhText, root, origDir, tranDir, ct);

            if (ct.IsCancellationRequested) return;

            if (results.Count == 0)
            {
                Status?.Invoke(this, "No parallel passages found.");
                return;
            }

            Status?.Invoke(this, $"Found {results.Count} parallel passage(s).");

            // Show results in a flyout
            ShowParallelResultsFlyout(anchorButton, results);
        }
        catch (Exception ex)
        {
            if (!ct.IsCancellationRequested)
                Status?.Invoke(this, "Parallel search failed: " + ex.Message);
        }
        finally
        {
            anchorButton.IsEnabled = true;
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
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel == null) return;

        var filePath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "master-dates.json");
        var repoRoot = _vm.GetRoot();
        var dlg = new MasterDatesEditorDialog(filePath, repoRoot)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        await dlg.ShowDialog(topLevel);

        if (dlg.Saved)
        {
            _vm.InvalidateMasterDatesCache();
            Status?.Invoke(this, "Master dates updated.");
        }
    }

    // ----- Insert Reference -----

    private void InsertPassageReference(ScholarPassage passage)
    {
        var txtStudyNotes = this.FindControl<TextBox>("TxtStudyNotes");
        if (txtStudyNotes == null) return;

        var reference = $"[[{passage.Id}]]";
        var caretIndex = txtStudyNotes.CaretIndex;
        var currentText = txtStudyNotes.Text ?? "";

        if (caretIndex < 0 || caretIndex > currentText.Length)
            caretIndex = currentText.Length;

        var newText = currentText.Insert(caretIndex, reference);
        txtStudyNotes.Text = newText;
        txtStudyNotes.CaretIndex = caretIndex + reference.Length;
    }

    // ----- Assistant panel -----

    private async Task RefreshAssistantAsync()
    {
        var passage = _vm.SelectedPassage ?? _vm.SelectedCommunityPassage;
        if (passage == null || string.IsNullOrWhiteSpace(passage.ZhText))
        {
            AssistantPanelRenderer.RenderSnapshot(null,
                _scholarQaHost, _scholarTermHost,
                _scholarApprovedTmHost, _scholarReferenceTmHost);
            _lastRenderedPassageId = null;
            return;
        }

        if (passage.Id == _lastRenderedPassageId) return;

        try
        {
            var oldCts = _assistantCts;
            oldCts?.Cancel();
            _assistantCts = new CancellationTokenSource();
            var ct = _assistantCts.Token;
            // Dispose old CTS after cancellation propagates (avoids ObjectDisposedException)
            try { oldCts?.Dispose(); } catch (ObjectDisposedException) { }

            var ctx = new CurrentSegmentContext
            {
                RelPath = passage.SourceRelPath ?? "",
                ZhText = passage.ZhText ?? "",
                EnText = passage.EnText ?? "",
                BlockNumber = 0,
                Mode = TranslationEditMode.Body
            };

            var root = _vm.GetRoot();
            var snapshot = await _assistantService.BuildSnapshotAsync(
                ctx, root, _originalDir, _translatedDir, ct);

            if (ct.IsCancellationRequested) return;

            _lastRenderedPassageId = passage.Id;

            AssistantPanelRenderer.RenderSnapshot(
                snapshot,
                _scholarQaHost, _scholarTermHost,
                _scholarApprovedTmHost, _scholarReferenceTmHost,
                brushResolver: GetAssistantBrush,
                navigationHandler: (_, req) => NavigationRequested?.Invoke(this, req),
                addToScholarHandler: passage => AddPassage(passage));
        }
        catch { /* assistant must never break scholar */ }
    }

    private static IBrush? GetAssistantBrush(string key)
    {
        if (Avalonia.Application.Current?.TryFindResource(key, out var obj) == true && obj is IBrush brush)
            return brush;
        return null;
    }

    // ----- Public API -----

    public void SetTranslationDirs(string? origDir, string? tranDir)
    {
        _originalDir = origDir;
        _translatedDir = tranDir;
        _lastRenderedPassageId = null; // Force re-render with new dirs
    }

    public async Task SaveCurrentStateAsync() => await _vm.SaveCurrentStateAsync();

    public void SetRoot(string root)
    {
        _cachedTermbaseEntries = null;
        _termbaseCacheRoot = null;
        _lastRenderedPassageId = null;
        _vm.SetRoot(root);
    }
    public void SetUsername(string? username) => _vm.SetUsername(username);

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
        _cachedTermbaseEntries = null;
        _termbaseCacheRoot = null;
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
        _ = AddPassageAsync(passage);
    }

    private async Task AddPassageAsync(ScholarPassage passage)
    {
        // Ensure root is set (may not be if scholar tab was never visited)
        if (string.IsNullOrWhiteSpace(_vm.GetRoot()))
        {
            System.Diagnostics.Debug.WriteLine("[Scholar] AddPassage: root not set, cannot save");
            return;
        }

        // If no collections, create a default one first and wait for its save
        if (_vm.Collections.Count == 0)
        {
            _vm.AddCollectionCommand.Execute(null);
            // Give the fire-and-forget save a moment to complete
            await Task.Delay(100);
        }

        // Add to selected collection (or first one)
        var target = _vm.SelectedCollection ?? (_vm.Collections.Count > 0 ? _vm.Collections[0] : null);
        if (target == null) return;

        await AddPassageAndNotifyAsync(target.Id, passage);
    }

    private async Task AddPassageAndNotifyAsync(string collectionId, ScholarPassage passage)
    {
        await _vm.AddPassageToCollectionAsync(collectionId, passage);
        ScholarDataChanged?.Invoke(this, EventArgs.Empty);
    }
}
