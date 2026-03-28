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
    private HoverDictionaryBehaviorTextBox? _hoverDict;

    // Termbase highlighting
    private readonly ITermbaseStorageService _termbaseStorage = App.Services.GetRequiredService<ITermbaseStorageService>();
    private List<TermbaseEntry>? _cachedTermbaseEntries;
    private string? _termbaseCacheRoot;

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
        _vm.PickExportFormatAsync = PickExportFormatAsync;

        _scholarQaHost = this.FindControl<StackPanel>("ScholarQaHost");
        _scholarTermHost = this.FindControl<StackPanel>("ScholarTermHost");
        _scholarApprovedTmHost = this.FindControl<StackPanel>("ScholarApprovedTmHost");
        _scholarReferenceTmHost = this.FindControl<StackPanel>("ScholarReferenceTmHost");

        WireViewEvents();
        SetupHoverDictionary();

        DetachedFromVisualTree += (_, _) =>
        {
            DisposeHoverDictionary();
            _assistantCts?.Cancel();
            _assistantCts?.Dispose();
        };
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

            // Context menu with "Link to..." on passages list
            var ctxMenu = new ContextMenu();
            var linkMenuItem = new MenuItem { Header = "Link to..." };
            linkMenuItem.Click += async (_, _) => await ShowLinkDialogAsync();
            ctxMenu.Items.Add(linkMenuItem);
            passagesList.ContextMenu = ctxMenu;
        }

        // Compare button
        var btnCompare = this.FindControl<Button>("BtnCompare");
        if (btnCompare != null)
        {
            btnCompare.Click += async (_, _) => await OnCompareClickedAsync();
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

    private async Task OnCompareClickedAsync()
    {
        var passagesList = this.FindControl<ListBox>("PassagesList");
        if (passagesList == null) return;

        var selected = passagesList.SelectedItems?
            .OfType<ScholarPassage>()
            .ToList();

        if (selected == null || selected.Count < 2 || selected.Count > 4)
        {
            Status?.Invoke(this, "Select 2-4 passages (Ctrl+click) to compare.");
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel == null) return;

        var compareWindow = new ComparePassagesWindow(selected);
        await compareWindow.ShowDialog(topLevel);
    }

    // ----- Links -----

    private void RefreshLinksPanel()
    {
        var panel = this.FindControl<ItemsControl>("PnlLinks");
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
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 4,
                Margin = new Thickness(0, 2)
            };
            row.Children.Add(relationChip);
            row.Children.Add(previewText);
            row.Children.Add(deleteBtn);

            controls.Add(row);
        }

        panel.ItemsSource = controls;
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

        var result = await dlg.ShowDialog<(string PassageId, string RelationType)?>(topLevel);
        if (result == null) return;

        await _vm.CreateLinkAsync(fromPassage.Id, result.Value.PassageId, result.Value.RelationType);
        RefreshLinksPanel();
        Status?.Invoke(this, $"Link created: {result.Value.RelationType}");
    }

    // ----- Link dialog (kept in code-behind as it's pure UI) -----

    private sealed class LinkPassageDialog : Window
    {
        private readonly ListBox _passageListBox;
        private readonly ComboBox _relationCombo;

        public LinkPassageDialog(List<ScholarPassage> passages)
        {
            Title = "Link to Passage";
            Width = 400;
            Height = 380;
            CanResize = false;

            var root = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
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
                    Close((selected.Id, relation));
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

            root.Children.Add(buttons);
            Grid.SetRow(buttons, 3);

            Content = root;
        }
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
            _assistantCts?.Cancel();
            _assistantCts?.Dispose();
            _assistantCts = new CancellationTokenSource();
            var ct = _assistantCts.Token;

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
    }

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
        _assistantCts?.Cancel();
        _assistantCts?.Dispose();
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

    /// <summary>Fires on ANY ScholarTabView instance after a passage is added and saved.
    /// Other windows can subscribe to reload their scholar data.</summary>
    public static event EventHandler? ScholarDataChanged;

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

        _ = AddPassageAndNotifyAsync(target.Id, passage);
    }

    private async Task AddPassageAndNotifyAsync(string collectionId, ScholarPassage passage)
    {
        await _vm.AddPassageToCollectionAsync(collectionId, passage);
        ScholarDataChanged?.Invoke(this, EventArgs.Empty);
    }
}
