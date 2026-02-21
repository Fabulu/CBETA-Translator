// Services/MarkdownTranslationService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace CbetaTranslator.App.Services;

public sealed class MarkdownTranslationService
{
    public const string CurrentFormat = "cbeta-translation-md-v2";

    private static readonly Regex MultiWs = new(@"\s+", RegexOptions.Compiled);

    // Allow empty id in the capture; we validate later. This prevents "missing group" weirdness.
    private static readonly Regex XmlRefPattern =
        new(@"^<!--\s+xml-ref:\s+p\s+xml:id=([^\s]*)\s+-->\s*$", RegexOptions.Compiled);

    private static readonly Regex XmlRefNodePathPattern =
        new(@"^<!--\s+xml-ref:\s+node\s+path=([^\s]+)\s+-->\s*$", RegexOptions.Compiled);

    // NOTES (Option 2++):
    // Preferred:
    //   <!-- xml-note: id=abc123 xml-start=123 xml-end=130 -->
    // Legacy:
    //   <!-- xml-note: xml-index=123 -->
    private static readonly Regex XmlNoteRangePattern =
        new(@"^<!--\s+xml-note:\s+id=([^\s]+)\s+xml-start=(\d+)\s+xml-end=(\d+)\s+-->\s*$",
            RegexOptions.Compiled);

    private static readonly Regex XmlNoteLegacyIndexPattern =
        new(@"^<!--\s+xml-note:\s+xml-index=(\d+)\s+-->\s*$",
            RegexOptions.Compiled);

    private static readonly Regex FrontmatterPattern =
        new(@"^([A-Za-z0-9_\-]+):\s*(.*)$", RegexOptions.Compiled);

    private static readonly string[] BoundaryPrefixes =
    {
        "<!--",
        "<!-- xml-ref:",
        "<!-- xml-note:",
        "<!-- line:",
        "<!-- page:",
        "<!-- doc-meta:",
        "<!-- author:"
    };

    private static readonly XNamespace Tei = "http://www.tei-c.org/ns/1.0";
    private static readonly XNamespace Cb = "http://www.cbeta.org/ns/1.0";
    private static readonly XNamespace XmlNs = "http://www.w3.org/XML/1998/namespace";

    // Resp values we manage
    private const string RespMdImport = "md-import";
    private const string RespMdNote = "md-note";

    // Attributes for TEI persistence of range notes
    private const string AttrMdId = "md-id";
    private const string AttrTargetXmlStart = "target-xml-start";
    private const string AttrTargetXmlEnd = "target-xml-end";

    // ------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------

