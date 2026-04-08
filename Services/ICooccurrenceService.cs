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
        int topK = 30);
}
