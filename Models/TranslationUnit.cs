using System.Collections.Generic;

public enum TranslationUnitKind
{
    Head,
    Body,
    Note
}

public sealed class TranslationUnit
{
    public int Index { get; set; }                // 1-based UI number
    public string StableKey { get; set; } = "";   // xml:id or node path
    public string NodePath { get; set; } = "";
    public string Zh { get; set; } = "";
    public string En { get; set; } = "";
    public TranslationUnitKind Kind { get; set; }

    // Used for merge-back
    public bool IsParagraph { get; set; }
    public string? XmlId { get; set; }

    // Optional preservation hooks
    public List<string> ExistingNoteXml { get; } = new(); // if you want to preserve/move notes
}