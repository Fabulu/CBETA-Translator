// Services/SelectionSyncService.cs
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public sealed class SelectionSyncService : ISelectionSyncService
{
    public bool TryGetDestinationSegment(RenderedDocument source, RenderedDocument destination, int sourceCaretIndex, out RenderSegment destinationSegment)
    {
        destinationSegment = default;

        if (source.IsEmpty || destination.IsEmpty)
            return false;

        var seg = source.FindSegmentAtOrBefore(sourceCaretIndex);
        if (seg is null)
            return false;

        if (destination.TryGetSegmentByKey(seg.Value.Key, out destinationSegment))
            return true;

        // Fallback: map source display offset -> source xml index -> destination display offset.
        var xmlIndex = source.DisplayIndexToXmlIndex(sourceCaretIndex);
        if (xmlIndex < 0)
            return false;

        if (!destination.TryFindRenderedOffsetByXmlIndex(xmlIndex, out var dstOffset))
            return false;

        var fallback = destination.FindSegmentAtOrBefore(dstOffset);
        if (fallback is null)
            return false;

        destinationSegment = fallback.Value;
        return true;
    }
}
