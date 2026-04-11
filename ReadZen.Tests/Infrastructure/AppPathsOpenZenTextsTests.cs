// Tests that AppPaths recognizes the OpenZenTexts folder layout
// (xml-open / xml-open-t) alongside the original CBETA layout
// (xml-p5 / xml-p5t). Without this, opening an OpenZenTexts root
// would fail at ValidateBothReposExist with "Both originals and
// translations repos are required" — the entire license-display
// MVP would be unreachable for the corpus it was built to support.

using System;
using System.IO;
using ReadZen.App.Infrastructure;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

public class AppPathsOpenZenTextsTests
{
    [Fact]
    public void DiscoverRepoPaths_FindsOpenZenTextsLayout()
    {
        var root = MakeTempRoot();
        try
        {
            var originalsRepo = Path.Combine(root, "OpenZenTexts");
            var translationsRepo = Path.Combine(root, "OpenZenTranslations");
            Directory.CreateDirectory(Path.Combine(originalsRepo, "xml-open"));
            Directory.CreateDirectory(Path.Combine(translationsRepo, "xml-open-t"));

            AppPaths.InvalidateDiscoveryCache(root);
            var (orig, trans) = AppPaths.DiscoverRepoPaths(root);

            Assert.Equal(originalsRepo, orig);
            Assert.Equal(translationsRepo, trans);
            Assert.True(AppPaths.ValidateBothReposExist(root));
        }
        finally { Cleanup(root); }
    }

    [Fact]
    public void DiscoverRepoPaths_StillFindsCbetaLayout()
    {
        var root = MakeTempRoot();
        try
        {
            var originalsRepo = Path.Combine(root, "CbetaZenTexts");
            var translationsRepo = Path.Combine(root, "CbetaZenTranslations");
            Directory.CreateDirectory(Path.Combine(originalsRepo, "xml-p5"));
            Directory.CreateDirectory(Path.Combine(translationsRepo, "xml-p5t"));

            AppPaths.InvalidateDiscoveryCache(root);
            var (orig, trans) = AppPaths.DiscoverRepoPaths(root);

            Assert.Equal(originalsRepo, orig);
            Assert.Equal(translationsRepo, trans);
        }
        finally { Cleanup(root); }
    }

    [Fact]
    public void GetOriginalDir_ReturnsXmlOpen_ForOpenZenTextsRepo()
    {
        var root = MakeTempRoot();
        try
        {
            var originalsRepo = Path.Combine(root, "OpenZenTexts");
            Directory.CreateDirectory(Path.Combine(originalsRepo, "xml-open"));
            Directory.CreateDirectory(Path.Combine(root, "OpenZenTranslations", "xml-open-t"));

            AppPaths.InvalidateDiscoveryCache(root);
            var origDir = AppPaths.GetOriginalDir(root);

            Assert.Equal(Path.Combine(originalsRepo, "xml-open"), origDir);
        }
        finally { Cleanup(root); }
    }

    [Fact]
    public void GetTranslatedDir_ReturnsXmlOpenT_ForOpenZenTranslationsRepo()
    {
        var root = MakeTempRoot();
        try
        {
            var translationsRepo = Path.Combine(root, "OpenZenTranslations");
            Directory.CreateDirectory(Path.Combine(root, "OpenZenTexts", "xml-open"));
            Directory.CreateDirectory(Path.Combine(translationsRepo, "xml-open-t"));

            AppPaths.InvalidateDiscoveryCache(root);
            var transDir = AppPaths.GetTranslatedDir(root);

            Assert.Equal(Path.Combine(translationsRepo, "xml-open-t"), transDir);
        }
        finally { Cleanup(root); }
    }

    [Fact]
    public void GetTranslatedCacheDir_PairsCorrectlyForOpenLayout()
    {
        var root = MakeTempRoot();
        try
        {
            var translationsRepo = Path.Combine(root, "OpenZenTranslations");
            Directory.CreateDirectory(Path.Combine(root, "OpenZenTexts", "xml-open"));
            Directory.CreateDirectory(Path.Combine(translationsRepo, "xml-open-t"));

            AppPaths.InvalidateDiscoveryCache(root);
            var cacheDir = AppPaths.GetTranslatedCacheDir(root);

            Assert.Equal(Path.Combine(translationsRepo, "xml-open-t-cache"), cacheDir);
        }
        finally { Cleanup(root); }
    }

