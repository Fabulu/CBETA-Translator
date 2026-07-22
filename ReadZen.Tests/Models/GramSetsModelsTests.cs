using System.Text.Json;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Models;

/// <summary>
/// Contract tests for the gramsets sidecar manifest DTOs <see cref="GramSetsManifest"/> and
/// <see cref="GramSetsEntry"/> (Models/GramSetsModels.cs, +63 lines this cycle for the
/// inverted-alphabet-only rewrite).
///
/// Two contracts the sidecar loader (<c>GramSetsStore.TryLoadAsync</c>) leans on, tested here on
/// the model in ISOLATION from the .bin format that GramSetsStoreTests covers:
///
///  (1) FRESH-INSTANCE DEFAULTS. The loader accepts a manifest only when <c>Version == 1</c> AND
///      <c>BuildGuid</c> equals the current build guid. A never-populated manifest must therefore
///      default to <c>Version = 1</c> (so a fresh save is loadable) but <c>BuildGuid = ""</c>
///      (so it can never accidentally validate against a real guid), and <c>Entries</c> must be a
///      non-null empty list so producers can Add without a null-guard.
///
///  (2) JSON ROUND-TRIP. The manifest persists as JSON via System.Text.Json. Every field —
///      including the ones added this cycle (RootPath, IndexStamp, BinPairingToken on the manifest;
///      ContentHash, InvOffset, InvCount on the entry) — must survive a serialize→deserialize cycle.
///      A property that silently loses its public getter/setter would corrupt the sidecar without
///      any compile error; this pins it.
/// </summary>
[Trait("Domain", "SearchSprint")]
public sealed class GramSetsModelsTests
{
    // ------------------------------------------------------------------ (1) defaults

    [Fact]
    public void FreshManifest_HasLoaderCompatibleDefaults()
    {
        var m = new GramSetsManifest();

        Assert.Equal(1, m.Version);            // loader requires Version == 1
        Assert.Equal("", m.BuildGuid);         // must NOT accidentally match a real build guid
        Assert.NotNull(m.Entries);             // producers Add without a null-check
        Assert.Empty(m.Entries);
        Assert.Null(m.RootPath);
        Assert.Null(m.IndexStamp);
        Assert.Null(m.BinPairingToken);
    }

    [Fact]
    public void FreshManifest_DefaultBuildGuid_NeverMatchesCurrentStoreGuid()
    {
        // The whole reason BuildGuid defaults to "" rather than the current guid: an
        // uninitialized manifest must be REJECTED by the loader, never silently accepted.
        Assert.NotEqual(GramSetsStore.BuildGuid, new GramSetsManifest().BuildGuid);
    }

    [Fact]
    public void FreshEntry_HasZeroDefaults_AndOriginalSide()
    {
        var e = new GramSetsEntry();

        Assert.Equal("", e.RelPath);
        Assert.Equal(SearchSide.Original, e.Side); // enum default is 0 == Original — the (RelPath, Side) key's zero value
        Assert.Null(e.ContentHash);
        Assert.Equal(0L, e.LastWriteUtcTicks);
        Assert.Equal(0L, e.LengthBytes);
        Assert.Equal(0L, e.InvOffset);
        Assert.Equal(0, e.InvCount);
    }

    // --------------------------------------------------------------- (2) JSON round-trip

    [Fact]
    public void Manifest_JsonRoundTrip_PreservesEveryField()
    {
        var original = new GramSetsManifest
        {
            Version = 1,
            BuildGuid = GramSetsStore.BuildGuid,
            RootPath = @"C:\corpus\xml-p5",
            IndexStamp = "stamp-xyz",
            BinPairingToken = "0123456789ABCDEF0123456789ABCDEF",
            Entries =
            {
                new GramSetsEntry
                {
                    RelPath = "orig/T47/T47n2005.xml",
                    Side = SearchSide.Translated,
                    ContentHash = new string('a', 64),
                    LastWriteUtcTicks = 638_000_000_000_000_000L,
                    LengthBytes = 12_345L,
                    InvOffset = 20L,
                    InvCount = 7,
                },
                new GramSetsEntry
                {
                    RelPath = "orig/X99/X99n0001.xml",
                    Side = SearchSide.Original,
                    ContentHash = null, // legacy row — must survive as null
                    LastWriteUtcTicks = 111L,
                    LengthBytes = 222L,
                    InvOffset = 48L,
                    InvCount = 0,
                },
            },
        };

        var json = JsonSerializer.Serialize(original);
        var back = JsonSerializer.Deserialize<GramSetsManifest>(json);

        Assert.NotNull(back);
        Assert.Equal(original.Version, back!.Version);
        Assert.Equal(original.BuildGuid, back.BuildGuid);
        Assert.Equal(original.RootPath, back.RootPath);
        Assert.Equal(original.IndexStamp, back.IndexStamp);
        Assert.Equal(original.BinPairingToken, back.BinPairingToken);
        Assert.Equal(original.Entries.Count, back.Entries.Count);

        for (int i = 0; i < original.Entries.Count; i++)
        {
            var a = original.Entries[i];
            var b = back.Entries[i];
            Assert.Equal(a.RelPath, b.RelPath);
            Assert.Equal(a.Side, b.Side);
            Assert.Equal(a.ContentHash, b.ContentHash);
            Assert.Equal(a.LastWriteUtcTicks, b.LastWriteUtcTicks);
            Assert.Equal(a.LengthBytes, b.LengthBytes);
            Assert.Equal(a.InvOffset, b.InvOffset);
            Assert.Equal(a.InvCount, b.InvCount);
        }
    }

    [Fact]
    public void Entry_Side_SerializesAsNumericEnumValue()
    {
        // System.Text.Json's default is numeric enums; the sidecar depends on that for
        // Original=0 / Translated=1 to stay stable across saves.
        var json = JsonSerializer.Serialize(new GramSetsEntry { Side = SearchSide.Translated });
        Assert.Contains("\"Side\":1", json);
    }
}
