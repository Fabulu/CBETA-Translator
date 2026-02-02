using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Text;

/// <summary>
/// Converts TEI/CBETA-ish XML into readable text WITH stable segment keys.
/// Fine-grained segmentation (preferred):
/// - Start new segment on sync-tags: lb, pb, p(xml:id), anchor, cb:juan
/// - Render lb as newline, pb/p/head as paragraph break
/// Not a full XML parser; tokenizes tags vs text.
/// </summary>
public static class CbetaTeiRenderer
{
    private static readonly Regex TokenRegex = new Regex(@"(<[^>]+>)", RegexOptions.Compiled);

    private static readonly Regex StartTagRegex = new Regex(
        @"^<(?<tagName>(?:cb:)?[A-Za-z]+)\b(?<attrs>[^<>]*?)(?<self>/?)>$",
        RegexOptions.Compiled);

    private static readonly Regex EndTagRegex = new Regex(
        @"^</(?<tagName>(?:cb:)?[A-Za-z]+)\s*>$",
        RegexOptions.Compiled);

    public static RenderedDocument Render(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return RenderedDocument.Empty;

        var sb = new StringBuilder(xml.Length);
        var segments = new List<RenderSegment>(capacity: 4096);

        string currentKey = "START";
        int segStart = 0;

        int teiHeaderDepth = 0;
        bool lastWasNewline = false;

        void StartNewSegment(string newKey)
        {
            int end = sb.Length;
            if (end > segStart)
                segments.Add(new RenderSegment(currentKey, segStart, end));

            currentKey = newKey;
            segStart = sb.Length;
        }

        StartNewSegment("START");

        foreach (var token in Tokenize(xml))
        {
            if (token.IsTag)
            {
                var t = token.Value;

                // End tag
                var endMatch = EndTagRegex.Match(t);
                if (endMatch.Success)
                {
                    var tagNameEnd = endMatch.Groups["tagName"].Value;

                    if (tagNameEnd.Equals("teiHeader", StringComparison.OrdinalIgnoreCase))
                        teiHeaderDepth = Math.Max(0, teiHeaderDepth - 1);

                    // Paragraph end spacing (outside header)
                    if (teiHeaderDepth == 0 && tagNameEnd.Equals("p", StringComparison.OrdinalIgnoreCase))
                        EnsureParagraphBreak(sb, ref lastWasNewline);

                    continue;
                }

                // Start or self-closing tag
                var startMatch = StartTagRegex.Match(t);
                if (!startMatch.Success)
                    continue;

                var tagName = startMatch.Groups["tagName"].Value;
                var attrs = startMatch.Groups["attrs"].Value;

                if (tagName.Equals("teiHeader", StringComparison.OrdinalIgnoreCase))
                {
                    teiHeaderDepth++;
                    continue;
                }

                if (teiHeaderDepth > 0)
                    continue;

                // Segment boundary keys (fine-grained)
                if (TryMakeSyncKey(tagName, attrs, out var key))
                    StartNewSegment(key);

                // Rendering structural breaks
                if (tagName.Equals("lb", StringComparison.OrdinalIgnoreCase))
                {
                    AppendNewline(sb, ref lastWasNewline);
                    continue;
                }

                if (tagName.Equals("pb", StringComparison.OrdinalIgnoreCase))
                {
                    EnsureParagraphBreak(sb, ref lastWasNewline);
                    continue;
                }

                if (tagName.Equals("p", StringComparison.OrdinalIgnoreCase))
                {
                    EnsureParagraphBreak(sb, ref lastWasNewline);
                    continue;
                }

                if (tagName.Equals("head", StringComparison.OrdinalIgnoreCase))
                {
                    EnsureParagraphBreak(sb, ref lastWasNewline);
                    continue;
                }

                continue;
            }
            else
            {
                if (teiHeaderDepth > 0)
                    continue;

                var text = WebUtility.HtmlDecode(token.Value);
                text = NormalizeTextNode(text);
                if (text.Length == 0)
                    continue;

                if (sb.Length > 0)
                {
                    char prev = sb[sb.Length - 1];
                    if (!char.IsWhiteSpace(prev) && !char.IsWhiteSpace(text[0]))
                        sb.Append(' ');
                }

                sb.Append(text);
                lastWasNewline = sb.Length > 0 && sb[sb.Length - 1] == '\n';
            }
        }

        // Close last segment
        int finalEnd = sb.Length;
        if (finalEnd > segStart)
            segments.Add(new RenderSegment(currentKey, segStart, finalEnd));

        // IMPORTANT: do not post-normalize in a way that changes length; offsets must stay valid.
        var renderedText = sb.ToString();

        segments.Sort((a, b) => a.Start.CompareTo(b.Start));
        return new RenderedDocument(renderedText, segments);
    }

