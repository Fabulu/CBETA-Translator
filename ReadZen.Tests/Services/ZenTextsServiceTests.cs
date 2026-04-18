using System.IO;
using System.Threading.Tasks;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Tests for ZenTextsService. The bug this guards against: when the user
/// switches between CBETA and OpenZen corpora, the in-memory _zen set must
/// be reloaded from the new corpus's zen_texts.json. Failing to do so makes
/// the "Zen only" filter return no results until the app is restarted
/// (reported against the post-RUN-20260416-2302 build).
/// </summary>
public class ZenTextsServiceTests
{
    /// <summary>
    /// Creates a clean temp directory + writes a minimal zen_texts.json
    /// listing the supplied relative paths.
    /// </summary>
    private static string MakeRoot(params string[] relPaths)
    {
        var dir = Path.Combine(Path.GetTempPath(), "zen_texts_test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        var json = "{\"Version\":1,\"UpdatedUtc\":\"2026-04-16T00:00:00Z\",\"Zen\":["
                 + string.Join(",", System.Array.ConvertAll(relPaths, p => "\"" + p + "\""))
                 + "]}";
        File.WriteAllText(Path.Combine(dir, "zen_texts.json"), json);
        return dir;
    }

    [Fact]
    public async Task LoadAsync_FromRoot_LoadsEntries()
    {
        var root = MakeRoot("T01/a.xml", "T02/b.xml");
        var svc = new ZenTextsService();

        await svc.LoadAsync(root);

        Assert.True(svc.IsZen("T01/a.xml"));
        Assert.True(svc.IsZen("T02/b.xml"));
        Assert.False(svc.IsZen("T03/c.xml"));
    }

    [Fact]
    public async Task LoadAsync_SecondTime_FromDifferentRoot_ReplacesSet()
    {
        // THIS IS THE CORPUS-SWITCH REGRESSION GUARD.
        //
        // Scenario: user starts in CBETA (5 zen entries loaded), switches to
        // OpenZen (empty file), switches back to CBETA. If LoadAsync isn't
        // called with the right root on every switch, the _zen HashSet
        // reflects the wrong corpus.
        //
        // Expected semantics: each LoadAsync fully replaces the in-memory
        // set with the content of the specified root's zen_texts.json.
        var cbetaRoot = MakeRoot("T01/a.xml", "T02/b.xml", "T03/c.xml");
        var openRoot = MakeRoot(); // empty — mirrors OpenZen's "all-zen-by-default" semantics

        var svc = new ZenTextsService();
        await svc.LoadAsync(cbetaRoot);
        Assert.True(svc.IsZen("T01/a.xml"));

        // Switch to OpenZen
        await svc.LoadAsync(openRoot);
        Assert.False(svc.IsZen("T01/a.xml"));
        Assert.False(svc.IsZen("T02/b.xml"));

        // Switch back to CBETA — the earlier entries must reappear. If the
        // app fails to call LoadAsync here (the pre-fix SwitchCorpusAsync
        // bug), IsZen would still return false for everything from the
        // last-loaded empty OpenZen root, and the "Zen only" filter would
        // hide every CBETA file.
        await svc.LoadAsync(cbetaRoot);
        Assert.True(svc.IsZen("T01/a.xml"));
        Assert.True(svc.IsZen("T02/b.xml"));
        Assert.True(svc.IsZen("T03/c.xml"));
    }

    [Fact]
    public async Task LoadAsync_MissingFile_CreatesEmptyWithNoException()
    {
        // First-run scenario for a corpus that's never had zen_texts.json
        // written. Must not throw; IsZen must return false for everything.
        var dir = Path.Combine(Path.GetTempPath(), "zen_texts_empty_" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);

        var svc = new ZenTextsService();
        await svc.LoadAsync(dir);

        Assert.False(svc.IsZen("any/path.xml"));
        Assert.True(File.Exists(Path.Combine(dir, "zen_texts.json")),
            "LoadAsync should have created an empty zen_texts.json");
    }

    [Fact]
    public async Task IsZen_NormalizesBackslashesToForwardSlashes()
    {
        // Windows-path regression guard: the stored key is forward-slash
        // normalized, so a Windows-style backslash lookup must still match.
        var root = MakeRoot("T01/a.xml");
        var svc = new ZenTextsService();
        await svc.LoadAsync(root);

        Assert.True(svc.IsZen(@"T01\a.xml"));
        Assert.True(svc.IsZen("T01/a.xml"));
    }

    [Fact]
    public async Task SetZenAsync_Persists_AcrossLoad()
    {
        var root = MakeRoot();
        var svc = new ZenTextsService();
        await svc.LoadAsync(root);

        await svc.SetZenAsync(root, "T05/new.xml", isZen: true);

        // Reload with a fresh service instance to confirm the file was written
        var svc2 = new ZenTextsService();
        await svc2.LoadAsync(root);
        Assert.True(svc2.IsZen("T05/new.xml"));
    }
}
