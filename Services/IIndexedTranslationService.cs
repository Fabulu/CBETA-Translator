namespace ReadZen.App.Services;

public interface IIndexedTranslationService
{
    string LastBuildTranslatedXmlDebugDump { get; }
    string LastBuildTranslatedXmlDebugDumpPath { get; }

    IndexedTranslationDocument BuildIndex(string originalXml, string? translatedXml, string? originalAbsPath = null);
    string RenderProjection(IndexedTranslationDocument doc, TranslationEditMode mode);

    /// <summary>
    /// Read-only merged preview (audit P4.3b): groups consecutive translation units by
    /// the semantic segment their trailing lb belongs to (per the segment map) and
    /// shows the concatenated ZH with the concatenated EN below — complete thoughts
    /// instead of 17-character woodblock shards. Display only: nothing here feeds
    /// ApplyProjectionEdits, and save-back is unchanged.
    /// </summary>
    string RenderMergedPreview(IndexedTranslationDocument doc, TranslationEditMode mode, ReadZen.App.Models.SegmentMap segmentMap);

    void ApplyProjectionEdits(IndexedTranslationDocument doc, TranslationEditMode mode, string editedText);
    string BuildTranslatedXml(IndexedTranslationDocument doc, out int updatedCount);
}
