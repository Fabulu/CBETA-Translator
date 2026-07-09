using System;
using System.Linq;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Reproduction tests for three data-loss defects found by the write-back audit, all in
/// <see cref="IndexedTranslationService"/>. Every test drives the REAL projection pipeline
/// end-to-end: BuildIndex -> RenderProjection -> (simulate a projection edit) ->
/// ApplyProjectionEdits -> BuildTranslatedXml, then asserts on the rebuilt TEI string.
///
/// F1 (HIGH, silent loss of edits): editing a Body paragraph that contains a visible inline
///     element (&lt;hi&gt;/&lt;term&gt;/&lt;foreign&gt;/...) is silently discarded — the group is
///     flagged "unsafe" and skipped, updatedCount stays 0, and the file is rewritten WITHOUT
///     the edit.
/// F2 (HIGH, markup loss): an inline wrapper containing a descendant &lt;lb/&gt; is dropped on
///     round-trip — the wrapper element and its attributes vanish from the output.
/// F3 (MED, attribution corruption): a mid-line &lt;pb/&gt; / &lt;milestone/&gt; sitting between two
///     text runs is relocated to the END of the run on patch, shifting the page/line boundary
///     the citation layer depends on.
///
/// These tests originally reproduced the defects and now pin the FIXED behavior: F1 and F3 are
/// real round-trips (the edit is written / the page break stays put), while F2 is the sanctioned
/// safe-minimum (fail-loud + structure-preserving). The round-2 review-follow-up tests below
/// pin the residual fixes (nested-note preservation, mismatch/missing surfacing, and the
/// service-side guarantee the editor relies on to keep unwritten edits).
/// </summary>
public class PipelineDataLossTests
{
    private const string Open = "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body>";
    private const string Close = "</body></text></TEI>";

    private static string Wrap(string body) => Open + body + Close;

    /// <summary>
    /// Replace the EN line of the block whose ZH equals <paramref name="zh"/>, leaving every
    /// other line (comments, ZH lines, other EN lines) untouched — mirroring a user editing a
    /// single EN line in the projection editor.
    /// </summary>
    private static string SetEn(string projection, string zh, string en)
    {
        var lines = projection.Replace("\r\n", "\n").Split('\n');
        bool replaced = false;
        for (int i = 0; i < lines.Length - 1; i++)
        {
            if (lines[i] == "ZH: " + zh && lines[i + 1].StartsWith("EN:", StringComparison.Ordinal))
            {
                lines[i + 1] = "EN: " + en;
                replaced = true;
                break;
            }
        }
        Assert.True(replaced, $"Test setup: no projection block with ZH '{zh}' was found.");
        return string.Join("\n", lines);
    }

    // ---------------------------------------------------------------
    // F1 — visible inline element (<hi>) => whole edit silently dropped
    // ---------------------------------------------------------------
    [Fact]
    public void F1_ParagraphWithHi_UserTranslation_MustSurviveSave()
    {
        var svc = new IndexedTranslationService();
        var xml = Wrap("<p>眾生<hi rend=\"bold\">皆</hi>有佛性</p>");

        var doc = svc.BuildIndex(xml, xml);
        var projection = svc.RenderProjection(doc, TranslationEditMode.Body);

        const string userEn = "All beings possess Buddha-nature";
        var edited = SetEn(projection, "眾生皆有佛性", userEn);

        svc.ApplyProjectionEdits(doc, TranslationEditMode.Body, edited);

        // The edit was accepted into the index (unit is dirty) BEFORE the save runs. (After a
        // successful save the flag is correctly cleared, so this must be checked here.)
        Assert.True(doc.Units.Any(u => u.Kind == TranslationUnitKind.Body && u.IsDirty),
            "Projection edit should have marked the Body unit dirty.");

        var rebuilt = svc.BuildTranslatedXml(doc, out var updated);

        // The save MUST persist the edit. The old bug flagged the group (a visible <hi>)
        // "unsafe" and skipped it: updatedCount was 0 and the rebuilt XML held only the
        // Chinese source. The fix drops the subsumed inline wrapper and writes the translation.
        Assert.True(updated >= 1,
            $"Expected the edited paragraph to be written back (updatedCount>=1) but it was {updated}.");
        Assert.Contains(userEn, rebuilt);
    }

