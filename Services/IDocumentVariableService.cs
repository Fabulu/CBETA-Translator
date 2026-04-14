using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Loads and saves document-level variables (metadata) from JSONL files,
/// and cross-tabulates them with tag data.
/// </summary>
public interface IDocumentVariableService
{
    Task<List<DocumentVariable>> LoadAsync(string root, CancellationToken ct = default);
    Task SaveAsync(string root, List<DocumentVariable> vars, CancellationToken ct = default);

    /// <summary>
    /// Cross-tabulates tags with a named variable: groups files by variable value,
    /// counts tag segments per group per code.
    /// Returns a dictionary: variableValue -> (tagName -> count).
    /// </summary>
    Dictionary<string, Dictionary<string, int>> CrossTabulate(
        List<DocumentTag> tags,
        List<DocumentVariable> vars,
        TagVocabulary vocab,
        string variableName);
}
