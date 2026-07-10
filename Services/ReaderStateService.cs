using System;
using System.IO;
using System.Text.Json;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Persists per-document reader state (reading layout mode + resume anchor) in
/// reader-state.json next to the executable. Mirrors <see cref="BookmarkService"/>:
/// lazy load, atomic write via temp-file + move, all I/O errors swallowed because
/// reader state is a non-critical convenience.
/// </summary>
public sealed class ReaderStateService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly string _filePath;
    private ReaderState _state = new();
    private bool _loaded;

    public ReaderStateService()
        : this(Path.Combine(AppContext.BaseDirectory, "reader-state.json"))
    {
    }

    /// <summary>Test seam: allows pointing the sidecar at an arbitrary path.</summary>
    internal ReaderStateService(string filePath)
    {
        _filePath = filePath;
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        try
        {
            if (!File.Exists(_filePath)) return;
            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json)) return;
            _state = JsonSerializer.Deserialize<ReaderState>(json, JsonOpts) ?? new();
            _state.Documents ??= new(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            _state = new();
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_state, JsonOpts);
            var tmp = _filePath + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, _filePath, overwrite: true);
        }
        catch
        {
            // Swallow I/O errors; reader state is non-critical.
        }
    }

    private ReaderDocumentState? Get(string relPath)
    {
        if (string.IsNullOrWhiteSpace(relPath)) return null;
        EnsureLoaded();
        return _state.Documents.TryGetValue(relPath, out var doc) ? doc : null;
    }

    private ReaderDocumentState GetOrCreate(string relPath)
    {
        EnsureLoaded();
        if (!_state.Documents.TryGetValue(relPath, out var doc))
        {
            doc = new ReaderDocumentState();
            _state.Documents[relPath] = doc;
        }
        return doc;
    }

    /// <summary>
    /// Returns the persisted layout mode for a file, or <see cref="ReadingLayoutMode.MergedFlow"/>
    /// when none (A2: MergedFlow is the SPA-parity default preference). Page remains the RUNTIME
    /// fallback for map-less files — the view degrades merged→page in
    /// <c>ApplyReadingLayoutAsync</c> without clobbering this stored preference.
    /// </summary>
    public ReadingLayoutMode GetLayoutMode(string relPath)
        => Get(relPath)?.LayoutMode ?? ReadingLayoutMode.MergedFlow;

    /// <summary>Persists the layout mode for a file.</summary>
    public void SetLayoutMode(string relPath, ReadingLayoutMode mode)
    {
        if (string.IsNullOrWhiteSpace(relPath)) return;
        var doc = GetOrCreate(relPath);
        if (doc.LayoutMode == mode) return;
        doc.LayoutMode = mode;
        Save();
    }

    /// <summary>Returns the resume anchor for a file, or null when none was captured.</summary>
    public ResumeAnchor? GetResumeAnchor(string relPath)
        => Get(relPath)?.Resume;

    /// <summary>Persists the resume anchor for a file.</summary>
    public void SetResumeAnchor(string relPath, string? lb, string? side)
    {
        if (string.IsNullOrWhiteSpace(relPath) || string.IsNullOrWhiteSpace(lb)) return;
        var doc = GetOrCreate(relPath);
        doc.Resume = new ResumeAnchor
        {
            Lb = lb,
            Side = side,
            UpdatedUtc = DateTime.UtcNow
        };
        Save();
    }
}
