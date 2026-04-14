using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Executes boolean queries over document tags and serializes/deserializes saved queries.
/// </summary>
public static class TagQueryService
{
    /// <summary>
    /// Executes a tag query against applied tags.
    /// AND: files where both CodeA and CodeB appear.
    /// OR: files where CodeA or CodeB (or both) appear.
    /// NOT: files where CodeA appears but CodeB does not.
    /// </summary>
    public static List<TagQueryMatch> Execute(TagQuery query, List<DocumentTag> tags)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (tags == null) throw new ArgumentNullException(nameof(tags));

        var byFile = tags.GroupBy(t => t.RelPath, StringComparer.OrdinalIgnoreCase);
        var results = new List<TagQueryMatch>();

        foreach (var fileGroup in byFile)
        {
            var fileTags = fileGroup.ToList();
            var aPresent = fileTags.Where(t => string.Equals(t.TagId, query.CodeA, StringComparison.Ordinal)).ToList();
            var bPresent = fileTags.Where(t => string.Equals(t.TagId, query.CodeB, StringComparison.Ordinal)).ToList();

            bool hasA = aPresent.Count > 0;
            bool hasB = bPresent.Count > 0;

            bool match = query.Operator switch
            {
                TagQueryOperator.And => hasA && hasB,
                TagQueryOperator.Or => hasA || hasB,
                TagQueryOperator.Not => hasA && !hasB,
                _ => false
            };

            if (!match) continue;

            // Collect matched tags for reporting
            var matchedIds = new List<string>();
            if (hasA) matchedIds.Add(query.CodeA);
            if (hasB) matchedIds.Add(query.CodeB);

            // Report the first tag's range as representative
            var representative = aPresent.FirstOrDefault() ?? bPresent.FirstOrDefault();
            if (representative != null)
            {
                results.Add(new TagQueryMatch(
                    fileGroup.Key,
                    representative.FromLb,
                    representative.ToLb,
                    matchedIds));
            }
        }

        return results;
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Serializes a list of saved queries to JSON.
    /// </summary>
    public static string SerializeQueries(List<TagQuery> queries)
    {
        return JsonSerializer.Serialize(queries, JsonOpts);
    }

    /// <summary>
    /// Deserializes a list of saved queries from JSON.
    /// </summary>
    public static List<TagQuery> DeserializeQueries(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<TagQuery>();

        try
        {
            return JsonSerializer.Deserialize<List<TagQuery>>(json, JsonOpts) ?? new List<TagQuery>();
        }
        catch
        {
            return new List<TagQuery>();
        }
    }
}
