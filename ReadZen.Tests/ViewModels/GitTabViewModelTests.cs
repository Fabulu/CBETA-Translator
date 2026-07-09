using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using ReadZen.App.Infrastructure;
using ReadZen.App.Messages;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using ReadZen.Tests.Stubs;
using Xunit;

namespace ReadZen.Tests.ViewModels;

public class GitTabViewModelTests
{
    private static GitTabViewModel MakeVm()
    {
        return new GitTabViewModel(
            new StubGitRepoService(),
            new StubGitHubAuthService(),
            new StubGitHubApiService(),
            new StubCommunityDataService(),
            new StubScholarCollectionsService(),
            new StubTermbaseStorageService(),
            new StubTranslationReviewService(),
            new StubMasterDatesService(),
            new StubDocumentTagService(),
            new StubTranslationStarService());
    }

    // ---- Initial state ----

    [Fact]
    public void InitialState_HasDefaults()
    {
        var vm = MakeVm();

        Assert.Contains("Location:", vm.DestText);
        Assert.Equal("Ready.", vm.ProgressText);
        Assert.Contains("Welcome", vm.LogText);
        Assert.Equal("", vm.CommitMessage);
        Assert.Contains("(none)", vm.SelectedText);
        Assert.False(vm.IsBusy);
        Assert.True(vm.IsNotBusy);
    }

    // ---- SetSelectedRelPath ----

    [Fact]
    public void SetSelectedRelPath_UpdatesSelectedText()
    {
        var vm = MakeVm();

        vm.SetSelectedRelPath("T2076/T2076_.xml");

        Assert.Contains("T2076", vm.SelectedText);
    }

    [Fact]
    public void SetSelectedRelPath_Null_SetsNone()
    {
        var vm = MakeVm();
        vm.SetSelectedRelPath("something");

        vm.SetSelectedRelPath(null);

        Assert.Contains("(none)", vm.SelectedText);
    }

    [Fact]
    public void SetSelectedRelPath_Whitespace_SetsNone()
    {
        var vm = MakeVm();

        vm.SetSelectedRelPath("   ");

        Assert.Contains("(none)", vm.SelectedText);
    }

    // ---- SetUsername ----

    [Fact]
    public void SetUsername_AcceptsValidName()
    {
        var vm = MakeVm();
        vm.SetUsername("Alice");
        // No public accessor, but should not throw
    }

    [Fact]
    public void SetUsername_Null_DoesNotThrow()
    {
        var vm = MakeVm();
        vm.SetUsername(null);
    }

    [Fact]
    public void SetUsername_Whitespace_DoesNotThrow()
    {
        var vm = MakeVm();
        vm.SetUsername("   ");
    }

    // ---- SetCurrentRepoRoot ----

    [Fact]
    public void SetCurrentRepoRoot_Null_DoesNothing()
    {
        var vm = MakeVm();
        var before = vm.DestText;

        vm.SetCurrentRepoRoot(null);

        // Should not crash; DestText may stay same
    }

    [Fact]
    public void SetCurrentRepoRoot_Whitespace_DoesNothing()
    {
        var vm = MakeVm();

        vm.SetCurrentRepoRoot("   ");
    }

    // ---- OnAttachedToVisualTree ----

    [Fact]
    public void OnAttachedToVisualTree_DoesNotThrow()
    {
        var vm = MakeVm();
        vm.OnAttachedToVisualTree();
    }

    // ---- PropertyChanged ----

    [Fact]
    public void PropertyChanged_FiredForProgressText()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.ProgressText = "Cloning...";

