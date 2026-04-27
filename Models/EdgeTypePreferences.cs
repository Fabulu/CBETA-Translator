using System.Collections.Generic;

namespace ReadZen.App.Models;

public sealed class EdgeTypePreferences
{
    public Dictionary<string, string> LastUsedEdgeType { get; set; } = new();

    public string? GetLastUsed(ScholarNodeType from, ScholarNodeType to)
    {
        var key = $"{(int)from}:{(int)to}";
        return LastUsedEdgeType.GetValueOrDefault(key);
    }

    public void SetLastUsed(ScholarNodeType from, ScholarNodeType to, string edgeTypeId)
    {
        var key = $"{(int)from}:{(int)to}";
        LastUsedEdgeType[key] = edgeTypeId;
    }
}
