// Models/LineageGraphNode.cs
// Graph nodes and edges for the Zen lineage web visualization.

namespace ReadZen.App.Models;

public sealed class LineageGraphNode
{
    public string CanonicalName { get; set; } = "";
    public string ChineseName { get; set; } = "";
    public string DatesSummary { get; set; } = "";
    public string? School { get; set; }
    public string? Notes { get; set; }
    public int Layer { get; set; }
    public int SortDate { get; set; } // death or floruit for Y placement
    public double X { get; set; }
    public double Y { get; set; }
    public bool IsSelected { get; set; }
    public bool IsHighlighted { get; set; }
    public bool IsHidden { get; set; }
    public bool IsOrphan { get; set; }
    public ZenMasterRecord? Record { get; set; }
}

public sealed class LineageEdge
{
    public LineageGraphNode From { get; set; } = null!;
    public LineageGraphNode To { get; set; } = null!;
}