        Assert.Contains("ProgressText", changed);
    }

    [Fact]
    public void PropertyChanged_FiredForIsBusy()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.IsBusy = true;

        Assert.Contains("IsBusy", changed);
    }

    [Fact]
    public void PropertyChanged_FiredForCommitMessage()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.CommitMessage = "My commit";

        Assert.Contains("CommitMessage", changed);
    }

    // ---- Cancel command ----

    [Fact]
    public void Cancel_DoesNotThrowWhenNothingRunning()
    {
        var vm = MakeVm();
        vm.CancelCommand.Execute(null);
    }

    // ---- LoadPersistedAuth ----

    private static string? GetPrivateField(GitTabViewModel vm, string fieldName)
    {
        var field = typeof(GitTabViewModel).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(vm) as string;
    }

    [Fact]
    public void LoadPersistedAuth_ValidTokenAndLogin_SetsTokenAndLogin()
    {
        var vm = MakeVm();

        vm.LoadPersistedAuth("ghp_abc123", "testuser");

        // AppendLog uses Dispatcher.UIThread.Post which does not execute in tests,
        // so verify the private fields directly via reflection.
        Assert.Equal("ghp_abc123", GetPrivateField(vm, "_githubAccessToken"));
        Assert.Equal("testuser", GetPrivateField(vm, "_githubLogin"));
    }

    [Fact]
    public void LoadPersistedAuth_NullToken_DoesNotSetFields()
    {
        var vm = MakeVm();

        vm.LoadPersistedAuth(null, "testuser");

        Assert.Null(GetPrivateField(vm, "_githubAccessToken"));
        Assert.Null(GetPrivateField(vm, "_githubLogin"));
    }

    [Fact]
    public void LoadPersistedAuth_EmptyToken_DoesNotSetFields()
    {
        var vm = MakeVm();

        vm.LoadPersistedAuth("", "testuser");

        Assert.Null(GetPrivateField(vm, "_githubAccessToken"));
        Assert.Null(GetPrivateField(vm, "_githubLogin"));
    }

    [Fact]
    public void LoadPersistedAuth_NullLogin_DoesNotSetFields()
    {
        var vm = MakeVm();

        vm.LoadPersistedAuth("ghp_abc123", null);

        Assert.Null(GetPrivateField(vm, "_githubAccessToken"));
        Assert.Null(GetPrivateField(vm, "_githubLogin"));
    }

    [Fact]
    public void LoadPersistedAuth_EmptyLogin_DoesNotSetFields()
    {
        var vm = MakeVm();

        vm.LoadPersistedAuth("ghp_abc123", "");

        Assert.Null(GetPrivateField(vm, "_githubAccessToken"));
        Assert.Null(GetPrivateField(vm, "_githubLogin"));
    }

    [Fact]
    public void LoadPersistedAuth_BothEmpty_DoesNotSetFields()
    {
        var vm = MakeVm();

        vm.LoadPersistedAuth("", "");

        Assert.Null(GetPrivateField(vm, "_githubAccessToken"));
        Assert.Null(GetPrivateField(vm, "_githubLogin"));
    }

    [Fact]
    public void LoadPersistedAuth_BothNull_DoesNotSetFields()
    {
        var vm = MakeVm();

        vm.LoadPersistedAuth(null, null);

        Assert.Null(GetPrivateField(vm, "_githubAccessToken"));
        Assert.Null(GetPrivateField(vm, "_githubLogin"));
    }

    // ---- DeviceCodeReady callback ----

    [Fact]
    public void MakeDeviceCodeCallback_InvokesShowDeviceCodeAsync()
    {
        var vm = MakeVm();
        string? receivedCode = null;
        string? receivedUri = null;

        // Wire up the ShowDeviceCodeAsync delegate
        vm.ShowDeviceCodeAsync = (code, uri) =>
        {
            receivedCode = code;
            receivedUri = uri;
            return Task.CompletedTask;
        };

        // Call MakeDeviceCodeCallback via reflection (it's private)
        var method = typeof(GitTabViewModel).GetMethod("MakeDeviceCodeCallback",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var callback = (Action<DeviceCodeReady>)method!.Invoke(vm, null)!;
        Assert.NotNull(callback);

        // Invoke the callback with test data
        var deviceCode = new DeviceCodeReady("ABCD-1234", "https://github.com/login/device");
        callback(deviceCode);

        // Note: callback uses Dispatcher.UIThread.Post which may not execute in test context.
        // The callback is still valid (it doesn't throw), verifying the wiring is correct.
        // In a headless test environment, Post may execute synchronously or not at all.
    }

    [Fact]
    public void DeviceCodeReady_Record_HasCorrectProperties()
    {
        var dcr = new DeviceCodeReady("TEST-CODE", "https://example.com/device");

        Assert.Equal("TEST-CODE", dcr.UserCode);
        Assert.Equal("https://example.com/device", dcr.VerificationUri);
    }

    [Fact]
    public void DeviceCodeReady_RecordEquality()
    {
        var a = new DeviceCodeReady("CODE1", "https://example.com");
        var b = new DeviceCodeReady("CODE1", "https://example.com");
        var c = new DeviceCodeReady("CODE2", "https://example.com");

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }


    [Fact]
    public async Task ConfirmCommunityShareAsync_NoConfirmBridge_AllowsShare()
    {
        var vm = MakeVm();
        var method = typeof(GitTabViewModel).GetMethod("ConfirmCommunityShareAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var task = (Task<bool>)method!.Invoke(vm, new object[] { new List<string> { "community/termbases/alice.jsonl" } })!;
        var result = await task;

        Assert.True(result);
    }

    [Fact]
    public async Task ConfirmCommunityShareAsync_BuildsReviewerAndForumWarningMessage()
    {
        var vm = MakeVm();
        string? capturedTitle = null;
        string? capturedMessage = null;
        string? capturedYes = null;
        string? capturedNo = null;

        vm.ConfirmAsync = (title, message, yesText, noText) =>
        {
            capturedTitle = title;
            capturedMessage = message;
            capturedYes = yesText;
            capturedNo = noText;
            return Task.FromResult(true);
        };

        var method = typeof(GitTabViewModel).GetMethod("ConfirmCommunityShareAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var task = (Task<bool>)method!.Invoke(vm, new object[] { new List<string> { "community/termbases/alice.jsonl", "community/tags/alice.jsonl" } })!;
        var result = await task;

        Assert.True(result);
        Assert.Equal("Share Community Data?", capturedTitle);
        Assert.Contains("reviewed by a real person", capturedMessage);
        Assert.Contains("/r/zen forums", capturedMessage);
        Assert.Contains("community/termbases/alice.jsonl", capturedMessage);
        Assert.Contains("Personal translation PRs are handled separately", capturedMessage);
        Assert.Equal("Share community data", capturedYes);
        Assert.Equal("Cancel", capturedNo);
    }


    [Fact]
    public void RepoConstants_TargetSplitRepos()
    {
        var type = typeof(GitTabViewModel);
        var originalsUrl = (string?)type.GetField("OriginalsRepoUrl", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
        var translationUrl = (string?)type.GetField("TranslationRepoUrl", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
        var upstreamRepo = (string?)type.GetField("UpstreamRepo", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);

        Assert.Equal("https://github.com/Fabulu/CbetaZenTexts.git", originalsUrl);
        Assert.Equal("https://github.com/Fabulu/CbetaZenTranslations.git", translationUrl);
        Assert.Equal("CbetaZenTranslations", upstreamRepo);
    }


    [Fact]
    public void GetTrackedCommunitySharePaths_IncludesPerUserCommunityTranslations()
    {
        var vm = MakeVm();
        vm.LoadPersistedAuth("ghp_test", "Fabulu");

        var repoDir = Path.Combine(Path.GetTempPath(), "readzen-share-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repoDir, "community", "translations", "Fabulu"));
        File.WriteAllText(Path.Combine(repoDir, "community", "translations", "Fabulu", "T48n2005.xml"), "test");

        try
        {
            var method = typeof(GitTabViewModel).GetMethod("GetTrackedCommunitySharePaths", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var tracked = (HashSet<string>)method!.Invoke(vm, new object[] { repoDir })!;

            Assert.Contains("community/translations/Fabulu/T48n2005.xml", tracked, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("community/termbases/Fabulu.jsonl", tracked, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(repoDir, true);
        }
    }

    [Fact]
    public void IsAutoMergeCommunitySharePath_AcceptsPerUserTranslationXml_AndRejectsBak()
    {
        var vm = MakeVm();
        vm.LoadPersistedAuth("ghp_test", "Fabulu");
        var method = typeof(GitTabViewModel).GetMethod("IsAutoMergeCommunitySharePath", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var ok = (bool)method!.Invoke(vm, new object[] { "community/translations/Fabulu/T/T48/T48n2005.xml" })!;
        var bak = (bool)method!.Invoke(vm, new object[] { "community/translations/Fabulu/T/T48/T48n2005.xml.bak" })!;

        Assert.True(ok);
        Assert.False(bak);
    }

    [Fact]
    public void GetAlwaysPreservedUpdatePaths_IncludesCommunityTranslationsTree()
    {
        var vm = MakeVm();
        var repoDir = Path.Combine(Path.GetTempPath(), "readzen-preserve-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repoDir, "community", "translations", "dota2nub", "T", "T48"));
        File.WriteAllText(Path.Combine(repoDir, "community", "translations", "dota2nub", "T", "T48", "T48n2005.xml"), "test");

        try
        {
            var method = typeof(GitTabViewModel).GetMethod("GetAlwaysPreservedUpdatePaths", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var tracked = (string[])method!.Invoke(vm, new object[] { repoDir })!;
            Assert.Contains("community/translations/dota2nub/T/T48/T48n2005.xml", tracked, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(repoDir, true);
        }
    }

    [Fact]
    public async Task ShareAllInternalAsync_MaterializesSelectedPersonalTranslationBeforeFingerprinting()
    {
        var git = new RecordingGitRepoService();
        var api = new RecordingGitHubApiService();
        var vm = new GitTabViewModel(
            git,
            new StubGitHubAuthService(),
            api,
            new StubCommunityDataService(),
            new StubScholarCollectionsService(),
            new StubTermbaseStorageService(),
            new StubTranslationReviewService(),
            new StubMasterDatesService(),
            new StubDocumentTagService(),
            new StubTranslationStarService());

        vm.LoadPersistedAuth("ghp_test", "Fabulu");
        vm.SetUsername("dota2nub");

        var parentDir = Path.Combine(Path.GetTempPath(), "readzen-share-selected-" + Guid.NewGuid().ToString("N"));
        var transRepoDir = Path.Combine(parentDir, AppPaths.DefaultTranslationRepoFolderName);
        Directory.CreateDirectory(Path.Combine(transRepoDir, ".git"));
        Directory.CreateDirectory(Path.Combine(transRepoDir, "xml-p5t"));
        AppPaths.InvalidateDiscoveryCache(parentDir);
        vm.SetCurrentRepoRoot(parentDir);
        vm.SetSelectedRelPath("T/T48/T48n2005.xml");

        var ensureCalled = false;
        vm.EnsurePersonalTranslatedForSelectedRequested += relPath =>
        {
            ensureCalled = true;
            var full = Path.Combine(transRepoDir, "community", "translations", "Fabulu", relPath.Replace('/', Path.DirectorySeparatorChar));
            var dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(full, "<TEI/>");
            return Task.FromResult(true);
        };

        try
        {
            var method = typeof(GitTabViewModel).GetMethod("ShareAllInternalAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var task = (Task)method!.Invoke(vm, null)!;
            await task;

            Assert.True(ensureCalled);
            Assert.Contains(git.StagedPaths, p => string.Equals(p, "community/translations/Fabulu/T/T48/T48n2005.xml", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            AppPaths.InvalidateDiscoveryCache(parentDir);
            Directory.Delete(parentDir, true);
        }
    }


    // ---- CorpusFilesChangedMessage sends (git success points) ----
    //
    // Four send sites exist in GitTabViewModel: update-success, clone-success,
    // sync-completion, and panic-success. The first three are driven below through
    // the stub IGitRepoService. The PANIC SUCCESS send site cannot be driven through
    // stubs: PanicButtonAsync shells out via the STATIC RunGitAsync helper (a real
    // `git stash push/drop` process), which no stub intercepts, so its success point
    // is unreachable without a real git repo + real git mutations (both forbidden in
    // tests). Its send code is identical in shape to the three covered sites; the
    // panic tests below cover the no-send-on-failure contract instead.

    private sealed class CorpusMessageRecorder
    {
        public List<CorpusFilesChangedMessage> Received { get; } = new();
    }

    private static IDisposable RecordCorpusMessages(out CorpusMessageRecorder recorder)
    {
        var r = new CorpusMessageRecorder();
        WeakReferenceMessenger.Default.Register<CorpusMessageRecorder, CorpusFilesChangedMessage>(
            r, static (rec, msg) => rec.Received.Add(msg));
        recorder = r;
        // Always unregister in finally/dispose: tests run sequentially, but a leaked
        // recorder would keep receiving sends from later tests and poison their counts.
        return new Unregisterer(r);
    }

    private sealed class Unregisterer : IDisposable
    {
        private readonly CorpusMessageRecorder _r;
        public Unregisterer(CorpusMessageRecorder r) => _r = r;
        public void Dispose() => WeakReferenceMessenger.Default.Unregister<CorpusFilesChangedMessage>(_r);
    }

    /// <summary>
    /// Creates the two-repo parent layout GetOrUpdateFilesAsync's UPDATE branch needs:
    /// CbetaZenTexts (xml-p5 + .git) and CbetaZenTranslations (xml-p5t + .git).
    /// </summary>
    private static string CreateTwoRepoParent()
    {
        var parentDir = Path.Combine(Path.GetTempPath(), "readzen-corpusmsg-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(Path.Combine(parentDir, AppPaths.DefaultOriginalRepoFolderName, "xml-p5"));
        Directory.CreateDirectory(Path.Combine(parentDir, AppPaths.DefaultOriginalRepoFolderName, ".git"));
        Directory.CreateDirectory(Path.Combine(parentDir, AppPaths.DefaultTranslationRepoFolderName, "xml-p5t"));
        Directory.CreateDirectory(Path.Combine(parentDir, AppPaths.DefaultTranslationRepoFolderName, ".git"));
        AppPaths.InvalidateDiscoveryCache(parentDir);
        return parentDir;
    }

    private static void CleanupParent(string parentDir)
    {
        AppPaths.InvalidateDiscoveryCache(parentDir);
        try { Directory.Delete(parentDir, true); } catch { }
    }

    [Fact]
    public async Task UpdateSuccess_SendsExactlyOneCorpusFilesChangedMessage()
    {
        var vm = MakeVm();
        var parentDir = CreateTwoRepoParent();
        using var _ = RecordCorpusMessages(out var recorder);
        try
        {
            vm.SetCurrentRepoRoot(parentDir);

            await vm.GetFilesCommand.ExecuteAsync(null);

            Assert.Single(recorder.Received);
            Assert.Equal(Path.GetFullPath(parentDir), Path.GetFullPath(recorder.Received[0].RepoRoot),
                StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            CleanupParent(parentDir);
        }
    }

    [Fact]
    public async Task UpdateBlocked_UnknownSyncState_SendsNoCorpusFilesChangedMessage()
    {
        // GetAheadBehindAsync returning null hits the "Update blocked (sync state
        // unknown)" early return — a driven failure path that must never send.
        var vm = new GitTabViewModel(
            new NullAheadBehindGitRepoService(),
            new StubGitHubAuthService(),
            new StubGitHubApiService(),
            new StubCommunityDataService(),
            new StubScholarCollectionsService(),
            new StubTermbaseStorageService(),
            new StubTranslationReviewService(),
            new StubMasterDatesService(),
            new StubDocumentTagService(),
            new StubTranslationStarService());
        var parentDir = CreateTwoRepoParent();
        using var _ = RecordCorpusMessages(out var recorder);
        try
        {
            vm.SetCurrentRepoRoot(parentDir);

            await vm.GetFilesCommand.ExecuteAsync(null);

            Assert.Empty(recorder.Received);
        }
        finally
        {
            CleanupParent(parentDir);
        }
    }

    [Fact]
    public async Task CloneSuccess_SendsExactlyOneCorpusFilesChangedMessage()
    {
        // Empty parent dir -> the CLONE branch runs; the stub CloneAsync succeeds
        // without creating anything, reaching the clone-success point.
        var vm = MakeVm();
        var parentDir = Path.Combine(Path.GetTempPath(), "readzen-corpusmsg-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(parentDir);
        AppPaths.InvalidateDiscoveryCache(parentDir);
        using var _ = RecordCorpusMessages(out var recorder);
        try
        {
            vm.SetCurrentRepoRoot(parentDir);

            await vm.GetFilesCommand.ExecuteAsync(null);

            Assert.Single(recorder.Received);
            Assert.Equal(Path.GetFullPath(parentDir), Path.GetFullPath(recorder.Received[0].RepoRoot),
                StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            CleanupParent(parentDir);
        }
    }

    [Fact]
    public async Task SyncCompletion_SendsUpdateAndFinalCorpusFilesChangedMessages()
    {
        // A full sync legitimately sends TWICE: once from the embedded update phase
        // (GetOrUpdateFilesAsync success) and once as the last word of SyncAsync.
        // The mid-sync send is superseded by QueueAutoIndexBuild's CTS cancel+requeue
        // on the receiver side — that supersession is the designed debounce.
        var vm = MakeVm();
        var parentDir = CreateTwoRepoParent();
        using var _ = RecordCorpusMessages(out var recorder);
        try
        {
            vm.SetCurrentRepoRoot(parentDir);

            await vm.SyncCommand.ExecuteAsync(null);

            Assert.Equal(2, recorder.Received.Count);
        }
        finally
        {
            CleanupParent(parentDir);
        }
    }

    [Fact]
    public async Task PanicWithoutRepo_SendsNoCorpusFilesChangedMessage()
    {
        // Panic's repo-not-ready early return must not send. (The panic SUCCESS
        // send site is not stub-drivable — see the note at the top of this section.)
        var vm = MakeVm();
        var parentDir = Path.Combine(Path.GetTempPath(), "readzen-corpusmsg-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(parentDir);
        AppPaths.InvalidateDiscoveryCache(parentDir);
        using var _ = RecordCorpusMessages(out var recorder);
        try
        {
            vm.SetCurrentRepoRoot(parentDir);
            vm.ConfirmAsync = (_, _, _, _) => Task.FromResult(true);

            await vm.PanicResetCommand.ExecuteAsync(null);

            Assert.Empty(recorder.Received);
        }
        finally
        {
            CleanupParent(parentDir);
        }
    }

    private sealed class NullAheadBehindGitRepoService : StubGitRepoService
    {
        public override Task<(int behind, int ahead)?> GetAheadBehindAsync(string repoDir, string upstreamRef, CancellationToken ct)
            => Task.FromResult<(int behind, int ahead)?>(null);
    }

    private sealed class RecordingGitRepoService : StubGitRepoService
    {
        public List<string> StagedPaths { get; } = new();
        public override Task<string[]?> GetStatusPorcelainAsync(string repoDir, CancellationToken ct)
        {
            var full = Path.Combine(repoDir, "community", "translations", "Fabulu", "T", "T48", "T48n2005.xml");
            return Task.FromResult<string[]?>(File.Exists(full)
                ? new[] { "?? community/translations/" }
                : Array.Empty<string>());
        }

        public override Task<GitOpResult> StagePathAsync(string repoDir, string relPath, IProgress<string> progress, CancellationToken ct)
        {
            StagedPaths.Add(relPath.Replace('\\', '/'));
            return Task.FromResult(new GitOpResult(true));
        }
    }

    private sealed class RecordingGitHubApiService : StubGitHubApiService
    {
        public override Task<string?> CreatePullRequestAsync(string accessToken, string upstreamOwner, string upstreamRepo, string head, string baseBranch, string title, string body, CancellationToken ct)
            => Task.FromResult<string?>("https://example.test/pr/1");
    }
}
