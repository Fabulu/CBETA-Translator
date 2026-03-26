using System;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

/// <summary>
/// Responsible for building, loading, and managing search index files
/// (bloom filter binaries, manifests, text sidecars, CJK bigram postings).
/// </summary>
public interface ISearchIndexBuilder
{
    string GetManifestPath(string root);
    string GetBinPath(string root);
    string GetTextManifestPath(string root);
    string GetTextBinPath(string root);
    string GetCjk2ManifestPath(string root);

    Task<SearchIndexManifest?> TryLoadAsync(string root);
    Task<SearchTextManifest?> TryLoadTextManifestAsync(string root);
    Task<SearchCjkBigramManifest?> TryLoadCjk2ManifestAsync(string root);

    Task BuildAsync(
        string root,
        string originalDir,
        string translatedDir,
        IProgress<(int done, int total, string phase)>? progress = null,
        CancellationToken ct = default);

    Task BuildOrUpdateAsync(
        string root,
        string originalDir,
        string translatedDir,
        bool forceRebuild,
        IProgress<(int done, int total, string phase)>? progress = null,
        CancellationToken ct = default);
}
