// Services/IndexedTranslationService.cs
using System;
using System.Collections.Generic;
using System.IO;
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

public enum TranslationSegmentKind
{
    Text,            // original text chunk
    LineBreakTag,    // <lb/>
    PreservedElement // <pb/>, <anchor/>, <g>, <note>, etc.
}

public sealed class TranslationSegment
{
    public TranslationSegmentKind Kind { get; set; }

    // For text segments
    public string Text { get; set; } = "";

    // For preserved element / lb segments (kept as XElement clone, NOT raw XML string)
    public XElement? ElementTemplate { get; set; }

    public string? ElementName { get; set; }
    public string? ElementXmlId { get; set; }

    // 1-based line index inside parent element
    public int LineIndex { get; set; } = 1;

    // Hide from user-facing projection text
    public bool HideInProjection { get; set; }

    // Move to line end on rebuild (e.g. note anchors / inline notes)
    public bool MoveToLineEnd { get; set; }

    // Visible contribution for preserved inline elements (e.g. <g>𡎖</g>) when safe
    public string VisibleText { get; set; } = "";
}

public sealed class TranslationUnit
{
    // UI numbering (assigned at render-time)
    public int Index { get; set; }

    // Stable identity for THIS LINE (e.g. id:pT48...#L3)
    public string StableKey { get; set; } = "";

    // Parent element identity (used to group line units back together)
    public string ElementStableKey { get; set; } = "";
    public string ElementNodePath { get; set; } = "";
    public string? ElementXmlId { get; set; }

    // 1-based line number inside parent element
    public int LineNumber { get; set; }

    public TranslationUnitKind Kind { get; set; }
    public bool IsParagraph { get; set; }

    // ORIGINAL line template (structure + preserved tags)
    public List<TranslationSegment> LineSegments { get; } = new();

    // ORIGINAL line ended with <lb/> ?
    public XElement? TrailingLbTemplate { get; set; }

    // Projection strings
    public string Zh { get; set; } = ""; // always from ORIGINAL
    public string En { get; set; } = ""; // from translated version if available (blanked if same as ZH)
    public string EnBaseline { get; set; } = "";

    // True only when user actually edited this line in the projection
    public bool IsDirty { get; set; }
}

public sealed class IndexedTranslationDocument
{
    public string OriginalXml { get; set; } = "";
    public string TranslatedXml { get; set; } = "";
    public bool HasSeparateTranslatedSource { get; set; }

    public List<TranslationUnit> Units { get; } = new();
}

public sealed class IndexedTranslationService
{
    private static readonly XNamespace Tei = "http://www.tei-c.org/ns/1.0";
    private static readonly XNamespace Cb = "http://www.cbeta.org/ns/1.0";
    private static readonly XNamespace XmlNs = "http://www.w3.org/XML/1998/namespace";

    // Debug dump from the last BuildTranslatedXml call (success or failure path up to crash point)
    public string LastBuildTranslatedXmlDebugDump { get; private set; } = "";

    // Where the dump was last written (best effort)
    public string LastBuildTranslatedXmlDebugDumpPath { get; private set; } = "";

    // Turn this on to validate after each patched element (slower, but pinpoints corruption source)
    private const bool ValidateAfterEachPatchedGroup = true;

    // ============================================================
    // BUILD INDEX
    // ============================================================

    public IndexedTranslationDocument BuildIndex(string originalXml, string? translatedXml)
    {
        originalXml ??= "";
        translatedXml = string.IsNullOrWhiteSpace(translatedXml) ? originalXml : translatedXml;

        var result = new IndexedTranslationDocument
        {
            OriginalXml = originalXml,
            TranslatedXml = translatedXml,
            HasSeparateTranslatedSource = !XmlEquivalent(originalXml, translatedXml)
        };

        if (string.IsNullOrWhiteSpace(originalXml))
            return result;

        var origDoc = XDocument.Parse(originalXml, LoadOptions.PreserveWhitespace);
        var tranDoc = XDocument.Parse(translatedXml, LoadOptions.PreserveWhitespace);

        var tranLookup = BuildDocLookup(tranDoc);

        // 1) teiHeader -> Head mode
        var header = origDoc.Root?.Element(Tei + "teiHeader");
        if (header != null)
        {
            foreach (var el in header.DescendantsAndSelf())
            {
                if (!IsTranslatableHeaderElement(el)) continue;

                var translatedMatch = FindTranslatedMatch(el, tranLookup);
                AddElementAsLineUnits(
                    result,
                    origEl: el,
                    translatedEl: translatedMatch,
                    kind: TranslationUnitKind.Head,
                    includeInlineNotesInProjection: false);
            }
        }

        // 2) body -> Head / Body / Note
        var body = origDoc.Descendants(Tei + "body").FirstOrDefault();
        if (body != null)
        {
            foreach (var el in body.DescendantsAndSelf())
            {
                if (!IsTranslatableBodyElement(el)) continue;

                var kind = ClassifyBodyElement(el);
                var translatedMatch = FindTranslatedMatch(el, tranLookup);

                // In body/head mode hide inline <note>; if unit itself is <note>, show it
                bool includeInlineNotes = kind == TranslationUnitKind.Note;

                AddElementAsLineUnits(
                    result,
                    origEl: el,
                    translatedEl: translatedMatch,
                    kind: kind,
                    includeInlineNotesInProjection: includeInlineNotes);
            }
        }

        // 3) back -> Notes mode
        var back = origDoc.Descendants(Tei + "back").FirstOrDefault();
        if (back != null)
        {
            foreach (var el in back.DescendantsAndSelf())
            {
                if (!IsTranslatableBackElement(el)) continue;

                var translatedMatch = FindTranslatedMatch(el, tranLookup);

                AddElementAsLineUnits(
                    result,
                    origEl: el,
                    translatedEl: translatedMatch,
                    kind: TranslationUnitKind.Note,
                    includeInlineNotesInProjection: true);
            }
        }

        return result;
    }

