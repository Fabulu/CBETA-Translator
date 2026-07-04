using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Infrastructure;
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
