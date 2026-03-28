namespace CbetaTranslator.App.Models;

public sealed class CorpusUsageHit
{
    public string ZhSnippet { get; set; } = "";
    public string SourceRelPath { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string MasterName { get; set; } = "";
    public int ApproximateDate { get; set; }
    public string DateDisplay => ApproximateDate > 0 ? $"~{ApproximateDate} CE" : "";
}
