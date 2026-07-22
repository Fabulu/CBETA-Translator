using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Behavior tests for <see cref="TypeaheadService"/> — the search-box suggestion
/// engine. Covers input gating, prefix-before-contains ranking, the Masters (top 3)
/// and Texts (top 5) caps, the always-present full-text action, the tooltip
/// "English · 中文" title split, and Initialize's don't-overwrite-with-null guard.
/// </summary>
public sealed class TypeaheadServiceTests
{
    private static ZenMasterRecord Master(string canonical, params string[] aliases)
        => new() { CanonicalName = canonical, Aliases = aliases.ToList() };

    private static ZenMasterCatalog Catalog(params ZenMasterRecord[] records)
        => new() { Records = records.ToList() };

    private static FileNavItem File(string display, string tooltip = "", string relPath = "", string fileName = "")
        => new()
        {
            DisplayShort = display,
            Tooltip = tooltip,
            RelPath = relPath.Length > 0 ? relPath : display + ".xml",
            FileName = fileName.Length > 0 ? fileName : display + ".xml",
        };

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Query_BlankInput_ReturnsEmpty(string input)
    {
        var svc = new TypeaheadService();
        svc.Initialize(Catalog(Master("Linji Yixuan", "臨濟義玄")), new List<FileNavItem>());
        Assert.Empty(svc.Query(input));
    }

    [Fact]
    public void Query_NoMatches_StillEmitsFullTextAction()
    {
        var svc = new TypeaheadService();
        svc.Initialize(Catalog(Master("Linji Yixuan")), new[] { File("Platform Sutra") });

        var results = svc.Query("zzzznothing");

        var only = Assert.Single(results);
        Assert.Equal(TypeaheadItemKind.FullTextAction, only.Kind);
        Assert.Equal("zzzznothing", only.Query);
    }

    [Fact]
    public void Query_FullTextAction_UsesTrimmedInput()
    {
        var svc = new TypeaheadService();
        svc.Initialize(Catalog(), new List<FileNavItem>());

        var action = svc.Query("  hello world  ").Single(r => r.Kind == TypeaheadItemKind.FullTextAction);
        Assert.Equal("hello world", action.Query);
    }

    [Fact]
    public void Query_MasterMatch_EmitsHeaderThenRecord()
    {
        var svc = new TypeaheadService();
        svc.Initialize(Catalog(Master("Linji Yixuan", "臨濟義玄")), new List<FileNavItem>());

        var results = svc.Query("linji");

        Assert.Equal(TypeaheadItemKind.SectionHeader, results[0].Kind);
        Assert.Equal("Masters", results[0].HeaderText);
        Assert.Equal(TypeaheadItemKind.Master, results[1].Kind);
        Assert.Equal("Linji Yixuan", results[1].DisplayName);
        Assert.Equal("Linji Yixuan", results[1].Master!.CanonicalName);
    }

    [Fact]
    public void Query_PrefixMatchRanksBeforeContainsMatch()
    {
        var svc = new TypeaheadService();
        svc.Initialize(
            Catalog(
                Master("Great Master Zhao"),   // "zhao" appears mid-blob (contains)
                Master("Zhaozhou Congshen")),  // starts with "zhao" (prefix)
            new List<FileNavItem>());

        var masters = svc.Query("zhao")
            .Where(r => r.Kind == TypeaheadItemKind.Master)
            .Select(r => r.DisplayName)
            .ToList();

        Assert.Equal("Zhaozhou Congshen", masters[0]);
        Assert.Contains("Great Master Zhao", masters);
    }

    [Fact]
    public void Query_MasterResults_CappedAtThree()
    {
        var svc = new TypeaheadService();
        svc.Initialize(
            Catalog(
                Master("Zen One"), Master("Zen Two"), Master("Zen Three"),
                Master("Zen Four"), Master("Zen Five")),
            new List<FileNavItem>());

        var masters = svc.Query("zen").Count(r => r.Kind == TypeaheadItemKind.Master);
        Assert.Equal(3, masters);
    }

    [Fact]
    public void Query_TitleResults_CappedAtFive()
    {
        var svc = new TypeaheadService();
        var files = Enumerable.Range(1, 8).Select(i => File($"Sutra {i}")).ToArray();
        svc.Initialize(Catalog(), files);

        var titles = svc.Query("sutra").Count(r => r.Kind == TypeaheadItemKind.Title);
        Assert.Equal(5, titles);
    }

    [Fact]
    public void Query_TitleTooltip_SplitsEnglishAndChinese()
    {
        var svc = new TypeaheadService();
        svc.Initialize(Catalog(), new[]
        {
            File("Platform Sutra", tooltip: "Platform Sutra of the Sixth Patriarch · 六祖壇經")
        });

        var title = svc.Query("platform").Single(r => r.Kind == TypeaheadItemKind.Title);
        Assert.Equal("Platform Sutra of the Sixth Patriarch", title.EnTitle);
        Assert.Equal("六祖壇經", title.ZhTitle);
    }

    [Fact]
    public void Query_TitleTooltip_CjkOnly_GoesToZhTitle()
    {
        var svc = new TypeaheadService();
        svc.Initialize(Catalog(), new[]
        {
            // No " · " separator, leading CJK → treated as Chinese title.
            File("壇經", tooltip: "六祖壇經")
        });

        var title = svc.Query("壇經").Single(r => r.Kind == TypeaheadItemKind.Title);
        Assert.Equal("六祖壇經", title.ZhTitle);
    }

    [Fact]
    public void Query_MasterAliasPullsInRelatedTexts()
    {
        var svc = new TypeaheadService();
        svc.Initialize(
            Catalog(Master("Huineng", "六祖")),
            new[]
            {
                // Title blob contains the alias but not the English query "huineng".
                File("Platform Sutra", tooltip: "Platform Sutra · 六祖壇經", relPath: "T48/platform.xml")
            });

        var results = svc.Query("huineng");
        var titles = results.Where(r => r.Kind == TypeaheadItemKind.Title).ToList();

        Assert.Contains(titles, t => t.FileItem!.RelPath == "T48/platform.xml");
    }

    [Fact]
    public void Initialize_NullArguments_DoNotOverwriteExistingState()
    {
        var svc = new TypeaheadService();
        svc.Initialize(Catalog(Master("Linji Yixuan")), new[] { File("Some Text") });

        // Second call with both null must be a no-op, not a wipe.
        svc.Initialize(null, null);

        Assert.Contains(svc.Query("linji"), r => r.Kind == TypeaheadItemKind.Master);
        Assert.Contains(svc.Query("some"), r => r.Kind == TypeaheadItemKind.Title);
    }
}
