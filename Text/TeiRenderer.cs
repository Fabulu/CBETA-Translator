// Text/TeiRenderer.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;

namespace ReadZen.App.Text;

/// <summary>
/// Converts TEI/CBETA-ish XML into readable text WITH stable segment keys.
/// Fine-grained segmentation (preferred):
/// - Start new segment on sync-tags: lb, pb, p(xml:id), l(n), anchor, cb:juan
/// - Render lb/l as newline, pb/p/head as paragraph break
/// Not a full XML parser; fast tag/text scanner.
///
/// Notes/annotations:
/// - Skips rendering <back> entirely (so “校注” blocks don’t show in the reader)
/// - Collects:
///   1) Inline notes: <note place="inline">...</note> at current text position
///   2) End notes in <back>: <note ... target="#nkr_note_mod_XXXX">...</note>
///      anchored by <anchor xml:id="nkr_note_mod_XXXX" .../> in the body.
///   3) Community notes meant as *annotations* (NOT translations):
///      <note type="community" resp="SOMEONE">...</note>
///      - We explicitly SKIP resp="md-import" (that’s translation glue, not an annotation)
///      - We KEEP resp="md-note" (your markdown-native notes)
/// - Builds DocAnnotation list + calls AnnotationMarkerInserter.InsertMarkers(...)
///
/// IMPORTANT (your bug):
/// - NEVER normalize newlines inside Render(). Indices (XmlStart/XmlEndExclusive/BaseToXmlIndex)
///   MUST be in the original XML string index space, because insert/delete mutate the original XML.
/// - If you need LF-normalized text for merge/validation, call NormalizeNewlinesForMerge() elsewhere.
///
/// CRITICAL DETAIL:
/// - BaseToXmlIndex is a *POSITION MAP* (caret positions), not a char map:
///     Length == baseText.Length + 1
///     BaseToXmlIndex[p] = XML index where an insertion at BASE caret position p logically occurs.
///
/// - Any characters we INSERT that do not exist in XML (newlines/paragraph breaks/spaces)
///   must map to an XML index that matches where the insertion *logically occurs*.
///   We therefore map inserted breaks to (gt+1), i.e. "after this tag", NOT to lt.
/// </summary>
public static class TeiRenderer
{
    // Your translation merge uses resp="md-import"; renderer must NOT treat those as annotations.
    private const string RespMdImport = "md-import";
    private const string RespMdNote = "md-note";

    private const string AttrTargetPath = "target-path";
    private const string AttrRespUser = "resp-user";
    private const string AttrType = "type";
    private const string AttrPlace = "place";
    private const string AttrTarget = "target";
    private const string AttrLang = "xml:lang"; // only used for checks via Attr(...) since we're scanning text

    // Segment keys + attribute parsing (no regex)
    private const string AttrN = "n";
    private const string AttrEd = "ed";
    private const string AttrXmlId = "xml:id";
    private const string AttrFun = "fun";

