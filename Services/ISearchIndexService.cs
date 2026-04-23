using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface ISearchIndexService : IDisposable
{
    SearchIndexService.SearchIndexServiceOptions Options { get; }

    string GetManifestPath(string root);
    string GetBinPath(string root);
    string GetTextManifestPath(string root);
    string GetTextBinPath(string root);
    string GetCjk2ManifestPath(string root);

    void ClearBloomCache();
    void ClearVerifyTextCache();

    Task<SearchIndexManifest?> TryLoadAsync(string root);
    Task<SearchTextManifest?> TryLoadTextManifestAsync(string root);
    Task<SearchCjkBigramManifest?> TryLoadCjk2ManifestAsync(string root);

    Task BuildAsync(
        string root,
        string originalDir,
        IReadOnlyList<string> translatedDirs,
        IProgress<(int done, int total, string phase)>? progress = null,
        CancellationToken ct = default);

    Task BuildOrUpdateAsync(
        string root,
        string originalDir,
        IReadOnlyList<string> translatedDirs,
        bool forceRebuild,
        IReadOnlyList<string>? additionalOriginalDirs = null,
        IProgress<(int done, int total, string phase)>? progress = null,
        CancellationToken ct = default);

    Task<bool> IsStaleAsync(string root, string originalDir, IReadOnlyList<string> translatedDirs);

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
        IReadOnlyList<string>? additionalOriginalDirs = null,
        IReadOnlyList<string>? additionalTranslatedDirs = null,
        CancellationToken ct = default);
}
