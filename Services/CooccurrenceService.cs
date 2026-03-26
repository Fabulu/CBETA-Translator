using System;
using System.Collections.Generic;
using System.Linq;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

/// <summary>
/// Computes character and ngram co-occurrence frequency metrics from search result groups.
/// Extracted from SearchIndexService during Wave 7 service split.
/// </summary>
public sealed class CooccurrenceService : ICooccurrenceService
{
    public SearchIndexService.CooccurrencePanelResult ComputeCooccurrences(
        IEnumerable<SearchResultGroup> groups,
        string query,
        int contextWidth,
        CoocMetric metric,
        int topK = 30)
    {
        // Delegate to the static implementation that was already on SearchIndexService.
        // This preserves exact behavior.
        return SearchIndexService.ComputeCooccurrences(groups, query, contextWidth, metric, topK);
    }
}
