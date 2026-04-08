using ReadZen.App.Models;
using Xunit;

namespace ReadZen.Tests.Models;

public class GraphEdgeTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var edge = new GraphEdge();

        Assert.Equal("", edge.RelationType);
        // From and To are null! by default (uninitialized required refs)
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        var from = new GraphNode { PassageId = "p1" };
        var to = new GraphNode { PassageId = "p2" };

        var edge = new GraphEdge
        {
            From = from,
            To = to,
            RelationType = "quotes"
        };

        Assert.Same(from, edge.From);
        Assert.Same(to, edge.To);
        Assert.Equal("quotes", edge.RelationType);
    }
}
