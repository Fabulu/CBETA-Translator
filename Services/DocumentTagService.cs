using ReadZen.App.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ReadZen.App.Services;

public sealed class DocumentTagService : IDocumentTagService
{
    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonSerializerOptions CompactOpts = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // ── Vocabulary ──────────────────────────────────────────────────────

    public async Task<TagVocabulary> LoadVocabularyAsync(string root, string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));

        var path = GetVocabularyPath(root);
        if (!File.Exists(path))
            return new TagVocabulary();

        try
        {
            string json = await File.ReadAllTextAsync(path, Encoding.UTF8, ct);
            if (string.IsNullOrWhiteSpace(json))
                return new TagVocabulary();

            return JsonSerializer.Deserialize<TagVocabulary>(json, ReadOpts)
                   ?? new TagVocabulary();
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            // Corrupted JSON should not crash the app — return empty and let user re-create
            return new TagVocabulary();
        }
    }

    public async Task SaveVocabularyAsync(string root, string username, TagVocabulary vocab, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        if (vocab == null)
            throw new ArgumentNullException(nameof(vocab));

        var path = GetVocabularyPath(root);
        Directory.CreateDirectory(root);

        // Safety: never overwrite a non-empty file with empty data
        if ((vocab.Tags == null || vocab.Tags.Count == 0) && File.Exists(path) && new FileInfo(path).Length > 10)
            return;

        var json = JsonSerializer.Serialize(vocab, WriteOpts);
        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, json, new UTF8Encoding(false), ct);
        File.Move(tmpPath, path, overwrite: true);
    }

    // ── Applied tags ────────────────────────────────────────────────────

    public async Task<List<DocumentTag>> LoadUserTagsAsync(string root, string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));

        // Try per-user community path first, fall back to legacy shared file
        var userPath = GetUserTagsPath(root, username);
        if (File.Exists(userPath))
            return await LoadTagsJsonlAsync(userPath, ct);

        var legacyPath = GetTagsPath(root);
        return await LoadTagsJsonlAsync(legacyPath, ct);
    }

    public async Task SaveUserTagsAsync(string root, string username, List<DocumentTag> tags, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        if (tags == null)
            throw new ArgumentNullException(nameof(tags));

        // Always save to per-user community path
        var dir = GetCommunityTagsDir(root);
        Directory.CreateDirectory(dir);

        var path = GetUserTagsPath(root, username);
        await WriteTagsJsonlAsync(path, tags, ct);
    }

    // ── Community tags ──────────────────────────────────────────────────

    public async Task<Dictionary<string, List<DocumentTag>>> LoadAllCommunityTagsAsync(string root, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));

        var result = new Dictionary<string, List<DocumentTag>>(StringComparer.OrdinalIgnoreCase);

        var dir = GetCommunityTagsDir(root);
        if (!Directory.Exists(dir))
            return result;

        foreach (var file in Directory.GetFiles(dir, "*.jsonl"))
        {
            ct.ThrowIfCancellationRequested();

            var username = Path.GetFileNameWithoutExtension(file);
            var tags = await LoadTagsJsonlAsync(file, ct);

            if (tags.Count > 0)
                result[username] = tags;
        }

        return result;
    }

    public async Task<Dictionary<string, TagVocabulary>> LoadAllCommunityVocabulariesAsync(string root, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));

        var result = new Dictionary<string, TagVocabulary>(StringComparer.OrdinalIgnoreCase);

        var dir = GetCommunityVocabulariesDir(root);
        if (!Directory.Exists(dir))
            return result;

        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            ct.ThrowIfCancellationRequested();

            var username = Path.GetFileNameWithoutExtension(file);

            try
            {
                string json = await File.ReadAllTextAsync(file, Encoding.UTF8, ct);
                if (string.IsNullOrWhiteSpace(json))
                    continue;

                var vocab = JsonSerializer.Deserialize<TagVocabulary>(json, ReadOpts);
                if (vocab != null)
                    result[username] = vocab;
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                // Skip corrupted community vocabulary files
            }
        }

        return result;
    }

    // ── Share to community ──────────────────────────────────────────────

    public async Task WriteUserCommunityTagsAsync(string root, string username, List<DocumentTag> tags, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        if (tags == null)
            throw new ArgumentNullException(nameof(tags));

        var dir = GetCommunityTagsDir(root);
        Directory.CreateDirectory(dir);

        var safeUsername = SanitizeFilename(username);
        var path = Path.Combine(dir, safeUsername + ".jsonl");
        GuardPathTraversal(path, dir);

        await WriteTagsJsonlAsync(path, tags, ct);
    }

    public async Task WriteUserCommunityVocabularyAsync(string root, string username, TagVocabulary vocab, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        if (vocab == null)
            throw new ArgumentNullException(nameof(vocab));

        var dir = GetCommunityVocabulariesDir(root);
        Directory.CreateDirectory(dir);

        var safeUsername = SanitizeFilename(username);
        var path = Path.Combine(dir, safeUsername + ".json");
        GuardPathTraversal(path, dir);

        var json = JsonSerializer.Serialize(vocab, WriteOpts);
        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, json, new UTF8Encoding(false), ct);
        File.Move(tmpPath, path, overwrite: true);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static async Task<List<DocumentTag>> LoadTagsJsonlAsync(string path, CancellationToken ct)
    {
        var tags = new List<DocumentTag>();

        if (!File.Exists(path))
            return tags;

        var lines = await File.ReadAllLinesAsync(path, Encoding.UTF8, ct);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var tag = JsonSerializer.Deserialize<DocumentTag>(line, ReadOpts);
                if (tag != null)
                    tags.Add(tag);
            }
            catch
            {
                // Skip malformed lines
            }
        }

        return tags;
    }

    private static async Task WriteTagsJsonlAsync(string path, List<DocumentTag> tags, CancellationToken ct)
    {
        // Safety: never overwrite a non-empty file with empty data
        if (tags.Count == 0 && File.Exists(path) && new FileInfo(path).Length > 10)
            return;

        var sb = new StringBuilder();
        foreach (var tag in tags)
        {
            sb.AppendLine(JsonSerializer.Serialize(tag, CompactOpts));
        }

        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, sb.ToString(), new UTF8Encoding(false), ct);
        File.Move(tmpPath, path, overwrite: true);
    }

    private static void GuardPathTraversal(string path, string dir)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDir = Path.GetFullPath(dir);
        // Ensure dir ends with separator so "tags-evil/f" doesn't pass for "tags/"
        if (!fullDir.EndsWith(Path.DirectorySeparatorChar))
            fullDir += Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(fullDir, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Path traversal detected");
    }

    private static string SanitizeFilename(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (Array.IndexOf(invalid, ch) < 0 && ch != '.' && ch != ' ')
                sb.Append(ch);
        }
        return sb.Length > 0 ? sb.ToString() : "unknown";
    }

    // ── Path conventions ────────────────────────────────────────────────

    public static string GetVocabularyPath(string root)
        => Path.Combine(root, "tag-vocabulary.json");

    /// <summary>Legacy shared tags path (pre-per-user migration).</summary>
    public static string GetTagsPath(string root)
        => Path.Combine(root, "document-tags.jsonl");

    /// <summary>Per-user tags path under the community directory.</summary>
    public static string GetUserTagsPath(string root, string username)
        => Path.Combine(GetCommunityTagsDir(root), SanitizeFilename(username) + ".jsonl");

    public static string GetCommunityTagsDir(string root)
        => Path.Combine(root, "community", "tags");

    public static string GetCommunityVocabulariesDir(string root)
        => Path.Combine(root, "community", "tag-vocabularies");
}
