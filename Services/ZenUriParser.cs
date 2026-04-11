using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Converts between <c>zen://</c> deep-link URIs and <see cref="NavigationRequest"/> objects.
/// <para>
/// Clean format: <c>zen://T48n2005/0292a26-0292a29/en?highlight=...&amp;lctx=...&amp;rctx=...&amp;block=...</c>
/// </para>
/// <para>
/// Legacy format (still parsed for backward compatibility):
/// <c>zen://T/T48/T48n2005.xml?from=0001a01&amp;to=0001a03&amp;side=...&amp;highlight=...</c>
/// </para>
/// </summary>
public static class ZenUriParser
{
    public const string Scheme = "zen";

    /// <summary>Base URL for shareable HTTPS links (e.g. for Reddit/Discord).</summary>
    public const string ShareableBase = "https://readzen.pages.dev/";

    /// <summary>
    /// Known publisher prefixes for OpenZenTexts file IDs. The dot-separated
    /// format (e.g. "ws.gateless-barrier") is the canonical OpenZen ID shape;
    /// the prefix corresponds to a top-level subfolder under xml-open/.
    /// </summary>
    private static readonly System.Collections.Generic.HashSet<string> OpenZenPublishers
        = new(StringComparer.OrdinalIgnoreCase) { "ws", "pd", "ce", "mit" };

    /// <summary>
    /// Returns true if the given file ID is in OpenZenTexts format
    /// ({publisher}.{slug}, e.g. "ws.gateless-barrier").
    /// </summary>
    public static bool IsOpenZenFileId(string fileId)
    {
        if (string.IsNullOrEmpty(fileId)) return false;
        int dotIdx = fileId.IndexOf('.');
        if (dotIdx < 1 || dotIdx >= fileId.Length - 1) return false;
        var prefix = fileId[..dotIdx];
        return OpenZenPublishers.Contains(prefix);
    }

    /// <summary>
    /// Converts a compact file ID to its relative path. Handles both formats:
    ///   - CBETA:    "T48n2005"          -> "T/T48/T48n2005.xml"
    ///   - OpenZen:  "ws.gateless-barrier" -> "ws/gateless-barrier/gateless-barrier.xml"
    /// Returns <c>null</c> for unrecognised file IDs.
    /// </summary>
    public static string? FileIdToRelPath(string fileId)
    {
        if (string.IsNullOrEmpty(fileId)) return null;

        // OpenZen format: {publisher}.{slug}
        if (IsOpenZenFileId(fileId))
        {
            int dotIdx = fileId.IndexOf('.');
            var publisher = fileId[..dotIdx];
            var slug = fileId[(dotIdx + 1)..];
            return $"{publisher}/{slug}/{slug}.xml";
        }

        // CBETA format: {canon}{volume}n{work}
        var nIdx = fileId.IndexOf('n');
        if (nIdx < 1) return null;
        var vol = fileId[..nIdx];
        var canon = Regex.Replace(vol, "[0-9]", "");
        if (string.IsNullOrEmpty(canon)) return null;
        return $"{canon}/{vol}/{fileId}.xml";
    }

    /// <summary>
    /// Extracts the file ID from a relative path. Handles both formats:
    ///   - CBETA:    "T/T48/T48n2005.xml"          -> "T48n2005"
    ///   - OpenZen:  "ws/gateless-barrier/gateless-barrier.xml" -> "ws.gateless-barrier"
    /// </summary>
    public static string RelPathToFileId(string relPath)
    {
        var normalized = relPath.Replace('\\', '/');
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // OpenZen layout: first segment is a known publisher prefix → return
        // {publisher}.{slug}, where slug is the SECOND segment (the directory
        // that holds the XML file). This preserves the publisher across
        // round-trips and lets the website + desktop app dispatch by prefix.
        if (parts.Length >= 3 && OpenZenPublishers.Contains(parts[0]))
        {
            return $"{parts[0]}.{parts[1]}";
        }
        // CBETA layout: filename without extension is the canonical ID.
        return Path.GetFileNameWithoutExtension(normalized);
    }

