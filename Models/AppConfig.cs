namespace ReadZen.App.Models
{
    public sealed class AppConfig
    {
        public string? TextRootPath { get; set; }
        public string? LastSelectedRelPath { get; set; }

        public bool IsDarkTheme { get; set; } = true;
        public bool ZenOnly { get; set; }

        public bool EnableHoverDictionary { get; set; } = true;

        public string? Username { get; set; }

        public string? GitHubAccessToken { get; set; }
        public string? GitHubUsername { get; set; }

        public bool HasCompletedOnboarding { get; set; }

        public bool HasRegisteredProtocolHandler { get; set; }

        public bool EnableStudyPanel { get; set; }

        /// <summary>Font size for the reader panes. Default 14, range 8–32.</summary>
        public double EditorFontSize { get; set; } = 14.0;

        public bool EnableProvenancePanel { get; set; }

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
        /// Maximum TM matches shown per assistant refresh. Default 8. Range 4–20.
        /// Higher = more comprehensive but more scrolling in the assistant panel.
        /// </summary>
        public int TmMaxResults { get; set; } = 8;

        public int Version { get; set; } = 4;
    }
}