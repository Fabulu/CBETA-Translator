using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CbetaTranslator.App.Services;

public sealed class MarkdownTranslationService
{
    public const string CurrentFormat = "cbeta-translation-md-v2";
    private static readonly Regex MultiWs = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex XmlRefPattern = new(@"^<!--\s+xml-ref:\s+p\s+xml:id=([^\s]+)\s+-->\s*$", RegexOptions.Compiled);
    private static readonly Regex XmlRefNodePathPattern = new(@"^<!--\s+xml-ref:\s+node\s+path=([^\s]+)\s+-->\s*$", RegexOptions.Compiled);
    private static readonly Regex FrontmatterPattern = new(@"^([A-Za-z0-9_\-]+):\s*(.*)$", RegexOptions.Compiled);
    private static readonly string[] BoundaryPrefixes =
    {
        "<!--",
        "<!-- xml-ref:",
        "<!-- line:",
        "<!-- page:",
        "<!-- doc-meta:",
        "<!-- author:"
    };

    private static readonly XNamespace Tei = "http://www.tei-c.org/ns/1.0";
    private static readonly XNamespace Cb = "http://www.cbeta.org/ns/1.0";
    private static readonly XNamespace Xml = "http://www.w3.org/XML/1998/namespace";

    public string ConvertTeiToMarkdown(string originalXml, string? sourceFileName)
    {
        if (string.IsNullOrWhiteSpace(originalXml))
            return "";

        var doc = XDocument.Parse(originalXml);
        var title = NormalizeSpace(
            (string?)doc.Descendants(Tei + "title").FirstOrDefault(t => (string?)t.Attribute("level") == "m")
            ?? (string?)doc.Descendants(Tei + "title").FirstOrDefault()
            ?? "Untitled");
        var docId = (string?)doc.Root?.Attribute(Xml + "id") ?? "";
        var cbetaId = NormalizeSpace((string?)doc.Descendants(Tei + "idno").FirstOrDefault(e => (string?)e.Attribute("type") == "CBETA") ?? "");
        var extent = NormalizeSpace((string?)doc.Descendants(Tei + "extent").FirstOrDefault() ?? "");
        var author = NormalizeSpace((string?)doc.Descendants(Tei + "author").FirstOrDefault() ?? "");

        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"title: {title}");
        sb.AppendLine($"doc_id: {docId}");
        sb.AppendLine($"source_xml: {(!string.IsNullOrWhiteSpace(sourceFileName) ? sourceFileName : docId + ".xml")}");
        sb.AppendLine($"format: {CurrentFormat}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {title}");
        sb.AppendLine();
        sb.AppendLine($"<!-- doc-meta: CBETA id={cbetaId} | extent={extent} -->");
        if (!string.IsNullOrWhiteSpace(author))
            sb.AppendLine($"<!-- author: {author} -->");
        sb.AppendLine("<!--");
        sb.AppendLine("Translation workflow:");
        sb.AppendLine("- Edit only EN lines.");
        sb.AppendLine("- Keep ZH lines and xml-ref comments unchanged.");
        sb.AppendLine("- One EN line maps to the preceding ZH line/paragraph.");
        sb.AppendLine("-->");
        sb.AppendLine();

        var body = doc.Descendants(Tei + "body").FirstOrDefault();
        if (body != null)
        {
            foreach (var n in body.Nodes())
                WriteNode(n, sb);
        }

