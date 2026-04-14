using System.Collections.Generic;

namespace ReadZen.App.Models;

/// <summary>Operator for combining two codes in a query.</summary>
public enum TagQueryOperator
{
    And,
    Or,
    Not
}

/// <summary>
/// A saved query that combines two codes with an operator.
/// </summary>
public sealed class TagQuery
{
    public string Name { get; set; } = "";
    public string CodeA { get; set; } = "";
    public string CodeB { get; set; } = "";
    public TagQueryOperator Operator { get; set; }
}

/// <summary>
/// A single match result from executing a tag query.
/// </summary>
public sealed record TagQueryMatch(
    string RelPath,
    string FromLb,
    string ToLb,
    List<string> MatchedTagIds);
