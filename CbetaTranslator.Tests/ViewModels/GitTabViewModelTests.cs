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
            new StubCommunityDataService());
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
}
