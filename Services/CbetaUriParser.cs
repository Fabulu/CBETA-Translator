using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

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
public static class CbetaUriParser
{
    public const string Scheme = "zen";

    /// <summary>Base URL for shareable HTTPS links (e.g. for Reddit/Discord).</summary>
    public const string ShareableBase = "https://readzen.pages.dev/";

    /// <summary>
    /// Converts a compact file ID (e.g. "T48n2005") to its relative path (e.g. "T/T48/T48n2005.xml").
    /// Returns <c>null</c> if the file ID does not contain an 'n' separator.
    /// </summary>
    public static string? FileIdToRelPath(string fileId)
    {
        var nIdx = fileId.IndexOf('n');
        if (nIdx < 1) return null;
        var volume = fileId[..nIdx];
        var canon = Regex.Replace(volume, "[0-9]", "");
        if (string.IsNullOrEmpty(canon)) return null;
        return $"{canon}/{volume}/{fileId}.xml";
    }

    /// <summary>
    /// Extracts the file ID from a relative path (e.g. "T/T48/T48n2005.xml" becomes "T48n2005").
    /// </summary>
    public static string RelPathToFileId(string relPath)
        => Path.GetFileNameWithoutExtension(relPath.Replace('\\', '/'));

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
    /// Parses the clean format: <c>zen://T48n2005/0292a26-0292a29/en?highlight=...</c>
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

        if (parts.Length >= 2)
        {
            var segment = parts[1];
            if (segment.Equals("en", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals("tran", StringComparison.OrdinalIgnoreCase))
            {
                side = SearchSide.Translated;
            }
            else
            {
                var bounds = segment.Split('-');
                fromLb = bounds[0];
                if (bounds.Length > 1) toLb = bounds[1];
            }
        }

        if (parts.Length >= 3)
        {
            var segment = parts[2];
            if (segment.Equals("en", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals("tran", StringComparison.OrdinalIgnoreCase))
            {
                side = SearchSide.Translated;
            }
        }

        var request = new NavigationRequest
        {
            RelPath = relPath,
            Side = side,
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
        int? blockNumber = null)
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

        var queryParts = new List<string>();

        if (!string.IsNullOrEmpty(highlightText))
        {
            // Truncate highlight to keep URLs short — 60 chars is enough to find the right spot
            var truncated = highlightText.Length > 60 ? highlightText[..60] : highlightText;
            // Also strip newlines — they bloat the URL and aren't needed for matching
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
        SearchSide side = SearchSide.Original)
    {
        // Extract file ID from relPath: "T/T48/T48n2005.xml" → "T48n2005"
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
