using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CbetaTranslator.App.Services;

namespace CbetaTranslator.App.Views;

public partial class GitTabView : UserControl
{
    private const string RepoUrl = "https://github.com/Fabulu/CbetaZenTexts.git";
    private const string RepoFolderName = "CbetaZenTexts";

    private const string RepoTranslatedRoot = "xml-p5t"; // where translated files live inside the repo
    private const string UpstreamOwner = "Fabulu";
    private const string UpstreamRepo = "CbetaZenTexts";

    private static readonly string[] LocalIgnorePatterns =
    {
        "index.cache.json",
        "search.index.manifest.json",
        "index.debug.log",
        "*.log"
    };

    private Button? _btnPickDest;
    private Button? _btnGetFiles;
    private Button? _btnCancel;

    private Button? _btnAuth;
    private Button? _btnPushPr;

    private Button? _btnPanic;

    private TextBlock? _txtDest;
    private TextBlock? _txtProgress;
    private TextBox? _txtLog;

    private TextBox? _txtCommitMessage;
    private Button? _btnSend;
    private TextBlock? _txtSelected;

    private string? _baseDestFolder;
    private string? _currentRepoRoot;

    private string? _selectedRelPath;
    private CancellationTokenSource? _cts;

    private readonly IGitRepoService _git = new GitRepoService();
    private readonly IGitHubAuthService _auth = new GitHubAuthService();
    private readonly IGitHubApiService _api = new GitHubApiService();

    private string? _githubAccessToken;
    private string? _githubLogin;

    private string? _lastContribBranch;

    public event EventHandler<string>? Status;
    public event EventHandler<string>? RootCloned;
    public event Func<string, Task<bool>>? EnsureTranslatedForSelectedRequested;

    public GitTabView()
    {
        InitializeComponent();
        FindControls();
        WireEvents();

        _baseDestFolder = GetDefaultBaseFolder();
        UpdateDestLabel();
        UpdateSelectedLabel();

        TryRestoreLastBranchFromDisk();

        SetProgress("Ready.");
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void FindControls()
    {
        _btnPickDest = this.FindControl<Button>("BtnPickDest");
        _btnGetFiles = this.FindControl<Button>("BtnGetFiles");
        _btnCancel = this.FindControl<Button>("BtnCancel");

        _btnAuth = this.FindControl<Button>("BtnAuth");
        _btnPushPr = this.FindControl<Button>("BtnPushPr");

        _btnPanic = this.FindControl<Button>("BtnPanic");

        _txtDest = this.FindControl<TextBlock>("TxtDest");
        _txtProgress = this.FindControl<TextBlock>("TxtProgress");
        _txtLog = this.FindControl<TextBox>("TxtLog");

        _txtCommitMessage = this.FindControl<TextBox>("TxtCommitMessage");
        _btnSend = this.FindControl<Button>("BtnSendContribution");
        _txtSelected = this.FindControl<TextBlock>("TxtSelected");
    }

    private void WireEvents()
    {
        if (_btnPickDest != null) _btnPickDest.Click += async (_, _) => await PickDestAsync();
        if (_btnGetFiles != null) _btnGetFiles.Click += async (_, _) => await GetOrUpdateFilesAsync();
        if (_btnCancel != null) _btnCancel.Click += (_, _) => Cancel();

        if (_btnSend != null) _btnSend.Click += async (_, _) => await SendContributionLocalAsync();

        if (_btnAuth != null) _btnAuth.Click += async (_, _) => await AuthorizeAsync();
        if (_btnPushPr != null) _btnPushPr.Click += async (_, _) => await PushAndCreatePrAsync();

        if (_btnPanic != null) _btnPanic.Click += async (_, _) => await PanicButtonAsync();

        AttachedToVisualTree += (_, _) =>
        {
            UpdateDestLabel();
            UpdateSelectedLabel();
            TryRestoreLastBranchFromDisk();
        };
    }

    public void SetCurrentRepoRoot(string? rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            return;

        var root = rootPath.Trim();

        if (Directory.Exists(root) && Directory.Exists(Path.Combine(root, ".git")))
        {
            _currentRepoRoot = root;
            _baseDestFolder = Path.GetDirectoryName(root);
            UpdateDestLabel();
            TryRestoreLastBranchFromDisk();
            return;
        }

        if (Directory.Exists(root))
        {
            _currentRepoRoot = null;
            _baseDestFolder = root;
            UpdateDestLabel();
        }
    }

    public void SetSelectedRelPath(string? relPath)
    {
        _selectedRelPath = string.IsNullOrWhiteSpace(relPath) ? null : NormalizeRel(relPath);
        UpdateSelectedLabel();
    }

    private void UpdateSelectedLabel()
    {
        if (_txtSelected == null) return;

        _txtSelected.Text = string.IsNullOrWhiteSpace(_selectedRelPath)
            ? "Selected: (none)"
            : "Selected: " + _selectedRelPath;
    }

    private static string GetDefaultBaseFolder()
    {
        try
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (string.IsNullOrWhiteSpace(docs))
                docs = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            return Path.Combine(docs, "CbetaTranslator");
        }
        catch
        {
            return AppContext.BaseDirectory;
        }
    }

