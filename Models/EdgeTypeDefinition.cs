using System.Collections.Generic;

namespace ReadZen.App.Models;

public sealed class EdgeTypeDefinition
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public List<ScholarNodeType> AllowedFromTypes { get; set; } = new();
    public List<ScholarNodeType> AllowedToTypes { get; set; } = new();
    public string ColorHex { get; set; } = "#9E9E9E";
    public bool IsBuiltIn { get; set; }
    public bool IsDirectional { get; set; } = true;
}
