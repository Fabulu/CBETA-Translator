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

        // Passage → ZenMaster (additional)
        new() { Id = "spoken-by", DisplayName = "Spoken by", Description = "Words spoken by this master", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "passage-references-master", DisplayName = "References", Description = "Passage references this master", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "influenced-by", DisplayName = "Influenced by", Description = "Passage influenced by this master", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },

        // Passage → Collection
        new() { Id = "belongs-to", DisplayName = "Belongs to", Description = "Passage belongs to this collection", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "passage-references-collection", DisplayName = "References", Description = "Passage references this collection", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "excerpted-from", DisplayName = "Excerpted from", Description = "Passage excerpted from this collection", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },

        // Concept → ZenMaster (additional)
        new() { Id = "formulated-by", DisplayName = "Formulated by", Description = "Concept formulated by this master", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "concept-associated-with-master", DisplayName = "Associated with", Description = "Concept associated with this master", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "rejected-by", DisplayName = "Rejected by", Description = "Concept rejected by this master", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },

        // Concept → Collection
        new() { Id = "featured-in-concept-collection", DisplayName = "Featured in", Description = "Concept featured in this collection", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "central-to", DisplayName = "Central to", Description = "Concept central to this collection", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "concept-references-collection", DisplayName = "References", Description = "Concept references this collection", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },

        // ZenMaster → Passage
        new() { Id = "authored", DisplayName = "Authored", Description = "Master authored this passage", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "master-commented-on", DisplayName = "Commented on", Description = "Master commented on this passage", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "endorsed", DisplayName = "Endorsed", Description = "Master endorsed this passage", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },

        // ZenMaster → Concept
        new() { Id = "taught", DisplayName = "Taught", Description = "Master taught this concept", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "developed", DisplayName = "Developed", Description = "Master developed this concept", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "opposed", DisplayName = "Opposed", Description = "Master opposed this concept", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },

        // ZenMaster → Term
        new() { Id = "coined", DisplayName = "Coined", Description = "Master coined this term", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "master-defined", DisplayName = "Defined", Description = "Master defined this term", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "popularized", DisplayName = "Popularized", Description = "Master popularized this term", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },

        // ZenMaster → Collection
        new() { Id = "subject-of", DisplayName = "Subject of", Description = "Master is the subject of this collection", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "master-featured-in", DisplayName = "Featured in", Description = "Master featured in this collection", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "preserved-in", DisplayName = "Preserved in", Description = "Master's teachings preserved in this collection", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },

        // Term → Passage
        new() { Id = "used-in", DisplayName = "Used in", Description = "Term used in this passage", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "defined-in-passage", DisplayName = "Defined in", Description = "Term defined in this passage", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "exemplified-in", DisplayName = "Exemplified in", Description = "Term exemplified in this passage", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },

        // Term → Concept
        new() { Id = "expresses", DisplayName = "Expresses", Description = "Term expresses this concept", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "exemplifies", DisplayName = "Exemplifies", Description = "Term exemplifies this concept", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "term-defines-concept", DisplayName = "Defines", Description = "Term defines this concept", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },

        // Term → ZenMaster
        new() { Id = "coined-by", DisplayName = "Coined by", Description = "Term coined by this master", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "term-associated-with", DisplayName = "Associated with", Description = "Term associated with this master", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "term-defined-by", DisplayName = "Defined by", Description = "Term defined by this master", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },

        // Term → Term
        new() { Id = "synonym-of", DisplayName = "Synonym of", Description = "Terms are synonymous", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "antonym-of", DisplayName = "Antonym of", Description = "Terms are antonyms", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "term-related-to", DisplayName = "Related to", Description = "Terms are related", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "variant-of", DisplayName = "Variant of", Description = "Alternate form of same term", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = false },

        // Term → Collection
        new() { Id = "term-featured-in", DisplayName = "Featured in", Description = "Term featured in this collection", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "defined-in-collection", DisplayName = "Defined in", Description = "Term defined in this collection", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },

        // Collection → Passage
        new() { Id = "contains", DisplayName = "Contains", Description = "Collection contains this passage", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "collection-references-passage", DisplayName = "References", Description = "Collection references this passage", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "complements-passage", DisplayName = "Complements", Description = "Collection complements this passage", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },

        // Collection → Concept
        new() { Id = "explores", DisplayName = "Explores", Description = "Collection explores this concept", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "develops", DisplayName = "Develops", Description = "Collection develops this concept", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "collection-features-concept", DisplayName = "Features", Description = "Collection features this concept", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },

        // Collection → ZenMaster
        new() { Id = "collection-features-master", DisplayName = "Features", Description = "Collection features this master", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "about", DisplayName = "About", Description = "Collection is about this master", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "preserves", DisplayName = "Preserves", Description = "Collection preserves this master's work", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },

        // Collection → Term
        new() { Id = "collection-defines", DisplayName = "Defines", Description = "Collection defines this term", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "collection-features-term", DisplayName = "Features", Description = "Collection features this term", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "introduces", DisplayName = "Introduces", Description = "Collection introduces this term", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },

        // Collection → Collection (additional)
        new() { Id = "cross-ref", DisplayName = "Cross-reference", Description = "Collections reference each other", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "builds-on", DisplayName = "Builds on", Description = "Collection builds on another", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "complements-collection", DisplayName = "Complements", Description = "Collections complement each other", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "contrasts-with", DisplayName = "Contrasts with", Description = "Collections present contrasting views", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = false },

        // Passage → Book
        new() { Id = "excerpted-from-book", DisplayName = "Excerpted from", Description = "Passage excerpted from this book", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "appears-in", DisplayName = "Appears in", Description = "Passage appears in this book", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },

        // Book → Passage
        new() { Id = "book-contains", DisplayName = "Contains", Description = "Book contains this passage", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },

        // Book → Concept
        new() { Id = "book-explores", DisplayName = "Explores", Description = "Book explores this concept", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },

        // Book → ZenMaster
        new() { Id = "book-attributed-to", DisplayName = "Attributed to", Description = "Book attributed to this master", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-records", DisplayName = "Records", Description = "Book records this master's teachings", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },

        // Book → Book
        new() { Id = "related-book", DisplayName = "Related to", Description = "Books are related", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "commentary-on-book", DisplayName = "Commentary on", Description = "Book is a commentary on another", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },

        // Book → Collection
        new() { Id = "book-in-collection", DisplayName = "In collection", Description = "Book included in this collection", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },

        // ZenMaster → Book
        new() { Id = "master-authored-book", DisplayName = "Authored", Description = "Master authored this book", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },
    }.AsReadOnly();

    public static IReadOnlyList<EdgeTypeDefinition> GetValidTypes(
        ScholarNodeType fromType, ScholarNodeType toType,
        IEnumerable<EdgeTypeDefinition>? customTypes = null)
    {
        var results = BuiltInTypes.Where(t =>
            (t.AllowedFromTypes.Count == 0 || t.AllowedFromTypes.Contains(fromType)) &&
            (t.AllowedToTypes.Count == 0 || t.AllowedToTypes.Contains(toType))
        ).ToList();

        if (customTypes != null)
        {
            foreach (var ct in customTypes)
            {
                if ((ct.AllowedFromTypes.Count == 0 || ct.AllowedFromTypes.Contains(fromType)) &&
                    (ct.AllowedToTypes.Count == 0 || ct.AllowedToTypes.Contains(toType)))
                {
                    results.Add(ct);
                }
            }
        }

        return results;
    }

    public static EdgeTypeDefinition? GetById(string id, IEnumerable<EdgeTypeDefinition>? customTypes = null)
    {
        var result = BuiltInTypes.FirstOrDefault(t => t.Id == id);
        if (result == null && customTypes != null)
            result = customTypes.FirstOrDefault(t => t.Id == id);
        return result;
    }

    public static EdgeTypeDefinition? GetDefault(ScholarNodeType fromType, ScholarNodeType toType)
    {
        return GetValidTypes(fromType, toType).FirstOrDefault();
    }
}
