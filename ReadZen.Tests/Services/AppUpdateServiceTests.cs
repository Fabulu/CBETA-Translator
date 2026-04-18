using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Tests for AppUpdateService's graceful-fallback behavior. Actual Velopack
/// install-time semantics can only be exercised by an integration test on a
/// packaged build — the unit tests here just verify that the service stays
/// safe + callable in the non-Velopack "zip extract" mode that test processes
/// run in.
/// </summary>
public class AppUpdateServiceTests
{
    [Fact]
    public void Constructor_DoesNotThrow_InZipMode()
    {
        // Test processes are never Velopack-installed, so the manager should
        // construct but report IsInstalled=false internally.
        var svc = new AppUpdateService();
        Assert.NotNull(svc);
    }

    [Fact]
    public void IsVelopackInstall_ReturnsFalse_InTestRunner()
    {
        // Test runners (xunit host + dotnet test) are never Velopack-installed
        // — there's no Velopack metadata in the runner's install path.
        var svc = new AppUpdateService();
        Assert.False(svc.IsVelopackInstall);
    }

    [Fact]
    public async Task TryInstallAndRestart_Fails_Gracefully_WhenNoVelopackUpdate()
    {
        // Passing a check result that has no VelopackUpdate attached must
        // return false rather than throwing, so the caller can fall back to
        // the browser-open path cleanly.
        var svc = new AppUpdateService();
        var dummyResult = new AppUpdateCheckResult
        {
            AvailableVersion = "99.0.0",
            ReleaseUrl = "https://example.com/releases",
            CanInstallInApp = false,
            VelopackUpdate = null
        };

        var ok = await svc.TryInstallAndRestartAsync(dummyResult, CancellationToken.None);

        Assert.False(ok);
    }

    [Fact]
    public async Task CheckForUpdates_NeverThrows_OnNetworkFailure()
    {
        // The service must absorb all network / API / Velopack failures
        // internally. This test can't force a network outage reliably, but
        // it exercises the happy-or-error path and verifies nothing escapes.
        var svc = new AppUpdateService();

        // 2-second timeout — we don't want a hung network call to blow up the
        // test suite.
        using var cts = new CancellationTokenSource(2000);
        var result = await svc.CheckForUpdatesAsync(cts.Token);

        // Either a real result, a null AvailableVersion (no update), or the
        // fallback ReleaseUrl path — all valid, none should throw.
        Assert.NotNull(result);
        Assert.NotNull(result.ReleaseUrl);
    }

    [Fact]
    public void AppUpdateCheckResult_DefaultReleaseUrl_PointsAtGitHubReleases()
    {
        // Guards the default fallback so even a completely-failed check
        // still hands the user something useful to click.
        var r = new AppUpdateCheckResult();
        Assert.Contains("Fabulu/ReadZen/releases", r.ReleaseUrl);
    }

    [Fact]
    public void AppUpdateCheckResult_CanInstallInApp_DefaultsToFalse()
    {
        var r = new AppUpdateCheckResult();
        Assert.False(r.CanInstallInApp);
    }
}
