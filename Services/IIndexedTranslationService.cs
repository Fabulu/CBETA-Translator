namespace ReadZen.App.Services;

public interface IIndexedTranslationService
{
    string LastBuildTranslatedXmlDebugDump { get; }
    string LastBuildTranslatedXmlDebugDumpPath { get; }

    IndexedTranslationDocument BuildIndex(string originalXml, string? translatedXml, string? originalAbsPath = null);
    string RenderProjection(IndexedTranslationDocument doc, TranslationEditMode mode);
    void ApplyProjectionEdits(IndexedTranslationDocument doc, TranslationEditMode mode, string editedText);
    string BuildTranslatedXml(IndexedTranslationDocument doc, out int updatedCount);
}
