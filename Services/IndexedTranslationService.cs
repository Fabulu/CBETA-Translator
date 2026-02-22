// Services/IndexedTranslationService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace CbetaTranslator.App.Services;

public enum TranslationEditMode
{
    Head,
    Body,
    Notes
}

public enum TranslationUnitKind
{
    Head,
    Body,
    Note
}

public sealed class TranslationUnit
{
    public int Index { get; set; } // UI numbering (per mode render)
    public string StableKey { get; set; } = "";   // xml:id or node path (internal only)
    public string NodePath { get; set; } = "";
    public string Zh { get; set; } = "";
    public string En { get; set; } = "";
    public TranslationUnitKind Kind { get; set; }

    public bool IsParagraph { get; set; }
    public string? XmlId { get; set; }
}

public sealed class IndexedTranslationDocument
{
    public string OriginalXml { get; set; } = "";
    public string TranslatedXml { get; set; } = "";
    public List<TranslationUnit> Units { get; } = new();
}

public sealed class IndexedTranslationService
{
    private static readonly XNamespace Tei = "http://www.tei-c.org/ns/1.0";
    private static readonly XNamespace Cb = "http://www.cbeta.org/ns/1.0";
    private static readonly XNamespace XmlNs = "http://www.w3.org/XML/1998/namespace";

    private const string RespUiImport = "ui-import"; // managed English notes stored in TEI

    // ------------------------------------------------------------
    // BUILD INDEX
    // ------------------------------------------------------------

    public IndexedTranslationDocument BuildIndex(string originalXml, string? translatedXml)
    {
        originalXml ??= "";
        translatedXml = string.IsNullOrWhiteSpace(translatedXml) ? originalXml : translatedXml;

        var result = new IndexedTranslationDocument
        {
            OriginalXml = originalXml,
            TranslatedXml = translatedXml
        };

        var origDoc = XDocument.Parse(originalXml, LoadOptions.PreserveWhitespace);
        var tranDoc = XDocument.Parse(translatedXml, LoadOptions.PreserveWhitespace);

        var origBody = origDoc.Descendants(Tei + "body").FirstOrDefault();
        if (origBody == null) return result;

        // Collect all translatable original nodes
        foreach (var el in origBody.DescendantsAndSelf())
        {
            if (!IsTranslatable(el)) continue;

            var nodePath = BuildNodePath(el);
            var xmlId = (string?)el.Attribute(XmlNs + "id");
            var stableKey = !string.IsNullOrWhiteSpace(xmlId)
                ? "id:" + xmlId.Trim()
                : "path:" + nodePath;

            var unit = new TranslationUnit
            {
                XmlId = xmlId,
                NodePath = nodePath,
                StableKey = stableKey,
                Zh = NormalizeSpace(ExtractInlineText(el)),
                Kind = ClassifyKind(el),
                IsParagraph = el.Name == Tei + "p",
            };

            unit.En = TryReadEnglishFromTranslated(tranDoc, unit) ?? "";
            result.Units.Add(unit);
        }

        return result;
    }

    // ------------------------------------------------------------
    // RENDER PROJECTION (single editor text)
    // ------------------------------------------------------------

    public string RenderProjection(IndexedTranslationDocument doc, TranslationEditMode mode)
    {
        if (doc == null) return "";

        var wantedKind = mode switch
        {
            TranslationEditMode.Head => TranslationUnitKind.Head,
            TranslationEditMode.Body => TranslationUnitKind.Body,
            TranslationEditMode.Notes => TranslationUnitKind.Note,
            _ => TranslationUnitKind.Body
        };

        var units = doc.Units.Where(u => u.Kind == wantedKind).ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"# MODE: {mode}");
        sb.AppendLine("# Edit ONLY EN lines.");
        sb.AppendLine("# Do not change <n> block numbers or ZH lines.");
        sb.AppendLine();

        int idx = 1;
        foreach (var u in units)
        {
            u.Index = idx++;

            sb.AppendLine($"<{u.Index}>");
            sb.AppendLine($"ZH: {u.Zh}");
            sb.AppendLine($"EN: {u.En}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    // ------------------------------------------------------------
    // APPLY EDITED PROJECTION BACK INTO INDEX (in-memory)
    // ------------------------------------------------------------

    public void ApplyProjectionEdits(IndexedTranslationDocument doc, TranslationEditMode mode, string editedText)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));
        editedText ??= "";

        var parsed = ParseProjection(editedText);

        var wantedKind = mode switch
        {
            TranslationEditMode.Head => TranslationUnitKind.Head,
            TranslationEditMode.Body => TranslationUnitKind.Body,
            TranslationEditMode.Notes => TranslationUnitKind.Note,
            _ => TranslationUnitKind.Body
        };

        var targetUnits = doc.Units.Where(u => u.Kind == wantedKind).ToList();

