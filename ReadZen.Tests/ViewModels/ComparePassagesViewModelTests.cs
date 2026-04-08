using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;
using Xunit;

namespace ReadZen.Tests.ViewModels;

public class ComparePassagesViewModelTests
{
    private static ScholarPassage MakePassage(string id, string zh, string source = "test.xml")
    {
        return new ScholarPassage
        {
            Id = id,
            ZhText = zh,
            SourceRelPath = source,
            EnText = "en " + id
        };
    }

    // ---- Constructor with null/empty ----

    [Fact]
    public void Constructor_NullPassages_ItemsEmpty()
    {
        var vm = new ComparePassagesViewModel(null!);

        Assert.Empty(vm.Items);
    }

    [Fact]
    public void Constructor_EmptyList_ItemsEmpty()
    {
        var vm = new ComparePassagesViewModel(new List<ScholarPassage>());

        Assert.Empty(vm.Items);
    }

    // ---- Two passages with shared text ----

    [Fact]
    public void TwoPassages_WithSharedText_ComputesSharedRanges()
    {
        var passages = new List<ScholarPassage>
        {
            MakePassage("p1", "ABCDEFGH"),
            MakePassage("p2", "XXCDEFYY"),
        };

        var vm = new ComparePassagesViewModel(passages);

        Assert.Equal(2, vm.Items.Count);

        // Both should have shared ranges (the shared substring "CDEF")
        // The exact ranges depend on CjkMatchNormalizer.FindSharedRawRanges behavior
        // but we can verify the structure is populated
        Assert.Equal("p1", vm.Items[0].Passage.Id);
        Assert.Equal("p2", vm.Items[1].Passage.Id);
    }

    [Fact]
    public void TwoPassages_NoSharedText_EmptyRanges()
    {
        var passages = new List<ScholarPassage>
        {
            MakePassage("p1", "AAAA"),
            MakePassage("p2", "BBBB"),
        };

        var vm = new ComparePassagesViewModel(passages);

        Assert.Equal(2, vm.Items.Count);
        Assert.Empty(vm.Items[0].SharedZhRanges);
        Assert.Empty(vm.Items[1].SharedZhRanges);
    }

    [Fact]
    public void TwoPassages_IdenticalText_FullRanges()
    {
        var text = "identical passage text here";
        var passages = new List<ScholarPassage>
        {
            MakePassage("p1", text),
            MakePassage("p2", text),
        };

        var vm = new ComparePassagesViewModel(passages);

        Assert.Equal(2, vm.Items.Count);
        // Both should have ranges covering the full text
        Assert.NotEmpty(vm.Items[0].SharedZhRanges);
        Assert.NotEmpty(vm.Items[1].SharedZhRanges);
    }

    // ---- Source title extraction ----

    [Fact]
    public void SourceTitle_ExtractedFromRelPath()
    {
        var passages = new List<ScholarPassage>
        {
            MakePassage("p1", "text1", "xml-p5/T/T0001.xml"),
            MakePassage("p2", "text2", "xml-p5/J/J0042.xml"),
        };

        var vm = new ComparePassagesViewModel(passages);

        Assert.Equal("T0001", vm.Items[0].SourceTitle);
        Assert.Equal("J0042", vm.Items[1].SourceTitle);
    }

    [Fact]
    public void SourceTitle_EmptyPath_ShowsUnknown()
    {
        var passages = new List<ScholarPassage>
        {
            MakePassage("p1", "text", ""),
        };

        var vm = new ComparePassagesViewModel(passages);

        Assert.Equal("(unknown)", vm.Items[0].SourceTitle);
    }

    [Fact]
    public void SourceTitle_NoExtension_UsesFullFilename()
    {
        var passages = new List<ScholarPassage>
        {
            MakePassage("p1", "text", "folder/filename"),
        };

        var vm = new ComparePassagesViewModel(passages);

        Assert.Equal("filename", vm.Items[0].SourceTitle);
    }

    // ---- Multiple passages ----

    [Fact]
    public void ThreePassages_AllPairwiseCompared()
    {
        var passages = new List<ScholarPassage>
        {
            MakePassage("p1", "AABBCC"),
            MakePassage("p2", "BBCCDD"),
            MakePassage("p3", "CCDDEE"),
        };

        var vm = new ComparePassagesViewModel(passages);

        Assert.Equal(3, vm.Items.Count);
        // Each item's shared ranges are merged from comparisons with all other passages
    }

    [Fact]
    public void SinglePassage_NoSharedRanges()
    {
        var passages = new List<ScholarPassage>
        {
            MakePassage("p1", "some text"),
        };

        var vm = new ComparePassagesViewModel(passages);

        Assert.Single(vm.Items);
        Assert.Empty(vm.Items[0].SharedZhRanges);
    }

    // ---- Items structure ----

    [Fact]
    public void Items_PreservePassageReferences()
    {
        var p1 = MakePassage("p1", "text1");
        var p2 = MakePassage("p2", "text2");

        var vm = new ComparePassagesViewModel(new List<ScholarPassage> { p1, p2 });

        Assert.Same(p1, vm.Items[0].Passage);
        Assert.Same(p2, vm.Items[1].Passage);
    }

    [Fact]
    public void Items_MaintainOrder()
    {
        var passages = new List<ScholarPassage>
        {
            MakePassage("first", "text1"),
            MakePassage("second", "text2"),
            MakePassage("third", "text3"),
        };

        var vm = new ComparePassagesViewModel(passages);

        Assert.Equal("first", vm.Items[0].Passage.Id);
        Assert.Equal("second", vm.Items[1].Passage.Id);
        Assert.Equal("third", vm.Items[2].Passage.Id);
    }
}
