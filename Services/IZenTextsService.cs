using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReadZen.App.Services;

public interface IZenTextsService
{
    Task LoadAsync(string root);
    bool IsZen(string relPath);
    Task SetZenAsync(string root, string relPath, bool isZen);

    /// <summary>Ordered rel-paths of the prescriptive Zen canon, as loaded (empty until LoadAsync).</summary>
    IReadOnlyList<string> Texts { get; }

    /// <summary>Human-readable version label of the loaded canon list, if the source declares one.</summary>
    string? ListVersion { get; }

    /// <summary>Provenance note baked into the canon list, if any.</summary>
    string? GeneratedNote { get; }
}
