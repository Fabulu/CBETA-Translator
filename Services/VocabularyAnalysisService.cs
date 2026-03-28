using System;
using System.Collections.Generic;
using System.Linq;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public sealed class VocabularyItem
{
    public string Phrase { get; set; } = "";
    public int Count { get; set; }
    public int PassageCount { get; set; }
}

public static class VocabularyAnalysisService
{
    private static readonly HashSet<char> StopParticles = new()
    {
        '之', '乎', '者', '也', '矣', '焉', '而', '以', '為', '於',
        '其', '所', '則', '乃', '若', '如', '雖', '既', '且', '猶',
        '況', '豈', '蓋', '夫', '惟', '唯', '即', '遂', '竟', '但',
        '然', '哉', '不', '是', '有', '無', '此', '彼', '何', '云'
    };

    public static List<VocabularyItem> Analyze(
        IEnumerable<ScholarPassage> passages, int minNGram = 2, int maxNGram = 4)
    {
        var passageList = passages.ToList();
        if (passageList.Count == 0)
            return new List<VocabularyItem>();

        // Track global frequency and per-passage frequency
        var globalCount = new Dictionary<string, int>(StringComparer.Ordinal);
        var passageCount = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var passage in passageList)
        {
            var normalized = CjkMatchNormalizer.Normalize(passage.ZhText);
            if (string.IsNullOrEmpty(normalized))
                continue;

            // Track which n-grams appear in this passage (dedup within a passage)
            var seenInPassage = new HashSet<string>(StringComparer.Ordinal);

            for (int n = minNGram; n <= maxNGram; n++)
            {
                for (int i = 0; i + n <= normalized.Length; i++)
                {
                    var ngram = normalized.Substring(i, n);

                    if (IsAllStopParticles(ngram))
                        continue;

                    if (globalCount.TryGetValue(ngram, out var c))
                        globalCount[ngram] = c + 1;
                    else
                        globalCount[ngram] = 1;

                    if (seenInPassage.Add(ngram))
                    {
                        if (passageCount.TryGetValue(ngram, out var pc))
                            passageCount[ngram] = pc + 1;
                        else
                            passageCount[ngram] = 1;
                    }
                }
            }
        }

        // Filter out n-grams appearing only once, sort by count desc, take top 200
        var result = globalCount
            .Where(kv => kv.Value > 1)
            .OrderByDescending(kv => kv.Value)
            .ThenByDescending(kv => passageCount.GetValueOrDefault(kv.Key, 0))
            .Take(200)
            .Select(kv => new VocabularyItem
            {
                Phrase = kv.Key,
                Count = kv.Value,
                PassageCount = passageCount.GetValueOrDefault(kv.Key, 0)
            })
            .ToList();

        return result;
    }

    private static bool IsAllStopParticles(string s)
    {
        foreach (char c in s)
        {
            if (!StopParticles.Contains(c))
                return false;
        }
        return true;
    }
}