    public string ConvertTeiToMarkdown(string originalXml, string? sourceFileName)
    {
        if (string.IsNullOrWhiteSpace(originalXml))
            return "";

        var doc = XDocument.Parse(originalXml);

        var title = NormalizeSpace(
            (string?)doc.Descendants(Tei + "title").FirstOrDefault(t => (string?)t.Attribute("level") == "m")
            ?? (string?)doc.Descendants(Tei + "title").FirstOrDefault()
            ?? "Untitled");

        var docId = (string?)doc.Root?.Attribute(XmlNs + "id") ?? "";
        var cbetaId = NormalizeSpace((string?)doc.Descendants(Tei + "idno")
            .FirstOrDefault(e => (string?)e.Attribute("type") == "CBETA") ?? "");
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
        sb.AppendLine("- Keep ZH lines and xml-ref / xml-note comments unchanged.");
        sb.AppendLine("- One EN line maps to the preceding ZH line/paragraph.");
        sb.AppendLine("- Notes (range-based):");
        sb.AppendLine("  <!-- xml-note: id=... xml-start=... xml-end=... -->");
        sb.AppendLine("  NOTE: ...");
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
        if (originalXml == null) originalXml = "";
        if (markdown == null) markdown = "";

        var parsed = ParseMarkdown(markdown);
        var rows = parsed.TranslationRows;
        var notes = parsed.Notes;

        var enById = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.XmlId))
            .ToDictionary(r => r.XmlId!, r => r.En ?? "", StringComparer.Ordinal);

        var enByPath = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.NodePath))
            .ToDictionary(r => r.NodePath!, r => r.En ?? "", StringComparer.Ordinal);

        // Notes keyed by stable id (required to support multiples)
        var noteById = notes.ToDictionary(n => n.Id, n => n, StringComparer.Ordinal);

        // Important: we validate note ranges against a normalized-newline view.
        // We'll also compute host anchors from a doc parsed from the same normalized text to avoid CRLF drift.
        var xmlForIndexing = NormalizeNewlines(originalXml);

        var doc = XDocument.Parse(originalXml, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        var docIndex = XDocument.Parse(xmlForIndexing, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

        updatedCount = 0;

        // -------- validate source ids (and catch duplicates) --------
        var sourceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in doc.Descendants(Tei + "p"))
        {
            var xmlId = (string?)p.Attribute(XmlNs + "id");
            if (string.IsNullOrWhiteSpace(xmlId)) continue;

            if (!sourceIds.Add(xmlId))
                throw new MarkdownTranslationException($"Duplicate xml:id in source TEI: {xmlId}");
        }

        // -------- validate markdown references --------
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
                throw new MarkdownTranslationException(
                    $"Markdown references node path not found in source TEI: {row.NodePath}",
                    row.XmlRefLine);
        }

        // -------- validate note ranges --------
        foreach (var n in notes)
        {
            if (n.XmlStart < 0 || n.XmlEnd < 0)
                throw new MarkdownTranslationException("xml-note xml-start/xml-end must be >= 0.", n.XmlNoteLine);

            if (n.XmlStart > n.XmlEnd)
                throw new MarkdownTranslationException("xml-note xml-start must be <= xml-end.", n.XmlNoteLine);

            // allow == Length (end of doc)
            if (n.XmlStart > xmlForIndexing.Length || n.XmlEnd > xmlForIndexing.Length)
                throw new MarkdownTranslationException(
                    $"xml-note range is beyond end of XML (len={xmlForIndexing.Length}).",
                    n.XmlNoteLine);
        }

        // ============================================================
        // FULL RESYNC WITHOUT TOUCHING LEGACY NOTES:
        // - We ONLY create/update/delete notes with resp="md-import" OR resp="md-note"
        // - We do NOT overwrite other community notes and we do NOT delete Chinese notes.
        // ============================================================

        // -------- PASS 1: xml:id rows on <tei:p xml:id="..."> --------
        foreach (var p in doc.Descendants(Tei + "p"))
        {
            var xmlId = (string?)p.Attribute(XmlNs + "id");
            if (string.IsNullOrWhiteSpace(xmlId)) continue;

            var existingMdImport = p.Elements(Tei + "note").FirstOrDefault(n =>
                string.Equals((string?)n.Attribute("type"), "community", StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string?)n.Attribute(XmlNs + "lang"), "en", StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string?)n.Attribute("resp"), RespMdImport, StringComparison.OrdinalIgnoreCase));

            if (!enById.TryGetValue(xmlId, out var enTextRaw))
            {
                if (existingMdImport != null)
                    existingMdImport.Remove();
                continue;
            }

            var enText = (enTextRaw ?? "").Trim();

            if (!string.IsNullOrWhiteSpace(enText))
            {
                if (existingMdImport == null)
                {
                    p.Add(new XElement(Tei + "note",
                        new XAttribute("type", "community"),
                        new XAttribute("resp", RespMdImport),
                        new XAttribute(XmlNs + "lang", "en"),
                        enText));
                }
                else
                {
                    existingMdImport.Value = enText;
                }

                updatedCount++;
            }
            else
            {
                if (existingMdImport != null)
                    existingMdImport.Remove();
            }
        }

        // -------- PASS 2: node-path rows --------
        var allPathNotes = doc.Descendants(Tei + "note")
            .Where(n =>
                string.Equals((string?)n.Attribute("type"), "community", StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string?)n.Attribute(XmlNs + "lang"), "en", StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string?)n.Attribute("resp"), RespMdImport, StringComparison.OrdinalIgnoreCase) &&
                n.Attribute("target-path") != null)
            .ToList();

        foreach (var n in allPathNotes)
        {
            var path = (string?)n.Attribute("target-path");
            if (string.IsNullOrWhiteSpace(path)) continue;

            if (!enByPath.ContainsKey(path))
                n.Remove();
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
                    string.Equals((string?)n.Attribute(XmlNs + "lang"), "en", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((string?)n.Attribute("resp"), RespMdImport, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((string?)n.Attribute("target-path"), kv.Key, StringComparison.Ordinal));

            if (!string.IsNullOrWhiteSpace(enText))
            {
                if (existingPathNote == null)
                {
                    target.AddAfterSelf(new XElement(Tei + "note",
                        new XAttribute("type", "community"),
                        new XAttribute("resp", RespMdImport),
                        new XAttribute(XmlNs + "lang", "en"),
                        new XAttribute("target-path", kv.Key),
                        enText));
                }
                else
                {
                    existingPathNote.Value = enText;
                }

                updatedCount++;
            }
            else
            {
                if (existingPathNote != null)
                    existingPathNote.Remove();
            }
        }

        // -------- PASS 3: range notes (resp="md-note", md-id="...") --------
        // Full resync: remove md-note notes whose md-id is not in markdown.
        var allMdNotes = doc.Descendants(Tei + "note")
            .Where(n =>
                string.Equals((string?)n.Attribute("type"), "community", StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string?)n.Attribute("resp"), RespMdNote, StringComparison.OrdinalIgnoreCase) &&
                n.Attribute(AttrMdId) != null)
            .ToList();

        foreach (var n in allMdNotes)
        {
            var id = (string?)n.Attribute(AttrMdId);
            if (string.IsNullOrWhiteSpace(id) || !noteById.ContainsKey(id))
                n.Remove();
        }

        if (notes.Count > 0)
        {
            var lineOffsets = BuildLineStartOffsets(xmlForIndexing);

            // Candidates and anchors must match xmlForIndexing (LF) to avoid CRLF drift.
            var bodyElIndex = docIndex.Descendants(Tei + "body").FirstOrDefault();
            var candidates = bodyElIndex == null
                ? new List<HostCandidate>()
                : BuildCandidateHostsByPath(bodyElIndex, lineOffsets);

            var bodyEl = doc.Descendants(Tei + "body").FirstOrDefault();

            foreach (var note in notes)
            {
                var id = note.Id;
                var text = (note.Note.Text ?? "").Trim();

                // Find existing managed note by md-id
                var existing = doc.Descendants(Tei + "note").FirstOrDefault(n =>
                    string.Equals((string?)n.Attribute("type"), "community", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((string?)n.Attribute("resp"), RespMdNote, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((string?)n.Attribute(AttrMdId), id, StringComparison.Ordinal));

                if (string.IsNullOrWhiteSpace(text))
                {
                    if (existing != null)
                        existing.Remove();
                    continue;
                }

                if (existing != null)
                {
                    existing.SetAttributeValue(AttrTargetXmlStart, note.XmlStart.ToString());
                    existing.SetAttributeValue(AttrTargetXmlEnd, note.XmlEnd.ToString());
                    existing.Value = text;
                    updatedCount++;
                    continue;
                }

                var noteEl = new XElement(Tei + "note",
                    new XAttribute("type", "community"),
                    new XAttribute("resp", RespMdNote),
                    new XAttribute(AttrMdId, id),
                    new XAttribute(AttrTargetXmlStart, note.XmlStart.ToString()),
                    new XAttribute(AttrTargetXmlEnd, note.XmlEnd.ToString()),
                    new XAttribute(XmlNs + "lang", "en"),
                    text);

                if (bodyEl == null)
                {
                    doc.Root?.Add(noteEl);
                    updatedCount++;
                    continue;
                }

                // Best-possible structure-aware anchor insertion near the selection start.
                var hostPath = FindHostPathForXmlIndex(candidates, note.XmlStart);

                if (!string.IsNullOrWhiteSpace(hostPath) &&
                    TryFindNodeByPath(doc, hostPath!, out var host) &&
                    host != null &&
                    host != bodyEl &&
                    !IsInsideNote(host))
                {
                    host.AddAfterSelf(noteEl);
                }
                else
                {
                    // Fallback: append to body (still inside body; renderer will pick it up)
                    bodyEl.Add(noteEl);
                }

                updatedCount++;
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
                string.Equals((string?)n.Attribute(XmlNs + "lang"), "en", StringComparison.OrdinalIgnoreCase) &&
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
                string.Equals((string?)n.Attribute(XmlNs + "lang"), "en", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace((string?)n.Attribute("target-path")) &&
                // skip md-note; it is not a paragraph translation replacement
                !string.Equals((string?)n.Attribute("resp"), RespMdNote, StringComparison.OrdinalIgnoreCase));

            if (note == null)
                continue;

            var enText = (note.Value ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(enText))
                ReplaceElementTextPreserveStructure(p, enText);

            note.Remove();
        }

        return SerializeWithDeclaration(doc);
    }

    public bool IsCurrentMarkdownFormat(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return false;

        var lines = (markdown ?? "").Replace("\r\n", "\n").Split('\n');
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
            var parsed = ParseMarkdown(markdown ?? "");
            foreach (var row in parsed.TranslationRows)
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

    // ------------------------------------------------------------
    // Internal helpers
    // ------------------------------------------------------------

    private static bool IsInsideNote(XElement el) => el.Ancestors(Tei + "note").Any();

    private static void ReplaceElementTextPreserveStructure(XElement element, string replacementText)
    {
        if (element == null)
            return;

        replacementText ??= "";

        // Collect all text nodes under this element (including itself)
        var textNodes = element
            .DescendantNodesAndSelf()
            .OfType<XText>()
            .ToList();

        if (textNodes.Count == 0)
        {
            // No existing text nodes -> just insert at start
            element.AddFirst(new XText(replacementText));
            return;
        }

        // Remember where the FIRST text node lived, so we can insert replacement at the same position.
        var first = textNodes[0];
        var insertionAnchor = first.NextNode; // may be null
        var insertionParent = first.Parent;   // should not be null, but guard anyway

        // Remove all text nodes (this preserves all element nodes / structure)
        foreach (var t in textNodes)
            t.Remove();

        // Insert the replacement at the original location of the first text node
        if (insertionParent == null)
        {
            element.AddFirst(new XText(replacementText));
            return;
        }

        if (insertionAnchor != null)
            insertionAnchor.AddBeforeSelf(new XText(replacementText));
        else
            insertionParent.Add(new XText(replacementText));
    }

    private static string NormalizeSpace(string s) => MultiWs.Replace(s ?? "", " ").Trim();

    // We want stable newlines because xml-start/xml-end are computed against LF-normalized text.
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
        using (var sw = new System.IO.StringWriter(sb))
        using (var xw = XmlWriter.Create(sw, settings))
        {
            doc.Save(xw);
        }

        return sb.ToString();
    }

    // ------------------------------------------------------------
    // MARKDOWN PARSING
    // ------------------------------------------------------------

    private sealed record ParsedMarkdown(
        List<MarkdownTranslationRow> TranslationRows,
        List<MarkdownNoteRow> Notes);

    private static ParsedMarkdown ParseMarkdown(string markdown)
    {
        var rows = new List<MarkdownTranslationRow>();
        var notes = new List<MarkdownNoteRow>();

        var lines = (markdown ?? "").Replace("\r\n", "\n").Split('\n');
        var start = ParseFrontmatter(lines);

        var rowById = new Dictionary<string, MarkdownTranslationRow>(StringComparer.Ordinal);
        var rowByPath = new Dictionary<string, MarkdownTranslationRow>(StringComparer.Ordinal);

        // Note IDs MUST be unique.
        var noteById = new Dictionary<string, MarkdownNoteRow>(StringComparer.Ordinal);

        // For legacy xml-index blocks, we allow them, but duplicates at same index are ambiguous without IDs.
        var legacyIndexSeen = new Dictionary<int, int>(); // index -> count

        for (int i = start; i < lines.Length; i++)
        {
            var sLine = lines[i].Trim();

            var m = XmlRefPattern.Match(sLine);
            var mp = XmlRefNodePathPattern.Match(sLine);

            var mnRange = XmlNoteRangePattern.Match(sLine);
            var mnLegacy = XmlNoteLegacyIndexPattern.Match(sLine);

            if (!m.Success && !mp.Success && !mnRange.Success && !mnLegacy.Success)
                continue;

            // ---- xml-note (range) ----
            if (mnRange.Success)
            {
                int xmlNoteLine = i + 1;

                var id = mnRange.Groups[1].Value.Trim();
                if (string.IsNullOrWhiteSpace(id))
                    throw new MarkdownTranslationException("xml-note id cannot be empty.", xmlNoteLine);

                if (!int.TryParse(mnRange.Groups[2].Value, out var xmlStart) ||
                    !int.TryParse(mnRange.Groups[3].Value, out var xmlEnd))
                    throw new MarkdownTranslationException("Invalid xml-start/xml-end in xml-note block.", xmlNoteLine);

                var noteText = ParseNoteText(lines, ref i, xmlNoteLine, context: $"id={id}");

                var row = new MarkdownNoteRow(
                    Id: id,
                    XmlStart: xmlStart,
                    XmlEnd: xmlEnd,
                    Note: noteText,
                    XmlNoteLine: xmlNoteLine,
                    NoteLine: noteText.NoteLine);

                if (noteById.TryGetValue(id, out var existing))
                {
                    throw new MarkdownTranslationException(
                        $"Duplicate xml-note id in markdown: {id} (first at line {existing.XmlNoteLine})",
                        xmlNoteLine);
                }

                noteById[id] = row;
                notes.Add(row);
                continue;
            }

            // ---- xml-note (legacy xml-index) ----
            if (mnLegacy.Success)
            {
                int xmlNoteLine = i + 1;

                if (!int.TryParse(mnLegacy.Groups[1].Value, out var xmlIndex))
                    throw new MarkdownTranslationException("Invalid xml-index in legacy xml-note block.", xmlNoteLine);

                int count = legacyIndexSeen.TryGetValue(xmlIndex, out var c) ? (c + 1) : 1;
                legacyIndexSeen[xmlIndex] = count;

                // If there is more than one legacy note at same index, we refuse (no stable identity).
                if (count > 1)
                {
                    throw new MarkdownTranslationException(
                        $"Multiple legacy xml-note blocks found for xml-index={xmlIndex}. Add stable ids and ranges:\n" +
                        $"<!-- xml-note: id=... xml-start={xmlIndex} xml-end={xmlIndex} -->",
                        xmlNoteLine);
                }

                var parsedNote = ParseNoteText(lines, ref i, xmlNoteLine, context: $"xml-index={xmlIndex}");

                // Generate a stable-ish id for migration (based on location + line; not perfect but one-time)
                var id = $"legacy:{xmlIndex}:{xmlNoteLine}";

                var row = new MarkdownNoteRow(
                    Id: id,
                    XmlStart: xmlIndex,
                    XmlEnd: xmlIndex,
                    Note: parsedNote,
                    XmlNoteLine: xmlNoteLine,
                    NoteLine: parsedNote.NoteLine);

                if (noteById.TryGetValue(id, out var existing))
                {
                    throw new MarkdownTranslationException(
                        $"Duplicate legacy xml-note id generated unexpectedly: {id} (first at line {existing.XmlNoteLine})",
                        xmlNoteLine);
                }

                noteById[id] = row;
                notes.Add(row);
                continue;
            }

            // ---- xml-ref translation blocks ----
            {
                var xmlRefLine = i + 1;
                var xmlIdRaw = m.Success ? m.Groups[1].Value : null;
                var xmlId = string.IsNullOrWhiteSpace(xmlIdRaw) ? null : xmlIdRaw.Trim();
                var nodePath = mp.Success ? mp.Groups[1].Value.Trim() : null;

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

                    if (XmlRefPattern.IsMatch(s) || XmlRefNodePathPattern.IsMatch(s) ||
                        XmlNoteRangePattern.IsMatch(s) || XmlNoteLegacyIndexPattern.IsMatch(s))
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

                            if (XmlRefPattern.IsMatch(nt) || XmlRefNodePathPattern.IsMatch(nt) ||
                                XmlNoteRangePattern.IsMatch(nt) || XmlNoteLegacyIndexPattern.IsMatch(nt) ||
                                IsBoundary(nt))
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
        }

        return new ParsedMarkdown(rows, notes);
    }

    private sealed record ParsedNote(string Text, int NoteLine);

    private static ParsedNote ParseNoteText(string[] lines, ref int i, int xmlNoteLine, string context)
    {
        bool sawNote = false;
        int noteLine = -1;
        var noteLines = new List<string>();

        for (i = i + 1; i < lines.Length; i++)
        {
            var cur = lines[i];
            var s = cur.Trim();

            if (XmlRefPattern.IsMatch(s) || XmlRefNodePathPattern.IsMatch(s) ||
                XmlNoteRangePattern.IsMatch(s) || XmlNoteLegacyIndexPattern.IsMatch(s))
            {
                i--;
                break;
            }

            if (IsBoundary(s))
            {
                i--;
                break;
            }

            if (!sawNote)
            {
                if (s.StartsWith("NOTE:", StringComparison.Ordinal))
                {
                    sawNote = true;
                    noteLine = i + 1;

                    var first = s.Substring(5).TrimStart();
                    if (!string.IsNullOrEmpty(first))
                        noteLines.Add(first);

                    for (i = i + 1; i < lines.Length; i++)
                    {
                        var n = lines[i];
                        var nt = n.Trim();

                        if (XmlRefPattern.IsMatch(nt) || XmlRefNodePathPattern.IsMatch(nt) ||
                            XmlNoteRangePattern.IsMatch(nt) || XmlNoteLegacyIndexPattern.IsMatch(nt) ||
                            IsBoundary(nt))
                        {
                            i--;
                            break;
                        }

                        noteLines.Add(n.TrimEnd());
                    }

                    continue;
                }

                if (string.IsNullOrWhiteSpace(s))
                    continue;

                throw new MarkdownTranslationException(
                    $"Unexpected content before NOTE line for xml-note ({context}): '{s}'",
                    i + 1);
            }

            noteLines.Add(cur.TrimEnd());
        }

        if (!sawNote)
            throw new MarkdownTranslationException($"Missing NOTE line for xml-note ({context}).", xmlNoteLine);

        var noteText = string.Join("\n", noteLines).Trim('\n');
        return new ParsedNote(noteText, noteLine);
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

    // ------------------------------------------------------------
    // TEI -> MARKDOWN node writer
    // ------------------------------------------------------------

    private static void WriteNode(XNode node, StringBuilder sb)
    {
        if (node is XText)
            return;

        if (node is not XElement e)
            return;

        if (e.Name == Tei + "pb") return;
        if (e.Name == Tei + "lb") return;
        if (e.Name == Tei + "milestone") return;

        // Emit managed range-notes (resp="md-note") into markdown as xml-note blocks
        if (e.Name == Tei + "note" &&
            string.Equals((string?)e.Attribute("type"), "community", StringComparison.OrdinalIgnoreCase) &&
            string.Equals((string?)e.Attribute("resp"), RespMdNote, StringComparison.OrdinalIgnoreCase))
        {
            var id = ((string?)e.Attribute(AttrMdId))?.Trim();
            var sStart = ((string?)e.Attribute(AttrTargetXmlStart))?.Trim();
            var sEnd = ((string?)e.Attribute(AttrTargetXmlEnd))?.Trim();

            if (!string.IsNullOrWhiteSpace(id) &&
                int.TryParse(sStart, out var xmlStart) &&
                int.TryParse(sEnd, out var xmlEnd))
            {
                var noteText = NormalizeSpace(e.Value ?? "");
                sb.AppendLine();
                sb.AppendLine($"<!-- xml-note: id={id} xml-start={xmlStart} xml-end={xmlEnd} -->");
                sb.AppendLine($"NOTE: {noteText}");
                return;
            }

            // malformed managed note -> skip exporting it
            return;
        }

        // Skip imported English translation notes in markdown export
        if (e.Name == Tei + "note" &&
            string.Equals((string?)e.Attribute("type"), "community", StringComparison.OrdinalIgnoreCase) &&
            string.Equals((string?)e.Attribute(XmlNs + "lang"), "en", StringComparison.OrdinalIgnoreCase) &&
            string.Equals((string?)e.Attribute("resp"), RespMdImport, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

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
            if (string.Equals(fun, "open", StringComparison.OrdinalIgnoreCase))
            {
                var jhead = NormalizeSpace((string?)e.Element(Cb + "jhead") ?? "");
                if (!string.IsNullOrWhiteSpace(jhead))
                {
                    sb.AppendLine();
                    sb.AppendLine($"## {jhead}");

                    var jheadEl = e.Element(Cb + "jhead");
                    if (jheadEl != null)
                        EmitNodeTextBlock(jheadEl, sb);
                }
            }
            return;
        }

        if (e.Name == Tei + "byline")
        {
            EmitNodeTextBlock(e, sb);
            return;
        }

        // IMPORTANT FIX:
        // Some TEI <p> do not have xml:id.
        // If we emit an empty xml:id, markdown parsing will throw.
        // So: use xml:id when present; otherwise fall back to stable node-path.
        if (e.Name == Tei + "p")
        {
            var xmlId = ((string?)e.Attribute(XmlNs + "id"))?.Trim();
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(xmlId))
                sb.AppendLine($"<!-- xml-ref: p xml:id={xmlId} -->");
            else
                sb.AppendLine($"<!-- xml-ref: node path={BuildNodePath(e)} -->");

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
            if (e.Name == Tei + "note" &&
                string.Equals((string?)e.Attribute("type"), "community", StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string?)e.Attribute(XmlNs + "lang"), "en", StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string?)e.Attribute("resp"), RespMdImport, StringComparison.OrdinalIgnoreCase))
                return false;

            if (e.Name == Tei + "note" &&
                string.Equals((string?)e.Attribute("type"), "community", StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string?)e.Attribute("resp"), RespMdNote, StringComparison.OrdinalIgnoreCase))
                return false;

            return !string.IsNullOrWhiteSpace(NormalizeSpace(ExtractInlineText(e)));
        }

        if (e.Name == Tei + "list" || e.Name == Cb + "div")
            return false;

        bool hasOwnText = e.Nodes().OfType<XText>().Any(t => !string.IsNullOrWhiteSpace(t.Value));
        return hasOwnText;
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

    // ------------------------------------------------------------
    // Node paths
    // ------------------------------------------------------------

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

    // ------------------------------------------------------------
    // XML-INDEX HOST MAPPING (for md-note persistence)
    // ------------------------------------------------------------

    private static string NormalizeNewlines(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    private static int[] BuildLineStartOffsets(string sLf)
    {
        var starts = new List<int>(capacity: 2048) { 0 };
        for (int i = 0; i < sLf.Length; i++)
        {
            if (sLf[i] == '\n')
                starts.Add(i + 1);
        }
        return starts.ToArray();
    }

    private sealed record HostCandidate(string Path, int AbsStart, string Kind, int Depth);

    // Build candidates as (path, absStart, kind, depth) from a document whose line/pos match the string we indexed (LF normalized).
    private static List<HostCandidate> BuildCandidateHostsByPath(XElement bodyEl, int[] lineStartOffsets)
    {
        var list = new List<HostCandidate>(capacity: 4096);

        foreach (var el in bodyEl.DescendantsAndSelf())
        {
            // Never anchor inside notes
            if (el.Name == Tei + "note")
                continue;

            // (Should be redundant since we start at bodyEl, but keep it bulletproof)
            if (el.Ancestors(Tei + "teiHeader").Any() || el.Ancestors(Tei + "back").Any())
                continue;

            if (el is not IXmlLineInfo li || !li.HasLineInfo())
                continue;

            int line = li.LineNumber;
            int pos = li.LinePosition;

            if (line <= 0 || pos <= 0) continue;
            if (line - 1 >= lineStartOffsets.Length) continue;

            int abs = lineStartOffsets[line - 1] + (pos - 1);
            if (abs < 0) continue;

            var path = BuildNodePath(el);
            int depth = path.Count(c => c == '/');

            string kind =
                el.Name == Tei + "lb" ? "tei:lb" :
                el.Name == Tei + "p" ? "tei:p" :
                el.Name == Tei + "head" ? "tei:head" :
                el.Name == Tei + "item" ? "tei:item" :
                el.Name == Tei + "ab" ? "tei:ab" :
                el.Name == Cb + "jhead" ? "cb:jhead" :
                el.Name == Cb + "juan" ? "cb:juan" :
                el.Name.Namespace == Tei ? $"tei:{el.Name.LocalName}" :
                el.Name.Namespace == Cb ? $"cb:{el.Name.LocalName}" :
                el.Name.LocalName;

            list.Add(new HostCandidate(path, abs, kind, depth));
        }

        list.Sort((a, b) => a.AbsStart.CompareTo(b.AbsStart));
        return list;
    }

    // Score-based “best possible” anchor selection:
    // - Prefer anchors at/just before the selection.
    // - Strongly prefer lb, then p/head/item/ab, etc.
    // - Prefer deeper (more specific) paths if distance is similar.
    // - Penalize anchors after the selection heavily.
    private static string? FindHostPathForXmlIndex(List<HostCandidate> candidates, int xmlIndex)
    {
        if (candidates.Count == 0)
            return null;

        int lo = 0, hi = candidates.Count - 1;
        int bestIdx = -1;

        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) / 2);
            int v = candidates[mid].AbsStart;

            if (v <= xmlIndex)
            {
                bestIdx = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        if (bestIdx < 0)
            bestIdx = 0;

        int start = Math.Max(0, bestIdx - 250);
        int end = Math.Min(candidates.Count - 1, bestIdx + 50);

        HostCandidate chosen = candidates[bestIdx];
        long bestScore = long.MinValue;

        for (int i = start; i <= end; i++)
        {
            var c = candidates[i];

            int delta = xmlIndex - c.AbsStart;
            bool isAfter = delta < 0;

            int dist = Math.Abs(delta);
            long distPenalty = dist;

            if (isAfter)
                distPenalty += 2000; // "after" should almost never win

            int kindWeight = c.Kind switch
            {
                "tei:lb" => 9000,
                "tei:p" => 7000,
                "tei:head" => 6500,
                "tei:item" => 6000,
                "tei:ab" => 5500,
                "cb:jhead" => 5200,
                "cb:juan" => 4000,
                _ => 1000
            };

            int depthBonus = Math.Min(2000, c.Depth * 40);
            int sideBonus = isAfter ? -1500 : 0;

            long score = (long)kindWeight + depthBonus + sideBonus - distPenalty;

            if (score > bestScore)
            {
                bestScore = score;
                chosen = c;
            }
        }

        return chosen.Path;
    }

    // ------------------------------------------------------------
    // Models
    // ------------------------------------------------------------

    private sealed record MarkdownTranslationRow(
        string? XmlId,
        string? NodePath,
        string Zh,
        string En,
        int XmlRefLine,
        int ZhLine,
        int EnLine);

    private sealed record MarkdownNoteRow(
        string Id,
        int XmlStart,
        int XmlEnd,
        ParsedNote Note,
        int XmlNoteLine,
        int NoteLine);
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