    /// <summary>
    /// LF-normalize CRLF/CR to LF.
    /// Use this ONLY when you explicitly want "merge/validation space"
    /// (e.g., markdown import validation).
    /// DO NOT use this for indices used to mutate the original XML string.
    /// </summary>
    public static string NormalizeNewlinesForMerge(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    public static RenderedDocument Render(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return RenderedDocument.Empty;

        // IMPORTANT:
        // Do NOT normalize newlines here.
        // Indices (XmlStart/XmlEndExclusive/BaseToXmlIndex) must match the ORIGINAL xml string
        // that later gets mutated for insert/delete.
        //
        // Rendered output is still stable because:
        // - structural breaks come from tags (lb/p/pb/head)
        // - AppendText ignores '\r' already (so CRLF won't render extra chars)

        var sb = new StringBuilder(xml.Length);

        // POSITION MAP:
        // baseToXml.Count == sb.Length + 1 always.
        // baseToXml[p] = XML insertion index at base caret position p.
        var baseToXml = new List<int>(capacity: Math.Max(1024, xml.Length / 4));
        baseToXml.Add(0); // caret at position 0 maps to xml index 0

        var segments = new List<RenderSegment>(capacity: 4096);

        // collected annotations
        var annotations = new List<DocAnnotation>(capacity: 128);

        // anchor xml:id -> rendered offset (in sb) + inferred kind
        var anchorPosById = new Dictionary<string, (int Pos, string? Kind)>(StringComparer.Ordinal);

        // note capture state (for <note> ... </note>)
        bool inNoteCapture = false;
        var noteSb = new StringBuilder(256);

        int noteAnchorPos = -1;     // in base rendered text (sb)
        string? noteKind = null;    // "mod"/"orig"/"add"/"inline"/"md-note"/"community"/...
        string? noteResp = null;    // author-ish (for md-note, prefer resp-user)

        // XML span for inline/community notes (for precise removal later)
        int noteXmlStart = -1;
        int noteXmlEndExclusive = -1;

        string currentKey = "START";
        int segStart = 0;

        int teiHeaderDepth = 0;
        int backDepth = 0; // when >0, we do not render text to sb, but we still parse notes
        int muluDepth = 0; // when >0, suppress text inside <cb:mulu> (TOC metadata, duplicates <head>)
        int appDepth = 0;  // when >0, suppress text inside <app> (critical apparatus variants)

        // apparatus capture state (for <app> ... </app>)
        bool inLemCapture = false;
        bool inRdgCapture = false;
        var lemSb = new StringBuilder(128);
        var rdgList = new List<(string Text, string Wit)>(4);
        var currentRdgSb = new StringBuilder(128);
        string? currentRdgWit = null;
        string? appFromId = null;
        int appXmlStart = -1;

        bool lastWasNewline = false;       // for main sb
        bool noteLastWasNewline = false;   // for noteSb

        // heading capture state (for <head> ... </head>)
        var headings = new List<HeadingInfo>(capacity: 32);
        bool inHeadCapture = false;
        var headSb = new StringBuilder(128);
        int headRenderedOffset = 0;

        void StartNewSegment(string newKey)
        {
            int end = sb.Length;
            if (end > segStart)
                segments.Add(new RenderSegment(currentKey, segStart, end));
            else if (currentKey.StartsWith("lb|", StringComparison.Ordinal))
                // Preserve zero-length lb segments — they carry position info
                // needed by FindNearestLbNValue even when no text follows before
                // the next structural tag (e.g., <lb/><cb:div><p>).
                segments.Add(new RenderSegment(currentKey, segStart, segStart));

            currentKey = newKey;
            segStart = sb.Length;
        }

        StartNewSegment("START");

        ReadOnlySpan<char> s = xml.AsSpan();
        int i = 0;

        while (i < s.Length)
        {
            int relLt = s.Slice(i).IndexOf('<');
            if (relLt < 0)
            {
                // trailing text
                if (teiHeaderDepth == 0 && backDepth == 0 && muluDepth == 0 && appDepth == 0 && !inNoteCapture)
                {
                    AppendText(sb, baseToXml, s.Slice(i), absStartXmlIndex: i, ref lastWasNewline);
                    if (inHeadCapture) AppendPlainText(headSb, s.Slice(i));
                }
                else if (inNoteCapture)
                    AppendText(noteSb, map: null, s.Slice(i), absStartXmlIndex: i, ref noteLastWasNewline);
                else if (appDepth > 0)
                {
                    var trailing = s.Slice(i);
                    if (inRdgCapture) AppendPlainText(currentRdgSb, trailing);
                    else if (inLemCapture) AppendPlainText(lemSb, trailing);
                }
                break;
            }

            int lt = i + relLt;

            // text before tag
            if (lt > i)
            {
                var rawText = s.Slice(i, lt - i);

                if (inNoteCapture)
                {
                    AppendText(noteSb, map: null, rawText, absStartXmlIndex: i, ref noteLastWasNewline);
                }
                else if (teiHeaderDepth == 0 && backDepth == 0 && muluDepth == 0 && appDepth == 0)
                {
                    AppendText(sb, baseToXml, rawText, absStartXmlIndex: i, ref lastWasNewline);
                    if (inHeadCapture) AppendPlainText(headSb, rawText);
                }
                else if (appDepth > 0)
                {
                    if (inRdgCapture) AppendPlainText(currentRdgSb, rawText);
                    else if (inLemCapture) AppendPlainText(lemSb, rawText);
                }
            }

            // find end of tag
            int relGt = s.Slice(lt).IndexOf('>');
            if (relGt < 0)
            {
                // malformed tail -> treat as text
                var tail = s.Slice(lt);
                if (inNoteCapture)
                    AppendText(noteSb, map: null, tail, absStartXmlIndex: lt, ref noteLastWasNewline);
                else if (teiHeaderDepth == 0 && backDepth == 0 && muluDepth == 0 && appDepth == 0)
                {
                    AppendText(sb, baseToXml, tail, absStartXmlIndex: lt, ref lastWasNewline);
                    if (inHeadCapture) AppendPlainText(headSb, tail);
                }
                else if (appDepth > 0)
                {
                    if (inRdgCapture) AppendPlainText(currentRdgSb, tail);
                    else if (inLemCapture) AppendPlainText(lemSb, tail);
                }
                break;
            }

            int gt = lt + relGt;
            int afterTag = gt + 1; // IMPORTANT: where insertions should map (logical "after this tag")
            var tagSpan = s.Slice(lt, gt - lt + 1);

            if (TryParseTag(tagSpan, out var isEndTag, out var tagName, out var attrs))
            {
                if (isEndTag)
                {
                    // depth tracking
                    if (EqualsIgnoreCase(tagName, "teiHeader"))
                        teiHeaderDepth = Math.Max(0, teiHeaderDepth - 1);

                    if (EqualsIgnoreCase(tagName, "back"))
                        backDepth = Math.Max(0, backDepth - 1);

                    if (EqualsIgnoreCase(tagName, "cb:mulu"))
                        muluDepth = Math.Max(0, muluDepth - 1);

                    if (EqualsIgnoreCase(tagName, "app"))
                    {
                        int prevAppDepth = appDepth;
                        appDepth = Math.Max(0, appDepth - 1);

                        if (prevAppDepth == 1 && appDepth == 0)
                        {
                            // Flush any in-progress rdg capture
                            if (inRdgCapture)
                            {
                                var rdgText = currentRdgSb.ToString().Trim();
                                rdgList.Add((rdgText.Length > 0 ? rdgText : "(empty)", currentRdgWit ?? ""));
                                currentRdgSb.Clear();
                                currentRdgWit = null;
                                inRdgCapture = false;
                            }
                            inLemCapture = false;

                            // Emit apparatus annotation
                            if (rdgList.Count > 0)
                            {
                                int anchorPos = -1;
                                if (appFromId != null && anchorPosById.TryGetValue(appFromId, out var begHit))
                                    anchorPos = begHit.Pos;
                                else
                                    anchorPos = sb.Length; // fallback: annotate at current position

                                var lemText = lemSb.ToString().Trim();
                                var appSb = new StringBuilder();
                                if (lemText.Length > 0)
                                    appSb.Append("Lem: ").Append(lemText);
                                foreach (var (rdgT, wit) in rdgList)
                                {
                                    if (appSb.Length > 0)
                                        appSb.Append('\n');
                                    appSb.Append("Rdg: ").Append(rdgT);
                                    if (wit.Length > 0)
                                        appSb.Append(" [").Append(wit).Append(']');
                                }
                                annotations.Add(new DocAnnotation(
                                    start: anchorPos,
                                    endExclusive: anchorPos,
                                    text: appSb.ToString(),
                                    kind: "apparatus",
                                    xmlStart: appXmlStart,
                                    xmlEndExclusive: afterTag));
                            }

                            // Reset
                            lemSb.Clear();
                            rdgList.Clear();
                            currentRdgSb.Clear();
                            appFromId = null;
                            appXmlStart = -1;
                        }
                    }

                    // Handle </lem> and </rdg> inside apparatus
                    if (appDepth > 0)
                    {
                        if (EqualsIgnoreCase(tagName, "lem") && inLemCapture)
                            inLemCapture = false;
                        else if (EqualsIgnoreCase(tagName, "rdg") && inRdgCapture)
                        {
                            var rdgText = currentRdgSb.ToString().Trim();
                            rdgList.Add((rdgText.Length > 0 ? rdgText : "(empty)", currentRdgWit ?? ""));
                            currentRdgSb.Clear();
                            currentRdgWit = null;
                            inRdgCapture = false;
                        }
                    }

                    // finish note capture
                    if (EqualsIgnoreCase(tagName, "note") && inNoteCapture)
                    {
                        inNoteCapture = false;

                        var noteText = noteSb.ToString().Trim();
                        noteSb.Clear();
                        noteLastWasNewline = false;

                        noteXmlEndExclusive = afterTag;

                        if (noteAnchorPos >= 0 && !string.IsNullOrWhiteSpace(noteText))
                        {
                            annotations.Add(new DocAnnotation(
                                start: noteAnchorPos,
                                endExclusive: noteAnchorPos,
                                text: noteText,
                                kind: noteKind,
                                resp: noteResp,
                                xmlStart: noteXmlStart,
                                xmlEndExclusive: noteXmlEndExclusive));
                        }

                        noteAnchorPos = -1;
                        noteKind = null;
                        noteResp = null;
                        noteXmlStart = -1;
                        noteXmlEndExclusive = -1;
                    }

                    // finish heading capture
                    if (EqualsIgnoreCase(tagName, "head") && inHeadCapture)
                    {
                        inHeadCapture = false;
                        var headText = headSb.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(headText))
                            headings.Add(new HeadingInfo(headText, headRenderedOffset, Level: 1));
                        headSb.Clear();
                    }

                    // paragraph / verse-group end spacing (only in main rendered part)
                    if (teiHeaderDepth == 0 && backDepth == 0 && appDepth == 0 && EqualsIgnoreCase(tagName, "p"))
                        EnsureParagraphBreak(sb, baseToXml, xmlIndexForInserted: afterTag, ref lastWasNewline);
                    else if (teiHeaderDepth == 0 && backDepth == 0 && appDepth == 0 && EqualsIgnoreCase(tagName, "lg"))
                        EnsureParagraphBreak(sb, baseToXml, xmlIndexForInserted: afterTag, ref lastWasNewline);
                }
                else
                {
                    // entering blocks
                    if (EqualsIgnoreCase(tagName, "teiHeader"))
                        teiHeaderDepth++;
                    else if (EqualsIgnoreCase(tagName, "back"))
                        backDepth++;
                    else if (EqualsIgnoreCase(tagName, "cb:mulu"))
                        muluDepth++;
                    else if (EqualsIgnoreCase(tagName, "app"))
                    {
                        appDepth++;
                        if (appDepth == 1)
                        {
                            // Parse from="#begXXX" to get anchor ID
                            var fromAttr = Attr(attrs, "from");
                            appFromId = fromAttr != null && fromAttr.Length > 1 && fromAttr[0] == '#'
                                ? fromAttr.Substring(1)
                                : null;
                            appXmlStart = lt;
                            // Clear capture buffers
                            inLemCapture = false;
                            inRdgCapture = false;
                            lemSb.Clear();
                            rdgList.Clear();
                            currentRdgSb.Clear();
                            currentRdgWit = null;
                        }
                    }

                    // Handle <lem> and <rdg> start tags inside apparatus
                    if (appDepth > 0)
                    {
                        if (EqualsIgnoreCase(tagName, "lem"))
                        {
                            inLemCapture = true;
                            inRdgCapture = false;
                        }
                        else if (EqualsIgnoreCase(tagName, "rdg"))
                        {
                            // Flush any previous rdg
                            if (inRdgCapture)
                            {
                                var rdgText = currentRdgSb.ToString().Trim();
                                rdgList.Add((rdgText.Length > 0 ? rdgText : "(empty)", currentRdgWit ?? ""));
                                currentRdgSb.Clear();
                            }
                            inRdgCapture = true;
                            inLemCapture = false;
                            currentRdgWit = Attr(attrs, "wit");
                        }
                    }

                    // If we're capturing a note and we hit any start-tag: treat as a soft separator
                    if (inNoteCapture)
                    {
                        if (EqualsIgnoreCase(tagName, "lb") ||
                            EqualsIgnoreCase(tagName, "p") ||
                            EqualsIgnoreCase(tagName, "head") ||
                            EqualsIgnoreCase(tagName, "br"))
                        {
                            AppendNewline(noteSb, map: null, xmlIndexForInserted: afterTag, ref noteLastWasNewline);
                        }
                        else
                        {
                            // space between words caused by tags inside notes
                            AppendText(noteSb, map: null, " ".AsSpan(), absStartXmlIndex: afterTag, ref noteLastWasNewline);
                        }
                    }

                    // Only do segmentation/rendering while not in teiHeader and not in back and not in note capture
                    if (teiHeaderDepth == 0 && backDepth == 0 && !inNoteCapture && appDepth == 0)
                    {
                        // Segment boundary keys
                        if (TryMakeSyncKey(tagName, attrs, out var key))
                            StartNewSegment(key);

                        // Record note anchors in main text:
                        // <anchor xml:id="nkr_note_mod_0535011" .../>
                        if (EqualsIgnoreCase(tagName, "anchor"))
                        {
                            var id = Attr(attrs, AttrXmlId);
                            if (!string.IsNullOrWhiteSpace(id) && id.StartsWith("nkr_note_", StringComparison.Ordinal))
                            {
                                var kind = InferNoteKindFromId(id);
                                anchorPosById[id] = (sb.Length, kind);
                            }
                            // Also record "beg" anchors for apparatus <app from="#begXXX">
                            if (!string.IsNullOrWhiteSpace(id) && id.StartsWith("beg", StringComparison.Ordinal))
                            {
                                anchorPosById[id] = (sb.Length, null);
                            }
                        }

                        // Inline notes: <note place="inline">...</note>
                        // Community notes (annotations): <note type="community" resp="NAME">...</note>
                        // - SKIP resp="md-import" (translations)
                        // - KEEP resp="md-note" (markdown-native notes)
                        if (EqualsIgnoreCase(tagName, "note"))
                        {
                            var place = Attr(attrs, AttrPlace);
                            var type = Attr(attrs, AttrType);
                            var resp = Attr(attrs, "resp");

                            bool isInline = string.Equals(place, "inline", StringComparison.OrdinalIgnoreCase);
                            bool isCommunity = string.Equals(type, "community", StringComparison.OrdinalIgnoreCase);

                            // Translation glue? Don't capture.
                            if (isCommunity && string.Equals(resp, RespMdImport, StringComparison.OrdinalIgnoreCase))
                            {
                                // invisible metadata for reader
                            }
                            else if (isInline || isCommunity)
                            {
                                inNoteCapture = true;
                                noteSb.Clear();
                                noteLastWasNewline = false;

                                // Anchor at current rendered pos (note appears here in XML).
                                noteAnchorPos = sb.Length;

                                noteXmlStart = lt;
                                noteXmlEndExclusive = -1;

                                // Kind: preserve md-note explicitly so UI can distinguish.
                                if (isCommunity && string.Equals(resp, RespMdNote, StringComparison.OrdinalIgnoreCase))
                                    noteKind = RespMdNote;
                                else
                                    noteKind = isCommunity ? "community" : (type ?? "inline");

                                // Author-ish: prefer resp-user for md-note, else resp attribute.
                                var respUser = Attr(attrs, AttrRespUser);
                                noteResp = !string.IsNullOrWhiteSpace(respUser) ? respUser : resp;
                            }
                        }

                        // Rendering structural breaks
                        if (EqualsIgnoreCase(tagName, "lb"))
                        {
                            // INSERTED newline should map AFTER the <lb .../> tag.
                            AppendNewline(sb, baseToXml, xmlIndexForInserted: afterTag, ref lastWasNewline);
                        }
                        else if (EqualsIgnoreCase(tagName, "pb") ||
                                 EqualsIgnoreCase(tagName, "p") ||
                                 EqualsIgnoreCase(tagName, "head"))
                        {
                            // INSERTED paragraph break should map AFTER the tag that caused it.
                            EnsureParagraphBreak(sb, baseToXml, xmlIndexForInserted: afterTag, ref lastWasNewline);

                            // Start heading capture when <head> opens
                            if (EqualsIgnoreCase(tagName, "head"))
                            {
                                inHeadCapture = true;
                                headSb.Clear();
                                headRenderedOffset = sb.Length;
                            }
                        }
                        else if (EqualsIgnoreCase(tagName, "caesura"))
                        {
                            // Ideographic space as verse caesura separator
                            AppendText(sb, baseToXml, "\u3000".AsSpan(), absStartXmlIndex: afterTag, ref lastWasNewline);
                        }
                        else if (EqualsIgnoreCase(tagName, "lg"))
                        {
                            EnsureParagraphBreak(sb, baseToXml, xmlIndexForInserted: afterTag, ref lastWasNewline);
                        }
                        else if (EqualsIgnoreCase(tagName, "l"))
                        {
                            // Verse line: newline before each line (except the first in a group)
                            AppendNewline(sb, baseToXml, xmlIndexForInserted: afterTag, ref lastWasNewline);
                        }
                    }
                    else
                    {
                        // Inside <back> (or header): do NOT render, but collect end-notes anchored by <anchor> ids.
                        if (teiHeaderDepth == 0 && EqualsIgnoreCase(tagName, "note"))
                        {
                            var target = Attr(attrs, AttrTarget);
                            if (!string.IsNullOrWhiteSpace(target) && target[0] == '#')
                            {
                                var targetId = target.Substring(1);

                                if (targetId.StartsWith("nkr_note_", StringComparison.Ordinal))
                                {
                                    inNoteCapture = true;
                                    noteSb.Clear();
                                    noteLastWasNewline = false;

                                    noteXmlStart = lt;
                                    noteXmlEndExclusive = -1;

                                    var resp = Attr(attrs, "resp");
                                    noteResp = resp;

                                    if (anchorPosById.TryGetValue(targetId, out var hit))
                                    {
                                        noteAnchorPos = hit.Pos;
                                        noteKind = hit.Kind ?? Attr(attrs, "type");
                                    }
                                    else
                                    {
                                        noteAnchorPos = -1;
                                        noteKind = InferNoteKindFromId(targetId) ?? Attr(attrs, "type");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            i = afterTag;
        }

        // Close last segment (main text only)
        int finalEnd = sb.Length;
        if (finalEnd > segStart)
            segments.Add(new RenderSegment(currentKey, segStart, finalEnd));

        // Build base text
        var baseText = sb.ToString();

        // POSITION MAP invariant: baseToXml.Count == baseText.Length + 1
        if (baseToXml.Count != baseText.Length + 1)
        {
            throw new InvalidOperationException(
                $"BaseToXmlIndex invariant violated. " +
                $"Count={baseToXml.Count}, Expected={baseText.Length + 1}");
        }

        var baseToXmlIndex = baseToXml.ToArray();

        // Insert visible markers/superscripts into the rendered text.
        // NOTE: BaseToXmlIndex is in BASE coordinates (pre-marker insertion). That's correct.
        var (newText, newSegments, markers) =
            AnnotationMarkerInserter.InsertMarkers(baseText, annotations, segments);

        return new RenderedDocument(
            newText,
            newSegments,
            annotations,
            markers,
            baseToXmlIndex: baseToXmlIndex,
            baseTextLength: baseText.Length,
            license: null,
            headings: headings);
    }

    // ------------------------------------------------------------
    // Fast tag parsing (no regex, minimal allocations)
    // ------------------------------------------------------------

    private static bool TryParseTag(ReadOnlySpan<char> tag, out bool isEndTag, out ReadOnlySpan<char> tagName, out ReadOnlySpan<char> attrs)
    {
        isEndTag = false;
        tagName = default;
        attrs = default;

        // must start with < and end with >
        if (tag.Length < 3 || tag[0] != '<' || tag[^1] != '>')
            return false;

        int p = 1;

        // comments / PI / doctype etc => ignore
        char c1 = tag[p];
        if (c1 == '!' || c1 == '?')
            return false;

        if (c1 == '/')
        {
            isEndTag = true;
            p++;
        }

        // skip whitespace
        while (p < tag.Length && char.IsWhiteSpace(tag[p])) p++;
        if (p >= tag.Length - 1) return false;

        int nameStart = p;

        // tag name: allow letters/digits + optional colon + '-' '_' (defensive)
        while (p < tag.Length - 1)
        {
            char ch = tag[p];
            if (char.IsLetterOrDigit(ch) || ch == ':' || ch == '-' || ch == '_')
            {
                p++;
                continue;
            }
            break;
        }

        if (p == nameStart) return false;

        tagName = tag.Slice(nameStart, p - nameStart);

        // attrs start at current p, end before closing '>' (and before trailing '/')
        int attrStart = p;

        int attrEnd = tag.Length - 1; // index of '>'
        int q = attrEnd - 1;
        while (q > attrStart && char.IsWhiteSpace(tag[q])) q--;
        if (!isEndTag && q > attrStart && tag[q] == '/')
            attrEnd = q;

        attrs = tag.Slice(attrStart, attrEnd - attrStart);
        return true;
    }

    private static bool EqualsIgnoreCase(ReadOnlySpan<char> a, string b)
        => a.Equals(b.AsSpan(), StringComparison.OrdinalIgnoreCase);

    // ------------------------------------------------------------
    // Text handling (normalize + optional entity decode) + OPTIONAL map
    // ------------------------------------------------------------

    // POSITION MAP HELPERS:
    // map.Count == sb.Length + 1



    private static void MapAppendChar(StringBuilder sb, List<int>? map, char c, int xmlCaretIndexAfter)
    {
        sb.Append(c);
        map?.Add(xmlCaretIndexAfter);
    }

    private static void MapRemoveLastChar(StringBuilder sb, List<int>? map)
    {
        if (sb.Length <= 0) return;

        sb.Length--;

        if (map != null)
        {
            // After removing one character, map must have one extra entry
            // corresponding to the removed caret position.
            if (map.Count == sb.Length + 2)
            {
                map.RemoveAt(map.Count - 1);
            }
            else
            {
                throw new InvalidOperationException(
                    $"MapRemoveLastChar invariant violated. " +
                    $"sb.Length={sb.Length}, map.Count={map.Count}");
            }
        }
    }

    private static void AppendText(StringBuilder outSb, List<int>? map, ReadOnlySpan<char> raw, int absStartXmlIndex, ref bool lastWasNewline)
    {
        if (raw.Length == 0) return;

        bool wroteAnyNonWs = false;
        bool pendingSpace = false;

        // If we are appending onto non-ws and our first real char is non-ws, we insert a space.
        bool hadOutputBefore = outSb.Length > 0;
        bool prevIsWs = hadOutputBefore && char.IsWhiteSpace(outSb[outSb.Length - 1]);
        bool needBoundarySpace = hadOutputBefore && !prevIsWs;

        bool boundarySpaceEmitted = false;

        void EmitBoundarySpaceIfNeeded(int xmlIndexForInserted)
        {
            if (!needBoundarySpace || boundarySpaceEmitted) return;

            // Space inserted BETWEEN chunks: caret after space is still at insertion point.
            MapAppendChar(outSb, map, ' ', xmlCaretIndexAfter: xmlIndexForInserted);

            boundarySpaceEmitted = true;
            wroteAnyNonWs = true;
            needBoundarySpace = false;
        }

        void EmitCollapsedSpaceIfNeeded(int xmlIndexWhereSpaceOccurs)
        {
            if (!pendingSpace) return;
            pendingSpace = false;

            // We only emit a collapsed space if we already wrote something in this chunk
            // AND last output isn't whitespace.
            if (wroteAnyNonWs && outSb.Length > 0 && !char.IsWhiteSpace(outSb[outSb.Length - 1]))
            {
                // Collapsed whitespace: this space is "inserted" at the next-non-ws XML index.
                // Caret after the space is still at that insertion point (before the next real char).
                MapAppendChar(outSb, map, ' ', xmlCaretIndexAfter: xmlIndexWhereSpaceOccurs);
            }
        }

        void EmitRealChar(char c, int xmlCharIndex, int xmlCaretAfter)
        {
            if (c == '\r') return;

            if (c == ' ' || c == '\t' || c == '\n' || c == '\f' || c == '\v')
            {
                pendingSpace = true;
                return;
            }

            // If we had whitespace, emit at most one space at the insertion point (before this char).
            EmitCollapsedSpaceIfNeeded(xmlIndexWhereSpaceOccurs: xmlCharIndex);

            // If this is the first real char and we need a boundary space, emit it.
            EmitBoundarySpaceIfNeeded(xmlIndexForInserted: xmlCharIndex);

            // Emit the actual character. Caret after it should map to AFTER the source char.
            MapAppendChar(outSb, map, c, xmlCaretIndexAfter: xmlCaretAfter);

            wroteAnyNonWs = true;
        }

        int i = 0;
        while (i < raw.Length)
        {
            char c = raw[i];

            if (c == '&')
            {
                int entityStartRel = i;
                int entityStartAbs = absStartXmlIndex + entityStartRel;

                int before = i;
                if (TryDecodeEntity(raw, ref i, out var decodedChar, out var decodedString))
                {
                    // i now points to the char AFTER ';' in raw (relative index)
                    int entityEndAbsExclusive = absStartXmlIndex + i;

                    if (decodedString != null)
                    {
                        // Surrogate pair etc. Both map to caret after the entire entity.
                        for (int k = 0; k < decodedString.Length; k++)
                            EmitRealChar(decodedString[k], xmlCharIndex: entityStartAbs, xmlCaretAfter: entityEndAbsExclusive);
                    }
                    else
                    {
                        EmitRealChar(decodedChar, xmlCharIndex: entityStartAbs, xmlCaretAfter: entityEndAbsExclusive);
                    }
                    continue;
                }

                // failed decode -> literal '&' from entityStartAbs
                EmitRealChar('&', xmlCharIndex: entityStartAbs, xmlCaretAfter: entityStartAbs + 1);
                i = before + 1;
                continue;
            }

            int abs = absStartXmlIndex + i;
            EmitRealChar(c, xmlCharIndex: abs, xmlCaretAfter: abs + 1);
            i++;
        }

        if (!wroteAnyNonWs)
            return;

        lastWasNewline = outSb.Length > 0 && outSb[outSb.Length - 1] == '\n';
    }

    /// <summary>
    /// Appends visible characters from a raw span into a plain-text StringBuilder (for heading capture).
    /// Skips CR, collapses whitespace runs. No index-map tracking needed.
    /// </summary>
    private static void AppendPlainText(StringBuilder dst, ReadOnlySpan<char> raw)
    {
        for (int j = 0; j < raw.Length; j++)
        {
            char ch = raw[j];
            if (ch == '\r') continue;
            if (ch == '\n' || char.IsWhiteSpace(ch))
            {
                if (dst.Length > 0 && !char.IsWhiteSpace(dst[dst.Length - 1]))
                    dst.Append(' ');
                continue;
            }
            dst.Append(ch);
        }
    }

    private static void AppendNewline(StringBuilder sb, List<int>? map, int xmlIndexForInserted, ref bool lastWasNewline)
    {
        if (!lastWasNewline)
        {
            // Inserted newline: caret after newline maps to the insertion point in XML.
            MapAppendChar(sb, map, '\n', xmlCaretIndexAfter: xmlIndexForInserted);
        }
        lastWasNewline = true;
    }

    private static void EnsureParagraphBreak(StringBuilder sb, List<int>? map, int xmlIndexForInserted, ref bool lastWasNewline)
    {
        if (sb.Length == 0)
        {
            lastWasNewline = false;
            return;
        }

        // Trim trailing spaces/tabs/CR (if any), keeping map invariant.
        while (sb.Length > 0 && (sb[^1] == ' ' || sb[^1] == '\t' || sb[^1] == '\r'))
            MapRemoveLastChar(sb, map);

        int trailingNewlines = 0;
        for (int i = sb.Length - 1; i >= 0 && sb[i] == '\n'; i--)
            trailingNewlines++;

        if (trailingNewlines == 0)
        {
            MapAppendChar(sb, map, '\n', xmlCaretIndexAfter: xmlIndexForInserted);
            MapAppendChar(sb, map, '\n', xmlCaretIndexAfter: xmlIndexForInserted);
        }
        else if (trailingNewlines == 1)
        {
            MapAppendChar(sb, map, '\n', xmlCaretIndexAfter: xmlIndexForInserted);
        }

        lastWasNewline = true;
    }

    /// <summary>
    /// Decodes &amp; &lt; &gt; &quot; &apos; plus numeric: &#123; and hex: &#x1F600;
    /// Advances i to the character after ';' on success.
    /// On failure, leaves i unchanged and returns false.
    /// </summary>
    private static bool TryDecodeEntity(ReadOnlySpan<char> s, ref int i, out char ch, out string? str)
    {
        ch = default;
        str = null;

        int start = i;
        if (i >= s.Length || s[i] != '&') return false;

        int semiRel = s.Slice(i).IndexOf(';');
        if (semiRel < 0) return false;
        int semi = i + semiRel;

        var ent = s.Slice(i + 1, semi - (i + 1));

        // named
        if (ent.SequenceEqual("amp".AsSpan())) { ch = '&'; i = semi + 1; return true; }
        if (ent.SequenceEqual("lt".AsSpan())) { ch = '<'; i = semi + 1; return true; }
        if (ent.SequenceEqual("gt".AsSpan())) { ch = '>'; i = semi + 1; return true; }
        if (ent.SequenceEqual("quot".AsSpan())) { ch = '"'; i = semi + 1; return true; }
        if (ent.SequenceEqual("apos".AsSpan())) { ch = '\''; i = semi + 1; return true; }

        // numeric: &#...; or &#x...;
        if (ent.Length >= 2 && ent[0] == '#')
        {
            bool hex = ent.Length >= 3 && (ent[1] == 'x' || ent[1] == 'X');
            int value = 0;

            try
            {
                if (hex)
                {
                    for (int k = 2; k < ent.Length; k++)
                    {
                        int d = HexVal(ent[k]);
                        if (d < 0) { i = start; return false; }
                        value = checked(value * 16 + d);
                    }
                }
                else
                {
                    for (int k = 1; k < ent.Length; k++)
                    {
                        char c = ent[k];
                        if (c < '0' || c > '9') { i = start; return false; }
                        value = checked(value * 10 + (c - '0'));
                    }
                }
            }
            catch (OverflowException)
            {
                i = start;
                return false;
            }

            if (value <= 0) { i = start; return false; }

            if (value <= 0xFFFF)
            {
                ch = (char)value;
                i = semi + 1;
                return true;
            }

            if (value <= 0x10FFFF)
            {
                value -= 0x10000;
                char high = (char)((value >> 10) + 0xD800);
                char low = (char)((value & 0x3FF) + 0xDC00);
                str = new string(new[] { high, low });
                i = semi + 1;
                return true;
            }

            i = start;
            return false;
        }

        i = start;
        return false;
    }

    private static int HexVal(char c)
    {
        if (c >= '0' && c <= '9') return c - '0';
        if (c >= 'a' && c <= 'f') return 10 + (c - 'a');
        if (c >= 'A' && c <= 'F') return 10 + (c - 'A');
        return -1;
    }

    // ------------------------------------------------------------
    // Segment keys + attribute parsing (no regex)
    // ------------------------------------------------------------

    private static bool TryMakeSyncKey(ReadOnlySpan<char> tagName, ReadOnlySpan<char> attrs, out string key)
    {
        key = "";

        if (EqualsIgnoreCase(tagName, "lb"))
        {
            var n = Attr(attrs, AttrN);
            var ed = Attr(attrs, AttrEd);
            key = MakeKey("lb", n, ed);
            return true;
        }

        if (EqualsIgnoreCase(tagName, "pb"))
        {
            var id = Attr(attrs, AttrXmlId) ?? Attr(attrs, AttrN);
            var ed = Attr(attrs, AttrEd);
            key = MakeKey("pb", id, ed);
            return true;
        }

        if (EqualsIgnoreCase(tagName, "p"))
        {
            var id = Attr(attrs, AttrXmlId);
            if (string.IsNullOrWhiteSpace(id)) return false;
            key = MakeKey("p", id);
            return true;
        }

        // Anchors are never structural boundaries — they mark note positions
        // and annotation ranges. Note rendering uses anchorPosById (character
        // offsets recorded at line ~296), not segment boundaries. Creating
        // segments from anchors breaks selection sync by splitting lb lines.
        if (EqualsIgnoreCase(tagName, "anchor"))
        {
            key = "";
            return false;
        }

        if (EqualsIgnoreCase(tagName, "l"))
        {
            var n = Attr(attrs, AttrN);
            if (string.IsNullOrWhiteSpace(n)) return false;
            key = MakeKey("l", n);
            return true;
        }

        if (EqualsIgnoreCase(tagName, "cb:juan"))
        {
            var n = Attr(attrs, AttrN);
            var fun = Attr(attrs, AttrFun);
            key = MakeKey("cb:juan", n, fun);
            return true;
        }

        return false;
    }

    private static string MakeKey(string baseName, params string?[] parts)
    {
        var filtered = parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()).ToList();
        if (filtered.Count == 0) return baseName;
        return $"{baseName}|{string.Join("|", filtered)}";
    }

    /// <summary>
    /// Extract attribute value from an attribute span.
    /// Supports double quotes and (defensively) single quotes.
    /// Returns null if not found.
    /// </summary>
    private static string? Attr(ReadOnlySpan<char> attrs, string attrName)
    {
        if (attrs.Length == 0 || string.IsNullOrEmpty(attrName))
            return null;

        ReadOnlySpan<char> needle = attrName.AsSpan();

        int i = 0;
        while (i < attrs.Length)
        {
            while (i < attrs.Length && char.IsWhiteSpace(attrs[i])) i++;
            if (i >= attrs.Length) break;

            int eqRel = attrs.Slice(i).IndexOf('=');
            if (eqRel < 0) break;
            int eq = i + eqRel;

            int nameEnd = eq - 1;
            while (nameEnd >= i && char.IsWhiteSpace(attrs[nameEnd])) nameEnd--;
            if (nameEnd < i) { i = eq + 1; continue; }

            var candName = attrs.Slice(i, nameEnd - i + 1);

            int j = eq + 1;
            while (j < attrs.Length && char.IsWhiteSpace(attrs[j])) j++;
            if (j >= attrs.Length) { i = eq + 1; continue; }

            char quote = attrs[j];
            if (quote != '"' && quote != '\'')
            {
                i = eq + 1;
                continue;
            }

            j++;
            int start = j;
            int endRel = attrs.Slice(start).IndexOf(quote);
            if (endRel < 0) return null;
            int end = start + endRel;

            if (candName.Equals(needle, StringComparison.Ordinal))
                return attrs.Slice(start, end - start).ToString();

            i = end + 1;
        }

        return null;
    }

    private static string? InferNoteKindFromId(string id)
    {
        if (id.StartsWith("nkr_note_mod_", StringComparison.Ordinal)) return "mod";
        if (id.StartsWith("nkr_note_orig_", StringComparison.Ordinal)) return "orig";
        if (id.StartsWith("nkr_note_add_", StringComparison.Ordinal)) return "add";

        // OpenZen critical-edition note kinds
        if (id.StartsWith("nkr_note_crit_", StringComparison.Ordinal)) return "crit";
        if (id.StartsWith("nkr_note_prov_", StringComparison.Ordinal)) return "prov";
        if (id.StartsWith("nkr_note_trans_", StringComparison.Ordinal)) return "trans";
        if (id.StartsWith("nkr_note_unres_", StringComparison.Ordinal)) return "unres";
        if (id.StartsWith("nkr_note_proc_", StringComparison.Ordinal)) return "proc";

        return null;
    }
}