using System.Linq;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Regression tests for audit item P1.3 (RUN-20260702-2259 R2-M5): write-back resolved
/// its patch target by xml:id / positional node path and called ReplaceNodes without
/// checking that the element actually holds this group's content. A drifted translated
/// document or a target detached by an earlier group's rebuild was silently
/// overwritten / silently lost. The guard verifies the target's current visible text
/// against the group's EN baseline / ZH and skips (with a debug-dump entry) on mismatch.
/// </summary>
public class IndexedTranslationWriteBackGuardTests
{
    private const string TeiOpen =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\" xmlns:cb=\"http://www.cbeta.org/ns/1.0\">" +
        "<teiHeader><fileDesc><titleStmt><title>T</title></titleStmt></fileDesc></teiHeader>" +
        "<text><body>";

    private const string TeiClose = "</body></text></TEI>";

    private static string Wrap(string body) => TeiOpen + body + TeiClose;

    // ---------------------------------------------------------------
    // Happy paths: the guard must NOT block legitimate saves
    // ---------------------------------------------------------------

    [Fact]
    public void UntranslatedCopy_StillPatchesNormally()
    {
        var svc = new IndexedTranslationService();
        var orig = Wrap("<p>甲乙丙丁</p>");

        var doc = svc.BuildIndex(orig, orig);
        var unit = doc.Units.Single(u => u.Kind == TranslationUnitKind.Body);
        unit.En = "HELLO";
        unit.IsDirty = true;

        var result = svc.BuildTranslatedXml(doc, out var updated);

        Assert.Equal(1, updated);
        Assert.Contains("HELLO", result);
    }

    [Fact]
    public void PreviouslyTranslatedTarget_MatchingBaseline_StillPatches()
    {
        var svc = new IndexedTranslationService();
        var orig = Wrap("<p>甲乙丙丁</p>");

        // First save produces a translated document...
        var doc1 = svc.BuildIndex(orig, orig);
        var u1 = doc1.Units.Single(u => u.Kind == TranslationUnitKind.Body);
        u1.En = "HELLO";
        u1.IsDirty = true;
        var translated = svc.BuildTranslatedXml(doc1, out _);

        // ...and a second edit session against it must still be able to save.
        var doc2 = svc.BuildIndex(orig, translated);
        var u2 = doc2.Units.Single(u => u.Kind == TranslationUnitKind.Body);
        Assert.Equal("HELLO", u2.EnBaseline);
        u2.En = "WORLD";
        u2.IsDirty = true;

        var result = svc.BuildTranslatedXml(doc2, out var updated);

        Assert.Equal(1, updated);
        Assert.Contains("WORLD", result);
        Assert.DoesNotContain("HELLO", result);
    }

    // ---------------------------------------------------------------
    // R2-M5 core: drifted translated snapshot -> the positionally matched
    // element belongs to someone else and must not be overwritten
    // ---------------------------------------------------------------

    [Fact]
    public void DriftedSnapshot_ForeignElementIsNotOverwritten()
    {
        var svc = new IndexedTranslationService();
        var orig = Wrap("<p>甲乙丙丁</p>");

        var doc = svc.BuildIndex(orig, orig);
        var unit = doc.Units.Single(u => u.Kind == TranslationUnitKind.Body);
        unit.En = "MY-EDIT";
        unit.IsDirty = true;

        // The translated snapshot is refreshed (e.g. community sync pulled a newer
        // file) and its p[1] is now a DIFFERENT passage. The positional path still
        // matches it.
        doc.TranslatedXml = Wrap("<p>子丑寅卯</p>");

        var result = svc.BuildTranslatedXml(doc, out var updated);

        // Before the guard: updated == 1 and 子丑寅卯 was destroyed by MY-EDIT.
        Assert.Equal(0, updated);
        Assert.Contains("子丑寅卯", result);
        Assert.DoesNotContain("MY-EDIT", result);
        Assert.Contains("Target verification failed", svc.LastBuildTranslatedXmlDebugDump);
    }

    // ---------------------------------------------------------------
    // Detachment: an earlier group's ReplaceNodes rebuilds the parent and detaches
    // the child element a later group is about to patch
    // ---------------------------------------------------------------

    [Fact]
    public void NoteDetachedByParentRebuild_EditIsNotSilentlyLost()
    {
        var svc = new IndexedTranslationService();
        var orig = Wrap("<p>甲乙丙丁<note place=\"inline\">注文</note></p>");

        var doc = svc.BuildIndex(orig, orig);
        var bodyUnit = doc.Units.Single(u => u.Kind == TranslationUnitKind.Body);
        var noteUnit = doc.Units.Single(u => u.Kind == TranslationUnitKind.Note);

        bodyUnit.En = "BODY-EN";
        bodyUnit.IsDirty = true;
        noteUnit.En = "NOTE-EN";
        noteUnit.IsDirty = true;

        var result = svc.BuildTranslatedXml(doc, out var updated);

        // Before the guard both groups "patched" (updated == 2), but the note group's
        // target had been detached by the body rebuild, so NOTE-EN vanished from the
        // output while being counted as updated — a silent loss. Whatever the count,
        // every counted update must actually be in the output.
        if (result.Contains("NOTE-EN"))
        {
            Assert.Equal(2, updated);
        }
        else
        {
            Assert.Equal(1, updated);
            Assert.Contains("BODY-EN", result);
            // The skipped group must be loudly recorded, not silently dropped.
            Assert.Contains("Target verification failed", svc.LastBuildTranslatedXmlDebugDump);
        }
    }
}