    /// <summary>
    /// Attempts to parse a <c>zen://</c> URI into a <see cref="NavigationRequest"/>.
    /// Supports both the clean format (<c>zen://T48n2005/0292a26-0292a29/en</c>)
    /// and the legacy format (<c>zen://T/T48/T48n2005.xml?from=...&amp;side=...</c>).
    /// Returns <c>null</c> if the URI is malformed or uses a different scheme.
    /// </summary>
    public static NavigationRequest? TryParse(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return null;

        // Convert shareable HTTPS URLs to zen:// format before parsing.
        // e.g. https://readzen.pages.dev/#/T48n2005/0292b29?side=Translated
        //    ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€žÂ¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¾ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ zen://T48n2005/0292b29?side=Translated
        if (uri.StartsWith("https://readzen.pages.dev/", StringComparison.OrdinalIgnoreCase) ||
            uri.StartsWith("http://readzen.pages.dev/", StringComparison.OrdinalIgnoreCase))
        {
            int hashIdx = uri.IndexOf("#/", StringComparison.Ordinal);
            if (hashIdx >= 0)
            {
                uri = Scheme + "://" + uri[(hashIdx + 2)..];
            }
            else if (Uri.TryCreate(uri, UriKind.Absolute, out var shareable))
            {
                var path = shareable.AbsolutePath.Trim('/');
                if (!string.IsNullOrWhiteSpace(path))
                {
                    uri = Scheme + "://" + path;
                    if (!string.IsNullOrWhiteSpace(shareable.Query))
                        uri += shareable.Query;
                }
            }
        }

        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
            return null;

        if (!string.Equals(parsed.Scheme, Scheme, StringComparison.OrdinalIgnoreCase))
            return null;

        // Extract everything after "zen://" from the original string to preserve case.
        var schemePrefix = Scheme + "://";
        var pathStart = uri.IndexOf(schemePrefix, StringComparison.OrdinalIgnoreCase);
        if (pathStart < 0) return null;
        pathStart += schemePrefix.Length;

        var afterScheme = uri.Substring(pathStart);

        // Detect legacy format: contains ".xml" in the path portion
        var qIdx = afterScheme.IndexOf('?');
        var pathPart = qIdx >= 0 ? afterScheme[..qIdx] : afterScheme;

        if (pathPart.Contains(".xml", StringComparison.OrdinalIgnoreCase))
            return TryParseLegacy(afterScheme, parsed.Query);

        return TryParseClean(afterScheme);
    }

    /// <summary>
    /// Parses the legacy format: <c>zen://T/T48/T48n2005.xml?from=...&amp;side=...</c>
    /// </summary>
    private static NavigationRequest? TryParseLegacy(string afterScheme, string queryString)
    {
        var qIdx = afterScheme.IndexOf('?');
        var relPath = qIdx >= 0 ? afterScheme[..qIdx] : afterScheme;
        relPath = Uri.UnescapeDataString(relPath).TrimStart('/');
        if (string.IsNullOrEmpty(relPath))
            return null;

        var query = ParseQueryString(queryString);

        var request = new NavigationRequest
        {
            RelPath = relPath,
        };

        if (query.TryGetValue("from", out var fromLb) && !string.IsNullOrEmpty(fromLb))
            request.FromLb = fromLb;

        if (query.TryGetValue("to", out var toLb) && !string.IsNullOrEmpty(toLb))
            request.ToLb = toLb;

        if (query.TryGetValue("highlight", out var highlight))
            request.MatchText = highlight;

        if (query.TryGetValue("side", out var sideStr)
            && Enum.TryParse<SearchSide>(sideStr, ignoreCase: true, out var side))
        {
            request.Side = side;
        }

        if (query.TryGetValue("lctx", out var lctx))
            request.LeftContext = lctx;

        if (query.TryGetValue("rctx", out var rctx))
            request.RightContext = rctx;

        if (query.TryGetValue("block", out var blockStr)
            && int.TryParse(blockStr, out var block))
        {
            request.AnchorStartHint = block;
        }

        return request;
    }

    /// <summary>
    /// Returns <c>true</c> if the segment looks like a TEI lb n-value (e.g. "0292b28", "0001a01-0001a03").
    /// Pattern: starts with 4 digits followed by a lowercase letter and at least 2 more digits.
    /// </summary>
    private static bool IsLbSegment(string segment)
    {
        // A range like "0292a26-0292a29" also counts ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€žÂ¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Â¦Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â check the first part before any dash.
        var first = segment.Split('-')[0];
        return first.Length >= 7
            && char.IsDigit(first[0]) && char.IsDigit(first[1])
            && char.IsDigit(first[2]) && char.IsDigit(first[3])
            && char.IsLetter(first[4]) && char.IsLower(first[4])
            && char.IsDigit(first[5]) && char.IsDigit(first[6]);
    }