        return sb.ToString();
    }

    public string MergeMarkdownIntoTei(string originalXml, string markdown, out int updatedCount)
    {
        var rows = ParseMarkdownRows(markdown);
        var enById = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.XmlId))
            .ToDictionary(r => r.XmlId!, r => r.En ?? "", StringComparer.Ordinal);
        var enByPath = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.NodePath))
            .ToDictionary(r => r.NodePath!, r => r.En ?? "", StringComparer.Ordinal);

        var doc = XDocument.Parse(originalXml, LoadOptions.PreserveWhitespace);
        updatedCount = 0;
        var sourceIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var p in doc.Descendants(Tei + "p"))
        {
            var xmlId = (string?)p.Attribute(Xml + "id");
            if (string.IsNullOrWhiteSpace(xmlId))
                continue;

            if (!sourceIds.Add(xmlId))
                throw new MarkdownTranslationException($"Duplicate xml:id in source TEI: {xmlId}");
        }

        var missingInSource = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.XmlId) && !sourceIds.Contains(r.XmlId!))
            .ToList();
        if (missingInSource.Count > 0)
        {
            var first = missingInSource[0];
            throw new MarkdownTranslationException(
                $"Markdown references xml:id not found in source TEI: {first.XmlId}",
                first.XmlRefLine);
        }

        foreach (var row in rows.Where(r => !string.IsNullOrWhiteSpace(r.NodePath)))
        {
            if (!TryFindNodeByPath(doc, row.NodePath!, out _))
                throw new MarkdownTranslationException($"Markdown references node path not found in source TEI: {row.NodePath}", row.XmlRefLine);
        }

        foreach (var p in doc.Descendants(Tei + "p"))
        {
            var xmlId = (string?)p.Attribute(Xml + "id");
            if (string.IsNullOrWhiteSpace(xmlId) || !enById.TryGetValue(xmlId, out var enTextRaw))
                continue;

            var enText = (enTextRaw ?? "").Trim();
            var existing = p.Elements(Tei + "note").FirstOrDefault(n =>
                string.Equals((string?)n.Attribute("type"), "community", StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string?)n.Attribute(Xml + "lang"), "en", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(enText))
            {
                if (existing == null)
                {
                    p.Add(new XElement(Tei + "note",
                        new XAttribute("type", "community"),
                        new XAttribute("resp", "md-import"),
                        new XAttribute(Xml + "lang", "en"),
                        enText));
                }
                else
                {
                    existing.SetAttributeValue("resp", "md-import");
                    existing.Value = enText;
                }

                updatedCount++;
            }
            else if (existing != null)
            {
                existing.Remove();
            }
        }

        foreach (var kv in enByPath)
        {
            if (!TryFindNodeByPath(doc, kv.Key, out var target) || target == null)
                continue;

            var enText = (kv.Value ?? "").Trim();
            var existingPathNote = target
                .ElementsAfterSelf(Tei + "note")
                .FirstOrDefault(n =>
                    string.Equals((string?)n.Attribute("type"), "community", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((string?)n.Attribute(Xml + "lang"), "en", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((string?)n.Attribute("target-path"), kv.Key, StringComparison.Ordinal));

            if (!string.IsNullOrWhiteSpace(enText))
            {
                if (existingPathNote == null)
                {
                    target.AddAfterSelf(new XElement(Tei + "note",
                        new XAttribute("type", "community"),
                        new XAttribute("resp", "md-import"),
                        new XAttribute(Xml + "lang", "en"),
                        new XAttribute("target-path", kv.Key),
                        enText));
                }
                else
                {
                    existingPathNote.SetAttributeValue("resp", "md-import");
                    existingPathNote.Value = enText;
                }
                updatedCount++;
            }
            else if (existingPathNote != null)
            {
                existingPathNote.Remove();
            }
        }

        return SerializeWithDeclaration(doc);
    }

    public string CreateReadableInlineEnglishXml(string mergedXml)
    {
        if (string.IsNullOrWhiteSpace(mergedXml))
            return "";

        var doc = XDocument.Parse(mergedXml, LoadOptions.PreserveWhitespace);

        var pathNotes = doc.Descendants(Tei + "note")
            .Where(n =>
                string.Equals((string?)n.Attribute("type"), "community", StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string?)n.Attribute(Xml + "lang"), "en", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace((string?)n.Attribute("target-path")))
            .ToList();

        foreach (var note in pathNotes)
        {
            var targetPath = (string?)note.Attribute("target-path");
            var enText = (note.Value ?? "").Trim();

            if (!string.IsNullOrWhiteSpace(targetPath) && !string.IsNullOrWhiteSpace(enText) &&
                TryFindNodeByPath(doc, targetPath, out var target) && target != null)
            {
                ReplaceElementTextPreserveStructure(target, enText);
            }

            note.Remove();
        }

        foreach (var p in doc.Descendants(Tei + "p"))
        {
            var note = p.Elements(Tei + "note").FirstOrDefault(n =>
                string.Equals((string?)n.Attribute("type"), "community", StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string?)n.Attribute(Xml + "lang"), "en", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace((string?)n.Attribute("target-path")));

            if (note == null)
                continue;

            var enText = (note.Value ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(enText))
                ReplaceElementTextPreserveStructure(p, enText);

            note.Remove();
        }

        return SerializeWithDeclaration(doc);
    }

    private static void ReplaceElementTextPreserveStructure(XElement element, string replacementText)
    {
        var textNodes = element
            .DescendantNodesAndSelf()
            .OfType<XText>()
            .ToList();

        foreach (var t in textNodes)
            t.Remove();

        element.AddFirst(new XText(replacementText));
    }

    private static List<MarkdownTranslationRow> ParseMarkdownRows(string markdown)
    {
        var rows = new List<MarkdownTranslationRow>();
        var lines = (markdown ?? "").Replace("\r\n", "\n").Split('\n');
        var start = ParseFrontmatter(lines);
        var rowById = new Dictionary<string, MarkdownTranslationRow>(StringComparer.Ordinal);
        var rowByPath = new Dictionary<string, MarkdownTranslationRow>(StringComparer.Ordinal);

        for (int i = start; i < lines.Length; i++)
        {
            var m = XmlRefPattern.Match(lines[i].Trim());
            var mp = XmlRefNodePathPattern.Match(lines[i].Trim());
            if (!m.Success && !mp.Success)
                continue;

            var xmlRefLine = i + 1;
            var xmlId = m.Success ? m.Groups[1].Value : null;
            var nodePath = mp.Success ? mp.Groups[1].Value : null;
            if (string.IsNullOrWhiteSpace(xmlId) && string.IsNullOrWhiteSpace(nodePath))
                throw new MarkdownTranslationException("Empty xml reference in xml-ref block.", xmlRefLine);

            string? zh = null;
            int zhLine = -1;
            var enLines = new List<string>();
            bool sawEn = false;
            int enLine = -1;

            for (i = i + 1; i < lines.Length; i++)
            {
                var cur = lines[i];
                var s = cur.Trim();

                if (XmlRefPattern.IsMatch(s) || XmlRefNodePathPattern.IsMatch(s))
                {
                    i--;
                    break;
                }

                if (s.StartsWith("ZH:", StringComparison.Ordinal))
                {
                    if (zh != null)
                        throw new MarkdownTranslationException($"Duplicate ZH line for reference {(xmlId ?? nodePath)}", i + 1);
                    zh = s.Substring(3).TrimStart();
                    zhLine = i + 1;
                    continue;
                }

                if (s.StartsWith("EN:", StringComparison.Ordinal))
                {
                    if (sawEn)
                        throw new MarkdownTranslationException($"Duplicate EN line for reference {(xmlId ?? nodePath)}", i + 1);
                    sawEn = true;
                    enLine = i + 1;
                    var first = s.Substring(3).TrimStart();
                    if (!string.IsNullOrEmpty(first))
                        enLines.Add(first);

                    for (i = i + 1; i < lines.Length; i++)
                    {
                        var n = lines[i];
                        var nt = n.Trim();
                        if (XmlRefPattern.IsMatch(nt) || XmlRefNodePathPattern.IsMatch(nt) || IsBoundary(nt))
                        {
                            i--;
                            break;
                        }
                        enLines.Add(n.TrimEnd());
                    }
                    continue;
                }

                if (string.IsNullOrWhiteSpace(s))
                    continue;

                if (!sawEn)
                {
                    throw new MarkdownTranslationException(
                        $"Unexpected content before EN line for reference {(xmlId ?? nodePath)}: '{s}'",
                        i + 1);
                }
            }

            if (zh == null)
                throw new MarkdownTranslationException($"Missing ZH line for reference {(xmlId ?? nodePath)}", xmlRefLine);
            if (!sawEn)
                throw new MarkdownTranslationException($"Missing EN line for reference {(xmlId ?? nodePath)}", xmlRefLine);

            var row = new MarkdownTranslationRow(xmlId, nodePath, zh, string.Join("\n", enLines).Trim('\n'), xmlRefLine, zhLine, enLine);
            if (!string.IsNullOrWhiteSpace(xmlId) && rowById.TryGetValue(xmlId, out var existing))
            {
                throw new MarkdownTranslationException(
                    $"Duplicate xml:id in markdown: {xmlId} (first at line {existing.XmlRefLine})",
                    xmlRefLine);
            }
            if (!string.IsNullOrWhiteSpace(nodePath) && rowByPath.TryGetValue(nodePath, out var existingPath))
            {
                throw new MarkdownTranslationException(
                    $"Duplicate node path in markdown: {nodePath} (first at line {existingPath.XmlRefLine})",
                    xmlRefLine);
            }

            if (!string.IsNullOrWhiteSpace(xmlId))
                rowById[xmlId] = row;
            if (!string.IsNullOrWhiteSpace(nodePath))
                rowByPath[nodePath] = row;
            rows.Add(row);
        }

        return rows;
    }

    private static int ParseFrontmatter(string[] lines)
    {
        if (lines.Length == 0 || lines[0].Trim() != "---")
            return 0;

        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "---")
                return i + 1;
            _ = FrontmatterPattern.Match(lines[i].Trim());
        }

        throw new MarkdownTranslationException("Unterminated YAML frontmatter.", 1);
    }

    private static bool IsBoundary(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;
        if (line.StartsWith("#", StringComparison.Ordinal))
            return true;
        return BoundaryPrefixes.Any(p => line.StartsWith(p, StringComparison.Ordinal));
    }

    private static void WriteNode(XNode node, StringBuilder sb)
    {
        if (node is XText)
            return;

        if (node is not XElement e)
            return;

        if (e.Name == Tei + "pb")
            return;

        if (e.Name == Tei + "lb")
        {
            return;
        }

        if (e.Name == Tei + "milestone")
            return;

        if (e.Name == Cb + "mulu" && string.Equals((string?)e.Attribute("type"), "科判", StringComparison.Ordinal))
        {
            int lvl = ParseInt((string?)e.Attribute("level"), 1);
            lvl = Math.Clamp(lvl, 1, 6);
            sb.AppendLine();
            sb.AppendLine($"{new string('#', lvl)} {NormalizeSpace(e.Value)}");
            return;
        }

        if (e.Name == Cb + "juan")
        {
            var fun = (string?)e.Attribute("fun");
            var n = (string?)e.Attribute("n");
            if (string.Equals(fun, "open", StringComparison.OrdinalIgnoreCase))
            {
                var jhead = NormalizeSpace((string?)e.Element(Cb + "jhead") ?? "");
                if (!string.IsNullOrWhiteSpace(jhead))
                {
                    sb.AppendLine();
                    sb.AppendLine($"## {jhead}");

                    // Keep the visible heading and also emit a translatable row.
                    var jheadEl = e.Element(Cb + "jhead");
                    if (jheadEl != null)
                        EmitNodeTextBlock(jheadEl, sb);
                }
            }
            else if (string.Equals(fun, "close", StringComparison.OrdinalIgnoreCase))
            {
                // close markers are non-translatable and remain in source TEI.
            }
            return;
        }

        if (e.Name == Tei + "byline")
        {
            EmitNodeTextBlock(e, sb);
            return;
        }

        if (e.Name == Tei + "p")
        {
            var xmlId = (string?)e.Attribute(Xml + "id");
            sb.AppendLine();
            sb.AppendLine($"<!-- xml-ref: p xml:id={xmlId} -->");
            sb.AppendLine($"ZH: {NormalizeSpace(ExtractInlineText(e))}");
            sb.AppendLine("EN: ");
            return;
        }

        if (IsTranslatableNode(e))
        {
            EmitNodeTextBlock(e, sb);
            return;
        }

        foreach (var n in e.Nodes())
            WriteNode(n, sb);
    }

    private static bool IsTranslatableNode(XElement e)
    {
        if (e.Name == Tei + "head" || e.Name == Tei + "item" || e.Name == Tei + "note")
        {
            // Skip imported English notes.
            if (e.Name == Tei + "note" &&
                string.Equals((string?)e.Attribute("type"), "community", StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string?)e.Attribute(Xml + "lang"), "en", StringComparison.OrdinalIgnoreCase))
                return false;

            return !string.IsNullOrWhiteSpace(NormalizeSpace(ExtractInlineText(e)));
        }

        if (e.Name == Tei + "list" || e.Name == Cb + "div")
            return false;

        // Catchall: any element with direct non-whitespace text in body can become a translation row.
        bool hasOwnText = e.Nodes().OfType<XText>().Any(t => !string.IsNullOrWhiteSpace(t.Value));
        return hasOwnText;

        return false;
    }

    private static void EmitNodeTextBlock(XElement e, StringBuilder sb)
    {
        var text = NormalizeSpace(ExtractInlineText(e));
        if (string.IsNullOrWhiteSpace(text))
            return;

        var path = BuildNodePath(e);
        sb.AppendLine();
        sb.AppendLine($"<!-- xml-ref: node path={path} -->");
        sb.AppendLine($"ZH: {text}");
        sb.AppendLine("EN: ");
    }

    private static string ExtractInlineText(XElement p)
    {
        var sb = new StringBuilder();
        foreach (var n in p.Nodes())
        {
            if (n is XText t)
                sb.Append(t.Value);
            else if (n is XElement e)
            {
                if (e.Name == Tei + "lb" || e.Name == Tei + "pb")
                    continue;
                if (e.Name == Tei + "note")
                {
                    var text = NormalizeSpace(e.Value);
                    if (!string.IsNullOrWhiteSpace(text))
                        sb.Append(" [NOTE:" + text + "]");
                    continue;
                }
                sb.Append(ExtractInlineText(e));
            }
        }
        return sb.ToString();
    }

    private static int ParseInt(string? s, int fallback) => int.TryParse(s, out var n) ? n : fallback;
    private static string NormalizeSpace(string s) => MultiWs.Replace(s ?? "", " ").Trim();
    private static string SerializeWithDeclaration(XDocument doc)
    {
        var body = doc.ToString();
        return doc.Declaration == null ? body : doc.Declaration + Environment.NewLine + body;
    }
    private static string PrefixFor(XNamespace ns) => ns == Tei ? "tei" : ns == Cb ? "cb" : "ns";
    private static XNamespace NamespaceForPrefix(string p) => p == "tei" ? Tei : p == "cb" ? Cb : XNamespace.None;

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
        if (doc.Root == null || string.IsNullOrWhiteSpace(path))
            return false;

        var segs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segs.Length == 0)
            return false;

        XElement current = doc.Root;
        for (int i = 0; i < segs.Length; i++)
        {
            var seg = segs[i];
            int open = seg.LastIndexOf('[');
            int close = seg.LastIndexOf(']');
            if (open <= 0 || close <= open + 1)
                return false;

            var nameToken = seg.Substring(0, open);
            if (!int.TryParse(seg.Substring(open + 1, close - open - 1), out var idx) || idx < 1)
                return false;

            var colon = nameToken.IndexOf(':');
            if (colon <= 0 || colon >= nameToken.Length - 1)
                return false;

            var prefix = nameToken.Substring(0, colon);
            var local = nameToken.Substring(colon + 1);
            var ns = NamespaceForPrefix(prefix);
            var qn = ns + local;

            if (i == 0)
            {
                if (current.Name != qn || idx != 1)
                    return false;
                continue;
            }

            var next = current.Elements(qn).Skip(idx - 1).FirstOrDefault();
            if (next == null)
                return false;
            current = next;
        }

        found = current;
        return true;
    }

    private sealed record MarkdownTranslationRow(string? XmlId, string? NodePath, string Zh, string En, int XmlRefLine, int ZhLine, int EnLine);

    public bool IsCurrentMarkdownFormat(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return false;

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        int start = ParseFrontmatterNoThrow(lines);
        if (start <= 0)
            return false;

        for (int i = 1; i < lines.Length && i < 80; i++)
        {
            var line = lines[i].Trim();
            if (line == "---")
                break;
            var m = FrontmatterPattern.Match(line);
            if (!m.Success)
                continue;
            if (string.Equals(m.Groups[1].Value, "format", StringComparison.OrdinalIgnoreCase))
                return string.Equals(m.Groups[2].Value.Trim(), CurrentFormat, StringComparison.Ordinal);
        }

        return false;
    }

    public bool TryExtractPdfSectionsFromMarkdown(
        string markdown,
        out List<string> chineseSections,
        out List<string> englishSections,
        out string? error)
    {
        chineseSections = new List<string>();
        englishSections = new List<string>();
        error = null;

        try
        {
            var rows = ParseMarkdownRows(markdown ?? "");
            foreach (var row in rows)
            {
                chineseSections.Add(row.Zh ?? string.Empty);
                englishSections.Add(row.En ?? string.Empty);
            }
            return chineseSections.Count > 0;
        }
        catch (MarkdownTranslationException ex)
        {
            error = ex.Message;
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static int ParseFrontmatterNoThrow(string[] lines)
    {
        if (lines.Length == 0 || lines[0].Trim() != "---")
            return 0;

        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "---")
                return i + 1;
        }
        return 0;
    }
}

public sealed class MarkdownTranslationException : Exception
{
    public int? LineNumber { get; }

    public MarkdownTranslationException(string message, int? lineNumber = null)
        : base(lineNumber.HasValue ? $"Line {lineNumber.Value}: {message}" : message)
    {
        LineNumber = lineNumber;
    }
}
