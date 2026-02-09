namespace CbetaTranslator.App.Models;

public sealed class FileNavItem
{
    public string RelPath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string DisplayShort { get; set; } = "";
    public string Tooltip { get; set; } = "";
    public TranslationStatus Status { get; set; } = TranslationStatus.Red;
}
