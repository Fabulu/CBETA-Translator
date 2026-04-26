using ReadZen.App.Models;

namespace ReadZen.App.ViewModels;

public class BacklinkItem
{
    public ScholarPassage Passage { get; set; } = null!;
    public string RelationType { get; set; } = "";
    public string Display => $"{RelationIcon} {Passage.DisplayTitle}";

    public string RelationIcon => RelationType switch
    {
        "quotes" => "\u275D",
        "alludes-to" => "\u2248",
        "comments-on" => "\uD83D\uDCAC",
        "contradicts" => "\u2717",
        "parallels" => "\u2261",
        "responds-to" => "\u21A9",
        "is-variant-of" => "\u2243",
        "translates" => "\u21C4",
        "summarizes" => "\u2211",
        _ => "\u2194"
    };
}
