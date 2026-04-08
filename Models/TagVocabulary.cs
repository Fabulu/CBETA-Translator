using System.Collections.Generic;

namespace ReadZen.App.Models;

/// <summary>
/// A user's complete tag vocabulary: definitions + code bar page assignments.
/// Stored as a single JSON file per user.
/// </summary>
public sealed class TagVocabulary
{
    public List<TagDefinition> Tags { get; set; } = new();

    /// <summary>
    /// Code bar pages: key = page number (1-18), value = array of 9 tag IDs (null = empty slot).
    /// </summary>
    public Dictionary<int, string?[]> Pages { get; set; } = new();
}
