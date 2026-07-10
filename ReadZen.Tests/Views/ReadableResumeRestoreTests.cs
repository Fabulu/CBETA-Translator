// ReadableResumeRestoreTests — the pure gate for R3.1's single-owner resume restore:
// it must keep waiting until the panes are loaded, no hard gate is up, and the sticky
// reading-layout re-apply has SETTLED (achieved OR degraded), then restore exactly once.
// The "settled" signal generalises the old MergedFlow-only wait to all seven Wave-A modes
// (review finding 1: a stored SyncedPanes/Interleaved/etc. must also wait for its reapply,
// and a map-less doc under the new MergedFlow default must not hang for 1.6s).

using ReadZen.App.Views;
using Xunit;

namespace ReadZen.Tests.Views;

[Trait("Domain", "Reader")]
public class ReadableResumeRestoreTests
{
    [Fact]
    public void Waits_WhenDocsNotLoaded()
    {
        // Even with everything else clear, an unloaded pane must defer the restore.
        Assert.True(ReadableTabView.ResumeRestoreShouldWait(
            gated: false, docsLoaded: false, stickyReapplyResolved: true));
    }

    [Fact]
    public void Waits_WhenGated()
    {
        Assert.True(ReadableTabView.ResumeRestoreShouldWait(
            gated: true, docsLoaded: true, stickyReapplyResolved: true));
    }

    [Fact]
    public void Waits_WhenStickyReapplyNotYetSettled()
    {
        // A non-Page mode is persisted but its re-apply hasn't landed → restoring now
        // would be clobbered by the re-render. Holds for EVERY mode, not just MergedFlow.
        Assert.True(ReadableTabView.ResumeRestoreShouldWait(
            gated: false, docsLoaded: true, stickyReapplyResolved: false));
    }

    [Fact]
    public void Restores_WhenStickyReapplySettled()
    {
        // Settled = achieved the persisted mode OR degraded to page (no-map). Either way
        // the surface is final, so restore. This is the case that FIXES finding 1(b): a
        // map-less doc under the MergedFlow default degrades and settles here instead of
        // hanging out the full poll budget.
        Assert.False(ReadableTabView.ResumeRestoreShouldWait(
            gated: false, docsLoaded: true, stickyReapplyResolved: true));
    }

    // ===== Resume CAPTURE key guard (stale debounce across navigation) =====

    [Fact]
    public void Capture_Aborts_WhenNavigatedToAnotherFile()
    {
        // Debounce scheduled while file A was showing must not write once the reader
        // swapped to file B — the live key no longer matches the scheduled key.
        Assert.False(ReadableTabView.ResumeCaptureKeyStillValid("orig/A.xml", "orig/B.xml"));
    }

    [Fact]
    public void Capture_Writes_WhenStillOnScheduledFile()
    {
        Assert.True(ReadableTabView.ResumeCaptureKeyStillValid("orig/A.xml", "orig/A.xml"));
    }

    [Fact]
    public void Capture_Aborts_WhenKeyUnknown()
    {
        // A missing scheduled or live key is never a valid capture target.
        Assert.False(ReadableTabView.ResumeCaptureKeyStillValid(null, "orig/A.xml"));
        Assert.False(ReadableTabView.ResumeCaptureKeyStillValid("", "orig/A.xml"));
        Assert.False(ReadableTabView.ResumeCaptureKeyStillValid("orig/A.xml", null));
    }

    // ===== Resume RESTORE arm guard (readable render must have landed) =====

    [Fact]
    public void Restore_Skipped_WhenReadableRenderDidNotComplete()
    {
        // Render for the new file failed → SetRendered never bumped the gen → the panes
        // still hold the previous file's docs, so arming a restore must be skipped.
        Assert.False(ReadableTabView.ReadableRenderCompletedForNav(renderGen: 1, lastProvenanceRenderGen: 1));
    }

    [Fact]
    public void Restore_Armed_WhenReadableRenderCompleted()
    {
        // A successful SetRendered bumped the gen since the previous provenance.
        Assert.True(ReadableTabView.ReadableRenderCompletedForNav(renderGen: 2, lastProvenanceRenderGen: 1));
    }
}