    /// <summary>
    /// Returns <c>true</c> if the segment is a recognised side alias ("en" or "tran").
    /// </summary>
    private static bool IsSideSegment(string segment)
        => segment.Equals("en", StringComparison.OrdinalIgnoreCase)
        || segment.Equals("tran", StringComparison.OrdinalIgnoreCase);

    private static string PaneToSegment(ComparePaneTarget pane) => pane switch
    {
        ComparePaneTarget.Original => "orig",
        ComparePaneTarget.TranslationA => "a",
        ComparePaneTarget.TranslationB => "b",
        _ => "orig"
    };

    private static ComparePaneTarget? TryParsePaneSegment(string? segment) => segment?.Trim().ToLowerInvariant() switch
    {
        "orig" or "original" => ComparePaneTarget.Original,
        "a" or "passage1" or "translationa" => ComparePaneTarget.TranslationA,
        "b" or "passage2" or "translationb" => ComparePaneTarget.TranslationB,
        _ => null,
    };

    /// <summary>
    /// Parses the clean format: <c>zen://T48n2005/0292a26-0292a29/en/bob?highlight=...</c>
    /// </summary>
    private static NavigationRequest? TryParseClean(string afterScheme)
    {
        var qIdx = afterScheme.IndexOf('?');
        var pathPart = qIdx >= 0 ? afterScheme[..qIdx] : afterScheme;
        var queryPart = qIdx >= 0 ? afterScheme[(qIdx + 1)..] : "";

        var parts = pathPart.Split('/').Where(s => !string.IsNullOrEmpty(s)).ToArray();
        if (parts.Length == 0) return null;

        var fileId = parts[0];
        var relPath = FileIdToRelPath(fileId);
        if (relPath == null) return null;

        string? fromLb = null, toLb = null;
        var side = SearchSide.Original;
        string? user = null;

        // Walk remaining segments: each is lb-range, side, or user (last unrecognised segment).
        for (int i = 1; i < parts.Length; i++)
        {
            var seg = parts[i];
            if (IsSideSegment(seg))
            {
                side = SearchSide.Translated;
            }
            else if (IsLbSegment(seg))
            {
                var bounds = seg.Split('-');
                fromLb = bounds[0];
                if (bounds.Length > 1) toLb = bounds[1];
            }
            else
            {
                // Anything else that's the last segment is the user.
                // If it's not last, ignore (defensive).
                if (i == parts.Length - 1)
                    user = Uri.UnescapeDataString(seg);
            }
        }

        var request = new NavigationRequest
        {
            RelPath = relPath,
            Side = side,
            User = user,
        };

        if (!string.IsNullOrEmpty(fromLb))
            request.FromLb = fromLb;
        if (!string.IsNullOrEmpty(toLb))
            request.ToLb = toLb;

        // Parse query params
        var query = ParseQueryString("?" + queryPart);

        if (query.TryGetValue("highlight", out var highlight))
            request.MatchText = highlight;

        if (query.TryGetValue("lctx", out var lctx))
            request.LeftContext = lctx;

        if (query.TryGetValue("rctx", out var rctx))
            request.RightContext = rctx;

        if (query.TryGetValue("block", out var blockStr)
            && int.TryParse(blockStr, out var block))
        {
            request.AnchorStartHint = block;
        }

        return request;
    }

