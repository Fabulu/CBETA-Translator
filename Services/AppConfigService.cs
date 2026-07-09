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

    /// <summary>Path of the backup written when a corrupt config.json is detected.</summary>
    public string CorruptBackupPath => ConfigPath + ".corrupt";

    /// <summary>
    /// One-time notice set by <see cref="TryLoadAsync"/> when config.json fails to
    /// parse: the bad file is preserved at <see cref="CorruptBackupPath"/> and this
    /// message describes the recovery so the load does not silently reset every
    /// setting (including the stored OAuth token). Null when the last load was clean.
    /// </summary>
    public string? LoadWarning { get; private set; }


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
        LoadWarning = null;

        // Read phase: an IO failure (file locked/unreadable) must leave the file
        // intact — do NOT treat it as corruption and do NOT overwrite anything.
        string json;
        try
        {
            if (!File.Exists(ConfigPath))
                return null;

            json = await File.ReadAllTextAsync(ConfigPath);
        }
        catch
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(json))
            return null;

        // Parse phase: a non-empty file that fails to deserialize is corrupt.
        // Previously the catch silently returned null, resetting ALL settings
        // (including the stored OAuth token) with zero feedback. Instead, preserve
        // the bad file so the user can recover it, and surface a one-time notice.
        AppConfig? cfg;
        try
        {
            cfg = JsonSerializer.Deserialize<AppConfig>(json, JsonOpts);
        }
        catch (Exception ex)
        {
            BackupCorruptConfig(ex.Message);
            return null;
        }

        if (cfg == null)
        {
            BackupCorruptConfig("config.json deserialized to null");
            return null;
        }

        try
        {
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
            // Migration / token-protection step failed (e.g. transient IO on the
            // one-time re-save). The config already parsed cleanly, so return it
            // rather than discarding every setting.
            return cfg;
        }
    }

    private void BackupCorruptConfig(string reason)
    {
        try
        {
            if (File.Exists(ConfigPath))
                File.Copy(ConfigPath, CorruptBackupPath, overwrite: true);
        }
        catch
        {
            // Best-effort preservation; never let backup failure crash startup.
        }

        LoadWarning =
            $"config.json could not be read ({reason}). Your previous settings were " +
            $"kept at {CorruptBackupPath} and defaults are being used for this session.";
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
