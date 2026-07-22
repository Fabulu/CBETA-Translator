using System;
using System.IO;
using System.Threading.Tasks;
using ReadZen.App.Services;
using Xunit;
using Xunit.Abstractions;

namespace ReadZen.Tests.Services;

/// <summary>
/// NOT a unit test — a headless driver to RE-BAKE the master-corpus shards with the
/// display-scope count fix (MasterCorpusSearchService strips teiHeader/note/cb:mulu/rdg
/// before counting). It builds the index over the real corpus and writes the SPA-facing
/// shards + master-corpus.json into a SIDE folder. It NEVER touches CbetaZenTranslations.
/// Run explicitly:
///   dotnet test --filter FullyQualifiedName~MasterCorpusRebakeRunner
/// </summary>
public class MasterCorpusRebakeRunner
{
    private readonly ITestOutputHelper _out;
    public MasterCorpusRebakeRunner(ITestOutputHelper o) => _out = o;

    [Fact]
    public async Task Rebake_MasterCorpus_ToSideFolder()
    {
        const string parentRoot = @"C:\programmieren";
        // Use the LIVE 944-master roster the SPA ships, NOT the stale 301-master desktop asset.
        const string baseFile   = @"C:\programmieren\CbetaZenTranslations\masters.json";
        const string outDir     = @"C:\programmieren\MergeWorkCbeta\CBETA-Translator\runs\CLAUDE-RUNS\master-corpus-rebake-20260722\out";

        Assert.True(File.Exists(baseFile), "base master-dates.json missing: " + baseFile);

        // Clean side folder so no stale shard survives.
        if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        Directory.CreateDirectory(outDir);

        var mgr = new ZenMasterManagerService(new MasterDatesService());
        var catalog = await mgr.LoadAsync(parentRoot, baseFile);
        _out.WriteLine($"catalog records: {catalog.Records.Count}");
        Assert.True(catalog.Records.Count > 0, "catalog empty");

        var svc = new MasterCorpusSearchService();
        var index = await svc.BuildFullIndexAsync(parentRoot, catalog);
        _out.WriteLine($"REBAKE masters={index.MasterCount} files={index.FileCount} appearances={index.Appearances.Count}");

        await MasterCorpusSearchService.ExportMasterCorpusShardedAsync(outDir, index);
        await MasterCorpusSearchService.ExportMasterCorpusJsonAsync(outDir, index);

        Assert.True(index.MasterCount > 0, "no masters found — corpus root or catalog wrong");
        Assert.True(index.Appearances.Count > 0, "no appearances found");
        Assert.True(Directory.Exists(Path.Combine(outDir, "corpus", "masters")), "shards not written");
        _out.WriteLine($"wrote shards to: {Path.Combine(outDir, "corpus", "masters")}");
    }
}
