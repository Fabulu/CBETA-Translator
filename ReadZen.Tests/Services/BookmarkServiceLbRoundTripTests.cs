// BookmarkServiceLbRoundTripTests — R3.2: bookmarks persist their lb re-anchor fields
// (LbAnchor/Side/IntraLineOffset) through the bookmarks.json sidecar so navigation can
// survive re-rendering and page<->merged-flow layout toggles across sessions.

using System;
using System.IO;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

[Trait("Domain", "Reader")]
public class BookmarkServiceLbRoundTripTests : IDisposable
{
    private readonly string _tmp;

    public BookmarkServiceLbRoundTripTests()
    {
        _tmp = Path.Combine(Path.GetTempPath(), "bookmarks-" + Guid.NewGuid().ToString("N") + ".json");
    }

    public void Dispose()
    {
        try { if (File.Exists(_tmp)) File.Delete(_tmp); } catch { }
        try { if (File.Exists(_tmp + ".tmp")) File.Delete(_tmp + ".tmp"); } catch { }
    }

    [Fact]
    public void Add_PersistsLbAnchorFields_AcrossInstances()
    {
        var svc = new BookmarkService(_tmp);
        svc.Add(new Bookmark
        {
            RelPath = "T/T2076_.xml",
            DisplayOffset = 500,
            Label = "line",
            LbAnchor = "0526c25",
            Side = "tran",
            IntraLineOffset = 7
        });

        // A fresh instance re-reads the sidecar (new session).
        var svc2 = new BookmarkService(_tmp);
        var bm = svc2.All().Single();

        Assert.Equal("0526c25", bm.LbAnchor);
        Assert.Equal("tran", bm.Side);
        Assert.Equal(7, bm.IntraLineOffset);
        Assert.Equal(500, bm.DisplayOffset);
    }

    [Fact]
    public void LegacyOffsetOnlyBookmark_StillPersists_WithNullAnchor()
    {
        var svc = new BookmarkService(_tmp);
        svc.Add(new Bookmark { RelPath = "T/T2076_.xml", DisplayOffset = 42, Label = "legacy" });

        var bm = new BookmarkService(_tmp).All().Single();

        Assert.Null(bm.LbAnchor);
        Assert.Null(bm.Side);
        Assert.Null(bm.IntraLineOffset);
        Assert.Equal(42, bm.DisplayOffset);
    }

    [Fact]
    public void Remove_MatchesByRelPathOffsetAndCreated()
    {
        var created = DateTime.UtcNow;
        var svc = new BookmarkService(_tmp);
        svc.Add(new Bookmark { RelPath = "A.xml", DisplayOffset = 10, CreatedUtc = created, LbAnchor = "0001a01" });
        svc.Add(new Bookmark { RelPath = "A.xml", DisplayOffset = 20, CreatedUtc = created });

        svc.Remove(new Bookmark { RelPath = "A.xml", DisplayOffset = 10, CreatedUtc = created });

        var remaining = new BookmarkService(_tmp).All().Single();
        Assert.Equal(20, remaining.DisplayOffset);
    }
}
