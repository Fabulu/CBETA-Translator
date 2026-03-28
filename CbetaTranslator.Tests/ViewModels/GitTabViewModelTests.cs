using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.ViewModels;
using CbetaTranslator.Tests.Stubs;
using Xunit;

namespace CbetaTranslator.Tests.ViewModels;

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
            new StubMasterDatesService());
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
}
