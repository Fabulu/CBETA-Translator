// Views/MainWindow.axaml.cs
//
// Thin code-behind after Wave 5 MVVM extraction.
// Responsibilities: control lookup, bridge wiring, dialogs, window chrome,
// keyboard shortcuts, theme application, DispatcherTimers.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.Text;
using CbetaTranslator.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Views;

public partial class MainWindow : Window
{
    // UI controls
    private Button? _btnToggleNav, _btnOpenRoot, _btnSettings, _btnSave, _btnLicenses;
    private Button? _btnMinimize, _btnMaximize, _btnClose;
    private Border? _navPanel, _topBar, _emptyStateOverlay;

    private ListBox? _filesList;
    private TextBox? _navSearch;
    private CheckBox? _chkShowFilenames, _chkZenOnly;
    private ComboBox? _cmbStatusFilter;

    private TextBlock? _txtRoot, _txtCurrentFile, _txtStatus;

    private TabControl? _tabs;
    private ReadableTabView? _readableView;
    private TranslationTabView? _translationView;
    private SearchTabView? _searchView;
    private GitTabView? _gitView;
    private ScholarTabView? _scholarView;

    // ViewModel
    private MainWindowViewModel _vm = null!;
    public MainWindowViewModel? ViewModel => _vm;

    // Dirty timer
    private DispatcherTimer? _dirtyTimer;

    // Nav filter debounce
    private DispatcherTimer? _navFilterDebounce;

    // Index cache save debounce
    private DispatcherTimer? _indexCacheSaveDebounce;

    // Suppress flags
    private bool _suppressNavSelectionChanged;
    private bool _suppressTabEvents;

    // Termbase editor (non-modal -- at most one instance per main window)
    private TermbaseEditorWindow? _termbaseEditorWindow;

    // Tag editor (non-modal -- at most one instance per main window)
    private TagEditorWindow? _tagEditorWindow;

    // Tour overlay controls
    private Canvas? _tourOverlayCanvas;
    private TourSpotlightOverlay? _tourSpotlight;
    private TourTooltipPanel? _tourTooltip;
    private OnboardingTourService? _tourService;

    // Stored handler for static event (must unsubscribe on close to avoid leak)
    private EventHandler? _scholarDataChangedHandler;

    // Stored handlers for child view events (Issue 24: unsubscribe on close)
    private EventHandler<ScholarPassage>? _readableAddToScholarHandler;
    private EventHandler<ScholarPassage>? _translationAddToScholarHandler;
    private EventHandler<ScholarPassage>? _searchAddToScholarHandler;
    private EventHandler<string>? _scholarStatusHandler;
    private EventHandler<string>? _gitStatusHandler;
    private EventHandler<string>? _rootClonedHandler;
    private EventHandler? _communityDataFetchedHandler;

    // -------------------------
    // Secondary-window support
    // -------------------------

    public bool IsSecondaryWindow { get; private set; }

    private readonly TaskCompletionSource _windowReady = new();

    // Parameterless ctor used by App.axaml.cs (main window) and Avalonia XAML loader.
    public MainWindow() : this(isSecondaryWindow: false) { }

