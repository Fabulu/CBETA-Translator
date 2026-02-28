using System.Collections.Generic;

namespace CbetaTranslator.App.Models;

public sealed class TermbaseEntry
{
    public string SourceTerm { get; set; } = "";
    public string PreferredTarget { get; set; } = "";
    public List<string> AlternateTargets { get; set; } = new();
    public string Status { get; set; } = "preferred";
    public string Note { get; set; } = "";
}