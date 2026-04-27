using System.Collections.Generic;
using System.Linq;

namespace ReadZen.App.Models;

public static class EdgeTypeRegistry
{
    public static IReadOnlyList<EdgeTypeDefinition> BuiltInTypes { get; } = new List<EdgeTypeDefinition>
    {
        // Passage → Passage
        new() { Id = "quotes", DisplayName = "Quotes", Description = "Direct quotation", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "alludes-to", DisplayName = "Alludes to", Description = "Indirect reference", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "comments-on", DisplayName = "Comments on", Description = "Commentary or analysis", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "contradicts", DisplayName = "Contradicts", Description = "Opposing view", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "parallels", DisplayName = "Parallels", Description = "Similar content", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "responds-to", DisplayName = "Responds to", Description = "Reply or response", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "is-variant-of", DisplayName = "Is variant of", Description = "Alternate version", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "translates", DisplayName = "Translates", Description = "Translation of same source", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "summarizes", DisplayName = "Summarizes", Description = "Condensed version", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },

        // Passage → Concept
        new() { Id = "evidences", DisplayName = "Evidences", Description = "Passage supports this concept", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "refutes", DisplayName = "Refutes", Description = "Passage contradicts this concept", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },

        // Passage → Master
        new() { Id = "attributed-to", DisplayName = "Attributed to", Description = "Passage attributed to this master", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },

        // Passage → Term
        new() { Id = "uses-term", DisplayName = "Uses term", Description = "Passage uses this key term", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#81C784", IsBuiltIn = true, IsDirectional = true },

        // Concept → Concept
        new() { Id = "subsumes", DisplayName = "Subsumes", Description = "Broader concept includes this one", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "opposes", DisplayName = "Opposes", Description = "Concepts are in tension", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "related-to", DisplayName = "Related to", Description = "Conceptually connected", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = false },

        // Concept → Master
        new() { Id = "taught-by", DisplayName = "Taught by", Description = "Concept associated with this master's teaching", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },

        // Concept → Term
        new() { Id = "defined-by", DisplayName = "Defined by", Description = "Concept expressed through this term", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#81C784", IsBuiltIn = true, IsDirectional = true },

        // Master → Master
        new() { Id = "teacher-of", DisplayName = "Teacher of", Description = "Lineage transmission", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "same-school", DisplayName = "Same school", Description = "Members of same school", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = false },

        // Collection → Collection
        new() { Id = "cross-ref", DisplayName = "Cross-reference", Description = "Collections reference each other", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = false },
    }.AsReadOnly();

    public static IReadOnlyList<EdgeTypeDefinition> GetValidTypes(ScholarNodeType fromType, ScholarNodeType toType)
    {
        return BuiltInTypes.Where(t =>
            (t.AllowedFromTypes.Count == 0 || t.AllowedFromTypes.Contains(fromType)) &&
            (t.AllowedToTypes.Count == 0 || t.AllowedToTypes.Contains(toType))
        ).ToList();
    }

    public static EdgeTypeDefinition? GetById(string id)
    {
        return BuiltInTypes.FirstOrDefault(t => t.Id == id);
    }

    public static EdgeTypeDefinition? GetDefault(ScholarNodeType fromType, ScholarNodeType toType)
    {
        return GetValidTypes(fromType, toType).FirstOrDefault();
    }
}
