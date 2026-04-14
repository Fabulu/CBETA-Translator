using System.Collections.Generic;

namespace ReadZen.App.Models;

/// <summary>
/// An N x N co-occurrence matrix for tags. Matrix[i,j] = number of files where
/// code i and code j both appear with overlapping lb-ranges.
/// </summary>
public sealed class CodeCooccurrenceMatrix
{
    public List<string> CodeIds { get; set; } = new();
    public List<string> CodeNames { get; set; } = new();
    public List<string> CodeColors { get; set; } = new();
    public int[,] Matrix { get; set; } = new int[0, 0];
}
