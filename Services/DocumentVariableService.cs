using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Manages document-level variables stored in document-variables.jsonl.
/// </summary>
public sealed class DocumentVariableService : IDocumentVariableService
{
    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions CompactOpts = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<List<DocumentVariable>> LoadAsync(string root, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));

        var path = GetPath(root);
        if (!File.Exists(path))
            return new List<DocumentVariable>();

        var result = new List<DocumentVariable>();
        var lines = await File.ReadAllLinesAsync(path, Encoding.UTF8, ct);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var v = JsonSerializer.Deserialize<DocumentVariable>(line, ReadOpts);
                if (v != null) result.Add(v);
            }
            catch
            {
                // Skip malformed lines
            }
        }
        return result;
    }

    public async Task SaveAsync(string root, List<DocumentVariable> vars, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));
        if (vars == null) throw new ArgumentNullException(nameof(vars));

        Directory.CreateDirectory(root);
        var path = GetPath(root);
        var sb = new StringBuilder();
        foreach (var v in vars)
            sb.AppendLine(JsonSerializer.Serialize(v, CompactOpts));

        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, sb.ToString(), new UTF8Encoding(false), ct);
        File.Move(tmpPath, path, overwrite: true);
    }

    public Dictionary<string, Dictionary<string, int>> CrossTabulate(
        List<DocumentTag> tags,
        List<DocumentVariable> vars,
        TagVocabulary vocab,
        string variableName)
    {
        if (tags == null) throw new ArgumentNullException(nameof(tags));
        if (vars == null) throw new ArgumentNullException(nameof(vars));

        var tagNames = new Dictionary<string, string>(StringComparer.Ordinal);
        if (vocab?.Tags != null)
        {
            foreach (var td in vocab.Tags)
                tagNames.TryAdd(td.Id, td.DisplayName);
        }

        // Build lookup: RelPath -> variable value (for the specified variable name)
        var varLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in vars)
        {
            if (string.Equals(v.VariableName, variableName, StringComparison.OrdinalIgnoreCase))
                varLookup[v.RelPath] = v.VariableValue;
        }

        // Group tags by file, then by variable value
        var result = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);

        foreach (var tag in tags)
        {
            if (!varLookup.TryGetValue(tag.RelPath, out var varValue))
                varValue = "(unset)";

            if (!result.TryGetValue(varValue, out var codeCounts))
            {
                codeCounts = new Dictionary<string, int>(StringComparer.Ordinal);
                result[varValue] = codeCounts;
            }

            string codeName = tagNames.TryGetValue(tag.TagId, out var n) ? n : tag.TagId;
            codeCounts.TryGetValue(codeName, out int count);
            codeCounts[codeName] = count + 1;
        }

        return result;
    }

    internal static string GetPath(string root) => Path.Combine(root, "document-variables.jsonl");
}
