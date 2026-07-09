namespace ReadZen.App.Services;

public interface IIndexedTranslationService
{
    string LastBuildTranslatedXmlDebugDump { get; }
    string LastBuildTranslatedXmlDebugDumpPath { get; }

    /// <summary>
    /// Count of dirty element groups the last <see cref="BuildTranslatedXml"/> refused to
    /// patch because they could not be round-tripped safely (their source XML was preserved
    /// verbatim and their edits were NOT persisted). Non-zero means the save was partial and
    /// the user must be warned.
    /// </summary>
    int LastBuildSkippedUnsafeGroupCount { get; }

    /// <summary>
    /// Total dirty element groups the last <c>BuildTranslatedXml</c> did NOT write back for any
    /// reason (unsafe/PreventsPatch + target-missing + target-mismatch). Superset of
    /// <see cref="LastBuildSkippedUnsafeGroupCount"/>; every one dropped a user edit, so callers
    /// surface it as a save warning (round-2 review finding 4).
    /// </summary>
    int LastBuildSkippedDirtyGroupCount { get; }

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