    private string GetTargetRepoDir()
    {
        if (!string.IsNullOrWhiteSpace(_currentRepoRoot) &&
            Directory.Exists(_currentRepoRoot) &&
            Directory.Exists(Path.Combine(_currentRepoRoot, ".git")))
        {
            return _currentRepoRoot!;
        }

        var baseDir = _baseDestFolder ?? GetDefaultBaseFolder();
        return Path.Combine(baseDir, RepoFolderName);
    }

    private void UpdateDestLabel()
    {
        var target = GetTargetRepoDir();
        if (_txtDest != null)
            _txtDest.Text = "Location: " + target;
    }

    private async Task PickDestAsync()
    {
        try
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner?.StorageProvider == null)
            {
                SetProgress("Storage provider not available.");
                return;
            }

            var picked = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select a folder where the repo will be stored"
            });

            var folder = picked.Count > 0 ? picked[0] : null;
            if (folder == null) return;

            _baseDestFolder = folder.Path.LocalPath;
            _currentRepoRoot = null;
            UpdateDestLabel();
            TryRestoreLastBranchFromDisk();

            Status?.Invoke(this, "Location updated.");
        }
        catch (Exception ex)
        {
            SetProgress("Pick folder failed: " + ex.Message);
            Status?.Invoke(this, "Pick folder failed: " + ex.Message);
        }
    }

    private async Task GetOrUpdateFilesAsync()
    {
        Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        SetButtonsBusy(true);
        ClearLog();

        var repoDir = GetTargetRepoDir();
        var baseDir = Path.GetDirectoryName(repoDir) ?? (_baseDestFolder ?? GetDefaultBaseFolder());

        try
        {
            AppendLog($"[repo] {RepoUrl}");
            AppendLog($"[path] {repoDir}");

            SetProgress("Checking git…");
            var gitOk = await _git.CheckGitAvailableAsync(ct);
            if (!gitOk)
            {
                SetProgress("Git not found. Install Git first.");
                AppendLog("[error] git not found in PATH");
                Status?.Invoke(this, "Git not found. Install Git first.");
                return;
            }

            var prog = new Progress<string>(line => Dispatcher.UIThread.Post(() => AppendLog(line)));

            // -------------------------
            // Repo exists -> UPDATE NO MATTER WHAT
            // -------------------------
            if (Directory.Exists(repoDir) && Directory.Exists(Path.Combine(repoDir, ".git")))
            {
                await _git.EnsureLocalExcludeAsync(repoDir, LocalIgnorePatterns, prog, ct);

                // If dirty, stash EVERYTHING so update never blocks.
                var statusBefore = await _git.GetStatusPorcelainAsync(repoDir, ct);
                bool hadLocalNoise = statusBefore.Length > 0;

                if (hadLocalNoise)
                {
                    SetProgress("Saving your local changes…");
                    AppendLog("[step] stash (update safety)");
                    var stashAll = await _git.StashAllAsync(repoDir, "cbeta-update-autostash", prog, ct);
                    if (!stashAll.Success)
                    {
                        SetProgress("Update failed (could not stash).");
                        AppendLog("[error] " + (stashAll.Error ?? "unknown error"));
                        Status?.Invoke(this, "Update failed (could not stash).");
                        return;
                    }
                }

                SetProgress("Fetching…");
                var fetch = await _git.FetchAsync(repoDir, prog, ct);
                if (!fetch.Success)
                {
                    SetProgress("Fetch failed.");
                    AppendLog("[error] " + (fetch.Error ?? "unknown error"));
                    Status?.Invoke(this, "Fetch failed.");
                    return;
                }

                // No merge, no ff-only. Force to origin/main.
                SetProgress("Updating files…");
                AppendLog("[step] reset --hard origin/main");
                var reset = await _git.HardResetToRemoteMainAsync(repoDir, "origin", "main", prog, ct);
                if (!reset.Success)
                {
                    SetProgress("Update failed (reset).");
                    AppendLog("[error] " + (reset.Error ?? "unknown error"));
                    Status?.Invoke(this, "Update failed (reset).");
                    return;
                }

                // Remove untracked (only after we stashed with -u).
                AppendLog("[step] clean -fd");
                var clean = await _git.CleanUntrackedAsync(repoDir, prog, ct);
                if (!clean.Success)
                {
                    SetProgress("Update failed (clean).");
                    AppendLog("[error] " + (clean.Error ?? "unknown error"));
                    Status?.Invoke(this, "Update failed (clean).");
                    return;
                }

                if (hadLocalNoise)
                {
                    SetProgress("Restoring your local changes…");
                    AppendLog("[step] stash pop");
                    var pop = await _git.StashPopAsync(repoDir, prog, ct);
                    if (!pop.Success)
                    {
                        SetProgress("Updated, but your local changes conflicted.");
                        AppendLog("[warn] stash pop failed (likely conflicts).");
                        AppendLog("[warn] Your stash should still exist. Run: git stash list");
                        AppendLog("[warn] Then resolve conflicts and run: git stash pop");
                        Status?.Invoke(this, "Updated, but local changes conflicted.");
                    }
                }

                SetProgress("Up to date.");
                AppendLog("[ok] update complete: " + repoDir);

                _currentRepoRoot = repoDir;
                _baseDestFolder = Path.GetDirectoryName(repoDir);
                UpdateDestLabel();
                TryRestoreLastBranchFromDisk();

                RootCloned?.Invoke(this, repoDir);
                Status?.Invoke(this, "Repo updated.");
                return;
            }

            // -------------------------
            // Repo missing -> CLONE
            // -------------------------

            if (!Directory.Exists(baseDir))
                Directory.CreateDirectory(baseDir);

            if (Directory.Exists(repoDir) && Directory.EnumerateFileSystemEntries(repoDir).Any())
            {
                SetProgress("Folder exists but is not a Git repo: " + repoDir);
                AppendLog("[error] target folder exists and is not a git repo");
                AppendLog("Pick a different location or delete that folder.");
                Status?.Invoke(this, "Target folder exists but is not a Git repo.");
                return;
            }

            SetProgress("Cloning…");
            var clone = await _git.CloneAsync(RepoUrl, repoDir, prog, ct);
            if (!clone.Success)
            {
                SetProgress("Clone failed.");
                AppendLog("[error] " + (clone.Error ?? "unknown error"));
                Status?.Invoke(this, "Clone failed.");
                return;
            }

            await _git.EnsureLocalExcludeAsync(repoDir, LocalIgnorePatterns, prog, ct);

            SetProgress("Done. Repo is ready.");
            AppendLog("[ok] clone complete: " + repoDir);

            _currentRepoRoot = repoDir;
            _baseDestFolder = Path.GetDirectoryName(repoDir);
            UpdateDestLabel();
            TryRestoreLastBranchFromDisk();

            RootCloned?.Invoke(this, repoDir);
            Status?.Invoke(this, "Repo cloned.");
        }
        catch (OperationCanceledException)
        {
            SetProgress("Canceled.");
            AppendLog("[cancel] canceled");
            Status?.Invoke(this, "Canceled.");
        }
        catch (Exception ex)
        {
            SetProgress("Failed: " + ex.Message);
            AppendLog("[error] " + ex);
            Status?.Invoke(this, "Failed: " + ex.Message);
        }
        finally
        {
            SetButtonsBusy(false);
        }
    }

    // -------------------------
    // PANIC BUTTON (stash + drop)
    // -------------------------

    private async Task PanicButtonAsync()
    {
        Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        SetButtonsBusy(true);
        ClearLog();

        try
        {
            var repoDir = GetTargetRepoDir();
            if (!Directory.Exists(repoDir) || !Directory.Exists(Path.Combine(repoDir, ".git")))
            {
                SetProgress("Repo not ready. Click Update Files first.");
                AppendLog("[error] repo not found / not a git working tree");
                return;
            }

            SetProgress("Checking git…");
            var gitOk = await _git.CheckGitAvailableAsync(ct);
            if (!gitOk)
            {
                SetProgress("Git not found. Install Git first.");
                AppendLog("[error] git not found in PATH");
                return;
            }

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner == null)
            {
                SetProgress("No window context for confirmation dialog.");
                return;
            }

            bool confirm = await ConfirmAsync(
                owner,
                title: "Don't Panic",
                message:
                    "This will ERASE all your local, uncommitted changes in the repo.\n\n" +
                    "It does two commands:\n" +
                    "  1) git stash\n" +
                    "  2) git stash drop\n\n" +
                    "Result: your edits are gone and cannot be recovered.\n\n" +
                    "Only do this if you want to throw away local work and return to a clean state.",
                yesText: "Yes, erase my local changes",
                noText: "No, keep my changes");

            if (!confirm)
            {
                SetProgress("Canceled.");
                AppendLog("[cancel] user chose safety");
                return;
            }

            var prog = new Progress<string>(line => Dispatcher.UIThread.Post(() => AppendLog(line)));

            SetProgress("PANIC: stashing…");
            AppendLog("[panic] git stash push -u");
            var stash = await RunGitAsync(repoDir, "stash", "push", "-u", "-m", "panic-button", progress: prog, ct: ct);
            if (!stash.Success)
            {
                SetProgress("Panic failed (stash).");
                AppendLog("[error] " + stash.Error);
                return;
            }

            SetProgress("PANIC: dropping stash…");
            AppendLog("[panic] git stash drop");
            var drop = await RunGitAsync(repoDir, "stash", "drop", progress: prog, ct: ct);
            if (!drop.Success)
            {
                SetProgress("Panic partial failure (drop).");
                AppendLog("[error] " + drop.Error);
                AppendLog("[hint] Your changes might still be in stash. Try: git stash list");
                return;
            }

            SetProgress("Repo cleaned. You're safe now.");
            AppendLog("[ok] panic complete: local uncommitted changes erased");
            Status?.Invoke(this, "Panic complete: repo cleaned.");
        }
        catch (OperationCanceledException)
        {
            SetProgress("Canceled.");
            AppendLog("[cancel] canceled");
        }
        catch (Exception ex)
        {
            SetProgress("Panic failed: " + ex.Message);
            AppendLog("[error] " + ex);
        }
        finally
        {
            SetButtonsBusy(false);
        }
    }

    private sealed record GitRunResult(bool Success, string? Error);

    /// <summary>
    /// Runs "git {args...}" in repoDir, capturing stdout/stderr and streaming lines to progress.
    /// This is self-contained so the Panic Button does not depend on extra IGitRepoService methods.
    /// </summary>
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

        var args = new[]
        {
            arg0, arg1, arg2, arg3, arg4, arg5
        }.Where(a => !string.IsNullOrWhiteSpace(a))
         .Select(a => a!)
         .ToArray();

        var psi = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = repoDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        psi.Arguments = string.Join(" ", args.Select(QuoteIfNeeded));

        using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };

        var sbErr = new StringBuilder();

        try
        {
            if (!p.Start())
                return new GitRunResult(false, "Failed to start git process.");

            using var reg = ct.Register(() =>
            {
                try { if (!p.HasExited) p.Kill(entireProcessTree: true); }
                catch { }
            });

            Task readOut = Task.Run(async () =>
            {
                while (!p.StandardOutput.EndOfStream)
                {
                    var line = await p.StandardOutput.ReadLineAsync();
                    if (line != null && line.Length > 0)
                        progress?.Report(line);
                }
            }, CancellationToken.None);

            Task readErr = Task.Run(async () =>
            {
                while (!p.StandardError.EndOfStream)
                {
                    var line = await p.StandardError.ReadLineAsync();
                    if (line != null && line.Length > 0)
                    {
                        sbErr.AppendLine(line);
                        progress?.Report("[git] " + line);
                    }
                }
            }, CancellationToken.None);

            await Task.WhenAll(readOut, readErr);
            await p.WaitForExitAsync(ct);

            if (p.ExitCode != 0)
                return new GitRunResult(false, sbErr.ToString().Trim());

            return new GitRunResult(true, null);
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

    private static async Task<bool> ConfirmAsync(Window owner, string title, string message, string yesText, string noText)
    {
        var dlg = new ConfirmDialog(title, message, yesText, noText)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        return await dlg.ShowDialog<bool>(owner);
    }

    private sealed class ConfirmDialog : Window
    {
        public ConfirmDialog(string title, string message, string yesText, string noText)
        {
            Title = title;
            Width = 560;
            Height = 320;
            CanResize = false;

            var root = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,* ,Auto"),
                Margin = new Thickness(16),
                RowSpacing = 12
            };

            var header = new TextBlock
            {
                Text = title,
                FontSize = 20,
                FontWeight = FontWeight.SemiBold
            };

            var bodyBorder = new Border
            {
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                Background = Brushes.Transparent,
                Padding = new Thickness(12),
                Child = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 14
                    }
                }
            };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8
            };

            var btnNo = new Button { Content = noText, MinWidth = 160 };
            btnNo.Click += (_, _) => Close(false);

            var btnYes = new Button
            {
                Content = yesText,
                MinWidth = 220
            };
            btnYes.Classes.Add("panic");
            btnYes.Click += (_, _) => Close(true);

            buttons.Children.Add(btnNo);
            buttons.Children.Add(btnYes);

            root.Children.Add(header);
            Grid.SetRow(header, 0);

            root.Children.Add(bodyBorder);
            Grid.SetRow(bodyBorder, 1);

            root.Children.Add(buttons);
            Grid.SetRow(buttons, 2);

            Content = root;
        }
    }

    // -------------------------
    // 1) LOCAL COMMIT ONLY
    // -------------------------

    private async Task SendContributionLocalAsync()
    {
        Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        SetButtonsBusy(true);
        ClearLog();

        try
        {
            var repoDir = GetTargetRepoDir();
            if (!Directory.Exists(repoDir) || !Directory.Exists(Path.Combine(repoDir, ".git")))
            {
                SetProgress("Repo not ready. Click Update Files first.");
                AppendLog("[error] repo not found / not a git working tree");
                return;
            }

            if (string.IsNullOrWhiteSpace(_selectedRelPath))
            {
                SetProgress("Select a file first.");
                AppendLog("[error] no selected file");
                return;
            }

            var cbetaRel = NormalizeRel(_selectedRelPath);
            var repoRel = NormalizeRel($"{RepoTranslatedRoot}/{cbetaRel}");

            if (EnsureTranslatedForSelectedRequested != null)
            {
                SetProgress("Preparing translated XML from Markdown…");
                bool prepared = true;
                foreach (var fn in EnsureTranslatedForSelectedRequested.GetInvocationList().Cast<Func<string, Task<bool>>>())
                {
                    if (!await fn(cbetaRel))
                    {
                        prepared = false;
                        break;
                    }
                }

                if (!prepared)
                {
                    SetProgress("Preparation failed. Save in Edit tab and retry.");
                    AppendLog("[error] failed to materialize translated XML for selected file");
                    return;
                }
            }

            AppendLog("[map] cbeta: " + cbetaRel);
            AppendLog("[map] repo : " + repoRel);

            string absTarget = Path.Combine(repoDir, repoRel.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absTarget))
            {
                SetProgress("Translated file does not exist in repo yet. Save it first.");
                AppendLog("[error] missing: " + absTarget);
                AppendLog("Expected at: " + repoRel);
                return;
            }

            SetProgress("Checking git…");
            var gitOk = await _git.CheckGitAvailableAsync(ct);
            if (!gitOk)
            {
                SetProgress("Git not found. Install Git first.");
                AppendLog("[error] git not found in PATH");
                return;
            }

            var prog = new Progress<string>(line => Dispatcher.UIThread.Post(() => AppendLog(line)));

            await _git.EnsureLocalExcludeAsync(repoDir, LocalIgnorePatterns, prog, ct);
            await _git.EnsureUserIdentityAsync(repoDir, prog, ct);

            var status = await _git.GetStatusPorcelainAsync(repoDir, ct);

            bool targetMentioned = status.Any(l =>
                l.EndsWith(" " + repoRel, StringComparison.OrdinalIgnoreCase) ||
                l.EndsWith("\t" + repoRel, StringComparison.OrdinalIgnoreCase) ||
                l.Contains(repoRel, StringComparison.OrdinalIgnoreCase));

            if (!targetMentioned)
            {
                SetProgress("No changes detected for selected file (git status).");
                AppendLog("[warn] git status does not show changes for: " + repoRel);
                AppendLog("If you edited it, ensure you saved, and that you are using the repo clone as root.");
                return;
            }

            string originalBranch = await _git.GetCurrentBranchAsync(repoDir, ct);
            AppendLog("[git] current branch: " + originalBranch);

            string msg = (_txtCommitMessage?.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(msg))
                msg = "Translation update: " + Path.GetFileName(cbetaRel);

            string branchName = MakeBranchName(cbetaRel);

            SetProgress("Staging selected file…");
            AppendLog("[step] git add -- " + repoRel);
            var stage = await _git.StagePathAsync(repoDir, repoRel, prog, ct);
            if (!stage.Success)
            {
                SetProgress("Stage failed.");
                AppendLog("[error] " + stage.Error);
                return;
            }

            SetProgress("Stashing other work…");
            AppendLog("[step] git stash push -u -k");
            var stash = await _git.StashKeepIndexAsync(repoDir, "cbeta-autostash", prog, ct);
            if (!stash.Success)
            {
                SetProgress("Stash failed.");
                AppendLog("[error] " + stash.Error);
                return;
            }

            SetProgress("Creating branch…");
            AppendLog("[step] new branch: " + branchName);
            var br = await _git.SwitchCreateBranchAsync(repoDir, branchName, prog, ct);
            if (!br.Success)
            {
                SetProgress("Branch create failed.");
                AppendLog("[error] " + br.Error);
                await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                return;
            }

            SetProgress("Committing…");
            AppendLog("[step] commit message: " + msg);
            var commit = await _git.CommitAsync(repoDir, msg, prog, ct);
            if (!commit.Success)
            {
                SetProgress("Commit failed.");
                AppendLog("[error] " + commit.Error);
                await SafeRestoreAsync(repoDir, originalBranch, prog, ct);
                return;
            }

            _lastContribBranch = branchName;
            PersistLastBranchToDisk(repoDir, branchName);

            SetProgress("Local commit created.");
            AppendLog("[ok] created single-file commit on branch: " + branchName);
            AppendLog("[next] 2) Authorize GitHub, then 3) Push + Create PR");

            SetProgress("Restoring your other work…");
            await SafeRestoreAsync(repoDir, originalBranch, prog, ct);

            Status?.Invoke(this, "Local commit ready on branch: " + branchName);
        }
        catch (OperationCanceledException)
        {
            SetProgress("Canceled.");
            AppendLog("[cancel] canceled");
        }
        catch (Exception ex)
        {
            SetProgress("Failed: " + ex.Message);
            AppendLog("[error] " + ex);
        }
        finally
        {
            SetButtonsBusy(false);
        }
    }

    // -------------------------
    // 2) AUTH
    // -------------------------

    private async Task AuthorizeAsync()
    {
        Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        SetButtonsBusy(true);
        ClearLog();

        try
        {
            var prog = new Progress<string>(line => Dispatcher.UIThread.Post(() => AppendLog(line)));

            SetProgress("Authorizing…");
            var token = await _auth.AuthorizeDeviceFlowAsync(prog, ct);
            if (token == null)
            {
                SetProgress("Auth failed.");
                return;
            }

            _githubAccessToken = token.access_token;

            var me = await _api.GetMeAsync(_githubAccessToken, ct);
            _githubLogin = me?.login;

            AppendLog("[auth] user: " + (_githubLogin ?? "(unknown)"));
            SetProgress("Authorized.");
            Status?.Invoke(this, "GitHub authorized.");
        }
        catch (OperationCanceledException)
        {
            SetProgress("Canceled.");
            AppendLog("[cancel] canceled");
        }
        catch (Exception ex)
        {
            SetProgress("Auth failed: " + ex.Message);
            AppendLog("[error] " + ex);
        }
        finally
        {
            SetButtonsBusy(false);
        }
    }

    // -------------------------
    // 3) PUSH + PR
    // -------------------------

    private async Task PushAndCreatePrAsync()
    {
        Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        SetButtonsBusy(true);
        ClearLog();

        try
        {
            var repoDir = GetTargetRepoDir();
            if (!Directory.Exists(repoDir) || !Directory.Exists(Path.Combine(repoDir, ".git")))
            {
                SetProgress("Repo not ready. Click Update Files first.");
                AppendLog("[error] repo not found / not a git working tree");
                return;
            }

            if (string.IsNullOrWhiteSpace(_lastContribBranch))
                TryRestoreLastBranchFromDisk();

            if (string.IsNullOrWhiteSpace(_lastContribBranch))
            {
                SetProgress("Step 1 not done yet.");
                AppendLog("[error] no prepared branch found");
                AppendLog("Do: 1) Create local commit (single file) first.");
                return;
            }

            var prog = new Progress<string>(line => Dispatcher.UIThread.Post(() => AppendLog(line)));

            // Ensure OAuth for API calls (fork + PR)
            if (string.IsNullOrWhiteSpace(_githubAccessToken) || string.IsNullOrWhiteSpace(_githubLogin))
            {
                SetProgress("Need GitHub auth first…");
                AppendLog("[step] authorize");
                var token = await _auth.AuthorizeDeviceFlowAsync(prog, ct);
                if (token == null)
                {
                    SetProgress("Auth failed.");
                    return;
                }

                _githubAccessToken = token.access_token;

                var me = await _api.GetMeAsync(_githubAccessToken, ct);
                _githubLogin = me?.login;

                AppendLog("[auth] user: " + (_githubLogin ?? "(unknown)"));

                if (string.IsNullOrWhiteSpace(_githubLogin))
                {
                    SetProgress("Auth ok but could not read username.");
                    AppendLog("[error] GET /user failed");
                    return;
                }
            }

            bool isUpstreamOwner = string.Equals(_githubLogin, UpstreamOwner, StringComparison.OrdinalIgnoreCase);

            // SECURITY: remove any previously-created token remote URL (from old builds)
            await ScrubTokenizedForkRemoteIfAny(repoDir, prog, ct);

            string remoteName;
            string remoteUrlClean;
            string prHeadOwner;

            if (isUpstreamOwner)
            {
                // Maintainer mode: push to origin, PR from same repo.
                AppendLog("[mode] upstream owner detected -> no fork");
                remoteName = "origin";
                remoteUrlClean = RepoUrl;
                prHeadOwner = UpstreamOwner;
            }
            else
            {
                // Contributor mode: ensure fork exists; push to fork remote.
                SetProgress("Ensuring fork…");

                bool forkExists = await _api.ForkExistsAsync(_githubAccessToken!, _githubLogin!, UpstreamRepo, ct);
                if (!forkExists)
                {
                    AppendLog("[step] create fork");
                    var okFork = await _api.CreateForkAsync(_githubAccessToken!, UpstreamOwner, UpstreamRepo, ct);
                    if (!okFork)
                    {
                        SetProgress("Fork failed.");
                        AppendLog("[error] fork creation failed");
                        return;
                    }

                    var ready = await _api.WaitForForkAsync(_githubAccessToken!, _githubLogin!, UpstreamRepo, TimeSpan.FromSeconds(60), prog, ct);
                    if (!ready)
                    {
                        SetProgress("Fork not ready yet.");
                        AppendLog("[error] fork did not appear within timeout");
                        return;
                    }
                }

                remoteName = "fork";
                remoteUrlClean = $"https://github.com/{_githubLogin}/{UpstreamRepo}.git";
                prHeadOwner = _githubLogin!;
            }

            // Ensure remote uses CLEAN url (no token embedded)
            SetProgress("Configuring remote…");
            AppendLog("[step] remote " + remoteName + " -> " + remoteUrlClean);

            var rem = await _git.EnsureRemoteUrlAsync(repoDir, remoteName, remoteUrlClean, prog, ct);
            if (!rem.Success)
            {
                SetProgress("Remote failed.");
                AppendLog("[error] " + rem.Error);
                return;
            }

            // Push branch (requires credential helper on Linux; on Windows Git for Windows includes GCM)
            SetProgress("Pushing branch…");
            AppendLog("[step] push -u " + remoteName + " " + _lastContribBranch);
            AppendLog("[hint] If Git asks you to log in, complete it once, then retry.");

            var push = await _git.PushSetUpstreamAsync(repoDir, remoteName, _lastContribBranch!, prog, ct);
            if (!push.Success)
            {
                SetProgress("Push failed.");
                AppendLog("[error] " + push.Error);

                AppendPushFailureHints(push.Error);
                return;
            }

            // Create PR (API)
            SetProgress("Creating PR…");

            string head = $"{prHeadOwner}:{_lastContribBranch}";
            string baseBranch = "main";

            string title = (_txtCommitMessage?.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(title))
                title = "Translation update";

            string body =
                "Created by CbetaTranslator.\n\n" +
                $"Branch: `{_lastContribBranch}`";

            var prUrl = await _api.CreatePullRequestAsync(
                _githubAccessToken!,
                UpstreamOwner,
                UpstreamRepo,
                head,
                baseBranch,
                title,
                body,
                ct);

            if (string.IsNullOrWhiteSpace(prUrl))
            {
                SetProgress("PR failed.");
                AppendLog("[error] create PR failed (API returned null)");
                return;
            }

            AppendLog("[ok] PR created: " + prUrl);
            SetProgress("PR created.");

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = prUrl,
                    UseShellExecute = true
                });
            }
            catch { }

            Status?.Invoke(this, "PR created: " + prUrl);
        }
        catch (OperationCanceledException)
        {
            SetProgress("Canceled.");
            AppendLog("[cancel] canceled");
        }
        catch (Exception ex)
        {
            SetProgress("Failed: " + ex.Message);
            AppendLog("[error] " + ex);
        }
        finally
        {
            SetButtonsBusy(false);
        }
    }

    private void AppendPushFailureHints(string? err)
    {
        err ??= "";

        // Common GitHub HTTPS auth failures (especially on Linux without a helper / GUI)
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

        if (looksLikeRepoNotFound)
        {
            AppendLog("[hint] Git says 'Repository not found'. Usually: wrong remote URL or you are not authenticated.");
        }

        if (looksLikeWrongAccount)
        {
            AppendLog("[hint] If you see 403: you are logged into the wrong GitHub account in the git credential helper.");
        }

        if (looksLikeNoCredStore)
        {
            AppendLog("[hint] Git Credential Manager is installed but no credential store is configured.");
            AppendLog("[hint] On GNOME desktops, you usually want: secretservice (GNOME Keyring).");
            AppendLog("[linux] Run these commands, then retry Step 3:");
            AppendLog("  git config --global credential.helper manager");
            AppendLog("  git config --global credential.credentialStore secretservice");
            AppendLog("  git-credential-manager configure");
            AppendLog("[linux] If it still fails, run and share:");
            AppendLog("  git config --list --show-origin | grep -E \"credential.helper|credential.credentialStore\"");
            AppendLog("  git config --show-origin --get-all credential.helper");
            return;
        }

        if (looksLikeNoPrompt)
        {
            AppendLog("[hint] This usually means Git could not open an interactive login prompt on your system.");
            AppendLog("[hint] On Linux, install Git Credential Manager (GCM) so Git can authenticate via browser/device flow.");

            AppendLog("[linux] Recommended (cross-distro) install via .NET tool:");
            AppendLog("  dotnet tool install -g git-credential-manager");
            AppendLog("  git-credential-manager configure");

            AppendLog("[linux] If you prefer the .deb package (Debian/Ubuntu-style):");
            AppendLog("  1) Download the latest .deb from the GCM releases page");
            AppendLog("  2) sudo dpkg -i <downloaded-file.deb>");
            AppendLog("  3) git-credential-manager configure");

            AppendLog("[hint] After installing GCM, retry the Push + Create PR step.");
        }
        else
        {
            AppendLog("[hint] If this is an auth problem: install/configure Git Credential Manager (GCM) and retry.");
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

    private static string MakeBranchName(string cbetaRel)
    {
        string ts = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string core = cbetaRel.Replace('\\', '/');

        core = Regex.Replace(core, @"[^a-zA-Z0-9/\-_.]+", "-");
        core = core.Trim('-').Trim('/');
        if (core.Length > 80) core = core.Substring(core.Length - 80);

        return $"contrib/{core}/{ts}";
    }

    private static string NormalizeRel(string p)
        => (p ?? "").Replace('\\', '/').TrimStart('/');

    private void Cancel()
    {
        try { _cts?.Cancel(); } catch { }
        _cts = null;

        _git.TryCancelRunningProcess();
        SetButtonsBusy(false);
    }

    private void SetButtonsBusy(bool busy)
    {
        if (_btnCancel != null) _btnCancel.IsEnabled = busy;

        if (_btnGetFiles != null) _btnGetFiles.IsEnabled = !busy;
        if (_btnPickDest != null) _btnPickDest.IsEnabled = !busy;
        if (_btnSend != null) _btnSend.IsEnabled = !busy;

        if (_btnAuth != null) _btnAuth.IsEnabled = !busy;
        if (_btnPushPr != null) _btnPushPr.IsEnabled = !busy;

        if (_btnPanic != null) _btnPanic.IsEnabled = !busy;
    }

    private void SetProgress(string msg)
    {
        if (_txtProgress != null)
            _txtProgress.Text = msg;
    }

    private void ClearLog()
    {
        if (_txtLog != null)
            _txtLog.Text = "";
    }

    private void AppendLog(string line)
    {
        if (_txtLog == null) return;

        if (_txtLog.Text?.Length > 200_000)
            _txtLog.Text = _txtLog.Text.Substring(_txtLog.Text.Length - 120_000);

        _txtLog.Text += line + Environment.NewLine;

        try { _txtLog.CaretIndex = _txtLog.Text.Length; } catch { }
    }

    // -------------------------
    // Persist last contrib branch
    // -------------------------

    private sealed record GitTabState(string RepoDir, string LastContribBranch, DateTimeOffset SavedAt);

    private static string GetStateFilePath()
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CbetaTranslator");

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
            var repoDir = GetTargetRepoDir();
            var path = GetStateFilePath();
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            var state = JsonSerializer.Deserialize<GitTabState>(json);
            if (state == null) return;

            if (!string.Equals(NormalizePath(state.RepoDir), NormalizePath(repoDir), StringComparison.OrdinalIgnoreCase))
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