    // ---------------------------------------------------------------
    // F2 — inline wrapper with descendant <lb/> => SAFE-MINIMUM behavior
    //
    // A real round-trip here would require re-wrapping text that the descendant <lb/> split
    // across two projection lines back inside a single <hi> — a change the flat, line-based
    // projection model cannot make without a large, risky rewrite. Per the fix brief, the
    // accepted MINIMUM is therefore fail-loud + structure-preserving rather than a real
    // round-trip: the group is flagged unsafe and SKIPPED, so
    //   (a) the original <hi rend="bold">...</hi> wrapper is preserved verbatim (no silent
    //       markup loss — this was the actual data-loss defect), and
    //   (b) the edit is NOT written, the group is counted as skipped-unsafe, and the unit
    //       stays dirty, so the loss is surfaced loudly instead of silently swallowed.
    // ---------------------------------------------------------------
    [Fact]
    public void F2_HiWrapperContainingLb_SafeMinimum_PreservesStructureAndFailsLoud()
    {
        var svc = new IndexedTranslationService();
        var xml = Wrap("<p><hi rend=\"bold\">前半<lb n=\"0384a02\"/>後半</hi></p>");

        var doc = svc.BuildIndex(xml, xml);
        var projection = svc.RenderProjection(doc, TranslationEditMode.Body);

        // Edit the first line to make the paragraph group dirty.
        var edited = SetEn(projection, "前半", "FIRST");

        svc.ApplyProjectionEdits(doc, TranslationEditMode.Body, edited);
        var rebuilt = svc.BuildTranslatedXml(doc, out var updated);

        // Structure preserved: the wrapper element and its attribute survive verbatim.
        Assert.Contains("<hi", rebuilt);
        Assert.Contains("rend=\"bold\"", rebuilt);
        Assert.Contains("前半", rebuilt);
        Assert.Contains("後半", rebuilt);
        Assert.Contains("<lb", rebuilt);

        // Fail-loud: the unsafe group was skipped, not silently applied+corrupted.
        Assert.Equal(0, updated);
        Assert.DoesNotContain("FIRST", rebuilt);
        Assert.Equal(1, svc.LastBuildSkippedUnsafeGroupCount);
        Assert.Contains("straddles a line break", svc.LastBuildTranslatedXmlDebugDump);

        // The edit is not "forgotten": the unit stays dirty so the app still shows unsaved
        // changes and the user is not told everything saved cleanly.
        Assert.True(doc.Units.Any(u => u.Kind == TranslationUnitKind.Body && u.IsDirty),
            "The skipped unit must remain dirty so the unwritten edit is surfaced, not silently dropped.");
    }

    // ---------------------------------------------------------------
    // F3 — mid-run <pb/> relocated to end of the text run on patch
    // ---------------------------------------------------------------
    [Fact]
    public void F3_MidLinePageBreak_MustStayBetweenTextRuns()
    {
        var svc = new IndexedTranslationService();
        // pb sits between 乙 and 丙 on line 1; a second line (戊己) lets us dirty the group.
        var xml = Wrap("<p>甲乙<pb ed=\"T\" n=\"0384b\"/>丙丁<lb n=\"0384b01\"/>戊己</p>");

        var doc = svc.BuildIndex(xml, xml);
        var projection = svc.RenderProjection(doc, TranslationEditMode.Body);

        var edited = SetEn(projection, "戊己", "SECOND");

        svc.ApplyProjectionEdits(doc, TranslationEditMode.Body, edited);
        var rebuilt = svc.BuildTranslatedXml(doc, out var updated);

        // Group was patched (edit applied to line 2).
        Assert.True(updated >= 1);
        Assert.Contains("SECOND", rebuilt);

        // The page break must remain BETWEEN 乙 and 丙 — i.e. 乙 ... <pb> ... 丙 in that order.
        // Today the whole line text is emitted first and the pb is appended AFTER it, producing
        // "甲乙丙丁<pb .../>", which shifts the page boundary and corrupts citation attribution.
        int posYi = rebuilt.IndexOf('乙');
        int posPb = rebuilt.IndexOf("n=\"0384b\"", StringComparison.Ordinal);
        int posBing = rebuilt.IndexOf('丙');

        Assert.True(posYi >= 0 && posPb >= 0 && posBing >= 0, "Expected 乙, pb and 丙 all present.");
        Assert.True(posYi < posPb && posPb < posBing,
            $"Page break drifted: expected 乙(<{posYi}) < pb(<{posPb}) < 丙(<{posBing}). " +
            "Mid-run <pb/> was relocated to the end of the text run.");
    }

    // ===============================================================
    // Round-2 review follow-up regressions
    // ===============================================================

