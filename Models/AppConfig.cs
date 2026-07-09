using System.Collections.Generic;

namespace ReadZen.App.Models
{
    public sealed class AppConfig
    {
        public string? TextRootPath { get; set; }
        public string? LastSelectedRelPath { get; set; }

        public bool IsDarkTheme { get; set; } = true;
        public bool ZenOnly { get; set; }

        public bool EnableHoverDictionary { get; set; } = true;

        /// <summary>
        /// Bilingual scroll sync in the reader: scrolling one pane keeps the other
        /// aligned on the shared segment grid (P4.3a). Off = panes scroll independently.
        /// </summary>
        public bool EnableBilingualScrollSync { get; set; } = true;

        public string? Username { get; set; }

        public string? GitHubAccessToken { get; set; }
        public string? GitHubUsername { get; set; }

        public bool HasCompletedOnboarding { get; set; }

        public bool HasRegisteredProtocolHandler { get; set; }

        public bool EnableStudyPanel { get; set; }

        /// <summary>Font size for the reader panes. Default 14, range 8–32.</summary>
        public double EditorFontSize { get; set; } = 14.0;

        public bool EnableProvenancePanel { get; set; }

        /// <summary>Preferred citation style used as default across all citation surfaces.</summary>
        public CitationStyle PreferredCitationStyle { get; set; } = CitationStyle.Chicago;

        /// <summary>
        /// Preferred citation style index, matching <see cref="CitationStyle"/> ordinal.
        /// 0=Plain, 1=Chicago, 2=APA, 3=MLA, 4=BibTeX, 5=CslJson, 6=CbetaReference, 7=Ris, 8=Sbl.
        /// Default 1 = Chicago.
        /// </summary>
        public int PreferredCitationStyleIndex { get; set; } = 1;

        // Window state persistence
        public double? WindowX { get; set; }
        public double? WindowY { get; set; }
        public double? WindowWidth { get; set; }
        public double? WindowHeight { get; set; }
        public bool IsMaximized { get; set; }

        /// <summary>Which corpus the last-loaded root belongs to. Defaults to CBETA.</summary>
        public CorpusKind ActiveCorpus { get; set; } = CorpusKind.Cbeta;

        /// <summary>
        /// When true, assistant panels (Translate, Reader Study, Scholar) show
        /// concordance matches from untranslated texts alongside TM results.
        /// These are Chinese-only — the source passage is shown without an
        /// English translation. Useful for comparative research; off by default
        /// because it adds a search query per assistant refresh.
        /// </summary>
        public bool EnableConcordance { get; set; }

        /// <summary>
        /// When true (default), full-text search uses the "instant" path: candidates
        /// are ranked by per-document term frequency read straight from the inverted
        /// index. For a 2-char/single-bigram query the hit count is shown from that tf
        /// (exact — the index proves the term is contiguous) and KWIC snippets are
        /// verified lazily for only the top-ranked results (the long tail loads on
        /// demand). Longer phrases are still tf-ranked but every candidate is verified,
        /// because the index proves only that the bigrams co-occur, not that they form
        /// the contiguous phrase. When false, every candidate is eagerly verified against
        /// document text for exact counts and full snippets. Default ON for responsiveness.
        /// </summary>
        public bool InstantSearch { get; set; } = true;

        /// <summary>
        /// Maximum TM matches shown per assistant refresh. Default 8. Range 4–20.
        /// Higher = more comprehensive but more scrolling in the assistant panel.
        /// </summary>
        public int TmMaxResults { get; set; } = 8;

        public int Version { get; set; } = 5;

        /// <summary>Persisted search history (most recent first, max 20).</summary>
        public List<string> SearchHistory { get; set; } = new();
    }
}