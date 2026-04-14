using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class ZenMasterManagerService
{
    private readonly IMasterDatesService _masterDatesService;

    public ZenMasterManagerService(IMasterDatesService masterDatesService)
    {
        _masterDatesService = masterDatesService;
    }

    public async Task<ZenMasterCatalog> LoadAsync(string? repoRoot, string? baseFilePath = null, CancellationToken ct = default)
    {
        var records = new List<ZenMasterRecord>();

        foreach (var entry in LoadBaseEntries(baseFilePath))
            AddVariant(records, entry, isBase: true, username: null);

        if (!string.IsNullOrWhiteSpace(repoRoot))
        {
            var communityDir = IMasterDatesService.GetCommunityMasterDatesDir(repoRoot);
            var allCommunity = await _masterDatesService.LoadAllCommunityMasterDatesAsync(communityDir, ct);

            foreach (var pair in allCommunity.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                foreach (var entry in pair.Value)
                    AddVariant(records, entry, isBase: false, username: pair.Key);
            }
        }

        foreach (var record in records)
            NormalizeRecord(record);

        return new ZenMasterCatalog
        {
            Records = records
                .OrderBy(r => r.CanonicalName, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    public ZenMasterRecord? FindByName(IEnumerable<ZenMasterRecord> records, string? name, string? preferredUser = null)
        => FindLandingMatch(records, name, preferredUser)?.Record;

    public ZenMasterLandingMatch? FindLandingMatch(IEnumerable<ZenMasterRecord> records, string? name, string? preferredUser = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var requested = name.Trim();
        var candidates = records
            .Where(r => r.Aliases.Any(a => string.Equals(a, requested, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = records
                .Where(r => r.Aliases.Any(a => a.Contains(requested, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        if (candidates.Count == 0)
            return null;

        var selected = candidates
            .OrderByDescending(r => HasPreferredUserVariant(r, preferredUser))
            .ThenByDescending(r => r.HasBase)
            .ThenBy(r => r.CanonicalName, StringComparer.OrdinalIgnoreCase)
            .First();

        var variant = selected.GetPreferredVariant(preferredUser) ?? selected.PrimaryVariant;
        if (variant == null)
            return null;

        return new ZenMasterLandingMatch
        {
            Record = selected,
            Variant = variant,
            UsedPreferredUser = HasPreferredUserVariant(selected, preferredUser)
        };
    }

    /// <summary>
    /// Scans a text passage for any mentioned master names (Chinese or English).
    /// Returns the first match found, or null. Checks CJK names (2+ chars) for substring match.
    /// </summary>
    public ZenMasterRecord? FindMasterInText(IEnumerable<ZenMasterRecord> records, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        // Check CJK names first (more specific, less false positives)
        foreach (var r in records)
        {
            foreach (var alias in r.Aliases)
            {
                if (alias.Length >= 2 && MasterDatesService.ContainsCjk(alias) && text.Contains(alias, StringComparison.Ordinal))
                    return r;
            }
        }

        return null;
    }

    private static IEnumerable<MasterDateEntry> LoadBaseEntries(string? baseFilePath)
    {
        var path = string.IsNullOrWhiteSpace(baseFilePath)
            ? Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "master-dates.json")
            : baseFilePath;

        if (!File.Exists(path))
            yield break;

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (!doc.RootElement.TryGetProperty("masters", out var mastersEl))
            yield break;

        foreach (var m in mastersEl.EnumerateArray())
        {
            var names = new List<string>();
            if (m.TryGetProperty("names", out var namesEl))
            {
                foreach (var n in namesEl.EnumerateArray())
                {
                    var s = n.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(s))
                        names.Add(s);
                }
            }

            if (names.Count == 0)
                continue;

            yield return new MasterDateEntry
            {
                Names = names,
                Floruit = m.TryGetProperty("floruit", out var f) ? f.GetInt32() : 0,
                Death = m.TryGetProperty("death", out var d) ? d.GetInt32() : 0,
            };
        }
    }

    private static void AddVariant(List<ZenMasterRecord> records, MasterDateEntry entry, bool isBase, string? username)
    {
        var names = entry.Names
            .Select(n => n.Trim())
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (names.Count == 0)
            return;

        var record = records.FirstOrDefault(r => SharesAnyName(r.Aliases, names));
        if (record == null)
        {
            record = new ZenMasterRecord();
            records.Add(record);
        }

        var duplicateVariant = record.Variants.Any(v =>
            v.IsBase == isBase &&
            string.Equals(v.Username, username, StringComparison.OrdinalIgnoreCase) &&
            v.Floruit == entry.Floruit &&
            v.Death == entry.Death &&
            SharesAnyName(v.Names, names));
        if (duplicateVariant)
            return;

        record.Variants.Add(new ZenMasterVariant
        {
            Names = names,
            Floruit = entry.Floruit,
            Death = entry.Death,
            IsBase = isBase,
            Username = username,
            Notes = entry.Notes,
            School = entry.School,
            Teacher = entry.Teacher,
            Students = entry.Students,
            Region = entry.Region,
            ReferenceUrl = entry.ReferenceUrl,
        });

        foreach (var name in names)
        {
            if (!record.Aliases.Contains(name, StringComparer.OrdinalIgnoreCase))
                record.Aliases.Add(name);
        }
    }

    private static void NormalizeRecord(ZenMasterRecord record)
    {
        var primary = record.PrimaryVariant ?? record.Variants.FirstOrDefault();
        record.CanonicalName = primary?.PrimaryName ?? record.Aliases.FirstOrDefault() ?? "(unnamed)";
        record.Aliases = record.Aliases
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => string.Equals(n, record.CanonicalName, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
        record.Variants = record.Variants
            .OrderByDescending(v => v.IsBase)
            .ThenBy(v => string.IsNullOrWhiteSpace(v.Username) ? 1 : 0)
            .ThenBy(v => v.Username ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(v => v.PrimaryName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool HasPreferredUserVariant(ZenMasterRecord record, string? preferredUser)
    {
        if (string.IsNullOrWhiteSpace(preferredUser))
            return false;

        return record.Variants.Any(v =>
            !v.IsBase &&
            !string.IsNullOrWhiteSpace(v.Username) &&
            string.Equals(v.Username, preferredUser.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static bool SharesAnyName(IEnumerable<string> a, IEnumerable<string> b)
    {
        var entryA = new MasterDateEntry { Names = a.ToList() };
        var entryB = new MasterDateEntry { Names = b.ToList() };
        return MasterDatesService.SharesAnyName(entryA, entryB);
    }
}
