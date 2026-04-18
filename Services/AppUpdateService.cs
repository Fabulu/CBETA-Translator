using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace ReadZen.App.Services;

/// <summary>
/// Outcome of an update check, decoupled from Velopack types so callers don't
/// have to reference the library directly.
/// </summary>
public sealed class AppUpdateCheckResult
{
    /// <summary>
    /// The version string (without the 'v' prefix) of the update that's available,
    /// e.g. "4.5.0". Null when no update is available.
    /// </summary>
    public string? AvailableVersion { get; init; }

    /// <summary>
    /// The web URL for this release (used as a fallback when in-app install
    /// isn't supported by the current install mode — e.g. zip extract).
    /// </summary>
    public string ReleaseUrl { get; init; } = "https://github.com/Fabulu/ReadZen/releases";

    /// <summary>
    /// True when <see cref="AppUpdateService.TryInstallAndRestartAsync"/> can
    /// actually apply this update in-place. False means the user must manually
    /// download the new version from <see cref="ReleaseUrl"/>.
    /// </summary>
    public bool CanInstallInApp { get; init; }

    /// <summary>
    /// Velopack-native update info, when available, held so we don't have to
    /// re-query before applying. Null on zip installs or when the underlying
    /// check failed and we fell back to the GitHub API.
    /// </summary>
    internal UpdateInfo? VelopackUpdate { get; init; }
}

/// <summary>
/// Wraps Velopack's UpdateManager with a graceful fallback for zip-extract
/// installs. On packaged builds (produced by <c>vpk pack</c>) this performs a
/// real in-place update; on plain zip extracts it reports the update version
/// via a simple GitHub API check and leaves the download to the user.
/// </summary>
public sealed class AppUpdateService
{
    private const string GithubRepoUrl = "https://github.com/Fabulu/ReadZen";
    private const string GithubApiLatest = "https://api.github.com/repos/Fabulu/ReadZen/releases/latest";
    private const string ReleasesPageUrl = "https://github.com/Fabulu/ReadZen/releases";

    private readonly UpdateManager? _manager;

    public AppUpdateService()
    {
        // GithubSource uses the public releases API and requires no token for
        // public repos. Construction is cheap; actual network hits happen in
        // CheckForUpdatesAsync.
        try
        {
            var source = new GithubSource(GithubRepoUrl, accessToken: null, prerelease: false);
            _manager = new UpdateManager(source);
        }
        catch
        {
            // If Velopack can't initialize (e.g. missing metadata), we fall
            // through to the HTTP-only check path.
            _manager = null;
        }
    }

    /// <summary>
    /// True when this binary was installed via Velopack (Setup.exe / AppImage /
    /// .pkg) and in-app updates are supported. False on zip-extract installs.
    /// </summary>
    public bool IsVelopackInstall => _manager?.IsInstalled ?? false;

    /// <summary>
    /// Checks for an available update. Never throws; always returns a result
    /// describing either an update, no-update, or a fallback URL.
    /// </summary>
    public async Task<AppUpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        // Fast path: Velopack-installed build → ask Velopack.
        if (IsVelopackInstall && _manager != null)
        {
            try
            {
                var info = await _manager.CheckForUpdatesAsync().ConfigureAwait(false);
                if (info == null)
                    return new AppUpdateCheckResult { ReleaseUrl = ReleasesPageUrl };

                return new AppUpdateCheckResult
                {
                    AvailableVersion = info.TargetFullRelease.Version.ToString(),
                    ReleaseUrl = ReleasesPageUrl,
                    CanInstallInApp = true,
                    VelopackUpdate = info
                };
            }
            catch
            {
                // Fall through to HTTP-only check — better to tell the user
                // "update available" via the browser than to silently do nothing.
            }
        }

        return await CheckGithubApiAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads + applies the update passed in, then restarts the app. Only
    /// call with a result that reports <see cref="AppUpdateCheckResult.CanInstallInApp"/>.
    /// Returns false when Velopack failed and the caller should fall back to
    /// opening the release URL in the browser (Avalonia issue #146 mitigation).
    /// </summary>
    public async Task<bool> TryInstallAndRestartAsync(AppUpdateCheckResult check, CancellationToken ct = default)
    {
        if (_manager == null || check.VelopackUpdate == null) return false;

        try
        {
            await _manager.DownloadUpdatesAsync(check.VelopackUpdate).ConfigureAwait(false);
            _manager.ApplyUpdatesAndRestart(check.VelopackUpdate);
            // ApplyUpdatesAndRestart exits the process on success; reaching here
            // means Velopack chose not to restart for some reason.
            return true;
        }
        catch
        {
            // Stall / download failure / whatever — let the caller open the
            // release page in a browser instead.
            return false;
        }
    }

    /// <summary>
    /// Fallback check that just hits the GitHub Releases API. Used when Velopack
    /// isn't applicable (zip install) or its own check failed. Mirrors the
    /// historical behavior that App.axaml.cs had before Phase 5.
    /// </summary>
    private async Task<AppUpdateCheckResult> CheckGithubApiAsync(CancellationToken ct)
    {
        try
        {
            using var http = new System.Net.Http.HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("ReadZen-UpdateCheck/2.0");
            var json = await http.GetStringAsync(GithubApiLatest, ct).ConfigureAwait(false);

            var tagMatch = System.Text.RegularExpressions.Regex.Match(json, "\"tag_name\"\\s*:\\s*\"v?([^\"]+)\"");
            var urlMatch = System.Text.RegularExpressions.Regex.Match(json, "\"html_url\"\\s*:\\s*\"([^\"]+)\"");
            if (!tagMatch.Success) return new AppUpdateCheckResult { ReleaseUrl = ReleasesPageUrl };

            var latest = tagMatch.Groups[1].Value;
            var url = urlMatch.Success ? urlMatch.Groups[1].Value : ReleasesPageUrl;

            var currentVersion = CurrentAssemblyVersion();
            if (string.Compare(latest, currentVersion, StringComparison.OrdinalIgnoreCase) <= 0)
                return new AppUpdateCheckResult { ReleaseUrl = url };

            return new AppUpdateCheckResult
            {
                AvailableVersion = latest,
                ReleaseUrl = url,
                CanInstallInApp = false
            };
        }
        catch
        {
            return new AppUpdateCheckResult { ReleaseUrl = ReleasesPageUrl };
        }
    }

    private static string CurrentAssemblyVersion()
    {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var raw = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                  ?? "0.0.0";
        // Strip +metadata and -prerelease so string comparison is clean against the tag name.
        return raw.Split('+')[0].Split('-')[0];
    }
}
