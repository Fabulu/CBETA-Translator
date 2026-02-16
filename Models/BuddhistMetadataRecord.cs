using System.Collections.Generic;

namespace CbetaTranslator.App.Models;

public sealed class BuddhistMetadataRecord
{
    public string RelPath { get; set; } = "";
    public string CanonCode { get; set; } = "Unknown";
    public List<string> Traditions { get; set; } = new();
    public string Period { get; set; } = "Unknown Period";
    public string Origin { get; set; } = "Unknown Origin";
}
