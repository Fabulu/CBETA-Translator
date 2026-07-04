using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class AppConfigService : IAppConfigService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string ConfigPath { get; }

    public int NavStatusFilterIndex { get; set; } = 0; // 0=All,1=Green,2=Yellow,3=Red


    public AppConfigService()
    {
        // Portable layout by design (user decision D8): config.json next to the exe
        ConfigPath = Path.Combine(AppContext.BaseDirectory, "config.json");
    }

    /// <summary>Test seam: point the service at a temp config file.</summary>
    internal AppConfigService(string configPath)
    {
        ConfigPath = configPath;
    }

    public async Task<AppConfig?> TryLoadAsync()
    {
        try
        {
            if (!File.Exists(ConfigPath))
                return null;

            var json = await File.ReadAllTextAsync(ConfigPath);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var cfg = JsonSerializer.Deserialize<AppConfig>(json, JsonOpts);
            if (cfg == null) return null;

            // v3 -> v4 migration: existing configs don't carry ActiveCorpus.
            // Assume CBETA (the original corpus) and bump version.
            if (cfg.Version < 4)
            {
                cfg.ActiveCorpus = CorpusKind.Cbeta;
                cfg.Version = 4;
            }

            // v4 -> v5 migration: add preferred citation style (default Chicago = 1).
            if (cfg.Version < 5)
            {
                cfg.PreferredCitationStyleIndex = 1; // Chicago
                cfg.PreferredCitationStyle = CitationStyle.Chicago;
                cfg.Version = 5;
            }

            // OAuth token at-rest protection (audit P1.5 / R3-H3). In-memory config
            // always carries the plaintext; only the file is protected.
            if (!string.IsNullOrEmpty(cfg.GitHubAccessToken))
            {
                if (TokenProtector.IsProtected(cfg.GitHubAccessToken))
                {
                    // Null result (different user/machine, corrupt blob) drops the
                    // token; the user simply re-authenticates.
                    cfg.GitHubAccessToken = TokenProtector.TryUnprotect(cfg.GitHubAccessToken);
                }
                else if (OperatingSystem.IsWindows())
                {
                    // One-time migration: legacy plaintext token found on disk —
                    // rewrite the file immediately with the protected form.
                    await SaveAsync(cfg);
                }
            }

            return cfg;
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveAsync(AppConfig cfg)
    {
        // Never write the OAuth token to disk in plaintext (audit P1.5 / R3-H3).
        // Protect a serialization-only copy so the caller's live config keeps the
        // plaintext it works with in memory.
        var toWrite = cfg;
        if (!string.IsNullOrEmpty(cfg.GitHubAccessToken) && !TokenProtector.IsProtected(cfg.GitHubAccessToken))
        {
            toWrite = JsonSerializer.Deserialize<AppConfig>(JsonSerializer.Serialize(cfg, JsonOpts), JsonOpts)!;
            toWrite.GitHubAccessToken = TokenProtector.Protect(cfg.GitHubAccessToken!);
        }

        var json = JsonSerializer.Serialize(toWrite, JsonOpts);
        var tmpPath = ConfigPath + ".tmp";
        await File.WriteAllTextAsync(tmpPath, json);
        File.Move(tmpPath, ConfigPath, overwrite: true);
    }
}