    /// <summary>
    /// Builds a <c>zen://</c> URI in the clean format.
    /// Examples: <c>zen://T48n2005</c>, <c>zen://T48n2005/0292a26-0292a29/en</c>.
    /// Query parameters are used only for highlight, lctx, rctx, and block.
    /// </summary>
    public static string BuildUri(
        string relPath,
        string? fromLb = null,
        string? toLb = null,
        string? highlightText = null,
        SearchSide side = SearchSide.Original,
        string? leftContext = null,
        string? rightContext = null,
        int? blockNumber = null,
        string? user = null)
    {
        var fileId = RelPathToFileId(relPath);
        var uri = Scheme + "://" + fileId;

        if (!string.IsNullOrEmpty(fromLb))
        {
            var range = fromLb;
            if (!string.IsNullOrEmpty(toLb) && toLb != fromLb)
                range += "-" + toLb;
            uri += "/" + range;
        }

        if (side != SearchSide.Original)
            uri += "/en";

        if (!string.IsNullOrEmpty(user))
            uri += "/" + Uri.EscapeDataString(user);

        var queryParts = new List<string>();

        if (!string.IsNullOrEmpty(highlightText))
        {
            // Truncate highlight to keep URLs short ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€žÂ¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Â¦Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â 60 chars is enough to find the right spot
            var truncated = highlightText.Length > 60 ? highlightText[..60] : highlightText;
            // Also strip newlines ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€žÂ¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Â¦Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â they bloat the URL and aren't needed for matching
            truncated = truncated.Replace("\n", "").Replace("\r", "");
            queryParts.Add("highlight=" + Uri.EscapeDataString(truncated));
        }

        if (!string.IsNullOrEmpty(leftContext))
            queryParts.Add("lctx=" + Uri.EscapeDataString(leftContext));

        if (!string.IsNullOrEmpty(rightContext))
            queryParts.Add("rctx=" + Uri.EscapeDataString(rightContext));

        if (blockNumber.HasValue)
            queryParts.Add("block=" + blockNumber.Value.ToString());

        if (queryParts.Count > 0)
            uri += "?" + string.Join("&", queryParts);

        return uri;
    }

    /// <summary>
    /// Builds a shareable HTTPS URL for the given file and optional line-break range.
    /// Format: <c>https://readzen.pages.dev/{fileId}/{fromLb}-{toLb}?side=...&amp;highlight=...</c>
    /// </summary>
    public static string BuildShareableUrl(
        string relPath,
        string? fromLb = null,
        string? toLb = null,
        string? highlightText = null,
        SearchSide side = SearchSide.Original,
        string? user = null)
    {
        // Extract file ID from relPath: "T/T48/T48n2005.xml" ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€žÂ¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¾ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ "T48n2005"
        var fileName = Path.GetFileNameWithoutExtension(relPath.Replace('\\', '/'));
        var url = ShareableBase + Uri.EscapeDataString(fileName);

        // Append lb range as path segment
        if (!string.IsNullOrEmpty(fromLb))
        {
            var range = fromLb;
            if (!string.IsNullOrEmpty(toLb) && toLb != fromLb)
                range += "-" + toLb;
            url += "/" + range;
        }

        // Side as path segment (cleaner than query param)
        if (side != SearchSide.Original)
            url += "/en";

        // User as last path segment
        if (!string.IsNullOrEmpty(user))
            url += "/" + Uri.EscapeDataString(user);

        // Optional query params
        var queryParts = new List<string>();
        if (!string.IsNullOrEmpty(highlightText))
        {
            var truncated = highlightText.Length > 60 ? highlightText[..60] : highlightText;
            truncated = truncated.Replace("\n", "").Replace("\r", "");
            queryParts.Add("highlight=" + Uri.EscapeDataString(truncated));
        }
        if (queryParts.Count > 0)
            url += "?" + string.Join("&", queryParts);

        return url;
    }

    // ---------------------------------------------------------------
    //  Deep-link parsing (supports dict, scholar, search, tags, term)
    // ---------------------------------------------------------------

