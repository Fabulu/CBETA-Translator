using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ReadZen.App.ViewModels;

public partial class GitTabViewModel : ViewModelBase
{
    private const string OriginalsRepoUrl = "https://github.com/Fabulu/CbetaZenTexts.git";
    private const string TranslationRepoUrl = "https://github.com/Fabulu/CbetaZenTranslations.git";

    // OpenZenTexts — the parallel commercial-OK corpus. Sync clones/updates
    // these alongside the CBETA pair so users always have both available
    // under one parent root, switchable via the corpus badge in the top bar.
    private const string OpenZenOriginalsRepoUrl = "https://github.com/Fabulu/OpenZenTexts.git";
    private const string OpenZenTranslationRepoUrl = "https://github.com/Fabulu/OpenZenTranslations.git";
    private const string OpenZenOriginalsFolderName = "OpenZenTexts";
    private const string OpenZenTranslationsFolderName = "OpenZenTranslations";

    private const string RepoTranslatedRoot = "xml-p5t";
    private const string UpstreamOwner = "Fabulu";
    private const string UpstreamRepo = "CbetaZenTranslations";

    private const string CommunityTmFile = "translation-memory.approved.jsonl";
    private const string CommunityTermbaseFile = "termbase.json";
    private const string ScholarCollectionsFile = "scholar-collections.json";

    private static readonly string[] LocalIgnorePatterns =
    {
        "index.cache.json",
        "search.index.manifest.json",
        "search.text.manifest.json",
        "search.text.bin",
        "search.cjk2.manifest.json",
        "search.index.bin",
        "index.debug.log",
        "*.log",
        "xml-p5t-cache"
    };

    private readonly IGitRepoService _git;
    private readonly IGitHubAuthService _auth;
    private readonly IGitHubApiService _api;
    private readonly ICommunityDataService _community;
    private readonly IScholarCollectionsService _scholarSvc;
    private readonly ITermbaseStorageService _termbaseSvc;
    private readonly ITranslationReviewService _translationReview;
    private readonly IMasterDatesService _masterDatesSvc;
    private readonly IDocumentTagService _tagService;

    private string? _baseDestFolder;
    private string? _currentRepoRoot;
    private string? _selectedRelPath;
    private string? _githubAccessToken;
    private string? _githubLogin;
    private string? _lastContribBranch;
    private string? _lastCommunityBranch;
    private string? _username;
    private CorpusKind _activeCorpus = CorpusKind.Cbeta;
    private CancellationTokenSource? _cts;

    // ----- Observable properties -----

    [ObservableProperty]
    private string _destText = "Location:";

    [ObservableProperty]
    private string _progressText = "Ready.";

    [ObservableProperty]
    private string _logText = "Welcome to Git Integration.\n\nWorkflow:\n  1) Pick a location and clone the text repos\n  2) Commit your translation\n  3) Authorize with GitHub\n  4) Push and create a pull request\n\nLog output will appear here.\n";

    [ObservableProperty]
    private string _commitMessage = "";

    [ObservableProperty]
    private string _selectedText = "Selected: (none)";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isNotBusy = true;

    // ----- Bridge delegates (wired by code-behind) -----

    public Func<Task<string?>>? PickFolderAsync { get; set; }
    public Func<string, string, string, string, Task<bool>>? ConfirmAsync { get; set; }
    public Action? ScrollLogToEnd { get; set; }
    public Func<string, string, Task>? ShowDeviceCodeAsync { get; set; }

    // ----- Events (for MainWindow) -----

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? RootCloned;
    public event EventHandler? CommunityDataFetched;
    public event Func<Task>? PrepareCommunityShareRequested;
    public event Func<string, Task<bool>>? EnsurePersonalTranslatedForSelectedRequested;
    /// Returns the absolute path of the saved file, or null on failure.
    public event Func<string, Task<string?>>? EnsureTranslatedForSelectedRequested;

    public GitTabViewModel(
        IGitRepoService git,
        IGitHubAuthService auth,
        IGitHubApiService api,
        ICommunityDataService community,
        IScholarCollectionsService scholarSvc,
        ITermbaseStorageService termbaseSvc,
        ITranslationReviewService translationReview,
        IMasterDatesService masterDatesSvc,
        IDocumentTagService tagService)
    {
        _git = git;
        _auth = auth;
        _api = api;
        _community = community;
        _scholarSvc = scholarSvc;
        _termbaseSvc = termbaseSvc;
        _translationReview = translationReview;
        _masterDatesSvc = masterDatesSvc;
        _tagService = tagService;

        _baseDestFolder = GetDefaultBaseFolder();
        UpdateDestLabel();
        TryRestoreLastBranchFromDisk();
    }

    // ----- Public methods (called by MainWindow via code-behind forwarding) -----

    public void SetCurrentRepoRoot(string? rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            return;

        var input = rootPath.Trim();
        var resolvedParent = TryResolveParentRootFromAnyFolder(input);

        if (!string.IsNullOrWhiteSpace(resolvedParent))
        {
            _currentRepoRoot = resolvedParent;
            _baseDestFolder = resolvedParent;
            UpdateDestLabel();
            TryRestoreLastBranchFromDisk();
            return;
        }

        if (Directory.Exists(input))
        {
            _currentRepoRoot = null;
            _baseDestFolder = input;
            UpdateDestLabel();
            TryRestoreLastBranchFromDisk();
        }
    }

    public void SetSelectedRelPath(string? relPath)
    {
        _selectedRelPath = string.IsNullOrWhiteSpace(relPath) ? null : NormalizeRel(relPath);
        UpdateSelectedLabel();
    }

    public void SetUsername(string? username)
    {
        _username = string.IsNullOrWhiteSpace(username) ? null : username.Trim();
    }

    /// <summary>
    /// Tells the Git tab which corpus is currently active in the rest of the
    /// app. Drives <see cref="GetTranslationRepoDir"/> /
    /// <see cref="GetOriginalsRepoDir"/> so the share + sync flows operate
    /// on the right pair of repos in a multi-corpus install.
    ///
    /// Without this, the legacy <see cref="AppPaths.GetTranslationRepoRoot"/>
    /// helper always returns the FIRST discovered translations repo (CBETA),
    /// so an OpenZen translation share would silently run against the wrong
    /// repo and report "no auto-mergeable changes" — exactly the
    /// data-disappearing failure mode the user just hit.
    /// </summary>
    public void SetActiveCorpus(CorpusKind corpus)
    {
        _activeCorpus = corpus;
    }