    private static IEnumerable<(bool IsTag, string Value)> Tokenize(string xml)
    {
        int last = 0;
        foreach (Match m in TokenRegex.Matches(xml))
        {
            if (m.Index > last)
                yield return (false, xml.Substring(last, m.Index - last));

            yield return (true, m.Value);
            last = m.Index + m.Length;
        }

        if (last < xml.Length)
            yield return (false, xml.Substring(last));
    }

    private static void AppendNewline(StringBuilder sb, ref bool lastWasNewline)
    {
        if (!lastWasNewline)
            sb.Append('\n');
        lastWasNewline = true;
    }

    private static void EnsureParagraphBreak(StringBuilder sb, ref bool lastWasNewline)
    {
        if (sb.Length == 0)
        {
            lastWasNewline = false;
            return;
        }

        while (sb.Length > 0 && (sb[^1] == ' ' || sb[^1] == '\t' || sb[^1] == '\r'))
            sb.Length--;

        int trailingNewlines = 0;
        for (int i = sb.Length - 1; i >= 0 && sb[i] == '\n'; i--)
            trailingNewlines++;

        if (trailingNewlines == 0) sb.Append("\n\n");
        else if (trailingNewlines == 1) sb.Append('\n');

        lastWasNewline = true;
    }

    private static string NormalizeTextNode(string s)
    {
        s = s.Replace("\r", "");
        s = Regex.Replace(s, @"[ \t\f\v]+", " ");
        return s.Trim();
    }

    private static bool TryMakeSyncKey(string tagName, string attrs, out string key)
    {
        key = "";

        switch (tagName)
        {
            case "lb":
                {
                    var n = Attr(attrs, "n");
                    var ed = Attr(attrs, "ed");
                    key = MakeKey("lb", n, ed);
                    return true;
                }
            case "pb":
                {
                    var id = Attr(attrs, "xml:id") ?? Attr(attrs, "n");
                    var ed = Attr(attrs, "ed");
                    key = MakeKey("pb", id, ed);
                    return true;
                }
            case "p":
                {
                    var id = Attr(attrs, "xml:id");
                    if (string.IsNullOrWhiteSpace(id)) return false; // only meaningful when xml:id present
                    key = MakeKey("p", id);
                    return true;
                }
            case "anchor":
                {
                    var id = Attr(attrs, "xml:id") ?? Attr(attrs, "n");
                    if (string.IsNullOrWhiteSpace(id)) return false;
                    key = MakeKey("anchor", id);
                    return true;
                }
            case "cb:juan":
                {
                    var n = Attr(attrs, "n");
                    var fun = Attr(attrs, "fun");
                    key = MakeKey("cb:juan", n, fun);
                    return true;
                }
            default:
                return false;
        }
    }

    private static string MakeKey(string baseName, params string?[] parts)
    {
        var filtered = parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()).ToList();
        if (filtered.Count == 0) return baseName;
        return $"{baseName}|{string.Join("|", filtered)}";
    }

    private static string? Attr(string attrs, string attrName)
    {
        var re = new Regex(@"\b" + Regex.Escape(attrName) + @"\s*=\s*""(?<v>[^""]*)""", RegexOptions.Compiled);
        var m = re.Match(attrs);
        return m.Success ? m.Groups["v"].Value : null;
    }
}
