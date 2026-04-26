// Models/CitationStyle.cs
namespace ReadZen.App.Models;

public enum CitationStyle
{
    /// <summary>Simple plain-text attribution.</summary>
    Plain,
    /// <summary>Chicago Manual of Style, Notes-Bibliography (17th ed.).</summary>
    Chicago,
    /// <summary>APA 7th edition.</summary>
    Apa,
    /// <summary>MLA 9th edition.</summary>
    Mla,
    /// <summary>BibTeX @misc entry.</summary>
    BibTeX,
    /// <summary>CSL-JSON single entry.</summary>
    CslJson,
    /// <summary>CBETA canonical reference only (e.g., "T no. 2005, 48: 292c18").</summary>
    CbetaReference,
    /// <summary>RIS format (Phase 2).</summary>
    Ris,
    /// <summary>Society of Biblical Literature style (Phase 2).</summary>
    Sbl
}
