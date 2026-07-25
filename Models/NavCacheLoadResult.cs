namespace ReadZen.App.Models;

/// <summary>
/// Classification of an on-disk nav cache produced by the root-tolerant loader
/// (NAV_CACHE_REDESIGN §4.4). Replaces the old "TryLoadAsync returns null" scheme so the
/// launch ladder can branch (V5 refresh vs v4 migrate vs rebuild) without re-reading the
/// file. In PR-NV2 a <see cref="V4NeedsMigration"/> cache still routes to rebuild
/// (migration is PR-NV3).
/// </summary>
public enum NavCacheLoadStatus
{
    /// <summary>A current-format (v5) cache with a matching build guid — refresh it.</summary>
    V5,

    /// <summary>A v4 cache present — migratable (PR-NV3); rebuilt in PR-NV2.</summary>
    V4NeedsMigration,

    /// <summary>Absent, corrupt, empty, wrong version, or wrong build guid — rebuild.</summary>
    Unusable,
}

/// <summary>
/// Result of a root-tolerant nav-cache load: the classification plus the deserialized
/// cache when one was read (null for <see cref="NavCacheLoadStatus.Unusable"/>).
/// </summary>
public sealed record NavCacheLoadResult(NavCacheLoadStatus Status, IndexCache? Cache)
{
    public static readonly NavCacheLoadResult Unusable =
        new(NavCacheLoadStatus.Unusable, null);
}
