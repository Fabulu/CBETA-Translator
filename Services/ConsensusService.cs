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
/// Finds disagreements between two coders and persists consensus resolutions
/// as per-user JSONL files under community/consensus/.
/// </summary>
public sealed class ConsensusService
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

    /// <summary>
    /// Identifies lb x tagId pairs where exactly one coder applied the tag.
    /// </summary>
    public static List<Disagreement> FindDisagreements(
        string relPath,
        List<string> allLbValues,
        List<DocumentTag> coder1Tags,
        List<DocumentTag> coder2Tags,
        TagVocabulary? vocab1,
        TagVocabulary? vocab2)
    {
        var tags1 = coder1Tags.Where(t => string.Equals(t.RelPath, relPath, StringComparison.Ordinal)).ToList();
        var tags2 = coder2Tags.Where(t => string.Equals(t.RelPath, relPath, StringComparison.Ordinal)).ToList();

        var tagNames = new Dictionary<string, string>(StringComparer.Ordinal);
        if (vocab1?.Tags != null)
            foreach (var td in vocab1.Tags)
                tagNames[td.Id] = td.DisplayName;
        if (vocab2?.Tags != null)
            foreach (var td in vocab2.Tags)
                tagNames.TryAdd(td.Id, td.DisplayName);

        var allTagIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var t in tags1) allTagIds.Add(t.TagId);
        foreach (var t in tags2) allTagIds.Add(t.TagId);

        var result = new List<Disagreement>();

        foreach (var tagId in allTagIds.OrderBy(id => id, StringComparer.Ordinal))
        {
            foreach (var lb in allLbValues)
            {
                bool c1 = LbContainedByAny(lb, tags1, tagId);
                bool c2 = LbContainedByAny(lb, tags2, tagId);

                if (c1 == c2) continue; // agreement — skip

                string name = tagNames.TryGetValue(tagId, out var n) ? n : tagId;

                result.Add(new Disagreement
                {
                    FromLb = lb,
                    ToLb = lb,
                    TagId = tagId,
                    TagName = name,
                    Coder1HasIt = c1,
                    Coder2HasIt = c2
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Saves consensus resolutions to community/consensus/{username}.jsonl using atomic write.
    /// </summary>
    public async Task SaveResolutionsAsync(
        string root,
        string username,
        List<ConsensusResolution> resolutions,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));

        var dir = GetConsensusDir(root);
        Directory.CreateDirectory(dir);

        var path = Path.Combine(dir, SanitizeFilename(username) + ".jsonl");

        // Safety: never overwrite a non-empty file with empty data
        if (resolutions.Count == 0 && File.Exists(path) && new FileInfo(path).Length > 10)
            return;

        var sb = new StringBuilder();
        foreach (var r in resolutions)
            sb.AppendLine(JsonSerializer.Serialize(r, CompactOpts));

        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, sb.ToString(), new UTF8Encoding(false), ct);
        File.Move(tmpPath, path, overwrite: true);
    }

    /// <summary>
    /// Loads consensus resolutions from community/consensus/{username}.jsonl.
    /// Returns empty list if file does not exist or is empty.
    /// </summary>
    public async Task<List<ConsensusResolution>> LoadResolutionsAsync(
        string root,
        string username,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));

        var dir = GetConsensusDir(root);
        var path = Path.Combine(dir, SanitizeFilename(username) + ".jsonl");

        var result = new List<ConsensusResolution>();
        if (!File.Exists(path))
            return result;

        var lines = await File.ReadAllLinesAsync(path, Encoding.UTF8, ct);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var r = JsonSerializer.Deserialize<ConsensusResolution>(line, ReadOpts);
                if (r != null) result.Add(r);
            }
            catch
            {
                // skip malformed lines
            }
        }

        return result;
    }

    // ── Path helpers ────────────────────────────────────────────────────

    private static string GetConsensusDir(string root)
        => Path.Combine(root, "community", "consensus");

    private static bool LbContainedByAny(string lb, List<DocumentTag> tags, string tagId)
    {
        foreach (var tag in tags)
        {
            if (!string.Equals(tag.TagId, tagId, StringComparison.Ordinal))
                continue;
            if (string.Compare(tag.FromLb, lb, StringComparison.Ordinal) <= 0 &&
                string.Compare(lb, tag.ToLb, StringComparison.Ordinal) <= 0)
                return true;
        }
        return false;
    }

    private static string SanitizeFilename(string name)
        => ReadZen.App.Infrastructure.FileNameSanitizer.Strict(name);
}