        foreach (var block in parsed)
        {
            // <n> is 1-based in the projection
            int idx = block.BlockNumber - 1;
            if (idx < 0 || idx >= targetUnits.Count)
                throw new InvalidOperationException($"Unknown block number <{block.BlockNumber}>.");

            var unit = targetUnits[idx];

            // Guard against accidental edits to ZH
            var expectedZh = (unit.Zh ?? "").Trim();
            var actualZh = (block.Zh ?? "").Trim();
            if (!string.Equals(expectedZh, actualZh, StringComparison.Ordinal))
                throw new InvalidOperationException($"Block <{block.BlockNumber}> ZH was modified. Only EN may be edited.");

            ValidateEnglish(block.En, block.BlockNumber);
            unit.En = block.En;
        }
    }

    // ------------------------------------------------------------
    // MERGE INDEX BACK TO TEI
    // ------------------------------------------------------------

    public string BuildTranslatedXml(IndexedTranslationDocument doc, out int updatedCount)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));

        var tranDoc = XDocument.Parse(doc.TranslatedXml, LoadOptions.PreserveWhitespace);
        updatedCount = 0;

        foreach (var unit in doc.Units)
        {
            var en = (unit.En ?? "").Trim();

            if (!TryFindTargetNode(tranDoc, unit, out var target) || target == null)
                continue;

            // We store EN as a managed community note in TEI (safe, reversible)
            // This avoids destroying source Chinese structure.
            var existing = target.Elements(Tei + "note").FirstOrDefault(n =>
                string.Equals((string?)n.Attribute("type"), "community", StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string?)n.Attribute(XmlNs + "lang"), "en", StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string?)n.Attribute("resp"), RespUiImport, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(en))
            {
                existing?.Remove();
                continue;
            }

            if (existing == null)
            {
                target.Add(new XElement(Tei + "note",
                    new XAttribute("type", "community"),
                    new XAttribute("resp", RespUiImport),
                    new XAttribute(XmlNs + "lang", "en"),
                    en));
            }
            else
            {
                existing.Value = en;
            }

            // Optional normalization: move all note tags to end
            MoveNotesToEnd(target);

            updatedCount++;
        }

        return SerializeWithDeclaration(tranDoc);
    }

    // ------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------

    private static void ValidateEnglish(string en, int blockNumber)
    {
        if ((en ?? "").Contains('<') || (en ?? "").Contains('>'))
            throw new InvalidOperationException($"Block <{blockNumber}> EN contains '<' or '>' which is not allowed.");
    }

    private static void MoveNotesToEnd(XElement target)
    {
        var notes = target.Elements().Where(e => e.Name.LocalName == "note").ToList();
        if (notes.Count == 0) return;

        foreach (var n in notes) n.Remove();
        foreach (var n in notes) target.Add(n);
    }

    private string? TryReadEnglishFromTranslated(XDocument tranDoc, TranslationUnit unit)
    {
        if (!TryFindTargetNode(tranDoc, unit, out var target) || target == null)
            return null;

        var enNote = target.Elements(Tei + "note").FirstOrDefault(n =>
            string.Equals((string?)n.Attribute("type"), "community", StringComparison.OrdinalIgnoreCase) &&
            string.Equals((string?)n.Attribute(XmlNs + "lang"), "en", StringComparison.OrdinalIgnoreCase));

        return enNote?.Value?.Trim() ?? "";
    }

    private static bool TryFindTargetNode(XDocument doc, TranslationUnit unit, out XElement? found)
    {
        found = null;

        if (!string.IsNullOrWhiteSpace(unit.XmlId))
        {
            // Try exact xml:id match on any TEI/CBETA element first (not just <p>)
            found = doc.Descendants()
                .FirstOrDefault(x => string.Equals((string?)x.Attribute(XmlNs + "id"), unit.XmlId, StringComparison.Ordinal));
            if (found != null) return true;
        }

        if (!string.IsNullOrWhiteSpace(unit.NodePath))
            return TryFindNodeByPath(doc, unit.NodePath, out found);

        return false;
    }

    private static TranslationUnitKind ClassifyKind(XElement el)
    {
        if (el.Name == Tei + "note") return TranslationUnitKind.Note;
        if (el.Name == Tei + "head" || el.Name == Cb + "jhead" || el.Name == Cb + "mulu" || el.Name == Tei + "byline")
            return TranslationUnitKind.Head;
        return TranslationUnitKind.Body;
    }

    private static bool IsTranslatable(XElement e)
    {
        if (e.Name == Tei + "pb" || e.Name == Tei + "lb" || e.Name == Tei + "milestone")
            return false;

        // Skip imported EN notes (managed)
        if (e.Name == Tei + "note" &&
            string.Equals((string?)e.Attribute("type"), "community", StringComparison.OrdinalIgnoreCase) &&
            string.Equals((string?)e.Attribute(XmlNs + "lang"), "en", StringComparison.OrdinalIgnoreCase))
            return false;

        // Explicitly include these
        if (e.Name == Tei + "p" ||
            e.Name == Tei + "head" ||
            e.Name == Tei + "item" ||
            e.Name == Tei + "note" ||
            e.Name == Tei + "byline" ||
            e.Name == Cb + "jhead" ||
            e.Name == Cb + "mulu")
        {
            return !string.IsNullOrWhiteSpace(NormalizeSpace(ExtractInlineText(e)));
        }

        return false;
    }

    private sealed record ProjectionBlock(int BlockNumber, string Zh, string En);

    private static List<ProjectionBlock> ParseProjection(string text)
    {
        var lines = (text ?? "").Replace("\r\n", "\n").Split('\n');
        var blocks = new List<ProjectionBlock>();

        int i = 0;
        while (i < lines.Length)
        {
            var line = lines[i].Trim();

            if (!line.StartsWith("<") || !line.EndsWith(">"))
            {
                i++;
                continue;
            }

            var numRaw = line.Substring(1, line.Length - 2);
            if (!int.TryParse(numRaw, out var blockNum))
                throw new InvalidOperationException($"Invalid block header: {line}");

            i++;

            string? zh = null;
            string? en = null;

            for (; i < lines.Length; i++)
            {
                var cur = lines[i];

                // next block starts
                if (cur.TrimStart().StartsWith("<") && cur.TrimEnd().EndsWith(">"))
                    break;

                if (cur.StartsWith("ZH:", StringComparison.Ordinal))
                    zh = cur.Substring(3).TrimStart();
                else if (cur.StartsWith("EN:", StringComparison.Ordinal))
                    en = cur.Substring(3).TrimStart();
            }

            if (zh == null)
                throw new InvalidOperationException($"Block <{blockNum}> missing ZH.");
            if (en == null)
                throw new InvalidOperationException($"Block <{blockNum}> missing EN.");

            blocks.Add(new ProjectionBlock(blockNum, zh, en));
        }

        return blocks;
    }

    private static string NormalizeSpace(string s)
        => Regex.Replace(s ?? "", @"\s+", " ").Trim();

    private static string ExtractInlineText(XElement p)
    {
        var sb = new StringBuilder();

        foreach (var n in p.Nodes())
        {
            if (n is XText t)
            {
                sb.Append(t.Value);
            }
            else if (n is XElement e)
            {
                if (e.Name.LocalName is "lb" or "pb" or "milestone")
                    continue;

                // Hide managed EN import notes from ZH extraction so they don't pollute ZH line
                if (e.Name == Tei + "note" &&
                    string.Equals((string?)e.Attribute("type"), "community", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((string?)e.Attribute(XmlNs + "lang"), "en", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (e.Name.LocalName == "note")
                {
                    var txt = NormalizeSpace(e.Value);
                    if (!string.IsNullOrWhiteSpace(txt))
                        sb.Append(" [NOTE:" + txt + "]");
                    continue;
                }

                sb.Append(ExtractInlineText(e));
            }
        }

        return sb.ToString();
    }

    private static string SerializeWithDeclaration(XDocument doc)
    {
        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = doc.Declaration == null,
            Indent = false,
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Replace
        };

        var sb = new StringBuilder();
        using var sw = new System.IO.StringWriter(sb);
        using var xw = XmlWriter.Create(sw, settings);
        doc.Save(xw);
        return sb.ToString();
    }

    // ---------------- Node paths ----------------

    private static readonly XNamespace TeiNs = "http://www.tei-c.org/ns/1.0";
    private static readonly XNamespace CbNs = "http://www.cbeta.org/ns/1.0";

    private static string PrefixFor(XNamespace ns) => ns == TeiNs ? "tei" : ns == CbNs ? "cb" : "ns";
    private static XNamespace NamespaceForPrefix(string p) => p == "tei" ? TeiNs : p == "cb" ? CbNs : XNamespace.None;

    private static string BuildNodePath(XElement e)
    {
        var segs = new Stack<string>();
        XElement? cur = e;

        while (cur != null)
        {
            var parent = cur.Parent;
            int idx = 1;
            if (parent != null)
                idx = parent.Elements(cur.Name).TakeWhile(x => x != cur).Count() + 1;

            var prefix = PrefixFor(cur.Name.Namespace);
            segs.Push($"{prefix}:{cur.Name.LocalName}[{idx}]");
            cur = parent;
        }

        return string.Join("/", segs);
    }

    private static bool TryFindNodeByPath(XDocument doc, string path, out XElement? found)
    {
        found = null;
        if (doc.Root == null || string.IsNullOrWhiteSpace(path)) return false;

        var segs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segs.Length == 0) return false;

        XElement current = doc.Root;

        for (int i = 0; i < segs.Length; i++)
        {
            var seg = segs[i];
            int open = seg.LastIndexOf('[');
            int close = seg.LastIndexOf(']');
            if (open <= 0 || close <= open + 1) return false;

            var nameToken = seg.Substring(0, open);
            if (!int.TryParse(seg.Substring(open + 1, close - open - 1), out var idx) || idx < 1) return false;

            int colon = nameToken.IndexOf(':');
            if (colon <= 0 || colon >= nameToken.Length - 1) return false;

            var prefix = nameToken[..colon];
            var local = nameToken[(colon + 1)..];
            var ns = NamespaceForPrefix(prefix);
            var qn = ns + local;

            if (i == 0)
            {
                if (current.Name != qn || idx != 1) return false;
                continue;
            }

            var next = current.Elements(qn).Skip(idx - 1).FirstOrDefault();
            if (next == null) return false;
            current = next;
        }

        found = current;
        return true;
    }
}