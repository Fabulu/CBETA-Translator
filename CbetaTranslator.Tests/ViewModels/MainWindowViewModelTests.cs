using System.IO;
using System.Reflection;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.ViewModels;
using CbetaTranslator.Tests.Stubs;
using Xunit;

namespace CbetaTranslator.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private static MainWindowViewModel MakeVm(StubDocumentTagService? documentTagService = null, IIndexedTranslationService? indexedTranslationService = null)
    {
        return new MainWindowViewModel(
            new StubFileService(),
            new StubAppConfigService(),
            new StubIndexCacheService(),
            new StubRenderedDocumentCacheService(),
            new StubZenTextsService(),
            indexedTranslationService ?? new StubIndexedTranslationService(),
            new StubTranslationAssistantService(),
            new StubTranslationAssistantBuildService(),
            new StubTranslationReviewService(),
            new StubSearchIndexService(),
            documentTagService ?? new StubDocumentTagService(),
            new StubGitRepoService());
    }

    /// <summary>
    /// Creates the two-repo directory layout expected by AppPaths.DiscoverRepoPaths.
    /// Returns (root, originalsSubfolder, translationsSubfolder).
    /// </summary>
    private static (string Root, string Originals, string Translations) CreateTwoRepoLayout(
        bool createXmlP5t = true,
        IEnumerable<string>? communityUsers = null)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var originals = Path.Combine(root, "originals");
        var translations = Path.Combine(root, "translations");
        Directory.CreateDirectory(Path.Combine(originals, "xml-p5"));
        if (createXmlP5t)
            Directory.CreateDirectory(Path.Combine(translations, "xml-p5t"));
        else
            Directory.CreateDirectory(translations); // needs to exist even if empty
        if (communityUsers != null)
        {
            foreach (var user in communityUsers)
                Directory.CreateDirectory(Path.Combine(translations, "community", "translations", user));
        }
        AppPaths.InvalidateDiscoveryCache();
        return (root, originals, translations);
    }

    private static void CleanupTwoRepoLayout(string root)
    {
        AppPaths.InvalidateDiscoveryCache();
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
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
    public async Task FindTranslatedPath_FallsBackToCommunityUserWhenSelectedSourceIsEffectivelyUntranslated()
    {
        var vm = MakeVm(indexedTranslationService: new IndexedTranslationService());
        var (root, originals, translations) = CreateTwoRepoLayout(communityUsers: new[] { "octocat", "otheruser" });
        var relPath = "T01/test.xml";
        var originalDir = Path.Combine(originals, "xml-p5", "T01");
        var communityDir = Path.Combine(translations, "xml-p5t", "T01");
        var myDir = Path.Combine(translations, "community", "translations", "octocat", "T01");
        var otherDir = Path.Combine(translations, "community", "translations", "otheruser", "T01");
        Directory.CreateDirectory(originalDir);
        Directory.CreateDirectory(communityDir);
        Directory.CreateDirectory(myDir);
        Directory.CreateDirectory(otherDir);

        const string originalXml = "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p xml:id=\"p1\">無門關</p></body></text></TEI>";
        const string translatedXml = "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p xml:id=\"p1\">Translated passage</p></body></text></TEI>";

        await File.WriteAllTextAsync(Path.Combine(originalDir, "test.xml"), originalXml);
        await File.WriteAllTextAsync(Path.Combine(myDir, "test.xml"), originalXml);
        await File.WriteAllTextAsync(Path.Combine(otherDir, "test.xml"), translatedXml);

        try
        {
            vm.UpdateConfig(new AppConfig { Username = "Alice", GitHubUsername = "octocat" });
            await vm.LoadRootAsync(root, saveToConfig: false);

            var resolved = InvokeFindTranslatedPath(vm, relPath);

            Assert.Equal(Path.GetFullPath(Path.Combine(otherDir, "test.xml")), Path.GetFullPath(resolved!));
        }
        finally
        {
            CleanupTwoRepoLayout(root);
        }
    }

    [Fact]
    public async Task FindTranslatedPath_PicksNewestMeaningfulCommunityUserFallback()
    {
        var vm = MakeVm(indexedTranslationService: new IndexedTranslationService());
        var (root, originals, translations) = CreateTwoRepoLayout(communityUsers: new[] { "olderuser", "neweruser" });
        var relPath = "T01/test.xml";
        var originalDir = Path.Combine(originals, "xml-p5", "T01");
        var communityDir = Path.Combine(translations, "xml-p5t", "T01");
        var olderDir = Path.Combine(translations, "community", "translations", "olderuser", "T01");
        var newerDir = Path.Combine(translations, "community", "translations", "neweruser", "T01");
        Directory.CreateDirectory(originalDir);
        Directory.CreateDirectory(communityDir);
        Directory.CreateDirectory(olderDir);
        Directory.CreateDirectory(newerDir);

        const string originalXml = "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p xml:id=\"p1\">無門關</p></body></text></TEI>";
        const string olderXml = "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p xml:id=\"p1\">Older translation</p></body></text></TEI>";
        const string newerXml = "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p xml:id=\"p1\">Newer translation</p></body></text></TEI>";

        var originalPath = Path.Combine(originalDir, "test.xml");
        var communityPath = Path.Combine(communityDir, "test.xml");
        var olderPath = Path.Combine(olderDir, "test.xml");
        var newerPath = Path.Combine(newerDir, "test.xml");

        await File.WriteAllTextAsync(originalPath, originalXml);
        await File.WriteAllTextAsync(communityPath, originalXml);
        await File.WriteAllTextAsync(olderPath, olderXml);
        await File.WriteAllTextAsync(newerPath, newerXml);
        File.SetLastWriteTimeUtc(olderPath, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(newerPath, new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc));

        try
        {
            vm.UpdateConfig(new AppConfig { Username = "Alice", GitHubUsername = "octocat" });
            await vm.LoadRootAsync(root, saveToConfig: false);
            await vm.SwitchTranslationSourceAsync(1);

            var resolved = InvokeFindTranslatedPath(vm, relPath);

            Assert.Equal(Path.GetFullPath(newerPath), Path.GetFullPath(resolved!));
        }
        finally
        {
            CleanupTwoRepoLayout(root);
        }
    }

    [Fact]
    public async Task ResolveBestTranslationSourceIndex_DefaultsToCommunityWhenCommunityTiesForBestTranslation()
    {
        var vm = MakeVm(indexedTranslationService: new IndexedTranslationService());
        var (root, originals, translations) = CreateTwoRepoLayout(communityUsers: new[] { "octocat" });
        var relPath = "T01/test.xml";
        var originalDir = Path.Combine(originals, "xml-p5", "T01");
        var communityDir = Path.Combine(translations, "xml-p5t", "T01");
        var myDir = Path.Combine(translations, "community", "translations", "octocat", "T01");
        Directory.CreateDirectory(originalDir);
        Directory.CreateDirectory(communityDir);
        Directory.CreateDirectory(myDir);

        const string originalXml = "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p xml:id=\"p1\">無門關</p></body></text></TEI>";
        const string translatedXml = "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p xml:id=\"p1\">Translated passage</p></body></text></TEI>";

        await File.WriteAllTextAsync(Path.Combine(originalDir, "test.xml"), originalXml);
        await File.WriteAllTextAsync(Path.Combine(communityDir, "test.xml"), translatedXml);
        await File.WriteAllTextAsync(Path.Combine(myDir, "test.xml"), translatedXml);

        try
        {
            vm.UpdateConfig(new AppConfig { Username = "Alice", GitHubUsername = "octocat" });
            await vm.LoadRootAsync(root, saveToConfig: false);
            Assert.Equal(1, InvokeResolveBestTranslationSourceIndex(vm, relPath));
        }
        finally
        {
            CleanupTwoRepoLayout(root);
        }
    }

    [Fact]
    public async Task RefreshAllCachedStatusesAsync_UsesBestAvailableTranslationStatusAcrossSources()
    {
        var vm = MakeVm(indexedTranslationService: new IndexedTranslationService());
        var (root, originals, translations) = CreateTwoRepoLayout(communityUsers: new[] { "otheruser" });
        var relPath = "T01/test.xml";
        var originalDir = Path.Combine(originals, "xml-p5", "T01");
        var communityDir = Path.Combine(translations, "xml-p5t", "T01");
        var otherDir = Path.Combine(translations, "community", "translations", "otheruser", "T01");
        Directory.CreateDirectory(originalDir);
        Directory.CreateDirectory(communityDir);
        Directory.CreateDirectory(otherDir);

        const string originalXml = "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p xml:id=\"p1\">無門關</p></body></text></TEI>";
        const string partialXml = "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p xml:id=\"p1\">無門關</p><p xml:id=\"p2\">Translated passage</p></body></text></TEI>";
        const string fullXml = "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p xml:id=\"p1\">Translated passage</p></body></text></TEI>";

        await File.WriteAllTextAsync(Path.Combine(originalDir, "test.xml"), originalXml);
        await File.WriteAllTextAsync(Path.Combine(communityDir, "test.xml"), partialXml);
        await File.WriteAllTextAsync(Path.Combine(otherDir, "test.xml"), fullXml);

        try
        {
            vm.UpdateConfig(new AppConfig { Username = "Alice", GitHubUsername = "octocat" });
            await vm.LoadRootAsync(root, saveToConfig: false);

            var item = new FileNavItem { RelPath = relPath, FileName = "test.xml", DisplayShort = "test", Tooltip = relPath, Status = TranslationStatus.Red };
            typeof(MainWindowViewModel)
                .GetField("_allItems", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(vm, new List<FileNavItem> { item });
            vm.AllItemsByRel[MainWindowViewModel.NormalizeRel(relPath)] = item;

            await vm.RefreshAllCachedStatusesAsync();

            Assert.Equal(TranslationStatus.Green, item.Status);
        }
        finally
        {
            CleanupTwoRepoLayout(root);
        }
    }
    private static string? InvokeFindTranslatedPath(MainWindowViewModel vm, string relPath)
    {
        return (string?)typeof(MainWindowViewModel)
            .GetMethod("FindTranslatedPath", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(vm, new object[] { relPath });
    }

    private static int InvokeResolveBestTranslationSourceIndex(MainWindowViewModel vm, string relPath)
    {
        return (int)typeof(MainWindowViewModel)
            .GetMethod("ResolveBestTranslationSourceIndex", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(vm, new object[] { relPath })!;
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
        var (root, _, translations) = CreateTwoRepoLayout();

        var legacyDir = Path.Combine(translations, "community", "translations", "Alice");
        Directory.CreateDirectory(Path.Combine(legacyDir, "T01"));
        var legacyFile = Path.Combine(legacyDir, "T01", "test.xml");
        await File.WriteAllTextAsync(legacyFile, "<xml>legacy</xml>");

        try
        {
            vm.UpdateConfig(new AppConfig { Username = "Alice" });
            await vm.LoadRootAsync(root, saveToConfig: false);

            await vm.HandleGitHubAuthCompletedAsync("ghp_test", "octocat");

            var githubFile = Path.Combine(translations, "community", "translations", "octocat", "T01", "test.xml");
            Assert.True(File.Exists(githubFile));
            Assert.False(Directory.Exists(legacyDir));
            Assert.Equal("octocat", vm.GetActiveTranslationUser());
        }
        finally
        {
            CleanupTwoRepoLayout(root);
        }
    }

    [Fact]
    public async Task RefreshTranslationSources_UsesGitHubFolderAsCurrentUserIdentity()
    {
        var vm = MakeVm();
        var (root, _, _) = CreateTwoRepoLayout(communityUsers: new[] { "octocat", "alice", "otheruser" });

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
            CleanupTwoRepoLayout(root);
        }
    }

    [Fact]
    public async Task RefreshTranslationSources_PushesReaderSourceOptionsAndIndex()
    {
        var vm = MakeVm();
        var (root, _, _) = CreateTwoRepoLayout(communityUsers: new[] { "octocat", "otheruser" });

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
            CleanupTwoRepoLayout(root);
        }
    }

    [Fact]
    public async Task HandleGitHubAuthCompletedAsync_ConflictingLegacyFolderIsDeletedAndGitHubFolderWins()
    {
        var vm = MakeVm();
        var (root, _, translations) = CreateTwoRepoLayout();

        var legacyDir = Path.Combine(translations, "community", "translations", "Alice");
        var githubDir = Path.Combine(translations, "community", "translations", "octocat");
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
            CleanupTwoRepoLayout(root);
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
        var (root, _, _) = CreateTwoRepoLayout();

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
            CleanupTwoRepoLayout(root);
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
        var (root, _, translations) = CreateTwoRepoLayout(communityUsers: new[] { "octocat", "otheruser" });

        string? rootContextTranslatedDir = null;
        string? searchContextTranslatedDir = null;

        try
        {
            vm.SetSearchRootContext = (_, _, translatedDir) => rootContextTranslatedDir = translatedDir;
            vm.SetSearchContext = (_, _, translatedDir, _) => searchContextTranslatedDir = translatedDir;
            vm.UpdateConfig(new AppConfig { Username = "Alice", GitHubUsername = "octocat" });
            await vm.LoadRootAsync(root, saveToConfig: false);

            Assert.Equal(Path.Combine(translations, "community", "translations", "octocat"), rootContextTranslatedDir);
            Assert.Equal(Path.Combine(translations, "community", "translations", "octocat"), searchContextTranslatedDir);

            await vm.SwitchTranslationSourceAsync(1);
            Assert.Equal(Path.Combine(translations, "xml-p5t"), rootContextTranslatedDir);
            Assert.Equal(Path.Combine(translations, "xml-p5t"), searchContextTranslatedDir);

            await vm.SwitchTranslationSourceAsync(2);
            Assert.Equal(Path.Combine(translations, "community", "translations", "otheruser"), rootContextTranslatedDir);
            Assert.Equal(Path.Combine(translations, "community", "translations", "otheruser"), searchContextTranslatedDir);
        }
        finally
        {
            CleanupTwoRepoLayout(root);
        }
    }

    [Fact]
    public async Task SwitchTranslationSourceAsync_UpdatesReaderSourceIndexBridge()
    {
        var vm = MakeVm();
        var (root, _, _) = CreateTwoRepoLayout(communityUsers: new[] { "octocat", "otheruser" });

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
            CleanupTwoRepoLayout(root);
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
            new StubDocumentTagService(),
            new StubGitRepoService());

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
        var (root, _, translations) = CreateTwoRepoLayout();

        try
        {
            vm.UpdateConfig(new AppConfig());
            await vm.LoadRootAsync(root, saveToConfig: false);

            await vm.HandleGitHubAuthCompletedAsync("ghp_test", "octocat");

            var githubDir = Path.Combine(translations, "community", "translations", "octocat");
            Assert.False(Directory.Exists(githubDir));
            Assert.Equal("octocat", vm.Config.Username);
            Assert.Equal("octocat", vm.GetActiveTranslationUser());
        }
        finally
        {
            CleanupTwoRepoLayout(root);
        }
    }

    [Fact]
    public async Task RestoreSearchTranslationSourceAsync_MapsMeCommunityAndOtherUser()
    {
        var vm = MakeVm();
        var (root, _, _) = CreateTwoRepoLayout(communityUsers: new[] { "octocat", "otheruser" });

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
            CleanupTwoRepoLayout(root);
        }
    }

    [Fact]
    public async Task RestoreSearchTranslationSourceAsync_InvalidSource_FallsBackSafely()
    {
        var vm = MakeVm();
        var (root, _, _) = CreateTwoRepoLayout(communityUsers: new[] { "octocat" });

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
            CleanupTwoRepoLayout(root);
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
            new StubDocumentTagService(),
            new StubGitRepoService());

        var (root, _, _) = CreateTwoRepoLayout(communityUsers: new[] { "octocat", "otheruser" });

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
            CleanupTwoRepoLayout(root);
        }
    }

    [Fact]
    public async Task OpenTermbaseEditorAsync_WhenViewingOtherUser_PassesOwnUsernameAndActiveCommunityUser()
    {
        var vm = MakeVm();
        var (root, _, translations) = CreateTwoRepoLayout(communityUsers: new[] { "octocat", "otheruser" });

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

            Assert.Equal(translations, seenRoot);
            Assert.Equal("octocat", seenUsername);
            Assert.Equal("gate", seenTerm);
            Assert.Equal("otheruser", seenCommunityUser);
        }
        finally
        {
            CleanupTwoRepoLayout(root);
        }
    }

    [Fact]
    public async Task RefreshTranslationSources_PushesScholarDictionarySourceOptionsAndIndex()
    {
        var vm = MakeVm();
        var (root, _, _) = CreateTwoRepoLayout(communityUsers: new[] { "octocat", "otheruser" });

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
            CleanupTwoRepoLayout(root);
        }
    }    [Fact]
    public async Task ResetTranslatedToUntranslatedAsync_ConfirmsAndOverwritesWritableTranslation()
    {
        var vm = MakeVm();
        var (root, originals, translations) = CreateTwoRepoLayout();
        Directory.CreateDirectory(Path.Combine(originals, "xml-p5", "T01"));
        Directory.CreateDirectory(Path.Combine(translations, "xml-p5t", "T01"));

        const string relPath = "T01/test.xml";
        const string originalXml = "<TEI><text><body><p>??</p></body></text></TEI>";
        const string translatedXml = "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><div><head>Translated Title</head><p>Body EN<lb/>Tail EN</p><p>Closing EN</p></div></body></text></TEI>";

        await File.WriteAllTextAsync(Path.Combine(originals, "xml-p5", relPath), originalXml);
        await File.WriteAllTextAsync(Path.Combine(translations, "xml-p5t", relPath), translatedXml);

        try
        {
            vm.UpdateConfig(new AppConfig { Username = "Alice", GitHubUsername = "octocat" });
            await vm.LoadRootAsync(root, saveToConfig: false);

            typeof(MainWindowViewModel)
                .GetField("_currentRelPath", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(vm, relPath);

            vm.GetTranslationProjectionText = () => string.Empty;

            string? promptTitle = null;
            string? promptMessage = null;
            vm.ShowYesNoDialogAsync = (title, message) =>
            {
                promptTitle = title;
                promptMessage = message;
                return Task.FromResult(true);
            };

            await vm.ResetTranslatedToUntranslatedAsync();

            var writePath = Path.Combine(translations, "community", "translations", "octocat", relPath);
            Assert.True(File.Exists(writePath));
            Assert.Equal(originalXml, await File.ReadAllTextAsync(writePath));
            Assert.Equal("Fresh Start Translation", promptTitle);
            Assert.Contains("will be lost", promptMessage);
        }
        finally
        {
            CleanupTwoRepoLayout(root);
        }
    }
}












