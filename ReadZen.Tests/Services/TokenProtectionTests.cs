using System;
using System.IO;
using System.Threading.Tasks;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Regression tests for audit item P1.5 (RUN-20260702-2259 R3-H3): the GitHub OAuth
/// token was persisted in plaintext in config.json next to the exe. It is now
/// DPAPI-protected at rest on Windows (portable exe-dir layout kept per decision D8);
/// the in-memory config always carries the plaintext; legacy plaintext configs are
/// migrated on first load.
/// </summary>
public sealed class TokenProtectionTests : IDisposable
{
    private readonly string _dir;
    private string ConfigPath => Path.Combine(_dir, "config.json");

    public TokenProtectionTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "readzen-token-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    [Fact]
    public void Protect_RoundTrips_AndMarksValue()
    {
        if (!OperatingSystem.IsWindows()) return;

        var protectedValue = TokenProtector.Protect("ghp_secret123");

        Assert.StartsWith("dpapi:v1:", protectedValue);
        Assert.DoesNotContain("ghp_secret123", protectedValue);
        Assert.Equal("ghp_secret123", TokenProtector.TryUnprotect(protectedValue));
    }

    [Fact]
    public void TryUnprotect_LegacyPlaintext_PassesThrough()
    {
        Assert.Equal("ghp_legacy", TokenProtector.TryUnprotect("ghp_legacy"));
    }

    [Fact]
    public void TryUnprotect_CorruptBlob_ReturnsNull_NotThrow()
    {
        if (!OperatingSystem.IsWindows()) return;
        Assert.Null(TokenProtector.TryUnprotect("dpapi:v1:not-base64!!"));
        Assert.Null(TokenProtector.TryUnprotect("dpapi:v1:" + Convert.ToBase64String(new byte[] { 1, 2, 3 })));
    }

    [Fact]
    public async Task SaveAsync_NeverWritesPlaintextTokenToDisk()
    {
        if (!OperatingSystem.IsWindows()) return;

        var svc = new AppConfigService(ConfigPath);
        var cfg = new AppConfig { GitHubAccessToken = "ghp_plain_token_456", GitHubUsername = "someone" };

        await svc.SaveAsync(cfg);

        var onDisk = await File.ReadAllTextAsync(ConfigPath);
        // Before the fix the raw token sat in config.json next to the exe.
        Assert.DoesNotContain("ghp_plain_token_456", onDisk);
        Assert.Contains("dpapi:v1:", onDisk);
        // The caller's live config must keep the plaintext it works with.
        Assert.Equal("ghp_plain_token_456", cfg.GitHubAccessToken);
    }

    [Fact]
    public async Task TryLoadAsync_ReturnsPlaintext_FromProtectedFile()
    {
        if (!OperatingSystem.IsWindows()) return;

        var svc = new AppConfigService(ConfigPath);
        await svc.SaveAsync(new AppConfig { GitHubAccessToken = "ghp_roundtrip_789" });

        var loaded = await svc.TryLoadAsync();

        Assert.NotNull(loaded);
        Assert.Equal("ghp_roundtrip_789", loaded!.GitHubAccessToken);
    }

    [Fact]
    public async Task TryLoadAsync_LegacyPlaintextConfig_IsMigratedOnDisk()
    {
        if (!OperatingSystem.IsWindows()) return;

        // A config written by an older version: plaintext token on disk.
        await File.WriteAllTextAsync(ConfigPath,
            "{\"Version\":5,\"GitHubAccessToken\":\"ghp_legacy_on_disk\"}");

        var svc = new AppConfigService(ConfigPath);
        var loaded = await svc.TryLoadAsync();

        Assert.NotNull(loaded);
        Assert.Equal("ghp_legacy_on_disk", loaded!.GitHubAccessToken);

        // One-time migration: the file itself no longer contains the raw token.
        var onDisk = await File.ReadAllTextAsync(ConfigPath);
        Assert.DoesNotContain("ghp_legacy_on_disk", onDisk);
        Assert.Contains("dpapi:v1:", onDisk);
    }
}
