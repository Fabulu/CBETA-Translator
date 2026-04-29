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

    /// <summary>Passage importance (0-5). Only relevant for TreeNodeKind.Passage.</summary>
    public int Importance { get; set; }

    /// <summary>Reading status string (e.g. "read", "skimmed", "unread").</summary>
    public string? ReadingStatus { get; set; }

    /// <summary>Returns filled/empty star glyphs for the importance rating (e.g. "★★★☆☆").</summary>
    public string ImportanceStars =>
        Kind == TreeNodeKind.Passage && Importance > 0
            ? new string('\u2605', Importance) + new string('\u2606', 5 - Importance)
            : "";

    /// <summary>Returns a color hex string based on reading status.</summary>
    public string StatusDotColor => ReadingStatus switch
    {
        "read" => "#4CAF50",
        "skimmed" => "#FFC107",
        _ => "#9E9E9E"
    };
}
