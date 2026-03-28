using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

/// <summary>
/// Finds parallel passages in the CBETA corpus by extracting distinctive
/// substrings from the source ZH text and searching the index for matches.
/// Results are ranked by the number of shared n-gram hits.
/// </summary>
public sealed class ParallelPassageFinderService : IParallelPassageFinderService
{
    private readonly ISearchIndexService _searchIndex;

    public ParallelPassageFinderService(ISearchIndexService searchIndex)
    {
        _searchIndex = searchIndex ?? throw new ArgumentNullException(nameof(searchIndex));
    }

    public async Task<List<ParallelPassageResult>> FindParallelsAsync(
        string zhText,
        string root,
        string originalDir,
        string translatedDir,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(zhText) || string.IsNullOrWhiteSpace(root))
            return new List<ParallelPassageResult>();

        // Load the search index manifest
        var manifest = await _searchIndex.TryLoadAsync(root);
        if (manifest == null)
            return new List<ParallelPassageResult>();

        // Extract distinctive 4-char substrings from the source text
        var queries = ExtractDistinctiveNgrams(zhText, gramLen: 4, maxQueries: 5);
        if (queries.Count == 0)
            return new List<ParallelPassageResult>();

        // Collect file hit counts across all queries
        var fileHits = new Dictionary<string, (int hitCount, string snippet)>(StringComparer.Ordinal);

        foreach (var query in queries)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var results = _searchIndex.SearchAllAsync(
                    root, originalDir, translatedDir, manifest,
                    query,
                    includeOriginal: true,
                    includeTranslated: false,
                    fileMeta: _ => ("", "", null),
                    contextWidth: 30,
                    ct: ct);

                await foreach (var group in results.WithCancellation(ct))
                {
                    if (!fileHits.ContainsKey(group.RelPath))
                    {
                        // Use the first child's KWIC as the snippet
                        var firstChild = group.Children.FirstOrDefault();
                        var snippet = firstChild != null
                            ? $"{firstChild.LeftText}{firstChild.MatchText}{firstChild.RightText}"
                            : "";
                        if (snippet.Length > 60) snippet = snippet[..60] + "...";
                        fileHits[group.RelPath] = (1, snippet);
                    }
                    else
                    {
                        var existing = fileHits[group.RelPath];
                        fileHits[group.RelPath] = (existing.hitCount + 1, existing.snippet);
                    }
                }
            }
            catch
            {
                // Individual query failures should not abort the whole search
            }
        }

        // Score: fraction of queries that hit this file
        var totalQueries = queries.Count;
        var results2 = fileHits
            .Select(kv => new ParallelPassageResult
            {
                RelPath = kv.Key,
                Snippet = kv.Value.snippet,
                OverlapScore = Math.Round((double)kv.Value.hitCount / totalQueries * 100, 1)
            })
            .OrderByDescending(r => r.OverlapScore)
            .Take(20)
            .ToList();

        return results2;
    }

    /// <summary>
    /// Extracts distinctive n-grams from the text by picking evenly spaced
    /// substrings after stripping whitespace and common punctuation.
    /// </summary>
    private static List<string> ExtractDistinctiveNgrams(string text, int gramLen, int maxQueries)
    {
        // Strip whitespace and common CJK punctuation
        var clean = new string(text.Where(c =>
            !char.IsWhiteSpace(c) &&
            c != '\u3002' && c != '\uff0c' && c != '\u3001' &&
            c != '\uff1b' && c != '\uff1a' && c != '\uff01' &&
            c != '\uff1f' && c != '\u300a' && c != '\u300b' &&
            c != '\u201c' && c != '\u201d' && c != '\u2018' &&
            c != '\u2019' && c != '\u3008' && c != '\u3009' &&
            c != '\u300c' && c != '\u300d' && c != '\u300e' &&
            c != '\u300f').ToArray());

        if (clean.Length < gramLen)
            return new List<string>();

        var grams = new List<string>();
        int totalPossible = clean.Length - gramLen + 1;

        if (totalPossible <= maxQueries)
        {
            // Text is short: take all unique grams
            for (int i = 0; i < totalPossible; i++)
                grams.Add(clean.Substring(i, gramLen));
        }
        else
        {
            // Pick evenly spaced grams
            double step = (double)totalPossible / maxQueries;
            for (int q = 0; q < maxQueries; q++)
            {
                int idx = (int)(q * step);
                if (idx + gramLen <= clean.Length)
                    grams.Add(clean.Substring(idx, gramLen));
            }
        }

        return grams.Distinct(StringComparer.Ordinal).ToList();
    }
}
