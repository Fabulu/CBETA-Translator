using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface ISelectionSyncService
{
    bool TryGetDestinationSegment(RenderedDocument source, RenderedDocument destination, int sourceCaretIndex, out RenderSegment destinationSegment);
}
