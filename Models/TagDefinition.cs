using System;
using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

/// <summary>
/// A tag definition in the user's coding vocabulary.
/// Supports hierarchy via ParentId.
/// </summary>
public sealed class TagDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? ParentId { get; set; }
    public string Color { get; set; } = "#3498DB";   // default blue
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }

    [JsonIgnore]
    public string DisplayName => string.IsNullOrEmpty(Name) ? Id : Name;
}