    /// <summary>
    /// Parses any <c>zen://</c> or shareable HTTPS URL into a <see cref="DeepLinkRequest"/>.
    /// Recognises keyword-prefixed paths (<c>zen://dict/ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€žÂ¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Â¦Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¦</c>, <c>zen://scholar/ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€žÂ¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Â¦Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¦</c>, etc.)
    /// and falls back to passage-based parsing via <see cref="TryParse"/> for file IDs.
    /// Returns <c>null</c> if the URI is malformed.
    /// </summary>
    public static DeepLinkRequest? TryParseDeepLink(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var uri = raw;

        // Convert shareable HTTPS URLs to zen:// format
        if (uri.StartsWith("https://readzen.pages.dev/", StringComparison.OrdinalIgnoreCase) ||
            uri.StartsWith("http://readzen.pages.dev/", StringComparison.OrdinalIgnoreCase))
        {
            int hashIdx = uri.IndexOf("#/", StringComparison.Ordinal);
            if (hashIdx >= 0)
                uri = Scheme + "://" + uri[(hashIdx + 2)..];
        }

        // Must start with zen://
        var schemePrefix = Scheme + "://";
        var prefixIdx = uri.IndexOf(schemePrefix, StringComparison.OrdinalIgnoreCase);
        if (prefixIdx < 0)
            return null;

        var afterScheme = uri[(prefixIdx + schemePrefix.Length)..];

        // Split path from query
        var qIdx = afterScheme.IndexOf('?');
        var pathPart = qIdx >= 0 ? afterScheme[..qIdx] : afterScheme;
        var queryPart = qIdx >= 0 ? afterScheme[(qIdx + 1)..] : "";
        var query = ParseQueryString("?" + queryPart);

        var segments = pathPart.Split('/');
        if (segments.Length == 0 || string.IsNullOrEmpty(segments[0]))
            return null;

        switch (segments[0].ToLowerInvariant())
        {
            case "dict":
                if (segments.Length < 2) return null;
                return new DeepLinkRequest
                {
                    Kind = DeepLinkKind.Dictionary,
                    DictTerm = Uri.UnescapeDataString(segments[1]),
                };

            case "scholar":
                return new DeepLinkRequest
                {
                    Kind = DeepLinkKind.Scholar,
                    ScholarCollectionId = segments.Length >= 2 ? Uri.UnescapeDataString(segments[1]) : null,
                    ScholarPassageId = segments.Length >= 3 ? Uri.UnescapeDataString(segments[2]) : null,
                    ScholarUser = segments.Length >= 4 ? Uri.UnescapeDataString(segments[3]) : null,
                };

            case "search":
                return new DeepLinkRequest
                {
                    Kind = DeepLinkKind.Search,
                    SearchQuery = query.TryGetValue("q", out var sq) ? sq : null,
                    SearchCorpus = query.TryGetValue("corpus", out var corpus) ? corpus : null,
                    SearchOriginal = TryParseBooleanQuery(query, "orig"),
                    SearchTranslated = TryParseBooleanQuery(query, "tran"),
                    SearchZenOnly = TryParseBooleanQuery(query, "zen"),
                    SearchStatusIndex = TryParseIntegerQuery(query, "status", "statusIndex"),
                    SearchTagId = query.TryGetValue("tag", out var tag)
                        ? tag
                        : (query.TryGetValue("tagId", out var legacyTag) ? legacyTag : null),
                    SearchContextIndex = ParseSearchContextIndex(query),
                    SearchTranslationSource = query.TryGetValue("src", out var src)
                        ? src
                        : (query.TryGetValue("translationSource", out var legacySrc) ? legacySrc : null),
                };

            case "tags":
                if (segments.Length < 2) return null;
                // Path shape: zen://tags/{fileId}/{user?}/{tagId?}
                // Use an empty user segment to carry tag-only links: zen://tags/{fileId}//{tagId}
                string? tagsUser = null;
                string? tagsTagId = null;
                if (segments.Length >= 3 && !string.IsNullOrEmpty(segments[2]))
                    tagsUser = Uri.UnescapeDataString(segments[2]);
                if (segments.Length >= 4 && !string.IsNullOrEmpty(segments[3]))
                    tagsTagId = Uri.UnescapeDataString(segments[3]);
                tagsUser ??= query.TryGetValue("user", out var tu) ? tu : null;
                tagsTagId ??= query.TryGetValue("tagId", out var tid) ? tid : null;
                return new DeepLinkRequest
                {
                    Kind = DeepLinkKind.Tags,
                    TagsRelPath = FileIdToRelPath(segments[1]),
                    TagsUser = tagsUser,
                    TagsTagId = tagsTagId,
                };

            case "term":
                if (segments.Length < 2) return null;
                // Termbase merged with dictionary ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€žÂ¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Â¦Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â both route to Dictionary kind
                return new DeepLinkRequest
                {
                    Kind = DeepLinkKind.Termbase,
                    TermbaseEntry = Uri.UnescapeDataString(segments[1]),
                    TermbaseUser = segments.Length >= 3 ? Uri.UnescapeDataString(segments[2]) : null,
                };

            case "master":
                if (segments.Length < 2) return null;
                return new DeepLinkRequest
                {
                    Kind = DeepLinkKind.Master,
                    MasterName = Uri.UnescapeDataString(segments[1]),
                    MasterUser = segments.Length >= 3 ? Uri.UnescapeDataString(segments[2]) : null,
                };

            case "compare":
                if (segments.Length < 5) return null;
                var compareRelPath = FileIdToRelPath(segments[1]);
                var comparePane = TryParsePaneSegment(segments[2]);
                if (compareRelPath == null || comparePane == null) return null;

                var compareNavigation = new NavigationRequest
                {
                    RelPath = compareRelPath,
                    Side = comparePane == ComparePaneTarget.Original ? SearchSide.Original : SearchSide.Translated,
                };

                if (query.TryGetValue("from", out var compareFrom) && !string.IsNullOrWhiteSpace(compareFrom))
                    compareNavigation.FromLb = compareFrom;
                if (query.TryGetValue("to", out var compareTo) && !string.IsNullOrWhiteSpace(compareTo))
                    compareNavigation.ToLb = compareTo;
                if (query.TryGetValue("highlight", out var compareHighlight) && !string.IsNullOrWhiteSpace(compareHighlight))
                    compareNavigation.MatchText = compareHighlight;

                return new DeepLinkRequest
                {
                    Kind = DeepLinkKind.Compare,
                    CompareRelPath = compareRelPath,
                    ComparePane = comparePane,
                    CompareSourceA = Uri.UnescapeDataString(segments[3]),
                    CompareSourceB = Uri.UnescapeDataString(segments[4]),
                    CompareNavigation = compareNavigation,
                };
        }

        // No keyword match ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â ÃƒÂ¢Ã¢â€šÂ¬Ã¢â€žÂ¢ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Â¦Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â fall through to passage parsing
        var nav = TryParse(raw);
        return nav != null ? new DeepLinkRequest { Kind = DeepLinkKind.Passage, Passage = nav } : null;
    }

