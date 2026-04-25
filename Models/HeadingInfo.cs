namespace ReadZen.App.Models;

/// <summary>
/// Represents a heading extracted from TEI &lt;head&gt; elements during rendering.
/// Used to build a document outline / table of contents.
/// </summary>
public readonly record struct HeadingInfo(string Text, int RenderedOffset, int Level);
