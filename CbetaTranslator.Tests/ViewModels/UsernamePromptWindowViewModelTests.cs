using CbetaTranslator.App.ViewModels;
using Xunit;

namespace CbetaTranslator.Tests.ViewModels;

public class UsernamePromptWindowViewModelTests
{
    [Fact]
    public void Commit_ValidUsername_FiresCommitRequested()
    {
        var vm = new UsernamePromptWindowViewModel();
        vm.Username = "  Alice  ";

        string? committed = null;
        vm.CommitRequested = name => committed = name;

        vm.CommitCommand.Execute(null);

        Assert.Equal("Alice", committed);
        Assert.False(vm.ShowError);
    }

    [Fact]
    public void Commit_EmptyUsername_ShowsError_DoesNotFireCommitRequested()
    {
        var vm = new UsernamePromptWindowViewModel();
        vm.Username = "   ";

        bool commitCalled = false;
        vm.CommitRequested = _ => commitCalled = true;

        vm.CommitCommand.Execute(null);

        Assert.True(vm.ShowError);
        Assert.False(commitCalled);
    }

    [Fact]
    public void Commit_EmptyString_ShowsError()
    {
        var vm = new UsernamePromptWindowViewModel();
        vm.Username = "";

        vm.CommitCommand.Execute(null);

        Assert.True(vm.ShowError);
    }

    [Fact]
    public void Commit_DefaultUsername_ShowsError()
    {
        var vm = new UsernamePromptWindowViewModel();
        // Username defaults to ""

        vm.CommitCommand.Execute(null);

        Assert.True(vm.ShowError);
    }

    [Fact]
    public void PropertyChanged_FiredForUsername()
    {
        var vm = new UsernamePromptWindowViewModel();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.Username = "Bob";

        Assert.Contains("Username", changed);
    }

    [Fact]
    public void PropertyChanged_FiredForShowError()
    {
        var vm = new UsernamePromptWindowViewModel();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.ShowError = true;

        Assert.Contains("ShowError", changed);
    }

    [Fact]
    public void CommitRequested_NotWired_DoesNotThrow()
    {
        var vm = new UsernamePromptWindowViewModel();
        vm.Username = "Valid";

        // Should not throw even with no handler
        vm.CommitCommand.Execute(null);
    }
}
