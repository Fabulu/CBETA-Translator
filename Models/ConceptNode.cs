using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

public enum ConceptStatus
{
    Active = 0,
    Deprecated = 1,
    MergedInto = 2
}

public sealed class ConceptNode
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string ColorHex { get; set; } = "#FF8A65";
    public List<string> Tags { get; set; } = new();
    public ConceptStatus Status { get; set; } = ConceptStatus.Active;
    public string? MergedIntoConceptId { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? ModifiedUtc { get; set; }

    [JsonIgnore]
    public string DisplayTitle => !string.IsNullOrWhiteSpace(Name) ? Name : "(unnamed concept)";
}
