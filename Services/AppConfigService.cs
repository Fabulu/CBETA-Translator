using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public sealed class AppConfigService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true
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

            return JsonSerializer.Deserialize<AppConfig>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveAsync(AppConfig cfg)
    {
        var json = JsonSerializer.Serialize(cfg, JsonOpts);
        await File.WriteAllTextAsync(ConfigPath, json);
    }
}
