using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CbetaTranslator.App.Services;

namespace CbetaTranslator.App.Views;

public partial class GitTabView : UserControl
{
    private const string RepoUrl = "https://github.com/Fabulu/CbetaZenTexts.git";
    private const string RepoFolderName = "CbetaZenTexts";

    private const string RepoTranslatedRoot = "xml-p5t"; // where translated files live inside the repo

    private Button? _btnPickDest;
    private Button? _btnGetFiles;
    private Button? _btnCancel;

    private TextBlock? _txtDest;
    private TextBlock? _txtProgress;
    private TextBox? _txtLog;

    private TextBox? _txtCommitMessage;
    private Button? _btnSend;
    private TextBlock? _txtSelected;

    private string? _baseDestFolder;
    private string? _currentRepoRoot;

    // This is the CBETA logical relpath e.g. "T/T47/T47n1987A.xml"
    private string? _selectedRelPath;

    private CancellationTokenSource? _cts;

    private readonly IGitRepoService _git = new GitRepoService();

    public event EventHandler<string>? Status;
    public event EventHandler<string>? RootCloned;

    public GitTabView()
    {
        InitializeComponent();
        FindControls();
        WireEvents();

        _baseDestFolder = GetDefaultBaseFolder();
        UpdateDestLabel();
        UpdateSelectedLabel();
        SetProgress("Ready.");
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void FindControls()
    {
        _btnPickDest = this.FindControl<Button>("BtnPickDest");
        _btnGetFiles = this.FindControl<Button>("BtnGetFiles");
        _btnCancel = this.FindControl<Button>("BtnCancel");

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

        AttachedToVisualTree += (_, _) =>
        {
            UpdateDestLabel();
            UpdateSelectedLabel();
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
            return;
        }

        if (Directory.Exists(root))
        {
            _currentRepoRoot = null;
            _baseDestFolder = root;
            UpdateDestLabel();
        }
    }

    /// <summary>
    /// The main window should pass the CBETA logical relpath like "T/T47/T47n1987A.xml"
    /// </summary>
    public void SetSelectedRelPath(string? relPath)
    {
        _selectedRelPath = string.IsNullOrWhiteSpace(relPath) ? null : NormalizeRel(relPath);
        UpdateSelectedLabel();
    }

    private void UpdateSelectedLabel()
    {
        if (_txtSelected == null) return;

        if (string.IsNullOrWhiteSpace(_selectedRelPath))
            _txtSelected.Text = "Selected: (none)";
        else
            _txtSelected.Text = "Selected: " + _selectedRelPath;
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

            if (Directory.Exists(repoDir) && Directory.Exists(Path.Combine(repoDir, ".git")))
            {
                SetProgress("Fetching…");
                var fetchProg = new Progress<string>(line => Dispatcher.UIThread.Post(() => AppendLog(line)));
                var fetch = await _git.FetchAsync(repoDir, fetchProg, ct);

                if (!fetch.Success)
                {
                    SetProgress("Fetch failed.");
                    AppendLog("[error] " + (fetch.Error ?? "unknown error"));
                    Status?.Invoke(this, "Fetch failed: " + (fetch.Error ?? "unknown error"));
                    return;
                }

                // IMPORTANT: Pull is now "safe" even if dirty (service auto-stashes and restores).
                SetProgress("Pulling…");
                var pullProg = new Progress<string>(line => Dispatcher.UIThread.Post(() => AppendLog(line)));
                var pull = await _git.PullFfOnlyAsync(repoDir, pullProg, ct);

                if (!pull.Success)
                {
                    SetProgress("Pull failed.");
                    AppendLog("[error] " + (pull.Error ?? "unknown error"));
                    AppendLog("If this mentions stash conflicts: resolve them, then run 'git stash pop' manually.");
                    Status?.Invoke(this, "Pull failed: " + (pull.Error ?? "unknown error"));
                    return;
                }

                SetProgress("Up to date.");
                AppendLog("[ok] update complete: " + repoDir);

                _currentRepoRoot = repoDir;
                _baseDestFolder = Path.GetDirectoryName(repoDir);
                UpdateDestLabel();

                RootCloned?.Invoke(this, repoDir);
                Status?.Invoke(this, "Repo updated.");
                return;
            }

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
            var prog = new Progress<string>(line => Dispatcher.UIThread.Post(() => AppendLog(line)));
            var clone = await _git.CloneAsync(RepoUrl, repoDir, prog, ct);

            if (!clone.Success)
            {
                SetProgress("Clone failed.");
                AppendLog("[error] " + (clone.Error ?? "unknown error"));
                Status?.Invoke(this, "Clone failed: " + (clone.Error ?? "unknown error"));
                return;
            }

            SetProgress("Done. Repo is ready.");
            AppendLog("[ok] clone complete: " + repoDir);

            _currentRepoRoot = repoDir;
            _baseDestFolder = Path.GetDirectoryName(repoDir);
            UpdateDestLabel();

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
    // SEND CONTRIBUTION (LOCAL COMMIT ONLY)
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

            // Map CBETA relpath -> repo relpath under xml-p5t/
            var cbetaRel = NormalizeRel(_selectedRelPath);
            var repoRel = $"{RepoTranslatedRoot}/{cbetaRel}";
            repoRel = NormalizeRel(repoRel);

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

            // 1) Stage ONLY target (repo path)
            SetProgress("Staging selected file…");
            AppendLog("[step] git add -- " + repoRel);
            var stage = await _git.StagePathAsync(repoDir, repoRel, prog, ct);
            if (!stage.Success)
            {
                SetProgress("Stage failed.");
                AppendLog("[error] " + stage.Error);
                return;
            }

            // 2) Stash everything else
            SetProgress("Stashing other work…");
            AppendLog("[step] git stash push -u -k");
            var stash = await _git.StashKeepIndexAsync(repoDir, "cbeta-autostash", prog, ct);
            if (!stash.Success)
            {
                SetProgress("Stash failed.");
                AppendLog("[error] " + stash.Error);
                return;
            }

            // 3) Create branch
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

            // 4) Commit staged only
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

            SetProgress("Local commit created.");
            AppendLog("[ok] created single-file commit on branch: " + branchName);
            AppendLog("[next] OAuth + fork + push + PR comes next.");

            // 5) Switch back + restore stash
            SetProgress("Restoring your other work…");
            await SafeRestoreAsync(repoDir, originalBranch, prog, ct);

            Status?.Invoke(this, "Local commit ready on branch: " + branchName);
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

    // -------------------------
    // Common controls/helpers
    // -------------------------

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
}
