// Models/DocAnnotation.cs
using System;

namespace CbetaTranslator.App.Models;

public sealed class DocAnnotation
{
    // Rendered text positions (in BASE rendered text, before superscripts are inserted)
    public int Start { get; }
    public int EndExclusive { get; }

    // What we show in the popup
    public string Text { get; }

    // Kind/category (cbeta: mod/orig/add etc, and our new: "community")
    public string? Kind { get; }

    // NEW: author / responsible person (maps well to TEI @resp)
    // Example: resp="Fabian"
    public string? Resp { get; }

    // NEW: absolute XML span that represents the note element we captured
    // If present, we can remove the note by deleting exactly this substring.
    // -1 means "unknown / not supported for deletion"
    public int XmlStart { get; }
    public int XmlEndExclusive { get; }

    public bool HasXmlSpan => XmlStart >= 0 && XmlEndExclusive > XmlStart;

    public bool IsCommunity
        => string.Equals(Kind, "community", StringComparison.OrdinalIgnoreCase);

    public DocAnnotation(
        int start,
        int endExclusive,
        string text,
        string? kind = null,
        string? resp = null,
        int xmlStart = -1,
        int xmlEndExclusive = -1)
    {
        Start = Math.Max(0, start);
        EndExclusive = Math.Max(Start, endExclusive);

        Text = text ?? "";
        Kind = kind;

        Resp = string.IsNullOrWhiteSpace(resp) ? null : resp.Trim();

        if (xmlStart >= 0 && xmlEndExclusive > xmlStart)
        {
            XmlStart = xmlStart;
            XmlEndExclusive = xmlEndExclusive;
        }
        else
        {
            XmlStart = -1;
            XmlEndExclusive = -1;
        }
    }

    public override string ToString()
    {
        var who = string.IsNullOrWhiteSpace(Resp) ? "" : $" ({Resp})";
        var k = string.IsNullOrWhiteSpace(Kind) ? "note" : Kind;
        return $"{k}{who}: {Text}";
    }
}
