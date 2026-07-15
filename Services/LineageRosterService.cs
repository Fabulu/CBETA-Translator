using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Lazy-loading, cached reader for the rich lineage roster
/// (<c>Assets/Data/lineage-masters.json</c>, a flat array of 609 records).
/// Resolves the asset via <see cref="AppPaths.LineageMastersPath"/> (portable
/// install layout — never %APPDATA%). Fail-soft: a missing or malformed asset
/// yields an empty list rather than throwing, mirroring the other roster
/// loaders (<see cref="MasterDatesService.LoadBaseNameSet"/>). Backs the new
/// lineage chart only; leaves master-dates.json untouched (decision D3).
/// </summary>
public sealed class LineageRosterService : ILineageRosterService
{
    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly object _gate = new();
    private IReadOnlyList<LineageMasterRecord>? _cache;

    public IReadOnlyList<LineageMasterRecord> GetAll()
    {
        var cached = _cache;
        if (cached != null) return cached;

        lock (_gate)
        {
            _cache ??= Load();
            return _cache;
        }
    }

    private static IReadOnlyList<LineageMasterRecord> Load()
    {
        try
        {
            var path = AppPaths.LineageMastersPath;
            if (!File.Exists(path))
                return Array.Empty<LineageMasterRecord>();

            using var stream = File.OpenRead(path);
            var records = JsonSerializer.Deserialize<List<LineageMasterRecord>>(stream, ReadOpts);
            return records ?? (IReadOnlyList<LineageMasterRecord>)Array.Empty<LineageMasterRecord>();
        }
        catch
        {
            // Fail-soft: a corrupt/absent asset must not take down the app.
            return Array.Empty<LineageMasterRecord>();
        }
    }
}
