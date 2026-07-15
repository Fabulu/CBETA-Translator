using System.IO;
using System.Threading.Tasks;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Tests for ZenTextsService. Zen membership is now PRESCRIPTIVE: it comes from the
/// app-baked allowlist Assets/Data/zen-corpus.json (derived from ZEN_TEXT_WORKLIST.md),
/// not from a per-repo, user-editable zen_texts.json. These tests point the service at a
/// temp asset file via the constructor override.
/// </summary>
public class ZenTextsServiceTests
{
    /// <summary>Writes a temp zen-corpus.json asset listing the supplied relpaths; returns its path.</summary>
    private static string MakeAsset(params string[] relPaths)
    {
        var dir = Path.Combine(Path.GetTempPath(), "zencorpus_test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "zen-corpus.json");
        var json = "{\"version\":1,\"source\":\"test\",\"texts\":["
                 + string.Join(",", System.Array.ConvertAll(relPaths, p => "\"" + p + "\""))
                 + "]}";
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public async Task LoadAsync_LoadsPrescriptiveAllowlist()
    {
        var svc = new ZenTextsService(MakeAsset("T/T48/T48n2005.xml", "J/J24/J24nB137.xml"));

        await svc.LoadAsync(root: "anything-ignored");

        Assert.True(svc.IsZen("T/T48/T48n2005.xml"));
        Assert.True(svc.IsZen("J/J24/J24nB137.xml"));
        Assert.False(svc.IsZen("T/T47/T47n1960.xml")); // a Pure Land text — not Zen
    }

    [Fact]
    public async Task LoadAsync_RootArgumentIsIgnored_MembershipIsAppGlobal()
    {
        // The prescriptive list is the same regardless of which repo root is passed.
        var svc = new ZenTextsService(MakeAsset("T/T48/T48n2005.xml"));

        await svc.LoadAsync("C:/some/cbeta/root");
        Assert.True(svc.IsZen("T/T48/T48n2005.xml"));

        await svc.LoadAsync("C:/a/totally/different/root");
        Assert.True(svc.IsZen("T/T48/T48n2005.xml"));
    }

    [Fact]
    public async Task IsZen_NormalizesBackslashesToForwardSlashes()
    {
        var svc = new ZenTextsService(MakeAsset("T/T48/T48n2005.xml"));
        await svc.LoadAsync("");

        Assert.True(svc.IsZen(@"T\T48\T48n2005.xml"));
        Assert.True(svc.IsZen("T/T48/T48n2005.xml"));
    }

    [Fact]
    public async Task SetZenAsync_IsNoOp_CannotAddOrRemove()
    {
        var svc = new ZenTextsService(MakeAsset("T/T48/T48n2005.xml"));
        await svc.LoadAsync("");

        // Attempting to "add" a non-Zen text must NOT make it Zen (prescriptive, not editable).
        await svc.SetZenAsync("root", "T/T47/T47n1960.xml", isZen: true);
        Assert.False(svc.IsZen("T/T47/T47n1960.xml"));

        // Attempting to "remove" a Zen text must NOT change it.
        await svc.SetZenAsync("root", "T/T48/T48n2005.xml", isZen: false);
        Assert.True(svc.IsZen("T/T48/T48n2005.xml"));
    }

    [Fact]
    public async Task LoadAsync_MissingAsset_NoThrow_EverythingFalse()
    {
        var missing = Path.Combine(Path.GetTempPath(), "zencorpus_missing_" + Path.GetRandomFileName(), "zen-corpus.json");
        var svc = new ZenTextsService(missing);

        await svc.LoadAsync(""); // must not throw

        Assert.False(svc.IsZen("T/T48/T48n2005.xml"));
    }
}
