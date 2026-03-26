using System;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

/// <summary>
/// Responsible for building, loading, and managing search index files.
/// Delegates to the SearchIndexService facade which owns the shared state
/// (caches, memory-mapped files, semaphores).
/// Extracted from SearchIndexService during Wave 7 service split.
/// </summary>
public sealed class SearchIndexBuilder : ISearchIndexBuilder
{
    private readonly ISearchIndexService _facade;

    public SearchIndexBuilder(ISearchIndexService facade)
    {
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));
    }

    public string GetManifestPath(string root) => _facade.GetManifestPath(root);
    public string GetBinPath(string root) => _facade.GetBinPath(root);
    public string GetTextManifestPath(string root) => _facade.GetTextManifestPath(root);
    public string GetTextBinPath(string root) => _facade.GetTextBinPath(root);
    public string GetCjk2ManifestPath(string root) => _facade.GetCjk2ManifestPath(root);

    public Task<SearchIndexManifest?> TryLoadAsync(string root)
        => _facade.TryLoadAsync(root);

    public Task<SearchTextManifest?> TryLoadTextManifestAsync(string root)
        => _facade.TryLoadTextManifestAsync(root);

    public Task<SearchCjkBigramManifest?> TryLoadCjk2ManifestAsync(string root)
        => _facade.TryLoadCjk2ManifestAsync(root);

    public Task BuildAsync(
        string root,
        string originalDir,
        string translatedDir,
        IProgress<(int done, int total, string phase)>? progress = null,
        CancellationToken ct = default)
        => _facade.BuildAsync(root, originalDir, translatedDir, progress, ct);

    public Task BuildOrUpdateAsync(
        string root,
        string originalDir,
        string translatedDir,
        bool forceRebuild,
        IProgress<(int done, int total, string phase)>? progress = null,
        CancellationToken ct = default)
        => _facade.BuildOrUpdateAsync(root, originalDir, translatedDir, forceRebuild, progress, ct);
}
