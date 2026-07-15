using System.Collections.Generic;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Loads and caches the rich lineage roster (609 records) from
/// <c>Assets/Data/lineage-masters.json</c>. This is the data foundation for the
/// new tidy-forest lineage chart (plan PR-L1) and is intentionally independent
/// of the thin master-dates.json roster (decision D3).
/// </summary>
public interface ILineageRosterService
{
    /// <summary>
    /// Returns all lineage master records, loading and caching them on first
    /// call. Never null; returns an empty list if the asset is missing or
    /// unparseable (the loader is fail-soft, matching the roster loaders).
    /// </summary>
    IReadOnlyList<LineageMasterRecord> GetAll();
}