    private void AddElementAsLineUnits(
        IndexedTranslationDocument doc,
        XElement origEl,
        XElement? translatedEl,
        TranslationUnitKind kind,
        bool includeInlineNotesInProjection)
    {
        var elementNodePath = BuildNodePath(origEl);
        var elementXmlId = (string?)origEl.Attribute(XmlNs + "id");
        var elementStableKey = !string.IsNullOrWhiteSpace(elementXmlId)
            ? "id:" + elementXmlId!.Trim()
            : "path:" + elementNodePath;

        // ORIGINAL = source of truth for structure + ZH
        var origSegments = new List<TranslationSegment>();
        BuildSegmentsFromElement(origEl, origSegments, includeInlineNotesInProjection);
        var zhLines = BuildVisibleLines(origSegments);

        // TRANSLATED (if separate file exists) = source of EN
        List<string> enLines;
        if (doc.HasSeparateTranslatedSource && translatedEl != null)
        {
            var tranSegments = new List<TranslationSegment>();
            BuildSegmentsFromElement(translatedEl, tranSegments, includeInlineNotesInProjection);
            enLines = BuildVisibleLines(tranSegments);
        }
        else
        {
            enLines = new List<string>();
        }

        AlignLineCount(enLines, zhLines.Count);

        // If translated line is equal to ZH, show blank EN in projection.
        for (int i = 0; i < zhLines.Count && i < enLines.Count; i++)
        {
            if (string.Equals(enLines[i], zhLines[i], StringComparison.Ordinal))
                enLines[i] = "";
        }

        // Split ORIGINAL segments into line templates
        var lineTemplates = SplitLineTemplates(origSegments);

        int lineCount = Math.Max(zhLines.Count, lineTemplates.Keys.DefaultIfEmpty(0).Max());
        if (lineCount == 0) lineCount = 1;

        for (int lineNo = 1; lineNo <= lineCount; lineNo++)
        {
            lineTemplates.TryGetValue(lineNo, out var tpl);
            tpl ??= new LineTemplate();

            string zh = lineNo - 1 < zhLines.Count ? zhLines[lineNo - 1] : "";
            string en = lineNo - 1 < enLines.Count ? enLines[lineNo - 1] : "";

            bool hasStructure = tpl.LineSegments.Count > 0 || tpl.TrailingLbTemplate != null;
            if (!hasStructure && string.IsNullOrWhiteSpace(zh) && string.IsNullOrWhiteSpace(en))
                continue;

            var unit = new TranslationUnit
            {
                ElementXmlId = elementXmlId,
                ElementNodePath = elementNodePath,
                ElementStableKey = elementStableKey,
                StableKey = $"{elementStableKey}#L{lineNo}",
                LineNumber = lineNo,
                Kind = kind,
                IsParagraph = origEl.Name == Tei + "p",
                Zh = zh,
                En = en,
                EnBaseline = en,
                IsDirty = false
            };

            foreach (var seg in tpl.LineSegments)
                unit.LineSegments.Add(CloneSegment(seg));

            if (tpl.TrailingLbTemplate != null)
                unit.TrailingLbTemplate = new XElement(tpl.TrailingLbTemplate);

            doc.Units.Add(unit);
        }
    }

    // ============================================================
    // RENDER PROJECTION (one block = one line)
    // ============================================================

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
        sb.AppendLine("# Edit ONLY EN: lines.");
        sb.AppendLine("# Do NOT change <n> or ZH: lines.");
        sb.AppendLine("# One block = one source line.");
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

    // ============================================================
    // APPLY PROJECTION EDITS
    // ============================================================

