using System;
using CbetaTranslator.App.Models;
using Xunit;

namespace CbetaTranslator.Tests.Models;

public class PassageLinkTests
{
    [Fact]
    public void RelationTypes_ContainsExpectedValues()
    {
        var types = PassageLink.RelationTypes;

        Assert.Contains("quotes", types);
        Assert.Contains("alludes-to", types);
        Assert.Contains("comments-on", types);
        Assert.Contains("contradicts", types);
        Assert.Contains("parallels", types);
        Assert.Contains("responds-to", types);
    }

    [Fact]
    public void RelationTypes_HasExactlySixEntries()
    {
        Assert.Equal(6, PassageLink.RelationTypes.Length);
    }

    [Fact]
    public void RelationTypes_IsStatic_SameInstance()
    {
        var a = PassageLink.RelationTypes;
        var b = PassageLink.RelationTypes;
        Assert.Same(a, b);
    }

    [Fact]
    public void DefaultValues_AreEmptyStrings()
    {
        var link = new PassageLink();

        Assert.Equal("", link.Id);
        Assert.Equal("", link.FromPassageId);
        Assert.Equal("", link.ToPassageId);
        Assert.Equal("", link.RelationType);
        Assert.Null(link.Note);
        Assert.Equal(default(DateTimeOffset), link.CreatedUtc);
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        var now = DateTimeOffset.UtcNow;
        var link = new PassageLink
        {
            Id = "link1",
            FromPassageId = "p1",
            ToPassageId = "p2",
            RelationType = "quotes",
            Note = "A note",
            CreatedUtc = now
        };

        Assert.Equal("link1", link.Id);
        Assert.Equal("p1", link.FromPassageId);
        Assert.Equal("p2", link.ToPassageId);
        Assert.Equal("quotes", link.RelationType);
        Assert.Equal("A note", link.Note);
        Assert.Equal(now, link.CreatedUtc);
    }
}
