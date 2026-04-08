using System.Collections.Generic;

namespace ReadZen.App.Models;

public sealed class NavTreeNode
{
    public string Header { get; set; } = "";
    public string NodeType { get; set; } = "group"; // group | subgroup
    public string TintBrush { get; set; } = "#150078D4";
    public List<object> Children { get; set; } = new();
}
