using System;
using System.Collections.Generic;

namespace ReadZen.App.Services;

/// <summary>
/// Builds a lookup from TeiRenderer segment keys (e.g. "l|22") to TEI locus URIs
/// (e.g. "urn:locus:T1-p031.l01") by scanning the source XML for &lt;l&gt; elements
/// with n and corresp attributes.
/// </summary>
public static class LociMappingService
{
    /// <summary>
    /// Scans XML for &lt;l n="X" corresp="Y"&gt; elements and returns a dictionary
    /// mapping segment key "l|{n}" to the corresp value.
    /// Also captures the type attribute if present (e.g. type="omission_judgment").
    /// </summary>
    public static Dictionary<string, LociEntry> BuildFromXml(string xml)
    {
        var map = new Dictionary<string, LociEntry>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(xml)) return map;

        // Fast tag scanner (same approach as TeiRenderer — no DOM, no regex)
        var span = xml.AsSpan();
        int pos = 0;
        while (pos < span.Length)
        {
            int lt = span.Slice(pos).IndexOf('<');
            if (lt < 0) break;
            lt += pos;

            int gt = span.Slice(lt).IndexOf('>');
            if (gt < 0) break;
            gt += lt;

            var tag = span.Slice(lt + 1, gt - lt - 1);

            // Skip closing tags and processing instructions
            if (tag.Length == 0 || tag[0] == '/' || tag[0] == '?' || tag[0] == '!')
            {
                pos = gt + 1;
                continue;
            }

            // Check for self-closing
            bool selfClosing = tag[tag.Length - 1] == '/';
            if (selfClosing) tag = tag.Slice(0, tag.Length - 1);

            // Extract tag name
            int spaceIdx = tag.IndexOf(' ');
            var tagName = spaceIdx < 0 ? tag : tag.Slice(0, spaceIdx);
            var attrs = spaceIdx < 0 ? ReadOnlySpan<char>.Empty : tag.Slice(spaceIdx);

            // Only care about <l> elements
            if (tagName.Length == 1 && tagName[0] == 'l')
            {
                var n = ExtractAttr(attrs, "n");
                var corresp = ExtractAttr(attrs, "corresp");
                var type = ExtractAttr(attrs, "type");

                if (n != null)
                {
                    var key = $"l|{n}";
                    map[key] = new LociEntry(corresp, type);
                }
            }

            pos = gt + 1;
        }

        return map;
    }

    /// <summary>
    /// Tries to get the locus URI for a segment key.
    /// Returns the raw corresp value (e.g. "urn:locus:T1-p031.l01") or null.
    /// </summary>
    public static string? TryGetLocus(Dictionary<string, LociEntry>? map, string segmentKey)
    {
        if (map == null) return null;
        return map.TryGetValue(segmentKey, out var entry) ? entry.Corresp : null;
    }

    /// <summary>
    /// Extracts the bare locus ID from a urn:locus: URI.
    /// "urn:locus:T1-p031.l01" → "T1-p031.l01"
    /// </summary>
    public static string? StripLocusUrn(string? locusUri)
    {
        if (locusUri == null) return null;
        const string prefix = "urn:locus:";
        return locusUri.StartsWith(prefix, StringComparison.Ordinal)
            ? locusUri.Substring(prefix.Length)
            : locusUri;
    }

    private static string? ExtractAttr(ReadOnlySpan<char> attrs, string name)
    {
        // Search for name="value" or name='value'
        int searchFrom = 0;
        while (searchFrom < attrs.Length)
        {
            int idx = attrs.Slice(searchFrom).IndexOf(name.AsSpan(), StringComparison.Ordinal);
            if (idx < 0) return null;
            idx += searchFrom;

            // Must be preceded by whitespace (or start of attrs) and followed by =
            if (idx > 0 && attrs[idx - 1] != ' ' && attrs[idx - 1] != '\t')
            {
                searchFrom = idx + name.Length;
                continue;
            }

            int eqPos = idx + name.Length;
            // Skip whitespace around =
            while (eqPos < attrs.Length && attrs[eqPos] == ' ') eqPos++;
            if (eqPos >= attrs.Length || attrs[eqPos] != '=')
            {
                searchFrom = eqPos;
                continue;
            }
            eqPos++;
            while (eqPos < attrs.Length && attrs[eqPos] == ' ') eqPos++;

            if (eqPos >= attrs.Length) return null;
            char quote = attrs[eqPos];
            if (quote != '"' && quote != '\'') return null;
            int valStart = eqPos + 1;
            int valEnd = attrs.Slice(valStart).IndexOf(quote);
            if (valEnd < 0) return null;
            return attrs.Slice(valStart, valEnd).ToString();
        }
        return null;
    }
}

/// <summary>
/// A single locus entry extracted from a TEI &lt;l&gt; element,
/// holding the corresp URI and optional type attribute.
/// </summary>
public readonly record struct LociEntry(string? Corresp, string? Type);
