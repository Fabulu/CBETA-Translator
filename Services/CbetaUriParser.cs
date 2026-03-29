using System;
using System.Collections.Generic;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

/// <summary>
/// Converts between <c>cbeta://</c> deep-link URIs and <see cref="NavigationRequest"/> objects.
/// URI format: <c>cbeta://T/T48/T48n2005.xml?highlight=...&amp;side=...&amp;lctx=...&amp;rctx=...&amp;block=...</c>
/// </summary>
public static class CbetaUriParser
{
    public const string Scheme = "cbeta";

    /// <summary>
    /// Attempts to parse a <c>cbeta://</c> URI into a <see cref="NavigationRequest"/>.
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

        // Extract path directly from the original string to preserve case.
        // System.Uri lowercases the Host component, breaking CBETA paths like "T/T48/..."
        var schemePrefix = Scheme + "://";
        var pathStart = uri.IndexOf(schemePrefix, StringComparison.OrdinalIgnoreCase);
        if (pathStart < 0) return null;
        pathStart += schemePrefix.Length;
        var queryStart = uri.IndexOf('?', pathStart);
        var relPath = queryStart >= 0
            ? uri.Substring(pathStart, queryStart - pathStart)
            : uri.Substring(pathStart);
        relPath = Uri.UnescapeDataString(relPath).TrimStart('/');
        if (string.IsNullOrEmpty(relPath))
            return null;

        var query = ParseQueryString(parsed.Query);

        var request = new NavigationRequest
        {
            RelPath = relPath,
        };

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
    /// Builds a <c>cbeta://</c> URI from the given parameters.
    /// All values are URI-encoded with <see cref="Uri.EscapeDataString"/>.
    /// </summary>
    public static string BuildUri(
        string relPath,
        string? highlightText = null,
        SearchSide side = SearchSide.Original,
        string? leftContext = null,
        string? rightContext = null,
        int? blockNumber = null)
    {
        // Normalize path separators to forward slashes
        relPath = relPath.Replace('\\', '/');

        // cbeta://T/T48/T48n2005.xml
        var baseUri = Scheme + "://" + relPath;

        var queryParts = new List<string>();

        if (!string.IsNullOrEmpty(highlightText))
        {
            // Truncate highlight to keep URLs short — 60 chars is enough to find the right spot
            var truncated = highlightText.Length > 60 ? highlightText[..60] : highlightText;
            // Also strip newlines — they bloat the URL and aren't needed for matching
            truncated = truncated.Replace("\n", "").Replace("\r", "");
            queryParts.Add("highlight=" + Uri.EscapeDataString(truncated));
        }

        if (side != SearchSide.Original)
            queryParts.Add("side=" + Uri.EscapeDataString(side.ToString()));

        if (!string.IsNullOrEmpty(leftContext))
            queryParts.Add("lctx=" + Uri.EscapeDataString(leftContext));

        if (!string.IsNullOrEmpty(rightContext))
            queryParts.Add("rctx=" + Uri.EscapeDataString(rightContext));

        if (blockNumber.HasValue)
            queryParts.Add("block=" + blockNumber.Value.ToString());

        if (queryParts.Count > 0)
            baseUri += "?" + string.Join("&", queryParts);

        return baseUri;
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
