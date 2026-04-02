namespace CbetaTranslator.App.Models;

public class GraphEdge
{
    public GraphNode From { get; set; } = null!;
    public GraphNode To { get; set; } = null!;
    public string RelationType { get; set; } = "";
}
