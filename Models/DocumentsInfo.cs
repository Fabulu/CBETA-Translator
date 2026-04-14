// Models/DocumentsInfo.cs
// Supplementary document catalogue from documents.json.
// Deserialized with System.Text.Json using JsonPropertyName attributes.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

public sealed class DocumentsInfo
{
    [JsonPropertyName("documents")]
    public List<DocumentEntry>? Documents { get; set; }
}

public sealed class DocumentEntry
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("sort_order")]
    public int? SortOrder { get; set; }
}
