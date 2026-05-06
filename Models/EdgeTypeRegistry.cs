using System.Collections.Generic;
using System.Linq;

namespace ReadZen.App.Models;

public static class EdgeTypeRegistry
{
    public static IReadOnlyList<EdgeTypeDefinition> BuiltInTypes { get; } = new List<EdgeTypeDefinition>
    {
        // ================================================================
        // Passage → Passage (9)
        // ================================================================
        new() { Id = "quotes", DisplayName = "Quotes", Description = "Direct quotation", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "alludes-to", DisplayName = "Alludes to", Description = "Indirect reference", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "comments-on", DisplayName = "Comments on", Description = "Commentary or analysis", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "contradicts", DisplayName = "Contradicts", Description = "Opposing view", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "parallels", DisplayName = "Parallels", Description = "Similar content", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "responds-to", DisplayName = "Responds to", Description = "Reply or response", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "is-variant-of", DisplayName = "Is variant of", Description = "Alternate version", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "translates", DisplayName = "Translates", Description = "Translation of same source", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "summarizes", DisplayName = "Summarizes", Description = "Condensed version", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Passage → Concept (7)
        // ================================================================
        new() { Id = "evidences", DisplayName = "Evidences", Description = "Passage supports this concept", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "refutes", DisplayName = "Refutes", Description = "Passage contradicts this concept", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "illustrates", DisplayName = "Illustrates", Description = "Passage provides concrete example of concept", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "introduces-concept", DisplayName = "Introduces", Description = "Passage is where concept first appears", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "presupposes", DisplayName = "Presupposes", Description = "Passage assumes this concept", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "redefines-concept", DisplayName = "Redefines", Description = "Passage reinterprets the concept", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "questions", DisplayName = "Questions", Description = "Passage raises doubt about concept", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Passage → ZenMaster (7)
        // ================================================================
        new() { Id = "attributed-to", DisplayName = "Attributed to", Description = "Passage attributed to this master", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "spoken-by", DisplayName = "Spoken by", Description = "Words spoken by this master", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "passage-references-master", DisplayName = "References", Description = "Passage references this master", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "influenced-by", DisplayName = "Influenced by", Description = "Passage influenced by this master", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "criticizes", DisplayName = "Criticizes", Description = "Passage criticizes master's position", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "records-encounter-with", DisplayName = "Records encounter with", Description = "Passage records meeting or dialogue with master", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "received-from", DisplayName = "Received from", Description = "Passage transmitted from master", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Passage → TermbaseEntry (7)
        // ================================================================
        new() { Id = "uses-term", DisplayName = "Uses term", Description = "Passage uses this key term", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#81C784", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "defines-term", DisplayName = "Defines term", Description = "Passage provides definition of term", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "coins-term", DisplayName = "Coins term", Description = "Passage is earliest known use of term", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "redefines-term", DisplayName = "Redefines term", Description = "Passage gives new meaning to existing term", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "exemplifies-term", DisplayName = "Exemplifies term", Description = "Passage is paradigmatic example of term usage", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "contrasts-term", DisplayName = "Contrasts term", Description = "Passage contrasts this term with another meaning", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "transliterates-term", DisplayName = "Transliterates term", Description = "Passage contains transliteration of this term", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Passage → Collection (7)
        // ================================================================
        new() { Id = "belongs-to", DisplayName = "Belongs to", Description = "Passage belongs to this collection", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "passage-references-collection", DisplayName = "References", Description = "Passage references this collection", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "excerpted-from", DisplayName = "Excerpted from", Description = "Passage excerpted from this collection", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "central-passage-of", DisplayName = "Central passage of", Description = "Passage is key to this collection", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "introduces-collection", DisplayName = "Introduces", Description = "Passage serves as introduction to collection's theme", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "contradicts-thesis-of", DisplayName = "Contradicts thesis of", Description = "Passage challenges collection's main thesis", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "supplements", DisplayName = "Supplements", Description = "Passage adds supplementary material to collection", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Passage → Book (7)
        // ================================================================
        new() { Id = "excerpted-from-book", DisplayName = "Excerpted from", Description = "Passage excerpted from this book", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "appears-in", DisplayName = "Appears in", Description = "Passage appears in this book", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "prefaces-book", DisplayName = "Prefaces", Description = "Passage serves as preface to book", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "colophon-of", DisplayName = "Colophon of", Description = "Passage is the colophon of book", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "passage-commentary-on-book", DisplayName = "Commentary on", Description = "Passage is commentary on this book", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "cited-in-book", DisplayName = "Cited in", Description = "Passage is cited within book", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "translated-from-book", DisplayName = "Translated from", Description = "Passage is translated from this book", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Passage → Link (7)
        // ================================================================
        new() { Id = "passage-references-link", DisplayName = "References Link", Description = "Passage references this link", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "discussed-at", DisplayName = "Discussed at", Description = "Passage is discussed at this URL", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "digitized-at", DisplayName = "Digitized at", Description = "Passage has digital facsimile at link", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "translated-at", DisplayName = "Translated at", Description = "Passage has translation at link", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "annotated-at", DisplayName = "Annotated at", Description = "Passage has scholarly annotation at link", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "cataloged-at", DisplayName = "Cataloged at", Description = "Passage is cataloged at this link", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "parallel-at", DisplayName = "Parallel at", Description = "Parallel version of passage at link", AllowedFromTypes = new(){ScholarNodeType.Passage}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Concept → Passage (7)
        // ================================================================
        new() { Id = "exemplified-by", DisplayName = "Exemplified by", Description = "Concept is exemplified by this passage", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "defined-in", DisplayName = "Defined in", Description = "Concept is defined in this passage", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "challenged-by", DisplayName = "Challenged by", Description = "Concept is challenged by this passage", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "originated-in", DisplayName = "Originated in", Description = "Concept originates in this passage", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "elaborated-in", DisplayName = "Elaborated in", Description = "Concept is elaborated in this passage", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "applied-in", DisplayName = "Applied in", Description = "Concept is applied in this passage", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "negated-in", DisplayName = "Negated in", Description = "Concept is negated in this passage", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Concept → Concept (7)
        // ================================================================
        new() { Id = "subsumes", DisplayName = "Subsumes", Description = "Broader concept includes this one", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "opposes", DisplayName = "Opposes", Description = "Concepts are in tension", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "related-to", DisplayName = "Related to", Description = "Conceptually connected", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "precondition-of", DisplayName = "Precondition of", Description = "This concept is prerequisite for understanding target", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "evolves-into", DisplayName = "Evolves into", Description = "Concept historically developed into target concept", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "complements", DisplayName = "Complements", Description = "Concepts are complementary", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "refines", DisplayName = "Refines", Description = "This concept is a more precise version of target", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Concept → ZenMaster (7)
        // ================================================================
        new() { Id = "taught-by", DisplayName = "Taught by", Description = "Concept associated with this master's teaching", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "formulated-by", DisplayName = "Formulated by", Description = "Concept formulated by this master", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "concept-associated-with-master", DisplayName = "Associated with", Description = "Concept associated with this master", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "rejected-by", DisplayName = "Rejected by", Description = "Concept rejected by this master", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "transmitted-by", DisplayName = "Transmitted by", Description = "Concept transmitted through this master's lineage", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "reinterpreted-by", DisplayName = "Reinterpreted by", Description = "Concept reinterpreted by master", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "embodied-by", DisplayName = "Embodied by", Description = "Concept exemplified through master's life and practice", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Concept → TermbaseEntry (7)
        // ================================================================
        new() { Id = "defined-by", DisplayName = "Defined by", Description = "Concept expressed through this term", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#81C784", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "named-by", DisplayName = "Named by", Description = "Concept is named by this term", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "distinguished-by", DisplayName = "Distinguished by", Description = "Term distinguishes this concept from others", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "translated-as", DisplayName = "Translated as", Description = "Concept is translated using this term", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "abbreviated-as", DisplayName = "Abbreviated as", Description = "Concept is abbreviated as this term", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "misrepresented-by", DisplayName = "Misrepresented by", Description = "Term is a common mistranslation of concept", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "technical-term-for", DisplayName = "Technical term for", Description = "Term is the technical designation for concept", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Concept → Collection (7)
        // ================================================================
        new() { Id = "featured-in-concept-collection", DisplayName = "Featured in", Description = "Concept featured in this collection", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "central-to", DisplayName = "Central to", Description = "Concept central to this collection", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "concept-references-collection", DisplayName = "References", Description = "Concept references this collection", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "explored-in", DisplayName = "Explored in", Description = "Concept is explored in this collection", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "debated-in", DisplayName = "Debated in", Description = "Concept is debated across passages in collection", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "traced-in", DisplayName = "Traced in", Description = "Concept's historical development traced in collection", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "absent-from", DisplayName = "Absent from", Description = "Concept notably absent from collection", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Concept → Book (7)
        // ================================================================
        new() { Id = "treated-in", DisplayName = "Treated in", Description = "Concept is treated in this book", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "central-to-book", DisplayName = "Central to", Description = "Concept is central theme of book", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "originated-in-book", DisplayName = "Originated in", Description = "Concept first appears in this book", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "critiqued-in", DisplayName = "Critiqued in", Description = "Concept is critiqued in this book", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "systematized-in", DisplayName = "Systematized in", Description = "Concept is systematically organized in this book", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "concept-translated-in", DisplayName = "Translated in", Description = "Concept's key terms are translated in this book", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "absent-from-book", DisplayName = "Absent from", Description = "Concept notably absent from book", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Concept → Link (7)
        // ================================================================
        new() { Id = "concept-references-link", DisplayName = "References Link", Description = "Concept references this link", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "explained-at", DisplayName = "Explained at", Description = "Concept explained at this URL", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "debated-at", DisplayName = "Debated at", Description = "Concept is debated at this URL", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "defined-at", DisplayName = "Defined at", Description = "Concept is defined at this URL", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "visualized-at", DisplayName = "Visualized at", Description = "Concept has visualization at link", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "researched-at", DisplayName = "Researched at", Description = "Academic research on concept at link", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "encyclopedic-entry-at", DisplayName = "Encyclopedia entry at", Description = "Encyclopedia entry for concept at link", AllowedFromTypes = new(){ScholarNodeType.Concept}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // ZenMaster → Passage (7)
        // ================================================================
        new() { Id = "authored", DisplayName = "Authored", Description = "Master authored this passage", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "master-commented-on", DisplayName = "Commented on", Description = "Master commented on this passage", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "endorsed", DisplayName = "Endorsed", Description = "Master endorsed this passage", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "recited", DisplayName = "Recited", Description = "Master recited this passage", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "master-transmitted", DisplayName = "Transmitted", Description = "Master transmitted this passage to students", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "disputed", DisplayName = "Disputed", Description = "Master disputed this passage", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "anthologized", DisplayName = "Anthologized", Description = "Master included this passage in an anthology", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // ZenMaster → Concept (7)
        // ================================================================
        new() { Id = "taught", DisplayName = "Taught", Description = "Master taught this concept", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "developed", DisplayName = "Developed", Description = "Master developed this concept", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "opposed", DisplayName = "Opposed", Description = "Master opposed this concept", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "realized", DisplayName = "Realized", Description = "Master attained realization of concept", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "transmitted-concept", DisplayName = "Transmitted", Description = "Master transmitted concept to successor", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "systematized", DisplayName = "Systematized", Description = "Master organized concept into systematic framework", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "deconstructed", DisplayName = "Deconstructed", Description = "Master deconstructed concept through practice", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // ZenMaster → ZenMaster (7)
        // ================================================================
        new() { Id = "teacher-of", DisplayName = "Teacher of", Description = "Lineage transmission", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "same-school", DisplayName = "Same school", Description = "Members of same school", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "dharma-heir-of", DisplayName = "Dharma heir of", Description = "Received dharma transmission from", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "contemporary-of", DisplayName = "Contemporary of", Description = "Lived in same era", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "debated-with", DisplayName = "Debated", Description = "Masters engaged in debate or dialogue", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "succeeded", DisplayName = "Succeeded", Description = "Succeeded as abbot or leader", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "rivaled", DisplayName = "Rivaled", Description = "Masters were rivals or in doctrinal competition", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = false },

        // ================================================================
        // ZenMaster → TermbaseEntry (7)
        // ================================================================
        new() { Id = "coined", DisplayName = "Coined", Description = "Master coined this term", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "master-defined", DisplayName = "Defined", Description = "Master defined this term", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "popularized", DisplayName = "Popularized", Description = "Master popularized this term", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "repurposed", DisplayName = "Repurposed", Description = "Master gave new meaning to existing term", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "master-translated-term", DisplayName = "Translated", Description = "Master translated this term", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "master-avoided", DisplayName = "Avoided", Description = "Master deliberately avoided using this term", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "master-contested", DisplayName = "Contested", Description = "Master contested prevailing meaning of term", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // ZenMaster → Collection (7)
        // ================================================================
        new() { Id = "subject-of", DisplayName = "Subject of", Description = "Master is the subject of this collection", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "master-featured-in", DisplayName = "Featured in", Description = "Master featured in this collection", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "preserved-in", DisplayName = "Preserved in", Description = "Master's teachings preserved in this collection", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "compiled", DisplayName = "Compiled", Description = "Master compiled this collection", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "master-inspired", DisplayName = "Inspired", Description = "Master's teachings inspired this collection", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "curated", DisplayName = "Curated", Description = "Master curated this collection", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "critiqued-in-collection", DisplayName = "Critiqued in", Description = "Master is critiqued in this collection", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // ZenMaster → Book (7)
        // ================================================================
        new() { Id = "master-authored-book", DisplayName = "Authored", Description = "Master authored this book", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "recorded-in", DisplayName = "Recorded in", Description = "Master's sayings recorded in book", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "compiled-book", DisplayName = "Compiled", Description = "Master compiled this book", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "master-prefaced", DisplayName = "Prefaced", Description = "Master wrote preface for book", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "subject-of-book", DisplayName = "Subject of", Description = "Master is primary subject of book", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "master-translated-book", DisplayName = "Translated", Description = "Master translated this book", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "commissioned", DisplayName = "Commissioned", Description = "Master commissioned this book", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // ZenMaster → Link (7)
        // ================================================================
        new() { Id = "biography-at", DisplayName = "Biography at", Description = "Master's biography at this URL", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "portrait-at", DisplayName = "Portrait at", Description = "Master's portrait at link", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "lineage-chart-at", DisplayName = "Lineage chart at", Description = "Lineage chart including master at link", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "academic-study-at", DisplayName = "Academic study at", Description = "Academic study about master at link", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "temple-site-at", DisplayName = "Temple site at", Description = "Master's temple at link", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "inscription-at", DisplayName = "Inscription at", Description = "Master's stele or inscription at link", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "audio-teaching-at", DisplayName = "Audio teaching at", Description = "Audio recording of master at link", AllowedFromTypes = new(){ScholarNodeType.ZenMaster}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // TermbaseEntry → Passage (7)
        // ================================================================
        new() { Id = "used-in", DisplayName = "Used in", Description = "Term used in this passage", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "defined-in-passage", DisplayName = "Defined in", Description = "Term defined in this passage", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "exemplified-in", DisplayName = "Exemplified in", Description = "Term exemplified in this passage", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "first-attested-in", DisplayName = "First attested in", Description = "Earliest attestation of term in this passage", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "mistranslated-in", DisplayName = "Mistranslated in", Description = "Term is mistranslated in this passage", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "glossed-in", DisplayName = "Glossed in", Description = "Term is glossed in this passage", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "disputed-in", DisplayName = "Disputed in", Description = "Term's meaning is disputed in this passage", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // TermbaseEntry → Concept (7)
        // ================================================================
        new() { Id = "expresses", DisplayName = "Expresses", Description = "Term expresses this concept", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "exemplifies", DisplayName = "Exemplifies", Description = "Term exemplifies this concept", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "term-defines-concept", DisplayName = "Defines", Description = "Term defines this concept", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "denotes", DisplayName = "Denotes", Description = "Term technically denotes this concept", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "connotes", DisplayName = "Connotes", Description = "Term connotes or implies this concept", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "disambiguates", DisplayName = "Disambiguates", Description = "Term disambiguates between concepts", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "obscures", DisplayName = "Obscures", Description = "Term obscures the concept in translation", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // TermbaseEntry → ZenMaster (7)
        // ================================================================
        new() { Id = "coined-by", DisplayName = "Coined by", Description = "Term coined by this master", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "term-associated-with", DisplayName = "Associated with", Description = "Term associated with this master", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "term-defined-by", DisplayName = "Defined by", Description = "Term defined by this master", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "popularized-by", DisplayName = "Popularized by", Description = "Term popularized by master", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "contested-by", DisplayName = "Contested by", Description = "Term's meaning contested by master", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "inherited-from", DisplayName = "Inherited from", Description = "Term inherited from master's tradition", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "avoided-by", DisplayName = "Avoided by", Description = "Term deliberately avoided by master", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // TermbaseEntry → TermbaseEntry (7)
        // ================================================================
        new() { Id = "synonym-of", DisplayName = "Synonym of", Description = "Terms are synonymous", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "antonym-of", DisplayName = "Antonym of", Description = "Terms are antonyms", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "term-related-to", DisplayName = "Related to", Description = "Terms are related", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "variant-of", DisplayName = "Variant of", Description = "Alternate form of same term", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "hypernym-of", DisplayName = "Hypernym of", Description = "Broader term", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "derived-from", DisplayName = "Derived from", Description = "Etymologically derived from", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "often-confused-with", DisplayName = "Often confused with", Description = "Commonly confused terms", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = false },

        // ================================================================
        // TermbaseEntry → Collection (7)
        // ================================================================
        new() { Id = "term-featured-in", DisplayName = "Featured in", Description = "Term featured in this collection", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "defined-in-collection", DisplayName = "Defined in", Description = "Term defined in this collection", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "key-term-of", DisplayName = "Key term of", Description = "Term is a key term of collection", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "glossary-entry-in", DisplayName = "Glossary entry in", Description = "Term has glossary entry in collection", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "introduced-in-collection", DisplayName = "Introduced in", Description = "Term first introduced in this collection", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "debated-in-collection", DisplayName = "Debated in", Description = "Term's meaning debated in collection", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "absent-from-collection", DisplayName = "Absent from", Description = "Term notably absent from collection", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // TermbaseEntry → Book (7)
        // ================================================================
        new() { Id = "defined-in-book", DisplayName = "Defined in", Description = "Term is defined in this book", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "coined-in-book", DisplayName = "Coined in", Description = "Term first appears in this book", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "key-term-of-book", DisplayName = "Key term of", Description = "Term is a key term of this book", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "glossed-in-book", DisplayName = "Glossed in", Description = "Term has gloss in this book", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "translated-in-book", DisplayName = "Translated in", Description = "Term's translation given in this book", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "indexed-in", DisplayName = "Indexed in", Description = "Term is indexed in this book", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "analyzed-in-book", DisplayName = "Analyzed in", Description = "Term is linguistically analyzed in this book", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // TermbaseEntry → Link (7)
        // ================================================================
        new() { Id = "defined-at-link", DisplayName = "Defined at", Description = "Term defined at this URL", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "dictionary-entry-at", DisplayName = "Dictionary entry at", Description = "Dictionary entry for term at link", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "etymology-at", DisplayName = "Etymology at", Description = "Etymology of term at link", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "usage-examples-at", DisplayName = "Usage examples at", Description = "Usage examples at link", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "term-academic-discussion-at", DisplayName = "Academic discussion at", Description = "Academic discussion of term at link", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "translation-guide-at", DisplayName = "Translation guide at", Description = "Translation guide for term at link", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "corpus-search-at", DisplayName = "Corpus search at", Description = "Corpus search results for term at link", AllowedFromTypes = new(){ScholarNodeType.TermbaseEntry}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Collection → Passage (7)
        // ================================================================
        new() { Id = "contains", DisplayName = "Contains", Description = "Collection contains this passage", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "collection-references-passage", DisplayName = "References", Description = "Collection references this passage", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "complements-passage", DisplayName = "Complements", Description = "Collection complements this passage", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "highlights", DisplayName = "Highlights", Description = "Collection highlights this passage as key", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "contextualizes", DisplayName = "Contextualizes", Description = "Collection provides context for passage", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "contrasts-with-passage", DisplayName = "Contrasts with", Description = "Collection thesis contrasts with passage", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "opens-with", DisplayName = "Opens with", Description = "Collection opens with this passage", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Collection → Concept (7)
        // ================================================================
        new() { Id = "explores", DisplayName = "Explores", Description = "Collection explores this concept", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "develops", DisplayName = "Develops", Description = "Collection develops this concept", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "collection-features-concept", DisplayName = "Features", Description = "Collection features this concept", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "argues-for", DisplayName = "Argues for", Description = "Collection argues in favor of concept", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "argues-against", DisplayName = "Argues against", Description = "Collection argues against concept", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "surveys", DisplayName = "Surveys", Description = "Collection surveys this concept", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "collection-redefines-concept", DisplayName = "Redefines", Description = "Collection redefines concept", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Collection → ZenMaster (7)
        // ================================================================
        new() { Id = "collection-features-master", DisplayName = "Features", Description = "Collection features this master", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "about", DisplayName = "About", Description = "Collection is about this master", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "preserves", DisplayName = "Preserves", Description = "Collection preserves this master's work", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "critiques-master", DisplayName = "Critiques", Description = "Collection critiques master", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "compares-masters", DisplayName = "Compares", Description = "Collection compares this master with others", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "biographical-collection", DisplayName = "Biography of", Description = "Collection is biographical study of master", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "lineage-collection", DisplayName = "Lineage of", Description = "Collection traces master's lineage", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Collection → TermbaseEntry (7)
        // ================================================================
        new() { Id = "collection-defines", DisplayName = "Defines", Description = "Collection defines this term", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "collection-features-term", DisplayName = "Features", Description = "Collection features this term", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "introduces", DisplayName = "Introduces", Description = "Collection introduces this term", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "glossary-contains", DisplayName = "Glossary contains", Description = "Collection's glossary contains term", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "standardizes", DisplayName = "Standardizes", Description = "Collection standardizes translation of term", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "collection-debates-term", DisplayName = "Debates", Description = "Collection debates meaning of term", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "collection-avoids-term", DisplayName = "Avoids", Description = "Collection deliberately avoids term", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Collection → Collection (7)
        // ================================================================
        new() { Id = "cross-ref", DisplayName = "Cross-reference", Description = "Collections reference each other", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "builds-on", DisplayName = "Builds on", Description = "Collection builds on another", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "complements-collection", DisplayName = "Complements", Description = "Collections complement each other", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "contrasts-with", DisplayName = "Contrasts with", Description = "Collections present contrasting views", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "supersedes", DisplayName = "Supersedes", Description = "This collection supersedes target", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "derived-from-collection", DisplayName = "Derived from", Description = "This collection is derived from target", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "subcollection-of", DisplayName = "Subcollection of", Description = "This collection is a subset of target", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Collection → Book (7)
        // ================================================================
        new() { Id = "draws-from", DisplayName = "Draws from", Description = "Collection draws material from this book", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "collection-about-book", DisplayName = "About", Description = "Collection is about this book", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "indexes-book", DisplayName = "Indexes", Description = "Collection indexes passages from this book", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "reviews-book", DisplayName = "Reviews", Description = "Collection reviews this book", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "annotates-book", DisplayName = "Annotates", Description = "Collection provides annotations for this book", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "collection-translates-book", DisplayName = "Translates", Description = "Collection contains translations from this book", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "companion-to", DisplayName = "Companion to", Description = "Collection is a companion to this book", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Collection → Link (7)
        // ================================================================
        new() { Id = "collection-references-link", DisplayName = "References", Description = "Collection references this link", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "published-at", DisplayName = "Published at", Description = "Collection is published at this URL", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "discussed-at-link", DisplayName = "Discussed at", Description = "Collection is discussed at this URL", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "dataset-at", DisplayName = "Dataset at", Description = "Collection's dataset available at link", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "related-project-at", DisplayName = "Related project at", Description = "Related project at link", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "bibliography-at", DisplayName = "Bibliography at", Description = "Collection's bibliography at link", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "collection-visualization-at", DisplayName = "Visualization at", Description = "Collection visualization at link", AllowedFromTypes = new(){ScholarNodeType.Collection}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Book → Passage (7)
        // ================================================================
        new() { Id = "book-contains", DisplayName = "Contains", Description = "Book contains this passage", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-opens-with", DisplayName = "Opens with", Description = "Book opens with this passage", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-concludes-with", DisplayName = "Concludes with", Description = "Book concludes with this passage", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-quotes", DisplayName = "Quotes", Description = "Book quotes this passage from another source", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-comments-on", DisplayName = "Comments on", Description = "Book provides commentary on passage", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-preserves", DisplayName = "Preserves", Description = "Book preserves this passage as unique witness", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-abridges", DisplayName = "Abridges", Description = "Book contains abridged version of passage", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Book → Concept (7)
        // ================================================================
        new() { Id = "book-explores", DisplayName = "Explores", Description = "Book explores this concept", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-introduces", DisplayName = "Introduces", Description = "Book introduces this concept", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-systematizes", DisplayName = "Systematizes", Description = "Book systematizes this concept", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-refutes", DisplayName = "Refutes", Description = "Book refutes this concept", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#FF6B6B", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-defines", DisplayName = "Defines", Description = "Book defines this concept", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-applies", DisplayName = "Applies", Description = "Book applies this concept practically", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-presupposes", DisplayName = "Presupposes", Description = "Book presupposes this concept", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Book → ZenMaster (7)
        // ================================================================
        new() { Id = "book-attributed-to", DisplayName = "Attributed to", Description = "Book attributed to this master", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-records", DisplayName = "Records", Description = "Book records this master's teachings", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-compiled-by", DisplayName = "Compiled by", Description = "Book compiled by master", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-translated-by", DisplayName = "Translated by", Description = "Book translated by master", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-commissioned-by", DisplayName = "Commissioned by", Description = "Book commissioned by master", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-about-master", DisplayName = "About", Description = "Book is about this master", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-prefaced-by", DisplayName = "Prefaced by", Description = "Book prefaced by master", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Book → TermbaseEntry (7)
        // ================================================================
        new() { Id = "book-defines-term", DisplayName = "Defines", Description = "Book defines this term", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-coins-term", DisplayName = "Coins", Description = "Book is where this term first appears", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-glosses", DisplayName = "Glosses", Description = "Book provides gloss for this term", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-indexes-term", DisplayName = "Indexes", Description = "Book indexes this term", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-translates-term", DisplayName = "Translates", Description = "Book provides translation of term", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-standardizes", DisplayName = "Standardizes", Description = "Book standardizes usage of term", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-analyzes-term", DisplayName = "Analyzes", Description = "Book provides analysis of term", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Book → Collection (7)
        // ================================================================
        new() { Id = "book-in-collection", DisplayName = "In collection", Description = "Book included in this collection", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-source-for", DisplayName = "Source for", Description = "Book is primary source for collection", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-referenced-by", DisplayName = "Referenced by", Description = "Book referenced by collection", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-reviewed-in", DisplayName = "Reviewed in", Description = "Book reviewed in collection", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-annotated-in", DisplayName = "Annotated in", Description = "Book annotated in collection", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#C854D9", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-excerpted-in", DisplayName = "Excerpted in", Description = "Book has passages excerpted in collection", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-translated-in", DisplayName = "Translated in", Description = "Book translated in collection", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Book → Book (7)
        // ================================================================
        new() { Id = "related-book", DisplayName = "Related to", Description = "Books are related", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#D4A574", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "commentary-on-book", DisplayName = "Commentary on", Description = "Book is a commentary on another", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#51D996", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "translation-of", DisplayName = "Translation of", Description = "Translation of another book", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#64B5F6", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "abridgment-of", DisplayName = "Abridgment of", Description = "Abridged version of book", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#FFB347", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "sequel-to", DisplayName = "Sequel to", Description = "Sequel or continuation of book", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#FF8A65", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "anthology-includes", DisplayName = "Anthology includes", Description = "This anthology includes material from target", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#AB47BC", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "edition-of", DisplayName = "Edition of", Description = "Different edition of same work", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#59B3FF", IsBuiltIn = true, IsDirectional = false },

        // ================================================================
        // Book → Link (7)
        // ================================================================
        new() { Id = "book-digitized-at", DisplayName = "Digitized at", Description = "Book digitized at this URL", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-catalog-at", DisplayName = "Catalog at", Description = "Book's catalog entry at link", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-reviewed-at", DisplayName = "Reviewed at", Description = "Book reviewed at link", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-purchased-at", DisplayName = "Available at", Description = "Book available for purchase at link", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-discussed-at", DisplayName = "Discussed at", Description = "Book discussed at link", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-facsimile-at", DisplayName = "Facsimile at", Description = "Book's facsimile at link", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "book-bibliography-at", DisplayName = "Bibliography at", Description = "Book's bibliography at link", AllowedFromTypes = new(){ScholarNodeType.Book}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Link → Passage (7)
        // ================================================================
        new() { Id = "link-references-passage", DisplayName = "References", Description = "Link references this passage", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-translates-passage", DisplayName = "Translates", Description = "Link provides translation of passage", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-annotates-passage", DisplayName = "Annotates", Description = "Link provides annotation of passage", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-discusses-passage", DisplayName = "Discusses", Description = "Link discusses passage", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-digitizes-passage", DisplayName = "Digitizes", Description = "Link has digital facsimile of passage", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-contextualizes", DisplayName = "Contextualizes", Description = "Link provides context for passage", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-corrects", DisplayName = "Corrects", Description = "Link provides textual correction for passage", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Passage}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Link → Concept (7)
        // ================================================================
        new() { Id = "link-supports", DisplayName = "Supports", Description = "Link supports this concept", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-explains", DisplayName = "Explains", Description = "Link explains concept", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-defines-concept", DisplayName = "Defines", Description = "Link defines concept", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-debates", DisplayName = "Debates", Description = "Link debates concept", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-visualizes", DisplayName = "Visualizes", Description = "Link has visualization of concept", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-teaches", DisplayName = "Teaches", Description = "Link teaches concept", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-critiques", DisplayName = "Critiques", Description = "Link critiques concept", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Concept}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Link → ZenMaster (7)
        // ================================================================
        new() { Id = "link-about-master", DisplayName = "About", Description = "Link is about this master", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-biography", DisplayName = "Biography", Description = "Link has biography of master", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-portrait", DisplayName = "Portrait", Description = "Link has portrait of master", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-lineage-chart", DisplayName = "Lineage chart", Description = "Link has lineage chart including master", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-temple-site", DisplayName = "Temple site", Description = "Link is master's temple", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-inscription", DisplayName = "Inscription", Description = "Link has master's stele or inscription", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-academic-study", DisplayName = "Academic study", Description = "Link has academic study about master", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.ZenMaster}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Link → TermbaseEntry (7)
        // ================================================================
        new() { Id = "link-defines-term", DisplayName = "Defines", Description = "Link defines term", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-dictionary-entry", DisplayName = "Dictionary entry", Description = "Link is dictionary entry for term", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-etymology", DisplayName = "Etymology", Description = "Link has etymology of term", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-usage-examples", DisplayName = "Usage examples", Description = "Link has usage examples", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-corpus-search", DisplayName = "Corpus search", Description = "Link has corpus search for term", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-translation-guide", DisplayName = "Translation guide", Description = "Link has translation guide for term", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-academic-analysis", DisplayName = "Academic analysis", Description = "Link has academic analysis of term", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.TermbaseEntry}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Link → Collection (7)
        // ================================================================
        new() { Id = "link-references-collection", DisplayName = "References", Description = "Link references collection", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-publishes", DisplayName = "Publishes", Description = "Link publishes collection", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-reviews-collection", DisplayName = "Reviews", Description = "Link reviews collection", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-dataset-for", DisplayName = "Dataset for", Description = "Link has dataset for collection", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-discusses-collection", DisplayName = "Discusses", Description = "Link discusses collection", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-visualizes-collection", DisplayName = "Visualizes", Description = "Link visualizes collection", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-archives", DisplayName = "Archives", Description = "Link archives collection", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Collection}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Link → Book (7)
        // ================================================================
        new() { Id = "link-references-book", DisplayName = "References", Description = "Link references this text", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-digitizes-book", DisplayName = "Digitizes", Description = "Link has digital version of book", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-reviews-book", DisplayName = "Reviews", Description = "Link reviews book", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-catalogs-book", DisplayName = "Catalogs", Description = "Link catalogs book", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-facsimile", DisplayName = "Facsimile", Description = "Link has facsimile of book", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-discusses-book", DisplayName = "Discusses", Description = "Link discusses book", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "link-translates-book", DisplayName = "Translates", Description = "Link has translation of book", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Book}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },

        // ================================================================
        // Link → Link (7)
        // ================================================================
        new() { Id = "related-link", DisplayName = "Related to", Description = "Links are related", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "mirror-of", DisplayName = "Mirror of", Description = "Link is a mirror of target", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "superseded-by", DisplayName = "Superseded by", Description = "Link superseded by newer URL", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "archived-version-of", DisplayName = "Archived version of", Description = "Link is archived version of target", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "same-topic-as", DisplayName = "Same topic as", Description = "Links cover same topic", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = false },
        new() { Id = "translated-version-of", DisplayName = "Translated version of", Description = "Link is translated version of target", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
        new() { Id = "responds-to-link", DisplayName = "Responds to", Description = "Link responds to target", AllowedFromTypes = new(){ScholarNodeType.Link}, AllowedToTypes = new(){ScholarNodeType.Link}, ColorHex = "#78909C", IsBuiltIn = true, IsDirectional = true },
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
