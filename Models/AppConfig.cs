namespace CbetaTranslator.App.Models
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

        public int Version { get; set; } = 3;
    }
}