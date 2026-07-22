// Services/ZenTextsService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ReadZen.App.Services;

/// <summary>
/// Prescriptive Zen-corpus membership. What counts as "Zen" is fixed by the app-baked
/// allowlist Assets/Data/zen-corpus.json (derived from ZEN_TEXT_WORKLIST.md — the records
/// of Zen Masters, filtered of Pure Land / Vinaya / sutra / stele material). It is NOT
/// user-editable: <see cref="SetZenAsync"/> is intentionally a no-op. Previously this was a
/// per-repo, per-user-editable zen_texts.json; that proved pointless (nobody curated it), so
/// classification is now definitional. See RUN-20260711-1248 SPEC_v3.
/// </summary>
public sealed class ZenTextsService : IZenTextsService
{
    private const string AssetFileName = "zen-corpus.json";

    private readonly string _assetPath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private HashSet<string> _zen = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<string> _texts = System.Array.Empty<string>();
    private string? _listVersion;
    private string? _generatedNote;
    private bool _loaded;

    public ZenTextsService(string? assetPathOverride = null)
    {
        _assetPath = string.IsNullOrWhiteSpace(assetPathOverride)
            ? Path.Combine(AppContext.BaseDirectory, "Assets", "Data", AssetFileName)
            : assetPathOverride!;
    }

    /// <summary>
    /// Loads the prescriptive allowlist. The <paramref name="root"/> is accepted for
    /// interface compatibility but ignored — membership is app-global, not per-repo.
    /// </summary>
    public async Task LoadAsync(string root)
    {
        await _gate.WaitAsync();
        try
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ordered = new List<string>();
            string? listVersion = null;
            string? generatedNote = null;

            if (File.Exists(_assetPath))
            {
                var json = await File.ReadAllTextAsync(_assetPath, Encoding.UTF8);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var data = JsonSerializer.Deserialize<ZenCorpusFile>(
                        json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (data != null)
                    {
                        listVersion = data.ListVersion;
                        generatedNote = data.GeneratedNote;
                        if (data.Texts != null)
                            foreach (var rel in data.Texts)
                                if (!string.IsNullOrWhiteSpace(rel))
                                {
                                    var n = Norm(rel);
                                    if (set.Add(n)) ordered.Add(n); // dedupe, preserve asset order
                                }
                    }
                }
            }

            _zen = set;
            _texts = ordered;
            _listVersion = listVersion;
            _generatedNote = generatedNote;
            _loaded = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public bool IsZen(string relPath)
        => _loaded && _zen.Contains(Norm(relPath));

    /// <inheritdoc/>
    public IReadOnlyList<string> Texts => _texts;

    /// <inheritdoc/>
    public string? ListVersion => _listVersion;

    /// <inheritdoc/>
    public string? GeneratedNote => _generatedNote;

    /// <summary>
    /// No-op. Zen classification is prescriptive (Assets/Data/zen-corpus.json) and cannot be
    /// changed by users. Kept for interface compatibility with existing call sites.
    /// </summary>
    public Task SetZenAsync(string root, string relPath, bool isZen) => Task.CompletedTask;

    private static string Norm(string p)
        => (p ?? "").Replace('\\', '/').TrimStart('/');

    private sealed class ZenCorpusFile
    {
        public int Version { get; set; } = 1;
        // Human-readable version label surfaced by the Canon Inspector (optional in the asset).
        public string? ListVersion { get; set; }
        public string? Source { get; set; }
        public string? GeneratedNote { get; set; }
        public List<string> Texts { get; set; } = new();
    }
}
