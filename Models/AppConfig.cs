namespace CbetaTranslator.App.Models
{
    public sealed class AppConfig
    {
        public string? TextRootPath { get; set; }
        public string? LastSelectedRelPath { get; set; }

        public bool IsDarkTheme { get; set; } = true;
        public bool ZenOnly { get; set; }

        public bool EnableHoverDictionary { get; set; } = true;

        public int Version { get; set; } = 3;
    }
}