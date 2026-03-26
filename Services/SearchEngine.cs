using System;
using System.Collections.Generic;
using System.Threading;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

/// <summary>
/// Responsible for executing search queries against the search index.
/// Delegates to the SearchIndexService facade which owns the shared state
/// (bloom cache, verify text cache, memory-mapped files).
/// Extracted from SearchIndexService during Wave 7 service split.
/// </summary>
public sealed class SearchEngine : ISearchEngine
{
    private readonly ISearchIndexService _facade;

    public SearchEngine(ISearchIndexService facade)
    {
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));
    }

    public void ClearBloomCache() => _facade.ClearBloomCache();
    public void ClearVerifyTextCache() => _facade.ClearVerifyTextCache();

    public IAsyncEnumerable<SearchResultGroup> SearchAllAsync(
        string root,
        string originalDir,
        string translatedDir,
        SearchIndexManifest manifest,
        string query,
        bool includeOriginal,
        bool includeTranslated,
        Func<string, (string display, string tooltip, TranslationStatus? status)> fileMeta,
        int contextWidth,
        IProgress<SearchIndexService.SearchProgress>? progress = null,
        Func<string, bool>? relPathFilter = null,
        CancellationToken ct = default)
    {
        return _facade.SearchAllAsync(
            root, originalDir, translatedDir, manifest, query,
            includeOriginal, includeTranslated, fileMeta, contextWidth,
            progress, relPathFilter, ct);
    }
}
