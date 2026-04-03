using CbetaTranslator.App.Models;
using Xunit;

namespace CbetaTranslator.Tests.Models;

public class GraphNodeTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var node = new GraphNode();

        Assert.Equal("", node.PassageId);
        Assert.Equal("", node.Label);
        Assert.Equal(0.0, node.X);
        Assert.Equal(0.0, node.Y);
        Assert.Equal(0.0, node.Vx);
        Assert.Equal(0.0, node.Vy);
        Assert.False(node.IsSelected);
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        var node = new GraphNode
        {
            PassageId = "p1",
            Label = "Test Label",
            X = 100.5,
            Y = 200.3,
            Vx = 1.5,
            Vy = -2.7,
            IsSelected = true
        };

        Assert.Equal("p1", node.PassageId);
        Assert.Equal("Test Label", node.Label);
        Assert.Equal(100.5, node.X);
        Assert.Equal(200.3, node.Y);
        Assert.Equal(1.5, node.Vx);
        Assert.Equal(-2.7, node.Vy);
        Assert.True(node.IsSelected);
    }
}
