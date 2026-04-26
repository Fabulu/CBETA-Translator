using System.Collections.ObjectModel;

namespace ReadZen.App.ViewModels;

public enum TreeNodeKind
{
    Collection,
    Passage
}

public class CollectionTreeNode
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public TreeNodeKind Kind { get; set; }
    public int ItemCount { get; set; }
    public bool IsExpanded { get; set; }
    public object? Tag { get; set; }
    public ObservableCollection<CollectionTreeNode> Children { get; } = new();
}