    // ---------------------------------------------------------------
    //  zen:// URI builders for new deep-link kinds
    // ---------------------------------------------------------------

    public static string BuildDictUri(string term)
        => $"{Scheme}://dict/{Uri.EscapeDataString(term)}";

    public static string BuildScholarUri(string collectionId, string? passageId = null, string? user = null)
    {
        var uri = passageId != null
            ? $"{Scheme}://scholar/{collectionId}/{passageId}"
            : $"{Scheme}://scholar/{collectionId}";
        if (!string.IsNullOrEmpty(user))
            uri += "/" + Uri.EscapeDataString(user);
        return uri;
    }

    public static string BuildSearchUri(
        string query,
        string? corpus = null,
        bool? searchOriginal = null,
        bool? searchTranslated = null,
        bool? zenOnly = null,
        int? statusIndex = null,
        string? tagId = null,
        int? contextIndex = null,
        string? translationSource = null)
    {
        var queryParts = new List<string>
        {
            "q=" + Uri.EscapeDataString(query)
        };

        if (!string.IsNullOrEmpty(corpus))
            queryParts.Add("corpus=" + Uri.EscapeDataString(corpus));
        if (searchOriginal.HasValue)
            queryParts.Add("orig=" + FormatBooleanQuery(searchOriginal.Value));
        if (searchTranslated.HasValue)
            queryParts.Add("tran=" + FormatBooleanQuery(searchTranslated.Value));
        if (zenOnly.HasValue)
            queryParts.Add("zen=" + FormatBooleanQuery(zenOnly.Value));
        if (statusIndex.HasValue)
            queryParts.Add("status=" + statusIndex.Value.ToString());
        if (!string.IsNullOrEmpty(tagId))
            queryParts.Add("tag=" + Uri.EscapeDataString(tagId));
        if (contextIndex.HasValue)
            queryParts.Add("ctxw=" + SearchContextIndexToWidth(contextIndex.Value).ToString());
        if (!string.IsNullOrEmpty(translationSource))
            queryParts.Add("src=" + Uri.EscapeDataString(translationSource));

        return $"{Scheme}://search?{string.Join("&", queryParts)}";
    }

    private static int? ParseSearchContextIndex(Dictionary<string, string> query)
    {
        int? width = TryParseIntegerQuery(query, "ctxw", "contextWidth", "contextChars");
        if (width.HasValue)
            return SearchContextWidthToIndex(width.Value);

        int? index = TryParseIntegerQuery(query, "ctx", "context", "contextIndex");
        if (!index.HasValue)
            return null;

        return index.Value switch
        {
            0 => 1,
            1 => 2,
            2 => 3,
            3 => 4,
            4 => 5,
            5 => 6,
            6 => 7,
            _ => SearchContextWidthToIndex(index.Value)
        };
    }

