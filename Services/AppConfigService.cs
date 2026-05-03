using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
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
        // "Hacked is fine": config.json next to the exe
        ConfigPath = Path.Combine(AppContext.BaseDirectory, "config.json");
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

            return cfg;
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveAsync(AppConfig cfg)
    {
        var json = JsonSerializer.Serialize(cfg, JsonOpts);
        var tmpPath = ConfigPath + ".tmp";
        await File.WriteAllTextAsync(tmpPath, json);
        File.Move(tmpPath, ConfigPath, overwrite: true);
    }
}
