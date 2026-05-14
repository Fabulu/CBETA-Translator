// Services/ISegmentMapService.cs
// Interface for loading and caching segment-map JSONL files.

using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface ISegmentMapService
{
    /// <summary>
    /// Attempts to load the segment map for the given XML file.
    /// Discovers the sibling JSONL by path convention: the XML path's
    /// "xml-p5" component is replaced with "segments" and ".xml" with
    /// ".segments.jsonl", rooted under the translations repo.
    /// Returns null when no JSONL exists or parsing fails (graceful degradation).
    /// Results are mtime-cached: repeated calls for the same file return
    /// the cached map unless the JSONL's last-write-time has changed.
    /// </summary>
    SegmentMap? TryLoad(string xmlAbsPath);
}
