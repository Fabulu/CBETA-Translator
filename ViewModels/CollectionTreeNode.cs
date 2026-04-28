using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ReadZen.App.ViewModels;

public enum TreeNodeKind
{
    Collection,
    Passage
}

public partial class CollectionTreeNode : ObservableObject
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public TreeNodeKind Kind { get; set; }
    public int ItemCount { get; set; }
    [ObservableProperty] private bool _isExpanded;
    public object? Tag { get; set; }
    public ObservableCollection<CollectionTreeNode> Children { get; } = new();
}
