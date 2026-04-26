using System.Collections.Generic;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Responsible for computing character/ngram co-occurrence frequency metrics
/// from search result groups.
/// </summary>
public interface ICooccurrenceService
{
    SearchIndexService.CooccurrencePanelResult ComputeCooccurrences(
        IEnumerable<SearchResultGroup> groups,
        string query,
        int contextWidth,
        CoocMetric metric,
        IReadOnlyDictionary<string, int>? corpusCharFreqs = null,
        IReadOnlyDictionary<string, int>? corpusBigramFreqs = null,
        long corpusTotalChars = 0,
        int topK = 30);
}
