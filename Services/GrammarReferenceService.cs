using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CbetaTranslator.App.Services;

public sealed class GrammarReferenceService : IGrammarReferenceService
{
    private Dictionary<char, GrammarParticleInfo>? _lookup;
    private bool _loadAttempted;
    private readonly object _lock = new();

    public GrammarParticleInfo? Lookup(char ch)
    {
        EnsureLoaded();
        if (_lookup == null)
            return null;
        return _lookup.GetValueOrDefault(ch);
    }

    private void EnsureLoaded()
    {
        if (_loadAttempted) return;
        lock (_lock)
        {
            if (_loadAttempted) return;
            _loadAttempted = true;

            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "grammar-particles.json");
                if (!File.Exists(path))
                    return;

                var json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);

                _lookup = new Dictionary<char, GrammarParticleInfo>();

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var charStr = element.GetProperty("char").GetString();
                    if (string.IsNullOrEmpty(charStr))
                        continue;

                    var ch = charStr[0];
                    var info = new GrammarParticleInfo { Character = ch };

                    if (element.TryGetProperty("functions", out var funcsEl))
                    {
                        foreach (var funcEl in funcsEl.EnumerateArray())
                        {
                            info.Functions.Add(new GrammarFunction
                            {
                                Role = funcEl.TryGetProperty("role", out var r) ? r.GetString() ?? "" : "",
                                Gloss = funcEl.TryGetProperty("gloss", out var g) ? g.GetString() ?? "" : "",
                                Example = funcEl.TryGetProperty("example", out var e) ? e.GetString() ?? "" : "",
                                ExampleGloss = funcEl.TryGetProperty("exampleGloss", out var eg) ? eg.GetString() ?? "" : ""
                            });
                        }
                    }

                    _lookup[ch] = info;
                }
            }
            catch
            {
                _lookup = null;
            }
        }
    }
}