    private static int SearchContextIndexToWidth(int index) => index switch
    {
        0 => 20,
        1 => 40,
        2 => 80,
        3 => 160,
        4 => 240,
        5 => 320,
        6 => 480,
        7 => 640,
        _ => 40
    };

    private static int SearchContextWidthToIndex(int width) => width switch
    {
        <= 20 => 0,
        <= 40 => 1,
        <= 80 => 2,
        <= 160 => 3,
        <= 240 => 4,
        <= 320 => 5,
        <= 480 => 6,
        _ => 7
    };

    public static string BuildTagsUri(string fileId, string? user = null, string? tagId = null)
    {
        var uri = $"{Scheme}://tags/{fileId}";
        if (!string.IsNullOrEmpty(user))
            uri += "/" + Uri.EscapeDataString(user);
        if (!string.IsNullOrEmpty(tagId))
        {
            if (string.IsNullOrEmpty(user))
                uri += "/";
            uri += "/" + Uri.EscapeDataString(tagId);
        }
        return uri;
    }

    public static string BuildTermUri(string term, string? user = null)
        => !string.IsNullOrEmpty(user)
            ? $"{Scheme}://term/{Uri.EscapeDataString(term)}/{Uri.EscapeDataString(user)}"
            : $"{Scheme}://term/{Uri.EscapeDataString(term)}";

    public static string BuildMasterUri(string name, string? user = null)
        => !string.IsNullOrEmpty(user)
            ? $"{Scheme}://master/{Uri.EscapeDataString(name)}/{Uri.EscapeDataString(user)}"
            : $"{Scheme}://master/{Uri.EscapeDataString(name)}";

    public static string BuildCompareUri(
        string relPath,
        ComparePaneTarget pane,
        string sourceAKey,
        string sourceBKey,
        string? fromLb = null,
        string? toLb = null,
        string? highlightText = null)
    {
        var fileId = RelPathToFileId(relPath);
        var uri = $"{Scheme}://compare/{Uri.EscapeDataString(fileId)}/{PaneToSegment(pane)}/{Uri.EscapeDataString(sourceAKey)}/{Uri.EscapeDataString(sourceBKey)}";

        var queryParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(fromLb))
            queryParts.Add("from=" + Uri.EscapeDataString(fromLb));
        if (!string.IsNullOrWhiteSpace(toLb) && !string.Equals(toLb, fromLb, StringComparison.Ordinal))
            queryParts.Add("to=" + Uri.EscapeDataString(toLb));
        if (!string.IsNullOrWhiteSpace(highlightText))
        {
            var truncated = highlightText.Length > 60 ? highlightText[..60] : highlightText;
            truncated = truncated.Replace("\n", "").Replace("\r", "");
            queryParts.Add("highlight=" + Uri.EscapeDataString(truncated));
        }

        if (queryParts.Count > 0)
            uri += "?" + string.Join("&", queryParts);

