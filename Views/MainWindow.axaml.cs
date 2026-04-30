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
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.Text;
using ReadZen.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadZen.App.Views;

public partial class MainWindow : Window
{
    // UI controls
    private Button? _btnToggleNav, _btnToggleTopBar, _btnOpenRoot, _btnSettings, _btnSave, _btnLicenses;
    private Button? _btnMinimize, _btnMaximize, _btnClose;
    private Border? _navPanel, _topBar, _emptyStateOverlay;
    private Button? _btnCorpusBadge;
    private Border? _corpusBadge;
    private TextBlock? _txtCorpusBadge;
    private StackPanel? _corpusSwitcherPanel;
    // Top-bar license chip — global per-file license display, sits next to
    // the corpus badge. The flyout content (LicenseDetailsView) is wider
    // than the old in-Reader version so attribution text doesn't need to
    // scroll horizontally.
    private Button? _btnLicenseChipTopBar;
    private Border? _licenseChipBorderTopBar;
    private TextBlock? _txtLicenseChipTopBar;
    private LicenseDetailsView? _licenseDetailsTopBar;
    private bool _navAutoHiddenByStudyPanel;
    private bool _navAutoHiddenByTab;

    private ListBox? _filesList;
    private TextBox? _navSearch;
    private CheckBox? _chkShowFilenames, _chkZenOnly;
    private ComboBox? _cmbStatusFilter;

    private TextBlock? _txtCurrentFile, _txtStatus;

    private TabStrip? _tabs;
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

    // Command palette
    private Border? _commandPaletteOverlay;
    private TextBox? _commandPaletteInput;
    private ListBox? _commandPaletteList;
    private List<(string Label, Action Execute)> _allCommands = new();

    // Termbase editor (non-modal -- at most one instance per main window)
    private TermbaseEditorWindow? _termbaseEditorWindow;

    // Tag editor (non-modal -- at most one instance per main window)
    private TagEditorWindow? _tagEditorWindow;

    // Zen Master manager (non-modal -- at most one instance per main window)
    private ZenMasterManagerWindow? _zenMasterManagerWindow;

    // Tour overlay controls
    private Canvas? _tourOverlayCanvas;
    private TourSpotlightOverlay? _tourSpotlight;
    private TourTooltipPanel? _tourTooltip;
    private OnboardingTourService? _tourService;
    private bool _tourDownloadInProgress;
    private string? _tourSampleCollectionId;

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
        if (isSecondaryWindow) _vm.SuppressConfigSavesForSecondaryWindow();
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

            // Persist window position/size/maximized state (primary window only)
            if (!IsSecondaryWindow)
            {
                try
                {
                    var cfg = _vm.Config;
                    cfg.IsMaximized = WindowState == WindowState.Maximized;
                    if (WindowState == WindowState.Normal)
                    {
                        cfg.WindowX = Position.X;
                        cfg.WindowY = Position.Y;
                        cfg.WindowWidth = Width;
                        cfg.WindowHeight = Height;
                    }
                    await _vm.SafeSaveConfigAsync();
                }
                catch { /* non-critical */ }
            }

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
    /// Shows the green "update available" banner. On Velopack-packaged installs
    /// (Setup.exe / AppImage / .pkg) the Download button performs an in-app update
    /// and restart; on plain zip-extract installs it falls back to opening the
    /// GitHub releases page in the user's default browser.
    /// </summary>
    private bool _updateNotificationWired;

    public void ShowUpdateNotification(Services.AppUpdateCheckResult result, Services.AppUpdateService updater)
    {
        if (_updateNotificationWired) return; // prevent double-wiring if called again
        _updateNotificationWired = true;

        var bar = Find<Border>("UpdateBar");
        var msg = Find<TextBlock>("TxtUpdateMessage");
        var download = Find<Button>("BtnDownloadUpdate");
        var dismiss = Find<Button>("BtnDismissUpdate");

        if (bar == null || msg == null) return;

        msg.Text = $"ReadZen v{result.AvailableVersion} is available";
        bar.IsVisible = true;

        if (download != null)
        {
            // Relabel the button when in-app install is available so the user
            // knows exactly what will happen.
            if (result.CanInstallInApp && download.Content is string)
                download.Content = "Install & Restart";

            download.Click += async (_, _) =>
            {
                try
                {
                    if (result.CanInstallInApp)
                    {
                        msg.Text = $"Downloading ReadZen v{result.AvailableVersion}\u2026";
                        download.IsEnabled = false;
                        var ok = await updater.TryInstallAndRestartAsync(result);
                        if (!ok)
                        {
                            // Velopack failed (stall / missing release / etc.) —
                            // fall back to opening the release page. Avalonia
                            // issue #146 mitigation per the run spec.
                            msg.Text = $"In-app update failed. Opening release page\u2026";
                            OpenInBrowser(result.ReleaseUrl);
                            download.IsEnabled = true;
                            download.Content = "Open Releases";
                        }
                        // On success the process restarts, so no UI update needed.
                    }
                    else
                    {
                        OpenInBrowser(result.ReleaseUrl);
                    }
                }
                catch { /* never crash the main window from an update click */ }
            };
        }

        if (dismiss != null)
        {
            dismiss.Click += (_, _) => bar.IsVisible = false;
        }
    }

    private static void OpenInBrowser(string url)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    public async Task HandleDeepLinkAsync(DeepLinkRequest request)
{
    await _windowReady.Task;

    switch (request.Kind)
    {
        case DeepLinkKind.Dictionary:
            await HandleDictDeepLinkAsync(request.DictTerm, null, isLegacyDictionaryAlias: true);
            break;
        case DeepLinkKind.Scholar:
            await HandleScholarDeepLinkAsync(request.ScholarCollectionId, request.ScholarPassageId, request.ScholarUser);
            break;
        case DeepLinkKind.ScholarGraph:
            await HandleScholarGraphDeepLinkAsync(request.ScholarCollectionId, request.ScholarUser);
            break;
        case DeepLinkKind.Search:
            await HandleSearchDeepLinkAsync(request);
            break;
        case DeepLinkKind.Tags:
            await HandleTagsDeepLinkAsync(request.TagsRelPath, request.TagsUser, request.TagsTagId);
            break;
        case DeepLinkKind.Termbase:
            await HandleDictDeepLinkAsync(request.TermbaseEntry ?? request.DictTerm, request.TermbaseUser, isLegacyDictionaryAlias: false);
            break;
        case DeepLinkKind.Master:
            await HandleMasterDeepLinkAsync(request.MasterName, request.MasterUser);
            break;
        case DeepLinkKind.Compare:
            await HandleCompareDeepLinkAsync(request);
            break;
    }
}

private async Task HandleDictDeepLinkAsync(string? term, string? user, bool isLegacyDictionaryAlias)
{
    if (string.IsNullOrWhiteSpace(term))
    {
        _vm.SetStatus((isLegacyDictionaryAlias ? "Dictionary" : "Termbase") + " link: no term specified.", StatusSeverity.Warning);
        return;
    }

    await _vm.OpenTermbaseEditorAsync(term, user);
    _vm.SetStatus($"Opened dictionary for \"{term}\"" + (!string.IsNullOrWhiteSpace(user) ? $" (user: {user})" : "") + ".", StatusSeverity.Info);
}

private async Task HandleCompareDeepLinkAsync(DeepLinkRequest request)
{
    if (string.IsNullOrWhiteSpace(request.CompareRelPath) ||
        string.IsNullOrWhiteSpace(request.CompareSourceA) ||
        string.IsNullOrWhiteSpace(request.CompareSourceB) ||
        request.ComparePane == null ||
        request.CompareNavigation == null)
    {
        _vm.SetStatus("Compare link is incomplete.", StatusSeverity.Warning);
        return;
    }

    await OpenCompareTranslationsWindowAsync(
        request.CompareRelPath,
        request.CompareSourceA,
        request.CompareSourceB,
        request.ComparePane.Value,
        request.CompareNavigation);
}

private async Task HandleMasterDeepLinkAsync(string? name, string? user)
{
    if (string.IsNullOrWhiteSpace(name))
    {
        _vm.SetStatus("Master link: no name specified.", StatusSeverity.Warning);
        return;
    }

    await OpenZenMasterManagerWindowAsync(name, user);
}

private async Task HandleScholarDeepLinkAsync(string? collectionId, string? passageId, string? user)
{
    if (string.IsNullOrWhiteSpace(collectionId))
    {
        _vm.SetStatus("Scholar link: no collection specified.", StatusSeverity.Warning);
        return;
    }

    ForceTab(4);

    if (_scholarView != null)
    {
        var vm = (ScholarTabViewModel)_scholarView.DataContext!;
        bool found = await vm.TryNavigateToPassageAsync(collectionId, passageId, user);
        if (!found)
            _vm.SetStatus("This scholar passage isn't available. The person who shared this link may not have synced their data yet.", StatusSeverity.Warning);
    }
}

private async Task HandleScholarGraphDeepLinkAsync(string? collectionId, string? user)
{
    if (string.IsNullOrWhiteSpace(collectionId))
    {
        _vm.SetStatus("Graph link: no collection specified.", StatusSeverity.Warning);
        return;
    }

    ForceTab(4);

    if (_scholarView != null)
    {
        var vm = (ScholarTabViewModel)_scholarView.DataContext!;
        // Navigate to the collection first, then open graph
        await vm.TryNavigateToPassageAsync(collectionId, null, user);
        if (vm.SelectedCollection != null)
            _scholarView.OpenGraphForCurrentCollection();
        else
            _vm.SetStatus("Collection not found for graph link.", StatusSeverity.Warning);
    }
}

private async Task HandleSearchDeepLinkAsync(DeepLinkRequest request)
{
    if (string.IsNullOrWhiteSpace(request.SearchQuery))
    {
        _vm.SetStatus("Search link: no query specified.", StatusSeverity.Warning);
        return;
    }

    if (!string.IsNullOrWhiteSpace(request.SearchTranslationSource))
        await _vm.RestoreSearchTranslationSourceAsync(request.SearchTranslationSource);

    var uiState = new SearchTabViewModel.SearchUiState
    {
        Query = request.SearchQuery,
        SearchOriginal = request.SearchOriginal ?? true,
        SearchTranslated = request.SearchTranslated ?? false,
        ZenOnly = request.SearchZenOnly ?? false,
        SelectedStatusIndex = request.SearchStatusIndex ?? 0,
        SelectedContextIndex = request.SearchContextIndex ?? 2,
        SelectedTagFilterId = request.SearchTagId
    };

    ForceTab(2);
    if (_searchView != null)
        await _searchView.ApplyUiStateAsync(uiState, executeSearch: true);

    _vm.SetStatus($"Searching: \"{request.SearchQuery}\"");
}

private async Task HandleTagsDeepLinkAsync(string? relPath, string? user, string? tagId)
{
    if (string.IsNullOrWhiteSpace(relPath))
    {
        _vm.SetStatus("Tags link: no file specified.", StatusSeverity.Warning);
        return;
    }
    if (string.IsNullOrWhiteSpace(_vm.Root))
    {
        _vm.SetStatus("Tags link failed: no text root is loaded.", StatusSeverity.Warning);
        return;
    }

    await _vm.OpenAtCoreAsync(_vm.Root!, new NavigationRequest
    {
        RelPath = relPath,
        Side = SearchSide.Original
    });

    ForceTab(0);

    if (_readableView != null)
    {
        bool applied = await _readableView.ApplyTagDeepLinkAsync(user, tagId);
        if (applied)
        {
            _vm.SetStatus($"Opened tags for {relPath}" + (!string.IsNullOrWhiteSpace(user) ? $" (user: {user})" : "") + (!string.IsNullOrWhiteSpace(tagId) ? $", tag {tagId}" : "") + ".", StatusSeverity.Info);
            return;
        }
    }

    var suffix = new List<string>();
    if (!string.IsNullOrWhiteSpace(user)) suffix.Add($"user: {user}");
    if (!string.IsNullOrWhiteSpace(tagId)) suffix.Add($"tag: {tagId}");
    _vm.SetStatus($"Tags: opened {relPath}" + (suffix.Count > 0 ? $" ({string.Join(", ", suffix)})" : ""), StatusSeverity.Info);
}

private async Task LoadConfigAndAutoloadAsync()
    {
        try
        {
            await _vm.LoadConfigApplyThemeAndMaybeAutoloadAsync(IsSecondaryWindow);
            RestoreWindowState();
            MaybeStartTour();
        }
        finally
        {
            _windowReady.TrySetResult();

            // Close splash screen once the main window is ready
            if (!IsSecondaryWindow && App.SplashScreen is { } splash)
            {
                App.SplashScreen = null;
                splash.Close();
            }
        }
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Restores saved window position, size, and maximized state from config.
    /// Validates that the saved position is still on a visible screen.
    /// </summary>
    private void RestoreWindowState()
    {
        var cfg = _vm.Config;

        if (cfg.WindowWidth.HasValue && cfg.WindowHeight.HasValue
            && cfg.WindowWidth.Value > 100 && cfg.WindowHeight.Value > 100
            && cfg.WindowWidth.Value < 8000 && cfg.WindowHeight.Value < 8000)
        {
            Width = cfg.WindowWidth.Value;
            Height = cfg.WindowHeight.Value;
        }

        if (cfg.WindowX.HasValue && cfg.WindowY.HasValue)
        {
            var x = (int)cfg.WindowX.Value;
            var y = (int)cfg.WindowY.Value;

            // Verify that the saved position is at least partially on a visible screen
            bool onScreen = false;
            if (Screens?.All != null)
            {
                foreach (var screen in Screens.All)
                {
                    var wa = screen.WorkingArea;
                    // Consider the window on-screen if its top-left quadrant overlaps
                    // a working area by at least 50px in each direction
                    if (x + 50 < wa.X + wa.Width && x + (int)(Width * screen.Scaling) > wa.X + 50
                        && y + 50 < wa.Y + wa.Height && y + (int)(Height * screen.Scaling) > wa.Y + 50)
                    {
                        onScreen = true;
                        break;
                    }
                }
            }

            if (onScreen)
                Position = new Avalonia.PixelPoint(x, y);
            // else: fall back to OS default placement
        }

        if (cfg.IsMaximized)
            WindowState = WindowState.Maximized;
    }

    private T? Find<T>(string name) where T : Control => this.FindControl<T>(name);

    private void FindControls()
    {
        _btnToggleNav = Find<Button>("BtnToggleNav");
        _btnToggleTopBar = Find<Button>("BtnToggleTopBar");
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

        _txtCurrentFile = Find<TextBlock>("TxtCurrentFile");
        _txtStatus = Find<TextBlock>("TxtStatus");
        var supportLink = Find<TextBlock>("BtnSupportStatusBar");
        if (supportLink != null)
        {
            supportLink.PointerPressed += (_, _) =>
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://ko-fi.com/readzen") { UseShellExecute = true }); }
                catch { }
            };
        }
        _btnCorpusBadge = Find<Button>("BtnCorpusBadge");
        _corpusBadge = Find<Border>("CorpusBadge");
        _txtCorpusBadge = Find<TextBlock>("TxtCorpusBadge");
        _corpusSwitcherPanel = Find<StackPanel>("CorpusSwitcherPanel");
        _btnLicenseChipTopBar = Find<Button>("BtnLicenseChipTopBar");
        _licenseChipBorderTopBar = Find<Border>("LicenseChipBorderTopBar");
        _txtLicenseChipTopBar = Find<TextBlock>("TxtLicenseChipTopBar");
        _licenseDetailsTopBar = Find<LicenseDetailsView>("LicenseDetailsTopBar");

        _tabs = Find<TabStrip>("MainTabs");
        _readableView = Find<ReadableTabView>("ReadableView");
        _translationView = Find<TranslationTabView>("TranslationView");
        _searchView = Find<SearchTabView>("SearchView");
        _gitView = Find<GitTabView>("GitView");
        _scholarView = Find<ScholarTabView>("ScholarView");

        _tourOverlayCanvas = Find<Canvas>("TourOverlayCanvas");
        _tourSpotlight = Find<TourSpotlightOverlay>("TourSpotlight");
        _tourTooltip = Find<TourTooltipPanel>("TourTooltip");

        _commandPaletteOverlay = Find<Border>("CommandPaletteOverlay");
        _commandPaletteInput = Find<TextBox>("CommandPaletteInput");
        _commandPaletteList = Find<ListBox>("CommandPaletteList");
        SetupCommandPalette();
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
            sp.GetRequiredService<IDocumentTagService>(),
            sp.GetRequiredService<IGitRepoService>(),
            sp.GetRequiredService<ILicenseMetadataService>(),
            sp.GetRequiredService<IManifestService>());

