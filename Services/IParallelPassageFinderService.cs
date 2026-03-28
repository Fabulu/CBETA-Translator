using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Services;

/// <summary>
/// Finds parallel passages in the corpus by searching for shared n-grams
/// with a given Chinese text.
/// </summary>
public interface IParallelPassageFinderService
{
    Task<List<ParallelPassageResult>> FindParallelsAsync(
        string zhText,
        string root,
        string originalDir,
        string translatedDir,
        CancellationToken ct = default);
}

public sealed class ParallelPassageResult
{
    public string RelPath { get; set; } = "";
    public string Snippet { get; set; } = "";
    public double OverlapScore { get; set; }
}