        return uri;
    }

    // ---------------------------------------------------------------
    //  Shareable HTTPS URL builders for new deep-link kinds
    // ---------------------------------------------------------------

    public static string BuildShareableDictUrl(string term)
        => $"{ShareableBase}#/dict/{Uri.EscapeDataString(term)}";

    public static string BuildShareableScholarUrl(string collectionId, string? passageId = null, string? user = null)
    {
        var url = passageId != null
            ? $"{ShareableBase}#/scholar/{collectionId}/{passageId}"
            : $"{ShareableBase}#/scholar/{collectionId}";
        if (!string.IsNullOrEmpty(user))
            url += "/" + Uri.EscapeDataString(user);
        return url;
    }

    public static string BuildShareableSearchUrl(
        string query,
        string? corpus = null,
        bool? searchOriginal = null,
        bool? searchTranslated = null,
        bool? zenOnly = null,
        int? statusIndex = null,
        string? tagId = null,
        int? contextIndex = null,
        string? translationSource = null)
    {
        var queryParts = new List<string>
        {
            "q=" + Uri.EscapeDataString(query)
        };

        if (!string.IsNullOrEmpty(corpus))
            queryParts.Add("corpus=" + Uri.EscapeDataString(corpus));
        if (searchOriginal.HasValue)
            queryParts.Add("orig=" + FormatBooleanQuery(searchOriginal.Value));
        if (searchTranslated.HasValue)
            queryParts.Add("tran=" + FormatBooleanQuery(searchTranslated.Value));
        if (zenOnly.HasValue)
            queryParts.Add("zen=" + FormatBooleanQuery(zenOnly.Value));
        if (statusIndex.HasValue)
            queryParts.Add("status=" + statusIndex.Value.ToString());
        if (!string.IsNullOrEmpty(tagId))
            queryParts.Add("tag=" + Uri.EscapeDataString(tagId));
        if (contextIndex.HasValue)
            queryParts.Add("ctxw=" + SearchContextIndexToWidth(contextIndex.Value).ToString());
        if (!string.IsNullOrEmpty(translationSource))
            queryParts.Add("src=" + Uri.EscapeDataString(translationSource));

        return $"{ShareableBase}#/search?{string.Join("&", queryParts)}";
    }

    public static string BuildShareableTagsUrl(string fileId, string? user = null, string? tagId = null)
    {
        var url = $"{ShareableBase}#/tags/{fileId}";
        if (!string.IsNullOrEmpty(user))
            url += "/" + Uri.EscapeDataString(user);
        if (!string.IsNullOrEmpty(tagId))
        {
            if (string.IsNullOrEmpty(user))
                url += "/";
            url += "/" + Uri.EscapeDataString(tagId);
        }
        return url;
    }

    public static string BuildShareableTermUrl(string term, string? user = null)
        => !string.IsNullOrEmpty(user)
            ? $"{ShareableBase}#/term/{Uri.EscapeDataString(term)}/{Uri.EscapeDataString(user)}"
            : $"{ShareableBase}#/term/{Uri.EscapeDataString(term)}";

    public static string BuildShareableMasterUrl(string name, string? user = null)
        => !string.IsNullOrEmpty(user)
            ? $"{ShareableBase}#/master/{Uri.EscapeDataString(name)}/{Uri.EscapeDataString(user)}"
            : $"{ShareableBase}#/master/{Uri.EscapeDataString(name)}";

    public static string BuildShareableCompareUrl(
        string relPath,
        ComparePaneTarget pane,
        string sourceAKey,
        string sourceBKey,
        string? fromLb = null,
        string? toLb = null,
        string? highlightText = null)
    {
        var fileId = RelPathToFileId(relPath);
        var url = $"{ShareableBase}#/compare/{Uri.EscapeDataString(fileId)}/{PaneToSegment(pane)}/{Uri.EscapeDataString(sourceAKey)}/{Uri.EscapeDataString(sourceBKey)}";

        var queryParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(fromLb))
            queryParts.Add("from=" + Uri.EscapeDataString(fromLb));
        if (!string.IsNullOrWhiteSpace(toLb) && !string.Equals(toLb, fromLb, StringComparison.Ordinal))
            queryParts.Add("to=" + Uri.EscapeDataString(toLb));
        if (!string.IsNullOrWhiteSpace(highlightText))
        {
            var truncated = highlightText.Length > 60 ? highlightText[..60] : highlightText;
            truncated = truncated.Replace("\n", "").Replace("\r", "");
            queryParts.Add("highlight=" + Uri.EscapeDataString(truncated));
        }

        if (queryParts.Count > 0)
            url += "?" + string.Join("&", queryParts);

        return url;
    }

    // ---------------------------------------------------------------

    private static bool? TryParseBooleanQuery(Dictionary<string, string> query, string key)
    {
        if (!query.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            return null;

        return raw.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "yes" or "on" => true,
            "0" or "false" or "no" or "off" => false,
            _ => null,
        };
    }

    private static int? TryParseIntegerQuery(Dictionary<string, string> query, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (query.TryGetValue(key, out var raw) && int.TryParse(raw, out var value))
                return value;
        }

        return null;
    }

    private static string FormatBooleanQuery(bool value) => value ? "1" : "0";

    /// <summary>
    /// Minimal query-string parser that avoids System.Web.HttpUtility
    /// (which may break under InvariantGlobalization).
    /// </summary>
    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(query))
            return result;

        // Strip leading '?'
        if (query[0] == '?')
            query = query.Substring(1);

        foreach (var segment in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eqIdx = segment.IndexOf('=');
            if (eqIdx < 0)
            {
                result[Uri.UnescapeDataString(segment)] = "";
            }
            else
            {
                var key = Uri.UnescapeDataString(segment.Substring(0, eqIdx));
                var val = Uri.UnescapeDataString(segment.Substring(eqIdx + 1));
                result[key] = val;
            }
        }

        return result;
    }
}




