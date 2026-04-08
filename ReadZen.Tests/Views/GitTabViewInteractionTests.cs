using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using ReadZen.App.ViewModels;
using ReadZen.App.Views;
using ReadZen.Tests.Stubs;
using Xunit;

namespace ReadZen.Tests.Views;

public class GitTabViewInteractionTests
{
    private static GitTabView CreateViewShell(out GitTabViewModel vm)
    {
        var view = (GitTabView)RuntimeHelpers.GetUninitializedObject(typeof(GitTabView));
        vm = new GitTabViewModel(
            new StubGitRepoService(),
            new StubGitHubAuthService(),
            new StubGitHubApiService(),
            new StubCommunityDataService(),
            new StubScholarCollectionsService(),
            new StubTermbaseStorageService(),
            new StubTranslationReviewService(),
            new StubMasterDatesService(),
            new StubDocumentTagService());
        SetField(typeof(GitTabView), view, "_vm", vm);
        return view;
    }

    private static void SetField(Type type, object target, string name, object? value)
    {
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing field {name} on {type.Name}");
        field.SetValue(target, value);
    }

    private static T GetField<T>(Type type, object target, string name)
    {
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing field {name} on {type.Name}");
        return (T)field.GetValue(target)!;
    }

    [Fact]
    public void ForwardingMethods_UpdateUnderlyingViewModelState()
    {
        var view = CreateViewShell(out var vm);

        var repoRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "git-tab-view-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(repoRoot);
        view.SetCurrentRepoRoot(repoRoot);
        view.SetSelectedRelPath("T\\T48\\T48n2005.xml");
        view.SetUsername("alice");
        view.LoadPersistedAuth("ghp_test", "octocat");

        Assert.Null(GetField<string?>(typeof(GitTabViewModel), vm, "_currentRepoRoot"));
        Assert.Equal(repoRoot, GetField<string?>(typeof(GitTabViewModel), vm, "_baseDestFolder"));
        Assert.Equal("T/T48/T48n2005.xml", GetField<string?>(typeof(GitTabViewModel), vm, "_selectedRelPath"));
        Assert.Equal("alice", GetField<string?>(typeof(GitTabViewModel), vm, "_username"));
        Assert.Equal("ghp_test", GetField<string?>(typeof(GitTabViewModel), vm, "_githubAccessToken"));
        Assert.Equal("octocat", GetField<string?>(typeof(GitTabViewModel), vm, "_githubLogin"));
    }
}