        _vm.SetStarService(sp.GetRequiredService<ITranslationStarService>());

        DataContext = _vm;

        _tourService = sp.GetRequiredService<OnboardingTourService>();
    }

    private void WireBridges()
    {
        // StatusText -> TxtStatus (via property changed, or direct bridge)
        // Marshal to UI thread — background tasks (e.g. RefreshAllCachedStatusesAsync)
        // can fire property changes from worker threads.
        _vm.PropertyChanged += (_, e) =>
        {
            if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => HandleVmPropertyChanged(e));
                return;
            }
            HandleVmPropertyChanged(e);
        };

        // ReadableTabView bridges
        _vm.SetReadableRendered = (ro, rt) =>
        {
            _readableView?.SetRendered(ro, rt);
            // Push the active file's license to BOTH the reader (for the
            // right-click context menu) and the top-bar chip (for display).
            var license = _vm.GetLicenseForCurrentFile();
            _readableView?.SetFileLicense(license);
            UpdateLicenseChip(license);

            // Populate version picker with git history for the translated file
            _ = PopulateReaderVersionPickerAsync();
        };
        _vm.ClearReadable = () =>
        {
            _readableView?.Clear();
            _readableView?.ClearCorrectionTimeline();
            UpdateLicenseChip(null);
        };
        // Late-arriving license push from the VM (after the indexTask builds
        // the license metadata cache). Needed because SetReadableRendered can
        // fire before BuildIndex completes on cold load, at which point
        // GetLicenseForCurrentFile() still returns null.
        _vm.SetCurrentFileLicense = license =>
        {
            _readableView?.SetFileLicense(license);
            UpdateLicenseChip(license);
        };
        _vm.SetCurrentFileProvenance = (manifest, license, corpus, xmlAbsPath) =>
        {
            _readableView?.SetProvenance(manifest, license, corpus, xmlAbsPath);
            DiscoverAndSetCorrectionLog(xmlAbsPath);
        };
        _vm.SetReadableProvenancePanelVisible = visible =>
            _readableView?.SetProvenancePanelVisible(visible);
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
        _vm.UpdateReadableTmSharedHighlights = (approved, reference, zh, hint, anchor) =>
            _readableView?.UpdateTmSharedHighlights(approved, reference, zh, preferredOccurrenceHint: hint, anchorTextSignal: anchor);
        _vm.SetReadableDefaultResp = resp =>
        {
            if (_readableView != null) _readableView.DefaultResp = resp;
        };
        _vm.SetReadableTagCompareIdentity = username =>
        {
            if (_readableView != null) _readableView.CurrentTagCompareIdentity = username;
        };
        _vm.SetReadableTagUsername = username =>
        {
            if (_readableView != null) _readableView.CurrentTagUsername = username;
        };
        _vm.SetReadableStudySnapshot = snapshot => _readableView?.SetStudyPanelSnapshot(snapshot);
        _vm.AppendReaderConcordance = hits =>
        {
            var host = _readableView?.FindControl<StackPanel>("StudyTmHost");
            AssistantPanelRenderer.RenderConcordance(hits, host,
                brushResolver: key => _readableView?.TryFindResource(key, out var obj) == true && obj is IBrush b ? b : null,
                navigationHandler: (_, req) => _vm.HandleNavigationRequested(req));
        };
        _vm.SetReadableStudyPanelVisible = visible => _readableView?.SetStudyPanelVisible(visible);

        // Wire zen master lookup for study panel bio section
        if (_readableView != null)
        {
            var masterDatesSvc = App.Services.GetRequiredService<IMasterDatesService>();
            var masterMgr = new ZenMasterManagerService(masterDatesSvc);
            ZenMasterCatalog? cachedCatalog = null;
            _readableView.FindMasterByName = zhText =>
            {
                // Lazy-load the catalog on first use (small dataset, fast)
                if (cachedCatalog == null && _vm.Root != null)
                {
                    try { cachedCatalog = masterMgr.LoadAsync(_vm.Root).GetAwaiter().GetResult(); }
                    catch { return null; }
                }
                return cachedCatalog != null ? masterMgr.FindMasterInText(cachedCatalog.Records, zhText) : null;
            };
        }
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
        _vm.SetAssistantLoading = isLoading =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _translationView?.SetAssistantLoading(isLoading);
                _readableView?.SetStudyLoading(isLoading);
                _scholarView?.SetScholarLoading(isLoading);
            });
        };
        _vm.SetAssistantSnapshot = snapshot => _translationView?.SetAssistantSnapshot(snapshot);
        _vm.AppendTranslateConcordance = hits =>
        {
            var host = _translationView?.FindControl<StackPanel>("ReferenceTmHost");
            AssistantPanelRenderer.RenderConcordance(hits, host,
                brushResolver: key => _translationView?.TryFindResource(key, out var obj) == true && obj is IBrush b ? b : null,
                navigationHandler: (_, req) => _vm.HandleNavigationRequested(req));
        };
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
        _vm.SetReadableTranslationSourceOptions = options => _readableView?.SetTranslationSourceOptions(options);
        _vm.SetTranslationSourceIndex = index => _translationView?.SetTranslationSourceIndex(index);
        _vm.SetReadableTranslationSourceIndex = index => _readableView?.SetTranslationSourceIndex(index);
        _vm.UpdateTranslationStarButton = isStarred => _translationView?.UpdateStarButton(isStarred);
        _vm.UpdateReadableStarButton = isStarred => _readableView?.UpdateStarButton(isStarred);
        _vm.SetTranslationEditorReadOnly = readOnly => _translationView?.SetEditorReadOnly(readOnly);
        _vm.SignalCoreLoadComplete = () => _windowReady.TrySetResult();

        // SearchTabView bridges
        _vm.SetSearchRootContext = (root, orig, tranDirs) => _searchView?.SetRootContext(root, orig, tranDirs);
        _vm.SetSearchZenResolver = resolver => _searchView?.SetZenResolver(resolver);
        _vm.SetSearchMasterCatalog = catalog =>
        {
            _searchView?.SetMasterCatalog(catalog);
            _searchView?.InitTypeahead(catalog, null);
        };
        _vm.SetSearchContext = (root, orig, tranDirs, meta, addOrig, addTrans) =>
            _searchView?.SetContext(root, orig, tranDirs, fileMeta: meta,
                additionalOriginalDirs: addOrig, additionalTranslatedDirs: addTrans);
        _vm.SetSearchFileIndex = items => _searchView?.SetFileIndex(items);
        _vm.ClearSearch = () => _searchView?.Clear();

        // GitTabView bridges
        _vm.SetGitRepoRoot = root => _gitView?.SetCurrentRepoRoot(root);
        _vm.SetGitSelectedRelPath = rel => _gitView?.SetSelectedRelPath(rel);
        _vm.SetGitUsername = user => _gitView?.SetUsername(user);
        _vm.LoadGitPersistedAuth = (token, login) => _gitView?.LoadPersistedAuth(token, login);
        // Seed the Git tab with the current corpus immediately so the very
        // first sync after launch dispatches to the right repo. The
        // PropertyChanged handler above keeps it in sync on later changes,
        // but it only fires on a state TRANSITION — if the persisted
        // ActiveCorpus equals the default, the handler never runs and the
        // Git tab would be stuck on its own default. Belt-and-braces.
        _gitView?.SetActiveCorpus(_vm.ActiveCorpus);

        // ScholarTabView bridges
        _vm.SetScholarRoot = root => _scholarView?.SetRoot(root);
        _vm.ClearScholar = () => _scholarView?.Clear();
        _vm.SetScholarUsername = user => _scholarView?.SetUsername(user);
        _vm.SetScholarAssistantUsername = user => _scholarView?.SetAssistantUsername(user);
        _vm.SetScholarTranslationDirs = (orig, tran) => _scholarView?.SetTranslationDirs(orig, tran);
        _vm.SetScholarDictionarySourceOptions = options => _scholarView?.SetDictionarySourceOptions(options);
        _vm.SetScholarDictionarySourceIndex = index => _scholarView?.SetDictionarySourceIndex(index);
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
            // 4C: restore persisted search history into the search view model
            if (config.SearchHistory.Count > 0)
                _searchView?.ViewModel.LoadHistory(config.SearchHistory);
            // 2E: apply preferred citation style from config
            CitationService.DefaultStyleIndex = config.PreferredCitationStyleIndex;
        };

        // Index cache save debounce
        _vm.ScheduleIndexCacheSave = ScheduleIndexCacheSave;

        // Termbase editor
        _vm.OpenTermbaseEditorRequested = (root, username, landingTerm, landingUser) => _ = OpenTermbaseEditorWindowAsync(root, username, landingTerm, landingUser);

        // Wire assistant title resolver
        _vm.SetAssistantTitleResolver?.Invoke(rel => _vm.ResolveAssistantTitle(rel));

        // Tour: auto-index complete
        _vm.OnAutoIndexCompleted = () => _tourService?.AdvanceIfWaitingFor("index-built");
    }

    private void HandleVmPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
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
            if (_emptyStateOverlay != null)
                _emptyStateOverlay.IsVisible = string.IsNullOrEmpty(_vm.RootDisplayText);
            UpdateCorpusBadge();

            // If texts just loaded (root became non-empty) and the tour is
            // still active, dismiss it — the user got texts via Open Folder
            // outside the tour flow. Mark onboarding done so it doesn't
            // restart on next launch.
            if (!string.IsNullOrEmpty(_vm.RootDisplayText) && _tourService is { IsActive: true })
            {
                _tourService.Complete();
            }
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.ActiveCorpus)
              || e.PropertyName == nameof(MainWindowViewModel.CorpusBadgeLabel)
              || e.PropertyName == nameof(MainWindowViewModel.AvailableCorpora))
        {
            UpdateCorpusBadge();
            // Keep the Git tab in sync with the active corpus so its share /
            // sync / PR pipeline operates against the matching translations
            // repo. Without this, switching to OpenZen leaves the Git tab
            // pointing at CBETA and personal translations silently fail to
            // ship (the "no auto-mergeable changes" failure mode).
            _gitView?.SetActiveCorpus(_vm.ActiveCorpus);
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.CurrentFileText))
        {
            if (_txtCurrentFile != null) _txtCurrentFile.Text = _vm.CurrentFileText;
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.WindowTitle))
        {
            Title = _vm.WindowTitle;
        }
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

        if (_btnToggleTopBar != null)
        {
            _btnToggleTopBar.Click += (_, _) => ToggleTopBarCommands();
            UpdateTopBarToggleState();
        }

        if (_btnOpenRoot != null) _btnOpenRoot.Click += async (_, _) =>
        {
            // Always allow Open Folder — it's the escape hatch if anything
            // goes wrong with the tour or text download. If the tour is active,
            // block other buttons but NOT this one.
            await _vm.OpenRootAsync();
        };
        if (_btnSettings != null) _btnSettings.Click += async (_, _) =>
        {
            if (BlockIfTourActive()) return;
            await _vm.OpenSettingsAsync();
        };
        if (_btnLicenses != null) _btnLicenses.Click += async (_, _) =>
        {
            if (BlockIfTourActive()) return;
            await _vm.OpenLicensesAsync();
        };

        var btnGetStarted = Find<Button>("BtnGetStarted");
        if (btnGetStarted != null)
            btnGetStarted.Click += (_, _) => StartTour();

        var btnOpenRootAlt = Find<Button>("BtnOpenRootAlt");
        if (btnOpenRootAlt != null)
            btnOpenRootAlt.Click += async (_, _) =>
            {
                // If the tour is active, funnel them into the tour flow.
                // If the tour is NOT active (e.g. it failed or was somehow dismissed),
                // this is the user's escape hatch — let them pick a folder directly.
                if (_tourService is { IsActive: true })
                    StartTour();
                else
                    await _vm.OpenRootAsync();
            };

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

                    var uri = ZenUriParser.BuildUri(navItem.RelPath);
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

                    var url = ZenUriParser.BuildShareableUrl(navItem.RelPath);
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
                if (_tourService is { IsActive: true, CurrentStep.SwitchToTabIndex: int expectedTab }
                    && _tabs.SelectedIndex != expectedTab)
                {
                    ForceTab(expectedTab);
                    return;
                }
                await _vm.OnTabSelectionChangedAsync();
                _vm.UpdateSaveButtonState();

                // Hide the file navigation panel on tabs that have their own navigation
                // (Scholar tab has its own tree panel; Masters tab is a launcher)
                if (_navPanel != null)
                {
                    bool hideNav = _tabs.SelectedIndex >= 4; // Scholar (4) and Masters (5)
                    if (hideNav && _navPanel.IsVisible)
                    {
                        _navPanel.IsVisible = false;
                        _navAutoHiddenByTab = true;
                    }
                    else if (!hideNav && _navAutoHiddenByTab)
                    {
                        _navPanel.IsVisible = true;
                        _navAutoHiddenByTab = false;
                    }
                }
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
            _tourTooltip.NextClicked += (_, _) =>
            {
                if (_setupPromptActive)
                {
                    // "Next" on the setup-complete prompt = start feature tour
                    _setupPromptActive = false;
                    _tourService?.StartFeatureTour();
                }
                else
                    _tourService?.Next();
            };
            _tourTooltip.BackClicked += (_, _) => _tourService?.Previous();
            _tourTooltip.SkipClicked += async (_, _) =>
            {
                if (_setupPromptActive)
                {
                    // "Skip Tour" on the setup-complete prompt = done, no tour
                    _setupPromptActive = false;
                    await OnTourFinished();
                }
                else
                    _tourService?.Skip();
            };
            _tourTooltip.ActionClicked += (_, _) => OnTourActionClicked();
            _tourTooltip.SkipWaitClicked += (_, _) =>
            {
                // SkipWait on the download step lets the user pick an existing folder
                // instead of downloading. Only advance if texts are actually loaded.
                if (_tourService?.CurrentStep?.Id == "download-texts" && string.IsNullOrWhiteSpace(_vm.Root))
                {
                    _vm.SetStatus("Choose a folder with texts first, or click the download button.");
                    return;
                }
                _tourService?.Next();
            };
        }

        // Tour service events
        if (_tourService != null)
        {
            _tourService.StepChanged += (_, step) => Dispatcher.UIThread.Post(() => ShowTourStep(step));
            _tourService.TourCompleted += async (_, _) => await Dispatcher.UIThread.InvokeAsync(OnTourFinished);
            _tourService.TourSkipped += async (_, _) => await Dispatcher.UIThread.InvokeAsync(OnTourFinished);
            _tourService.SetupPhaseCompleted += async (_, _) => await Dispatcher.UIThread.InvokeAsync(OnSetupPhaseCompleted);
        }

        // Recalculate tour spotlight on resize
        ((AvaloniaObject)this).PropertyChanged += (_, e) =>
        {
            if (e.Property == ClientSizeProperty && _tourService is { IsActive: true, CurrentStep: not null })
                ShowTourStep(_tourService.CurrentStep);
        };
    }

    private void MainTabs_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        e.Handled = true;
    }

    private void MainTabContent_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Prevent Carousel from interpreting wheel events as tab navigation.
        // Inner ScrollViewers handle their own scrolling; anything that bubbles
        // up to the Carousel should be swallowed, not used to switch tabs.
        e.Handled = true;
    }
    private void ToggleTopBarCommands()
    {
        var host = Find<Control>("TopBarCommandsHost");
        if (host == null)
            return;

        host.IsVisible = !host.IsVisible;
        UpdateTopBarToggleState();
    }

    private void UpdateTopBarToggleState()
    {
        if (_btnToggleTopBar == null)
            return;

        var host = Find<Control>("TopBarCommandsHost");
        bool expanded = host?.IsVisible != false;
        _btnToggleTopBar.Content = expanded ? "\u25B2" : "\u25BC";
        ToolTip.SetTip(_btnToggleTopBar, expanded ? "Collapse command bar" : "Expand command bar");
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
            _readableView.GetTranslationUser = () => _vm.GetActiveTranslationUser();
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
                // Enrich with community root and resolver username for consensus workflow
                var enriched = data with
                {
                    CommunityRoot = _vm.TranslationRoot ?? _vm.Root ?? "",
                    ResolverUsername = _vm.Config.Username ?? ""
                };
                OpenCompareTagsWindow(enriched);
            };

            _readableView.CompareTranslationsRequested += (_, _) =>
            {
                _ = OpenCompareTranslationsWindowAsync();
            };

            _readableView.CodeFrequencyRequested += (_, _) => _ = OpenCodeFrequencyWindowAsync();
            _readableView.CooccurrenceRequested += (_, _) => _ = OpenCodeCooccurrenceWindowAsync();
            _readableView.DocumentVariablesRequested += (_, _) => _ = OpenDocumentVariablesWindowAsync();
            _readableView.QueryBuilderRequested += (_, _) => _ = OpenQueryBuilderWindowAsync();
            _readableView.ExportQdpxRequested += (_, _) => _ = ExportQdpxAsync();

            _readableView.TranslationSourceChanged += async (_, idx) =>
            {
                await _vm.SwitchTranslationSourceAsync(idx);
            };

            _readableView.StarToggleRequested += async (_, _) =>
            {
                await _vm.ToggleStarAsync();
            };

            _readableView.VersionPickerChanged += async (_, commitHash) =>
            {
                await LoadReaderHistoricalVersionAsync(commitHash);
            };

            _readableView.EditionDetailsRequested += async (manifest, xmlPath) =>
            {
                await OpenEditionProcessDialogAsync(manifest, xmlPath);
            };

            _readableView.NavigationRequested += (_, req) =>
            {
                _vm.HandleNavigationRequested(req);
            };

            _readableView.OpenMasterRequested += async (_, masterName) =>
            {
                await OpenZenMasterManagerWindowAsync(masterName);
            };

            _readableView.StudyPanelContextChanged += async (_, ctx) =>
            {
                await _vm.RefreshReaderStudyPanelAsync(ctx);
            };
            _readableView.DictionaryRequested += async (_, _) =>
            {
                await _vm.OpenTermbaseEditorAsync();
            };

            // 4H: Jump to Search tab and run the selected text as a corpus query.
            _readableView.SearchCorpusRequested += (_, query) =>
            {
                ForceTab(2);
                _searchView?.FocusQueryBox();
                _searchView?.SetQueryAndSearch(query);
            };

            _readableView.StudyPanelVisibilityChanged += (_, visible) =>
            {
                _vm.Config.EnableStudyPanel = visible;
                _ = _vm.SafeSaveConfigAsync();

                if (_navPanel == null)
                    return;

                if (visible)
                {
                    if (_navPanel.IsVisible)
                    {
                        _navAutoHiddenByStudyPanel = true;
                        _navPanel.IsVisible = false;
                    }
                }
                else if (_navAutoHiddenByStudyPanel)
                {
                    _navPanel.IsVisible = true;
                    _navAutoHiddenByStudyPanel = false;
                }
            };

            _readableView.ProvenancePanelVisibilityChanged += (_, visible) =>
            {
                _vm.Config.EnableProvenancePanel = visible;
                _ = _vm.SafeSaveConfigAsync();
            };

            _readableView.FontSizeChanged += (_, size) =>
            {
                _vm.Config.EditorFontSize = size;
                _ = _vm.SafeSaveConfigAsync();
            };

            // Apply persisted font size
            _readableView.SetEditorFontSize(_vm.Config.EditorFontSize);
        }

        if (_translationView != null)
        {
            _translationView.GetTranslationUser = () => _vm.GetActiveTranslationUser();
            _translationView.SaveRequested += async (_, _) =>
            {
                await _vm.SaveTranslatedFromTabAsync();
                await PromptLicenseIfNeededAsync();
            };
            _translationView.FreshStartRequested += async (_, _) => await _vm.ResetTranslatedToUntranslatedAsync();
            _translationView.RevertRequested += async (_, _) => await _vm.RevertTranslatedXmlFromDiskAsync();
            _translationView.HistoryRequested += async (_, _) => await OpenTranslationHistoryAsync();

            // Wire zen master lookup for AI prompt enrichment
            {
                var masterDatesSvc2 = App.Services.GetRequiredService<IMasterDatesService>();
                var masterMgr2 = new ZenMasterManagerService(masterDatesSvc2);
                ZenMasterCatalog? masterCatalog2 = null;
                _translationView.FindMastersInText = text =>
                {
                    if (masterCatalog2 == null && _vm.Root != null)
                    {
                        try { masterCatalog2 = masterMgr2.LoadAsync(_vm.Root).GetAwaiter().GetResult(); }
                        catch { return new(); }
                    }
                    return masterCatalog2 != null ? masterMgr2.FindAllMastersInText(masterCatalog2.Records, text) : new();
                };
            }
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

            _translationView.StarToggleRequested += async (_, _) =>
            {
                await _vm.ToggleStarAsync();
            };

            // 5D-1: Jump to Search tab and run the selected text as a corpus query.
            _translationView.SearchCorpusRequested += (_, query) =>
            {
                ForceTab(2);
                _searchView?.FocusQueryBox();
                _searchView?.SetQueryAndSearch(query);
            };

            _translationView.ResolveLbForBlock = blockNumber =>
            {
                var doc = _vm.IndexedDoc;
                if (doc == null) return null;
                var mode = _vm.TranslationMode;
                var wantedKind = mode switch
                {
                    TranslationEditMode.Head => ReadZen.App.Services.TranslationUnitKind.Head,
                    TranslationEditMode.Notes => ReadZen.App.Services.TranslationUnitKind.Note,
                    _ => ReadZen.App.Services.TranslationUnitKind.Body
                };
                var unit = doc.Units
                    .Where(u => u.Kind == wantedKind)
                    .FirstOrDefault(u => u.Index == blockNumber);
                return ReadZen.App.Services.TranslationUnit.GetLbNValueForUnit(unit);
            };
        }

        if (_searchView != null)
        {
            _searchView.GetTranslationUser = () => _vm.GetActiveTranslationUser();
            _searchView.GetTranslationSourceKey = () => _vm.GetActiveSearchSourceKey();
            _searchView.GetShareableTranslationSourceKey = () => _vm.GetActiveSearchSourceKey(forShareableLink: true);
            _searchView.Status += (_, msg) =>
            {
                _vm.SetStatus(msg);
                // 4E: Toast for index build completion
                if (msg.StartsWith("Search index rebuilt", StringComparison.OrdinalIgnoreCase) ||
                    msg.StartsWith("Search index updated", StringComparison.OrdinalIgnoreCase))
                    ShowToast(msg);
            };
            _searchView.NavigationRequested += (_, req) =>
            {
                _vm.HandleNavigationRequested(req);
            };
            _searchView.OpenMasterRequested += async (_, masterName) =>
            {
                await OpenZenMasterManagerWindowAsync(masterName);
            };
            _searchAddToScholarHandler = async (_, passage) =>
            {
                await HandleAddToScholarAsync(passage);
            };
            _searchView.AddToScholarRequested += _searchAddToScholarHandler;

            // 4C: persist search history to config after each search
            _searchView.SearchHistoryChanged += (_, _) =>
            {
                _vm.Config.SearchHistory = _searchView.ViewModel.SnapshotHistory();
                _ = _vm.SafeSaveConfigAsync();
            };

            // 4J: open a search result document in a new independent reader window.
            _searchView.OpenInNewWindowRequested += (_, relPath) =>
            {
                var root = _vm.TranslationRoot ?? _vm.Root;
                if (string.IsNullOrEmpty(root)) return;
                WindowNavigationService.OpenAndNavigate(root, new NavigationRequest { RelPath = relPath });
            };
        }

        if (_gitView != null)
        {
            _gitStatusHandler = (_, msg) =>
            {
                _vm.SetStatus(msg);
                // 4E: Toast for significant git operations
                if (msg.StartsWith("PR created:", StringComparison.OrdinalIgnoreCase) ||
                    msg.StartsWith("Local commit ready", StringComparison.OrdinalIgnoreCase) ||
                    msg.StartsWith("Community data shared", StringComparison.OrdinalIgnoreCase) ||
                    msg.StartsWith("Scholar collections shared", StringComparison.OrdinalIgnoreCase))
                    ShowToast(msg);
            };
            _gitView.Status += _gitStatusHandler;

            _gitView.GitHubAuthCompleted += async (_, args) =>
            {
                try { await _vm.HandleGitHubAuthCompletedAsync(args.Token, args.Login); }
                catch { }
            };

            _gitView.PrepareCommunityShareRequested += async () =>
            {
                try { await _vm.EnsureUserTranslationDirectoryCanonicalizedForSyncAsync(); }
                catch (Exception ex) { _vm.SetStatus("Prepare sync migration failed: " + ex.Message); throw; }
            };


            _gitView.EnsurePersonalTranslatedForSelectedRequested += async relPath =>
            {
                try { return await _vm.EnsurePersonalTranslatedXmlForRelPathAsync(relPath, saveCurrentEditor: true); }
                catch (Exception ex) { _vm.SetStatus("Prepare personal translated XML failed: " + ex.Message); return false; }
            };
            _gitView.EnsureTranslatedForSelectedRequested += async relPath =>
            {
                try { return await _vm.EnsureTranslatedXmlForRelPathAsync(relPath, saveCurrentEditor: true); }
                catch (Exception ex) { _vm.SetStatus("Prepare translated XML failed: " + ex.Message); return null; }
            };

            _rootClonedHandler = async (_, repoRoot) =>
            {
                await _vm.HandleRootClonedAsync(repoRoot, IsSecondaryWindow);
                _tourService?.AdvanceIfWaitingFor("root-cloned");
            };
            _gitView.RootCloned += _rootClonedHandler;

            _communityDataFetchedHandler = async (_, _) =>
            {
                await _vm.RefreshCommunityDataForCurrentFileAsync();
            };
            _gitView.CommunityDataFetched += _communityDataFetchedHandler;

            // Refresh the sidebar file list after sync completes so new/renamed
            // titles from upstream appear without restarting the app.
            // 4E: Also show a toast on sync completion.
            _gitView.SyncCompleted += async (_, _) =>
            {
                ShowToast("Sync completed successfully");

                try { await _vm.LoadFileListFromCacheOrBuildAsync(); }
                catch { /* non-critical — sidebar stays stale until restart */ }

                try { await _vm.ReloadStarsAsync(); }
                catch { /* non-critical — star counts stay stale until restart */ }

                // Background index rebuild — old index stays usable while building
                if (_searchView?.ViewModel.BuildIndexCommand.CanExecute(null) == true)
                {
                    ShowToast("Updating search index in background...", 4000);
                    _searchView.ViewModel.BuildIndexCommand.Execute(null);
                }
            };
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
            _scholarView.OpenDictionaryTermRequested += async (_, term) =>
            {
                await _vm.OpenTermbaseEditorAsync();
                // TODO: could pre-select the term in the editor
            };
            _scholarView.OpenMasterRequested += async (_, name) =>
            {
                await OpenZenMasterManagerWindowAsync(name);
            };
            _scholarView.DictionarySourceChanged += async (_, idx) =>
            {
                await _vm.SwitchTranslationSourceAsync(idx);
            };

            _scholarView.ZenMastersRequested += async (_, _) =>
            {
                await OpenZenMasterManagerWindowAsync();
            };

            // Reload scholar data when ANY window (including secondary) adds a passage
            if (!IsSecondaryWindow)
            {
                _scholarDataChangedHandler = (sender, _) =>
                {
                    // Only reload if the change came from a different view instance
                    if (sender != _scholarView && !string.IsNullOrWhiteSpace(_vm.TranslationRoot ?? _vm.Root))
                        _scholarView.SetRoot((_vm.TranslationRoot ?? _vm.Root)!);
                };
                ScholarTabView.ScholarDataChanged += _scholarDataChangedHandler;
            }
        }

        if (_readableView != null)
        {
            _readableAddToScholarHandler = async (_, passage) =>
            {
                await HandleAddToScholarAsync(passage);
            };
            _readableView.AddToScholarRequested += _readableAddToScholarHandler;
        }

        if (_translationView != null)
        {
            _translationAddToScholarHandler = async (_, passage) =>
            {
                await HandleAddToScholarAsync(passage);
            };
            _translationView.AddToScholarRequested += _translationAddToScholarHandler;
        }

        // Masters tab buttons
        var btnOpenMasters = Find<Button>("BtnOpenMasters");
        if (btnOpenMasters != null)
            btnOpenMasters.Click += async (_, _) => await OpenZenMasterManagerWindowAsync();

        var btnBuildMasterIndex = Find<Button>("BtnBuildMasterIndex");
        if (btnBuildMasterIndex != null)
            btnBuildMasterIndex.Click += async (_, _) =>
            {
                if (string.IsNullOrEmpty(_vm.Root)) return;
                btnBuildMasterIndex.IsEnabled = false;
                var txtInfo = Find<TextBlock>("TxtMastersCorpusInfo");
                try
                {
                    var svc = new MasterCorpusSearchService();
                    var masterDatesSvc = App.Services.GetRequiredService<IMasterDatesService>();
                    var masterMgr = new ZenMasterManagerService(masterDatesSvc);
                    var catalog = await masterMgr.LoadAsync(_vm.Root);

                    if (txtInfo != null) txtInfo.Text = "Scanning corpus...";

                    var progress = new Progress<(int done, int total, string status)>(p =>
                    {
                        if (txtInfo != null) txtInfo.Text = p.status;
                    });

                    var index = await svc.BuildFullIndexAsync(_vm.Root, catalog, progress);
                    var cacheDir = MasterCorpusSearchService.GetCacheDir(_vm.Root);
                    await svc.SaveAsync(cacheDir, index);

                    if (txtInfo != null)
                        txtInfo.Text = $"Index ready: {index.MasterCount} of {catalog.Records.Count} masters found in texts, {index.Appearances.Count} appearances across {index.FileCount} files";
                    _vm.SetStatus($"Master corpus index rebuilt ({index.MasterCount} of {catalog.Records.Count} masters appear in texts).");
                }
                catch (Exception ex)
                {
                    if (txtInfo != null) txtInfo.Text = $"Failed: {ex.Message}";
                }
                finally { btnBuildMasterIndex.IsEnabled = true; }
            };

        // Update Masters tab status on load
        UpdateMastersTabInfo();

        AddHandler(InputElement.KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
    }

    private void UpdateMastersTabInfo()
    {
        var txtStatus = Find<TextBlock>("TxtMastersStatus");
        var txtCorpus = Find<TextBlock>("TxtMastersCorpusInfo");
        if (txtStatus == null || txtCorpus == null || string.IsNullOrEmpty(_vm.Root)) return;

        _ = Task.Run(async () =>
        {
            try
            {
                // Load master count
                var masterDatesSvc = App.Services.GetRequiredService<IMasterDatesService>();
                var masterMgr = new ZenMasterManagerService(masterDatesSvc);
                var catalog = await masterMgr.LoadAsync(_vm.Root);
                Dispatcher.UIThread.Post(() =>
                {
                    txtStatus.Text = $"{catalog.Records.Count} masters catalogued";
                });

                var svc = new MasterCorpusSearchService();
                var cacheDir = MasterCorpusSearchService.GetCacheDir(_vm.Root!);
                var cached = await svc.TryLoadAsync(cacheDir);

                Dispatcher.UIThread.Post(() =>
                {
                    if (cached != null)
                        txtCorpus.Text = $"Corpus index: {cached.MasterCount} masters found in {cached.FileCount} files ({cached.Appearances.Count} total appearances)";
                    else
                        txtCorpus.Text = "Text scanning hasn't run yet. Click 'Scan Texts for Masters' or it will happen automatically next time you open the app.";
                });
            }
            catch { }
        });
    }

    private void EnsureScholarContextReady()
    {
        if (_scholarView == null) return;
        if (!string.IsNullOrWhiteSpace(_vm.TranslationRoot ?? _vm.Root))
            _scholarView.SetRoot((_vm.TranslationRoot ?? _vm.Root)!);
        _scholarView.SetUsername(_vm.Config.GitHubUsername ?? _vm.Config.Username);
        _scholarView.SetAssistantUsername(_vm.GetActiveDictionaryUser());
        _scholarView.SetTranslationDirs(_vm.OriginalDir, _vm.GetActiveTranslatedDir());
    }

    private async Task HandleAddToScholarAsync(ScholarPassage passage)
    {
        try
        {
            if (_scholarView == null)
            {
                _vm.SetStatus("Scholar is unavailable.");
                return;
            }

            EnsureScholarContextReady();

            var scholarVm = _scholarView.DataContext as ScholarTabViewModel;
            if (scholarVm != null && scholarVm.Collections.Count > 0)
            {
                var picker = new CollectionPickerDialog(scholarVm.Collections);
                var selected = await picker.ShowDialog<ScholarCollection?>(this);
                if (selected == null) return; // user cancelled

                // If this is a newly created collection (not yet in VM), add it
                if (!scholarVm.Collections.Any(c => c.Id == selected.Id))
                    scholarVm.Collections.Add(selected);

                await scholarVm.AddPassageToCollectionAsync(selected.Id, passage);
                scholarVm.RefreshPassagesList();
                scholarVm.RebuildTree();
                _vm.SetStatus($"Passage added to '{selected.Name}'.");
            }
            else
            {
                // Auto-create if empty
                if (await _scholarView.TryAddPassageAsync(passage))
                    _vm.SetStatus("Passage added to Scholar collection.");
                else
                    _vm.SetStatus("Could not add passage. " + (scholarVm != null ? scholarVm.StatusMessage : ""));
            }
        }
        catch (Exception ex)
        {
            _vm.SetStatus($"Add to Scholar failed: {ex.Message}");
        }
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
        // Ctrl+Shift+P  -  open command palette
        if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.P)
        {
            e.Handled = true;
            ToggleCommandPalette();
            return;
        }

        // Ctrl+D  -  open dictionary from any tab
        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.D)
        {
            e.Handled = true;
            _ = _vm.OpenTermbaseEditorAsync();
            return;
        }

        // Ctrl+K  -  jump to Search tab and focus query box
        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.K)
        {
            e.Handled = true;
            ForceTab(2);
            _searchView?.FocusQueryBox();
            return;
        }

        if (e.KeyModifiers != KeyModifiers.Alt) return;
        if (_tabs?.SelectedIndex != 1) return; // only active on Translation tab

        if (e.Key == Key.A)
        {
            e.Handled = true;
            _ = _vm.HandleReviewActionAsync(TranslationReviewStatuses.Approved);
        }
        // Alt+N (needs-work) removed  -  button hidden, shortcut disabled
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
            Title = "Select Read Zen texts folder"
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
        // Safety net: if the user closes via the window's X button (or Alt+F4),
        // treat it as "No" so tcs.Task doesn't hang forever → app freeze.
        win.Closed += (_, _) => tcs.TrySetResult(false);

        await win.ShowDialog(this);
        return await tcs.Task;
    }

    // ===========================================================
    // Tour guard — blocks non-modal windows and settings while tour is active
    // ===========================================================

    private bool BlockIfTourActive()
    {
        if (_tourService?.IsActive != true) return false;
        _vm.SetStatus("Complete or skip the tutorial first.");
        return true;
    }

    // ===========================================================
    // Command palette
    // ===========================================================

    private void SetupCommandPalette()
    {
        _allCommands = new List<(string Label, Action Execute)>
        {
            ("Read: Open reader",            () => ForceTab(0)),
            ("Translate: Open editor",       () => ForceTab(1)),
            ("Search: Open search",          () => ForceTab(2)),
            ("Sync: Open git sync",          () => ForceTab(3)),
            ("Collect: Open collections",    () => ForceTab(4)),
            ("Lineage: Open masters",        () => ForceTab(5)),
            ("Settings: Open preferences",   () => _ = _vm.OpenSettingsAsync()),
            ("Termbase: Open editor",        () => _ = _vm.OpenTermbaseEditorAsync()),
            ("Index: Build search index",    () => _searchView?.ViewModel.BuildIndexCommand.Execute(null)),
            ("Theme: Toggle dark/light",     () => ToggleDarkLight()),
            ("Search: Clear all filters",    () => _searchView?.Clear()),
            ("Search: Focus query",          () => { ForceTab(2); _searchView?.FocusQueryBox(); }),
            ("Reader: Toggle sidebar",       () => { if (_navPanel != null) _navPanel.IsVisible = !_navPanel.IsVisible; }),
            ("Export: Export search results", () => _searchView?.ViewModel.ExportCommand.Execute(null)),
        };

        if (_commandPaletteInput != null)
        {
            _commandPaletteInput.TextChanged += (_, _) => FilterCommandPalette();
            _commandPaletteInput.KeyDown += CommandPaletteInput_KeyDown;
        }

        if (_commandPaletteList != null)
        {
            _commandPaletteList.DoubleTapped += (_, _) => ExecuteSelectedCommand();
        }
    }

    private void ToggleDarkLight()
    {
        var isDark = _vm.Config.IsDarkTheme;
        ApplyTheme(!isDark);
        _vm.Config.IsDarkTheme = !isDark;
        _ = _vm.SafeSaveConfigAsync();
    }

    private Panel? _commandPaletteScrim;

    private void ToggleCommandPalette()
    {
        if (_commandPaletteOverlay == null) return;
        _commandPaletteScrim ??= this.FindControl<Panel>("CommandPaletteScrim");

        if (_commandPaletteOverlay.IsVisible)
        {
            _commandPaletteOverlay.IsVisible = false;
            if (_commandPaletteScrim != null) _commandPaletteScrim.IsVisible = false;
        }
        else
        {
            if (_commandPaletteScrim != null)
            {
                _commandPaletteScrim.IsVisible = true;
                _commandPaletteScrim.PointerPressed -= OnCommandPaletteScrimPressed;
                _commandPaletteScrim.PointerPressed += OnCommandPaletteScrimPressed;
            }
            _commandPaletteOverlay.IsVisible = true;
            if (_commandPaletteInput != null)
                _commandPaletteInput.Text = string.Empty;
            PopulateCommandPalette(string.Empty);
            Dispatcher.UIThread.Post(() => _commandPaletteInput?.Focus(), DispatcherPriority.Input);
        }
    }

    private void OnCommandPaletteScrimPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (_commandPaletteOverlay != null) _commandPaletteOverlay.IsVisible = false;
        if (_commandPaletteScrim != null) _commandPaletteScrim.IsVisible = false;
    }

    private static int FuzzyScore(string text, string query)
    {
        int qi = 0;
        int score = 0;
        int lastMatch = -1;
        var lower = text.ToLowerInvariant();
        var qLower = query.ToLowerInvariant();

        for (int ti = 0; ti < lower.Length && qi < qLower.Length; ti++)
        {
            if (lower[ti] == qLower[qi])
            {
                score += (ti == lastMatch + 1) ? 3 : 1; // consecutive bonus
                if (ti == 0 || text[ti - 1] == ' ' || text[ti - 1] == ':') score += 2; // word boundary bonus
                lastMatch = ti;
                qi++;
            }
        }
        return qi == qLower.Length ? score : -1; // -1 = no match
    }

    private void PopulateCommandPalette(string filter)
    {
        if (_commandPaletteList == null) return;
        List<string> items;
        if (string.IsNullOrWhiteSpace(filter))
        {
            items = _allCommands.Select(c => c.Label).ToList();
        }
        else
        {
            items = _allCommands
                .Select(c => (c.Label, Score: FuzzyScore(c.Label, filter)))
                .Where(x => x.Score >= 0)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Label)
                .ToList();
        }
        _commandPaletteList.ItemsSource = items;
        if (items.Count > 0)
            _commandPaletteList.SelectedIndex = 0;
    }

    private void FilterCommandPalette()
    {
        var text = _commandPaletteInput?.Text ?? string.Empty;
        PopulateCommandPalette(text);
    }

    private void CommandPaletteInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            if (_commandPaletteOverlay != null)
                _commandPaletteOverlay.IsVisible = false;
            return;
        }

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            ExecuteSelectedCommand();
            return;
        }

        if (e.Key == Key.Down && _commandPaletteList != null)
        {
            e.Handled = true;
            var count = (_commandPaletteList.ItemsSource as System.Collections.IList)?.Count ?? 0;
            if (count > 0)
                _commandPaletteList.SelectedIndex = Math.Min((_commandPaletteList.SelectedIndex + 1), count - 1);
            return;
        }

        if (e.Key == Key.Up && _commandPaletteList != null)
        {
            e.Handled = true;
            _commandPaletteList.SelectedIndex = Math.Max((_commandPaletteList.SelectedIndex - 1), 0);
            return;
        }
    }

    private void ExecuteSelectedCommand()
    {
        if (_commandPaletteList?.SelectedItem is not string label) return;
        var cmd = _allCommands.FirstOrDefault(c => c.Label == label);
        if (cmd.Execute == null) return;
        if (_commandPaletteOverlay != null)
            _commandPaletteOverlay.IsVisible = false;
        cmd.Execute();
    }

    // ===========================================================
    // Termbase editor window
    // ===========================================================

    private async Task OpenTermbaseEditorWindowAsync(string root, string? username = null, string? landingTerm = null, string? landingCommunityUser = null)
    {
        if (BlockIfTourActive()) return;
        try
        {
            if (_termbaseEditorWindow != null)
            {
                _termbaseEditorWindow.ApplyLanding(landingTerm, landingCommunityUser);
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

            var win = new TermbaseEditorWindow(root, username, landingTerm, landingCommunityUser)
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
    // Zen Master manager window
    // ===========================================================

    private Task OpenZenMasterManagerWindowAsync(string? landingName = null, string? landingUser = null)
    {
        if (BlockIfTourActive()) return Task.CompletedTask;
        try
        {
            if (_zenMasterManagerWindow != null)
            {
                _zenMasterManagerWindow.ApplyLanding(landingName, landingUser);
                _zenMasterManagerWindow.Activate();
                return Task.CompletedTask;
            }

            var win = new ZenMasterManagerWindow(_vm.TranslationRoot ?? _vm.Root, _vm.Root)
            {
                RequestedThemeVariant = this.ActualThemeVariant
            };

            win.ApplyLanding(landingName, landingUser);
            win.CorpusNavigationRequested += (_, req) => _vm.HandleNavigationRequested(req);
            win.Closed += (_, _) => _zenMasterManagerWindow = null;

            _zenMasterManagerWindow = win;
            win.Show();

            if (!string.IsNullOrWhiteSpace(landingName))
                _vm.SetStatus($"Opened Zen Master Manager for \"{landingName}\"." , StatusSeverity.Info);
        }
        catch (Exception ex)
        {
            _vm.SetStatus("Open Zen Master Manager failed: " + ex.Message, StatusSeverity.Warning);
        }

        return Task.CompletedTask;
    }
    // ===========================================================
    // Tag editor window
    // ===========================================================
    private Task OpenTagEditorWindowAsync()
    {
        if (BlockIfTourActive()) return Task.CompletedTask;
        try
        {
            if (_tagEditorWindow != null)
            {
                _tagEditorWindow.Activate();
                return Task.CompletedTask;
            }

            var root = _vm.TranslationRoot ?? _vm.Root;
            if (string.IsNullOrEmpty(root)) return Task.CompletedTask;

            var win = new TagEditorWindow(root, _vm.Username)
            {
                RequestedThemeVariant = this.ActualThemeVariant
            };

            win.VocabularySaved += async (_, _) => await _vm.ReloadTagVocabularyAsync();
            win.Closed += (_, _) => _tagEditorWindow = null;

            _tagEditorWindow = win;
            win.Show();
        }
        catch (Exception ex)
        {
            _vm.SetStatus("Open tag editor failed: " + ex.Message);
        }

        return Task.CompletedTask;
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
                data.OtherVocab,
                data.CommunityRoot,
                data.ResolverUsername);
            win.Show(this);
        }
        catch (Exception ex)
        {
            _vm.SetStatus("Open tag comparison failed: " + ex.Message);
        }
    }

    // ===========================================================
    // Analytics windows
    // ===========================================================

    private async Task OpenCodeFrequencyWindowAsync()
    {
        try
        {
            var root = _vm.TranslationRoot ?? _vm.Root;
            var username = _vm.Username;
            if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(username))
            {
                _vm.SetStatus("No project or username set.");
                return;
            }

            var tagSvc = new Services.DocumentTagService();
            var tags = await tagSvc.LoadUserTagsAsync(root, username);
            var vocab = await tagSvc.LoadVocabularyAsync(root, username);

            var win = new CodeFrequencyWindow
            {
                RequestedThemeVariant = this.ActualThemeVariant
            };
            win.LoadData(tags, vocab);
            win.Show(this);
        }
        catch (Exception ex)
        {
            _vm.SetStatus("Code frequency failed: " + ex.Message);
        }
    }

    private async Task OpenCodeCooccurrenceWindowAsync()
    {
        try
        {
            var root = _vm.TranslationRoot ?? _vm.Root;
            var username = _vm.Username;
            if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(username))
            {
                _vm.SetStatus("No project or username set.");
                return;
            }

            var tagSvc = new Services.DocumentTagService();
            var tags = await tagSvc.LoadUserTagsAsync(root, username);
            var vocab = await tagSvc.LoadVocabularyAsync(root, username);

            var win = new CodeCooccurrenceWindow
            {
                RequestedThemeVariant = this.ActualThemeVariant
            };
            win.LoadData(tags, vocab);
            win.Show(this);
        }
        catch (Exception ex)
        {
            _vm.SetStatus("Co-occurrence matrix failed: " + ex.Message);
        }
    }

    private async Task OpenDocumentVariablesWindowAsync()
    {
        try
        {
            var root = _vm.TranslationRoot ?? _vm.Root;
            var username = _vm.Username;
            if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(username))
            {
                _vm.SetStatus("No project or username set.");
                return;
            }

            var tagSvc = new Services.DocumentTagService();
            var tags = await tagSvc.LoadUserTagsAsync(root, username);
            var vocab = await tagSvc.LoadVocabularyAsync(root, username);

            var win = new DocumentVariablesWindow
            {
                RequestedThemeVariant = this.ActualThemeVariant
            };
            await win.LoadDataAsync(root, tags, vocab);
            win.Show(this);
        }
        catch (Exception ex)
        {
            _vm.SetStatus("Document variables failed: " + ex.Message);
        }
    }

    private async Task OpenQueryBuilderWindowAsync()
    {
        try
        {
            var root = _vm.TranslationRoot ?? _vm.Root;
            var username = _vm.Username;
            if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(username))
            {
                _vm.SetStatus("No project or username set.");
                return;
            }

            var tagSvc = new Services.DocumentTagService();
            var tags = await tagSvc.LoadUserTagsAsync(root, username);
            var vocab = await tagSvc.LoadVocabularyAsync(root, username);

            var win = new QueryBuilderWindow
            {
                RequestedThemeVariant = this.ActualThemeVariant
            };
            win.LoadData(root, tags, vocab);
            win.Show(this);
        }
        catch (Exception ex)
        {
            _vm.SetStatus("Query builder failed: " + ex.Message);
        }
    }

    private async Task ExportQdpxAsync()
    {
        try
        {
            var root = _vm.TranslationRoot ?? _vm.Root;
            var username = _vm.Username;
            if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(username))
            {
                _vm.SetStatus("No project or username set.");
                return;
            }

            var sp = GetTopLevel(this)?.StorageProvider;
            if (sp == null) return;

            var file = await sp.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Export QDPX Project",
                DefaultExtension = "qdpx",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("QDPX") { Patterns = new[] { "*.qdpx" } }
                }
            });

            if (file == null) return;

            var tagSvc = new Services.DocumentTagService();
            var tags = await tagSvc.LoadUserTagsAsync(root, username);
            var vocab = await tagSvc.LoadVocabularyAsync(root, username);

            var outputPath = file.Path.LocalPath;

            await Services.QdpxExportService.ExportAsync(
                outputPath, tags, vocab,
                async (relPath, ct) =>
                {
                    // Load plaintext via the renderer
                    try
                    {
                        var xmlPath = System.IO.Path.Combine(_vm.OriginalDir ?? root, relPath);
                        if (!System.IO.File.Exists(xmlPath)) return null;
                        var xml = await System.IO.File.ReadAllTextAsync(xmlPath, ct);
                        var doc = Text.TeiRenderer.Render(xml);
                        return doc.Text;
                    }
                    catch { return null; }
                });

            _vm.SetStatus($"Exported QDPX to {outputPath}");
        }
        catch (Exception ex)
        {
            _vm.SetStatus("QDPX export failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Shows a picker dialog for two translation sources, then opens a 3-pane comparison window
    /// with the original Chinese text and both selected translations.
    /// </summary>
    // ── Edition Process Dialog ────────────────────────────────────────

    private bool _editionDialogOpen;

    private Task OpenEditionProcessDialogAsync(ManifestInfo manifest, string? xmlAbsPath)
    {
        if (_editionDialogOpen) return Task.CompletedTask;
        _editionDialogOpen = true;

        try
        {
            var dialog = new EditionProcessDialog
            {
                RequestedThemeVariant = this.ActualThemeVariant,
            };

            var processSvc = App.Services.GetService<ProcessService>();
            var apparatusSvc = App.Services.GetService<ApparatusService>();
            var statsSvc = App.Services.GetService<EditionStatsService>();
            var docsSvc = App.Services.GetService<DocumentsService>();
            var timelineSvc = App.Services.GetService<TimelineService>();
            var logSvc = App.Services.GetService<HumanLogService>();
            var witnessSvc = App.Services.GetService<WitnessTextService>();

            // Get the current rendered translation for the text preview
            RenderedDocument? renderedTran = null;
            try { renderedTran = _readableView?.GetRenderedTranslation(); }
            catch { }

            dialog.Load(manifest, xmlAbsPath,
                processSvc, apparatusSvc, statsSvc, docsSvc,
                timelineSvc, logSvc, renderedTran, witnessSvc);

            dialog.Closed += (_, _) => _editionDialogOpen = false;
            dialog.Show(this);
        }
        catch (Exception ex)
        {
            _editionDialogOpen = false;
            _vm.SetStatus($"Edition details: {ex.Message}", StatusSeverity.Error);
        }
        return Task.CompletedTask;
    }

    // ── Translation Licensing ─────────────────────────────────────────

    private readonly TranslationLicenseService _licenseService = new();
    private bool _licenseServiceLoaded;

    private async Task PromptLicenseIfNeededAsync()
    {
        try
        {
            if (_vm.CurrentRelPath == null || _vm.TranslationRoot == null) return;

            var username = _vm.Config.GitHubUsername ?? _vm.Config.Username;
            if (string.IsNullOrWhiteSpace(username)) return;

            // Lazy-load the license service
            if (!_licenseServiceLoaded)
            {
                await _licenseService.LoadUserLicensesAsync(_vm.TranslationRoot, username);
                _licenseServiceLoaded = true;
            }

            // Already has a license for this file? Skip.
            var existing = _licenseService.GetLicense(_vm.CurrentRelPath);
            if (existing?.License != null) return;

            // CBETA auto-applies NC — no prompt needed
            if (_vm.ActiveCorpus == CorpusKind.Cbeta) return;

            // Get source license info
            var sourceLicense = _vm.GetLicenseForCurrentFile();
            var sourceLicenseId = sourceLicense?.ShortLabel;

            var dialog = new TranslationLicenseDialog();
            dialog.Load(sourceLicenseId, _vm.ActiveCorpus, sourceLicenseId);
            await dialog.ShowDialog(this);

            if (dialog.ChosenLicense != null)
            {
                var info = new Models.TranslationLicenseInfo
                {
                    RelPath = _vm.CurrentRelPath,
                    License = dialog.ChosenLicense.Id,
                    LicenseUrl = dialog.ChosenLicense.Url,
                    CopyrightHolder = username,
                    Username = username,
                    CommercialUseAllowed = dialog.ChosenLicense.CommercialOk,
                    AttributionRequired = dialog.ChosenLicense.AttributionRequired,
                    ShareAlikeRequired = dialog.ChosenLicense.ShareAlikeRequired,
                };
                await _licenseService.SaveLicenseAsync(_vm.TranslationRoot, username, info);
                _vm.SetStatus($"License set: {dialog.ChosenLicense.DisplayName}");
            }
        }
        catch (Exception ex)
        {
            _vm.SetStatus($"License prompt: {ex.Message}");
        }
    }

    // ── Time Travel (Reader) ────────────────────────────────────────

    /// <summary>
    /// Discovers a correction-log.md for a CE file and enables the Reader's
    /// time-travel bar if found. Convention: provenance at
    /// ../../../provenance/{slug}/process/correction-log.md relative to the
    /// edition directory.
    /// </summary>
    private void DiscoverAndSetCorrectionLog(string? xmlAbsPath)
    {
        if (xmlAbsPath == null || _readableView == null)
        {
            _readableView?.ClearCorrectionTimeline();
            return;
        }

        try
        {
            var editionDir = System.IO.Path.GetDirectoryName(xmlAbsPath);
            if (editionDir == null) { _readableView.ClearCorrectionTimeline(); return; }

            var slug = System.IO.Path.GetFileName(editionDir);
            var provRoot = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(editionDir, "..", "..", "..", "provenance", slug));

            string? corrLogPath = null;
            string? workingTextPath = null;

            if (System.IO.Directory.Exists(provRoot))
            {
                var candidate = System.IO.Path.Combine(provRoot, "process", "correction-log.md");
                if (System.IO.File.Exists(candidate)) corrLogPath = candidate;

                var correctedDir = System.IO.Path.Combine(provRoot, "transcription", "corrected");
                if (System.IO.Directory.Exists(correctedDir))
                {
                    try
                    {
                        workingTextPath = System.IO.Directory.GetFiles(correctedDir, "*working*")
                            .FirstOrDefault();
                    }
                    catch { }
                }
            }

            _readableView.SetCorrectionLog(corrLogPath, workingTextPath);
        }
        catch
        {
            _readableView.ClearCorrectionTimeline();
        }
    }

    private async Task PopulateReaderVersionPickerAsync()
    {
        try
        {
            if (_vm.CurrentRelPath == null || _vm.TranslationRoot == null)
            {
                _readableView?.ClearVersionPicker();
                return;
            }

            var git = App.Services.GetRequiredService<IGitRepoService>();
            var translatedDir = _vm.GetActiveTranslatedDir();
            if (translatedDir == null) { _readableView?.ClearVersionPicker(); return; }

            var tranAbsPath = System.IO.Path.Combine(translatedDir, _vm.CurrentRelPath);
            var repoRelPath = System.IO.Path.GetRelativePath(_vm.TranslationRoot, tranAbsPath);

            var commits = await git.GetFileLogAsync(_vm.TranslationRoot, repoRelPath, 30);
            _readableView?.PopulateVersionPicker(commits);
        }
        catch
        {
            _readableView?.ClearVersionPicker();
        }
    }

    private async Task LoadReaderHistoricalVersionAsync(string? commitHash)
    {
        try
        {
            if (commitHash == null)
            {
                // "(current)" selected — reload from disk and clear diff
                _readableView?.ClearDiffHighlighting();
                await _vm.ReloadCurrentReadableAsync();
                return;
            }

            if (_vm.CurrentRelPath == null || _vm.TranslationRoot == null) return;

            var git = App.Services.GetRequiredService<IGitRepoService>();
            var translatedDir = _vm.GetActiveTranslatedDir();
            if (translatedDir == null) return;

            var tranAbsPath = System.IO.Path.Combine(translatedDir, _vm.CurrentRelPath);
            var repoRelPath = System.IO.Path.GetRelativePath(_vm.TranslationRoot, tranAbsPath);

            var content = await git.GetFileAtCommitAsync(_vm.TranslationRoot, commitHash, repoRelPath);
            if (content == null)
            {
                _vm.SetStatus("This file did not exist at the selected version.");
                return;
            }

            // Render the historical content as Chinese-only. The English
            // translation pane is blanked because the translation was written
            // against a potentially different version of the Chinese source —
            // showing old English next to historical Chinese is misleading,
            // especially for critical editions where the source text evolves.
            var historicalDoc = Text.TeiRenderer.Render(content);
            _readableView?.SetRenderedOriginalOnly(historicalDoc);
            _vm.SetStatus($"Viewing historical version ({commitHash[..7]}) — Chinese text only");
        }
        catch (Exception ex)
        {
            _vm.SetStatus($"History: {ex.Message}", StatusSeverity.Error);
        }
    }

    private async Task OpenTranslationHistoryAsync()
    {
        try
        {
            if (_vm.CurrentRelPath == null || _vm.TranslationRoot == null)
            {
                _vm.SetStatus("No file loaded.");
                return;
            }

            var git = App.Services.GetRequiredService<IGitRepoService>();
            var translatedDir = _vm.GetActiveTranslatedDir();
            if (translatedDir == null)
            {
                _vm.SetStatus("No translation directory available.");
                return;
            }

            // Build the path relative to the translations repo root
            var tranAbsPath = System.IO.Path.Combine(translatedDir, _vm.CurrentRelPath);
            var repoRelPath = System.IO.Path.GetRelativePath(_vm.TranslationRoot, tranAbsPath);

            var dialog = new TranslationHistoryDialog();
            dialog.LoadHistory(git, _vm.TranslationRoot, repoRelPath, _vm.CurrentRelPath);
            await dialog.ShowDialog(this);

            // If the user chose to restore, save any unsaved work first, then write and reload
            if (!string.IsNullOrEmpty(dialog.RestoredContent))
            {
                // Auto-save current work before overwriting to prevent data loss
                if (_vm.IsDirty)
                {
                    try { await _vm.SaveTranslatedFromTabAsync(); }
                    catch (Exception saveEx)
                    {
                        _vm.SetStatus($"Warning: could not auto-save before restore ({saveEx.Message}). Proceeding anyway.");
                    }
                }

                await System.IO.File.WriteAllTextAsync(tranAbsPath, dialog.RestoredContent);
                await _vm.RevertTranslatedXmlFromDiskAsync();
                _vm.SetStatus("Restored translation from history.");
            }
        }
        catch (Exception ex)
        {
            _vm.SetStatus($"History: {ex.Message}", StatusSeverity.Error);
        }
    }

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

            var sourceList = new List<string>(sources);
            var activeIndex = Math.Clamp(_vm.GetActiveTranslationSourceIndex(), 0, sourceList.Count - 1);
            var fallbackIndex = Enumerable.Range(0, sourceList.Count).FirstOrDefault(i => i != activeIndex);

            var dialog = new Window
            {
                Title = "Select Translations to Compare",
                Width = 400,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                RequestedThemeVariant = this.ActualThemeVariant
            };

            var cmbA = new ComboBox { ItemsSource = sourceList, SelectedIndex = activeIndex, MinWidth = 300, Margin = new Thickness(0, 4, 0, 0) };
            var cmbB = new ComboBox { ItemsSource = sourceList, SelectedIndex = fallbackIndex, MinWidth = 300, Margin = new Thickness(0, 4, 0, 0) };
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

            var sourceAKey = GetCompareSourceKey(indexA);
            var sourceBKey = GetCompareSourceKey(indexB);
            if (sourceAKey == null || sourceBKey == null)
            {
                _vm.SetStatus("Could not resolve comparison sources.");
                return;
            }

            await OpenCompareTranslationsWindowAsync(_vm.CurrentRelPath, sourceAKey, sourceBKey, null, null);
        }
        catch (Exception ex)
        {
            _vm.SetStatus("Open translation comparison failed: " + ex.Message);
        }
    }

    private async Task OpenCompareTranslationsWindowAsync(
        string relPath,
        string sourceAKey,
        string sourceBKey,
        ComparePaneTarget? landingPane,
        NavigationRequest? landingRequest)
    {
        if (string.IsNullOrWhiteSpace(_vm.Root))
        {
            _vm.SetStatus("Compare link requires a loaded text root.");
            return;
        }

        await _vm.OpenAtCoreAsync(_vm.Root!, new NavigationRequest { RelPath = relPath });

        var sourceList = new List<string>(_vm.GetTranslationSourceLabels());
        var indexA = ResolveCompareSourceIndex(sourceAKey, sourceList);
        var indexB = ResolveCompareSourceIndex(sourceBKey, sourceList);
        if (!indexA.HasValue || !indexB.HasValue || indexA.Value == indexB.Value)
        {
            _vm.SetStatus("Compare link refers to unavailable translation sources.");
            return;
        }

        var origDir = _vm.OriginalDir;
        if (origDir == null)
            return;

        var origPath = Path.Combine(origDir, relPath);
        if (!File.Exists(origPath))
        {
            _vm.SetStatus("Original file not found.");
            return;
        }

        var origXml = await File.ReadAllTextAsync(origPath, Encoding.UTF8);
        var origDoc = TeiRenderer.Render(origXml);

        var transADoc = _vm.RenderTranslationSource(indexA.Value);
        if (transADoc == null || transADoc.IsEmpty)
        {
            _vm.SetStatus($"Translation A ({sourceList[indexA.Value]}) is empty or not found for this file.");
            return;
        }

        var transBDoc = _vm.RenderTranslationSource(indexB.Value);
        if (transBDoc == null || transBDoc.IsEmpty)
        {
            _vm.SetStatus($"Translation B ({sourceList[indexB.Value]}) is empty or not found for this file.");
            return;
        }

        var data = new CompareTranslationsRequestData(
            relPath,
            relPath,
            sourceAKey,
            sourceBKey,
            origDoc,
            transADoc,
            sourceList[indexA.Value],
            transBDoc,
            sourceList[indexB.Value],
            landingPane,
            landingRequest);

        var win = new CompareTranslationsWindow
        {
            RequestedThemeVariant = this.ActualThemeVariant
        };
        win.LoadComparison(data, _vm.Config.GitHubUsername ?? _vm.Config.Username);

        // Populate version pickers if git is available
        if (_vm.TranslationRoot != null)
        {
            try
            {
                var git = App.Services.GetRequiredService<IGitRepoService>();
                var relPathA = _vm.GetTranslationSourceRepoRelPath(indexA.Value);
                var relPathB = _vm.GetTranslationSourceRepoRelPath(indexB.Value);
                await win.PopulateVersionPickersAsync(git, _vm.TranslationRoot, relPathA, relPathB);
            }
            catch { /* graceful — version pickers just stay hidden */ }
        }

        win.Show(this);
    }

    private string? GetCompareSourceKey(int index, bool forShareableLink = false)
    {
        var labels = _vm.GetTranslationSourceLabels();
        if (index < 0 || index >= labels.Count)
            return null;

        if (index == 0)
        {
            if (forShareableLink)
            {
                // Shareable links need the actual username so other people can see your translation
                var user = _vm.GetActiveTranslationUser();
                return string.IsNullOrWhiteSpace(user) ? "me" : user;
            }
            return "me";
        }
        if (index == 1)
            return "community";
        return labels[index];
    }

    private int? ResolveCompareSourceIndex(string sourceKey, IReadOnlyList<string> labels)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
            return null;

        var normalized = sourceKey.Trim();
        if (string.Equals(normalized, "me", StringComparison.OrdinalIgnoreCase))
            return labels.Count > 0 ? 0 : null;
        if (string.Equals(normalized, "community", StringComparison.OrdinalIgnoreCase))
            return labels.Count > 1 ? 1 : null;

        // If the source key matches the current user's username (GitHub or local), that's index 0
        var ghUser = _vm.Config.GitHubUsername;
        var localUser = _vm.Config.Username;
        if ((!string.IsNullOrWhiteSpace(ghUser) && string.Equals(normalized, ghUser, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(localUser) && string.Equals(normalized, localUser, StringComparison.OrdinalIgnoreCase)))
            return labels.Count > 0 ? 0 : null;

        for (int i = 2; i < labels.Count; i++)
        {
            if (string.Equals(labels[i], normalized, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return null;
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
        "AccentLinkFg","TermbaseHighlightBg",
        "TabFgMuted","TabAccent"
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

        // Don't overwrite the tooltip while a download is in progress
        if (_tourDownloadInProgress && step.Id == "download-texts")
            return;

        // Switch tab if step requires it
        if (step.SwitchToTabIndex.HasValue)
            ForceTab(step.SwitchToTabIndex.Value);

        // Auto-advance Wait steps that are already satisfied
        if (step.Type == Models.TourStepType.Wait)
        {
            bool alreadySatisfied = step.WaitForEvent switch
            {
                "root-cloned" => _vm.Root != null,
                "index-built" => _vm.AllItemsByRel.Count > 0,
                "git-check-complete" => true,
                _ => false
            };
            if (alreadySatisfied)
            {
                _ = Task.Run(async () => await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await Task.Delay(500);
                    _tourService?.AdvanceIfWaitingFor(step.WaitForEvent!);
                }));
                return;
            }
        }

        // Auto-open a file if the step requires it
        if (!string.IsNullOrWhiteSpace(step.AutoOpenRelPath) && _vm.Root != null)
        {
            _ = Task.Run(async () => await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var neededCorpus = _vm.InferCorpusForPath(step.AutoOpenRelPath);
                if (neededCorpus != CorpusKind.Unknown && neededCorpus != _vm.ActiveCorpus)
                    await _vm.SwitchCorpusAsync(neededCorpus);
                _vm.SelectInNav(step.AutoOpenRelPath);
                await _vm.LoadPairAsync(step.AutoOpenRelPath);
            }));
        }

        // Guard 1: If step targets a Reader/Translate control but no file is loaded, open default
        if (!string.IsNullOrEmpty(step.TargetControlName)
            && (step.SwitchToTabIndex is 0 or 1 || step.SwitchToTabIndex == null)
            && string.IsNullOrEmpty(_vm.CurrentRelPath)
            && _vm.Root != null
            && string.IsNullOrWhiteSpace(step.AutoOpenRelPath))
        {
            var defaultFile = "T/T48/T48n2005.xml";
            _ = Task.Run(async () => await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var neededCorpus = _vm.InferCorpusForPath(defaultFile);
                if (neededCorpus != CorpusKind.Unknown && neededCorpus != _vm.ActiveCorpus)
                    await _vm.SwitchCorpusAsync(neededCorpus);
                _vm.SelectInNav(defaultFile);
                await _vm.LoadPairAsync(defaultFile);
            }));
        }

        // Auto-jump to a specific block in the translation editor
        if (step.AutoJumpToBlock.HasValue)
            _translationView?.JumpToBlockNumber(step.AutoJumpToBlock.Value);

        // Add sample scholar data when entering scholar tutorial
        if (step.Id == "scholar-tab" && _scholarView?.DataContext is ScholarTabViewModel scholarVm)
            _ = EnsureTourScholarSampleAsync(scholarVm);

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
            if (target != null && target.IsEffectivelyVisible
                && target.Bounds.Width > 0 && target.Bounds.Height > 0)
            {
                var pt = target.TranslatePoint(new Point(0, 0), this);
                if (pt.HasValue)
                    targetBounds = new Rect(pt.Value, target.Bounds.Size);
            }
        }

        _tourSpotlight.TargetBounds = targetBounds;

        // Update tooltip content — show phase-relative progress so the user
        // sees "Step 1 of 4" during setup, not "Step 1 of 56".
        int lowerBound = _tourService.IsInSetupPhase ? 0 : _tourService.SetupStepCount;
        _tourTooltip.Update(
            step.Title,
            step.Body,
            _tourService.PhaseRelativeIndex,
            _tourService.PhaseStepCount,
            canGoBack: _tourService.CurrentIndex > lowerBound,
            actionButtonLabel: step.ActionButtonLabel,
            canSkipWait: step.CanSkipWait,
            isMandatory: step.IsMandatory);

        // Position tooltip
        PositionTooltip(step, targetBounds);

        // Handle Wait steps: trigger async actions
        if (step.Type == Models.TourStepType.Wait)
        {
            _ = HandleWaitStepAsync(step);

            // Guard 3: 30-second timeout for all Wait steps
            var stepId = step.Id;
            var waitEvent = step.WaitForEvent;
            _ = Task.Run(async () =>
            {
                await Task.Delay(30_000);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_tourService is { IsActive: true }
                        && _tourService.CurrentStep?.Id == stepId
                        && !string.IsNullOrEmpty(waitEvent))
                    {
                        bool conditionMet = waitEvent switch
                        {
                            "root-cloned" => _vm.Root != null,
                            "index-built" => _vm.AllItemsByRel.Count > 0,
                            "git-check-complete" => true,
                            _ => true
                        };

                        if (conditionMet)
                        {
                            _tourService.AdvanceIfWaitingFor(waitEvent);
                        }
                        else
                        {
                            // Condition not met — skip the tour instead of advancing into broken state
                            _tourService?.Skip();
                        }
                    }
                });
            });
        }
    }

    private void PositionTooltip(Models.TourStep step, Rect? targetBounds)
    {
        if (_tourTooltip == null) return;

        // Use ClientSize (visible area, excludes title bar) for accurate positioning
        double windowWidth = ClientSize.Width;
        double windowHeight = ClientSize.Height;

        // Let the tooltip size naturally (body text is in a ScrollViewer with MaxHeight)
        _tourTooltip.ClearValue(MaxHeightProperty);
        _tourTooltip.Measure(new Size(Math.Min(400, windowWidth - 32), windowHeight - 32));
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
            // Check git availability in background
            bool gitFound = false;
            await Task.Run(() =>
            {
                try { GitBinaryLocator.ResolveGitExecutablePath(); gitFound = true; }
                catch { }
            });

            if (gitFound)
            {
                // Brief confirmation so the user sees the step resolved
                _tourTooltip?.Update(
                    "Git Found",
                    "Git is available on your system. Ready to download texts.",
                    _tourService?.CurrentIndex ?? 1,
                    _tourService?.Steps.Count ?? 1,
                    canGoBack: true,
                    isMandatory: true);
                await Task.Delay(3000);
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
                _tourService?.AdvanceIfWaitingFor("git-check-complete"));
        }
    }

    /// <summary>
    /// Called when the mandatory setup phase (4 steps) finishes. Shows a
    /// "Take the tour?" prompt via the tooltip. If the user declines, setup
    /// is marked complete and they're free. If they accept, the optional
    /// feature tour begins as a separate sequence with its own step counter.
    /// </summary>
    private async Task OnSetupPhaseCompleted()
    {
        if (_tourTooltip == null || _tourOverlayCanvas == null || _tourService == null)
        {
            await OnTourFinished();
            return;
        }

        // Show a center-screen prompt — reuse the tooltip panel with custom
        // content. No spotlight target, just a clean card.
        _tourOverlayCanvas.IsVisible = true;
        if (_tourSpotlight != null)
            _tourSpotlight.TargetBounds = null;

        _tourTooltip.Update(
            "Setup Complete!",
            "Read Zen is ready to use. You can start reading and translating right away.\n\n" +
            "Want a quick walkthrough of all the features? It covers the Reader, Translate, " +
            "Search, Scholar, Masters, and more. You can also take it later from Settings.",
            stepIndex: 0,
            totalSteps: 1, // no progress bar — this is a prompt, not a step
            canGoBack: false,
            actionButtonLabel: "Take the Tour",
            canSkipWait: false,
            isMandatory: false); // Skip button visible → "No thanks"

        // Position center
        if (_tourTooltip != null)
        {
            _tourTooltip.Measure(new Avalonia.Size(400, 300));
            var sz = _tourTooltip.DesiredSize;
            Avalonia.Controls.Canvas.SetLeft(_tourTooltip, (Bounds.Width - sz.Width) / 2);
            Avalonia.Controls.Canvas.SetTop(_tourTooltip, (Bounds.Height - sz.Height) / 2);
        }

        // Rewire the buttons temporarily for this prompt:
        // - "Take the Tour" (Action button) → starts feature tour
        // - "Skip Tour" → finishes and marks onboarding done
        // The existing wiring calls _tourService.Next() on Next and
        // _tourService.Skip() on Skip. For this prompt:
        //   Next click → StartFeatureTour
        //   Skip click → Complete (finish)
        //   Action click → StartFeatureTour
        // Since the service is !IsActive after SetupPhaseCompleted, the
        // existing Skip/Next handlers won't do anything useful. We handle
        // it via the Action button (which fires OnTourActionClicked) and
        // the Next button. Let's mark onboarding complete and start the
        // feature tour from the action handler.
        //
        // The simplest approach: stash a flag so OnTourActionClicked and
        // the existing Skip handler know what state we're in.
        _setupPromptActive = true;
    }

    private bool _setupPromptActive;

    private async Task OnTourFinished()
    {
        _setupPromptActive = false;
        if (_tourOverlayCanvas != null)
            _tourOverlayCanvas.IsVisible = false;

        // Clean up sample scholar data
        await RemoveTourScholarSampleAsync();

        // Only mark onboarding complete if texts are actually loaded.
        // Without this check, a user who skips the download step ends up
        // with HasCompletedOnboarding=true but no texts — permanently broken.
        if (string.IsNullOrWhiteSpace(_vm.Root))
        {
            // Texts not loaded — don't mark onboarding done. Next launch
            // will re-trigger the mandatory setup.
            await _vm.SafeSaveConfigAsync();
            return;
        }

        _vm.Config.HasCompletedOnboarding = true;
        await _vm.SafeSaveConfigAsync();
    }

    private async Task EnsureTourScholarSampleAsync(ScholarTabViewModel scholarVm)
    {
        if (_tourSampleCollectionId != null) return; // Already created
        try
        {
            var collection = await scholarVm.EnsureDefaultCollectionAsync();
            _tourSampleCollectionId = collection.Id;

            // Add a sample passage from the Gateless Barrier (Case 1: Zhaozhou's Dog)
            var samplePassage = new Models.ScholarPassage
            {
                SourceRelPath = "T/T48/T48n2005.xml",
                ZhText = "\u8d99\u5dde\u548c\u5c1a\u3001\u56e0\u50e7\u554f\u300c\u72d7\u5b50\u9084\u6709\u4f5b\u6027\u4e5f\u7121\u300d\u3002\u5dde\u4e91\u300c\u7121\u300d\u3002",
                EnText = "A monk asked Zhaozhou, \u201CDoes a dog have Buddha-nature or not?\u201D Zhaozhou said, \u201CNo.\u201D",
                Notes = "Case 1 of the Gateless Barrier. Added as a sample during the tutorial.",
            };
            await scholarVm.AddPassageToCollectionAsync(collection.Id, samplePassage);
        }
        catch { /* non-critical — tutorial continues without sample data */ }
    }

    private async Task RemoveTourScholarSampleAsync()
    {
        if (_tourSampleCollectionId == null || _scholarView?.DataContext is not ScholarTabViewModel scholarVm)
            return;

        try
        {
            await scholarVm.RemoveCollectionAsync(_tourSampleCollectionId);
            _tourSampleCollectionId = null;
        }
        catch { /* non-critical */ }
    }

    private async void OnTourActionClicked()
    {
        // "Take the Tour" button on the setup-complete prompt
        if (_setupPromptActive)
        {
            _setupPromptActive = false;
            _tourService?.StartFeatureTour();
            return;
        }

        if (_tourService?.CurrentStep?.Id == "download-texts")
        {
            if (_gitView != null)
            {
                _tourDownloadInProgress = true;

                // Update tooltip — no action button, no skip, no back (locked during download)
                _tourTooltip?.Update(
                    "Downloading Texts\u2026",
                    "Downloading the original text corpus and the translation workspace. This is a large download (~2.5 GB) and will take several minutes.\n\nProgress is shown in the status bar at the bottom of the window. Please wait.",
                    _tourService?.CurrentIndex ?? 2,
                    _tourService?.Steps.Count ?? 1,
                    canGoBack: false,
                    actionButtonLabel: null,
                    canSkipWait: false,
                    isMandatory: true);

                // Pipe git status updates to the tooltip body so the user sees live progress
                void OnStatus(object? s, string msg)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        _vm.SetStatus(msg);
                        _tourTooltip?.Update(
                            "Downloading Texts\u2026",
                            msg + "\n\nThis is a large download (~2.5 GB). Please wait.",
                            _tourService?.CurrentIndex ?? 2,
                            _tourService?.Steps.Count ?? 1,
                            canGoBack: false,
                            actionButtonLabel: null,
                            canSkipWait: false,
                            isMandatory: true);
                    });
                }

                _gitView.Status += OnStatus;
                try
                {
                    await _gitView.TriggerInitialDownloadAsync();
                }
                finally
                {
                    _gitView.Status -= OnStatus;
                    _tourDownloadInProgress = false;
                }
            }
        }
    }

    /// <summary>
    /// Called after config is loaded and username prompt is done,
    /// to check whether the onboarding tour should start.
    /// </summary>
    public void MaybeStartTour()
    {
        if (IsSecondaryWindow) return;

        // Don't start tour if launched via deep link — the user wants to go somewhere specific
        var hasDeepLink = App.StartupArgs?.Any(a =>
            a.StartsWith("zen://", StringComparison.OrdinalIgnoreCase)) == true;
        if (hasDeepLink) return;

        // If root already loaded and onboarding complete, nothing to do
        if (_vm.Config.HasCompletedOnboarding && !string.IsNullOrWhiteSpace(_vm.Root))
            return;

        // If root already loaded but onboarding not marked complete (returning
        // user who requested tour restart), skip to feature walkthrough
        if (!string.IsNullOrWhiteSpace(_vm.Root))
        {
            _tourService?.Start(startIndex: 5); // Skip to "sidebar" step
            if (_tourService?.IsActive == true)
            {
                if (_tourOverlayCanvas != null) _tourOverlayCanvas.IsVisible = true;
                if (_emptyStateOverlay != null) _emptyStateOverlay.IsVisible = false;
            }
            return;
        }

        // No texts loaded — run the mandatory setup regardless of whether
        // onboarding was previously "completed" (handles upgrades from older
        // versions that set the flag without actually downloading texts).
        _vm.Config.HasCompletedOnboarding = false;
        StartTour();
    }

    /// <summary>
    /// Repaints the corpus badge in the top bar to match the active corpus,
    /// and rebuilds the click-to-switch flyout content from the VM's
    /// AvailableCorpora list. Hidden when no root is loaded; colored via
    /// DynamicResource keys the VM exposes (SuccessBg/Fg for OpenZen,
    /// WarningBg/Fg for CBETA).
    /// </summary>
    /// <summary>
    /// Repaints the top-bar license chip to match the active file's
    /// license metadata, and pushes the same metadata into the flyout
    /// content. Three states:
    ///   - license == null:                 chip hidden
    ///   - license.Class == Unknown:        "License unclear" + tooltip + raw
    ///                                      availability text in flyout
    ///   - license.Class is known:          short label + colored badge
    /// </summary>
    private void UpdateLicenseChip(TextLicenseInfo? license)
    {
        if (_btnLicenseChipTopBar == null || _txtLicenseChipTopBar == null || _licenseChipBorderTopBar == null)
            return;

        _licenseDetailsTopBar?.SetLicense(license);

        if (license == null)
        {
            _btnLicenseChipTopBar.IsVisible = false;
            ToolTip.SetTip(_btnLicenseChipTopBar, null);
            return;
        }

        if (license.LicenseClass == LicenseClass.Unknown)
        {
            _btnLicenseChipTopBar.IsVisible = true;
            _txtLicenseChipTopBar.Text = "License unclear";
            ToolTip.SetTip(_btnLicenseChipTopBar,
                "The file has a header but no recognized license keywords were detected. " +
                "Click to inspect the raw availability text and verify manually.");
            ApplyLicenseChipColorsTopBar("BarBg", "TextMutedFg");
            return;
        }

        _btnLicenseChipTopBar.IsVisible = true;
        _txtLicenseChipTopBar.Text = license.ShortLabel;
        ToolTip.SetTip(_btnLicenseChipTopBar, $"License: {license.ShortLabel}. Click for full attribution.");

        var (bgKey, fgKey) = license.LicenseClass switch
        {
            LicenseClass.PublicDomain          => ("SuccessBg", "SuccessFg"),
            LicenseClass.PermissiveAttribution => ("SuccessBg", "SuccessFg"),
            LicenseClass.CopyleftAttribution   => ("SuccessBg", "SuccessFg"),
            LicenseClass.NonCommercial         => ("WarningBg", "WarningFg"),
            LicenseClass.AllRightsReserved     => ("WarningBg", "WarningFg"),
            _                                  => ("BarBg",     "TextMutedFg"),
        };
        ApplyLicenseChipColorsTopBar(bgKey, fgKey);
    }

    private void ApplyLicenseChipColorsTopBar(string bgKey, string fgKey)
    {
        if (Application.Current?.Resources.TryGetValue(bgKey, out var bg) == true
            && bg is Avalonia.Media.IBrush bgBrush && _licenseChipBorderTopBar != null)
            _licenseChipBorderTopBar.Background = bgBrush;
        if (Application.Current?.Resources.TryGetValue(fgKey, out var fg) == true
            && fg is Avalonia.Media.IBrush fgBrush && _txtLicenseChipTopBar != null)
            _txtLicenseChipTopBar.Foreground = fgBrush;
    }

    private void UpdateCorpusBadge()
    {
        if (_btnCorpusBadge == null || _corpusBadge == null || _txtCorpusBadge == null) return;

        bool hasRoot = !string.IsNullOrEmpty(_vm.RootDisplayText);
        _btnCorpusBadge.IsVisible = hasRoot;
        if (!hasRoot) return;

        _txtCorpusBadge.Text = _vm.CorpusBadgeLabel;

        // Tooltip carries the full root path (since we no longer show
        // TxtRoot text in the top bar). Users who need to see where the
        // corpus is on disk can hover the badge.
        ToolTip.SetTip(_btnCorpusBadge,
            $"Active corpus: {_vm.CorpusBadgeLabel}\nRoot: {_vm.RootDisplayText}\nClick to switch corpus.");

        if (Application.Current?.Resources.TryGetValue(_vm.CorpusBadgeBgKey, out var bg) == true
            && bg is Avalonia.Media.IBrush bgBrush)
            _corpusBadge.Background = bgBrush;

        if (Application.Current?.Resources.TryGetValue(_vm.CorpusBadgeFgKey, out var fg) == true
            && fg is Avalonia.Media.IBrush fgBrush)
            _txtCorpusBadge.Foreground = fgBrush;

        RebuildCorpusSwitcherFlyout();
    }

    /// <summary>
    /// Populates the corpus-switcher flyout from the VM's AvailableCorpora list.
    /// Each entry becomes a Button that, when clicked, calls SwitchCorpusAsync.
    /// The currently-active corpus is highlighted and disabled. If only one
    /// corpus is available the flyout shows a "no other corpora" notice instead.
    /// </summary>
    private void RebuildCorpusSwitcherFlyout()
    {
        if (_corpusSwitcherPanel == null) return;

        _corpusSwitcherPanel.Children.Clear();

        var available = _vm.AvailableCorpora;
        var muted = Application.Current?.Resources.TryGetValue("TextMutedFg", out var mutedRes) == true
            && mutedRes is Avalonia.Media.IBrush mb ? mb : null;

        var header = new TextBlock
        {
            Text = "Switch corpus",
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            FontSize = 12,
            Margin = new Avalonia.Thickness(0, 0, 0, 6)
        };
        _corpusSwitcherPanel.Children.Add(header);

        // Always render every available corpus as a button, even the active
        // one (which is disabled). If there are zero, the legacy single-pair
        // path is in use — explain that.
        if (available.Count == 0)
        {
            var msg = new TextBlock
            {
                Text = "No multi-corpus layout detected at this root. The app is operating on a single repository pair.",
                FontSize = 11,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Foreground = muted
            };
            _corpusSwitcherPanel.Children.Add(msg);
            return;
        }

        foreach (var corpus in available)
        {
            bool isActive = corpus.Kind == _vm.ActiveCorpus;
            string label = corpus.Kind switch
            {
                ReadZen.App.Models.CorpusKind.Open => "OpenZen (commercial-OK)",
                ReadZen.App.Models.CorpusKind.Cbeta => "CBETA (non-commercial)",
                _ => corpus.Kind.ToString()
            };

            var btn = new Button
            {
                Content = isActive ? "● " + label : "○ " + label,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                FontSize = 11,
                IsEnabled = !isActive
            };

            var capturedKind = corpus.Kind;
            btn.Click += async (_, _) =>
            {
                // Close the flyout before triggering the switch so the user
                // sees the badge update without the popover blocking it.
                if (_btnCorpusBadge?.Flyout is Flyout f) f.Hide();
                await _vm.SwitchCorpusAsync(capturedKind);
            };

            _corpusSwitcherPanel.Children.Add(btn);
        }

        // If only one corpus was discovered, add a hint below the buttons
        // explaining how to get the other one.
        if (available.Count == 1)
        {
            var hint = new TextBlock
            {
                Text = "Sync via the Git tab to add the other corpus alongside this one.",
                FontSize = 10,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Foreground = muted,
                Margin = new Avalonia.Thickness(0, 6, 0, 0)
            };
            _corpusSwitcherPanel.Children.Add(hint);
        }
    }

    // -------------------------
    // Toast Notification System
    // -------------------------

    /// <summary>
    /// Displays a non-blocking toast notification in the bottom-right corner.
    /// Must be called on the UI thread.
    /// </summary>
    private StackPanel? _toastContainer;

    private void ShowToast(string message, int durationMs = 3000)
    {
        _toastContainer ??= this.FindControl<StackPanel>("ToastContainer");
        var container = _toastContainer;
        if (container == null) return;

        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(230, 30, 30, 30)),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 8),
            MaxWidth = 350,
            Child = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            }
        };

        container.Children.Add(border);

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
        timer.Tick += (_, _) => { timer.Stop(); container.Children.Remove(border); };
        timer.Start();
    }
}









