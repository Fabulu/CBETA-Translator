using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Persists user bookmarks in bookmarks.json next to the executable.
/// Provides CRUD operations with atomic file writes.
/// </summary>
public sealed class BookmarkService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly string _filePath;
    private List<Bookmark> _bookmarks = new();
    private bool _loaded;

    public BookmarkService()
        : this(Path.Combine(AppContext.BaseDirectory, "bookmarks.json"))
    {
    }

    /// <summary>Test seam: allows pointing the sidecar at an arbitrary path.</summary>
    internal BookmarkService(string filePath)
    {
        _filePath = filePath;
    }

    /// <summary>Ensures bookmarks are loaded from disk (idempotent).</summary>
    private void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        try
        {
            if (!File.Exists(_filePath)) return;
            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json)) return;
            _bookmarks = JsonSerializer.Deserialize<List<Bookmark>>(json, JsonOpts) ?? new();
        }
        catch
        {
            _bookmarks = new();
        }
    }

    /// <summary>Persists the current bookmark list to disk with atomic write.</summary>
    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_bookmarks, JsonOpts);
            var tmp = _filePath + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, _filePath, overwrite: true);
        }
        catch
        {
            // Swallow I/O errors; bookmarks are non-critical.
        }
    }

    /// <summary>Adds a bookmark and persists.</summary>
    public void Add(Bookmark bookmark)
    {
        EnsureLoaded();
        _bookmarks.Add(bookmark);
        Save();
    }

    /// <summary>Removes a bookmark by reference equality or matching key fields.</summary>
    public void Remove(Bookmark bookmark)
    {
        EnsureLoaded();
        _bookmarks.RemoveAll(b =>
            string.Equals(b.RelPath, bookmark.RelPath, StringComparison.OrdinalIgnoreCase) &&
            b.DisplayOffset == bookmark.DisplayOffset &&
            b.CreatedUtc == bookmark.CreatedUtc);
        Save();
    }

    /// <summary>Returns all bookmarks for a given file.</summary>
    public IReadOnlyList<Bookmark> ForFile(string relPath)
    {
        EnsureLoaded();
        return _bookmarks
            .Where(b => string.Equals(b.RelPath, relPath, StringComparison.OrdinalIgnoreCase))
            .OrderBy(b => b.DisplayOffset)
            .ToList();
    }

    /// <summary>Returns all bookmarks across all files, newest first.</summary>
    public IReadOnlyList<Bookmark> All()
    {
        EnsureLoaded();
        return _bookmarks.OrderByDescending(b => b.CreatedUtc).ToList();
    }
}
