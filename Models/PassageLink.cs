using System;

namespace CbetaTranslator.App.Models;

public sealed class PassageLink
{
    public string Id { get; set; } = "";
    public string FromPassageId { get; set; } = "";
    public string ToPassageId { get; set; } = "";
    public string RelationType { get; set; } = ""; // quotes, alludes-to, comments-on, contradicts, parallels, responds-to
    public string? Note { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }

    public static string[] RelationTypes { get; } =
        { "quotes", "alludes-to", "comments-on", "contradicts", "parallels", "responds-to" };
}
