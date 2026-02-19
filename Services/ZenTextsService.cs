// Services/ZenTextsService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Services;

public sealed class ZenTextsService
{
    private const string FileName = "zen_texts.json";
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private HashSet<string> _zen = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    private static string GetPath(string root) => Path.Combine(root, FileName);

    public async Task LoadAsync(string root)
    {
        await _gate.WaitAsync();
        try
        {
            _zen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _loaded = true;

            var path = GetPath(root);
            if (!File.Exists(path))
            {
                await SaveInternalAsync(root); // creates empty file
                return;
            }

            var json = await File.ReadAllTextAsync(path, Utf8NoBom);
            if (string.IsNullOrWhiteSpace(json)) return;

            var data = JsonSerializer.Deserialize<ZenFile>(json);
            if (data?.Zen != null)
            {
                foreach (var rel in data.Zen)
                    if (!string.IsNullOrWhiteSpace(rel))
                        _zen.Add(Norm(rel));
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public bool IsZen(string relPath)
        => _loaded && _zen.Contains(Norm(relPath));

    public async Task SetZenAsync(string root, string relPath, bool isZen)
    {
        await _gate.WaitAsync();
        try
        {
            if (!_loaded)
                await LoadAsync(root);

            relPath = Norm(relPath);

            if (isZen) _zen.Add(relPath);
            else _zen.Remove(relPath);

            await SaveInternalAsync(root);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SaveInternalAsync(string root)
    {
        var path = GetPath(root);

        var data = new ZenFile
        {
            Version = 1,
            UpdatedUtc = DateTime.UtcNow,
            Zen = new List<string>(_zen)
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(path, json, Utf8NoBom);
    }

    private static string Norm(string p)
        => (p ?? "").Replace('\\', '/').TrimStart('/');

    private sealed class ZenFile
    {
        public int Version { get; set; } = 1;
        public DateTime UpdatedUtc { get; set; }
        public List<string> Zen { get; set; } = new();
    }
}
