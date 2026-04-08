using ReadZen.App.Models;
using Xunit;

namespace ReadZen.Tests.Models;

public class CorpusUsageHitTests
{
    [Fact]
    public void DateDisplay_PositiveDate_FormatsAsCE()
    {
        var hit = new CorpusUsageHit { ApproximateDate = 400 };

        Assert.Equal("~400 CE", hit.DateDisplay);
    }

    [Fact]
    public void DateDisplay_ZeroDate_ReturnsEmpty()
    {
        var hit = new CorpusUsageHit { ApproximateDate = 0 };

        Assert.Equal("", hit.DateDisplay);
    }

    [Fact]
    public void DateDisplay_NegativeDate_ReturnsEmpty()
    {
        var hit = new CorpusUsageHit { ApproximateDate = -100 };

        Assert.Equal("", hit.DateDisplay);
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var hit = new CorpusUsageHit();

        Assert.Equal("", hit.ZhSnippet);
        Assert.Equal("", hit.SourceRelPath);
        Assert.Equal("", hit.DisplayName);
        Assert.Equal("", hit.MasterName);
        Assert.Equal(0, hit.ApproximateDate);
        Assert.Equal("", hit.DateDisplay);
    }
}