    // ---------------------------------------------------------------
    // R2-F2b — translating a note must NOT delete a nested <note>
    //
    // The F1 fix relaxed the visible-preserved gate so a translated line drops subsumed inline
    // wrappers (<hi>/<term>/...). A visible <note> in Notes mode ALSO matched that gate, so
    // patching the outer note dropped the inner <note n="6b" resp="x"> — its element, its
    // attributes, and (since it is its own translation unit) any separate translation of it.
    // The fix excludes <note> from the droppable set: it is re-emitted like a hidden note.
    // ---------------------------------------------------------------
    [Fact]
    public void R2_NestedNote_TranslatingOuterNote_MustNotDeleteInnerNote()
    {
        var svc = new IndexedTranslationService();
        var xml = Wrap("<p>正文<note n=\"6\">甲<note n=\"6b\" resp=\"x\">乙</note>丙</note></p>");

        var doc = svc.BuildIndex(xml, xml);
        var projection = svc.RenderProjection(doc, TranslationEditMode.Notes);

        // Translate the outer note line (its visible ZH is the concatenation 甲乙丙).
        var edited = SetEn(projection, "甲乙丙", "NOTE-EN");
        svc.ApplyProjectionEdits(doc, TranslationEditMode.Notes, edited);

        var rebuilt = svc.BuildTranslatedXml(doc, out var updated);

        Assert.True(updated >= 1, "The edited note should have been written back.");
        Assert.Contains("NOTE-EN", rebuilt);

        // The inner <note> element and its attributes must survive — no markup deletion.
        Assert.Contains("n=\"6b\"", rebuilt);
        Assert.Contains("resp=\"x\"", rebuilt);
        Assert.Contains("乙", rebuilt);
    }

    // ---------------------------------------------------------------
    // R2-F1b — a skipped-unsafe group must RETAIN the user's edited EN in the index.
    //
    // This is the service-side contract the editor relies on: after a save that skips an unsafe
    // group, the ViewModel keeps the current editor text (rather than re-rendering from disk)
    // ONLY because the dirty unit still carries the unwritten English. If BuildTranslatedXml
    // reverted the unit's En to baseline, the VM's "keep editor text" fix could not recover it.
    // ---------------------------------------------------------------
    [Fact]
    public void R2_SkippedUnsafeGroup_RetainsEditedEnAndStaysDirty()
    {
        var svc = new IndexedTranslationService();
        var xml = Wrap("<p><hi rend=\"bold\">前半<lb n=\"0384a02\"/>後半</hi></p>");

        var doc = svc.BuildIndex(xml, xml);
        var projection = svc.RenderProjection(doc, TranslationEditMode.Body);

        const string userEn = "FIRST-HALF-EN";
        var edited = SetEn(projection, "前半", userEn);
        svc.ApplyProjectionEdits(doc, TranslationEditMode.Body, edited);

        svc.BuildTranslatedXml(doc, out var updated);

        Assert.Equal(0, updated);
        // The unsafe skip is surfaced (both the unsafe-only and the total counters see it).
        Assert.Equal(1, svc.LastBuildSkippedUnsafeGroupCount);
        Assert.Equal(1, svc.LastBuildSkippedDirtyGroupCount);

        // Crucially, the edit is NOT reverted to baseline: the dirty unit still holds the user's
        // English so the editor can preserve it (VM keeps the projection text; does not re-render).
        var dirty = doc.Units.Where(u => u.Kind == TranslationUnitKind.Body && u.IsDirty).ToList();
        Assert.Single(dirty);
        Assert.Equal(userEn, dirty[0].En);
    }

    // ---------------------------------------------------------------
    // R2-F4 — a target-mismatch skip must be surfaced through the total counter.
    //
    // Before the fix only PreventsPatch (unsafe) skips fed the surfaced count, so a group whose
    // patch target drifted on disk was skipped with NO user-facing warning (silent-loss class).
    // LastBuildSkippedDirtyGroupCount now includes mismatch/missing skips, while
    // LastBuildSkippedUnsafeGroupCount stays the PreventsPatch-only subset.
    // ---------------------------------------------------------------
    [Fact]
    public void R2_TargetMismatchSkip_CountedInDirtyTotal_NotInUnsafe()
    {
        var svc = new IndexedTranslationService();
        var orig = Wrap("<p>甲乙丙丁</p>");

        var doc = svc.BuildIndex(orig, orig);
        var unit = doc.Units.Single(u => u.Kind == TranslationUnitKind.Body);
        unit.En = "MY-EDIT";
        unit.IsDirty = true;

        // The translated snapshot drifted (its p[1] is now a different passage), so the
        // write-back guard refuses to overwrite it: a target-mismatch skip.
        doc.TranslatedXml = Wrap("<p>子丑寅卯</p>");

        var rebuilt = svc.BuildTranslatedXml(doc, out var updated);

        Assert.Equal(0, updated);
        Assert.DoesNotContain("MY-EDIT", rebuilt);

        // Not an unsafe (PreventsPatch) skip...
        Assert.Equal(0, svc.LastBuildSkippedUnsafeGroupCount);
        // ...but still surfaced through the total so the user is warned instead of silence.
        Assert.Equal(1, svc.LastBuildSkippedDirtyGroupCount);

        // And the edit is retained (dirty) so it is not silently forgotten.
        Assert.True(doc.Units.Any(u => u.Kind == TranslationUnitKind.Body && u.IsDirty));
    }
}
