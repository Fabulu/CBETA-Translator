using System;

namespace ReadZen.App.Models;

public sealed class ScholarGraphEdge
{
    public string Id { get; set; } = "";
    public string FromNodeId { get; set; } = "";
    public ScholarNodeType FromNodeType { get; set; }
    public string ToNodeId { get; set; } = "";
    public ScholarNodeType ToNodeType { get; set; }
    public string RelationType { get; set; } = "";
    public string? EdgeLabel { get; set; }
    public string? Note { get; set; }
    public double Weight { get; set; } = 1.0;
    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? ModifiedUtc { get; set; }
}
