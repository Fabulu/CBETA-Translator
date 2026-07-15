using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Unit tests for the pure evidence helpers (MapOccurrence + AttachMasterAttribution). The async
/// query path is integration-level (needs a built search index) and is exercised in-app.
/// </summary>
public class DictionaryEvidenceServiceTests
{
    [Fact]
    public void MapOccurrence_BuildsKwicAndOffset()
    {
        var child = new SearchResultChild
        {
            RelPath = "T/T48/T48n2005.xml",
            Side = SearchSide.Original,
            Hit = new SearchHit { Left = "前", Match = "水牯牛", Right = "後", Index = 42 },
        };

        var occ = DictionaryEvidenceService.MapOccurrence(child, "T/T48/T48n2005.xml");

        Assert.Equal("T/T48/T48n2005.xml", occ.RelPath);
        Assert.Equal("前水牯牛後", occ.Kwic);
        Assert.Equal(42, occ.CharOffset);
        Assert.False(occ.Curated);
    }

    [Fact]
    public void AttachMasterAttribution_SetsMasterName_AcrossSlashStyles()
    {
        var texts = new List<DictEvidenceGroup>
        {
            new() { RelPath = "T/T48/T48n2005.xml", HitCount = 3 },
        };
        var index = new MasterCorpusIndex
        {
            // Master index stores backslash relpaths — join must normalize.
            Appearances = new List<MasterTextAppearance>
            {
                new() { MasterName = "Wumen Huikai", RelPath = @"T\T48\T48n2005.xml", AppearanceType = "primary" },
                new() { MasterName = "Someone Else", RelPath = @"T\T48\T48n2005.xml", AppearanceType = "secondary" },
            }
        };

        var rollup = DictionaryEvidenceService.AttachMasterAttribution(texts, index);

        Assert.Equal("Wumen Huikai", texts[0].MasterName); // primary only
        var u = Assert.Single(rollup);
        Assert.Equal("Wumen Huikai", u.MasterName);
        Assert.Equal(1, u.TextCount);
        Assert.Equal(3, u.HitCount);
    }

    [Fact]
    public void AttachMasterAttribution_RanksByTextCountThenHits()
    {
        var texts = new List<DictEvidenceGroup>
        {
            new() { RelPath = "a.xml", HitCount = 2 },
            new() { RelPath = "b.xml", HitCount = 3 },
            new() { RelPath = "c.xml", HitCount = 10 },
        };
        var index = new MasterCorpusIndex
        {
            Appearances = new List<MasterTextAppearance>
            {
                new() { MasterName = "A", RelPath = "a.xml", AppearanceType = "primary" }, // A in 2 texts (5 hits)
                new() { MasterName = "A", RelPath = "b.xml", AppearanceType = "primary" },
                new() { MasterName = "B", RelPath = "c.xml", AppearanceType = "primary" }, // B in 1 text (10 hits)
            }
        };

        var rollup = DictionaryEvidenceService.AttachMasterAttribution(texts, index);

        Assert.Equal(2, rollup.Count);
        Assert.Equal("A", rollup[0].MasterName); // more texts wins over more hits
        Assert.Equal(2, rollup[0].TextCount);
        Assert.Equal("B", rollup[1].MasterName);
    }

    [Fact]
    public void AttachMasterAttribution_NullIndex_YieldsNoRollupNoNames()
    {
        var texts = new List<DictEvidenceGroup> { new() { RelPath = "a.xml", HitCount = 1 } };

        var rollup = DictionaryEvidenceService.AttachMasterAttribution(texts, null);

        Assert.Empty(rollup);
        Assert.Null(texts[0].MasterName);
    }
}
