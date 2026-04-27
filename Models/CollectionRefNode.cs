namespace ReadZen.App.Models;

public sealed class CollectionRefNode
{
    public string CollectionId { get; set; } = "";
    public string CollectionName { get; set; } = "";
    public bool IsShared { get; set; }
    public string? OwnerUsername { get; set; }
}
