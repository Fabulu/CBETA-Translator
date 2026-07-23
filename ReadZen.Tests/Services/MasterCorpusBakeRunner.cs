using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;
using Xunit.Abstractions;

namespace ReadZen.Tests.Services;

/// <summary>
/// NOT a unit test — the headless BAKE driver that generates the committed shipping asset
/// <c>Assets/Data/master-corpus-index.json</c> for PR-M3 (SPEC §6.1). It builds the master
/// corpus index over the SHIPPED CBETA corpus ONLY (CbetaZenTexts + CbetaZenTranslations —
/// NOT OpenZen) using the committed 944-record roster (<c>Assets/Data/master-dates.json</c>),
/// so the baked composite v2 stamp equals what a fresh CBETA-only install computes live and
/// the bundle adopts with zero rebuild. Writes CACHE FORMAT (built_utc / corpus / corpus_stamp
/// / file_count / master_count / appearances), COMPACT JSON.
///
/// CBETA-only scoping is achieved by pointing the bake at an isolated parent directory that
/// contains ONLY junctions to the two CBETA repos, so <c>AppPaths.DiscoverAllCorpora</c> finds
/// exactly the Cbeta layout. Set up the junction parent BEFORE running (see the run-dir
/// generate script <c>tools/bake-master-corpus-asset.ps1</c>).
///
/// The committed roster half is authoritative for the CI lockstep guard (SPEC §6.1) — this
/// driver loads the SAME <c>Assets/Data/master-dates.json</c> the app loads at runtime, with
/// NO community overlay (bake machine has none ⇒ merged == base), so the asset stamp's roster
/// half == ComputeRosterIdentity(shipped roster).
///
/// Run explicitly (never in the normal test sweep — gated on env var so `dotnet test` skips it):
///   $env:READZEN_BAKE_PARENT = "C:\path\to\junction-parent-with-only-cbeta"
///   dotnet test --filter FullyQualifiedName~MasterCorpusBakeRunner
/// </summary>
public class MasterCorpusBakeRunner
{
    private readonly ITestOutputHelper _out;
    public MasterCorpusBakeRunner(ITestOutputHelper o) => _out = o;

    // Repo root (matches the MasterCorpusRebakeRunner precedent's absolute-path convention).
    private const string RepoRoot = @"C:\programmieren\MergeWorkCbeta\CBETA-Translator";

    [Fact]
    public async Task Bake_MasterCorpusAsset_CbetaOnly()
    {
        // The CBETA-only isolated parent (junctions to CbetaZenTexts + CbetaZenTranslations only).
        // Passed via env var so this driver never runs — or hard-codes a machine path — in a
        // normal `dotnet test`.
        var parentRoot = Environment.GetEnvironmentVariable("READZEN_BAKE_PARENT");
        if (string.IsNullOrWhiteSpace(parentRoot))
        {
            _out.WriteLine("READZEN_BAKE_PARENT not set — skipping bake driver.");
            return; // no-op when not explicitly driven
        }

        Assert.True(Directory.Exists(parentRoot), "bake parent missing: " + parentRoot);

        var baseFile = Path.Combine(RepoRoot, "Assets", "Data", "master-dates.json");
        Assert.True(File.Exists(baseFile), "committed roster missing: " + baseFile);

        var outPath = Path.Combine(RepoRoot, "Assets", "Data", "master-corpus-index.json");

        // Load the committed roster with NO community overlay (repoRoot = null ⇒ base only),
        // exactly the record set the shipped app resolves on a fresh install.
        var mgr = new ZenMasterManagerService(new MasterDatesService());
        var catalog = await mgr.LoadAsync(repoRoot: null, baseFilePath: baseFile);
        _out.WriteLine($"roster records: {catalog.Records.Count}");
        Assert.True(catalog.Records.Count > 0, "catalog empty");

        // Confirm CBETA-only discovery (guards against an accidental Cbeta+Open bake).
        var dirs = MasterCorpusSearchService.DiscoverCorpusDirs(parentRoot);
        _out.WriteLine("discovered corpora: " + string.Join(", ", dirs.ConvertAll(d => d.Label)));
        Assert.All(dirs, d => Assert.Equal("Cbeta", d.Label));

        var svc = new MasterCorpusSearchService();
        var index = await svc.BuildFullIndexAsync(parentRoot, catalog);

        _out.WriteLine($"BAKE corpus={index.Corpus} files={index.FileCount} " +
                       $"masters={index.MasterCount} appearances={index.Appearances.Count}");
        _out.WriteLine($"stamp={index.CorpusStamp}");

        Assert.Equal("Cbeta", index.Corpus);
        Assert.NotNull(index.CorpusStamp);
        Assert.StartsWith("v2;corpus=", index.CorpusStamp);
        Assert.True(index.MasterCount > 0, "no masters found — corpus root or catalog wrong");
        Assert.True(index.Appearances.Count > 0, "no appearances found");

        // Write CACHE FORMAT, COMPACT (SPEC §6.1: the deserializer is indifferent to indent;
        // compact keeps the committed asset ~57 MB rather than the app's indented ~66 MB).
        var compact = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        var tmp = outPath + ".tmp";
        var json = JsonSerializer.Serialize(index, compact);
        await File.WriteAllTextAsync(tmp, json, new UTF8Encoding(false));
        File.Move(tmp, outPath, overwrite: true);

        var size = new FileInfo(outPath).Length;
        _out.WriteLine($"wrote {outPath} ({size:N0} bytes)");

        // Round-trip sanity: the committed asset must deserialize as a MasterCorpusIndex and
        // its stamp must survive verbatim.
        var reread = JsonSerializer.Deserialize<MasterCorpusIndex>(
            await File.ReadAllTextAsync(outPath, Encoding.UTF8), compact);
        Assert.NotNull(reread);
        Assert.Equal(index.CorpusStamp, reread!.CorpusStamp);
        Assert.Equal(index.MasterCount, reread.MasterCount);
        Assert.Equal(index.Appearances.Count, reread.Appearances.Count);
    }
}
