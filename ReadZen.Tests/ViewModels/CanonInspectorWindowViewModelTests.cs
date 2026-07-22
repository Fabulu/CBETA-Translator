using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using Xunit;

namespace ReadZen.Tests.ViewModels;

/// <summary>
/// Tests for the read-only Canon Inspector VM: the pure section-grouping helper, CBETA-id
/// extraction, English-title fallback, and the navigation event that opens a text in the reader.
/// </summary>
public class CanonInspectorWindowViewModelTests
{
    private static CanonTextRow Row(string rel, string? en = null, string? zh = null)
        => CanonTextRow.Create(rel, en, zh);

    [Theory]
    [InlineData("T/T48/T48n2003.xml", "T")]
    [InlineData(@"J\J24\J24nB137.xml", "J")]
    [InlineData("/X/X68/X68n1315.xml", "X")]
    [InlineData("loose.xml", "LOOSE.XML")]
    [InlineData("", "?")]
    public void SectionOf_UsesFirstPathSegment(string rel, string expected)
        => Assert.Equal(expected, CanonInspectorWindowViewModel.SectionOf(rel));

    [Theory]
    [InlineData("T/T48/T48n2003.xml", "T48n2003")]
    [InlineData(@"J\J24\J24nB137.xml", "J24nB137")]
    [InlineData("bare", "bare")]
    public void ExtractCbetaId_StripsFolderAndExtension(string rel, string expected)
        => Assert.Equal(expected, CanonTextRow.ExtractCbetaId(rel));

    [Fact]
    public void GroupBySection_GroupsBySection_OrdersSectionsAlpha_KeepsRowOrder()
    {
        var rows = new List<CanonTextRow>
        {
            Row("T/T48/T48n2003.xml"),
            Row("J/J24/J24nB137.xml"),
            Row("T/T48/T48n2005.xml"),
            Row("B/B14/B14n0082.xml"),
        };

        var groups = CanonInspectorWindowViewModel.GroupBySection(rows);

        // Sections alphabetical: B, J, T
        Assert.Equal(new[] { "B", "J", "T" }, groups.Select(g => g.Section).ToArray());

        var t = groups.Single(g => g.Section == "T");
        Assert.Equal(2, t.Count);
        // Row order within a section is preserved from input (asset order).
        Assert.Equal("T48n2003", t.Rows[0].CbetaId);
        Assert.Equal("T48n2005", t.Rows[1].CbetaId);
    }

    [Fact]
    public void PrimaryTitle_FallsBackToCbetaId_WhenNoEnglish()
    {
        var withEn = Row("T/T48/T48n2003.xml", en: "Records of the Blue Cliff", zh: "碧巖錄");
        Assert.Equal("Records of the Blue Cliff", withEn.PrimaryTitle);
        Assert.True(withEn.HasEnglishTitle);
        Assert.Equal("碧巖錄", withEn.ChineseTitle);

        var noEn = Row("T/T48/T48n2005.xml", en: null, zh: "從容錄");
        Assert.Equal("T48n2005", noEn.PrimaryTitle);
        Assert.False(noEn.HasEnglishTitle);
    }

    [Fact]
    public async Task Ctor_ProjectsCanon_AndOpenTextRaisesNavigation()
    {
        var zen = new FakeZen(
            texts: new[] { "T/T48/T48n2003.xml", "J/J24/J24nB137.xml" },
            listVersion: "v1",
            note: "Curated allowlist.");

        var vm = new CanonInspectorWindowViewModel(
            zen,
            rel => rel.Contains("T48n2003") ? ("Blue Cliff Record", "碧巖錄") : (null, null));

        Assert.Equal("v1", vm.HeaderVersion);
        Assert.True(vm.HasVersion);
        Assert.Equal("2 texts", vm.HeaderCount);
        Assert.Equal("Curated allowlist.", vm.GeneratedNote);
        Assert.Equal(2, vm.Sections.Sum(s => s.Count));

        NavigationRequest? captured = null;
        vm.NavigationRequested += (_, req) => captured = req;

        var row = vm.Sections.SelectMany(s => s.Rows).First(r => r.CbetaId == "T48n2003");
        vm.OpenTextCommand.Execute(row);

        Assert.NotNull(captured);
        Assert.Equal("T/T48/T48n2003.xml", captured!.RelPath);
        Assert.Equal(SearchSide.Original, captured.Side);
    }

    private sealed class FakeZen : IZenTextsService
    {
        private readonly IReadOnlyList<string> _texts;
        public FakeZen(IReadOnlyList<string> texts, string? listVersion, string? note)
        {
            _texts = texts;
            ListVersion = listVersion;
            GeneratedNote = note;
        }

        public Task LoadAsync(string root) => Task.CompletedTask;
        public bool IsZen(string relPath) => _texts.Contains(relPath);
        public Task SetZenAsync(string root, string relPath, bool isZen) => Task.CompletedTask;
        public IReadOnlyList<string> Texts => _texts;
        public string? ListVersion { get; }
        public string? GeneratedNote { get; }
    }
}
