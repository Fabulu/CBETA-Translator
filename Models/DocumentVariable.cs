namespace ReadZen.App.Models;

/// <summary>
/// A user-defined variable associated with a document (file), stored in JSONL.
/// </summary>
public sealed class DocumentVariable
{
    public string RelPath { get; set; } = "";
    public string VariableName { get; set; } = "";
    public string VariableValue { get; set; } = "";
}
