using System.IO;
using System.Reflection;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.ViewModels;
using CbetaTranslator.Tests.Stubs;
using Xunit;

namespace CbetaTranslator.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private static MainWindowViewModel MakeVm(StubDocumentTagService? documentTagService = null)
    {
        return new MainWindowViewModel(
            new StubFileService(),
            new StubAppConfigService(),
            new StubIndexCacheService(),
            new StubRenderedDocumentCacheService(),
            new StubZenTextsService(),
            new StubIndexedTranslationService(),
            new StubTranslationAssistantService(),
            new StubTranslationAssistantBuildService(),
            new StubTranslationReviewService(),
            new StubSearchIndexService(),
            documentTagService ?? new StubDocumentTagService());
    }

    [Fact]
    public void InitialState_HasDefaults()
    {
        var vm = MakeVm();

        Assert.Equal("Ready.", vm.StatusText);
        Assert.Equal("", vm.RootDisplayText);
        Assert.Equal("", vm.CurrentFileText);
        Assert.Contains("Read Zen", vm.WindowTitle);
        Assert.False(vm.IsDirty);
        Assert.Null(vm.Root);
        Assert.Null(vm.CurrentRelPath);
    }

    [Fact]
    public void SetStatus_UpdatesStatusText()
    {
        var vm = MakeVm();

        vm.SetStatus("Loading...");

        Assert.Equal("Loading...", vm.StatusText);
    }

    [Fact]
    public void UpdateWindowTitle_NoFile_ShowsBaseTitle()
    {
        var vm = MakeVm();

        vm.UpdateWindowTitle();

        Assert.Equal("Read Zen", vm.WindowTitle);
        Assert.Equal("", vm.CurrentFileText);
    }

    [Fact]
    public void UpdateWindowTitle_BridgeInvoked()
    {
        var vm = MakeVm();
        string? received = null;
        vm.SetWindowTitle = t => received = t;

        vm.UpdateWindowTitle();

        Assert.NotNull(received);
        Assert.Contains("Read Zen", received!);
    }

    [Fact]
    public void UpdateWindowTitle_WithFile_UsesCleanSeparator()
    {
        var vm = MakeVm();
        typeof(MainWindowViewModel)
            .GetField("_currentRelPath", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(vm, "T01/test.xml");

        vm.UpdateWindowTitle();

        Assert.Equal("Read Zen - T01/test.xml", vm.WindowTitle);
        Assert.Equal("T01/test.xml", vm.CurrentFileText);
    }

    [Fact]
    public void NormalizeRel_ConvertsBackslashesAndTrimsLeadingSlash()
    {
        Assert.Equal("a/b/c.xml", MainWindowViewModel.NormalizeRel("\\a\\b\\c.xml"));
        Assert.Equal("a/b.xml", MainWindowViewModel.NormalizeRel("/a/b.xml"));
        Assert.Equal("a/b.xml", MainWindowViewModel.NormalizeRel("a/b.xml"));
    }

    [Fact]
    public void NormalizeRel_Null_ReturnsEmpty()
    {
        Assert.Equal("", MainWindowViewModel.NormalizeRel(null!));
    }

    [Fact]
    public void PropertyChanged_FiredForStatusText()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.StatusText = "Hello";

        Assert.Contains("StatusText", changed);
    }

    [Fact]
    public void PropertyChanged_FiredForWindowTitle()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.WindowTitle = "New Title";

        Assert.Contains("WindowTitle", changed);
    }

    [Fact]
    public void PropertyChanged_FiredForRootDisplayText()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.RootDisplayText = "/new/root";

        Assert.Contains("RootDisplayText", changed);
    }

    [Fact]
    public void SetStatus_DefaultSeverity_SetsStatusSeverityToInfo()
    {
        var vm = MakeVm();

        vm.SetStatus("Something happened");

        Assert.Equal(StatusSeverity.Info, vm.StatusSeverity);
        Assert.Equal("Something happened", vm.StatusText);
    }

    [Fact]
    public void SetStatus_ErrorSeverity_SetsStatusSeverityToError()
    {
        var vm = MakeVm();

        vm.SetStatus("Failed!", StatusSeverity.Error);

        Assert.Equal(StatusSeverity.Error, vm.StatusSeverity);
        Assert.Equal("Failed!", vm.StatusText);
    }

    [Fact]
    public void SetStatus_SuccessSeverity_SetsStatusSeverityToSuccess()
    {
        var vm = MakeVm();

        vm.SetStatus("Done!", StatusSeverity.Success);

        Assert.Equal(StatusSeverity.Success, vm.StatusSeverity);
        Assert.Equal("Done!", vm.StatusText);
    }

    [Fact]
    public void SetStatus_WarningSeverity_SetsStatusSeverityToWarning()
    {
        var vm = MakeVm();

        vm.SetStatus("Caution", StatusSeverity.Warning);

        Assert.Equal(StatusSeverity.Warning, vm.StatusSeverity);
    }

    [Fact]
    public void SetStatus_FiresPropertyChangedForBothStatusTextAndStatusSeverity()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.SetStatus("test", StatusSeverity.Error);

        Assert.Contains("StatusText", changed);
        Assert.Contains("StatusSeverity", changed);
    }

    [Theory]
    [InlineData(StatusSeverity.Info)]
    [InlineData(StatusSeverity.Success)]
    [InlineData(StatusSeverity.Warning)]
    [InlineData(StatusSeverity.Error)]
    public void SetStatus_AllSeverities_SetCorrectly(StatusSeverity severity)
    {
        var vm = MakeVm();

        vm.SetStatus("msg", severity);

        Assert.Equal(severity, vm.StatusSeverity);
    }


    [Fact]
    public void ViewConfigUsernameForAssistant_PrefersGitHubUsername()
    {
        var vm = MakeVm();

        vm.UpdateConfig(new AppConfig { Username = "Alice", GitHubUsername = "octocat" });

        Assert.Equal("octocat", vm.ViewConfigUsernameForAssistant());
    }

    [Fact]
    public void Config_HasDefaultDarkTheme()
    {
        var vm = MakeVm();
        Assert.True(vm.Config.IsDarkTheme);
    }

    [Fact]
    public void FilteredItems_InitiallyEmpty()
    {
        var vm = MakeVm();
        Assert.Empty(vm.FilteredItems);
    }

    [Fact]
    public void AllItemsByRel_InitiallyEmpty()
    {
        var vm = MakeVm();
        Assert.Empty(vm.AllItemsByRel);
    }

    [Fact]
    public async Task HandleGitHubAuthCompletedAsync_MigratesLegacyUserTranslationDirToGitHubFolder()
    {
        var vm = MakeVm();
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5"));

        var legacyDir = Path.Combine(root, "community", "translations", "Alice");
        Directory.CreateDirectory(Path.Combine(legacyDir, "T01"));
        var legacyFile = Path.Combine(legacyDir, "T01", "test.xml");
        await File.WriteAllTextAsync(legacyFile, "<xml>legacy</xml>");

        try
        {
            vm.UpdateConfig(new AppConfig { Username = "Alice" });
            await vm.LoadRootAsync(root, saveToConfig: false);

            await vm.HandleGitHubAuthCompletedAsync("ghp_test", "octocat");

            var githubFile = Path.Combine(root, "community", "translations", "octocat", "T01", "test.xml");
            Assert.True(File.Exists(githubFile));
            Assert.False(Directory.Exists(legacyDir));
            Assert.Equal("octocat", vm.GetActiveTranslationUser());
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RefreshTranslationSources_UsesGitHubFolderAsCurrentUserIdentity()
    {
        var vm = MakeVm();
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5"));
        Directory.CreateDirectory(Path.Combine(root, "community", "translations", "octocat"));
        Directory.CreateDirectory(Path.Combine(root, "community", "translations", "alice"));
        Directory.CreateDirectory(Path.Combine(root, "community", "translations", "otheruser"));

        try
        {
            vm.UpdateConfig(new AppConfig { Username = "Alice", GitHubUsername = "octocat" });
            await vm.LoadRootAsync(root, saveToConfig: false);

            var labels = vm.GetTranslationSourceLabels();

            Assert.DoesNotContain("octocat", labels);
            Assert.Contains("alice", labels);
            Assert.Contains("otheruser", labels);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RefreshTranslationSources_PushesReaderSourceOptionsAndIndex()
    {
        var vm = MakeVm();
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5"));
        Directory.CreateDirectory(Path.Combine(root, "community", "translations", "octocat"));
        Directory.CreateDirectory(Path.Combine(root, "community", "translations", "otheruser"));

        List<string>? readerOptions = null;
        int? readerIndex = null;

        try
        {
            vm.SetReadableTranslationSourceOptions = options => readerOptions = new List<string>(options);
            vm.SetReadableTranslationSourceIndex = index => readerIndex = index;
            vm.UpdateConfig(new AppConfig { Username = "Alice", GitHubUsername = "octocat" });
            await vm.LoadRootAsync(root, saveToConfig: false);

            Assert.NotNull(readerOptions);
            Assert.Equal(vm.GetTranslationSourceLabels(), readerOptions);
            Assert.Equal(0, readerIndex);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task HandleGitHubAuthCompletedAsync_ConflictingLegacyFolderIsDeletedAndGitHubFolderWins()
    {
        var vm = MakeVm();
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5"));

        var legacyDir = Path.Combine(root, "community", "translations", "Alice");
        var githubDir = Path.Combine(root, "community", "translations", "octocat");
        Directory.CreateDirectory(Path.Combine(legacyDir, "T01"));
        Directory.CreateDirectory(Path.Combine(githubDir, "T01"));
        await File.WriteAllTextAsync(Path.Combine(legacyDir, "T01", "test.xml"), "<xml>legacy</xml>");
        await File.WriteAllTextAsync(Path.Combine(githubDir, "T01", "test.xml"), "<xml>github</xml>");

        try
        {
            vm.UpdateConfig(new AppConfig { Username = "Alice" });
            await vm.LoadRootAsync(root, saveToConfig: false);

            await vm.HandleGitHubAuthCompletedAsync("ghp_test", "octocat");

            Assert.False(Directory.Exists(legacyDir));
            var githubFile = Path.Combine(githubDir, "T01", "test.xml");
            Assert.Equal("<xml>github</xml>", await File.ReadAllTextAsync(githubFile));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RefreshCommunityTagDataAsync_FiltersOutCurrentUserIdentities()
    {
        var tagService = new StubDocumentTagService
        {
            CommunityTags = new Dictionary<string, List<DocumentTag>>
            {
                ["Alice"] = new(),
                ["octocat"] = new(),
                ["otheruser"] = new() { new DocumentTag { RelPath = "T01/test.xml", TagId = "tag" } }
            },
            CommunityVocabularies = new Dictionary<string, TagVocabulary>
            {
                ["Alice"] = new(),
                ["octocat"] = new(),
                ["otheruser"] = new()
            }
        };
        var vm = MakeVm(tagService);
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5"));

        Dictionary<string, List<DocumentTag>>? capturedTags = null;
        Dictionary<string, TagVocabulary>? capturedVocabs = null;

        try
        {
            vm.SetReadableCommunityTags = tags => capturedTags = tags;
            vm.SetReadableCommunityVocabularies = vocabs => capturedVocabs = vocabs;
            vm.UpdateConfig(new AppConfig { Username = "Alice", GitHubUsername = "octocat" });
            await vm.LoadRootAsync(root, saveToConfig: false);

            await vm.RefreshCommunityTagDataAsync();

            Assert.NotNull(capturedTags);
            Assert.NotNull(capturedVocabs);
            Assert.DoesNotContain("Alice", capturedTags!.Keys);
            Assert.DoesNotContain("octocat", capturedTags.Keys);
            Assert.Equal(new[] { "otheruser" }, capturedTags.Keys.OrderBy(x => x));
            Assert.DoesNotContain("Alice", capturedVocabs!.Keys);
            Assert.DoesNotContain("octocat", capturedVocabs.Keys);
            Assert.Equal(new[] { "otheruser" }, capturedVocabs.Keys.OrderBy(x => x));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ApplySettingsToChildViews_PrefersGitHubLoginForReadableTagCompareIdentity()
    {
        var vm = MakeVm();
        string? compareIdentity = null;
        string? tagUsername = null;

        vm.SetReadableTagCompareIdentity = value => compareIdentity = value;
        vm.SetReadableTagUsername = value => tagUsername = value;
        vm.UpdateConfig(new AppConfig { Username = "Alice", GitHubUsername = "octocat" });

        vm.ApplySettingsToChildViews();

        Assert.Equal("octocat", compareIdentity);
        Assert.Equal("Alice", tagUsername);
    }

    [Fact]
    public void ApplySettingsToChildViews_FallsBackToUsernameForReadableTagCompareIdentity()
    {
        var vm = MakeVm();
        string? compareIdentity = null;
        string? tagUsername = null;

        vm.SetReadableTagCompareIdentity = value => compareIdentity = value;
        vm.SetReadableTagUsername = value => tagUsername = value;
        vm.UpdateConfig(new AppConfig { Username = "Alice", GitHubUsername = null });

        vm.ApplySettingsToChildViews();

        Assert.Equal("Alice", compareIdentity);
        Assert.Equal("Alice", tagUsername);
    }

    [Fact]
    public async Task SwitchTranslationSourceAsync_UpdatesSearchContextToMatchActiveTranslationSource()
    {
        var vm = MakeVm();
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5t"));
        Directory.CreateDirectory(Path.Combine(root, "community", "translations", "octocat"));
        Directory.CreateDirectory(Path.Combine(root, "community", "translations", "otheruser"));

        string? rootContextTranslatedDir = null;
        string? searchContextTranslatedDir = null;

        try
        {
            vm.SetSearchRootContext = (_, _, translatedDir) => rootContextTranslatedDir = translatedDir;
            vm.SetSearchContext = (_, _, translatedDir, _) => searchContextTranslatedDir = translatedDir;
            vm.UpdateConfig(new AppConfig { Username = "Alice", GitHubUsername = "octocat" });
            await vm.LoadRootAsync(root, saveToConfig: false);

            Assert.Equal(Path.Combine(root, "community", "translations", "octocat"), rootContextTranslatedDir);
            Assert.Equal(Path.Combine(root, "community", "translations", "octocat"), searchContextTranslatedDir);

            await vm.SwitchTranslationSourceAsync(1);
            Assert.Equal(Path.Combine(root, "xml-p5t"), rootContextTranslatedDir);
            Assert.Equal(Path.Combine(root, "xml-p5t"), searchContextTranslatedDir);

            await vm.SwitchTranslationSourceAsync(2);
            Assert.Equal(Path.Combine(root, "community", "translations", "otheruser"), rootContextTranslatedDir);
            Assert.Equal(Path.Combine(root, "community", "translations", "otheruser"), searchContextTranslatedDir);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SwitchTranslationSourceAsync_UpdatesReaderSourceIndexBridge()
    {
        var vm = MakeVm();
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5t"));
        Directory.CreateDirectory(Path.Combine(root, "community", "translations", "octocat"));
        Directory.CreateDirectory(Path.Combine(root, "community", "translations", "otheruser"));

        var seenIndexes = new List<int>();

        try
        {
            vm.SetReadableTranslationSourceIndex = index => seenIndexes.Add(index);
            vm.UpdateConfig(new AppConfig { Username = "Alice", GitHubUsername = "octocat" });
            await vm.LoadRootAsync(root, saveToConfig: false);

            await vm.SwitchTranslationSourceAsync(2);

            Assert.Contains(0, seenIndexes);
            Assert.Contains(2, seenIndexes);
            Assert.Equal(2, vm.GetActiveTranslationSourceIndex());
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task LoadConfigApplyThemeAndMaybeAutoloadAsync_DoesNotPromptForUsernameWhenMissing()
    {
        var configService = new StubAppConfigService
        {
            ConfigToReturn = new AppConfig { IsDarkTheme = true }
        };
        var vm = new MainWindowViewModel(
            new StubFileService(),
            configService,
            new StubIndexCacheService(),
            new StubRenderedDocumentCacheService(),
            new StubZenTextsService(),
            new StubIndexedTranslationService(),
            new StubTranslationAssistantService(),
            new StubTranslationAssistantBuildService(),
            new StubTranslationReviewService(),
            new StubSearchIndexService(),
            new StubDocumentTagService());

        var promptCalls = 0;
        vm.ShowUsernamePromptAsync = () =>
        {
            promptCalls++;
            return Task.FromResult<string?>("Alice");
        };

        await vm.LoadConfigApplyThemeAndMaybeAutoloadAsync(isSecondaryWindow: false);

        Assert.Equal(0, promptCalls);
        Assert.Null(vm.Config.Username);
    }

    [Fact]
    public async Task HandleGitHubAuthCompletedAsync_WithNoLegacyFolder_DoesNotCreateUserTranslationDirectory()
    {
        var vm = MakeVm();
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5"));

        try
        {
            vm.UpdateConfig(new AppConfig());
            await vm.LoadRootAsync(root, saveToConfig: false);

            await vm.HandleGitHubAuthCompletedAsync("ghp_test", "octocat");

            var githubDir = Path.Combine(root, "community", "translations", "octocat");
            Assert.False(Directory.Exists(githubDir));
            Assert.Equal("octocat", vm.Config.Username);
            Assert.Equal("octocat", vm.GetActiveTranslationUser());
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RestoreSearchTranslationSourceAsync_MapsMeCommunityAndOtherUser()
    {
        var vm = MakeVm();
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5t"));
        Directory.CreateDirectory(Path.Combine(root, "community", "translations", "octocat"));
        Directory.CreateDirectory(Path.Combine(root, "community", "translations", "otheruser"));

        try
        {
            vm.UpdateConfig(new AppConfig { Username = "Alice", GitHubUsername = "octocat" });
            await vm.LoadRootAsync(root, saveToConfig: false);

            Assert.Equal("me", vm.GetActiveSearchSourceKey());

            await vm.RestoreSearchTranslationSourceAsync("community");
            Assert.Equal(1, vm.GetActiveTranslationSourceIndex());
            Assert.Equal("community", vm.GetActiveSearchSourceKey());

            await vm.RestoreSearchTranslationSourceAsync("otheruser");
            Assert.Equal(2, vm.GetActiveTranslationSourceIndex());
            Assert.Equal("otheruser", vm.GetActiveSearchSourceKey());

            await vm.RestoreSearchTranslationSourceAsync("me");
            Assert.Equal(0, vm.GetActiveTranslationSourceIndex());
            Assert.Equal("me", vm.GetActiveSearchSourceKey());
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RestoreSearchTranslationSourceAsync_InvalidSource_FallsBackSafely()
    {
        var vm = MakeVm();
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5t"));
        Directory.CreateDirectory(Path.Combine(root, "community", "translations", "octocat"));

        try
        {
            vm.UpdateConfig(new AppConfig { Username = "Alice", GitHubUsername = "octocat" });
            await vm.LoadRootAsync(root, saveToConfig: false);

            var restored = await vm.RestoreSearchTranslationSourceAsync("missing-user");

            Assert.False(restored);
            Assert.Equal(0, vm.GetActiveTranslationSourceIndex());
            Assert.Equal("me", vm.GetActiveSearchSourceKey());
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
    [Fact]
    public async Task ApplySettingsToChildViews_UsesActiveTranslationUserForAssistantAndScholar()
    {
        var assistant = new StubTranslationAssistantService();
        var vm = new MainWindowViewModel(
            new StubFileService(),
            new StubAppConfigService(),
            new StubIndexCacheService(),
            new StubRenderedDocumentCacheService(),
            new StubZenTextsService(),
            new StubIndexedTranslationService(),
            assistant,
            new StubTranslationAssistantBuildService(),
            new StubTranslationReviewService(),
            new StubSearchIndexService(),
            new StubDocumentTagService());

        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5t"));
        Directory.CreateDirectory(Path.Combine(root, "community", "translations", "octocat"));
        Directory.CreateDirectory(Path.Combine(root, "community", "translations", "otheruser"));

        try
        {
            vm.UpdateConfig(new AppConfig { Username = "Alice", GitHubUsername = "octocat" });
            await vm.LoadRootAsync(root, saveToConfig: false);
            await vm.SwitchTranslationSourceAsync(2);

            string? scholarAssistantUser = null;
            vm.SetScholarAssistantUsername = user => scholarAssistantUser = user;

            vm.ApplySettingsToChildViews();

            Assert.Equal("otheruser", assistant.LastUsername);
            Assert.Equal("otheruser", scholarAssistantUser);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task OpenTermbaseEditorAsync_WhenViewingOtherUser_PassesOwnUsernameAndActiveCommunityUser()
    {
        var vm = MakeVm();
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5t"));
        Directory.CreateDirectory(Path.Combine(root, "community", "translations", "octocat"));
        Directory.CreateDirectory(Path.Combine(root, "community", "translations", "otheruser"));

        try
        {
            vm.UpdateConfig(new AppConfig { Username = "Alice", GitHubUsername = "octocat" });
            await vm.LoadRootAsync(root, saveToConfig: false);
            await vm.SwitchTranslationSourceAsync(2);

            string? seenRoot = null;
            string? seenUsername = null;
            string? seenTerm = null;
            string? seenCommunityUser = null;
            vm.OpenTermbaseEditorRequested = (r, username, term, communityUser) =>
            {
                seenRoot = r;
                seenUsername = username;
                seenTerm = term;
                seenCommunityUser = communityUser;
            };

            await vm.OpenTermbaseEditorAsync("gate");

            Assert.Equal(root, seenRoot);
            Assert.Equal("octocat", seenUsername);
            Assert.Equal("gate", seenTerm);
            Assert.Equal("otheruser", seenCommunityUser);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RefreshTranslationSources_PushesScholarDictionarySourceOptionsAndIndex()
    {
        var vm = MakeVm();
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5"));
        Directory.CreateDirectory(Path.Combine(root, "xml-p5t"));
        Directory.CreateDirectory(Path.Combine(root, "community", "translations", "octocat"));
        Directory.CreateDirectory(Path.Combine(root, "community", "translations", "otheruser"));

        try
        {
            vm.UpdateConfig(new AppConfig { Username = "Alice", GitHubUsername = "octocat" });
            List<string>? scholarOptions = null;
            int scholarIndex = -1;
            vm.SetScholarDictionarySourceOptions = options => scholarOptions = new List<string>(options);
            vm.SetScholarDictionarySourceIndex = index => scholarIndex = index;

            await vm.LoadRootAsync(root, saveToConfig: false);

            Assert.Equal(vm.GetTranslationSourceLabels(), scholarOptions);
            Assert.Equal(0, scholarIndex);

            await vm.SwitchTranslationSourceAsync(2);
            Assert.Equal(2, scholarIndex);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