    [Fact]
    public void GetOriginalDir_StillReturnsXmlP5_ForCbetaRepo()
    {
        var root = MakeTempRoot();
        try
        {
            var originalsRepo = Path.Combine(root, "CbetaZenTexts");
            Directory.CreateDirectory(Path.Combine(originalsRepo, "xml-p5"));
            Directory.CreateDirectory(Path.Combine(root, "CbetaZenTranslations", "xml-p5t"));

            AppPaths.InvalidateDiscoveryCache(root);
            var origDir = AppPaths.GetOriginalDir(root);

            Assert.Equal(Path.Combine(originalsRepo, "xml-p5"), origDir);
        }
        finally { Cleanup(root); }
    }

    [Fact]
    public void DiscoverAllCorpora_FindsBothCbetaAndOpenAsSiblings()
    {
        var root = MakeTempRoot();
        try
        {
            // CBETA pair
            var cbetaOrig = Path.Combine(root, "CbetaZenTexts");
            var cbetaTrans = Path.Combine(root, "CbetaZenTranslations");
            Directory.CreateDirectory(Path.Combine(cbetaOrig, "xml-p5"));
            Directory.CreateDirectory(Path.Combine(cbetaTrans, "xml-p5t"));
            // Open pair
            var openOrig = Path.Combine(root, "OpenZenTexts");
            var openTrans = Path.Combine(root, "OpenZenTranslations");
            Directory.CreateDirectory(Path.Combine(openOrig, "xml-open"));
            Directory.CreateDirectory(Path.Combine(openTrans, "xml-open-t"));

            AppPaths.InvalidateDiscoveryCache(root);
            var corpora = AppPaths.DiscoverAllCorpora(root);

            Assert.Equal(2, corpora.Count);
            Assert.Contains(corpora, c => c.Kind == ReadZen.App.Models.CorpusKind.Cbeta);
            Assert.Contains(corpora, c => c.Kind == ReadZen.App.Models.CorpusKind.Open);

            var cbeta = corpora.First(c => c.Kind == ReadZen.App.Models.CorpusKind.Cbeta);
            Assert.Equal(cbetaOrig, cbeta.OriginalsRepoRoot);
            Assert.Equal(cbetaTrans, cbeta.TranslationsRepoRoot);
            Assert.Equal("xml-p5", cbeta.OriginalFolderName);
            Assert.Equal("xml-p5t", cbeta.TranslatedFolderName);

            var open = corpora.First(c => c.Kind == ReadZen.App.Models.CorpusKind.Open);
            Assert.Equal(openOrig, open.OriginalsRepoRoot);
            Assert.Equal(openTrans, open.TranslationsRepoRoot);
            Assert.Equal("xml-open", open.OriginalFolderName);
            Assert.Equal("xml-open-t", open.TranslatedFolderName);
        }
        finally { Cleanup(root); }
    }

    [Fact]
    public void DiscoverAllCorpora_OneCorpusOnly_ReturnsOne()
    {
        var root = MakeTempRoot();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "CbetaZenTexts", "xml-p5"));
            Directory.CreateDirectory(Path.Combine(root, "CbetaZenTranslations", "xml-p5t"));

            AppPaths.InvalidateDiscoveryCache(root);
            var corpora = AppPaths.DiscoverAllCorpora(root);

            Assert.Single(corpora);
            Assert.Equal(ReadZen.App.Models.CorpusKind.Cbeta, corpora[0].Kind);
        }
        finally { Cleanup(root); }
    }

    [Fact]
    public void DiscoverAllCorpora_EmptyRoot_ReturnsEmpty()
    {
        var root = MakeTempRoot();
        try
        {
            AppPaths.InvalidateDiscoveryCache(root);
            var corpora = AppPaths.DiscoverAllCorpora(root);
            Assert.Empty(corpora);
        }
        finally { Cleanup(root); }
    }

    [Fact]
    public void CorpusLayout_ExposesCombinedDirs()
    {
        var root = MakeTempRoot();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "OpenZenTexts", "xml-open"));
            Directory.CreateDirectory(Path.Combine(root, "OpenZenTranslations", "xml-open-t"));

            AppPaths.InvalidateDiscoveryCache(root);
            var corpora = AppPaths.DiscoverAllCorpora(root);
            var open = Assert.Single(corpora);

            Assert.Equal(Path.Combine(root, "OpenZenTexts", "xml-open"), open.OriginalDir);
            Assert.Equal(Path.Combine(root, "OpenZenTranslations", "xml-open-t"), open.TranslatedDir);
            Assert.Equal(Path.Combine(root, "OpenZenTranslations", "xml-open-t-cache"), open.TranslatedCacheDir);
        }
        finally { Cleanup(root); }
    }

    private static string MakeTempRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"appzpaths-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void Cleanup(string path)
    {
        try
        {
            AppPaths.InvalidateDiscoveryCache(path);
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
        catch { }
    }
}
