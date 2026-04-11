// Tests for LicenseMetadataService — the per-file license cache used by
// the desktop reader to render the chip + flyout. The cache key is
// (absPath, mtimeTicks), so a stale entry is invalidated automatically
// when the underlying file changes on disk.

using System;
using System.IO;
using System.Threading;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

public class LicenseMetadataServiceTests
{
    [Fact]
    public void TryGet_ReturnsFalse_ForUnknownPath()
    {
        var svc = new LicenseMetadataService();
        Assert.False(svc.TryGet("C:/does/not/exist.xml", out var info));
        Assert.Null(info);
    }

    [Fact]
    public void Set_ThenTryGet_ReturnsCachedRecord()
    {
        var svc = new LicenseMetadataService();
        var path = Path.Combine(Path.GetTempPath(), $"licmeta-{Guid.NewGuid():N}.xml");
        try
        {
            File.WriteAllText(path, "<TEI/>");
            var record = new TextLicenseInfo { LicenseClass = LicenseClass.PublicDomain, ShortLabel = "PD-old" };
            svc.Set(path, record);

            Assert.True(svc.TryGet(path, out var fetched));
            Assert.NotNull(fetched);
            Assert.Equal("PD-old", fetched!.ShortLabel);
            Assert.Equal(LicenseClass.PublicDomain, fetched.LicenseClass);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void TryGet_ReturnsFalse_AfterFileMtimeChanges()
    {
        var svc = new LicenseMetadataService();
        var path = Path.Combine(Path.GetTempPath(), $"licmeta-{Guid.NewGuid():N}.xml");
        try
        {
            File.WriteAllText(path, "<TEI/>");
            svc.Set(path, new TextLicenseInfo { ShortLabel = "v1" });
            Assert.True(svc.TryGet(path, out _));

            // Bump the mtime by writing fresh bytes after a short delay
            // (NTFS LastWriteTime resolution is typically ~10ms but can be
            // worse on slow disks; the explicit SetLastWriteTimeUtc removes
            // the dependency on filesystem clock granularity entirely).
            Thread.Sleep(50);
            File.WriteAllText(path, "<TEI version='2'/>");
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow);

            Assert.False(svc.TryGet(path, out var stale));
            Assert.Null(stale);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Clear_DropsAllEntries()
    {
        var svc = new LicenseMetadataService();
        var path = Path.Combine(Path.GetTempPath(), $"licmeta-{Guid.NewGuid():N}.xml");
        try
        {
            File.WriteAllText(path, "<TEI/>");
            svc.Set(path, new TextLicenseInfo { ShortLabel = "x" });
            Assert.True(svc.TryGet(path, out _));

            svc.Clear();
            Assert.False(svc.TryGet(path, out _));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
