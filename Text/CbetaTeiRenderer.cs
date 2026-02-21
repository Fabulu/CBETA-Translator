// Text/CbetaTeiRenderer.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Text;

/// <summary>
/// Converts TEI/CBETA-ish XML into readable text WITH stable segment keys.
/// Fine-grained segmentation (preferred):
/// - Start new segment on sync-tags: lb, pb, p(xml:id), anchor, cb:juan
/// - Render lb as newline, pb/p/head as paragraph break
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
/// IMPORTANT (connected to MarkdownTranslationService):
/// - Markdown range notes validate/anchor against XML with LF-normalized newlines.
/// - Therefore this renderer also normalizes newlines (CRLF/CR -> LF) BEFORE computing BaseToXmlIndex,
///   so click->xmlIndex matches the merge logic.
/// </summary>
public static class CbetaTeiRenderer
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

    public static RenderedDocument Render(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return RenderedDocument.Empty;

        // CRITICAL: normalize newlines so our baseToXmlIndex matches merge/validation logic.
        xml = NormalizeNewlines(xml);

        var sb = new StringBuilder(xml.Length);
        var baseToXml = new List<int>(capacity: Math.Max(1024, xml.Length / 4));

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

        bool lastWasNewline = false;       // for main sb
        bool noteLastWasNewline = false;   // for noteSb

        void StartNewSegment(string newKey)
        {
            int end = sb.Length;
            if (end > segStart)
                segments.Add(new RenderSegment(currentKey, segStart, end));

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
                if (teiHeaderDepth == 0 && backDepth == 0 && !inNoteCapture)
                    AppendText(sb, baseToXml, s.Slice(i), absStartXmlIndex: i, ref lastWasNewline);
                else if (inNoteCapture)
                    AppendText(noteSb, map: null, s.Slice(i), absStartXmlIndex: i, ref noteLastWasNewline);
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
                else if (teiHeaderDepth == 0 && backDepth == 0)
                {
                    AppendText(sb, baseToXml, rawText, absStartXmlIndex: i, ref lastWasNewline);
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
                else if (teiHeaderDepth == 0 && backDepth == 0)
                    AppendText(sb, baseToXml, tail, absStartXmlIndex: lt, ref lastWasNewline);
                break;
            }

            int gt = lt + relGt;
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

                    // finish note capture
                    if (EqualsIgnoreCase(tagName, "note") && inNoteCapture)
                    {
                        inNoteCapture = false;

                        var noteText = noteSb.ToString().Trim();
                        noteSb.Clear();
                        noteLastWasNewline = false;

                        noteXmlEndExclusive = gt + 1;

                        if (noteAnchorPos >= 0 && !string.IsNullOrWhiteSpace(noteText))
                        {
                            // DocAnnotation: (start, endExclusive, text, kind, resp, xmlStart, xmlEndExclusive)
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

                    // paragraph end spacing (only in main rendered part)
                    if (teiHeaderDepth == 0 && backDepth == 0 && EqualsIgnoreCase(tagName, "p"))
                        EnsureParagraphBreak(sb, baseToXml, xmlIndexForInserted: lt, ref lastWasNewline);
                }
                else
                {
                    // entering blocks
                    if (EqualsIgnoreCase(tagName, "teiHeader"))
                        teiHeaderDepth++;
                    else if (EqualsIgnoreCase(tagName, "back"))
                        backDepth++;

                    // If we're capturing a note and we hit any start-tag: treat as a soft separator
                    if (inNoteCapture)
                    {
                        if (EqualsIgnoreCase(tagName, "lb") ||
                            EqualsIgnoreCase(tagName, "p") ||
                            EqualsIgnoreCase(tagName, "head") ||
                            EqualsIgnoreCase(tagName, "br"))
                        {
                            AppendNewline(noteSb, map: null, xmlIndexForInserted: lt, ref noteLastWasNewline);
                        }
                        else
                        {
                            AppendText(noteSb, map: null, " ".AsSpan(), absStartXmlIndex: lt, ref noteLastWasNewline);
                        }
                    }

                    // Only do segmentation/rendering while not in teiHeader and not in back and not in note capture
                    if (teiHeaderDepth == 0 && backDepth == 0 && !inNoteCapture)
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
                            AppendNewline(sb, baseToXml, xmlIndexForInserted: lt, ref lastWasNewline);
                        }
                        else if (EqualsIgnoreCase(tagName, "pb") ||
                                 EqualsIgnoreCase(tagName, "p") ||
                                 EqualsIgnoreCase(tagName, "head"))
                        {
                            EnsureParagraphBreak(sb, baseToXml, xmlIndexForInserted: lt, ref lastWasNewline);
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

            i = gt + 1;
        }

        // Close last segment (main text only)
        int finalEnd = sb.Length;
        if (finalEnd > segStart)
            segments.Add(new RenderSegment(currentKey, segStart, finalEnd));

        // Build base text
        var baseText = sb.ToString();
        var baseToXmlIndex = baseToXml.ToArray();

        // Insert visible markers/superscripts into the rendered text.
        var (newText, newSegments, markers) =
            AnnotationMarkerInserter.InsertMarkers(baseText, annotations, segments);

        return new RenderedDocument(
            newText,
            newSegments,
            annotations,
            markers,
            baseToXmlIndex: baseToXmlIndex);
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

    private static void AppendText(StringBuilder outSb, List<int>? map, ReadOnlySpan<char> raw, int absStartXmlIndex, ref bool lastWasNewline)
    {
        if (raw.Length == 0) return;

        bool wroteAny = false;
        bool pendingSpace = false;

        // If we are appending onto non-ws and our first real char is non-ws, we insert a space.
        bool hadOutputBefore = outSb.Length > 0;
        bool prevIsWs = hadOutputBefore && char.IsWhiteSpace(outSb[outSb.Length - 1]);
        bool needBoundarySpace = hadOutputBefore && !prevIsWs;

        bool boundarySpaceEmitted = false;

        void EmitBoundarySpaceIfNeeded(int xmlIndexForInserted)
        {
            if (!needBoundarySpace || boundarySpaceEmitted) return;

            outSb.Append(' ');
            map?.Add(xmlIndexForInserted);

            boundarySpaceEmitted = true;
            wroteAny = true;

            needBoundarySpace = false;
        }

        void EmitChar(char c, int xmlIndexForChar)
        {
            if (c == '\r') return;

            if (c == ' ' || c == '\t' || c == '\n' || c == '\f' || c == '\v')
            {
                pendingSpace = true;
                return;
            }

            if (pendingSpace)
            {
                if (wroteAny)
                {
                    if (outSb.Length > 0 && !char.IsWhiteSpace(outSb[outSb.Length - 1]))
                    {
                        outSb.Append(' ');
                        map?.Add(xmlIndexForChar);
                    }
                }
                pendingSpace = false;
            }

            EmitBoundarySpaceIfNeeded(xmlIndexForChar);

            outSb.Append(c);
            map?.Add(xmlIndexForChar);

            wroteAny = true;
        }

        // Scan raw span. Decode entities only if we see '&'.
        int i = 0;
        while (i < raw.Length)
        {
            char c = raw[i];

            if (c == '&')
            {
                int entityStartAbs = absStartXmlIndex + i;

                if (TryDecodeEntity(raw, ref i, out var decodedChar, out var decodedString))
                {
                    if (decodedString != null)
                    {
                        for (int k = 0; k < decodedString.Length; k++)
                            EmitChar(decodedString[k], entityStartAbs);
                    }
                    else
                    {
                        EmitChar(decodedChar, entityStartAbs);
                    }
                    continue;
                }

                // failed decode -> literal '&'
                EmitChar('&', entityStartAbs);
                i++;
                continue;
            }

            EmitChar(c, absStartXmlIndex + i);
            i++;
        }

        if (!wroteAny)
            return;

        lastWasNewline = outSb.Length > 0 && outSb[outSb.Length - 1] == '\n';
    }

    private static void AppendNewline(StringBuilder sb, List<int>? map, int xmlIndexForInserted, ref bool lastWasNewline)
    {
        if (!lastWasNewline)
        {
            sb.Append('\n');
            map?.Add(xmlIndexForInserted);
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

        while (sb.Length > 0 && (sb[^1] == ' ' || sb[^1] == '\t' || sb[^1] == '\r'))
        {
            sb.Length--;
            if (map != null && map.Count > sb.Length)
                map.RemoveAt(map.Count - 1);
        }

        int trailingNewlines = 0;
        for (int i = sb.Length - 1; i >= 0 && sb[i] == '\n'; i--)
            trailingNewlines++;

        if (trailingNewlines == 0)
        {
            sb.Append('\n'); map?.Add(xmlIndexForInserted);
            sb.Append('\n'); map?.Add(xmlIndexForInserted);
        }
        else if (trailingNewlines == 1)
        {
            sb.Append('\n'); map?.Add(xmlIndexForInserted);
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

        if (EqualsIgnoreCase(tagName, "anchor"))
        {
            var id = Attr(attrs, AttrXmlId) ?? Attr(attrs, AttrN);
            if (string.IsNullOrWhiteSpace(id)) return false;
            key = MakeKey("anchor", id);
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
        return null;
    }

    private static string NormalizeNewlines(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}