namespace CbetaTranslator.App.Models
{
    public enum PdfLayoutMode
    {
        Alternating = 0,
        SideBySide = 1
    }

    public sealed class AppConfig
    {
        public string? TextRootPath { get; set; }
        public string? LastSelectedRelPath { get; set; }
        public bool IsDarkTheme { get; set; } = true;

        public PdfLayoutMode PdfLayoutMode { get; set; } = PdfLayoutMode.Alternating;
        public bool PdfIncludeEnglish { get; set; } = true;
        public bool PdfForceSideBySideWhenEnglish { get; set; } = true;
        public float PdfLineSpacing { get; set; } = 1.4f;
        public float PdfTrackingChinese { get; set; } = 12.0f;
        public float PdfTrackingEnglish { get; set; } = 8.0f;
        public float PdfParagraphSpacing { get; set; } = 0.6f;
        public bool PdfAutoScaleFonts { get; set; } = true;
        public float PdfTargetFillRatio { get; set; } = 0.88f;
        public float PdfMinFontSize { get; set; } = 10.0f;
        public float PdfMaxFontSize { get; set; } = 18.0f;
        public bool PdfLockBilingualFontSize { get; set; } = true;

        public int Version { get; set; } = 3;
    }
}
