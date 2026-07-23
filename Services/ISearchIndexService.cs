using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface ISearchIndexService : IDisposable
{
    SearchIndexService.SearchIndexServiceOptions Options { get; }

    /// <summary>Corpus-wide character frequencies (populated after index build/load). Null if not yet available.</summary>
    IReadOnlyDictionary<string, int>? CorpusCharFreqs { get; }
    /// <summary>Corpus-wide bigram frequencies (populated after index build/load). Null if not yet available.</summary>
    IReadOnlyDictionary<string, int>? CorpusBigramFreqs { get; }
    /// <summary>Total CJK characters counted across the corpus. 0 if not yet available.</summary>
    long CorpusTotalChars { get; }
    /// <summary>True when corpus frequency data is loaded and usable.</summary>
    bool HasCorpusFrequencies { get; }

    string GetManifestPath(string root);
    string GetBinPath(string root);
    string GetTextManifestPath(string root);
    string GetTextBinPath(string root);

    void InvalidateIndexCaches();
    void ClearBloomCache();
    void ClearVerifyTextCache();

    Task<SearchIndexManifest?> TryLoadAsync(string root);
    Task<SearchTextManifest?> TryLoadTextManifestAsync(string root);

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
        IReadOnlyList<string>? additionalTranslatedDirs = null,
        IProgress<(int done, int total, string phase)>? progress = null,
        CancellationToken ct = default);

    Task<bool> IsStaleAsync(
        string root,
        string originalDir,
        IReadOnlyList<string> translatedDirs,
        IReadOnlyList<string>? additionalOriginalDirs = null,
        IReadOnlyList<string>? additionalTranslatedDirs = null);

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

    /// <summary>
    /// PR A (load-all-snippets): verifies skip-verify placeholder children in
    /// <paramref name="groups"/> on demand. For each group whose Children list contains any
    /// <see cref="SearchResultChild.IsSkippedVerify"/>=true entry, re-runs
    /// <c>VerifyFileAllHits</c> for the requested sides and produces a fresh list of real
    /// snippet children, replacing the placeholders. Groups with no skip-verified children
    /// are left untouched. Idempotent: a second call is a no-op (no remaining placeholders).
    ///
    /// Honors <see cref="SearchIndexService.SearchIndexServiceOptions.MaxVerifyDegreeOfParallelism"/>
    /// so the operation does not thrash disk. Reports progress via <paramref name="progress"/>
    /// as the verify phase advances (Phase = "Loading snippets...").
    ///
    /// The service is intentionally NOT responsible for marshalling the produced Children
    /// onto the UI thread — the caller (view-model) does that, applies the children cap,
    /// and preserves <see cref="SearchResultGroup"/> identity.
    /// </summary>
    /// <returns>
    /// A dictionary keyed by RelPath of every group that had at least one placeholder
    /// promoted, mapping to the new (unbounded, uncapped) list of real snippet children.
    /// Groups not present in the dictionary were left unchanged.
    /// </returns>
    Task<IReadOnlyDictionary<string, IReadOnlyList<SearchResultChild>>> LoadSnippetsForAsync(
        string root,
        string originalDir,
        string translatedDir,
        SearchIndexManifest manifest,
        IReadOnlyList<SearchResultGroup> groups,
        string query,
        int contextWidth,
        IProgress<SearchIndexService.SearchProgress>? progress = null,
        IReadOnlyList<string>? additionalOriginalDirs = null,
        IReadOnlyList<string>? additionalTranslatedDirs = null,
        CancellationToken ct = default);
}
