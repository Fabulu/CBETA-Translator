using System;
using System.Collections.Generic;
using System.Threading;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Responsible for executing search queries against the search index,
/// including bloom prefiltering, verification, scoring, and KWIC extraction.
/// </summary>
public interface ISearchEngine
{
    void ClearBloomCache();
    void ClearVerifyTextCache();

    IAsyncEnumerable<SearchResultGroup> SearchAllAsync(
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
        CancellationToken ct = default);
}
