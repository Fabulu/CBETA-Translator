// Services/ILicenseMetadataService.cs
// Per-file license cache. The IndexedTranslationService populates this during
// BuildIndex; UI surfaces (ReadableTabView, MainWindow corpus badge) read from
// it on demand. Cache entries are keyed by (absolute path, mtime ticks) so stale
// entries are naturally invalidated when a file changes on disk.
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface ILicenseMetadataService
{
    /// <summary>Try to retrieve a cached license record for the given file path.</summary>
    bool TryGet(string absPath, out TextLicenseInfo? info);

    /// <summary>Store or replace the license record for the given file path.</summary>
    void Set(string absPath, TextLicenseInfo info);

    /// <summary>Drop all cached records. Called on root change / corpus switch.</summary>
    void Clear();
}