    // Parameterized ctor used by WindowNavigationService (secondary windows).
    public MainWindow(bool isSecondaryWindow)
    {
        IsSecondaryWindow = isSecondaryWindow;

        InitializeComponent();
        FindControls();
        CreateViewModel();
        WireBridges();
        WireEvents();
        WireChildViewEvents();

        _vm.SetStatus("Ready.");
        _vm.UpdateSaveButtonState();

        _ = LoadConfigAndAutoloadAsync();
        StartDirtyTimer();

        string closeWhat = isSecondaryWindow ? "close this window" : "close the app";
        Closing += async (_, e) =>
        {
            try { if (_scholarView != null) await _scholarView.SaveCurrentStateAsync(); } catch { }
            if (!await _vm.ConfirmNavigateIfDirtyAsync(closeWhat)) e.Cancel = true;

            // Issue 2: Unsubscribe from static event to prevent leak
            if (_scholarDataChangedHandler != null)
                ScholarTabView.ScholarDataChanged -= _scholarDataChangedHandler;

            // Issue 11: Stop DispatcherTimers
            _dirtyTimer?.Stop();
            _navFilterDebounce?.Stop();
            _indexCacheSaveDebounce?.Stop();

            // Issue 24: Unsubscribe child view events to prevent accumulation on window recreation
            UnsubscribeChildViewEvents();
        };

        // Ensure maximize respects taskbar / screen working area
        PropertyChanged += (_, e) =>
        {
            if (e.Property == WindowStateProperty && WindowState == WindowState.Maximized)
            {
                var screen = Screens?.Primary;
                if (screen != null)
                {
                    var wa = screen.WorkingArea;
                    var scaling = screen.Scaling;
                    var maxW = wa.Width / scaling;
                    var maxH = wa.Height / scaling;
                    if (Height > maxH + 10 || Position.Y < wa.Y / scaling)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            Position = new Avalonia.PixelPoint(wa.X, wa.Y);
                            Width = maxW;
                            Height = maxH;
                        });
                    }
                }
            }
        };
    }

    public async Task OpenAtAsync(string root, NavigationRequest request)
    {
        await _windowReady.Task;

        await _vm.OpenAtCoreAsync(root, request);

        if (_readableView != null && !string.IsNullOrEmpty(request.MatchText))
            await _readableView.NavigateToAsync(request);
    }

    /// <summary>
    /// Routes a non-passage deep link to the appropriate tab/handler.
    /// </summary>
    public async Task HandleDeepLinkAsync(DeepLinkRequest request)
    {
        await _windowReady.Task;

        switch (request.Kind)
        {
            case DeepLinkKind.Dictionary:
                HandleDictDeepLink(request.DictTerm);
                break;
            case DeepLinkKind.Scholar:
                await HandleScholarDeepLinkAsync(request.ScholarCollectionId, request.ScholarPassageId);
                break;
            case DeepLinkKind.Search:
                HandleSearchDeepLink(request.SearchQuery);
                break;
            case DeepLinkKind.Tags:
                HandleTagsDeepLink(request.TagsRelPath, request.TagsUser);
                break;
            case DeepLinkKind.Termbase:
                HandleTermbaseDeepLink(request.TermbaseEntry);
                break;
        }
    }

    private void HandleDictDeepLink(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            _vm.SetStatus("Dictionary link: no term specified.", StatusSeverity.Warning);
            return;
        }
        // Switch to reader tab (where hover dictionary is available)
        ForceTab(0);
        _vm.SetStatus($"Dictionary: \"{term}\" \u2014 hover over Chinese text in the reader to see definitions.", StatusSeverity.Info);
    }

    private async Task HandleScholarDeepLinkAsync(string? collectionId, string? passageId)
    {
        if (string.IsNullOrWhiteSpace(collectionId))
        {
            _vm.SetStatus("Scholar link: no collection specified.", StatusSeverity.Warning);
            return;
        }
        // Switch to scholar tab (index 4)
        ForceTab(4);

        if (_scholarView != null)
        {
            var vm = (ScholarTabViewModel)_scholarView.DataContext!;
            bool found = await vm.TryNavigateToPassageAsync(collectionId, passageId);
            if (!found)
                _vm.SetStatus("This scholar passage isn't available. The person who shared this link may not have synced their data yet.", StatusSeverity.Warning);
        }
    }

    private void HandleSearchDeepLink(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _vm.SetStatus("Search link: no query specified.", StatusSeverity.Warning);
            return;
        }
        // Switch to search tab (index 2)
        ForceTab(2);
        if (_searchView != null)
        {
            _searchView.SetSearchTextAndExecute(query);
        }
        _vm.SetStatus($"Searching: \"{query}\"");
    }

    private void HandleTagsDeepLink(string? relPath, string? user)
    {
        if (string.IsNullOrWhiteSpace(relPath))
        {
            _vm.SetStatus("Tags link: no file specified.", StatusSeverity.Warning);
            return;
        }
        // Switch to reader tab and select the file
        ForceTab(0);
        _vm.SetStatus($"Tags: opened {relPath}" + (user != null ? $" (user: {user})" : ""), StatusSeverity.Info);
    }

    private void HandleTermbaseDeepLink(string? entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
        {
            _vm.SetStatus("Termbase link: no term specified.", StatusSeverity.Warning);
            return;
        }
        _vm.SetStatus($"Termbase: \"{entry}\" \u2014 open the termbase editor to find this entry.", StatusSeverity.Info);
    }

    private async Task LoadConfigAndAutoloadAsync()
    {
        try
        {
            await _vm.LoadConfigApplyThemeAndMaybeAutoloadAsync(IsSecondaryWindow);
            MaybeStartTour();
        }
        finally
        {
            _windowReady.TrySetResult();
        }
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    private T? Find<T>(string name) where T : Control => this.FindControl<T>(name);

    private void FindControls()
    {
        _btnToggleNav = Find<Button>("BtnToggleNav");
        _btnOpenRoot = Find<Button>("BtnOpenRoot");
        _btnSettings = Find<Button>("BtnSettings");
        _btnSave = Find<Button>("BtnSave");
        _btnLicenses = Find<Button>("BtnLicenses");
        _btnMinimize = Find<Button>("BtnMinimize");
        _btnMaximize = Find<Button>("BtnMaximize");
        _btnClose = Find<Button>("BtnClose");

        _topBar = Find<Border>("TopBar");
        _navPanel = Find<Border>("NavPanel");
        _emptyStateOverlay = Find<Border>("EmptyStateOverlay");

        _filesList = Find<ListBox>("FilesList");
        _navSearch = Find<TextBox>("NavSearch");
        _chkShowFilenames = Find<CheckBox>("ChkShowFilenames");
        _chkZenOnly = Find<CheckBox>("ChkZenOnly");
        _cmbStatusFilter = Find<ComboBox>("CmbStatusFilter");

        _txtRoot = Find<TextBlock>("TxtRoot");
        _txtCurrentFile = Find<TextBlock>("TxtCurrentFile");
        _txtStatus = Find<TextBlock>("TxtStatus");

        _tabs = Find<TabControl>("MainTabs");
        _readableView = Find<ReadableTabView>("ReadableView");
        _translationView = Find<TranslationTabView>("TranslationView");
        _searchView = Find<SearchTabView>("SearchView");
        _gitView = Find<GitTabView>("GitView");
        _scholarView = Find<ScholarTabView>("ScholarView");

        _tourOverlayCanvas = Find<Canvas>("TourOverlayCanvas");
        _tourSpotlight = Find<TourSpotlightOverlay>("TourSpotlight");
        _tourTooltip = Find<TourTooltipPanel>("TourTooltip");
    }

    private void CreateViewModel()
    {
        var sp = App.Services;
        _vm = new MainWindowViewModel(
            sp.GetRequiredService<IFileService>(),
            sp.GetRequiredService<IAppConfigService>(),
            sp.GetRequiredService<IIndexCacheService>(),
            sp.GetRequiredService<IRenderedDocumentCacheService>(),
            sp.GetRequiredService<IZenTextsService>(),
            sp.GetRequiredService<IIndexedTranslationService>(),
            sp.GetRequiredService<ITranslationAssistantService>(),
            sp.GetRequiredService<ITranslationAssistantBuildService>(),
            sp.GetRequiredService<ITranslationReviewService>(),
            sp.GetRequiredService<ISearchIndexService>(),
            sp.GetRequiredService<IDocumentTagService>());

        DataContext = _vm;

        _tourService = sp.GetRequiredService<OnboardingTourService>();
    }

    private void WireBridges()
    {
        // StatusText -> TxtStatus (via property changed, or direct bridge)
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.StatusText))
            {
                if (_txtStatus != null) _txtStatus.Text = _vm.StatusText;
            }
            else if (e.PropertyName == nameof(MainWindowViewModel.StatusSeverity))
            {
                if (_txtStatus != null)
                {
                    var key = _vm.StatusSeverity switch
                    {
                        StatusSeverity.Error   => "ErrorFg",
                        StatusSeverity.Warning => "WarningFg",
                        StatusSeverity.Success => "SuccessFg",
                        _                      => "TextMutedFg"
                    };
                    if (Application.Current?.Resources.TryGetValue(key, out var brush) == true && brush is Avalonia.Media.IBrush b)
                        _txtStatus.Foreground = b;
                }
            }
            else if (e.PropertyName == nameof(MainWindowViewModel.RootDisplayText))
            {
                if (_txtRoot != null) _txtRoot.Text = _vm.RootDisplayText;
                if (_emptyStateOverlay != null)
                    _emptyStateOverlay.IsVisible = string.IsNullOrEmpty(_vm.RootDisplayText);
            }
            else if (e.PropertyName == nameof(MainWindowViewModel.CurrentFileText))
            {
                if (_txtCurrentFile != null) _txtCurrentFile.Text = _vm.CurrentFileText;
            }
            else if (e.PropertyName == nameof(MainWindowViewModel.WindowTitle))
            {
                Title = _vm.WindowTitle;
            }
        };

        // ReadableTabView bridges
        _vm.SetReadableRendered = (ro, rt) => _readableView?.SetRendered(ro, rt);
        _vm.ClearReadable = () => _readableView?.Clear();
        _vm.SetReadableHoverDict = enabled =>
        {
            try
            {
                var m = _readableView?.GetType().GetMethod("SetHoverDictionaryEnabled");
                m?.Invoke(_readableView, new object[] { enabled });
            }
            catch { }
        };
        _vm.SetReadableZenContext = (rel, isZen) => _readableView?.SetZenContext(rel, isZen);
        _vm.UpdateReadableTermHighlights = (hits, zh, hint, anchor) =>
            _readableView?.UpdateTermbaseHighlights(hits, zh, preferredOccurrenceHint: hint, anchorTextSignal: anchor);
        _vm.SetReadableDefaultResp = resp =>
        {
            if (_readableView != null) _readableView.DefaultResp = resp;
        };
        _vm.SetReadableStudySnapshot = snapshot => _readableView?.SetStudyPanelSnapshot(snapshot);
        _vm.SetReadableStudyPanelVisible = visible => _readableView?.SetStudyPanelVisible(visible);
        _vm.SetReadableTagVocabulary = vocab => _readableView?.SetTagVocabulary(vocab);
        _vm.SetReadableAppliedTags = tags => _readableView?.SetAppliedTags(tags);
        _vm.SetReadableCommunityTags = tags => _readableView?.SetCommunityTags(tags);
        _vm.SetReadableCommunityVocabularies = vocabs => _readableView?.SetCommunityVocabularies(vocabs);
        _vm.SetSearchTagFilterData = (tags, vocab) => _searchView?.SetTagFilterData(tags, vocab);

        // TranslationTabView bridges
        _vm.SetTranslationModeProjection = (mode, text) => _translationView?.SetModeProjection(mode, text);
        _vm.GetTranslationProjectionText = () => _translationView?.GetCurrentProjectionText() ?? "";
        _vm.ClearTranslation = () => _translationView?.Clear();
        _vm.SetTranslationHoverDict = enabled => _translationView?.SetHoverDictionaryEnabled(enabled);
        _vm.SetAssistantSnapshot = snapshot => _translationView?.SetAssistantSnapshot(snapshot);
        _vm.SetCurrentReviewState = (status, reviewer, date, agg) => _translationView?.SetCurrentReviewState(status, reviewer, date, agg);
        _vm.SetProgressStats = (a, n, t) => _translationView?.SetProgressStats(a, n, t);
        _vm.FillEnForCurrentBlock = (en, block) => _translationView?.FillEnForCurrentBlock(en, block);
        _vm.JumpToNextBlock = () => _translationView?.JumpToNextBlock();
        _vm.JumpToPreviousBlock = () => _translationView?.JumpToPreviousBlock();
        _vm.JumpToNextUnapproved = approved => _translationView?.JumpToNextUnapproved(approved);
        _vm.IsTranslationEditorFocused = () => _translationView?.IsEditorFocused() ?? false;
        _vm.GetAllBlockNumbers = () => _translationView?.GetAllBlockNumbers() ?? Array.Empty<int>();
        _vm.UpdateTranslationTermHighlights = (hits, zh) => _translationView?.UpdateTermbaseHighlights(hits, zh);
        _vm.UpdateTranslationTmSharedHighlights = (approved, reference, zh) =>
            _translationView?.UpdateTmSharedHighlights(approved, reference, zh);
        _vm.SetTranslationFilePaths = (orig, tran) => _translationView?.SetCurrentFilePaths(orig, tran);
        _vm.SetAssistantTitleResolver = resolver => _translationView?.SetAssistantTitleResolver(resolver);
        _vm.SetTranslationSourceOptions = options => _translationView?.SetTranslationSourceOptions(options);
        _vm.SetTranslationSourceIndex = index => _translationView?.SetTranslationSourceIndex(index);
        _vm.SetTranslationEditorReadOnly = readOnly => _translationView?.SetEditorReadOnly(readOnly);
        _vm.SignalCoreLoadComplete = () => _windowReady.TrySetResult();

        // SearchTabView bridges
        _vm.SetSearchRootContext = (root, orig, tran) => _searchView?.SetRootContext(root, orig, tran);
        _vm.SetSearchZenResolver = resolver => _searchView?.SetZenResolver(resolver);
        _vm.SetSearchContext = (root, orig, tran, meta) => _searchView?.SetContext(root, orig, tran, fileMeta: meta);
        _vm.ClearSearch = () => _searchView?.Clear();

        // GitTabView bridges
        _vm.SetGitRepoRoot = root => _gitView?.SetCurrentRepoRoot(root);
        _vm.SetGitSelectedRelPath = rel => _gitView?.SetSelectedRelPath(rel);
        _vm.SetGitUsername = user => _gitView?.SetUsername(user);
        _vm.LoadGitPersistedAuth = (token, login) => _gitView?.LoadPersistedAuth(token, login);

        // ScholarTabView bridges
        _vm.SetScholarRoot = root => _scholarView?.SetRoot(root);
        _vm.ClearScholar = () => _scholarView?.Clear();
        _vm.SetScholarUsername = user => _scholarView?.SetUsername(user);
        _vm.SetScholarTranslationDirs = (orig, tran) => _scholarView?.SetTranslationDirs(orig, tran);
        _vm.SaveScholarStateAsync = async () => { if (_scholarView != null) await _scholarView.SaveCurrentStateAsync(); };

        // Dialog bridges
        _vm.ShowFolderPickerAsync = ShowFolderPickerDialogAsync;
        _vm.ShowSettingsDialogAsync = ShowSettingsDialogAsync;
        _vm.ShowUsernamePromptAsync = ShowUsernamePromptDialogAsync;
        _vm.ShowLicensesAsync = ShowLicensesDialogAsync;
        _vm.ShowYesNoDialogAsync = ShowYesNoAsync;

        // Window bridges
        _vm.SetWindowTitle = title => Title = title;
        _vm.ApplyTheme = dark => ApplyTheme(dark);
        _vm.SetSaveButtonEnabled = enabled => { if (_btnSave != null) _btnSave.IsEnabled = enabled; };
        _vm.GetSelectedTabIndex = () => _tabs?.SelectedIndex ?? -1;
        _vm.ForceTabIndex = idx => ForceTab(idx);
        _vm.NavigateInReadable = async req =>
        {
            if (_readableView != null && (!string.IsNullOrEmpty(req.MatchText) || !string.IsNullOrEmpty(req.FromLb)))
                await _readableView.NavigateToAsync(req);
        };

        // Nav bridges
        _vm.SetNavItemsSource = items =>
        {
            try
            {
                _suppressNavSelectionChanged = true;
                if (_filesList != null) _filesList.ItemsSource = items;
            }
            finally { _suppressNavSelectionChanged = false; }
        };
        _vm.SetNavSelectedItem = item =>
        {
            try
            {
                _suppressNavSelectionChanged = true;
                if (_filesList != null) _filesList.SelectedItem = item;
            }
            finally { _suppressNavSelectionChanged = false; }
        };
        _vm.GetNavSelectedItem = () => _filesList?.SelectedItem as FileNavItem;
        _vm.RestoreNavSearchFocus = () =>
        {
            if (_navSearch != null)
                Dispatcher.UIThread.Post(() => _navSearch.Focus(), DispatcherPriority.Background);
        };
        _vm.IsNavSearchFocused = () => _navSearch != null && _navSearch.IsFocused;
        _vm.GetNavSearchText = () => _navSearch?.Text ?? "";
        _vm.GetShowFilenames = () => _chkShowFilenames != null && _chkShowFilenames.IsChecked == true;
        _vm.GetZenOnly = () => _chkZenOnly != null && _chkZenOnly.IsChecked == true;
        _vm.GetStatusFilterIndex = () => _cmbStatusFilter?.SelectedIndex ?? 0;

        // Config loaded callback
        _vm.OnConfigLoaded = config =>
        {
            if (_chkZenOnly != null) _chkZenOnly.IsChecked = config.ZenOnly;
        };

        // Index cache save debounce
        _vm.ScheduleIndexCacheSave = ScheduleIndexCacheSave;

        // Termbase editor
        _vm.OpenTermbaseEditorRequested = (root, username) => _ = OpenTermbaseEditorWindowAsync(root, username);

        // Wire assistant title resolver
        _vm.SetAssistantTitleResolver?.Invoke(rel => _vm.ResolveAssistantTitle(rel));

        // Tour: auto-index complete
        _vm.OnAutoIndexCompleted = () => _tourService?.AdvanceIfWaitingFor("index-built");
    }

    // ===========================================================
    // Events
    // ===========================================================

    private void WireEvents()
    {
        if (_btnToggleNav != null)
        {
            _btnToggleNav.Click += (_, _) =>
            {
                if (_navPanel != null) _navPanel.IsVisible = !_navPanel.IsVisible;
            };
        }

        if (_btnOpenRoot != null) _btnOpenRoot.Click += async (_, _) => await _vm.OpenRootAsync();
        if (_btnSettings != null) _btnSettings.Click += async (_, _) => await _vm.OpenSettingsAsync();
        if (_btnLicenses != null) _btnLicenses.Click += async (_, _) => await _vm.OpenLicensesAsync();

        var btnGetStarted = Find<Button>("BtnGetStarted");
        if (btnGetStarted != null)
            btnGetStarted.Click += (_, _) => StartTour();

        var btnOpenRootAlt = Find<Button>("BtnOpenRootAlt");
        if (btnOpenRootAlt != null)
            btnOpenRootAlt.Click += async (_, _) => await _vm.OpenRootAsync();

        if (_btnSave != null)
            _btnSave.Click += async (_, _) => await _vm.SaveTranslatedFromTabAsync();

        if (_btnMinimize != null)
            _btnMinimize.Click += (_, _) => WindowState = WindowState.Minimized;

        if (_btnMaximize != null)
            _btnMaximize.Click += (_, _) =>
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        if (_btnClose != null)
            _btnClose.Click += (_, _) => Close();

        if (_filesList != null)
        {
            _filesList.SelectionChanged += FilesList_SelectionChanged;

            var mnuLinkToPassage = Find<MenuItem>("MnuLinkToPassage");
            if (mnuLinkToPassage != null)
            {
                // Enable/disable based on whether a scholar passage is selected
                if (_filesList.ContextMenu != null)
                {
                    _filesList.ContextMenu.Opening += (_, _) =>
                    {
                        mnuLinkToPassage.IsEnabled = _scholarView?.GetSelectedPassage() != null;
                    };
                }

                mnuLinkToPassage.Click += async (_, _) =>
                {
                    var navItem = _filesList.SelectedItem as FileNavItem;
                    if (navItem == null || string.IsNullOrWhiteSpace(navItem.RelPath)) return;

                    if (_scholarView == null) return;
                    var passage = _scholarView.GetSelectedPassage();
                    if (passage == null)
                    {
                        _vm.SetStatus("No scholar passage selected.");
                        return;
                    }

                    await _scholarView.AddLinkedTextAsync(navItem.RelPath);
                };
            }

            var mnuCopyLink = Find<MenuItem>("MnuCopyLink");
            if (mnuCopyLink != null)
            {
                mnuCopyLink.Click += async (_, _) =>
                {
                    var navItem = _filesList.SelectedItem as FileNavItem;
                    if (navItem == null || string.IsNullOrWhiteSpace(navItem.RelPath)) return;

                    var uri = CbetaUriParser.BuildUri(navItem.RelPath);
                    var top = TopLevel.GetTopLevel(this);
                    if (top?.Clipboard != null)
                        await top.Clipboard.SetTextAsync(uri);
                    _vm.SetStatus("Link copied to clipboard.");
                };
            }

            var mnuCopyRedditLink = Find<MenuItem>("MnuCopyRedditLink");
            if (mnuCopyRedditLink != null)
            {
                mnuCopyRedditLink.Click += async (_, _) =>
                {
                    var navItem = _filesList.SelectedItem as FileNavItem;
                    if (navItem == null || string.IsNullOrWhiteSpace(navItem.RelPath)) return;

                    var url = CbetaUriParser.BuildShareableUrl(navItem.RelPath);
                    var top = TopLevel.GetTopLevel(this);
                    if (top?.Clipboard != null)
                        await top.Clipboard.SetTextAsync(url);
                    _vm.SetStatus("Reddit link copied to clipboard.");
                };
            }
        }

        if (_tabs != null)
        {
            _tabs.SelectionChanged += async (_, _) =>
            {
                if (_suppressTabEvents) return;
                await _vm.OnTabSelectionChangedAsync();
                _vm.UpdateSaveButtonState();
            };
            _vm.SetLastTabIndex(_tabs.SelectedIndex);
        }

        if (_navSearch != null)
            _navSearch.TextChanged += (_, _) => ScheduleApplyFilter(debounce: true);

        if (_chkShowFilenames != null)
            _chkShowFilenames.IsCheckedChanged += (_, _) => ScheduleApplyFilter(debounce: false);

        if (_cmbStatusFilter != null)
            _cmbStatusFilter.SelectionChanged += (_, _) => ScheduleApplyFilter(debounce: false);

        if (_chkZenOnly != null)
        {
            _chkZenOnly.IsCheckedChanged += async (_, _) =>
            {
                ScheduleApplyFilter(debounce: false);
                await _vm.SaveUiStateAsync();
            };
        }

        if (_topBar != null)
        {
            _topBar.PointerPressed += TopBar_PointerPressed;
            _topBar.DoubleTapped += (_, _) =>
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            };
        }

        // Tour tooltip buttons
        if (_tourTooltip != null)
        {
            _tourTooltip.NextClicked += (_, _) => _tourService?.Next();
            _tourTooltip.BackClicked += (_, _) => _tourService?.Previous();
            _tourTooltip.SkipClicked += (_, _) => _tourService?.Skip();
        }

        // Tour service events
        if (_tourService != null)
        {
            _tourService.StepChanged += (_, step) => Dispatcher.UIThread.Post(() => ShowTourStep(step));
            _tourService.TourCompleted += async (_, _) => await Dispatcher.UIThread.InvokeAsync(OnTourFinished);
            _tourService.TourSkipped += async (_, _) => await Dispatcher.UIThread.InvokeAsync(OnTourFinished);
        }

        // Recalculate tour spotlight on resize
        ((AvaloniaObject)this).PropertyChanged += (_, e) =>
        {
            if (e.Property == ClientSizeProperty && _tourService is { IsActive: true, CurrentStep: not null })
                ShowTourStep(_tourService.CurrentStep);
        };
    }

    private void TopBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var visual = e.Source as Visual;
        while (visual != null)
        {
            if (visual is Button || visual is TextBox || visual is CheckBox || visual is ComboBox) return;
            visual = visual.GetVisualParent();
        }

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            try { BeginMoveDrag(e); } catch { }
        }
    }

    private void WireChildViewEvents()
    {
        if (_readableView != null)
        {
            _readableView.Status += (_, msg) => _vm.SetStatus(msg);

            _readableView.ZenFlagChanged += async (_, ev) =>
            {
                await _vm.HandleZenFlagChangedAsync(ev.RelPath, ev.IsZen);
            };

            _readableView.CommunityNoteInsertRequested += async (_, req) =>
            {
                await _vm.OnCommunityNoteInsertRequestedAsync(req.XmlIndex, req.NoteText, req.Resp);
            };

            _readableView.CommunityNoteDeleteRequested += async (_, req) =>
            {
                await _vm.OnCommunityNoteDeleteRequestedAsync(req.XmlStart, req.XmlEndExclusive);
            };

            _readableView.FootnoteMoveRequested += async (_, req) =>
            {
                await _vm.OnFootnoteMoveRequestedAsync(req);
            };

            _readableView.TagApplied += async (_, tag) =>
            {
                await _vm.OnTagAppliedAsync(tag);
            };

            _readableView.TagEditorRequested += (_, _) =>
            {
                _ = OpenTagEditorWindowAsync();
            };

            _readableView.VocabularyChanged += async (_, vocab) =>
            {
                await _vm.SaveTagVocabularyAsync(vocab);
            };

            _readableView.CompareTagsRequested += (_, data) =>
            {
                OpenCompareTagsWindow(data);
            };

            _readableView.CompareTranslationsRequested += (_, _) =>
            {
                _ = OpenCompareTranslationsWindowAsync();
            };

            _readableView.StudyPanelContextChanged += async (_, ctx) =>
            {
                await _vm.RefreshReaderStudyPanelAsync(ctx);
            };

            _readableView.StudyPanelVisibilityChanged += (_, visible) =>
            {
                _vm.Config.EnableStudyPanel = visible;
                _ = _vm.SafeSaveConfigAsync();
                // Auto-collapse nav sidebar when study panel opens to give more reading space
                if (visible && _navPanel != null)
                    _navPanel.IsVisible = false;
            };
        }

        if (_translationView != null)
        {
            _translationView.SaveRequested += async (_, _) => await _vm.SaveTranslatedFromTabAsync();
            _translationView.RevertRequested += async (_, _) => await _vm.RevertTranslatedXmlFromDiskAsync();
            _translationView.Status += (_, msg) => _vm.SetStatus(msg);

            _translationView.CurrentSegmentChanged += async (_, ev) =>
            {
                await _vm.RefreshAssistantForCurrentSegmentAsync(ev);
            };

            _translationView.ReviewActionRequested += async (_, status) =>
            {
                await _vm.HandleReviewActionAsync(status);
            };

            _translationView.BuildReferenceTmRequested += async (_, _) =>
            {
                await _vm.BuildReferenceTmAsync();
            };

            _translationView.ManageTermsRequested += async (_, _) =>
            {
                await _vm.OpenTermbaseEditorAsync();
            };

            _translationView.NextUnapprovedRequested += async (_, _) =>
            {
                await _vm.HandleNextUnapprovedAsync();
            };

            _translationView.ModeChanged += (_, mode) =>
            {
                _vm.HandleModeChanged(mode);
            };

            _translationView.NavigationRequested += (_, req) =>
            {
                _vm.HandleNavigationRequested(req);
            };

            _translationView.TranslationSourceChanged += async (_, idx) =>
            {
                await _vm.SwitchTranslationSourceAsync(idx);
            };

            _translationView.ResolveLbForBlock = blockNumber =>
            {
                var doc = _vm.IndexedDoc;
                if (doc == null) return null;
                var mode = _vm.TranslationMode;
                var wantedKind = mode switch
                {
                    TranslationEditMode.Head => CbetaTranslator.App.Services.TranslationUnitKind.Head,
                    TranslationEditMode.Notes => CbetaTranslator.App.Services.TranslationUnitKind.Note,
                    _ => CbetaTranslator.App.Services.TranslationUnitKind.Body
                };
                var unit = doc.Units
                    .Where(u => u.Kind == wantedKind)
                    .FirstOrDefault(u => u.Index == blockNumber);
                return CbetaTranslator.App.Services.TranslationUnit.GetLbNValueForUnit(unit);
            };
        }

        if (_searchView != null)
        {
            _searchView.Status += (_, msg) => _vm.SetStatus(msg);
            _searchView.NavigationRequested += (_, req) =>
            {
                _vm.HandleNavigationRequested(req);
            };
            _searchAddToScholarHandler = (_, passage) =>
            {
                _scholarView?.AddPassage(passage);
                _vm.SetStatus("Passage added to Scholar collection.");
            };
            _searchView.AddToScholarRequested += _searchAddToScholarHandler;
        }

        if (_gitView != null)
        {
            _gitStatusHandler = (_, msg) => _vm.SetStatus(msg);
            _gitView.Status += _gitStatusHandler;

            _gitView.GitHubAuthCompleted += async (_, args) =>
            {
                try { await _vm.HandleGitHubAuthCompletedAsync(args.Token, args.Login); }
                catch { }
            };

            _gitView.EnsureTranslatedForSelectedRequested += async relPath =>
            {
                try { return await _vm.EnsureTranslatedXmlForRelPathAsync(relPath, saveCurrentEditor: true); }
                catch (Exception ex) { _vm.SetStatus("Prepare translated XML failed: " + ex.Message); return false; }
            };

            _rootClonedHandler = async (_, repoRoot) =>
            {
                await _vm.HandleRootClonedAsync(repoRoot, IsSecondaryWindow);
                _tourService?.AdvanceIfWaitingFor("root-cloned");
            };
            _gitView.RootCloned += _rootClonedHandler;

            _communityDataFetchedHandler = async (_, _) =>
            {
                await _vm.RefreshReviewAggregationAsync();
            };
            _gitView.CommunityDataFetched += _communityDataFetchedHandler;
        }

        if (_scholarView != null)
        {
            _scholarStatusHandler = (_, msg) => _vm.SetStatus(msg);
            _scholarView.Status += _scholarStatusHandler;
            _scholarView.NavigationRequested += (_, req) =>
            {
                _vm.HandleNavigationRequested(req);
            };
            _scholarView.DictionaryRequested += async (_, _) =>
            {
                await _vm.OpenTermbaseEditorAsync();
            };

            // Reload scholar data when ANY window (including secondary) adds a passage
            if (!IsSecondaryWindow)
            {
                _scholarDataChangedHandler = (sender, _) =>
                {
                    // Only reload if the change came from a different view instance
                    if (sender != _scholarView && !string.IsNullOrWhiteSpace(_vm.Root))
                        _scholarView.SetRoot(_vm.Root);
                };
                ScholarTabView.ScholarDataChanged += _scholarDataChangedHandler;
            }
        }

        if (_readableView != null)
        {
            _readableAddToScholarHandler = (_, passage) =>
            {
                _scholarView?.AddPassage(passage);
                _vm.SetStatus("Passage added to Scholar collection.");
            };
            _readableView.AddToScholarRequested += _readableAddToScholarHandler;
        }

        if (_translationView != null)
        {
            _translationAddToScholarHandler = (_, passage) =>
            {
                _scholarView?.AddPassage(passage);
                _vm.SetStatus("Passage added to Scholar collection.");
            };
            _translationView.AddToScholarRequested += _translationAddToScholarHandler;
        }

        AddHandler(InputElement.KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
    }

    private void UnsubscribeChildViewEvents()
    {
        if (_readableView != null && _readableAddToScholarHandler != null)
            _readableView.AddToScholarRequested -= _readableAddToScholarHandler;

        if (_translationView != null && _translationAddToScholarHandler != null)
            _translationView.AddToScholarRequested -= _translationAddToScholarHandler;

        if (_searchView != null && _searchAddToScholarHandler != null)
            _searchView.AddToScholarRequested -= _searchAddToScholarHandler;

        if (_scholarView != null && _scholarStatusHandler != null)
            _scholarView.Status -= _scholarStatusHandler;

        if (_gitView != null)
        {
            if (_gitStatusHandler != null) _gitView.Status -= _gitStatusHandler;
            if (_rootClonedHandler != null) _gitView.RootCloned -= _rootClonedHandler;
            if (_communityDataFetchedHandler != null) _gitView.CommunityDataFetched -= _communityDataFetchedHandler;
        }

        RemoveHandler(InputElement.KeyDownEvent, OnWindowKeyDown);
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        // Ctrl+D — open dictionary from any tab
        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.D)
        {
            e.Handled = true;
            _ = _vm.OpenTermbaseEditorAsync();
            return;
        }

        if (e.KeyModifiers != KeyModifiers.Alt) return;
        if (_tabs?.SelectedIndex != 1) return; // only active on Translation tab

        if (e.Key == Key.A)
        {
            e.Handled = true;
            _ = _vm.HandleReviewActionAsync(TranslationReviewStatuses.Approved);
        }
        // Alt+N (needs-work) removed — button hidden, shortcut disabled
        else if (e.Key == Key.Right && !(_translationView?.IsEditorFocused() ?? false))
        {
            e.Handled = true;
            _translationView?.JumpToNextBlock();
        }
        else if (e.Key == Key.Left && !(_translationView?.IsEditorFocused() ?? false))
        {
            e.Handled = true;
            _translationView?.JumpToPreviousBlock();
        }
    }

    private async void FilesList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressNavSelectionChanged) return;
        if (_filesList?.SelectedItem is not FileNavItem item) return;
        await _vm.OnFileSelectedAsync(item);
    }

    // ===========================================================
    // Nav filter scheduling / debounce
    // ===========================================================

    private void ScheduleApplyFilter(bool debounce)
    {
        if (!debounce)
        {
            _ = _vm.ApplyFilterSafeAsync();
            return;
        }

        _navFilterDebounce ??= new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(220)
        };

        _navFilterDebounce.Tick -= NavFilterDebounce_Tick;
        _navFilterDebounce.Tick += NavFilterDebounce_Tick;

        _navFilterDebounce.Stop();
        _navFilterDebounce.Start();
    }

    private void NavFilterDebounce_Tick(object? sender, EventArgs e)
    {
        _navFilterDebounce?.Stop();
        _ = _vm.ApplyFilterSafeAsync();
    }

    // ===========================================================
    // Index cache save debounce
    // ===========================================================

    private void ScheduleIndexCacheSave()
    {
        _indexCacheSaveDebounce ??= new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        _indexCacheSaveDebounce.Tick -= IndexCacheSaveDebounce_Tick;
        _indexCacheSaveDebounce.Tick += IndexCacheSaveDebounce_Tick;

        _indexCacheSaveDebounce.Stop();
        _indexCacheSaveDebounce.Start();
    }

    private async void IndexCacheSaveDebounce_Tick(object? sender, EventArgs e)
    {
        _indexCacheSaveDebounce?.Stop();
        await _vm.SaveIndexCacheIfDirtyAsync();
    }

    // ===========================================================
    // Dirty timer
    // ===========================================================

    private void StartDirtyTimer()
    {
        _dirtyTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _dirtyTimer.Tick -= DirtyTimer_Tick;
        _dirtyTimer.Tick += DirtyTimer_Tick;
        _dirtyTimer.Start();
    }

    private void DirtyTimer_Tick(object? sender, EventArgs e)
    {
        _vm.CheckDirtyTick();
    }

    // ===========================================================
    // Tab helpers
    // ===========================================================

    private void ForceTab(int idx)
    {
        if (_tabs == null) return;
        _suppressTabEvents = true;
        try { _tabs.SelectedIndex = idx; }
        finally { _suppressTabEvents = false; }
    }

    // ===========================================================
    // Dialogs
    // ===========================================================

    private async Task<string?> ShowFolderPickerDialogAsync()
    {
        if (StorageProvider is null) { _vm.SetStatus("StorageProvider not available."); return null; }

        var picked = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select CBETA root folder"
        });

        var folder = picked.FirstOrDefault();
        return folder?.Path.LocalPath;
    }

    private async Task<AppConfig?> ShowSettingsDialogAsync(AppConfig current)
    {
        var settingsWindow = new SettingsWindow(current);
        return await settingsWindow.ShowDialog<AppConfig?>(this);
    }

    private async Task<string?> ShowUsernamePromptDialogAsync()
    {
        var prompt = new UsernamePromptWindow();
        return await prompt.ShowDialog<string?>(this);
    }

    private async Task ShowLicensesDialogAsync(string? root)
    {
        await new LicensesWindow(root).ShowDialog(this);
    }

    private async Task<bool> ShowYesNoAsync(string title, string message)
    {
        var btnYes = new Button { Content = "Yes", MinWidth = 90 };
        var btnNo = new Button { Content = "No", MinWidth = 90 };

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 10 };
        buttons.Children.Add(btnNo);
        buttons.Children.Add(btnYes);

        var text = new TextBox { Text = message, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Height = 200 };
        ScrollViewer.SetVerticalScrollBarVisibility(text, ScrollBarVisibility.Auto);

        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 10 };
        panel.Children.Add(text);
        panel.Children.Add(buttons);

        var win = new Window
        {
            Title = title,
            Width = 620,
            Height = 360,
            Content = panel,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Topmost = false
        };
        win.RequestedThemeVariant = this.ActualThemeVariant;

        var tcs = new TaskCompletionSource<bool>();
        btnYes.Click += (_, _) => { win.Close(); tcs.TrySetResult(true); };
        btnNo.Click += (_, _) => { win.Close(); tcs.TrySetResult(false); };

        await win.ShowDialog(this);
        return await tcs.Task;
    }

    // ===========================================================
    // Termbase editor window
    // ===========================================================

    private async Task OpenTermbaseEditorWindowAsync(string root, string? username = null)
    {
        try
        {
            if (_termbaseEditorWindow != null)
            {
                _termbaseEditorWindow.Activate();
                return;
            }

            var path = Path.Combine(root, "termbase.json");

            if (!File.Exists(path))
            {
                var starterJson =
@"[
  {
    ""sourceTerm"": ""\u548c\u5c1a"",
    ""preferredTarget"": ""the master"",
    ""alternateTargets"": [""Venerable""],
    ""status"": ""preferred"",
    ""note"": ""do not leave as Chinese in EN""
  }
]";
                await File.WriteAllTextAsync(path, starterJson, new UTF8Encoding(false));
            }

            var win = new TermbaseEditorWindow(root, username)
            {
                RequestedThemeVariant = this.ActualThemeVariant
            };

            win.TermsSaved += (_, _) =>
            {
                _vm.HandleTermsSaved();
                _scholarView?.InvalidateTermbaseCache();
            };
            win.CorpusNavigationRequested += (_, req) => _vm.HandleNavigationRequested(req);
            win.AddToScholarRequested += (_, hit) =>
            {
                var passage = new ScholarPassage
                {
                    ZhText = hit.ZhSnippet,
                    SourceRelPath = hit.SourceRelPath,
                };
                _scholarView?.AddPassage(passage);
                _vm.SetStatus("Corpus hit added to Scholar collection.");
            };
            win.Closed += (_, _) => _termbaseEditorWindow = null;

            _termbaseEditorWindow = win;
            win.Show();
        }
        catch (Exception ex)
        {
            _vm.SetStatus("Open dictionary failed: " + ex.Message);
        }
    }

    // ===========================================================
    // Tag editor window
    // ===========================================================

    private async Task OpenTagEditorWindowAsync()
    {
        try
        {
            if (_tagEditorWindow != null)
            {
                _tagEditorWindow.Activate();
                return;
            }

            var root = _vm.Root;
            if (string.IsNullOrEmpty(root)) return;

            var win = new TagEditorWindow(root, _vm.Username)
            {
                RequestedThemeVariant = this.ActualThemeVariant
            };

            win.VocabularySaved += async (_, _) =>
            {
                await _vm.ReloadTagVocabularyAsync();
            };
            win.Closed += (_, _) => _tagEditorWindow = null;

            _tagEditorWindow = win;
            win.Show();
        }
        catch (Exception ex)
        {
            _vm.SetStatus("Open tag editor failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Opens a non-modal 3-pane window comparing the current user's tags with another user's tags.
    /// </summary>
    private void OpenCompareTagsWindow(CompareTagsRequestData data)
    {
        try
        {
            var win = new CompareTagsWindow
            {
                RequestedThemeVariant = this.ActualThemeVariant
            };
            win.LoadComparison(
                data.Title,
                data.Doc,
                data.MyUsername,
                data.MyTags,
                data.MyVocab,
                data.OtherUsername,
                data.OtherTags,
                data.OtherVocab);
            win.Show(this);
        }
        catch (Exception ex)
        {
            _vm.SetStatus("Open tag comparison failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Shows a picker dialog for two translation sources, then opens a 3-pane comparison window
    /// with the original Chinese text and both selected translations.
    /// </summary>
    private async Task OpenCompareTranslationsWindowAsync()
    {
        try
        {
            var sources = _vm.GetTranslationSourceLabels();
            if (sources.Count < 2)
            {
                _vm.SetStatus("Need at least 2 translation sources to compare.");
                return;
            }

            if (_vm.CurrentRelPath == null)
            {
                _vm.SetStatus("No file loaded.");
                return;
            }

            // Build picker dialog with two ComboBoxes
            var dialog = new Window
            {
                Title = "Select Translations to Compare",
                Width = 400,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                RequestedThemeVariant = this.ActualThemeVariant
            };

            var sourceList = new List<string>(sources);

            var cmbA = new ComboBox { ItemsSource = sourceList, SelectedIndex = 0, MinWidth = 300, Margin = new Thickness(0, 4, 0, 0) };
            var cmbB = new ComboBox { ItemsSource = sourceList, SelectedIndex = Math.Min(1, sourceList.Count - 1), MinWidth = 300, Margin = new Thickness(0, 4, 0, 0) };

            var btnOk = new Button { Content = "Compare", HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 16, 0, 0), Padding = new Thickness(20, 6) };

            var panel = new StackPanel
            {
                Margin = new Thickness(20),
                Children =
                {
                    new TextBlock { Text = "Translation A:", FontWeight = FontWeight.Bold },
                    cmbA,
                    new TextBlock { Text = "Translation B:", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 12, 0, 0) },
                    cmbB,
                    btnOk
                }
            };

            dialog.Content = panel;

            var tcs = new TaskCompletionSource<(int, int)?>();
            btnOk.Click += (_, _) =>
            {
                tcs.TrySetResult((cmbA.SelectedIndex, cmbB.SelectedIndex));
                dialog.Close();
            };
            dialog.Closed += (_, _) => tcs.TrySetResult(null);

            dialog.Show(this);
            var result = await tcs.Task;

            if (result == null) return;
            var (indexA, indexB) = result.Value;

            if (indexA == indexB)
            {
                _vm.SetStatus("Please select two different translation sources.");
                return;
            }

            // Render original
            var origDir = _vm.OriginalDir;
            if (origDir == null) return;
            var origPath = Path.Combine(origDir, _vm.CurrentRelPath);
            if (!File.Exists(origPath))
            {
                _vm.SetStatus("Original file not found.");
                return;
            }
            var origXml = await File.ReadAllTextAsync(origPath, Encoding.UTF8);
            var origDoc = CbetaTeiRenderer.Render(origXml);

            // Render translation A
            var transADoc = _vm.RenderTranslationSource(indexA);
            if (transADoc == null || transADoc.IsEmpty)
            {
                _vm.SetStatus($"Translation A ({sourceList[indexA]}) is empty or not found for this file.");
                return;
            }

            // Render translation B
            var transBDoc = _vm.RenderTranslationSource(indexB);
            if (transBDoc == null || transBDoc.IsEmpty)
            {
                _vm.SetStatus($"Translation B ({sourceList[indexB]}) is empty or not found for this file.");
                return;
            }

            var data = new CompareTranslationsRequestData(
                _vm.CurrentRelPath,
                origDoc,
                transADoc,
                sourceList[indexA],
                transBDoc,
                sourceList[indexB]);

            var win = new CompareTranslationsWindow
            {
                RequestedThemeVariant = this.ActualThemeVariant
            };
            win.LoadComparison(data);
            win.Show(this);
        }
        catch (Exception ex)
        {
            _vm.SetStatus("Open translation comparison failed: " + ex.Message);
        }
    }

    // ===========================================================
    // Theme
    // ===========================================================

    private static readonly string[] ThemeTokens =
    {
        "AppBg","BarBg","NavBg","TextFg","TextMutedFg","ControlBg","BorderBrush",
        "BtnBg","BtnBgHover","BtnBgPressed","BtnFg",
        "TabBg","TabBgSelected","TabFgSelected","TooltipBg","TooltipBorder","TooltipFg",
        "SelectionBg","SelectionFg",
        "ControlBgHover","ControlBgFocus","TabBgHover","TabFg",
        "ComboBg","ComboBgHover","ComboBorder","ComboBorderHover",
        "CheckBorder","CheckBorderHover",
        "MenuBg","MenuItemHoverBg",
        "XmlViewerBg","XmlViewerBorder",
        "NavStatusGreenBg","NavStatusYellowBg","NavStatusRedBg",
        "SearchMatchFg","NoteMarkerCommunityFg",
        "PanelBg","PanelBorder","PanelAltBg",
        "ErrorFg","WarningFg","SuccessFg",
        "ErrorBg","WarningBg","SuccessBg",
        "AccentLinkFg","TermbaseHighlightBg"
    };

    private void ApplyTheme(bool dark)
    {
        var variant = dark ? ThemeVariant.Dark : ThemeVariant.Light;

        RequestedThemeVariant = variant;
        if (Application.Current != null)
            Application.Current.RequestedThemeVariant = variant;

        var res = Application.Current?.Resources;
        if (res == null) return;

        string prefix = dark ? "Night_" : "Light_";

        foreach (var token in ThemeTokens)
        {
            var sourceKey = prefix + token;

            if (!res.TryGetValue(sourceKey, out var sourceObj) || sourceObj is null)
                continue;

            if (res.TryGetValue(token, out var activeObj) &&
                activeObj is SolidColorBrush activeBrush &&
                sourceObj is SolidColorBrush sourceBrush)
            {
                activeBrush.Color = sourceBrush.Color;
                activeBrush.Opacity = sourceBrush.Opacity;
                continue;
            }

            res[token] = sourceObj;
        }

        RefreshNavListVisuals();
    }

    private void RefreshNavListVisuals()
    {
        try
        {
            if (_filesList == null) return;

            var selected = _filesList.SelectedItem;
            var src = _filesList.ItemsSource;

            _filesList.ItemsSource = null;
            _filesList.ItemsSource = src;
            _filesList.SelectedItem = selected;
        }
        catch { }
    }

    // ===========================================================
    // Onboarding Tour
    // ===========================================================

    private void StartTour()
    {
        if (_tourService == null) return;

        _tourService.Start();
    }

    private Control? FindControlDeep(string name)
    {
        return this.FindControl<Control>(name)
            ?? _readableView?.FindControl<Control>(name)
            ?? _translationView?.FindControl<Control>(name)
            ?? _searchView?.FindControl<Control>(name)
            ?? _gitView?.FindControl<Control>(name)
            ?? _scholarView?.FindControl<Control>(name);
    }

    private void ShowTourStep(Models.TourStep step)
    {
        if (_tourOverlayCanvas == null || _tourSpotlight == null || _tourTooltip == null || _tourService == null)
            return;

        // Switch tab if step requires it
        if (step.SwitchToTabIndex.HasValue)
            ForceTab(step.SwitchToTabIndex.Value);

        _tourOverlayCanvas.IsVisible = true;

        // Make spotlight fill the entire overlay canvas
        _tourSpotlight.Width = Bounds.Width;
        _tourSpotlight.Height = Bounds.Height;
        Canvas.SetLeft(_tourSpotlight, 0);
        Canvas.SetTop(_tourSpotlight, 0);

        // Find target control bounds
        Rect? targetBounds = null;
        if (!string.IsNullOrEmpty(step.TargetControlName))
        {
            var target = FindControlDeep(step.TargetControlName);
            if (target != null && target.IsVisible)
            {
                var pt = target.TranslatePoint(new Point(0, 0), this);
                if (pt.HasValue)
                    targetBounds = new Rect(pt.Value, target.Bounds.Size);
            }
        }

        _tourSpotlight.TargetBounds = targetBounds;

        // Update tooltip content
        _tourTooltip.Update(
            step.Title,
            step.Body,
            _tourService.CurrentIndex,
            _tourService.Steps.Count,
            canGoBack: _tourService.CurrentIndex > 0);

        // Position tooltip
        PositionTooltip(step, targetBounds);

        // Handle Wait steps: trigger async actions
        if (step.Type == Models.TourStepType.Wait)
            _ = HandleWaitStepAsync(step);
    }

    private void PositionTooltip(Models.TourStep step, Rect? targetBounds)
    {
        if (_tourTooltip == null) return;

        // Use ClientSize (visible area, excludes title bar) for accurate positioning
        double windowWidth = ClientSize.Width;
        double windowHeight = ClientSize.Height;

        // Limit tooltip height to 40% of window to prevent overflow on small screens
        double maxTooltipHeight = Math.Max(150, windowHeight * 0.4);
        _tourTooltip.MaxHeight = maxTooltipHeight;
        _tourTooltip.Measure(new Size(Math.Min(400, windowWidth - 32), maxTooltipHeight));
        var tooltipSize = _tourTooltip.DesiredSize;

        double left, top;
        const double pad = 12;

        if (step.Placement == Models.TourPlacement.Center || targetBounds == null)
        {
            left = (windowWidth - tooltipSize.Width) / 2;
            top = (windowHeight - tooltipSize.Height) / 2;
        }
        else
        {
            var tb = targetBounds.Value;

            switch (step.Placement)
            {
                case Models.TourPlacement.Bottom:
                    left = tb.X + (tb.Width - tooltipSize.Width) / 2;
                    top = tb.Bottom + pad;
                    // If it would go off bottom, flip to top
                    if (top + tooltipSize.Height > windowHeight - pad)
                        top = tb.Y - tooltipSize.Height - pad;
                    break;
                case Models.TourPlacement.Top:
                    left = tb.X + (tb.Width - tooltipSize.Width) / 2;
                    top = tb.Y - tooltipSize.Height - pad;
                    // If it would go off top, flip to bottom
                    if (top < pad)
                        top = tb.Bottom + pad;
                    break;
                case Models.TourPlacement.Right:
                    left = tb.Right + pad;
                    top = tb.Y + (tb.Height - tooltipSize.Height) / 2;
                    // If it would go off right, flip to left
                    if (left + tooltipSize.Width > windowWidth - pad)
                        left = tb.X - tooltipSize.Width - pad;
                    break;
                case Models.TourPlacement.Left:
                    left = tb.X - tooltipSize.Width - pad;
                    top = tb.Y + (tb.Height - tooltipSize.Height) / 2;
                    // If it would go off left, flip to right
                    if (left < pad)
                        left = tb.Right + pad;
                    break;
                default:
                    left = (windowWidth - tooltipSize.Width) / 2;
                    top = (windowHeight - tooltipSize.Height) / 2;
                    break;
            }
        }

        // Final clamp: ensure tooltip is always fully within visible window area
        left = Math.Max(pad, Math.Min(left, windowWidth - tooltipSize.Width - pad));
        top = Math.Max(pad, Math.Min(top, windowHeight - tooltipSize.Height - pad));

        Canvas.SetLeft(_tourTooltip, left);
        Canvas.SetTop(_tourTooltip, top);
    }

    private async Task HandleWaitStepAsync(Models.TourStep step)
    {
        if (step.WaitForEvent == "git-check-complete")
        {
            // Check git availability in background, then auto-advance
            await Task.Run(() =>
            {
                try { GitBinaryLocator.ResolveGitExecutablePath(); }
                catch { }
            });
            await Dispatcher.UIThread.InvokeAsync(() =>
                _tourService?.AdvanceIfWaitingFor("git-check-complete"));
        }
    }

    private async Task OnTourFinished()
    {
        if (_tourOverlayCanvas != null)
            _tourOverlayCanvas.IsVisible = false;

        _vm.Config.HasCompletedOnboarding = true;
        await _vm.SafeSaveConfigAsync();
    }

    /// <summary>
    /// Called after config is loaded and username prompt is done,
    /// to check whether the onboarding tour should start.
    /// </summary>
    public void MaybeStartTour()
    {
        if (_vm.Config.HasCompletedOnboarding) return;
        if (IsSecondaryWindow) return;

        // Don't start tour if launched via deep link — the user wants to go somewhere specific
        var hasDeepLink = App.StartupArgs?.Any(a =>
            a.StartsWith("zen://", StringComparison.OrdinalIgnoreCase)) == true;
        if (hasDeepLink) return;

        // If root already loaded (returning user who requested tour restart),
        // skip setup steps and go straight to feature walkthrough
        if (!string.IsNullOrWhiteSpace(_vm.Root))
        {
            _tourService?.Start(startIndex: 5); // Skip to "sidebar" step
            if (_tourService?.IsActive == true)
            {
                _tourOverlayCanvas.IsVisible = true;
                _emptyStateOverlay.IsVisible = false;
            }
            return;
        }

        StartTour();
    }
}
