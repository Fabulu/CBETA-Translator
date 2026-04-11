// Services/LicenseMetadataService.cs
// ConcurrentDictionary-backed implementation of ILicenseMetadataService.
// Cache key is (absPath, mtimeTicks) — if the file changes on disk the old
// entry is ignored and the extractor re-runs on the next BuildIndex.
using System.Collections.Concurrent;
using System.IO;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class LicenseMetadataService : ILicenseMetadataService
{
    private readonly ConcurrentDictionary<string, (long mtimeTicks, TextLicenseInfo info)> _cache
        = new(System.StringComparer.OrdinalIgnoreCase);

    public bool TryGet(string absPath, out TextLicenseInfo? info)
    {
        info = null;
        if (string.IsNullOrWhiteSpace(absPath)) return false;

        if (!_cache.TryGetValue(absPath, out var entry)) return false;

        long currentTicks = GetMtimeTicks(absPath);
        if (currentTicks != entry.mtimeTicks) return false;

        info = entry.info;
        return true;
    }

    public void Set(string absPath, TextLicenseInfo info)
    {
        if (string.IsNullOrWhiteSpace(absPath) || info == null) return;
        long ticks = GetMtimeTicks(absPath);
        _cache[absPath] = (ticks, info);
    }

    public void Clear() => _cache.Clear();

    private static long GetMtimeTicks(string absPath)
    {
        try { return new FileInfo(absPath).LastWriteTimeUtc.Ticks; }
        catch { return 0L; }
    }
}
