using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Exports qualitative coding data as a QDPX (REFI-QDA) project archive.
/// The ZIP contains project.qde (XML) + Sources/{guid}.txt plaintext files.
/// </summary>
public static class QdpxExportService
{
    /// <summary>
    /// Delegate that loads plaintext for a given relative path.
    /// Returns the plaintext content of the document.
    /// </summary>
    public delegate Task<string?> DocLoader(string relPath, CancellationToken ct);

    /// <summary>
    /// Exports tags and sources to a QDPX archive.
    /// </summary>
    public static async Task ExportAsync(
        string outputPath,
        List<DocumentTag> tags,
        TagVocabulary vocab,
        DocLoader docLoader,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(outputPath)) throw new ArgumentNullException(nameof(outputPath));
        if (tags == null) throw new ArgumentNullException(nameof(tags));
        if (docLoader == null) throw new ArgumentNullException(nameof(docLoader));

        var tagLookup = new Dictionary<string, TagDefinition>(StringComparer.Ordinal);
        if (vocab?.Tags != null)
        {
            foreach (var td in vocab.Tags)
                tagLookup.TryAdd(td.Id, td);
        }

        // Collect distinct RelPaths
        var relPaths = tags.Select(t => t.RelPath)
                           .Distinct(StringComparer.OrdinalIgnoreCase)
                           .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                           .ToList();

        // Load plaintext for each source
        var sources = new Dictionary<string, (string Guid, string Text)>(StringComparer.OrdinalIgnoreCase);
        foreach (var rp in relPaths)
        {
            ct.ThrowIfCancellationRequested();
            var text = await docLoader(rp, ct) ?? "";
            sources[rp] = (System.Guid.NewGuid().ToString("D"), text);
        }

        // Build XML
        XNamespace ns = "urn:QDA-XML:project:1.0";
        var projectGuid = System.Guid.NewGuid().ToString("D");

        // CodeBook
        var codeElements = new List<XElement>();
        var codeGuids = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var codeId in tags.Select(t => t.TagId).Distinct(StringComparer.Ordinal).OrderBy(id => id))
        {
            var codeGuid = System.Guid.NewGuid().ToString("D");
            codeGuids[codeId] = codeGuid;
            string name = tagLookup.TryGetValue(codeId, out var def) ? def.DisplayName : codeId;
            string color = tagLookup.TryGetValue(codeId, out var def2) ? def2.Color : "#808080";

            codeElements.Add(new XElement(ns + "Code",
                new XAttribute("guid", codeGuid),
                new XAttribute("name", name),
                new XAttribute("color", color)));
        }

        // Sources
        var sourceElements = new List<XElement>();
        foreach (var (rp, (guid, text)) in sources)
        {
            sourceElements.Add(new XElement(ns + "TextSource",
                new XAttribute("guid", guid),
                new XAttribute("name", rp),
                new XElement(ns + "PlainTextContent",
                    new XAttribute("path", $"Sources/{guid}.txt"))));
        }

        // Codings — map lb-ranges to character positions within the plaintext
        var codingElements = new List<XElement>();
        foreach (var tag in tags)
        {
            if (!sources.TryGetValue(tag.RelPath, out var src))
                continue;
            if (!codeGuids.TryGetValue(tag.TagId, out var codeGuid))
                continue;

            // Simple heuristic: search for lb markers in the text
            // Since we're working with plaintext, map FromLb/ToLb to approximate positions
            int startPos = 0;
            int endPos = src.Text.Length;

            // Try to find lb-based positions
            var (foundStart, foundEnd) = FindLbRange(src.Text, tag.FromLb, tag.ToLb);
            if (foundStart >= 0)
            {
                startPos = foundStart;
                endPos = foundEnd;
            }

            codingElements.Add(new XElement(ns + "Coding",
                new XAttribute("guid", System.Guid.NewGuid().ToString("D")),
                new XElement(ns + "CodeRef",
                    new XAttribute("targetGUID", codeGuid)),
                new XElement(ns + "TextSelection",
                    new XAttribute("guid", System.Guid.NewGuid().ToString("D")),
                    new XAttribute("startPosition", startPos),
                    new XAttribute("endPosition", endPos),
                    new XElement(ns + "SourceRef",
                        new XAttribute("targetGUID", src.Guid)))));
        }

        var projectXml = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(ns + "Project",
                new XAttribute("name", "ReadZen Export"),
                new XAttribute("origin", "ReadZen"),
                new XElement(ns + "CodeBook",
                    new XElement(ns + "Codes", codeElements)),
                new XElement(ns + "Sources", sourceElements),
                new XElement(ns + "Codings", codingElements)));

        // Write ZIP
        if (File.Exists(outputPath))
            File.Delete(outputPath);

        using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);

        // project.qde
        var qdeEntry = zip.CreateEntry("project.qde");
        using (var qdeStream = qdeEntry.Open())
        {
            using var writer = new StreamWriter(qdeStream, new UTF8Encoding(false));
            await writer.WriteAsync(projectXml.Declaration?.ToString() + "\n" + projectXml.Root?.ToString());
        }

        // Sources/*.txt
        foreach (var (rp, (guid, text)) in sources)
        {
            ct.ThrowIfCancellationRequested();
            var entry = zip.CreateEntry($"Sources/{guid}.txt");
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            await writer.WriteAsync(text);
        }
    }

    /// <summary>
    /// Attempts to locate lb markers in plaintext and return character positions.
    /// Returns (-1, -1) if not found.
    /// </summary>
    internal static (int Start, int End) FindLbRange(string text, string fromLb, string toLb)
    {
        // Look for lb markers like "[p0001a01]" or just the n-value
        int startIdx = FindLbMarker(text, fromLb);
        int endIdx = FindLbMarker(text, toLb);

        if (startIdx < 0) return (-1, -1);
        if (endIdx < 0) endIdx = text.Length;
        if (endIdx < startIdx) endIdx = text.Length;

        return (startIdx, endIdx);
    }

    private static int FindLbMarker(string text, string lb)
    {
        if (string.IsNullOrEmpty(lb) || string.IsNullOrEmpty(text))
            return -1;

        // Try bracketed form first: [lb]
        int idx = text.IndexOf($"[{lb}]", StringComparison.Ordinal);
        if (idx >= 0) return idx;

        // Try bare n-value
        idx = text.IndexOf(lb, StringComparison.Ordinal);
        return idx;
    }
}
