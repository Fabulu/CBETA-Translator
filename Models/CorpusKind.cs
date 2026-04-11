// Models/CorpusKind.cs
// Identifies which corpus a text root belongs to. Used to color-code the top-bar
// badge, drive license defaults, and keep CBETA (non-commercial) and OpenZenTexts
// (commercial-OK) content legally segregated.
namespace ReadZen.App.Models;

public enum CorpusKind
{
    Unknown = 0,
    Cbeta = 1,
    Open = 2
}
