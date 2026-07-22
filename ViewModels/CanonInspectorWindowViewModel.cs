using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.App.ViewModels;

/// <summary>
/// Read-only view model for the Canon Inspector window. It projects the prescriptive
/// Zen canon (<see cref="IZenTextsService.Texts"/>) into section-grouped rows with resolved
/// English/Chinese titles, and raises <see cref="NavigationRequested"/> so the host can open
/// a text in the reader. Nothing here mutates the canon — classification is definitional.
/// </summary>
public sealed partial class CanonInspectorWindowViewModel : ViewModelBase
{
    /// <summary>Raised when the user opens a canon row; the host navigates the reader to it.</summary>
    public event EventHandler<NavigationRequest>? NavigationRequested;

    public string HeaderVersion { get; }
    public string HeaderCount { get; }
    public string GeneratedNote { get; }
    public bool HasGeneratedNote => !string.IsNullOrWhiteSpace(GeneratedNote);
    public bool HasVersion => !string.IsNullOrWhiteSpace(HeaderVersion);

    public IReadOnlyList<CanonSectionGroup> Sections { get; }

    /// <param name="zen">The loaded prescriptive canon service.</param>
    /// <param name="titleResolver">
    /// Resolves a normalized rel path to (English, Chinese) titles. Either may be null/blank when
    /// unknown (e.g. titles.jsonl absent); the row then falls back to the CBETA id.
    /// </param>
    public CanonInspectorWindowViewModel(
        IZenTextsService zen,
        Func<string, (string? En, string? Zh)> titleResolver)
    {
        if (zen == null) throw new ArgumentNullException(nameof(zen));
        titleResolver ??= _ => (null, null);

        var rows = new List<CanonTextRow>(zen.Texts.Count);
        foreach (var rel in zen.Texts)
        {
            if (string.IsNullOrWhiteSpace(rel)) continue;
            var (en, zh) = titleResolver(rel);
            rows.Add(CanonTextRow.Create(rel, en, zh));
        }

        Sections = GroupBySection(rows);

        HeaderVersion = zen.ListVersion ?? "";
        HeaderCount = $"{zen.Texts.Count} text" + (zen.Texts.Count == 1 ? "" : "s");
        GeneratedNote = zen.GeneratedNote ?? "";
    }

    [RelayCommand]
    private void OpenText(CanonTextRow? row)
    {
        if (row == null || string.IsNullOrWhiteSpace(row.RelPath))
            return;

        NavigationRequested?.Invoke(this, new NavigationRequest
        {
            RelPath = row.RelPath,
            Side = SearchSide.Original,
        });
    }

    /// <summary>
    /// Groups canon rows by their canon-section prefix (the first rel-path segment, e.g.
    /// "T"/"X"/"J"/"L"/"M"). Sections are ordered alphabetically; rows keep their input order
    /// (which is the curated order of the canon asset). Pure and deterministic — unit tested.
    /// </summary>
    public static IReadOnlyList<CanonSectionGroup> GroupBySection(IEnumerable<CanonTextRow> rows)
    {
        var groups = new Dictionary<string, List<CanonTextRow>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows ?? Enumerable.Empty<CanonTextRow>())
        {
            if (row == null) continue;
            var section = SectionOf(row.RelPath);
            if (!groups.TryGetValue(section, out var list))
            {
                list = new List<CanonTextRow>();
                groups[section] = list;
            }
            list.Add(row);
        }

        return groups
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new CanonSectionGroup(kv.Key, kv.Value))
            .ToList();
    }

    /// <summary>The canon section is the first path segment; falls back to the leading letter.</summary>
    public static string SectionOf(string? relPath)
    {
        if (string.IsNullOrWhiteSpace(relPath)) return "?";
        var norm = relPath.Replace('\\', '/').TrimStart('/');
        var slash = norm.IndexOf('/');
        var seg = slash > 0 ? norm.Substring(0, slash) : norm;
        return string.IsNullOrWhiteSpace(seg) ? "?" : seg.ToUpperInvariant();
    }
}

/// <summary>A single canon text row: rel path, CBETA id, and resolved titles.</summary>
public sealed class CanonTextRow
{
    public string RelPath { get; init; } = "";
    public string CbetaId { get; init; } = "";
    public string EnglishTitle { get; init; } = "";
    public string ChineseTitle { get; init; } = "";

    /// <summary>True when an English title was resolved (drives the fallback display).</summary>
    public bool HasEnglishTitle => !string.IsNullOrWhiteSpace(EnglishTitle);
    public bool HasChineseTitle => !string.IsNullOrWhiteSpace(ChineseTitle);

    /// <summary>Primary line: English title when known, else the CBETA id.</summary>
    public string PrimaryTitle => HasEnglishTitle ? EnglishTitle : CbetaId;

    public static CanonTextRow Create(string relPath, string? en, string? zh)
    {
        var norm = (relPath ?? "").Replace('\\', '/').TrimStart('/');
        return new CanonTextRow
        {
            RelPath = norm,
            CbetaId = ExtractCbetaId(norm),
            EnglishTitle = (en ?? "").Trim(),
            ChineseTitle = (zh ?? "").Trim(),
        };
    }

    /// <summary>"T/T48/T48n2003.xml" -> "T48n2003" (filename without extension).</summary>
    public static string ExtractCbetaId(string? relPath)
    {
        if (string.IsNullOrWhiteSpace(relPath)) return "";
        var norm = relPath.Replace('\\', '/');
        var slash = norm.LastIndexOf('/');
        var file = slash >= 0 ? norm.Substring(slash + 1) : norm;
        var dot = file.LastIndexOf('.');
        return dot > 0 ? file.Substring(0, dot) : file;
    }
}

/// <summary>A canon section (e.g. "T") holding its rows, with a display header.</summary>
public sealed class CanonSectionGroup
{
    public string Section { get; }
    public IReadOnlyList<CanonTextRow> Rows { get; }
    public int Count => Rows.Count;
    public string Header => $"{Section}  ·  {Count} text" + (Count == 1 ? "" : "s");

    public CanonSectionGroup(string section, IReadOnlyList<CanonTextRow> rows)
    {
        Section = section;
        Rows = rows;
    }
}