    /// <summary>
    /// Restore a previously persisted GitHub token so the user does not need to re-authorize every session.
    /// </summary>
    public void LoadPersistedAuth(string? token, string? login)
    {
        if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(login))
        {
            _githubAccessToken = token;
            _githubLogin = login;
            AppendLog($"GitHub: Restored session for {login}");
        }
    }

    /// <summary>
    /// Fired after a successful GitHub device-flow authorization so the caller can persist the token.
    /// </summary>
    public event EventHandler<(string Token, string Login)>? GitHubAuthCompleted;
    public event EventHandler? DeviceFlowCompleted;

    public void OnAttachedToVisualTree()
    {
        UpdateDestLabel();
        UpdateSelectedLabel();
        TryRestoreLastBranchFromDisk();
    }

    public async Task StartInitialDownloadAsync()
    {
        try
        {
            if (PickFolderAsync == null)
            {
                ProgressText = "Folder picker not available.";
                return;
            }

            var pickedPath = await PickFolderAsync();
            if (pickedPath == null)
            {
                ProgressText = "Canceled.";
                StatusChanged?.Invoke(this, "Canceled.");
                return;
            }

            var resolvedParent = TryResolveParentRootFromAnyFolder(pickedPath);

            if (!string.IsNullOrWhiteSpace(resolvedParent))
            {
                _currentRepoRoot = resolvedParent;
                _baseDestFolder = resolvedParent;
            }
            else
            {
                _currentRepoRoot = null;
                _baseDestFolder = pickedPath;
            }

            UpdateDestLabel();
            TryRestoreLastBranchFromDisk();
            StatusChanged?.Invoke(this, "Location updated.");

            await GetOrUpdateFilesAsync(UpdateMode.KeepLocalChanges);
        }
        catch (Exception ex)
        {
            ProgressText = "Initial download failed: " + ex.Message;
            StatusChanged?.Invoke(this, "Initial download failed: " + ex.Message);
        }
    }

    // ----- Commands -----

    [RelayCommand]
    private async Task PickDestAsync()
    {
        try
        {
            if (PickFolderAsync == null)
            {
                ProgressText = "Folder picker not available.";
                return;
            }

            var pickedPath = await PickFolderAsync();
            if (pickedPath == null) return;

            var resolvedParent = TryResolveParentRootFromAnyFolder(pickedPath);

            if (!string.IsNullOrWhiteSpace(resolvedParent))
            {
                _currentRepoRoot = resolvedParent;
                _baseDestFolder = resolvedParent;
            }
            else
            {
                _currentRepoRoot = null;
                _baseDestFolder = pickedPath;
            }

            UpdateDestLabel();
            TryRestoreLastBranchFromDisk();
            StatusChanged?.Invoke(this, "Location updated.");
        }
        catch (Exception ex)
        {
            ProgressText = "Pick folder failed: " + ex.Message;
            StatusChanged?.Invoke(this, "Pick folder failed: " + ex.Message);
        }
    }

    [RelayCommand]
    private async Task GetFilesAsync()
    {
        await GetOrUpdateFilesAsync(UpdateMode.KeepLocalChanges);
    }

    [RelayCommand]
    private async Task UpdateKeepLocalAsync()
    {
        await GetOrUpdateFilesAsync(UpdateMode.KeepLocalChanges);
    }

    [RelayCommand]
    private async Task UpdateDiscardLocalAsync()
    {
        await GetOrUpdateFilesAsync(UpdateMode.DiscardLocalChanges);
    }

    [RelayCommand]
    private void Cancel()
    {
        try { _cts?.Cancel(); } catch { }
        try { _cts?.Dispose(); } catch { }
        _cts = null;

        _git.TryCancelRunningProcess();
        SetButtonsBusy(false);
    }

    /// <summary>Cancel any in-flight operation, dispose the old CTS, and create a fresh one.</summary>
    private void ResetCts()
    {
        _cts?.Cancel();
        try { _cts?.Dispose(); } catch { }
        _git.TryCancelRunningProcess();
        _cts = new CancellationTokenSource();
    }

    private CancellationToken BeginOperation()
    {
        ResetCts();
        return _cts?.Token ?? CancellationToken.None;
    }

    [RelayCommand]
    private async Task SendContributionAsync()
    {
        await SendContributionLocalAsync();
    }

    [RelayCommand]
    private async Task AuthorizeAsync()
    {
        await AuthorizeGitHubAsync();
    }

    [RelayCommand]
    private async Task PushPrAsync()
    {
        await PushAndCreatePrAsync();
    }

    [RelayCommand]
    private async Task ShareAllAsync()
    {
        await ShareAllInternalAsync();
    }

    [RelayCommand]
    private async Task FetchMergeCommunityAsync()
    {
        await FetchAndMergeCommunityDataAsync();
    }

    [RelayCommand]
    private async Task SyncAsync()
    {
        // Phase 1: Clone or update repo texts (safe, keeps local changes)
        await GetOrUpdateFilesAsync(UpdateMode.KeepLocalChanges);

        // Check repo is available after clone/update
        var transDir = GetTranslationRepoDir();
        if (!Directory.Exists(transDir) || !Directory.Exists(Path.Combine(transDir, ".git")))
            return; // Clone/update failed or was canceled

        // Phase 2: Share all community data
        string? shareError = null;
        try
        {
            await ShareAllInternalAsync();
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            shareError = ex.Message;
        }

        // Phase 3: Fetch + merge others' community data
        try
        {
            await FetchAndMergeCommunityDataAsync();
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            AppendLog("[sync] Fetch+merge error: " + ex.Message);
        }

        if (shareError != null)
        {
            AppendLog("[sync] Note: share phase had an error: " + shareError);
        }

        // Phase 4: If the currently selected file has translation changes, create a PR for it only
        try
        {
            if (!string.IsNullOrWhiteSpace(_selectedRelPath) && !string.IsNullOrWhiteSpace(_githubLogin))
            {
                var ct = _cts?.Token ?? CancellationToken.None;
                var prog = new Progress<string>(line => Dispatcher.UIThread.Post(() => AppendLog(line)));

                string? savedAbsPath = null;
                if (EnsureTranslatedForSelectedRequested != null)
                {
                    foreach (var fn in EnsureTranslatedForSelectedRequested.GetInvocationList().Cast<Func<string, Task<string?>>>())
                    {
                        savedAbsPath = await fn(_selectedRelPath);
                        if (savedAbsPath == null) break;
                    }
                }

                if (savedAbsPath == null)
                {
                    AppendLog($"[sync] Translation PR step skipped: could not prepare translation for {_selectedRelPath}.");
                }
                else
                {
                    var selectedRepoRel = NormalizeRel(Path.GetRelativePath(transDir, savedAbsPath));
                    var status = await _git.GetStatusPorcelainAsync(transDir, ct);
                    var hasSelectedFileChanges = status.Any(line =>
                        line.Contains(selectedRepoRel, StringComparison.OrdinalIgnoreCase));

                    if (hasSelectedFileChanges)
                    {
                        AppendLog($"\n[sync] Translation change detected in {_selectedRelPath} - creating pull request...");
                        ProgressText = "Submitting translation for " + _selectedRelPath + "...";

                        // Stage ONLY the canonical selected translated file for the contribution PR.
                        var stage = await _git.StagePathAsync(transDir, selectedRepoRel, prog, ct);
                        if (!stage.Success)
                            throw new InvalidOperationException(stage.Error ?? "Failed to stage selected translated file.");

                        var branchName = $"contrib/{_githubLogin}/{DateTime.UtcNow:yyyyMMdd-HHmmss}";
                        var msg = $"{_githubLogin}: {Path.GetFileNameWithoutExtension(_selectedRelPath)} translation update";

                        await _git.EnsureUserIdentityAsync(transDir, _username, prog, ct);
                        await _git.EnsureLocalExcludeAsync(transDir, LocalIgnorePatterns, prog, ct);
                        await _git.EnsureLineEndingConfigAsync(transDir, prog, ct);

                        var currentBranch = await _git.GetCurrentBranchAsync(transDir, ct);
                        var stash = await _git.StashKeepIndexAsync(transDir, "sync-auto-stash", prog, ct);
                        await _git.SwitchCreateBranchAsync(transDir, branchName, prog, ct);
                        await _git.CommitAsync(transDir, msg, prog, ct);

                        _lastContribBranch = branchName;

                        await PushAndCreatePrAsync();

                        try
                        {
                            await _git.SwitchBranchAsync(transDir, currentBranch ?? "main", prog, ct);
                            if (stash.Success)
                                await _git.StashPopAsync(transDir, prog, ct);
                        }
                        catch { }

                        AppendLog("[sync] Translation PR created.");
                    }
                    else
                    {
                        AppendLog($"[sync] No canonical translation diff detected for {_selectedRelPath}.");
                    }
                }
            }
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            AppendLog("[sync] Translation PR step failed (non-critical): " + ex.Message);
        }

        AppendLog("\n[sync] Sync finished.");
        ProgressText = shareError == null ? "Sync complete." : "Sync complete (share had errors, see log).";
        StatusChanged?.Invoke(this, ProgressText);
    }

    [RelayCommand]
    private async Task PanicResetAsync()
    {
        await PanicButtonAsync();
    }

    // ----- Private: Update mode enum -----

    private enum UpdateMode
    {
        KeepLocalChanges,
        DiscardLocalChanges
    }

    // ----- Private: Clone / Update -----

    private async Task GetOrUpdateFilesAsync(UpdateMode mode)
    {
        var ct = BeginOperation();

        SetButtonsBusy(true);
        ClearLog();

        var parentDir = GetTargetRepoDir();
        var originalsDir = GetOriginalsRepoDir();
        var translationDir = GetTranslationRepoDir();

        try
        {
            AppendLog($"[originals] {OriginalsRepoUrl}");
            AppendLog($"[translations] {TranslationRepoUrl}");
            AppendLog($"[path] {parentDir}");

            ProgressText = "Checking git\u2026";
            StatusChanged?.Invoke(this, "Checking for Git\u2026");
            var gitOk = await _git.CheckGitAvailableAsync(ct);
            if (!gitOk)
            {
                ProgressText = "Git not found (portable/system).";
                AppendLog("[error] git not found");
                AppendLog("[hint] If using bundled Portable Git, make sure the PortableGit folder is included beside the app.");
                StatusChanged?.Invoke(this, "Git not found.");
                return;
            }

            var prog = new Progress<string>(line => Dispatcher.UIThread.Post(() => AppendLog(line)));

            bool originalsExist = Directory.Exists(originalsDir) && Directory.Exists(Path.Combine(originalsDir, ".git"));
            bool translationsExist = Directory.Exists(translationDir) && Directory.Exists(Path.Combine(translationDir, ".git"));

            // Both repos exist -> UPDATE
            if (originalsExist && translationsExist)
            {
                // --- Update originals repo (safe to hard-reset, no user data) ---
                AppendLog("\n--- Updating originals repo ---");
                await _git.EnsureLocalExcludeAsync(originalsDir, LocalIgnorePatterns, prog, ct);
                await _git.EnsureLineEndingConfigAsync(originalsDir, prog, ct);

                ProgressText = "Fetching originals\u2026";
                var fetchOrig = await _git.FetchAsync(originalsDir, prog, ct);
                if (!fetchOrig.Success)
                {
                    ProgressText = "Fetch originals failed.";
                    AppendLog("[error] " + (fetchOrig.Error ?? "unknown error"));
                    StatusChanged?.Invoke(this, "Fetch originals failed.");
                    return;
                }

                // Originals: always hard-reset (no user data)
                await DoUpdateDiscardLocalAsync(originalsDir, prog, ct);

                // --- Update translations repo (preserve user data) ---
                AppendLog("\n--- Updating translations repo ---");
                await _git.EnsureLocalExcludeAsync(translationDir, LocalIgnorePatterns, prog, ct);
                await _git.EnsureLineEndingConfigAsync(translationDir, prog, ct);

                ProgressText = "Fetching translations\u2026";
                var fetchTrans = await _git.FetchAsync(translationDir, prog, ct);
                if (!fetchTrans.Success)
                {
                    ProgressText = "Fetch translations failed.";
                    AppendLog("[error] " + (fetchTrans.Error ?? "unknown error"));
                    StatusChanged?.Invoke(this, "Fetch translations failed.");
                    return;
                }

                var ab = await _git.GetAheadBehindAsync(translationDir, "origin/main", ct);
                AppendLog($"[git] translations ahead/behind vs origin/main: ahead={ab.ahead}, behind={ab.behind}");

                if (ab.ahead > 0)
                {
                    string rescueBranch = "rescue/local-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    AppendLog("[safety] local commits detected in translations. Creating rescue branch: " + rescueBranch);

                    var rescue = await _git.CreateBranchAtHeadAsync(translationDir, rescueBranch, prog, ct);
                    if (!rescue.Success)
                    {
                        ProgressText = "Update blocked (could not create rescue branch).";
                        AppendLog("[error] " + (rescue.Error ?? "unknown error"));
                        AppendLog("[hint] Translations repo has local commits. Create/push a PR first, or fix branch state manually.");
                        StatusChanged?.Invoke(this, "Update blocked (rescue branch failed).");
                        return;
                    }

                    AppendLog("[safety] rescue branch saved: " + rescueBranch);
                    AppendLog("[note] Update will continue on main. Your local commits remain on that rescue branch.");
                }

                if (mode == UpdateMode.DiscardLocalChanges)
                {
                    bool confirmDiscard = await ConfirmDangerousUpdateDiscardAsync();
                    if (!confirmDiscard)
                    {
                        ProgressText = "Canceled.";
                        AppendLog("[cancel] user canceled discard update");
                        return;
                    }

                    await DoUpdateDiscardLocalAsync(translationDir, prog, ct);
                }
                else
                {
                    await DoUpdateKeepLocalAsync(translationDir, prog, ct);
                }

                // Now also clone/update the OpenZenTexts pair so existing
                // CBETA-only users automatically pick up the parallel free
                // corpus on their next sync.
                await EnsureOpenZenReposAsync(prog, ct);

                _currentRepoRoot = parentDir;
                _baseDestFolder = parentDir;
                UpdateDestLabel();
                TryRestoreLastBranchFromDisk();

                AppPaths.InvalidateDiscoveryCache(parentDir);
                RootCloned?.Invoke(this, parentDir);
                StatusChanged?.Invoke(this, mode == UpdateMode.KeepLocalChanges
                    ? "Repos updated (kept local changes)."
                    : "Repos updated (discarded local changes).");
                return;
            }

            // Missing repos -> CLONE
            if (!Directory.Exists(parentDir))
                Directory.CreateDirectory(parentDir);

            // Clone originals repo
            if (!originalsExist)
            {
                if (Directory.Exists(originalsDir) && Directory.EnumerateFileSystemEntries(originalsDir).Any())
                {
                    ProgressText = "Folder exists but is not a Git repo: " + originalsDir;
                    AppendLog("[error] originals target folder exists and is not a git repo");
                    AppendLog("Pick a different location or delete that folder.");
                    StatusChanged?.Invoke(this, "Originals folder exists but is not a Git repo.");
                    return;
                }

                ProgressText = "Cloning originals (this may take several minutes)\u2026";
                StatusChanged?.Invoke(this, "Cloning originals repo (1/2)\u2026 ~2.5 GB download.");
                AppendLog("\n--- Cloning originals repo ---");
                var cloneOrigProg = new Progress<string>(line =>
                {
                    Dispatcher.UIThread.Post(() => AppendLog(line));
                    // Forward git progress lines to status so the tooltip can show them
                    if (line.Contains("Receiving") || line.Contains("Resolving") || line.Contains("%"))
                        StatusChanged?.Invoke(this, "Cloning originals (1/2): " + line.Trim());
                });
                var cloneOrig = await _git.CloneAsync(OriginalsRepoUrl, originalsDir, cloneOrigProg, ct);
                if (!cloneOrig.Success)
                {
                    ProgressText = "Clone originals failed.";
                    AppendLog("[error] " + (cloneOrig.Error ?? "unknown error"));
                    StatusChanged?.Invoke(this, "Clone originals failed.");
                    return;
                }

                await _git.EnsureLocalExcludeAsync(originalsDir, LocalIgnorePatterns, prog, ct);
                await _git.EnsureLineEndingConfigAsync(originalsDir, prog, ct);
                AppendLog("[ok] originals clone complete: " + originalsDir);
                StatusChanged?.Invoke(this, "Originals downloaded. Cloning translations\u2026");
            }

            // Clone translations repo
            if (!translationsExist)
            {
                if (Directory.Exists(translationDir) && Directory.EnumerateFileSystemEntries(translationDir).Any())
                {
                    ProgressText = "Folder exists but is not a Git repo: " + translationDir;
                    AppendLog("[error] translations target folder exists and is not a git repo");
                    AppendLog("Pick a different location or delete that folder.");
                    StatusChanged?.Invoke(this, "Translations folder exists but is not a Git repo.");
                    return;
                }

                ProgressText = "Cloning translations (2/2)\u2026";
                StatusChanged?.Invoke(this, "Cloning translations repo (2/2)\u2026");
                AppendLog("\n--- Cloning translations repo ---");
                var cloneTrans = await _git.CloneAsync(TranslationRepoUrl, translationDir, prog, ct);
                if (!cloneTrans.Success)
                {
                    ProgressText = "Clone translations failed.";
                    AppendLog("[error] " + (cloneTrans.Error ?? "unknown error"));
                    StatusChanged?.Invoke(this, "Clone translations failed.");
                    return;
                }

                await _git.EnsureLocalExcludeAsync(translationDir, LocalIgnorePatterns, prog, ct);
                await _git.EnsureLineEndingConfigAsync(translationDir, prog, ct);
                AppendLog("[ok] translations clone complete: " + translationDir);
            }

            // Now also clone the OpenZenTexts pair so the user has both
            // corpora available from the start. New users get both at once;
            // existing CBETA-only users (who already cloned CBETA earlier
            // and don't go through this branch) pick it up via the update
            // path's EnsureOpenZenReposAsync call above.
            await EnsureOpenZenReposAsync(prog, ct);

            ProgressText = "Done. CBETA + OpenZenTexts ready.";
            AppendLog("[ok] clone complete");

            _currentRepoRoot = parentDir;
            _baseDestFolder = parentDir;
            UpdateDestLabel();
            TryRestoreLastBranchFromDisk();

            AppPaths.InvalidateDiscoveryCache(parentDir);
            RootCloned?.Invoke(this, parentDir);
            StatusChanged?.Invoke(this, "Repos cloned (CBETA + OpenZenTexts).");
        }
        catch (OperationCanceledException)
        {
            ProgressText = "Canceled.";
            AppendLog("[cancel] canceled");
            StatusChanged?.Invoke(this, "Canceled.");
        }
        catch (Exception ex)
        {
            ProgressText = "Failed: " + ex.Message;
            AppendLog("[error] " + ex);
            StatusChanged?.Invoke(this, "Failed: " + ex.Message);
        }
        finally
        {
            SetButtonsBusy(false);
        }
    }

    private async Task DoUpdateKeepLocalAsync(string repoDir, IProgress<string> prog, CancellationToken ct)
    {
        var changedPaths = await _git.GetChangedPathsForBackupAsync(repoDir, includePrefixes: null, ct);
        changedPaths = changedPaths
            .Concat(GetAlwaysPreservedUpdatePaths(repoDir))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        AppendLog($"[scan] changed paths to preserve: {changedPaths.Length}");

        string backupDir = CreateBackupDir();
        bool backupCreated = false;

        try
        {
            if (changedPaths.Length > 0)
            {
                ProgressText = "Backing up your local changes\u2026";
                AppendLog("[step] backup changed files -> " + backupDir);

                foreach (var rel in changedPaths)
                {
                    ct.ThrowIfCancellationRequested();

                    var src = Path.Combine(repoDir, rel.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(src))
                    {
                        AppendLog("[skip] missing (likely deleted/renamed): " + rel);
                        continue;
                    }

                    var dst = Path.Combine(backupDir, rel.Replace('/', Path.DirectorySeparatorChar));
                    var dstDir = Path.GetDirectoryName(dst);
                    if (!string.IsNullOrWhiteSpace(dstDir))
                        Directory.CreateDirectory(dstDir);

                    File.Copy(src, dst, overwrite: true);
                    AppendLog("[backup] " + rel);
                    backupCreated = true;
                }
            }
            else
            {
                AppendLog("[scan] no local file changes detected");
            }

            ProgressText = "Resetting to latest files\u2026";
            AppendLog("[step] reset --hard origin/main");
            var reset = await _git.HardResetToRemoteMainAsync(repoDir, "origin", "main", prog, ct);
            if (!reset.Success)
            {
                ProgressText = "Update failed (reset).";
                AppendLog("[error] " + (reset.Error ?? "unknown error"));
                throw new InvalidOperationException(reset.Error ?? "reset failed");
            }

            AppendLog("[step] clean -fd");
            var clean = await _git.CleanUntrackedAsync(repoDir, prog, ct);
            if (!clean.Success)
            {
                ProgressText = "Update failed (clean).";
                AppendLog("[error] " + (clean.Error ?? "unknown error"));
                throw new InvalidOperationException(clean.Error ?? "clean failed");
            }

            if (backupCreated)
            {
                ProgressText = "Restoring your local files\u2026";
                AppendLog("[step] restore backup files");

                foreach (var file in Directory.EnumerateFiles(backupDir, "*", SearchOption.AllDirectories))
                {
                    ct.ThrowIfCancellationRequested();

                    var rel = Path.GetRelativePath(backupDir, file)
                        .Replace('\\', '/');

                    var dst = Path.Combine(repoDir, rel.Replace('/', Path.DirectorySeparatorChar));
                    var dstDir = Path.GetDirectoryName(dst);
                    if (!string.IsNullOrWhiteSpace(dstDir))
                        Directory.CreateDirectory(dstDir);

                    File.Copy(file, dst, overwrite: true);
                    AppendLog("[restore] " + rel);
                }

                AppendLog("[ok] local file changes restored on top of latest repo");
                AppendLog("[note] If upstream changed the same file, your local version was kept (copied back).");
            }

            ProgressText = "Up to date (kept local changes).";
            AppendLog("[ok] update complete (kept local changes)");
        }
        finally
        {
            TryDeleteDirectory(backupDir);
        }
    }

    private async Task DoUpdateDiscardLocalAsync(string repoDir, IProgress<string> prog, CancellationToken ct)
    {
        ProgressText = "Discarding local changes and updating\u2026";

        AppendLog("[step] reset --hard origin/main");
        var reset = await _git.HardResetToRemoteMainAsync(repoDir, "origin", "main", prog, ct);
        if (!reset.Success)
        {
            ProgressText = "Update failed (reset).";
            AppendLog("[error] " + (reset.Error ?? "unknown error"));
            return;
        }

        AppendLog("[step] clean -fd");
        var clean = await _git.CleanUntrackedAsync(repoDir, prog, ct);
        if (!clean.Success)
        {
            ProgressText = "Update failed (clean).";
            AppendLog("[error] " + (clean.Error ?? "unknown error"));
            return;
        }

        ProgressText = "Up to date (discarded local changes).";
        AppendLog("[ok] update complete (discarded local changes)");
    }

    private async Task<bool> ConfirmDangerousUpdateDiscardAsync()
    {
        if (ConfirmAsync == null) return false;

        return await ConfirmAsync(
            "Update and Discard Local Changes",
            "This will update the repo to the newest files and ERASE all uncommitted local changes.\n\n" +
            "Your local commits are kept on a rescue branch if they exist.\n" +
            "But unsaved/uncommitted file edits will be lost.\n\n" +
            "Use this when you want a clean, fresh copy.",
            "Yes, update and discard local changes",
            "No, keep my changes");
    }

    private async Task<bool> ConfirmCommunityShareAsync(IReadOnlyList<string> changedFiles)
    {
        if (ConfirmAsync == null)
            return true;

        var preview = string.Join("\n", changedFiles.Take(8).Select(f => $"- {f}"));
        if (changedFiles.Count > 8)
            preview += $"\n- ... and {changedFiles.Count - 8} more";

        var message =
            "You are about to share community data changes.\n\n" +
            "These community contributions will be reviewed by a real person before merge. " +
            "Please discuss substantial changes on the /r/zen forums so reviewers have context.\n\n" +
            "Files to share:\n" + preview + "\n\n" +
            "Personal translation PRs are handled separately and will not be blocked by this prompt.";

        return await ConfirmAsync(
            "Share Community Data?",
            message,
            "Share community data",
            "Cancel");
    }

    // ----- Private: Panic -----

    private async Task PanicButtonAsync()
    {
        var ct = BeginOperation();

        SetButtonsBusy(true);
        ClearLog();

        try
        {
            var repoDir = GetTranslationRepoDir();
            if (!Directory.Exists(repoDir) || !Directory.Exists(Path.Combine(repoDir, ".git")))
            {
                ProgressText = "Repo not ready. Click Get/Update first.";
                AppendLog("[error] repo not found / not a git working tree");
                return;
            }

            ProgressText = "Checking git\u2026";
            var gitOk = await _git.CheckGitAvailableAsync(ct);
            if (!gitOk)
            {
                ProgressText = "Git not found.";
                AppendLog("[error] git not found");
                return;
            }

            if (ConfirmAsync == null)
            {
                ProgressText = "No confirmation dialog available.";
                return;
            }

            bool confirm = await ConfirmAsync(
                "Discard local changes",
                "This will ERASE all your local, uncommitted changes in the repo.\n\n" +
                "It does:\n" +
                "  1) git stash push -u\n" +
                "  2) git stash drop\n\n" +
                "Result: local edits are gone.\n" +
                "Use this only if you want a clean working tree.",
                "Yes, erase my local changes",
                "No, keep my changes");

            if (!confirm)
            {
                ProgressText = "Canceled.";
                AppendLog("[cancel] user chose safety");
                return;
            }

            var prog = new Progress<string>(line => Dispatcher.UIThread.Post(() => AppendLog(line)));

            ProgressText = "PANIC: stashing\u2026";
            AppendLog("[panic] git stash push -u");
            var stash = await RunGitAsync(repoDir, "stash", "push", "-u", "-m", "panic-button", progress: prog, ct: ct);
            if (!stash.Success)
            {
                ProgressText = "Panic failed (stash).";
                AppendLog("[error] " + stash.Error);
                return;
            }

            ProgressText = "PANIC: dropping stash\u2026";
            AppendLog("[panic] git stash drop");
            var drop = await RunGitAsync(repoDir, "stash", "drop", progress: prog, ct: ct);
            if (!drop.Success)
            {
                ProgressText = "Panic partial failure (drop).";
                AppendLog("[error] " + drop.Error);
                AppendLog("[hint] Your changes might still be in stash. Try: git stash list");
                return;
            }

            ProgressText = "Repo cleaned. You're safe now.";
            AppendLog("[ok] panic complete: local uncommitted changes erased");
            StatusChanged?.Invoke(this, "Panic complete: repo cleaned.");
        }
        catch (OperationCanceledException)
        {
            ProgressText = "Canceled.";
            AppendLog("[cancel] canceled");
        }
        catch (Exception ex)
        {
            ProgressText = "Panic failed: " + ex.Message;
            AppendLog("[error] " + ex);
        }
        finally
        {
            SetButtonsBusy(false);
        }
    }

    // ----- Private: 1) Local commit -----

    private async Task SendContributionLocalAsync()
    {
        var ct = BeginOperation();

        SetButtonsBusy(true);
        ClearLog();

        try
        {
            var repoDir = GetTranslationRepoDir();
            if (!Directory.Exists(repoDir) || !Directory.Exists(Path.Combine(repoDir, ".git")))
            {
                ProgressText = "Repo not ready. Click Get/Update first.";
                AppendLog("[error] repo not found / not a git working tree");
                return;
            }

            if (string.IsNullOrWhiteSpace(_selectedRelPath))
            {
                ProgressText = "Select a file first.";
                AppendLog("[error] no selected file");
                return;
            }

            var relPath = NormalizeRel(_selectedRelPath);

            string? absTarget = null;
            if (EnsureTranslatedForSelectedRequested != null)
            {
                ProgressText = "Preparing translated XML\u2026";
                foreach (var fn in EnsureTranslatedForSelectedRequested.GetInvocationList().Cast<Func<string, Task<string?>>>())
                {
                    absTarget = await fn(relPath);
                    if (absTarget == null) break;
                }

                if (absTarget == null)
                {
                    ProgressText = "Preparation failed. Save in Edit tab and retry.";
                    AppendLog("[error] failed to materialize translated XML for selected file");
                    return;
                }
            }

            // Compute repo-relative path from the actual file location
            var repoRel = absTarget != null
                ? NormalizeRel(Path.GetRelativePath(repoDir, absTarget))
                : NormalizeRel($"{RepoTranslatedRoot}/{relPath}");

            AppendLog("[commit] Source file: " + relPath);
            AppendLog("[commit] Staging: " + repoRel);

            if (absTarget == null || !File.Exists(absTarget))
            {
                ProgressText = "Translated file does not exist in repo yet. Save it first.";
                AppendLog("[error] missing: " + (absTarget ?? repoRel));
                return;
            }

            ProgressText = "Checking git\u2026";
            var gitOk = await _git.CheckGitAvailableAsync(ct);
            if (!gitOk)
            {
                ProgressText = "Git not found.";
                AppendLog("[error] git not found");
                return;
            }

            var prog = new Progress<string>(line => Dispatcher.UIThread.Post(() => AppendLog(line)));

            await _git.EnsureLocalExcludeAsync(repoDir, LocalIgnorePatterns, prog, ct);
            await _git.EnsureLineEndingConfigAsync(repoDir, prog, ct);
            await _git.EnsureUserIdentityAsync(repoDir, _username, prog, ct);

            var status = await _git.GetStatusPorcelainAsync(repoDir, ct);

            bool targetMentioned = status.Any(l =>
                l.EndsWith(" " + repoRel, StringComparison.OrdinalIgnoreCase) ||
                l.EndsWith("\t" + repoRel, StringComparison.OrdinalIgnoreCase) ||
                l.Contains(repoRel, StringComparison.OrdinalIgnoreCase));

            if (!targetMentioned)
            {
                ProgressText = "No changes detected for selected file (git status).";
                AppendLog("[warn] git status does not show changes for: " + repoRel);
                AppendLog("If you edited it, ensure you saved, and that you are using the repo clone as root.");
                return;
            }

            string originalBranch = await _git.GetCurrentBranchAsync(repoDir, ct);
            AppendLog("[git] current branch: " + originalBranch);

            string msg = CommitMessage.Trim();
            if (string.IsNullOrWhiteSpace(msg))
                msg = BuildDefaultTranslationCommitMessage(relPath);

            string branchName = MakeBranchName(relPath);

            ProgressText = "Staging selected file\u2026";
            AppendLog("[step] git add -- " + repoRel);
            var stage = await _git.StagePathAsync(repoDir, repoRel, prog, ct);
            if (!stage.Success)
            {
                ProgressText = "Stage failed.";
                AppendLog("[error] " + stage.Error);
                return;
            }

            ProgressText = "Stashing other work\u2026";
            AppendLog("[step] git stash push -u -k");
            var stash = await _git.StashKeepIndexAsync(repoDir, "readzen-autostash", prog, ct);
            if (!stash.Success)
            {
                ProgressText = "Stash failed.";
                AppendLog("[error] " + stash.Error);
                return;
            }

            ProgressText = "Creating branch\u2026";
            AppendLog("[step] new branch: " + branchName);
            var br = await _git.SwitchCreateBranchAsync(repoDir, branchName, prog, ct);
            if (!br.Success)
            {
                ProgressText = "Branch create failed.";
                AppendLog("[error] " + br.Error);
                await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                return;
            }

            ProgressText = "Committing\u2026";
            AppendLog("[step] commit message: " + msg);
            var commit = await _git.CommitAsync(repoDir, msg, prog, ct);
            if (!commit.Success)
            {
                ProgressText = "Commit failed.";
                AppendLog("[error] " + commit.Error);
                await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                return;
            }

            _lastContribBranch = branchName;
            PersistLastBranchToDisk(repoDir, branchName);

            ProgressText = "Local commit created.";
            AppendLog("[ok] created single-file commit on branch: " + branchName);
            AppendLog("[next] 2) Authorize GitHub, then 3) Push + Create PR");

            ProgressText = "Restoring your other work\u2026";
            await SafeRestoreAsync(repoDir, originalBranch, prog, ct);

            StatusChanged?.Invoke(this, "Local commit ready on branch: " + branchName);
        }
        catch (OperationCanceledException)
        {
            ProgressText = "Canceled.";
            AppendLog("[cancel] canceled");
        }
        catch (Exception ex)
        {
            ProgressText = "Failed: " + ex.Message;
            AppendLog("[error] " + ex);
        }
        finally
        {
            SetButtonsBusy(false);
        }
    }

    // ----- Private: 2) Auth -----

    private async Task AuthorizeGitHubAsync()
    {
        var ct = BeginOperation();

        SetButtonsBusy(true);
        ClearLog();

        try
        {
            var prog = new Progress<string>(line => Dispatcher.UIThread.Post(() => AppendLog(line)));

            ProgressText = "Authorizing\u2026";
            var token = await _auth.AuthorizeDeviceFlowAsync(prog, ct, MakeDeviceCodeCallback());
            if (token == null)
            {
                ProgressText = "Auth failed.";
                return;
            }

            _githubAccessToken = token.access_token;
            var me = await _api.GetMeAsync(_githubAccessToken, ct);
            _githubLogin = me?.login;

            AppendLog("[auth] user: " + (_githubLogin ?? "(unknown)"));
            ProgressText = "Authorized.";
            StatusChanged?.Invoke(this, "GitHub authorized.");
            if (!string.IsNullOrWhiteSpace(_githubLogin))
            {
                GitHubAuthCompleted?.Invoke(this, (_githubAccessToken!, _githubLogin!));
                FireDeviceFlowCompleted();
            }
        }
        catch (OperationCanceledException)
        {
            ProgressText = "Canceled.";
            AppendLog("[cancel] canceled");
        }
        catch (Exception ex)
        {
            ProgressText = "Auth failed: " + ex.Message;
            AppendLog("[error] " + ex);
        }
        finally
        {
            FireDeviceFlowCompleted();
            SetButtonsBusy(false);
        }
    }

    // ----- Private: 3) Push + PR -----

    private async Task PushAndCreatePrAsync()
    {
        var ct = BeginOperation();

        SetButtonsBusy(true);
        ClearLog();

        try
        {
            var repoDir = GetTranslationRepoDir();
            if (!Directory.Exists(repoDir) || !Directory.Exists(Path.Combine(repoDir, ".git")))
            {
                ProgressText = "Repo not ready. Click Get/Update first.";
                AppendLog("[error] repo not found / not a git working tree");
                return;
            }

            if (string.IsNullOrWhiteSpace(_lastContribBranch))
                TryRestoreLastBranchFromDisk();

            if (string.IsNullOrWhiteSpace(_lastContribBranch))
            {
                ProgressText = "Step 1 not done yet.";
                AppendLog("[error] no prepared branch found");
                AppendLog("Do: 1) Create local commit (single file) first.");
                return;
            }

            var prog = new Progress<string>(line => Dispatcher.UIThread.Post(() => AppendLog(line)));

            if (string.IsNullOrWhiteSpace(_githubAccessToken) || string.IsNullOrWhiteSpace(_githubLogin))
            {
                ProgressText = "Need GitHub auth first\u2026";
                AppendLog("[step] authorize");
                var token = await _auth.AuthorizeDeviceFlowAsync(prog, ct, MakeDeviceCodeCallback());
                if (token == null)
                {
                    ProgressText = "Auth failed.";
                    return;
                }

                _githubAccessToken = token.access_token;
                var me = await _api.GetMeAsync(_githubAccessToken, ct);
                _githubLogin = me?.login;

                AppendLog("[auth] user: " + (_githubLogin ?? "(unknown)"));
                if (string.IsNullOrWhiteSpace(_githubLogin))
                {
                    ProgressText = "Auth ok but could not read username.";
                    AppendLog("[error] GET /user failed");
                    return;
                }

                GitHubAuthCompleted?.Invoke(this, (_githubAccessToken!, _githubLogin!));
                FireDeviceFlowCompleted();
            }

            bool isUpstreamOwner = string.Equals(_githubLogin, UpstreamOwner, StringComparison.OrdinalIgnoreCase);

            await ScrubTokenizedForkRemoteIfAny(repoDir, prog, ct);

            string remoteName;
            string remoteUrlClean;
            string prHeadOwner;

            if (isUpstreamOwner)
            {
                AppendLog("[mode] upstream owner detected -> no fork");
                remoteName = "origin";
                remoteUrlClean = TranslationRepoUrl;
                prHeadOwner = UpstreamOwner;
            }
            else
            {
                ProgressText = "Ensuring fork\u2026";

                bool forkExists = await _api.ForkExistsAsync(_githubAccessToken!, _githubLogin!, UpstreamRepo, ct);
                if (!forkExists)
                {
                    AppendLog("[step] create fork");
                    var okFork = await _api.CreateForkAsync(_githubAccessToken!, UpstreamOwner, UpstreamRepo, ct);
                    if (!okFork)
                    {
                        ProgressText = "Fork failed.";
                        AppendLog("[error] fork creation failed");
                        return;
                    }

                    var ready = await _api.WaitForForkAsync(_githubAccessToken!, _githubLogin!, UpstreamRepo, TimeSpan.FromSeconds(60), prog, ct);
                    if (!ready)
                    {
                        ProgressText = "Fork not ready yet.";
                        AppendLog("[error] fork did not appear within timeout");
                        return;
                    }
                }

                remoteName = "fork";
                remoteUrlClean = $"https://github.com/{_githubLogin}/{UpstreamRepo}.git";
                prHeadOwner = _githubLogin!;
            }

            ProgressText = "Configuring remote\u2026";
            AppendLog("[step] remote " + remoteName + " -> " + remoteUrlClean);
            var rem = await _git.EnsureRemoteUrlAsync(repoDir, remoteName, remoteUrlClean, prog, ct);
            if (!rem.Success)
            {
                ProgressText = "Remote failed.";
                AppendLog("[error] " + rem.Error);
                return;
            }

            ProgressText = "Pushing branch\u2026";
            AppendLog("[step] push -u " + remoteName + " " + _lastContribBranch);
            var push = await PushBranchUsingConfiguredAuthAsync(repoDir, remoteName, remoteUrlClean, _lastContribBranch!, prog, ct);
            if (!push.Success)
            {
                ProgressText = "Push failed.";
                AppendLog("[error] " + push.Error);
                AppendPushFailureHints(push.Error);
                return;
            }

            ProgressText = "Creating PR\u2026";

            string head = $"{prHeadOwner}:{_lastContribBranch}";
            string title = CommitMessage.Trim();
            if (string.IsNullOrWhiteSpace(title))
                title = BuildDefaultPrTitle();

            string body =
                "Created by Read Zen.\n\n" +
                $"Branch: `{_lastContribBranch}`";

            var prUrl = await _api.CreatePullRequestAsync(
                _githubAccessToken!,
                UpstreamOwner,
                UpstreamRepo,
                head,
                "main",
                title,
                body,
                ct);

            if (string.IsNullOrWhiteSpace(prUrl))
            {
                ProgressText = "PR failed.";
                AppendLog("[error] create PR failed (API returned null)");
                return;
            }

            AppendLog("[ok] PR created: " + prUrl);
            ProgressText = "PR created.";

            try
            {
                Process.Start(new ProcessStartInfo { FileName = prUrl, UseShellExecute = true });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[GitTabViewModel] Open PR URL failed: {ex.Message}"); }

            StatusChanged?.Invoke(this, "PR created: " + prUrl);
        }
        catch (OperationCanceledException)
        {
            ProgressText = "Canceled.";
            AppendLog("[cancel] canceled");
        }
        catch (Exception ex)
        {
            ProgressText = "Failed: " + ex.Message;
            AppendLog("[error] " + ex);
        }
        finally
        {
            SetButtonsBusy(false);
        }
    }

    private async Task PrepareCommunityShareAsync()
    {
        if (PrepareCommunityShareRequested == null)
            return;

        foreach (var fn in PrepareCommunityShareRequested.GetInvocationList().Cast<Func<Task>>())
            await fn();
    }

    // ----- Private: Unified Share All (TM + termbase + collections + reviews) -----

    private async Task ShareAllInternalAsync()
    {
        var ct = BeginOperation();

        SetButtonsBusy(true);
        ClearLog();

        try
        {
            var repoDir = GetTranslationRepoDir();
            if (!Directory.Exists(repoDir) || !Directory.Exists(Path.Combine(repoDir, ".git")))
            {
                ProgressText = "Repo not ready. Click Get/Update first.";
                AppendLog("[error] repo not found / not a git working tree");
                return;
            }

            ProgressText = "Checking git\u2026";
            var gitOk = await _git.CheckGitAvailableAsync(ct);
            if (!gitOk)
            {
                ProgressText = "Git not found.";
                AppendLog("[error] git not found");
                return;
            }

            var prog = new Progress<string>(line => Dispatcher.UIThread.Post(() => AppendLog(line)));

            // --- Auth (single auth block) ---
            if (string.IsNullOrWhiteSpace(_githubAccessToken) || string.IsNullOrWhiteSpace(_githubLogin))
            {
                ProgressText = "Authorizing GitHub\u2026";
                var token = await _auth.AuthorizeDeviceFlowAsync(prog, ct, MakeDeviceCodeCallback());
                if (token == null)
                {
                    ProgressText = "Auth failed.";
                    return;
                }

                _githubAccessToken = token.access_token;
                var me = await _api.GetMeAsync(_githubAccessToken, ct);
                _githubLogin = me?.login;

                if (string.IsNullOrWhiteSpace(_githubLogin))
                {
                    ProgressText = "Auth ok but could not read username.";
                    return;
                }

                AppendLog("[auth] user: " + _githubLogin);
                GitHubAuthCompleted?.Invoke(this, (_githubAccessToken!, _githubLogin!));
                FireDeviceFlowCompleted();
            }


            await PrepareCommunityShareAsync();

            if (!string.IsNullOrWhiteSpace(_selectedRelPath) && EnsurePersonalTranslatedForSelectedRequested != null)
            {
                ProgressText = "Preparing selected personal translation...";
                var preparedSelectedTranslation = true;
                foreach (var fn in EnsurePersonalTranslatedForSelectedRequested.GetInvocationList().Cast<Func<string, Task<bool>>>())
                {
                    if (!await fn(_selectedRelPath))
                    {
                        preparedSelectedTranslation = false;
                        break;
                    }
                }

                if (!preparedSelectedTranslation)
                {
                    AppendLog($"[warn] could not materialize personal translation for {_selectedRelPath} before share");
                }
            }


            var preShareFingerprints = CaptureCommunityShareFingerprints(repoDir);

            // --- TM dedup ---
            var tmPath = Path.Combine(repoDir, CommunityTmFile);
            if (File.Exists(tmPath))
            {
                ProgressText = "Sorting/deduping approved TM\u2026";
                var kept = await _community.SortAndDedupApprovedTmAsync(repoDir, ct);
                AppendLog($"[dedup] TM: {kept:n0} unique rows after dedup");
            }

            // --- Termbase dedup ---
            var tbPath = Path.Combine(repoDir, CommunityTermbaseFile);
            if (File.Exists(tbPath))
            {
                ProgressText = "Sorting/deduping termbase\u2026";
                var kept = await _community.SortAndDedupTermbaseAsync(repoDir, ct);
                AppendLog($"[dedup] termbase: {kept:n0} entries after dedup");
            }

            // --- Write per-user termbase JSONL ---
            ProgressText = "Writing per-user termbase JSONL\u2026";
            var communityTbDir = TermbaseStorageService.GetCommunityTermbasesDir(repoDir);
            var localTermbase = await _termbaseSvc.LoadAsync(repoDir, ct);
            await _termbaseSvc.WriteUserJsonlAsync(communityTbDir, _githubLogin!, localTermbase, ct);
            var tbJsonlRelPath = Path.Combine("community", "termbases", _githubLogin + ".jsonl").Replace('\\', '/');
            AppendLog($"[step] wrote {localTermbase.Count} term(s) to {tbJsonlRelPath}");

            // --- Scholar collections dedup ---
            var scPath = Path.Combine(repoDir, ScholarCollectionsFile);
            if (File.Exists(scPath))
            {
                ProgressText = "Sorting/deduping scholar collections\u2026";
                var kept = await _community.SortAndDedupScholarCollectionsAsync(repoDir, ct);
                AppendLog($"[dedup] scholar collections: {kept:n0} after dedup");
            }

            // --- Write per-user collections JSONL ---
            ProgressText = "Writing per-user collections JSONL\u2026";
            var communityCollDir = ScholarCollectionsService.GetCommunityCollectionsDir(repoDir);
            var localCollections = await _scholarSvc.LoadAsync(repoDir, ct);
            await _scholarSvc.WriteUserJsonlAsync(communityCollDir, _githubLogin!, localCollections, ct);
            var collJsonlRelPath = Path.Combine("community", "collections", _githubLogin + ".jsonl").Replace('\\', '/');
            AppendLog($"[step] wrote {localCollections.Count} collection(s) to {collJsonlRelPath}");

            // --- Write per-user review JSONL ---
            ProgressText = "Writing per-user review JSONL\u2026";
            var communityReviewsDir = ITranslationReviewService.GetCommunityReviewsDir(repoDir);
            await _translationReview.WriteUserReviewJsonlAsync(communityReviewsDir, _githubLogin!, ct);
            var reviewJsonlRelPath = Path.Combine("community", "reviews", _githubLogin + ".jsonl").Replace('\\', '/');
            AppendLog($"[step] wrote review data to {reviewJsonlRelPath}");

            // --- Write per-user master dates JSONL (custom entries only) ---
            ProgressText = "Writing per-user master dates JSONL\u2026";
            var communityMdDir = IMasterDatesService.GetCommunityMasterDatesDir(repoDir);
            var customMasters = ExtractCustomMasterEntries(repoDir);
            if (customMasters.Count > 0)
            {
                foreach (var cm in customMasters)
                {
                    cm.CreatedBy = _githubLogin;
                    cm.WrittenUtc = DateTimeOffset.UtcNow;
                }
                await _masterDatesSvc.WriteMasterDatesJsonlAsync(communityMdDir, _githubLogin!, customMasters, ct);
                var mdJsonlRelPath = Path.Combine("community", "master-dates", _githubLogin + ".jsonl").Replace('\\', '/');
                AppendLog($"[step] wrote {customMasters.Count} custom master(s) to {mdJsonlRelPath}");
            }
            else
            {
                AppendLog("[info] no custom master dates to share (all entries are canonical)");
            }

            // --- Share tags ---
            try
            {
                var localTags = await _tagService.LoadUserTagsAsync(repoDir, _githubLogin!, ct);
                if (localTags.Count > 0)
                {
                    await _tagService.WriteUserCommunityTagsAsync(repoDir, _githubLogin!, localTags, ct);
                    AppendLog($"[step] wrote {localTags.Count} tags to community/tags/{_githubLogin}.jsonl");
                }

                var vocab = await _tagService.LoadVocabularyAsync(repoDir, _githubLogin!, ct);
                if (vocab?.Tags.Count > 0)
                {
                    await _tagService.WriteUserCommunityVocabularyAsync(repoDir, _githubLogin!, vocab, ct);
                    AppendLog($"[step] wrote tag vocabulary to community/tag-vocabularies/{_githubLogin}.json");
                }
            }
            catch (Exception ex) { AppendLog($"[warn] tag share failed: {ex.Message}"); }

            // --- Ensure .gitattributes has merge=union for all community dirs ---
            var gitattribPath = Path.Combine(repoDir, ".gitattributes");
            string[] mergeRules =
            {
                "community/termbases/*.jsonl merge=union",
                "community/collections/*.jsonl merge=union",
                "community/reviews/*.jsonl merge=union",
                "community/master-dates/*.jsonl merge=union",
                "community/tags/*.jsonl merge=union"
            };
            string gitattribContent = File.Exists(gitattribPath)
                ? await File.ReadAllTextAsync(gitattribPath, Encoding.UTF8, ct)
                : "";

            foreach (var rule in mergeRules)
            {
                if (!gitattribContent.Contains(rule, StringComparison.Ordinal))
                {
                    if (gitattribContent.Length > 0 && !gitattribContent.EndsWith("\n", StringComparison.Ordinal))
                        gitattribContent += "\n";
                    gitattribContent += rule + "\n";
                }
            }

            var currentGitattributes = File.Exists(gitattribPath)
                ? await File.ReadAllTextAsync(gitattribPath, Encoding.UTF8, ct)
                : "";
            if (!string.Equals(currentGitattributes, gitattribContent, StringComparison.Ordinal))
            {
                await File.WriteAllTextAsync(gitattribPath, gitattribContent, new UTF8Encoding(false), ct);
                AppendLog("[step] updated .gitattributes with merge=union rules");
            }

            await _git.EnsureLocalExcludeAsync(repoDir, LocalIgnorePatterns, prog, ct);
            await _git.EnsureLineEndingConfigAsync(repoDir, prog, ct);
            await _git.EnsureUserIdentityAsync(repoDir, _username, prog, ct);

            // --- Check git status for community changes authored by this share ---
            var status = await _git.GetStatusPorcelainAsync(repoDir, ct);
            var trackedSharePaths = GetTrackedCommunitySharePaths(repoDir);
            var changedFiles = new List<string>();
            var postShareFingerprints = CaptureCommunityShareFingerprints(repoDir);

            foreach (var relPath in preShareFingerprints.Keys.Union(postShareFingerprints.Keys, StringComparer.OrdinalIgnoreCase))
            {
                preShareFingerprints.TryGetValue(relPath, out var before);
                postShareFingerprints.TryGetValue(relPath, out var after);
                if (!string.Equals(before, after, StringComparison.Ordinal) && !changedFiles.Contains(relPath))
                    changedFiles.Add(relPath);
            }

            foreach (var line in status)
            {
                var filePath = NormalizePorcelainPath(line);

                if (string.IsNullOrWhiteSpace(filePath))
                    continue;

                if (trackedSharePaths.Contains(filePath) && !changedFiles.Contains(filePath))
                {
                    changedFiles.Add(filePath);
                    continue;
                }

                var normalizedDirPath = filePath.TrimEnd('/');
                if (!string.IsNullOrWhiteSpace(normalizedDirPath))
                {
                    foreach (var trackedPath in trackedSharePaths.Where(p => p.StartsWith(normalizedDirPath + '/', StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!changedFiles.Contains(trackedPath))
                            changedFiles.Add(trackedPath);
                    }
                }
            }

            var autoMergeFiles = changedFiles
                .Where(IsAutoMergeCommunitySharePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var skippedFiles = changedFiles
                .Except(autoMergeFiles, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (skippedFiles.Count > 0)
            {
                AppendLog("[warn] skipping non-auto-merge paths from personal share flow:");
                foreach (var skipped in skippedFiles)
                    AppendLog("[warn] skipped " + skipped);
            }

            if (autoMergeFiles.Count == 0)
            {
                ProgressText = "No auto-mergeable personal share changes found.";
                AppendLog("[warn] share produced no auto-mergeable personal changes to commit");
                return;
            }

            changedFiles = autoMergeFiles;
            // --- Stage, stash, branch, commit, push ---
            string originalBranch = await _git.GetCurrentBranchAsync(repoDir, ct);
            AppendLog("[git] current branch: " + originalBranch);

            string msg = CommitMessage.Trim();
            if (string.IsNullOrWhiteSpace(msg))
                msg = $"{GetUsernameForDefaults()}: Community data update";

            string branchName = $"community/data/{DateTime.Now:yyyyMMdd-HHmmss}";

            // Stage all changed community files
            foreach (var rel in changedFiles)
            {
                ProgressText = "Staging " + rel + "\u2026";
                AppendLog("[step] git add -- " + rel);
                var stage = await _git.StagePathAsync(repoDir, rel, prog, ct);
                if (!stage.Success)
                {
                    AppendLog("[warn] failed to stage " + rel + ": " + stage.Error);
                }
            }

            ProgressText = "Stashing other work\u2026";
            AppendLog("[step] git stash push -u -k");
            var stash = await _git.StashKeepIndexAsync(repoDir, "readzen-community-autostash", prog, ct);
            if (!stash.Success)
            {
                ProgressText = "Stash failed.";
                AppendLog("[error] " + stash.Error);
                return;
            }

            ProgressText = "Creating branch\u2026";
            AppendLog("[step] new branch: " + branchName);
            var br = await _git.SwitchCreateBranchAsync(repoDir, branchName, prog, ct);
            if (!br.Success)
            {
                ProgressText = "Branch create failed.";
                AppendLog("[error] " + br.Error);
                await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                return;
            }

            ProgressText = "Committing\u2026";
            AppendLog("[step] commit: " + msg);
            var commit = await _git.CommitAsync(repoDir, msg, prog, ct);
            if (!commit.Success)
            {
                ProgressText = "Commit failed.";
                AppendLog("[error] " + commit.Error);
                await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                return;
            }

            _lastCommunityBranch = branchName;

            // --- Push ---
            bool isUpstreamOwner = string.Equals(_githubLogin, UpstreamOwner, StringComparison.OrdinalIgnoreCase);

            await ScrubTokenizedForkRemoteIfAny(repoDir, prog, ct);

            string remoteName;
            string remoteUrlClean;

            if (isUpstreamOwner)
            {
                remoteName = "origin";
                remoteUrlClean = TranslationRepoUrl;
                AppendLog("[mode] upstream owner -> push to origin");
            }
            else
            {
                ProgressText = "Ensuring fork\u2026";
                bool forkExists = await _api.ForkExistsAsync(_githubAccessToken!, _githubLogin!, UpstreamRepo, ct);
                if (!forkExists)
                {
                    var okFork = await _api.CreateForkAsync(_githubAccessToken!, UpstreamOwner, UpstreamRepo, ct);
                    if (!okFork)
                    {
                        ProgressText = "Fork failed.";
                        AppendLog("[error] fork creation failed");
                        await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                        return;
                    }

                    var ready = await _api.WaitForForkAsync(_githubAccessToken!, _githubLogin!, UpstreamRepo, TimeSpan.FromSeconds(60), prog, ct);
                    if (!ready)
                    {
                        ProgressText = "Fork not ready yet.";
                        await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                        return;
                    }
                }

                remoteName = "fork";
                remoteUrlClean = $"https://github.com/{_githubLogin}/{UpstreamRepo}.git";
            }

            var rem = await _git.EnsureRemoteUrlAsync(repoDir, remoteName, remoteUrlClean, prog, ct);
            if (!rem.Success)
            {
                ProgressText = "Remote config failed.";
                AppendLog("[error] " + rem.Error);
                await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                return;
            }

            ProgressText = "Pushing community branch\u2026";
            AppendLog("[step] push -u " + remoteName + " " + branchName);
            var push = await PushBranchUsingConfiguredAuthAsync(repoDir, remoteName, remoteUrlClean, branchName, prog, ct);
            if (!push.Success)
            {
                ProgressText = "Push failed.";
                AppendLog("[error] " + push.Error);
                AppendPushFailureHints(push.Error);
                await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                return;
            }

            // --- Auto-create PR for community data (auto-merged by GitHub Action) ---
            if (_githubAccessToken != null)
            {
                try
                {
                    ProgressText = "Creating community data PR\u2026";
                    string prHead = isUpstreamOwner ? branchName : $"{_githubLogin}:{branchName}";
                    string prTitle = $"Community data: {_githubLogin}";
                    string prBody =
                        $"Auto-generated community data sync from **{_githubLogin}**.\n\n" +
                        $"Branch: `{branchName}`\n" +
                        "Files: " + string.Join(", ", changedFiles.Select(f => $"`{f}`")) + "\n\n" +
                        "_This PR will be auto-merged if it only modifies per-user community files._";

                    var prUrl = await _api.CreatePullRequestAsync(
                        _githubAccessToken,
                        UpstreamOwner,
                        UpstreamRepo,
                        prHead,
                        "main",
                        prTitle,
                        prBody,
                        ct);

                    if (!string.IsNullOrWhiteSpace(prUrl))
                    {
                        AppendLog("[ok] PR created: " + prUrl);
                        AppendLog("[info] This PR will be auto-merged by the repository's GitHub Action.");
                    }
                    else
                    {
                        AppendLog("[warn] PR creation returned empty URL; data was pushed but PR may need manual creation.");
                    }
                }
                catch (Exception prEx)
                {
                    AppendLog("[warn] PR creation failed: " + prEx.Message);
                    AppendLog("[info] Data was pushed. You can create a PR manually on GitHub.");
                }
            }

            // --- Restore and finish ---
            ProgressText = "Restoring other work\u2026";
            await SafeRestoreAsync(repoDir, originalBranch, prog, ct);

            ProgressText = "All community data shared.";
            AppendLog("[ok] committed and pushed all community data on branch: " + branchName);
            AppendLog("[files] " + string.Join(", ", changedFiles));

            StatusChanged?.Invoke(this, "Community data shared: " + branchName);
        }
        catch (OperationCanceledException)
        {
            ProgressText = "Canceled.";
            AppendLog("[cancel] canceled");
        }
        catch (Exception ex)
        {
            ProgressText = "Failed: " + ex.Message;
            AppendLog("[error] " + ex);
        }
        finally
        {
            SetButtonsBusy(false);
        }
    }

    // ----- Private: Community Data (combined share = commit + push) -----

    private async Task ShareCommunityDataInternalAsync()
    {
        var ct = BeginOperation();

        SetButtonsBusy(true);
        ClearLog();

        try
        {
            var repoDir = GetTranslationRepoDir();
            if (!Directory.Exists(repoDir) || !Directory.Exists(Path.Combine(repoDir, ".git")))
            {
                ProgressText = "Repo not ready. Click Get/Update first.";
                AppendLog("[error] repo not found / not a git working tree");
                return;
            }

            ProgressText = "Checking git\u2026";
            var gitOk = await _git.CheckGitAvailableAsync(ct);
            if (!gitOk)
            {
                ProgressText = "Git not found.";
                AppendLog("[error] git not found");
                return;
            }

            var tmPath = Path.Combine(repoDir, CommunityTmFile);
            var tbPath = Path.Combine(repoDir, CommunityTermbaseFile);

            bool hasTm = File.Exists(tmPath);
            bool hasTb = File.Exists(tbPath);

            if (!hasTm && !hasTb)
            {
                ProgressText = "No community data files found.";
                AppendLog("[warn] neither " + CommunityTmFile + " nor " + CommunityTermbaseFile + " exists at: " + repoDir);
                AppendLog("[hint] Approve some translations in the Translation tab and use the termbase editor first.");
                return;
            }

            var prog = new Progress<string>(line => Dispatcher.UIThread.Post(() => AppendLog(line)));

            if (hasTm)
            {
                ProgressText = "Sorting/deduping approved TM\u2026";
                var kept = await _community.SortAndDedupApprovedTmAsync(repoDir, ct);
                AppendLog($"[dedup] TM: {kept:n0} unique rows after dedup");
            }

            if (hasTb)
            {
                ProgressText = "Sorting/deduping termbase\u2026";
                var kept = await _community.SortAndDedupTermbaseAsync(repoDir, ct);
                AppendLog($"[dedup] termbase: {kept:n0} entries after dedup");
            }

            await _git.EnsureLocalExcludeAsync(repoDir, LocalIgnorePatterns, prog, ct);
            await _git.EnsureLineEndingConfigAsync(repoDir, prog, ct);
            await _git.EnsureUserIdentityAsync(repoDir, _username, prog, ct);

            var status = await _git.GetStatusPorcelainAsync(repoDir, ct);

            var communityFiles = new List<string>();
            if (hasTm && status.Any(l => l.Contains(CommunityTmFile, StringComparison.OrdinalIgnoreCase)))
                communityFiles.Add(CommunityTmFile);
            if (hasTb && status.Any(l => l.Contains(CommunityTermbaseFile, StringComparison.OrdinalIgnoreCase)))
                communityFiles.Add(CommunityTermbaseFile);

            if (communityFiles.Count == 0)
            {
                ProgressText = "No changes in community data files (already up to date).";
                AppendLog("[warn] git status shows no changes for community data files");
                AppendLog("[hint] If you recently approved entries, they may already be committed.");
                return;
            }

            // --- Auth (auto-auth if token available, otherwise trigger device flow) ---
            if (string.IsNullOrWhiteSpace(_githubAccessToken) || string.IsNullOrWhiteSpace(_githubLogin))
            {
                ProgressText = "Authorizing GitHub\u2026";
                var token = await _auth.AuthorizeDeviceFlowAsync(prog, ct, MakeDeviceCodeCallback());
                if (token == null)
                {
                    ProgressText = "Auth failed.";
                    return;
                }

                _githubAccessToken = token.access_token;
                var me = await _api.GetMeAsync(_githubAccessToken, ct);
                _githubLogin = me?.login;

                if (string.IsNullOrWhiteSpace(_githubLogin))
                {
                    ProgressText = "Auth ok but could not read username.";
                    return;
                }

                AppendLog("[auth] user: " + _githubLogin);
                GitHubAuthCompleted?.Invoke(this, (_githubAccessToken!, _githubLogin!));
                FireDeviceFlowCompleted();
            }

            // --- Write per-user termbase JSONL ---
            ProgressText = "Writing per-user termbase JSONL\u2026";
            var communityTbDir = TermbaseStorageService.GetCommunityTermbasesDir(repoDir);
            var localTermbase = await _termbaseSvc.LoadAsync(repoDir, ct);
            await _termbaseSvc.WriteUserJsonlAsync(communityTbDir, _githubLogin!, localTermbase, ct);
            var tbJsonlRelPath = Path.Combine("community", "termbases", _githubLogin + ".jsonl").Replace('\\', '/');
            AppendLog($"[step] wrote {localTermbase.Count} term(s) to {tbJsonlRelPath}");

            // Ensure .gitattributes has merge=union for termbase JSONL
            var gitattribPath = Path.Combine(repoDir, ".gitattributes");
            const string tbJsonlMergeRule = "community/termbases/*.jsonl merge=union";
            bool tbGitattribChanged = false;
            if (File.Exists(gitattribPath))
            {
                var content = await File.ReadAllTextAsync(gitattribPath, Encoding.UTF8, ct);
                if (!content.Contains(tbJsonlMergeRule, StringComparison.Ordinal))
                {
                    if (!content.EndsWith("\n", StringComparison.Ordinal))
                        content += "\n";
                    content += tbJsonlMergeRule + "\n";
                    await File.WriteAllTextAsync(gitattribPath, content, new UTF8Encoding(false), ct);
                    tbGitattribChanged = true;
                    AppendLog("[step] appended termbase merge=union rule to .gitattributes");
                }
            }
            else
            {
                await File.WriteAllTextAsync(gitattribPath, tbJsonlMergeRule + "\n", new UTF8Encoding(false), ct);
                tbGitattribChanged = true;
                AppendLog("[step] created .gitattributes with termbase merge=union rule");
            }

            // --- Commit ---
            string originalBranch = await _git.GetCurrentBranchAsync(repoDir, ct);
            AppendLog("[git] current branch: " + originalBranch);

            string msg = CommitMessage.Trim();
            if (string.IsNullOrWhiteSpace(msg))
                msg = BuildDefaultCommunityCommitMessage();

            string branchName = $"community/data/{DateTime.Now:yyyyMMdd-HHmmss}";

            foreach (var rel in communityFiles)
            {
                ProgressText = "Staging " + rel + "\u2026";
                AppendLog("[step] git add -- " + rel);
                var stage = await _git.StagePathAsync(repoDir, rel, prog, ct);
                if (!stage.Success)
                {
                    ProgressText = "Stage failed.";
                    AppendLog("[error] " + stage.Error);
                    return;
                }
            }

            // Stage per-user termbase JSONL
            ProgressText = "Staging " + tbJsonlRelPath + "\u2026";
            AppendLog("[step] git add -- " + tbJsonlRelPath);
            var stageTbJsonl = await _git.StagePathAsync(repoDir, tbJsonlRelPath, prog, ct);
            if (!stageTbJsonl.Success)
            {
                AppendLog("[warn] failed to stage termbase JSONL: " + stageTbJsonl.Error);
            }

            // Stage .gitattributes if changed by termbase JSONL rule
            if (tbGitattribChanged)
            {
                AppendLog("[step] git add -- .gitattributes (termbase rule)");
                var stageAttr = await _git.StagePathAsync(repoDir, ".gitattributes", prog, ct);
                if (!stageAttr.Success)
                {
                    AppendLog("[warn] failed to stage .gitattributes: " + stageAttr.Error);
                }
            }

            ProgressText = "Stashing other work\u2026";
            AppendLog("[step] git stash push -u -k");
            var stash = await _git.StashKeepIndexAsync(repoDir, "readzen-community-autostash", prog, ct);
            if (!stash.Success)
            {
                ProgressText = "Stash failed.";
                AppendLog("[error] " + stash.Error);
                return;
            }

            ProgressText = "Creating branch\u2026";
            AppendLog("[step] new branch: " + branchName);
            var br = await _git.SwitchCreateBranchAsync(repoDir, branchName, prog, ct);
            if (!br.Success)
            {
                ProgressText = "Branch create failed.";
                AppendLog("[error] " + br.Error);
                await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                return;
            }

            ProgressText = "Committing\u2026";
            AppendLog("[step] commit: " + msg);
            var commit = await _git.CommitAsync(repoDir, msg, prog, ct);
            if (!commit.Success)
            {
                ProgressText = "Commit failed.";
                AppendLog("[error] " + commit.Error);
                await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                return;
            }

            _lastCommunityBranch = branchName;

            // --- Push (directly, no PR) ---
            bool isUpstreamOwner = string.Equals(_githubLogin, UpstreamOwner, StringComparison.OrdinalIgnoreCase);

            await ScrubTokenizedForkRemoteIfAny(repoDir, prog, ct);

            string remoteName;
            string remoteUrlClean;

            if (isUpstreamOwner)
            {
                remoteName = "origin";
                remoteUrlClean = TranslationRepoUrl;
                AppendLog("[mode] upstream owner -> push to origin");
            }
            else
            {
                ProgressText = "Ensuring fork\u2026";
                bool forkExists = await _api.ForkExistsAsync(_githubAccessToken!, _githubLogin!, UpstreamRepo, ct);
                if (!forkExists)
                {
                    var okFork = await _api.CreateForkAsync(_githubAccessToken!, UpstreamOwner, UpstreamRepo, ct);
                    if (!okFork)
                    {
                        ProgressText = "Fork failed.";
                        AppendLog("[error] fork creation failed");
                        await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                        return;
                    }

                    var ready = await _api.WaitForForkAsync(_githubAccessToken!, _githubLogin!, UpstreamRepo, TimeSpan.FromSeconds(60), prog, ct);
                    if (!ready)
                    {
                        ProgressText = "Fork not ready yet.";
                        await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                        return;
                    }
                }

                remoteName = "fork";
                remoteUrlClean = $"https://github.com/{_githubLogin}/{UpstreamRepo}.git";
            }

            var rem = await _git.EnsureRemoteUrlAsync(repoDir, remoteName, remoteUrlClean, prog, ct);
            if (!rem.Success)
            {
                ProgressText = "Remote config failed.";
                AppendLog("[error] " + rem.Error);
                await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                return;
            }

            ProgressText = "Pushing community branch\u2026";
            AppendLog("[step] push -u " + remoteName + " " + branchName);
            var push = await PushBranchUsingConfiguredAuthAsync(repoDir, remoteName, remoteUrlClean, branchName, prog, ct);
            if (!push.Success)
            {
                ProgressText = "Push failed.";
                AppendLog("[error] " + push.Error);
                AppendPushFailureHints(push.Error);
                await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                return;
            }

            // --- Restore and finish ---
            ProgressText = "Restoring other work\u2026";
            await SafeRestoreAsync(repoDir, originalBranch, prog, ct);

            ProgressText = "Community data shared.";
            AppendLog("[ok] committed and pushed community data on branch: " + branchName);
            AppendLog("[files] " + string.Join(", ", communityFiles));

            StatusChanged?.Invoke(this, "Community data shared: " + branchName);
        }
        catch (OperationCanceledException)
        {
            ProgressText = "Canceled.";
            AppendLog("[cancel] canceled");
        }
        catch (Exception ex)
        {
            ProgressText = "Failed: " + ex.Message;
            AppendLog("[error] " + ex);
        }
        finally
        {
            SetButtonsBusy(false);
        }
    }

    // ----- Private: Scholar Collections Share -----

    private async Task ShareScholarCollectionsInternalAsync()
    {
        var ct = BeginOperation();

        SetButtonsBusy(true);
        ClearLog();

        try
        {
            var repoDir = GetTranslationRepoDir();
            if (!Directory.Exists(repoDir) || !Directory.Exists(Path.Combine(repoDir, ".git")))
            {
                ProgressText = "Repo not ready. Click Get/Update first.";
                AppendLog("[error] repo not found / not a git working tree");
                return;
            }

            ProgressText = "Checking git\u2026";
            var gitOk = await _git.CheckGitAvailableAsync(ct);
            if (!gitOk)
            {
                ProgressText = "Git not found.";
                AppendLog("[error] git not found");
                return;
            }

            var scPath = Path.Combine(repoDir, ScholarCollectionsFile);
            if (!File.Exists(scPath))
            {
                ProgressText = "No scholar collections file found.";
                AppendLog("[warn] " + ScholarCollectionsFile + " does not exist at: " + repoDir);
                AppendLog("[hint] Add some passages to collections in the Scholar tab first.");
                return;
            }

            var prog = new Progress<string>(line => Dispatcher.UIThread.Post(() => AppendLog(line)));

            await _git.EnsureLocalExcludeAsync(repoDir, LocalIgnorePatterns, prog, ct);
            await _git.EnsureLineEndingConfigAsync(repoDir, prog, ct);
            await _git.EnsureUserIdentityAsync(repoDir, _username, prog, ct);

            // --- Auth ---
            if (string.IsNullOrWhiteSpace(_githubAccessToken) || string.IsNullOrWhiteSpace(_githubLogin))
            {
                ProgressText = "Authorizing GitHub\u2026";
                var token = await _auth.AuthorizeDeviceFlowAsync(prog, ct, MakeDeviceCodeCallback());
                if (token == null)
                {
                    ProgressText = "Auth failed.";
                    return;
                }

                _githubAccessToken = token.access_token;
                var me = await _api.GetMeAsync(_githubAccessToken, ct);
                _githubLogin = me?.login;

                if (string.IsNullOrWhiteSpace(_githubLogin))
                {
                    ProgressText = "Auth ok but could not read username.";
                    return;
                }

                AppendLog("[auth] user: " + _githubLogin);
                GitHubAuthCompleted?.Invoke(this, (_githubAccessToken!, _githubLogin!));
                FireDeviceFlowCompleted();
            }

            // --- Write per-user JSONL ---
            ProgressText = "Writing per-user JSONL\u2026";
            var communityCollDir = ScholarCollectionsService.GetCommunityCollectionsDir(repoDir);
            var localCollections = await _scholarSvc.LoadAsync(repoDir, ct);
            await _scholarSvc.WriteUserJsonlAsync(communityCollDir, _githubLogin!, localCollections, ct);
            var jsonlRelPath = Path.Combine("community", "collections", _githubLogin + ".jsonl").Replace('\\', '/');
            AppendLog($"[step] wrote {localCollections.Count} collection(s) to {jsonlRelPath}");

            // --- Ensure .gitattributes has merge=union for JSONL ---
            var gitattribPath = Path.Combine(repoDir, ".gitattributes");
            bool gitattribChanged = false;
            const string jsonlMergeRule = "community/collections/*.jsonl merge=union";
            if (File.Exists(gitattribPath))
            {
                var content = await File.ReadAllTextAsync(gitattribPath, Encoding.UTF8, ct);
                if (!content.Contains(jsonlMergeRule, StringComparison.Ordinal))
                {
                    if (!content.EndsWith("\n", StringComparison.Ordinal))
                        content += "\n";
                    content += jsonlMergeRule + "\n";
                    await File.WriteAllTextAsync(gitattribPath, content, new UTF8Encoding(false), ct);
                    AppendLog("[step] appended merge=union rule to .gitattributes");
                }
            }
            else
            {
                await File.WriteAllTextAsync(gitattribPath, jsonlMergeRule + "\n", new UTF8Encoding(false), ct);
                gitattribChanged = true;
                AppendLog("[step] created .gitattributes with merge=union rule");
            }

            var status = await _git.GetStatusPorcelainAsync(repoDir, ct);
            bool hasChanges = status.Any(l =>
                l.Contains(jsonlRelPath, StringComparison.OrdinalIgnoreCase) ||
                l.Contains("community/collections/", StringComparison.OrdinalIgnoreCase));

            if (!hasChanges && !gitattribChanged)
            {
                ProgressText = "No changes in scholar collections (already up to date).";
                AppendLog("[warn] git status shows no changes for scholar collections JSONL");
                return;
            }

            // --- Commit ---
            string originalBranch = await _git.GetCurrentBranchAsync(repoDir, ct);
            AppendLog("[git] current branch: " + originalBranch);

            string msg = $"{GetUsernameForDefaults()}: Scholar collections update";
            string branchName = $"community/scholar/{DateTime.Now:yyyyMMdd-HHmmss}";

            // Stage JSONL file
            ProgressText = "Staging " + jsonlRelPath + "\u2026";
            AppendLog("[step] git add -- " + jsonlRelPath);
            var stage = await _git.StagePathAsync(repoDir, jsonlRelPath, prog, ct);
            if (!stage.Success)
            {
                ProgressText = "Stage failed.";
                AppendLog("[error] " + stage.Error);
                return;
            }

            // Stage .gitattributes if changed
            if (gitattribChanged)
            {
                AppendLog("[step] git add -- .gitattributes");
                var stageAttr = await _git.StagePathAsync(repoDir, ".gitattributes", prog, ct);
                if (!stageAttr.Success)
                {
                    AppendLog("[warn] failed to stage .gitattributes: " + stageAttr.Error);
                }
            }

            ProgressText = "Stashing other work\u2026";
            var stash = await _git.StashKeepIndexAsync(repoDir, "readzen-scholar-autostash", prog, ct);
            if (!stash.Success)
            {
                ProgressText = "Stash failed.";
                AppendLog("[error] " + stash.Error);
                return;
            }

            ProgressText = "Creating branch\u2026";
            AppendLog("[step] new branch: " + branchName);
            var br = await _git.SwitchCreateBranchAsync(repoDir, branchName, prog, ct);
            if (!br.Success)
            {
                ProgressText = "Branch create failed.";
                AppendLog("[error] " + br.Error);
                await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                return;
            }

            ProgressText = "Committing\u2026";
            AppendLog("[step] commit: " + msg);
            var commit = await _git.CommitAsync(repoDir, msg, prog, ct);
            if (!commit.Success)
            {
                ProgressText = "Commit failed.";
                AppendLog("[error] " + commit.Error);
                await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                return;
            }

            // --- Push (directly, no PR) ---
            bool isUpstreamOwner = string.Equals(_githubLogin, UpstreamOwner, StringComparison.OrdinalIgnoreCase);

            await ScrubTokenizedForkRemoteIfAny(repoDir, prog, ct);

            string remoteName;
            string remoteUrlClean;

            if (isUpstreamOwner)
            {
                remoteName = "origin";
                remoteUrlClean = TranslationRepoUrl;
                AppendLog("[mode] upstream owner -> push to origin");
            }
            else
            {
                ProgressText = "Ensuring fork\u2026";
                bool forkExists = await _api.ForkExistsAsync(_githubAccessToken!, _githubLogin!, UpstreamRepo, ct);
                if (!forkExists)
                {
                    var okFork = await _api.CreateForkAsync(_githubAccessToken!, UpstreamOwner, UpstreamRepo, ct);
                    if (!okFork)
                    {
                        ProgressText = "Fork failed.";
                        AppendLog("[error] fork creation failed");
                        await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                        return;
                    }

                    var ready = await _api.WaitForForkAsync(_githubAccessToken!, _githubLogin!, UpstreamRepo, TimeSpan.FromSeconds(60), prog, ct);
                    if (!ready)
                    {
                        ProgressText = "Fork not ready yet.";
                        await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                        return;
                    }
                }

                remoteName = "fork";
                remoteUrlClean = $"https://github.com/{_githubLogin}/{UpstreamRepo}.git";
            }

            var rem = await _git.EnsureRemoteUrlAsync(repoDir, remoteName, remoteUrlClean, prog, ct);
            if (!rem.Success)
            {
                ProgressText = "Remote config failed.";
                AppendLog("[error] " + rem.Error);
                await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                return;
            }

            ProgressText = "Pushing scholar branch\u2026";
            AppendLog("[step] push -u " + remoteName + " " + branchName);
            var push = await PushBranchUsingConfiguredAuthAsync(repoDir, remoteName, remoteUrlClean, branchName, prog, ct);
            if (!push.Success)
            {
                ProgressText = "Push failed.";
                AppendLog("[error] " + push.Error);
                AppendPushFailureHints(push.Error);
                await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                return;
            }

            // --- Restore and finish ---
            ProgressText = "Restoring other work\u2026";
            await SafeRestoreAsync(repoDir, originalBranch, prog, ct);

            ProgressText = "Scholar collections shared.";
            AppendLog("[ok] committed and pushed scholar collections on branch: " + branchName);

            StatusChanged?.Invoke(this, "Scholar collections shared: " + branchName);
        }
        catch (OperationCanceledException)
        {
            ProgressText = "Canceled.";
            AppendLog("[cancel] canceled");
        }
        catch (Exception ex)
        {
            ProgressText = "Failed: " + ex.Message;
            AppendLog("[error] " + ex);
        }
        finally
        {
            SetButtonsBusy(false);
        }
    }

    private async Task FetchAndMergeCommunityDataAsync()
    {
        var ct = BeginOperation();

        SetButtonsBusy(true);
        ClearLog();

        try
        {
            var repoDir = GetTranslationRepoDir();
            if (!Directory.Exists(repoDir) || !Directory.Exists(Path.Combine(repoDir, ".git")))
            {
                ProgressText = "Repo not ready. Click Get/Update first.";
                AppendLog("[error] repo not found / not a git working tree");
                return;
            }

            ProgressText = "Checking git\u2026";
            var gitOk = await _git.CheckGitAvailableAsync(ct);
            if (!gitOk)
            {
                ProgressText = "Git not found.";
                AppendLog("[error] git not found");
                return;
            }

            var prog = new Progress<string>(line => Dispatcher.UIThread.Post(() => AppendLog(line)));

            ProgressText = "Fetching origin\u2026";
            AppendLog("[step] git fetch origin");
            var fetch = await _git.FetchAsync(repoDir, prog, ct);
            if (!fetch.Success)
            {
                ProgressText = "Fetch failed.";
                AppendLog("[error] " + (fetch.Error ?? "unknown"));
                return;
            }

            var tempDir = Path.Combine(Path.GetTempPath(), "ReadZen", "community-merge", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var upstreamTmTemp = Path.Combine(tempDir, CommunityTmFile);
                var upstreamTbTemp = Path.Combine(tempDir, CommunityTermbaseFile);

                int mergedTm = 0;
                int mergedTb = 0;

                ProgressText = "Reading upstream TM\u2026";
                AppendLog("[step] git show origin/main:" + CommunityTmFile);
                var showTm = await RunGitOutputAsync(repoDir, "show", $"origin/main:{CommunityTmFile}", ct);
                if (showTm != null)
                {
                    await File.WriteAllTextAsync(upstreamTmTemp, showTm, new UTF8Encoding(false), ct);
                    ProgressText = "Merging TM\u2026";
                    mergedTm = await _community.MergeApprovedTmFromAsync(repoDir, upstreamTmTemp, ct);
                    AppendLog($"[merge] TM: {mergedTm:n0} unique rows after merge");
                }
                else
                {
                    AppendLog("[info] no " + CommunityTmFile + " found in origin/main (skipping TM merge)");
                }

                ProgressText = "Reading upstream termbase\u2026";
                AppendLog("[step] git show origin/main:" + CommunityTermbaseFile);
                var showTb = await RunGitOutputAsync(repoDir, "show", $"origin/main:{CommunityTermbaseFile}", ct);
                if (showTb != null)
                {
                    await File.WriteAllTextAsync(upstreamTbTemp, showTb, new UTF8Encoding(false), ct);
                    ProgressText = "Merging termbase\u2026";
                    mergedTb = await _community.MergeTermbaseFromAsync(repoDir, upstreamTbTemp, ct);
                    AppendLog($"[merge] termbase: {mergedTb:n0} entries after merge");
                }
                else
                {
                    AppendLog("[info] no " + CommunityTermbaseFile + " found in origin/main (skipping termbase merge)");
                }

                // Scholar collections merge (legacy JSON)
                int mergedSc = 0;
                var upstreamScTemp = Path.Combine(tempDir, ScholarCollectionsFile);

                ProgressText = "Reading upstream scholar collections\u2026";
                AppendLog("[step] git show origin/main:" + ScholarCollectionsFile);
                var showSc = await RunGitOutputAsync(repoDir, "show", $"origin/main:{ScholarCollectionsFile}", ct);
                if (showSc != null)
                {
                    await File.WriteAllTextAsync(upstreamScTemp, showSc, new UTF8Encoding(false), ct);
                    ProgressText = "Merging scholar collections\u2026";
                    mergedSc = await _community.MergeScholarCollectionsFromAsync(repoDir, upstreamScTemp, ct);
                    AppendLog($"[merge] scholar collections: {mergedSc:n0} collections after merge");
                }
                else
                {
                    AppendLog("[info] no " + ScholarCollectionsFile + " found in origin/main (skipping scholar merge)");
                }

                // Scholar collections JSONL (per-user)
                int communityJsonlCount = 0;
                ProgressText = "Pulling community collection JSONL files\u2026";
                AppendLog("[step] git checkout origin/main -- community/collections/");
                try
                {
                    var checkoutResult = await RunGitArgsAsync(repoDir, ct, "checkout", "origin/main", "--", "community/collections/");
                    // checkoutResult may be null even on success (checkout writes to working tree, not stdout)
                    var communityCollDir = ScholarCollectionsService.GetCommunityCollectionsDir(repoDir);
                    if (Directory.Exists(communityCollDir))
                    {
                        communityJsonlCount = Directory.GetFiles(communityCollDir, "*.jsonl").Length;
                        AppendLog($"[info] community collections: {communityJsonlCount} user JSONL file(s)");
                    }
                    else
                    {
                        AppendLog("[info] no community/collections/ directory found in origin/main");
                    }
                }
                catch
                {
                    AppendLog("[info] community/collections/ not found in origin/main (skipping JSONL pull)");
                }

                // Community termbases JSONL (per-user)
                int communityTbJsonlCount = 0;
                ProgressText = "Pulling community termbase JSONL files\u2026";
                AppendLog("[step] git checkout origin/main -- community/termbases/");
                try
                {
                    var checkoutTbResult = await RunGitArgsAsync(repoDir, ct, "checkout", "origin/main", "--", "community/termbases/");
                    var communityTbDir = TermbaseStorageService.GetCommunityTermbasesDir(repoDir);
                    if (Directory.Exists(communityTbDir))
                    {
                        communityTbJsonlCount = Directory.GetFiles(communityTbDir, "*.jsonl").Length;
                        AppendLog($"[info] community termbases: {communityTbJsonlCount} user JSONL file(s)");
                    }
                    else
                    {
                        AppendLog("[info] no community/termbases/ directory found in origin/main");
                    }
                }
                catch
                {
                    AppendLog("[info] community/termbases/ not found in origin/main (skipping termbase JSONL pull)");
                }

                // Community reviews JSONL (per-user)
                int communityReviewJsonlCount = 0;
                ProgressText = "Pulling community review files\u2026";
                AppendLog("[step] git checkout origin/main -- community/reviews/");
                try
                {
                    await RunGitArgsAsync(repoDir, ct, "checkout", "origin/main", "--", "community/reviews/");
                    var communityReviewsDir = ITranslationReviewService.GetCommunityReviewsDir(repoDir);
                    if (Directory.Exists(communityReviewsDir))
                    {
                        communityReviewJsonlCount = Directory.GetFiles(communityReviewsDir, "*.jsonl").Length;
                        AppendLog($"[info] community reviews: {communityReviewJsonlCount} user JSONL file(s)");
                    }
                }
                catch { AppendLog("[info] community/reviews/ not found in origin/main"); }

                // Community master dates JSONL (per-user)
                int communityMdJsonlCount = 0;
                ProgressText = "Pulling community master dates\u2026";
                AppendLog("[step] git checkout origin/main -- community/master-dates/");
                try
                {
                    await RunGitArgsAsync(repoDir, ct, "checkout", "origin/main", "--", "community/master-dates/");
                    var communityMdDir = IMasterDatesService.GetCommunityMasterDatesDir(repoDir);
                    if (Directory.Exists(communityMdDir))
                    {
                        communityMdJsonlCount = Directory.GetFiles(communityMdDir, "*.jsonl").Length;
                        AppendLog($"[info] community master dates: {communityMdJsonlCount} user JSONL file(s)");
                    }
                }
                catch { AppendLog("[info] community/master-dates/ not found in origin/main"); }

                // Community tags
                int communityTagJsonlCount = 0;
                AppendLog("[step] git checkout origin/main -- community/tags/");
                try
                {
                    await RunGitArgsAsync(repoDir, ct, "checkout", "origin/main", "--", "community/tags/");
                    var communityTagsDir = DocumentTagService.GetCommunityTagsDir(repoDir);
                    if (Directory.Exists(communityTagsDir))
                    {
                        communityTagJsonlCount = Directory.GetFiles(communityTagsDir, "*.jsonl").Length;
                        AppendLog($"[info] community tags: {communityTagJsonlCount} user JSONL file(s)");
                    }
                }
                catch { AppendLog("[info] community/tags/ not found in origin/main"); }

                try
                {
                    await RunGitArgsAsync(repoDir, ct, "checkout", "origin/main", "--", "community/tag-vocabularies/");
                }
                catch { AppendLog("[info] community/tag-vocabularies/ not found in origin/main"); }

                // Community translations (per-user)
                int communityTransUserCount = 0;
                ProgressText = "Pulling community translationsÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¦";
                AppendLog("[step] git checkout origin/main -- community/translations/");
                try
                {
                    string? preservedUserTranslationBackupDir = null;
                    string? preservedUserTranslationDir = null;
                    if (!string.IsNullOrWhiteSpace(_githubLogin))
                    {
                        preservedUserTranslationDir = Path.Combine(repoDir, "community", "translations", AppPaths.SanitizeUsername(_githubLogin));
                        if (Directory.Exists(preservedUserTranslationDir))
                        {
                            preservedUserTranslationBackupDir = Path.Combine(tempDir, "preserve-user-translations");
                            CopyDirectoryContents(preservedUserTranslationDir, preservedUserTranslationBackupDir);
                            AppendLog($"[preserve] kept local personal translations for {_githubLogin}");
                        }
                    }

                    await RunGitArgsAsync(repoDir, ct, "checkout", "origin/main", "--", "community/translations/");

                    if (!string.IsNullOrWhiteSpace(preservedUserTranslationBackupDir) &&
                        !string.IsNullOrWhiteSpace(preservedUserTranslationDir) &&
                        Directory.Exists(preservedUserTranslationBackupDir))
                    {
                        CopyDirectoryContents(preservedUserTranslationBackupDir, preservedUserTranslationDir);
                        AppendLog($"[restore] reapplied local personal translations for {_githubLogin}");
                    }

                    var communityTransDir = Path.Combine(repoDir, "community", "translations");
                    if (Directory.Exists(communityTransDir))
                    {
                        communityTransUserCount = Directory.GetDirectories(communityTransDir).Length;
                        AppendLog($"[info] community translations: {communityTransUserCount} user dir(s)");
                    }
                }
                catch { AppendLog("[info] community/translations/ not found in origin/main"); }

                if (mergedTm == 0 && mergedTb == 0 && mergedSc == 0 && communityJsonlCount == 0 && communityTbJsonlCount == 0 && communityReviewJsonlCount == 0 && communityMdJsonlCount == 0 && communityTagJsonlCount == 0 && communityTransUserCount == 0 && showTm == null && showTb == null && showSc == null)
                {
                    ProgressText = "No community data found in origin/main.";
                    AppendLog("[info] origin/main has no community data files yet");
                    return;
                }

                ProgressText = $"Merge complete. TM: {mergedTm:n0} rows, termbase: {mergedTb:n0} entries, collections: {mergedSc:n0}.";
                AppendLog("[ok] community data merged into local files");
                AppendLog("[note] Your local files have been updated. Save/reload the app to see new entries in the assistant.");

                StatusChanged?.Invoke(this, $"Community merge done: {mergedTm:n0} TM rows, {mergedTb:n0} termbase entries, {mergedSc:n0} collections.");
                CommunityDataFetched?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                TryDeleteDirectory(tempDir);
            }
        }
        catch (OperationCanceledException)
        {
            ProgressText = "Canceled.";
            AppendLog("[cancel] canceled");
        }
        catch (Exception ex)
        {
            ProgressText = "Failed: " + ex.Message;
            AppendLog("[error] " + ex);
        }
        finally
        {
            SetButtonsBusy(false);
        }
    }

    // ----- Private helpers -----

    private Action<DeviceCodeReady> MakeDeviceCodeCallback()
    {
        return deviceCode =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _ = ShowDeviceCodeAsync?.Invoke(deviceCode.UserCode, deviceCode.VerificationUri);
            });
        };
    }

    private void FireDeviceFlowCompleted()
    {
        DeviceFlowCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void SetButtonsBusy(bool busy)
    {
        IsBusy = busy;
        IsNotBusy = !busy;
    }

    private void ClearLog()
    {
        LogText = "";
    }

    private void AppendLog(string line)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var current = LogText;

            if (current.Length > 200_000)
                current = current.Substring(current.Length - 120_000);

            LogText = current + line + Environment.NewLine;
            ScrollLogToEnd?.Invoke();
        });
    }

    private void UpdateDestLabel()
    {
        var parentDir = GetTargetRepoDir();
        DestText = "Location: " + parentDir;
    }

    private void UpdateSelectedLabel()
    {
        SelectedText = string.IsNullOrWhiteSpace(_selectedRelPath)
            ? "Selected: (none)"
            : "Selected: " + _selectedRelPath;
    }

    /// <summary>
    /// Given a picked folder, try to resolve it as a valid parent root that contains both repo subfolders.
    /// Also handles legacy single-repo folders and direct repo subfolder picks.
    /// Returns the PARENT folder path or null.
    /// </summary>
    private string? TryResolveParentRootFromAnyFolder(string? folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return null;

        try
        {
            var full = Path.GetFullPath(folderPath.Trim());

            if (!Directory.Exists(full))
                return null;

            // If the picked folder itself contains both repos (or at least one), treat it as the parent
            if (AppPaths.ValidateBothReposExist(full))
                return full;

            // If the picked folder IS one of the repo subfolders, return its parent
            if (Directory.Exists(Path.Combine(full, ".git")))
            {
                var parent = Path.GetDirectoryName(full);
                if (parent != null && AppPaths.ValidateBothReposExist(parent))
                    return parent;
                // Legacy: single repo folder that hasn't been split yet — treat parent as parent root
                if (parent != null)
                    return parent;
            }

            // Check if there's at least one discoverable repo subfolder
            var (orig, trans) = AppPaths.DiscoverRepoPaths(full);
            if (orig != null || trans != null)
                return full;

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string GetDefaultBaseFolder()
    {
        try
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (string.IsNullOrWhiteSpace(docs))
                docs = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            return Path.Combine(docs, "ReadZen");
        }
        catch
        {
            return AppContext.BaseDirectory;
        }
    }

    /// <summary>
    /// Returns the PARENT folder that contains (or will contain) both repo subfolders.
    /// </summary>
    private string GetTargetRepoDir()
    {
        if (!string.IsNullOrWhiteSpace(_currentRepoRoot) && Directory.Exists(_currentRepoRoot))
        {
            // If _currentRepoRoot already has both repos discovered, use it
            if (AppPaths.ValidateBothReposExist(_currentRepoRoot))
                return _currentRepoRoot!;

            // Legacy: if _currentRepoRoot is itself a .git repo (old single-repo setup), go up one level
            if (Directory.Exists(Path.Combine(_currentRepoRoot, ".git")))
            {
                var parent = Path.GetDirectoryName(_currentRepoRoot);
                if (parent != null)
                {
                    _currentRepoRoot = parent;
                    return parent;
                }
            }

            return _currentRepoRoot!;
        }

        var baseDir = _baseDestFolder ?? GetDefaultBaseFolder();
        return baseDir;
    }

    private string GetOriginalsRepoDir()
    {
        var parent = GetTargetRepoDir();
        // Multi-corpus dispatch: in OpenZen mode, use the OpenZen text repo
        // explicitly. The legacy AppPaths.GetOriginalRepoRoot returns the
        // first discovered originals repo (CBETA in a co-installed setup),
        // which would point share/sync flows at the wrong repo. Same fix
        // pattern as the data-loss bug closed earlier today in
        // MainWindowViewModel.LoadRootAsync.
        if (_activeCorpus == CorpusKind.Open)
            return Path.Combine(parent, OpenZenOriginalsFolderName);
        var discovered = AppPaths.GetOriginalRepoRoot(parent);
        return discovered ?? Path.Combine(parent, AppPaths.DefaultOriginalRepoFolderName);
    }

    private string GetTranslationRepoDir()
    {
        var parent = GetTargetRepoDir();
        // Multi-corpus dispatch: see GetOriginalsRepoDir for the rationale.
        // The whole share + sync + PR pipeline reads this; if it returns
        // CBETA while the user is in OpenZen mode, OpenZen translations
        // never get committed and the user's work appears to vanish.
        if (_activeCorpus == CorpusKind.Open)
            return Path.Combine(parent, OpenZenTranslationsFolderName);
        var discovered = AppPaths.GetTranslationRepoRoot(parent);
        return discovered ?? Path.Combine(parent, AppPaths.DefaultTranslationRepoFolderName);
    }

    private string GetOpenZenOriginalsDir()
    {
        var parent = GetTargetRepoDir();
        return Path.Combine(parent, OpenZenOriginalsFolderName);
    }

    private string GetOpenZenTranslationsDir()
    {
        var parent = GetTargetRepoDir();
        return Path.Combine(parent, OpenZenTranslationsFolderName);
    }

    /// <summary>
    /// Clone or update the OpenZenTexts + OpenZenTranslations pair as a
    /// secondary corpus alongside CBETA. Called from both the CBETA update
    /// path and the CBETA clone path so existing users automatically get
    /// the OpenZen pair on their next sync (the migration story for
    /// CBETA-only installs).
    ///
    /// Defensive: any failure here is logged but does NOT propagate. The
    /// CBETA half of the sync is the primary deliverable; OpenZen failing
    /// (e.g. network blip, repo not yet pushed) must not break the
    /// existing CBETA-only experience.
    /// </summary>
    private async Task EnsureOpenZenReposAsync(IProgress<string> prog, CancellationToken ct)
    {
        try
        {
            var openOrigDir = GetOpenZenOriginalsDir();
            var openTransDir = GetOpenZenTranslationsDir();

            bool openOrigExists = Directory.Exists(openOrigDir) && Directory.Exists(Path.Combine(openOrigDir, ".git"));
            bool openTransExists = Directory.Exists(openTransDir) && Directory.Exists(Path.Combine(openTransDir, ".git"));

            if (openOrigExists && openTransExists)
            {
                AppendLog("\n--- Updating OpenZenTexts repos ---");
                ProgressText = "Fetching OpenZenTexts…";
                StatusChanged?.Invoke(this, "Updating OpenZenTexts (originals)…");
                var f1 = await _git.FetchAsync(openOrigDir, prog, ct);
                if (f1.Success)
                {
                    await DoUpdateDiscardLocalAsync(openOrigDir, prog, ct);
                    AppendLog("[ok] OpenZenTexts originals updated");
                }
                else
                {
                    AppendLog("[warn] OpenZenTexts originals fetch failed: " + (f1.Error ?? "unknown"));
                }

                StatusChanged?.Invoke(this, "Updating OpenZenTexts (translations)…");
                var f2 = await _git.FetchAsync(openTransDir, prog, ct);
                if (f2.Success)
                {
                    // Translations: keep local changes (user might have personal contributions)
                    await DoUpdateKeepLocalAsync(openTransDir, prog, ct);
                    AppendLog("[ok] OpenZenTranslations updated (kept local)");
                }
                else
                {
                    AppendLog("[warn] OpenZenTranslations fetch failed: " + (f2.Error ?? "unknown"));
                }
                return;
            }

            // Clone any missing OpenZen repo. The clone is small (< 5 MB
            // currently) so progress notifications are simpler.
            if (!openOrigExists)
            {
                if (Directory.Exists(openOrigDir) && Directory.EnumerateFileSystemEntries(openOrigDir).Any())
                {
                    AppendLog("[warn] OpenZenTexts target folder exists but is not a Git repo — skipping clone. Path: " + openOrigDir);
                }
                else
                {
                    AppendLog("\n--- Cloning OpenZenTexts ---");
                    ProgressText = "Cloning OpenZenTexts…";
                    StatusChanged?.Invoke(this, "Cloning OpenZenTexts (free corpus)…");
                    var c1 = await _git.CloneAsync(OpenZenOriginalsRepoUrl, openOrigDir, prog, ct);
                    if (c1.Success)
                    {
                        await _git.EnsureLocalExcludeAsync(openOrigDir, LocalIgnorePatterns, prog, ct);
                        await _git.EnsureLineEndingConfigAsync(openOrigDir, prog, ct);
                        AppendLog("[ok] OpenZenTexts cloned: " + openOrigDir);
                    }
                    else
                    {
                        AppendLog("[warn] OpenZenTexts clone failed: " + (c1.Error ?? "unknown"));
                        return;
                    }
                }
            }

            if (!openTransExists)
            {
                if (Directory.Exists(openTransDir) && Directory.EnumerateFileSystemEntries(openTransDir).Any())
                {
                    AppendLog("[warn] OpenZenTranslations target folder exists but is not a Git repo — skipping clone. Path: " + openTransDir);
                }
                else
                {
                    AppendLog("\n--- Cloning OpenZenTranslations ---");
                    ProgressText = "Cloning OpenZenTranslations…";
                    StatusChanged?.Invoke(this, "Cloning OpenZenTranslations…");
                    var c2 = await _git.CloneAsync(OpenZenTranslationRepoUrl, openTransDir, prog, ct);
                    if (c2.Success)
                    {
                        await _git.EnsureLocalExcludeAsync(openTransDir, LocalIgnorePatterns, prog, ct);
                        await _git.EnsureLineEndingConfigAsync(openTransDir, prog, ct);
                        AppendLog("[ok] OpenZenTranslations cloned: " + openTransDir);
                    }
                    else
                    {
                        AppendLog("[warn] OpenZenTranslations clone failed: " + (c2.Error ?? "unknown"));
                    }
                }
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (System.Exception ex)
        {
            // Best-effort: log and move on. CBETA sync is the primary path.
            AppendLog("[warn] OpenZenTexts sync skipped: " + ex.Message);
        }
    }

    private async Task SafeRestoreAsync(string repoDir, string originalBranch, IProgress<string> prog, CancellationToken ct)
    {
        try
        {
            AppendLog("[restore] switching back to: " + originalBranch);
            await _git.SwitchBranchAsync(repoDir, originalBranch, prog, ct);

            AppendLog("[restore] stash pop");
            var pop = await _git.StashPopAsync(repoDir, prog, ct);
            if (!pop.Success)
            {
                AppendLog("[warn] stash pop had conflicts or failed.");
                AppendLog("[warn] your stash is probably still saved. You can resolve conflicts and run: git stash pop");
            }
        }
        catch (Exception ex)
        {
            AppendLog("[warn] restore failed: " + ex.Message);
            AppendLog("[warn] your stash should still exist. Run: git stash list");
        }
    }

    private static string BuildTokenizedGitRemoteUrl(string cleanRemoteUrl, string githubAccessToken)
    {
        var uri = new Uri(cleanRemoteUrl);
        var builder = new UriBuilder(uri)
        {
            UserName = "x-access-token",
            Password = githubAccessToken
        };
        return builder.Uri.ToString();
    }

    private async Task<GitOpResult> PushBranchUsingConfiguredAuthAsync(
        string repoDir,
        string remoteName,
        string remoteUrlClean,
        string branchName,
        IProgress<string> prog,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_githubAccessToken))
        {
            await _git.EnsureCredentialHelperAsync(repoDir, prog, ct);
            return await _git.PushSetUpstreamAsync(repoDir, remoteName, branchName, prog, ct);
        }

        var remoteUrlForPush = BuildTokenizedGitRemoteUrl(remoteUrlClean, _githubAccessToken!);
        var rem = await _git.EnsureRemoteUrlAsync(repoDir, remoteName, remoteUrlForPush, prog, ct);
        if (!rem.Success)
            return rem;

        try
        {
            prog.Report("[auth] using OAuth token for git push");
            return await _git.PushSetUpstreamAsync(repoDir, remoteName, branchName, prog, ct);
        }
        finally
        {
            await _git.EnsureRemoteUrlAsync(repoDir, remoteName, remoteUrlClean, prog, ct);
        }
    }
    private async Task ScrubTokenizedForkRemoteIfAny(string repoDir, IProgress<string> prog, CancellationToken ct)
    {
        try
        {
            var url = await _git.GetRemoteUrlAsync(repoDir, "fork", ct);
            if (string.IsNullOrWhiteSpace(url)) return;

            bool hasCreds = url.Contains("x-access-token:", StringComparison.OrdinalIgnoreCase) ||
                            Regex.IsMatch(url, @"https://[^/]+@github\.com/", RegexOptions.IgnoreCase);

            if (hasCreds)
            {
                prog.Report("[security] removing tokenized 'fork' remote");
                await _git.RemoveRemoteAsync(repoDir, "fork", prog, ct);
            }
        }
        catch
        {
            // never block on cleanup
        }
    }

    private void AppendPushFailureHints(string? err)
    {
        err ??= "";

        bool looksLikeNoPrompt =
            err.Contains("terminal prompts disabled", StringComparison.OrdinalIgnoreCase) ||
            err.Contains("could not read Username", StringComparison.OrdinalIgnoreCase) ||
            err.Contains("could not read Password", StringComparison.OrdinalIgnoreCase) ||
            err.Contains("Authentication failed", StringComparison.OrdinalIgnoreCase) ||
            err.Contains("support for password authentication was removed", StringComparison.OrdinalIgnoreCase) ||
            err.Contains("fatal: Authentication failed", StringComparison.OrdinalIgnoreCase);

        bool looksLikeWrongAccount =
            err.Contains("403", StringComparison.OrdinalIgnoreCase) ||
            err.Contains("Forbidden", StringComparison.OrdinalIgnoreCase);

        bool looksLikeRepoNotFound =
            err.Contains("Repository not found", StringComparison.OrdinalIgnoreCase) ||
            err.Contains("404", StringComparison.OrdinalIgnoreCase);

        bool looksLikeNoCredStore =
            err.Contains("No credential store has been selected", StringComparison.OrdinalIgnoreCase) ||
            err.Contains("GCM_CREDENTIAL_STORE", StringComparison.OrdinalIgnoreCase) ||
            err.Contains("credential.credentialStore", StringComparison.OrdinalIgnoreCase);

        bool looksLikeNoHelper =
            err.Contains("git: 'credential-manager-core' is not a git command", StringComparison.OrdinalIgnoreCase) ||
            err.Contains("git: 'credential-manager' is not a git command", StringComparison.OrdinalIgnoreCase);

        if (looksLikeRepoNotFound)
            AppendLog("[hint] Repository not found -> wrong remote URL or not authenticated.");

        if (looksLikeWrongAccount)
            AppendLog("[hint] 403 usually means wrong GitHub account is cached in credential helper.");

        if (looksLikeNoHelper)
        {
            AppendLog("[hint] Credential helper not found.");
            AppendLog("[hint] Bundle full PortableGit (including git-core helpers) or install Git for Windows.");
            return;
        }

        if (looksLikeNoCredStore)
        {
            AppendLog("[hint] Git Credential Manager has no credential store configured.");
            AppendLog("[linux] Try:");
            AppendLog("  git config --global credential.helper manager");
            AppendLog("  git config --global credential.credentialStore secretservice");
            AppendLog("  git-credential-manager configure");
            return;
        }

        if (looksLikeNoPrompt)
        {
            AppendLog("[hint] Git could not open a login prompt.");
            AppendLog("[hint] On Windows, the shipped Git may be missing Git Credential Manager files.");
        }
        else
        {
            AppendLog("[hint] If this is auth-related, check credential helper setup and retry.");
        }
    }

    private static string MakeBranchName(string relPath)
    {
        string ts = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string core = relPath.Replace('\\', '/');

        core = Regex.Replace(core, @"[^a-zA-Z0-9/\-_.]+", "-");
        core = core.Trim('-').Trim('/');
        if (core.Length > 80) core = core.Substring(core.Length - 80);

        return $"contrib/{core}/{ts}";
    }

    private string BuildDefaultTranslationCommitMessage(string relPath)
    {
        string fileName = Path.GetFileName((relPath ?? "").Replace('/', Path.DirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "selected-file";

        return $"{GetUsernameForDefaults()}: Translation update: {fileName}";
    }

    private string BuildDefaultCommunityCommitMessage()
        => $"{GetUsernameForDefaults()}: Community data: approved TM + termbase update";

    private string BuildDefaultPrTitle()
        => $"{GetUsernameForDefaults()}: Translation update";

    private string GetUsernameForDefaults()
        => string.IsNullOrWhiteSpace(_username) ? "User" : _username!.Trim();

    private static string NormalizeRel(string p)
        => (p ?? "").Replace('\\', '/').TrimStart('/');

    private static string NormalizePorcelainPath(string line)
    {
        var filePath = line.Length > 3 ? line.Substring(3).Trim().Trim('\"') : line.Trim();
        if (filePath.Contains(" -> ", StringComparison.Ordinal))
            filePath = filePath.Split(new[] { " -> " }, StringSplitOptions.None).Last();
        return NormalizeRel(filePath);
    }

    private Dictionary<string, string?> CaptureCommunityShareFingerprints(string repoDir)
    {
        var trackedPaths = GetTrackedCommunitySharePaths(repoDir);
        var fingerprints = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var relPath in trackedPaths)
        {
            var fullPath = Path.Combine(repoDir, relPath.Replace('/', Path.DirectorySeparatorChar));
            fingerprints[relPath] = File.Exists(fullPath)
                ? ComputeCommunityShareFingerprint(fullPath)
                : null;
        }

        return fingerprints;
    }

    private HashSet<string> GetTrackedCommunitySharePaths(string repoDir)
    {
        var trackedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            NormalizeRel(CommunityTmFile),
            NormalizeRel(CommunityTermbaseFile),
            NormalizeRel(ScholarCollectionsFile),
            NormalizeRel("translation-review.jsonl"),
            ".gitattributes"
        };
        if (string.IsNullOrWhiteSpace(_githubLogin))
            return trackedPaths;

        var login = _githubLogin.Trim();
        trackedPaths.Add($"community/termbases/{login}.jsonl");
        trackedPaths.Add($"community/collections/{login}.jsonl");
        trackedPaths.Add($"community/reviews/{login}.jsonl");
        trackedPaths.Add($"community/master-dates/{login}.jsonl");
        trackedPaths.Add($"community/tags/{login}.jsonl");
        trackedPaths.Add($"community/tag-vocabularies/{login}.json");

        var translationUserDir = Path.Combine(repoDir, "community", "translations", AppPaths.SanitizeUsername(login));
        if (Directory.Exists(translationUserDir))
        {
            foreach (var fullPath in Directory.EnumerateFiles(translationUserDir, "*", SearchOption.AllDirectories))
                trackedPaths.Add(NormalizeRel(Path.GetRelativePath(repoDir, fullPath)));
        }

        return trackedPaths;
    }

    private bool IsAutoMergeCommunitySharePath(string relPath)
    {
        var normalized = NormalizeRel(relPath);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;
        if (normalized.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
            return false;
        if (string.Equals(normalized, ".gitattributes", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(normalized, NormalizeRel(CommunityTmFile), StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(normalized, NormalizeRel(CommunityTermbaseFile), StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(normalized, NormalizeRel(ScholarCollectionsFile), StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(normalized, "translation-review.jsonl", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.IsNullOrWhiteSpace(_githubLogin))
            return false;

        var login = _githubLogin.Trim();
        return string.Equals(normalized, $"community/termbases/{login}.jsonl", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, $"community/collections/{login}.jsonl", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, $"community/reviews/{login}.jsonl", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, $"community/master-dates/{login}.jsonl", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, $"community/tags/{login}.jsonl", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, $"community/tag-vocabularies/{login}.json", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith($"community/translations/{AppPaths.SanitizeUsername(login)}/", StringComparison.OrdinalIgnoreCase);
    }
    private bool IsTrackedCommunitySharePath(string relPath)
    {
        var normalized = NormalizeRel(relPath);
        return !string.IsNullOrWhiteSpace(normalized) &&
               GetTrackedCommunitySharePaths(GetTranslationRepoDir()).Contains(normalized);
    }

    private static string? ComputeCommunityShareFingerprint(string fullPath)
    {
        if (!File.Exists(fullPath))
            return null;

        using var stream = File.OpenRead(fullPath);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private string[] GetAlwaysPreservedUpdatePaths(string repoDir)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(repoDir) || !Directory.Exists(repoDir))
            return result.ToArray();

        // Preserve all personal translation directories
        var translationsDir = Path.Combine(repoDir, "community", "translations");
        if (Directory.Exists(translationsDir))
        {
            foreach (var file in Directory.EnumerateFiles(translationsDir, "*", SearchOption.AllDirectories))
                result.Add(NormalizeRel(Path.GetRelativePath(repoDir, file)));
        }

        // Preserve locally modified xml-p5t files (user may have saved to community source)
        var xmlP5tDir = Path.Combine(repoDir, "xml-p5t");
        if (Directory.Exists(xmlP5tDir))
        {
            foreach (var file in Directory.EnumerateFiles(xmlP5tDir, "*.xml", SearchOption.AllDirectories))
                result.Add(NormalizeRel(Path.GetRelativePath(repoDir, file)));
        }

        return result.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string CreateBackupDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ReadZen", "git-keep-local", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // ignore temp cleanup failures
        }
    }

    /// <summary>
    /// Extracts custom (non-base) master entries from the local master-dates.json for sharing.
    /// </summary>

    private static void CopyDirectoryContents(string sourceDir, string destinationDir)
    {
        if (!Directory.Exists(sourceDir))
            return;

        Directory.CreateDirectory(destinationDir);

        foreach (var dir in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, dir);
            Directory.CreateDirectory(Path.Combine(destinationDir, rel));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, file);
            var dst = Path.Combine(destinationDir, rel);
            var dstDir = Path.GetDirectoryName(dst);
            if (!string.IsNullOrWhiteSpace(dstDir))
                Directory.CreateDirectory(dstDir);
            File.Copy(file, dst, overwrite: true);
        }
    }

    private static List<Models.MasterDateEntry> ExtractCustomMasterEntries(string repoDir)
    {
        var baseNames = MasterDatesService.LoadBaseNameSet();
        var result = new List<Models.MasterDateEntry>();

        try
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "master-dates.json");
            if (!File.Exists(filePath))
                return result;

            var json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("masters", out var mastersEl))
                return result;

            foreach (var m in mastersEl.EnumerateArray())
            {
                var names = new List<string>();
                if (m.TryGetProperty("names", out var namesEl))
                {
                    foreach (var n in namesEl.EnumerateArray())
                    {
                        var s = n.GetString();
                        if (!string.IsNullOrEmpty(s)) names.Add(s);
                    }
                }

                int floruit = m.TryGetProperty("floruit", out var f) ? f.GetInt32() : 0;
                int death = m.TryGetProperty("death", out var d) ? d.GetInt32() : 0;

                var entry = new Models.MasterDateEntry
                {
                    Names = names,
                    Floruit = floruit,
                    Death = death
                };

                // Only include if NOT a base entry
                if (!MasterDatesService.OverlapsWithBase(entry, baseNames))
                    result.Add(entry);
            }
        }
        catch
        {
            // Non-fatal
        }

        return result;
    }

    // ----- Git process helpers -----

    private sealed record GitRunResult(bool Success, string? Error);

    private static async Task<GitRunResult> RunGitAsync(
        string repoDir,
        string arg0,
        string? arg1 = null,
        string? arg2 = null,
        string? arg3 = null,
        string? arg4 = null,
        string? arg5 = null,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        static string QuoteIfNeeded(string s) => s.Contains(' ') ? $"\"{s}\"" : s;

        var args = new[] { arg0, arg1, arg2, arg3, arg4, arg5 }
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a!)
            .ToArray();

        var psi = new ProcessStartInfo
        {
            FileName = GitBinaryLocator.ResolveGitExecutablePath(),
            WorkingDirectory = repoDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            Arguments = string.Join(" ", args.Select(QuoteIfNeeded))
        };

        GitBinaryLocator.EnrichProcessStartInfoForBundledGit(psi);

        try
        {
            psi.Environment["GIT_TERMINAL_PROMPT"] = "0";
            psi.Environment["GCM_INTERACTIVE"] = "Always";
        }
        catch { }

        using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var sbErr = new StringBuilder();

        try
        {
            if (!p.Start())
                return new GitRunResult(false, "Failed to start git process.");

            using var reg = ct.Register(() =>
            {
                try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
            });

            Task readOut = Task.Run(async () =>
            {
                while (!p.StandardOutput.EndOfStream)
                {
                    var line = await p.StandardOutput.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                        progress?.Report(line);
                }
            });

            Task readErr = Task.Run(async () =>
            {
                while (!p.StandardError.EndOfStream)
                {
                    var line = await p.StandardError.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        sbErr.AppendLine(line);
                        progress?.Report("[git] " + line);
                    }
                }
            });

            await Task.WhenAll(readOut, readErr);
            await p.WaitForExitAsync(ct);

            return p.ExitCode == 0
                ? new GitRunResult(true, null)
                : new GitRunResult(false, sbErr.ToString().Trim());
        }
        catch (OperationCanceledException)
        {
            return new GitRunResult(false, "Canceled.");
        }
        catch (Exception ex)
        {
            return new GitRunResult(false, ex.Message);
        }
    }

    private static async Task<string?> RunGitArgsAsync(
        string repoDir,
        CancellationToken ct,
        params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = GitBinaryLocator.ResolveGitExecutablePath(),
            WorkingDirectory = repoDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var arg in args.Where(a => !string.IsNullOrWhiteSpace(a)))
            psi.ArgumentList.Add(arg);

        GitBinaryLocator.EnrichProcessStartInfoForBundledGit(psi);

        try
        {
            psi.Environment["GIT_TERMINAL_PROMPT"] = "0";
        }
        catch { }

        using var p = new Process { StartInfo = psi };
        var sbOut = new StringBuilder();

        if (!p.Start())
            return null;

        using var reg = ct.Register(() =>
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
        });

        Task readOut = Task.Run(async () =>
        {
            while (!p.StandardOutput.EndOfStream)
            {
                var line = await p.StandardOutput.ReadLineAsync();
                if (line != null)
                    sbOut.AppendLine(line);
            }
        });

        Task readErr = Task.Run(async () =>
        {
            while (!p.StandardError.EndOfStream)
                await p.StandardError.ReadLineAsync();
        });

        await Task.WhenAll(readOut, readErr);
        await p.WaitForExitAsync(ct);

        if (p.ExitCode != 0)
            return null;

        var output = sbOut.ToString();
        return string.IsNullOrWhiteSpace(output) ? null : output;
    }

    private static async Task<string?> RunGitOutputAsync(
        string repoDir,
        string arg0,
        string? arg1 = null,
        CancellationToken ct = default)
    {
        var args = new[] { arg0, arg1 }
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a!)
            .ToArray();

        static string QuoteIfNeeded(string s) => s.Contains(' ') ? $"\"{s}\"" : s;

        var psi = new ProcessStartInfo
        {
            FileName = GitBinaryLocator.ResolveGitExecutablePath(),
            WorkingDirectory = repoDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            Arguments = string.Join(" ", args.Select(QuoteIfNeeded))
        };

        GitBinaryLocator.EnrichProcessStartInfoForBundledGit(psi);

        try
        {
            psi.Environment["GIT_TERMINAL_PROMPT"] = "0";
        }
        catch { }

        using var p = new Process { StartInfo = psi };

        var sbOut = new StringBuilder();

        if (!p.Start())
            return null;

        using var reg = ct.Register(() =>
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
        });

        Task readOut = Task.Run(async () =>
        {
            while (!p.StandardOutput.EndOfStream)
            {
                var line = await p.StandardOutput.ReadLineAsync();
                if (line != null)
                    sbOut.AppendLine(line);
            }
        });

        Task readErr = Task.Run(async () =>
        {
            while (!p.StandardError.EndOfStream)
                await p.StandardError.ReadLineAsync();
        });

        await Task.WhenAll(readOut, readErr);
        await p.WaitForExitAsync(ct);

        if (p.ExitCode != 0)
            return null;

        var output = sbOut.ToString();
        return string.IsNullOrWhiteSpace(output) ? null : output;
    }

    // ----- Persist last contrib branch -----

    private sealed record GitTabState(string RepoDir, string LastContribBranch, DateTimeOffset SavedAt);

    private static string GetStateFilePath()
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ReadZen");

            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "git-tab-state.json");
        }
        catch
        {
            return Path.Combine(AppContext.BaseDirectory, "git-tab-state.json");
        }
    }

    private void PersistLastBranchToDisk(string repoDir, string branch)
    {
        try
        {
            var path = GetStateFilePath();
            var state = new GitTabState(repoDir, branch, DateTimeOffset.Now);

            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(path, json);
        }
        catch
        {
            // ignore
        }
    }

    private void TryRestoreLastBranchFromDisk()
    {
        try
        {
            var transDir = GetTranslationRepoDir();
            var parentDir = GetTargetRepoDir();
            var path = GetStateFilePath();
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            var state = JsonSerializer.Deserialize<GitTabState>(json);
            if (state == null) return;

            // Match against translation repo dir (new) or parent dir (for forward compat)
            var savedNorm = NormalizePath(state.RepoDir);
            if (!string.Equals(savedNorm, NormalizePath(transDir), StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(savedNorm, NormalizePath(parentDir), StringComparison.OrdinalIgnoreCase))
                return;

            if (!string.IsNullOrWhiteSpace(state.LastContribBranch))
                _lastContribBranch = state.LastContribBranch;
        }
        catch
        {
            // ignore
        }
    }

    private static string NormalizePath(string p)
    {
        try { return Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
        catch { return (p ?? "").Trim(); }
    }
}