    public void ApplyProjectionEdits(IndexedTranslationDocument doc, TranslationEditMode mode, string editedText)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));
        editedText ??= "";

        var parsedBlocks = ParseProjection(editedText);

        var wantedKind = mode switch
        {
            TranslationEditMode.Head => TranslationUnitKind.Head,
            TranslationEditMode.Body => TranslationUnitKind.Body,
            TranslationEditMode.Notes => TranslationUnitKind.Note,
            _ => TranslationUnitKind.Body
        };

        var targetUnits = doc.Units.Where(u => u.Kind == wantedKind).ToList();

        foreach (var block in parsedBlocks)
        {
            int unitIndex = block.BlockNumber - 1;
            if (unitIndex < 0 || unitIndex >= targetUnits.Count)
                throw new InvalidOperationException($"Block <{block.BlockNumber}> is out of range.");

            var unit = targetUnits[unitIndex];

            if (!string.Equals(block.Zh, unit.Zh, StringComparison.Ordinal))
                throw new InvalidOperationException($"Block <{block.BlockNumber}> ZH was changed. Only EN may be edited.");

            ValidateEnglish(block.En, block.BlockNumber);

            if (!string.Equals(unit.En, block.En, StringComparison.Ordinal))
            {
                unit.En = block.En;
                unit.IsDirty = true;
            }
        }
    }

    // ============================================================
    // BUILD TRANSLATED XML (SAFE INLINE WRITE-BACK)
    // ============================================================

    public string BuildTranslatedXml(IndexedTranslationDocument doc, out int updatedCount)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));

        LastBuildTranslatedXmlDebugDump = "";
        LastBuildTranslatedXmlDebugDumpPath = "";

        var dbg = new StringBuilder(32_000);
        dbg.AppendLine("=== BuildTranslatedXml Debug ===");
        dbg.AppendLine($"TimeUtc: {DateTime.UtcNow:O}");
        dbg.AppendLine($"HasSeparateTranslatedSource: {doc.HasSeparateTranslatedSource}");
        dbg.AppendLine($"Units total: {doc.Units.Count}");
        dbg.AppendLine($"Dirty units total: {doc.Units.Count(u => u.IsDirty)}");
        dbg.AppendLine();

        var tranDoc = XDocument.Parse(doc.TranslatedXml, LoadOptions.PreserveWhitespace);
        var tranLookup = BuildDocLookup(tranDoc);

        updatedCount = 0;

        var groups = doc.Units.GroupBy(u => u.ElementStableKey).ToList();
        dbg.AppendLine($"Element groups total: {groups.Count}");
        dbg.AppendLine();

        int skippedNoDirtyGroups = 0;
        int skippedTargetMissingGroups = 0;
        int skippedUnsafeGroups = 0;

        int groupIx = 0;
        foreach (var group in groups)
        {
            groupIx++;
            var first = group.First();
            var orderedLines = group.OrderBy(u => u.LineNumber).ToList();
            bool anyDirty = orderedLines.Any(u => u.IsDirty);

            dbg.AppendLine($"--- Group {groupIx}/{groups.Count} ---");
            dbg.AppendLine($"ElementStableKey: {first.ElementStableKey}");
            dbg.AppendLine($"Kind: {first.Kind}");
            dbg.AppendLine($"XmlId: {first.ElementXmlId ?? "(none)"}");
            dbg.AppendLine($"NodePath: {first.ElementNodePath}");
            dbg.AppendLine($"Lines: {orderedLines.Count}");
            dbg.AppendLine($"AnyDirty: {anyDirty}");

            if (!anyDirty)
            {
                skippedNoDirtyGroups++;
                dbg.AppendLine("SKIP: No dirty lines in this element group.");
                dbg.AppendLine();
                continue;
            }

            var target = FindByIdentity(first.ElementXmlId, first.ElementNodePath, tranLookup);
            if (target == null)
            {
                skippedTargetMissingGroups++;
                dbg.AppendLine("SKIP: Target element not found in translated XML by xml:id/nodePath.");
                DumpGroupLines(dbg, orderedLines, includeSegments: false);
                dbg.AppendLine();
                continue;
            }

            if (!CanPatchGroupSafely(orderedLines, out var unsafeReason))
            {
                skippedUnsafeGroups++;
                dbg.AppendLine($"SKIP: Unsafe group for line-based patching. Reason: {unsafeReason}");
                dbg.AppendLine($"TargetBefore(inner): {Trunc(SerializeInnerXml(target), 700)}");
                DumpGroupLines(dbg, orderedLines, includeSegments: true);
                dbg.AppendLine();
                continue;
            }

            dbg.AppendLine($"TargetFound: <{target.Name}>");
            dbg.AppendLine($"TargetBefore(inner): {Trunc(SerializeInnerXml(target), 700)}");
            DumpGroupLines(dbg, orderedLines, includeSegments: true);

            var rebuiltNodes = RebuildInlineNodesForElement(orderedLines).ToList();

            dbg.AppendLine("RebuiltNodes:");
            for (int i = 0; i < rebuiltNodes.Count; i++)
            {
                var n = rebuiltNodes[i];
                dbg.Append("  [").Append(i + 1).Append("] ");
                dbg.AppendLine(DescribeNode(n));
            }

            try
            {
                target.ReplaceNodes(rebuiltNodes);
                updatedCount++;

                dbg.AppendLine($"TargetAfter(inner): {Trunc(SerializeInnerXml(target), 700)}");

                if (ValidateAfterEachPatchedGroup)
                {
                    try
                    {
                        string interimXml;
                        try
                        {
                            interimXml = SerializeWithDeclaration(tranDoc);
                        }
                        catch (Exception sx)
                        {
                            dbg.AppendLine("InterimSerialization: FAILED");
                            dbg.AppendLine(sx.ToString());

                            dbg.AppendLine();
                            dbg.AppendLine("Summary:");
                            dbg.AppendLine($"  Updated groups: {updatedCount}");
                            dbg.AppendLine($"  Skipped no-dirty groups: {skippedNoDirtyGroups}");
                            dbg.AppendLine($"  Skipped target-missing groups: {skippedTargetMissingGroups}");
                            dbg.AppendLine($"  Skipped unsafe groups: {skippedUnsafeGroups}");

                            LastBuildTranslatedXmlDebugDump = dbg.ToString();
                            LastBuildTranslatedXmlDebugDumpPath = WriteDebugDumpToCTemp(LastBuildTranslatedXmlDebugDump, "FAIL-interim-serialize");

                            throw new InvalidOperationException(
                                $"Failed to serialize XML after patching group {first.ElementStableKey} ({first.Kind}): {sx.Message}" +
                                (!string.IsNullOrWhiteSpace(LastBuildTranslatedXmlDebugDumpPath)
                                    ? $"\n\nDebug dump written to: {LastBuildTranslatedXmlDebugDumpPath}"
                                    : "\n\nDebug dump write to C:\\temp failed."),
                                sx);
                        }

                        _ = XDocument.Parse(interimXml, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
                        dbg.AppendLine("InterimValidation: OK");
                    }
                    catch (XmlException xex)
                    {
                        var interimXml = SerializeWithDeclaration(tranDoc);
                        var snippet = GetXmlErrorSnippet(interimXml, xex.LineNumber, xex.LinePosition, 220);
                        dbg.AppendLine("InterimValidation: FAILED");
                        dbg.AppendLine($"XmlException: line {xex.LineNumber}, pos {xex.LinePosition}: {xex.Message}");
                        dbg.AppendLine("Context:");
                        dbg.AppendLine(snippet);

                        dbg.AppendLine();
                        dbg.AppendLine("Summary:");
                        dbg.AppendLine($"  Updated groups: {updatedCount}");
                        dbg.AppendLine($"  Skipped no-dirty groups: {skippedNoDirtyGroups}");
                        dbg.AppendLine($"  Skipped target-missing groups: {skippedTargetMissingGroups}");
                        dbg.AppendLine($"  Skipped unsafe groups: {skippedUnsafeGroups}");

                        LastBuildTranslatedXmlDebugDump = dbg.ToString();
                        LastBuildTranslatedXmlDebugDumpPath = WriteDebugDumpToCTemp(LastBuildTranslatedXmlDebugDump, "FAIL-interim");

                        throw new InvalidOperationException(
                            $"Generated XML became malformed right after patching group {first.ElementStableKey} " +
                            $"({first.Kind}). XML error at line {xex.LineNumber}, pos {xex.LinePosition}: {xex.Message}\n\n" +
                            $"Context:\n{snippet}\n\n" +
                            (!string.IsNullOrWhiteSpace(LastBuildTranslatedXmlDebugDumpPath)
                                ? $"Debug dump written to: {LastBuildTranslatedXmlDebugDumpPath}"
                                : "Debug dump write to C:\\temp failed."),
                            xex);
                    }
                }
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                dbg.AppendLine("ReplaceNodes failed:");
                dbg.AppendLine(ex.ToString());

                dbg.AppendLine();
                dbg.AppendLine("Summary:");
                dbg.AppendLine($"  Updated groups: {updatedCount}");
                dbg.AppendLine($"  Skipped no-dirty groups: {skippedNoDirtyGroups}");
                dbg.AppendLine($"  Skipped target-missing groups: {skippedTargetMissingGroups}");
                dbg.AppendLine($"  Skipped unsafe groups: {skippedUnsafeGroups}");

                LastBuildTranslatedXmlDebugDump = dbg.ToString();
                LastBuildTranslatedXmlDebugDumpPath = WriteDebugDumpToCTemp(LastBuildTranslatedXmlDebugDump, "FAIL-replace");

                throw new InvalidOperationException(
                    $"Failed rebuilding element {first.ElementStableKey} ({first.Kind}, line {first.LineNumber}): {ex.Message}" +
                    (!string.IsNullOrWhiteSpace(LastBuildTranslatedXmlDebugDumpPath)
                        ? $"\n\nDebug dump written to: {LastBuildTranslatedXmlDebugDumpPath}"
                        : "\n\nDebug dump write to C:\\temp failed."),
                    ex);
            }

            dbg.AppendLine();
        }

        string xml;
        try
        {
            xml = SerializeWithDeclaration(tranDoc);
            xml = InsertPrettyTagNewlines(xml); // cosmetic only
        }
        catch (Exception sx)
        {
            dbg.AppendLine("FinalSerialization: FAILED");
            dbg.AppendLine(sx.ToString());

            dbg.AppendLine();
            dbg.AppendLine("Summary:");
            dbg.AppendLine($"  Updated groups: {updatedCount}");
            dbg.AppendLine($"  Skipped no-dirty groups: {skippedNoDirtyGroups}");
            dbg.AppendLine($"  Skipped target-missing groups: {skippedTargetMissingGroups}");
            dbg.AppendLine($"  Skipped unsafe groups: {skippedUnsafeGroups}");

            LastBuildTranslatedXmlDebugDump = dbg.ToString();
            LastBuildTranslatedXmlDebugDumpPath = WriteDebugDumpToCTemp(LastBuildTranslatedXmlDebugDump, "FAIL-final-serialize");

            throw new InvalidOperationException(
                $"Failed to serialize final translated XML: {sx.Message}" +
                (!string.IsNullOrWhiteSpace(LastBuildTranslatedXmlDebugDumpPath)
                    ? $"\n\nDebug dump written to: {LastBuildTranslatedXmlDebugDumpPath}"
                    : "\n\nDebug dump write to C:\\temp failed."),
                sx);
        }

        try
        {
            _ = XDocument.Parse(xml, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            dbg.AppendLine("FinalValidation: OK");
        }
        catch (XmlException xex)
        {
            var snippet = GetXmlErrorSnippet(xml, xex.LineNumber, xex.LinePosition, 220);
            dbg.AppendLine("FinalValidation: FAILED");
            dbg.AppendLine($"XmlException: line {xex.LineNumber}, pos {xex.LinePosition}: {xex.Message}");
            dbg.AppendLine("Context:");
            dbg.AppendLine(snippet);

            dbg.AppendLine();
            dbg.AppendLine("Summary:");
            dbg.AppendLine($"  Updated groups: {updatedCount}");
            dbg.AppendLine($"  Skipped no-dirty groups: {skippedNoDirtyGroups}");
            dbg.AppendLine($"  Skipped target-missing groups: {skippedTargetMissingGroups}");
            dbg.AppendLine($"  Skipped unsafe groups: {skippedUnsafeGroups}");

            LastBuildTranslatedXmlDebugDump = dbg.ToString();
            LastBuildTranslatedXmlDebugDumpPath = WriteDebugDumpToCTemp(LastBuildTranslatedXmlDebugDump, "FAIL-final");

            throw new InvalidOperationException(
                $"Generated XML is malformed at line {xex.LineNumber}, pos {xex.LinePosition}: {xex.Message}\n\n" +
                $"Context:\n{snippet}\n\n" +
                (!string.IsNullOrWhiteSpace(LastBuildTranslatedXmlDebugDumpPath)
                    ? $"Debug dump written to: {LastBuildTranslatedXmlDebugDumpPath}"
                    : "Debug dump write to C:\\temp failed."),
                xex);
        }

        doc.TranslatedXml = xml;
        doc.HasSeparateTranslatedSource = !XmlEquivalent(doc.OriginalXml, doc.TranslatedXml);

        foreach (var u in doc.Units)
        {
            if (u.IsDirty)
            {
                u.EnBaseline = u.En;
                u.IsDirty = false;
            }
        }

        dbg.AppendLine();
        dbg.AppendLine("Summary:");
        dbg.AppendLine($"  Updated groups: {updatedCount}");
        dbg.AppendLine($"  Skipped no-dirty groups: {skippedNoDirtyGroups}");
        dbg.AppendLine($"  Skipped target-missing groups: {skippedTargetMissingGroups}");
        dbg.AppendLine($"  Skipped unsafe groups: {skippedUnsafeGroups}");

        LastBuildTranslatedXmlDebugDump = dbg.ToString();
        LastBuildTranslatedXmlDebugDumpPath = WriteDebugDumpToCTemp(LastBuildTranslatedXmlDebugDump, "OK");

        return xml;
    }

    private static string GetXmlErrorSnippet(string xml, int line, int pos, int radius = 200)
    {
        if (string.IsNullOrEmpty(xml)) return "(empty xml)";

        var lines = xml.Replace("\r\n", "\n").Split('\n');
        if (line < 1 || line > lines.Length)
            return "(line out of range)";

        var target = lines[line - 1] ?? "";
        int p = Math.Clamp(pos - 1, 0, Math.Max(0, target.Length));

        int start = Math.Max(0, p - radius);
        int len = Math.Min(target.Length - start, radius * 2);

        var slice = target.Substring(start, len);

        var caretOffset = p - start;
        var caretLine = new string(' ', Math.Max(0, caretOffset)) + "^";

        return slice + "\n" + caretLine;
    }

    private IEnumerable<XNode> RebuildInlineNodesForElement(List<TranslationUnit> lines)
    {
        var output = new List<XNode>();

        foreach (var line in lines.OrderBy(l => l.LineNumber))
        {
            // If EN blank, keep original ZH for safe write-back.
            string lineText = string.IsNullOrWhiteSpace(line.En) ? line.Zh : line.En;
            lineText = SanitizeXmlText(lineText);

            bool emittedLineText = false;

            foreach (var seg in line.LineSegments)
            {
                if (seg.Kind == TranslationSegmentKind.Text)
                {
                    if (!emittedLineText && !string.IsNullOrEmpty(lineText))
                    {
                        output.Add(new XText(lineText));
                        emittedLineText = true;
                    }

                    continue;
                }

                if (seg.Kind != TranslationSegmentKind.PreservedElement)
                    continue;

                if (seg.ElementTemplate == null)
                    continue;

                bool visiblePreserved = !seg.HideInProjection && !string.IsNullOrEmpty(seg.VisibleText);
                if (visiblePreserved)
                    throw new InvalidOperationException(
                        $"Unsafe rebuild: visible preserved inline element <{seg.ElementName}> exists in line-based patch template.");

                if (!seg.MoveToLineEnd)
                    output.Add(ClonePreservedElementForTranslated(seg));
            }

            if (!emittedLineText && !string.IsNullOrEmpty(lineText))
                output.Add(new XText(lineText));

            foreach (var seg in line.LineSegments)
            {
                if (seg.Kind != TranslationSegmentKind.PreservedElement) continue;
                if (!seg.MoveToLineEnd) continue;
                if (seg.ElementTemplate == null) continue;

                bool visiblePreserved = !seg.HideInProjection && !string.IsNullOrEmpty(seg.VisibleText);
                if (visiblePreserved)
                    throw new InvalidOperationException(
                        $"Unsafe rebuild: visible preserved inline element <{seg.ElementName}> exists in line-end patch template.");

                output.Add(ClonePreservedElementForTranslated(seg));
            }

            if (line.TrailingLbTemplate != null)
                output.Add(new XElement(line.TrailingLbTemplate));
        }

        return output;
    }

    private static XElement ClonePreservedElementForTranslated(TranslationSegment seg)
    {
        if (seg.ElementTemplate == null)
            throw new InvalidOperationException("Preserved element template is missing.");

        var clone = new XElement(seg.ElementTemplate);

        // IMPORTANT:
        // In translated XML, keep <g> tags for structural equivalence,
        // but purge their inner glyph content so no Chinese leaks into English text.
        if (string.Equals(seg.ElementName, "g", StringComparison.Ordinal))
        {
            clone.RemoveNodes(); // keep attributes like ref=..., drop inner text/glyph
        }

        return clone;
    }

    private static bool CanPatchGroupSafely(IEnumerable<TranslationUnit> lines, out string reason)
    {
        foreach (var line in lines)
        {
            foreach (var seg in line.LineSegments)
            {
                if (seg.Kind == TranslationSegmentKind.PreservedElement)
                {
                    var name = seg.ElementName ?? "";

                    bool visiblePreserved = !seg.HideInProjection && !string.IsNullOrEmpty(seg.VisibleText);
                    if (visiblePreserved)
                    {
                        reason = $"Visible preserved inline element <{name}> cannot be round-tripped by line-based rebuild.";
                        return false;
                    }
                }
            }
        }

        reason = "";
        return true;
    }

    private static string SanitizeXmlText(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";

        var sb = new StringBuilder(s.Length);

        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];

            if (char.IsHighSurrogate(ch))
            {
                if (i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
                {
                    sb.Append(ch);
                    sb.Append(s[i + 1]);
                    i++;
                }
                continue;
            }

            if (char.IsLowSurrogate(ch))
                continue;

            bool ok =
                ch == '\t' ||
                ch == '\n' ||
                ch == '\r' ||
                (ch >= 0x20 && ch <= 0xD7FF) ||
                (ch >= 0xE000 && ch <= 0xFFFD);

            if (ok) sb.Append(ch);
        }

        return sb.ToString();
    }

    // ============================================================
    // SEGMENT BUILDING / VISIBLE TEXT
    // ============================================================

    private void BuildSegmentsFromElement(XElement el, List<TranslationSegment> target, bool includeInlineNotesInProjection)
    {
        int line = 1;
        BuildSegmentsRecursive(el.Nodes(), target, ref line, includeInlineNotesInProjection);

        if (target.Count == 0)
        {
            target.Add(new TranslationSegment
            {
                Kind = TranslationSegmentKind.Text,
                Text = "",
                LineIndex = 1
            });
        }
    }

    private void BuildSegmentsRecursive(IEnumerable<XNode> nodes, List<TranslationSegment> target, ref int line, bool includeInlineNotesInProjection)
    {
        foreach (var node in nodes)
        {
            if (node is XText xt)
            {
                target.Add(new TranslationSegment
                {
                    Kind = TranslationSegmentKind.Text,
                    Text = xt.Value,
                    LineIndex = line
                });
                continue;
            }

            if (node is not XElement xe)
                continue;

            var ln = xe.Name.LocalName;

            if (ln == "lb")
            {
                target.Add(new TranslationSegment
                {
                    Kind = TranslationSegmentKind.LineBreakTag,
                    ElementTemplate = new XElement(xe),
                    ElementName = "lb",
                    LineIndex = line
                });

                line++;
                continue;
            }

            // Preserve nested notes; hide in body/head projection, show in notes mode.
            // BUT: apparatus inline cross-ref notes (cf/cf1/cf2) must stay hidden even in Notes mode,
            // otherwise they become visible text and make the line unsafe for round-trip patching.
            if (ln == "note")
            {
                var noteType = ((string?)xe.Attribute("type"))?.Trim() ?? "";

                bool isInlineCfRef =
                    noteType.Equals("cf", StringComparison.OrdinalIgnoreCase) ||
                    noteType.Equals("cf1", StringComparison.OrdinalIgnoreCase) ||
                    noteType.Equals("cf2", StringComparison.OrdinalIgnoreCase);

                bool hideInProjection = isInlineCfRef || !includeInlineNotesInProjection;

                target.Add(new TranslationSegment
                {
                    Kind = TranslationSegmentKind.PreservedElement,
                    ElementTemplate = new XElement(xe),
                    ElementName = "note",
                    ElementXmlId = (string?)xe.Attribute(XmlNs + "id"),
                    LineIndex = line,
                    HideInProjection = hideInProjection,
                    MoveToLineEnd = true,
                    VisibleText = hideInProjection ? "" : NormalizeVisibleInlineText(xe)
                });
                continue;
            }

            if (ln == "anchor")
            {
                bool noteAnchor = IsNoteAnchor(xe);

                target.Add(new TranslationSegment
                {
                    Kind = TranslationSegmentKind.PreservedElement,
                    ElementTemplate = new XElement(xe),
                    ElementName = "anchor",
                    ElementXmlId = (string?)xe.Attribute(XmlNs + "id"),
                    LineIndex = line,
                    HideInProjection = true,
                    MoveToLineEnd = noteAnchor,
                    VisibleText = ""
                });
                continue;
            }

            if (ln is "pb" or "milestone")
            {
                target.Add(new TranslationSegment
                {
                    Kind = TranslationSegmentKind.PreservedElement,
                    ElementTemplate = new XElement(xe),
                    ElementName = ln,
                    LineIndex = line,
                    HideInProjection = true,
                    MoveToLineEnd = false,
                    VisibleText = ""
                });
                continue;
            }

            // Wrapper with nested lb => recurse into children so line splits stay correct.
            if (xe.Descendants().Any(d => d.Name.LocalName == "lb"))
            {
                BuildSegmentsRecursive(xe.Nodes(), target, ref line, includeInlineNotesInProjection);
                continue;
            }

            // <g> (CBETA gaiji glyph wrapper): preserve structurally but hide in projection.
            // We move it to line end on rebuild (acceptable offset per spec).
            if (ln == "g")
            {
                target.Add(new TranslationSegment
                {
                    Kind = TranslationSegmentKind.PreservedElement,
                    ElementTemplate = new XElement(xe),
                    ElementName = "g",
                    ElementXmlId = (string?)xe.Attribute(XmlNs + "id"),
                    LineIndex = line,
                    HideInProjection = true,
                    MoveToLineEnd = true,
                    VisibleText = ""
                });
                continue;
            }

            // Generic preserved inline element
            bool hasNestedElements = xe.Elements().Any();
            target.Add(new TranslationSegment
            {
                Kind = TranslationSegmentKind.PreservedElement,
                ElementTemplate = new XElement(xe),
                ElementName = ln,
                ElementXmlId = (string?)xe.Attribute(XmlNs + "id"),
                LineIndex = line,
                HideInProjection = hasNestedElements,
                MoveToLineEnd = hasNestedElements,
                VisibleText = hasNestedElements ? "" : NormalizeVisibleInlineText(xe)
            });
        }
    }

    private sealed class LineTemplate
    {
        public List<TranslationSegment> LineSegments { get; } = new();
        public XElement? TrailingLbTemplate { get; set; }
    }

    private static Dictionary<int, LineTemplate> SplitLineTemplates(List<TranslationSegment> segments)
    {
        var result = new Dictionary<int, LineTemplate>();

        foreach (var seg in segments)
        {
            if (!result.TryGetValue(seg.LineIndex, out var tpl))
            {
                tpl = new LineTemplate();
                result[seg.LineIndex] = tpl;
            }

            if (seg.Kind == TranslationSegmentKind.LineBreakTag)
            {
                if (seg.ElementTemplate != null)
                    tpl.TrailingLbTemplate = new XElement(seg.ElementTemplate);
            }
            else
            {
                tpl.LineSegments.Add(CloneSegment(seg));
            }
        }

        if (result.Count == 0)
            result[1] = new LineTemplate();

        return result;
    }

    private static TranslationSegment CloneSegment(TranslationSegment s)
    {
        return new TranslationSegment
        {
            Kind = s.Kind,
            Text = s.Text,
            ElementTemplate = s.ElementTemplate != null ? new XElement(s.ElementTemplate) : null,
            ElementName = s.ElementName,
            ElementXmlId = s.ElementXmlId,
            LineIndex = s.LineIndex,
            HideInProjection = s.HideInProjection,
            MoveToLineEnd = s.MoveToLineEnd,
            VisibleText = s.VisibleText
        };
    }

    private static List<string> BuildVisibleLines(List<TranslationSegment> segments)
    {
        int maxLine = segments.Count == 0 ? 1 : Math.Max(1, segments.Max(s => s.LineIndex));
        var lines = Enumerable.Repeat("", maxLine).ToList();

        foreach (var seg in segments)
        {
            if (seg.Kind == TranslationSegmentKind.LineBreakTag) continue;
            if (seg.HideInProjection) continue;
            if (seg.LineIndex < 1 || seg.LineIndex > lines.Count) continue;

            string piece = seg.Kind == TranslationSegmentKind.Text ? seg.Text : seg.VisibleText;
            if (string.IsNullOrEmpty(piece)) continue;

            lines[seg.LineIndex - 1] += piece;
        }

        for (int i = 0; i < lines.Count; i++)
            lines[i] = NormalizeProjectionLine(lines[i]);

        return lines;
    }

    private static string NormalizeVisibleInlineText(XElement e)
    {
        var sb = new StringBuilder();

        foreach (var n in e.Nodes())
        {
            if (n is XText t)
            {
                sb.Append(t.Value);
            }
            else if (n is XElement child)
            {
                if (child.Name.LocalName is "lb" or "pb" or "milestone" or "anchor")
                    continue;

                if (child.Name.LocalName == "note")
                    continue;

                sb.Append(NormalizeVisibleInlineText(child));
            }
        }

        return sb.ToString();
    }

    private static string NormalizeProjectionLine(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Replace("\r", "").Replace("\n", " ");
        s = Regex.Replace(s, @"[ \t]+", " ");
        return s.Trim();
    }

    private static void AlignLineCount(List<string> lines, int count)
    {
        if (count < 1) count = 1;
        while (lines.Count < count) lines.Add("");
        while (lines.Count > count) lines.RemoveAt(lines.Count - 1);
    }

    // ============================================================
    // PROJECTION PARSER (<n> / ZH / EN)
    // ============================================================

    private sealed record ProjectionBlock(int BlockNumber, string Zh, string En);

    private static List<ProjectionBlock> ParseProjection(string text)
    {
        var lines = (text ?? "").Replace("\r\n", "\n").Split('\n');
        var blocks = new List<ProjectionBlock>();

        int i = 0;
        while (i < lines.Length)
        {
            var t = lines[i].Trim();

            if (!(t.StartsWith("<") && t.EndsWith(">")))
            {
                i++;
                continue;
            }

            var rawNum = t.Substring(1, t.Length - 2);
            if (!int.TryParse(rawNum, out int blockNum))
                throw new InvalidOperationException($"Invalid block header: {t}");

            i++;

            string? zh = null;
            string? en = null;

            while (i < lines.Length)
            {
                var cur = lines[i];
                var curTrim = cur.Trim();

                if (curTrim.StartsWith("<") && curTrim.EndsWith(">"))
                    break;

                if (cur.StartsWith("ZH:", StringComparison.Ordinal))
                    zh = cur.Substring(3).TrimStart();
                else if (cur.StartsWith("EN:", StringComparison.Ordinal))
                    en = cur.Substring(3).TrimStart();

                i++;
            }

            if (zh == null)
                throw new InvalidOperationException($"Block <{blockNum}> missing ZH.");
            if (en == null)
                throw new InvalidOperationException($"Block <{blockNum}> missing EN.");

            blocks.Add(new ProjectionBlock(blockNum, zh, en));
        }

        return blocks;
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    private static void ValidateEnglish(string en, int blockNumber)
    {
        en ??= "";

        if (en.Contains('<') || en.Contains('>'))
            throw new InvalidOperationException($"Block <{blockNumber}> EN contains '<' or '>' which is not allowed.");

        for (int i = 0; i < en.Length; i++)
        {
            char ch = en[i];

            if (char.IsHighSurrogate(ch))
            {
                if (i + 1 < en.Length && char.IsLowSurrogate(en[i + 1]))
                {
                    i++;
                    continue;
                }

                throw new InvalidOperationException(
                    $"Block <{blockNumber}> EN contains an invalid XML character (unpaired high surrogate U+{((int)ch):X4}) at position {i + 1}.");
            }

            if (char.IsLowSurrogate(ch))
            {
                throw new InvalidOperationException(
                    $"Block <{blockNumber}> EN contains an invalid XML character (unpaired low surrogate U+{((int)ch):X4}) at position {i + 1}.");
            }

            bool ok =
                ch == '\t' ||
                ch == '\n' ||
                ch == '\r' ||
                (ch >= 0x20 && ch <= 0xD7FF) ||
                (ch >= 0xE000 && ch <= 0xFFFD);

            if (!ok)
            {
                throw new InvalidOperationException(
                    $"Block <{blockNumber}> EN contains an invalid XML character (U+{((int)ch):X4}) at position {i + 1}.");
            }
        }
    }

    // ============================================================
    // LOOKUP / MATCHING (PERF)
    // ============================================================

    private sealed class DocLookup
    {
        public Dictionary<string, XElement> ByXmlId { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, XElement> ByNodePath { get; } = new(StringComparer.Ordinal);
    }

    private static DocLookup BuildDocLookup(XDocument doc)
    {
        var lookup = new DocLookup();
        if (doc.Root == null) return lookup;

        foreach (var el in doc.Root.DescendantsAndSelf())
        {
            var xmlId = (string?)el.Attribute(XmlNs + "id");
            if (!string.IsNullOrWhiteSpace(xmlId) && !lookup.ByXmlId.ContainsKey(xmlId))
                lookup.ByXmlId[xmlId] = el;

            var path = BuildNodePath(el);
            if (!lookup.ByNodePath.ContainsKey(path))
                lookup.ByNodePath[path] = el;
        }

        return lookup;
    }

    private static XElement? FindTranslatedMatch(XElement origEl, DocLookup tranLookup)
    {
        var xmlId = (string?)origEl.Attribute(XmlNs + "id");
        var path = BuildNodePath(origEl);
        return FindByIdentity(xmlId, path, tranLookup);
    }

    private static XElement? FindByIdentity(string? xmlId, string nodePath, DocLookup lookup)
    {
        if (!string.IsNullOrWhiteSpace(xmlId) && lookup.ByXmlId.TryGetValue(xmlId, out var byId))
            return byId;

        if (!string.IsNullOrWhiteSpace(nodePath) && lookup.ByNodePath.TryGetValue(nodePath, out var byPath))
            return byPath;

        return null;
    }

    // ============================================================
    // CLASSIFICATION / FILTERS
    // ============================================================

    private static TranslationUnitKind ClassifyBodyElement(XElement el)
    {
        // Real TEI notes still go to Notes mode
        if (el.Name == Tei + "note")
            return TranslationUnitKind.Note;

        // Everything inside body is Body mode otherwise (including heads, mulu, etc.)
        return TranslationUnitKind.Body;
    }

    private static bool IsTranslatableHeaderElement(XElement e)
    {
        if (e.HasElements) return false;
        if (string.IsNullOrWhiteSpace(e.Value)) return false;

        return e.Name.LocalName switch
        {
            "title" => true,
            "author" => true,
            "resp" => true,
            "name" => true,
            "edition" => true,
            "p" => true,
            "bibl" => true,
            "language" => true,
            _ => false
        };
    }

    private static bool IsTranslatableBodyElement(XElement e)
    {
        if (e.Name == Tei + "pb" || e.Name == Tei + "lb" || e.Name == Tei + "milestone")
            return false;

        if (e.Name == Tei + "p" ||
            e.Name == Tei + "head" ||
            e.Name == Tei + "item" ||
            e.Name == Tei + "note" ||
            e.Name == Tei + "byline" ||
            e.Name == Tei + "l" ||
            e.Name == Cb + "jhead" ||
            e.Name == Cb + "mulu" ||
            e.Name == Cb + "docNumber")
        {
            return !string.IsNullOrWhiteSpace(VisibleTextWithoutInlineNotes(e));
        }

        return false;
    }

    private static bool IsTranslatableBackElement(XElement e)
    {
        if (e.Name == Tei + "pb" || e.Name == Tei + "lb" || e.Name == Tei + "milestone")
            return false;

        if (e.HasElements)
        {
            if (e.Name == Tei + "head" || e.Name == Tei + "p" || e.Name == Tei + "note" || e.Name.LocalName is "lem" or "rdg")
                return !string.IsNullOrWhiteSpace(NormalizeProjectionLine(e.Value));
            return false;
        }

        return !string.IsNullOrWhiteSpace(NormalizeProjectionLine(e.Value));
    }

    private static string VisibleTextWithoutInlineNotes(XElement e)
    {
        var sb = new StringBuilder();

        foreach (var n in e.Nodes())
        {
            if (n is XText t)
            {
                sb.Append(t.Value);
            }
            else if (n is XElement x)
            {
                if (x.Name.LocalName is "lb" or "pb" or "milestone" or "anchor")
                    continue;

                if (x.Name.LocalName == "note")
                    continue;

                // Do include gaiji visible text for "is this translatable?" detection.
                // (write-back safety is handled separately in segment builder)
                sb.Append(VisibleTextWithoutInlineNotes(x));
            }
        }

        return NormalizeProjectionLine(sb.ToString());
    }

    private static bool IsNoteAnchor(XElement anchor)
    {
        if (anchor.Name.LocalName != "anchor") return false;

        var xid = ((string?)anchor.Attribute(XmlNs + "id")) ?? "";
        return xid.StartsWith("nkr_note_", StringComparison.Ordinal)
            || xid.StartsWith("beg", StringComparison.Ordinal)
            || xid.StartsWith("end", StringComparison.Ordinal);
    }

    // ============================================================
    // XML EQUIVALENCE / SERIALIZE
    // ============================================================

    private static bool XmlEquivalent(string a, string b)
    {
        try
        {
            var sa = XDocument.Parse(a ?? "", LoadOptions.PreserveWhitespace).ToString(SaveOptions.DisableFormatting);
            var sb = XDocument.Parse(b ?? "", LoadOptions.PreserveWhitespace).ToString(SaveOptions.DisableFormatting);
            return string.Equals(sa, sb, StringComparison.Ordinal);
        }
        catch
        {
            return string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.Ordinal);
        }
    }

    private static string SerializeWithDeclaration(XDocument doc)
    {
        // Force a sane XML declaration for string output that will later be written as UTF-8.
        // This prevents "Cannot switch to Unicode / no BOM" errors caused by utf-16 declarations.
        if (doc.Declaration == null)
        {
            doc.Declaration = new XDeclaration("1.0", "utf-8", null);
        }
        else
        {
            // Preserve version/standalone if present, but force encoding to utf-8
            doc.Declaration = new XDeclaration(
                string.IsNullOrWhiteSpace(doc.Declaration.Version) ? "1.0" : doc.Declaration.Version,
                "utf-8",
                doc.Declaration.Standalone);
        }

        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = false, // always write declaration now
            Indent = false,
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Replace,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };

        var sb = new StringBuilder();

        using (var sw = new Utf8StringWriter())
        using (var xw = XmlWriter.Create(sw, settings))
        {
            doc.Save(xw);
            xw.Flush();

            // StringWriter has its own internal buffer, so return that
            return sw.ToString();
        }
    }

    private static string InsertPrettyTagNewlines(string xml)
    {
        if (string.IsNullOrEmpty(xml)) return xml;

        xml = Regex.Replace(
            xml,
            @"(?<!\n)(?<tag><(?:lb|pb|milestone)\b[^>]*/>)",
            "\n${tag}",
            RegexOptions.IgnoreCase);

        xml = Regex.Replace(
            xml,
            @"(?<tag><(?:lb|pb|milestone)\b[^>]*/>)(?=<)",
            "${tag}\n",
            RegexOptions.IgnoreCase);

        xml = Regex.Replace(xml, @"\n{3,}", "\n\n");

        return xml;
    }

    // ============================================================
    // NODE PATHS
    // ============================================================

    private static readonly XNamespace TeiNs = "http://www.tei-c.org/ns/1.0";
    private static readonly XNamespace CbNs = "http://www.cbeta.org/ns/1.0";

    private static string PrefixFor(XNamespace ns) => ns == TeiNs ? "tei" : ns == CbNs ? "cb" : "ns";

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

    // ============================================================
    // DEBUG HELPERS
    // ============================================================

    private static void DumpGroupLines(StringBuilder sb, List<TranslationUnit> lines, bool includeSegments)
    {
        sb.AppendLine("LinesDetail:");
        foreach (var line in lines.OrderBy(x => x.LineNumber))
        {
            sb.Append("  L").Append(line.LineNumber)
              .Append(" dirty=").Append(line.IsDirty ? "Y" : "N")
              .Append(" zh=").Append(Quote(Trunc(line.Zh, 160)))
              .Append(" en=").Append(Quote(Trunc(line.En, 160)))
              .Append(" baseline=").Append(Quote(Trunc(line.EnBaseline, 160)))
              .AppendLine();

            if (!includeSegments) continue;

            if (line.LineSegments.Count == 0)
            {
                sb.AppendLine("    (no line segments)");
            }
            else
            {
                int si = 0;
                foreach (var seg in line.LineSegments)
                {
                    si++;
                    sb.Append("    seg[").Append(si).Append("] ")
                      .Append(seg.Kind)
                      .Append(" line=").Append(seg.LineIndex)
                      .Append(" name=").Append(seg.ElementName ?? "(text)")
                      .Append(" hide=").Append(seg.HideInProjection ? "Y" : "N")
                      .Append(" end=").Append(seg.MoveToLineEnd ? "Y" : "N");

                    if (seg.Kind == TranslationSegmentKind.Text)
                    {
                        sb.Append(" text=").Append(Quote(Trunc(seg.Text, 100)));
                    }
                    else if (seg.ElementTemplate != null)
                    {
                        sb.Append(" xml=").Append(Trunc(seg.ElementTemplate.ToString(SaveOptions.DisableFormatting), 180));
                    }

                    sb.AppendLine();
                }
            }

            if (line.TrailingLbTemplate != null)
                sb.AppendLine($"    trailingLb={line.TrailingLbTemplate.ToString(SaveOptions.DisableFormatting)}");
        }
    }

    private static string DescribeNode(XNode n)
    {
        if (n is XText t)
            return $"XText \"{Esc(Trunc(t.Value, 180))}\"";

        if (n is XElement e)
            return $"XElement {Trunc(e.ToString(SaveOptions.DisableFormatting), 280)}";

        return n.GetType().Name;
    }

    private static string SerializeInnerXml(XElement el)
    {
        return string.Concat(el.Nodes().Select(n => n.ToString(SaveOptions.DisableFormatting)));
    }

    private static string Trunc(string? s, int max)
    {
        s ??= "";
        if (s.Length <= max) return s;
        return s.Substring(0, Math.Max(0, max - 3)) + "...";
    }

    private static string Quote(string s) => "\"" + Esc(s) + "\"";

    private static string Esc(string? s)
    {
        s ??= "";
        return s.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
    }

    private static string WriteDebugDumpToCTemp(string content, string suffix)
    {
        try
        {
            var dir = @"C:\temp";
            Directory.CreateDirectory(dir);

            var ts = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            var file = $"xml-rebuild-debug-{ts}-{suffix}.log";
            var path = Path.Combine(dir, file);

            File.WriteAllText(path, content ?? "", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return path;
        }
        catch
        {
            return "";
        }
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }
}
