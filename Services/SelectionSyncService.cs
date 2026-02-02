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

        return destination.TryGetSegmentByKey(seg.Value.Key, out destinationSegment);
    }
}
