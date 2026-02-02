using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public interface ISelectionSyncService
{
    bool TryGetDestinationSegment(RenderedDocument source, RenderedDocument destination, int sourceCaretIndex, out RenderSegment destinationSegment);
}
