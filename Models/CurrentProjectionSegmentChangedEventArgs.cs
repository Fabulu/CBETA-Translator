using System;
using CbetaTranslator.App.Services;

namespace CbetaTranslator.App.Models;

/// <summary>
/// Carries information about the currently selected projection segment in the translation editor.
/// Shared between Views and ViewModels.
/// </summary>
public sealed class CurrentProjectionSegmentChangedEventArgs : EventArgs
{
    public int BlockNumber { get; init; }
    public string Zh { get; init; } = "";
    public string En { get; init; } = "";
    /// <summary>Previous-tail + current + next-head ZH context (for TM/search cross-tag matching).</summary>
    public string ZhContext { get; init; } = "";
    public int BlockStartOffset { get; init; }
    public int BlockEndOffsetExclusive { get; init; }
    public TranslationEditMode Mode { get; init; }
}
