// ReadableResumeRestoreTests — the pure gate for R3.1's single-owner resume restore:
// it must keep waiting until the panes are loaded, no hard gate is up, and the persisted
// merged-flow re-apply has settled, then restore exactly once.

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
            gated: false, docsLoaded: false, persistedMerged: false, isReadingLayout: false));
    }

    [Fact]
    public void Waits_WhenGated()
    {
        Assert.True(ReadableTabView.ResumeRestoreShouldWait(
            gated: true, docsLoaded: true, persistedMerged: false, isReadingLayout: false));
    }

    [Fact]
    public void Waits_WhenMergedReapplyNotYetSettled()
    {
        // Persisted merged flow, but the pane is still page layout → the sticky re-apply
        // has not finished; restoring now would be clobbered by its re-render.
        Assert.True(ReadableTabView.ResumeRestoreShouldWait(
            gated: false, docsLoaded: true, persistedMerged: true, isReadingLayout: false));
    }

    [Fact]
    public void Restores_WhenMergedReapplySettled()
    {
        Assert.False(ReadableTabView.ResumeRestoreShouldWait(
            gated: false, docsLoaded: true, persistedMerged: true, isReadingLayout: true));
    }

    [Fact]
    public void Restores_ForPlainPageLayout()
    {
        Assert.False(ReadableTabView.ResumeRestoreShouldWait(
            gated: false, docsLoaded: true, persistedMerged: false, isReadingLayout: false));
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
