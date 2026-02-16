using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public sealed class BuddhistMetadataService
{
    private const string UnknownTradition = "Unknown Tradition";
    private static readonly XNamespace TeiNs = "http://www.tei-c.org/ns/1.0";
    private static readonly string MetadataFileName = "buddhist_metadata_analysis.json";
    private static readonly string MetadataFolderName = "CBETA_Sorting_Data";

    private static readonly Dictionary<string, string[]> TraditionKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Chan/Zen"] = new[] { "禪", "禅", "chan", "zen", "曹洞", "臨濟", "雲門", "潙仰", "法眼" },
        ["Pure Land"] = new[] { "淨土", "净土", "净土宗", "淨土宗", "阿彌陀" },
        ["Tiantai"] = new[] { "天台", "法華", "止觀", "智者", "天台宗" },
        ["Huayan"] = new[] { "華嚴", "华严", "賢首", "法藏", "澄觀" },
        ["Vinaya"] = new[] { "律", "毗奈耶", "戒律", "四分律", "五分律" },
        ["Madhyamaka"] = new[] { "中觀", "中論", "龍樹", "提婆", "三論" },
        ["Yogacara"] = new[] { "瑜伽", "唯識", "無著", "世親", "成唯識" },
        ["Esoteric"] = new[] { "密", "密教", "陀羅尼", "真言", "壇城", "曼荼羅" },
        ["Pure Precepts"] = new[] { "菩薩戒", "梵網經", "心地戒" },
        ["Pali/Theravada"] = new[] { "南傳", "巴利", "therav", "尼柯耶" },
        ["Tibetan"] = new[] { "藏傳", "西藏", "tibetan", "甘珠爾", "丹珠爾" },
        ["Commentarial"] = new[] { "註", "疏", "記", "釋", "解", "論", "鈔" },
        ["Historical"] = new[] { "史", "傳", "誌", "錄", "譜" },
        ["Liturgical"] = new[] { "儀軌", "法事", "懺", "儀", "課誦" }
    };

    private static readonly Dictionary<string, string[]> PeriodKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Pre-Tang"] = new[] { "漢", "魏", "晉", "南北朝", "劉宋", "南齊", "梁", "陳", "北魏", "北齊", "北周", "隋" },
        ["Tang"] = new[] { "唐" },
        ["Song"] = new[] { "宋", "北宋", "南宋" },
        ["Yuan"] = new[] { "元" },
        ["Ming"] = new[] { "明" },
        ["Qing"] = new[] { "清" },
        ["Modern"] = new[] { "民國", "中華民國", "現代" },
        ["Contemporary"] = new[] { "當代" }
    };

    private static readonly Dictionary<string, string[]> OriginKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["India"] = new[] { "印度", "天竺", "中天竺", "北天竺", "南天竺", "西天" },
        ["Central Asia"] = new[] { "西域", "中亞", "龜茲", "于闐", "高昌" },
        ["China"] = new[] { "中國", "漢地", "中土", "大唐", "大宋", "大元", "大明", "大清" },
        ["Korea"] = new[] { "高麗", "新羅", "百濟", "朝鮮" },
        ["Japan"] = new[] { "日本", "倭" },
        ["Southeast Asia"] = new[] { "南海", "扶南", "真臘", "林邑" }
    };

    public IReadOnlyDictionary<string, BuddhistMetadataRecord> LoadByRelPath(string root)
    {
        var path = ResolveMetadataPath(root);
        if (!File.Exists(path))
            return new Dictionary<string, BuddhistMetadataRecord>(StringComparer.OrdinalIgnoreCase);

        using var doc = JsonDocument.Parse(File.ReadAllText(path));

        if (!doc.RootElement.TryGetProperty("detailed_analysis", out var detailed) ||
            detailed.ValueKind != JsonValueKind.Array)
        {
            return new Dictionary<string, BuddhistMetadataRecord>(StringComparer.OrdinalIgnoreCase);
        }

        var map = new Dictionary<string, BuddhistMetadataRecord>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in detailed.EnumerateArray())
        {
            var abs = item.TryGetProperty("file", out var fileEl) ? fileEl.GetString() ?? "" : "";
            var rel = NormalizeRelFromXmlP5(abs);
            if (string.IsNullOrWhiteSpace(rel))
                continue;

            var traditions = new List<string>();
            if (item.TryGetProperty("traditions", out var trEl) && trEl.ValueKind == JsonValueKind.Array)
            {
                traditions = trEl.EnumerateArray()
                    .Select(x => x.GetString())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            if (traditions.Count == 0)
                traditions.Add(UnknownTradition);

            map[rel] = new BuddhistMetadataRecord
            {
                RelPath = rel,
                CanonCode = ReadStringOrDefault(item, "canon", "Unknown"),
                Traditions = traditions,
                Period = ReadStringOrDefault(item, "period", "Unknown Period"),
                Origin = ReadStringOrDefault(item, "origin", "Unknown Origin")
            };
        }

        return map;
    }

    public BuddhistMetadataRecord InferFromXml(string relPath, string absPath)
    {
        var inferenceText = ExtractInferenceText(absPath);
        var traditions = DetectTraditions(inferenceText);
        if (traditions.Count == 0)
            traditions.Add(UnknownTradition);

        return new BuddhistMetadataRecord
        {
            RelPath = relPath,
            CanonCode = ExtractCanonCode(relPath),
            Traditions = traditions,
            Period = DetectByKeywords(inferenceText, PeriodKeywords, "Unknown Period"),
            Origin = DetectByKeywords(inferenceText, OriginKeywords, "Unknown Origin")
        };
    }

    private static string ResolveMetadataPath(string root)
    {
        var candidates = new List<string>();

        candidates.Add(Path.Combine(root, MetadataFolderName, MetadataFileName));

        var parent = Directory.GetParent(root)?.FullName;
        if (!string.IsNullOrWhiteSpace(parent))
            candidates.Add(Path.Combine(parent, MetadataFolderName, MetadataFileName));

        var grandParent = Directory.GetParent(parent ?? string.Empty)?.FullName;
        if (!string.IsNullOrWhiteSpace(grandParent))
            candidates.Add(Path.Combine(grandParent, MetadataFolderName, MetadataFileName));

        candidates.Add(Path.Combine(AppContext.BaseDirectory, MetadataFolderName, MetadataFileName));

        foreach (var candidate in candidates)
        {
            try
            {
                var full = Path.GetFullPath(candidate);
                if (File.Exists(full))
                    return full;
            }
            catch
            {
                // ignore invalid path entries
            }
        }

        return Path.Combine(root, MetadataFolderName, MetadataFileName);
    }

    private static string ExtractInferenceText(string absPath)
    {
        try
        {
            var doc = XDocument.Load(absPath);
            var titles = doc.Descendants(TeiNs + "title").Select(x => x.Value);
            var author = doc.Descendants(TeiNs + "author").Select(x => x.Value).FirstOrDefault() ?? "";
            var bibl = doc.Descendants(TeiNs + "bibl").Select(x => x.Value).FirstOrDefault() ?? "";
            return string.Join(" ", titles.Append(author).Append(bibl));
        }
        catch
        {
            try
            {
                var fallback = File.ReadAllText(absPath);
                return fallback.Length > 12000 ? fallback[..12000] : fallback;
            }
            catch
            {
                return "";
            }
        }
    }

    private static List<string> DetectTraditions(string text)
    {
        var found = new List<string>();
        foreach (var pair in TraditionKeywords)
        {
            if (pair.Value.Any(k => text.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0))
                found.Add(pair.Key);
        }

        return found.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string DetectByKeywords(
        string text,
        IReadOnlyDictionary<string, string[]> dictionary,
        string fallback)
    {
        foreach (var pair in dictionary)
        {
            if (pair.Value.Any(k => text.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0))
                return pair.Key;
        }

        return fallback;
    }

    private static string ExtractCanonCode(string relPath)
    {
        if (string.IsNullOrWhiteSpace(relPath))
            return "Unknown";

        var first = relPath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.IsNullOrWhiteSpace(first) ? "Unknown" : first;
    }

    private static string ReadStringOrDefault(JsonElement obj, string propName, string fallback)
    {
        if (!obj.TryGetProperty(propName, out var el))
            return fallback;

        var s = el.GetString();
        return string.IsNullOrWhiteSpace(s) ? fallback : s.Trim();
    }

    private static string NormalizeRelFromXmlP5(string abs)
    {
        if (string.IsNullOrWhiteSpace(abs))
            return "";

        var p = abs.Replace('\\', '/');
        const string marker = "/xml-p5/";

        var idx = p.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
            return p[(idx + marker.Length)..].TrimStart('/');

        // fallback if upstream format changes unexpectedly
        var parts = p.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3)
            return string.Join('/', parts.Skip(parts.Length - 3));

        return p;
    }
}
