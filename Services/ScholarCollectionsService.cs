using CbetaTranslator.App.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Services;

public sealed class ScholarCollectionsService : IScholarCollectionsService
{
    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOpts = new()
    {
        WriteIndented = true
    };

    public async Task<List<ScholarCollection>> LoadAsync(string root, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));

        var path = GetPath(root);
        if (!File.Exists(path))
            return new List<ScholarCollection>();

        string json = await File.ReadAllTextAsync(path, Encoding.UTF8, ct);
        if (string.IsNullOrWhiteSpace(json))
            return new List<ScholarCollection>();

        var collections = JsonSerializer.Deserialize<List<ScholarCollection>>(json, ReadOpts)
                          ?? new List<ScholarCollection>();

        return collections;
    }

    public async Task SaveAsync(string root, List<ScholarCollection> collections, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));

        if (collections == null)
            throw new ArgumentNullException(nameof(collections));

        var path = GetPath(root);
        Directory.CreateDirectory(root);

        var json = JsonSerializer.Serialize(collections, WriteOpts);
        await File.WriteAllTextAsync(path, json, new UTF8Encoding(false), ct);
    }

    public static string GetPath(string root)
    {
        return Path.Combine(root, "scholar-collections.json");
    }
}
