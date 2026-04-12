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

        public bool EnableProvenancePanel { get; set; }

        /// <summary>Which corpus the last-loaded root belongs to. Defaults to CBETA.</summary>
        public CorpusKind ActiveCorpus { get; set; } = CorpusKind.Cbeta;

        public int Version { get; set; } = 4;
    }
}