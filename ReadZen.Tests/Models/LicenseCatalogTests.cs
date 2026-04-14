using ReadZen.App.Models;
using Xunit;

namespace ReadZen.Tests.Models;

public class LicenseCatalogTests
{
    // ── GetCompatible: CBETA hard lock ──────────────────────────────

    [Fact]
    public void GetCompatible_Cbeta_ReturnsOnlyNcOptions()
    {
        var result = LicenseCatalog.GetCompatible("anything", CorpusKind.Cbeta);
        Assert.All(result, opt => Assert.False(opt.CommercialOk));
        Assert.Contains(result, o => o.Id == "cbeta-nc");
    }

    [Fact]
    public void GetCompatible_Cbeta_NeverReturnsCommercialLicenses()
    {
        var result = LicenseCatalog.GetCompatible("CC0-1.0", CorpusKind.Cbeta);
        Assert.DoesNotContain(result, o => o.Id == "CC-BY-4.0");
        Assert.DoesNotContain(result, o => o.Id == "CC0-1.0");
        Assert.DoesNotContain(result, o => o.Id == "MIT");
    }

    // ── GetCompatible: CC0 / PD ─────────────────────────────────────

    [Theory]
    [InlineData("CC0-1.0")]
    [InlineData("PD-old")]
    [InlineData("public domain")]
    [InlineData("Unlicense")]
    public void GetCompatible_PublicDomain_ReturnsAllLicenses(string source)
    {
        var result = LicenseCatalog.GetCompatible(source, CorpusKind.Open);
        Assert.Equal(LicenseCatalog.All.Length, result.Count);
    }

    // ── GetCompatible: CC BY ────────────────────────────────────────

    [Theory]
    [InlineData("CC-BY-4.0")]
    [InlineData("CC BY 4.0")]
    public void GetCompatible_CcBy_ReturnsAllLicenses(string source)
    {
        var result = LicenseCatalog.GetCompatible(source, CorpusKind.Open);
        Assert.Equal(LicenseCatalog.All.Length, result.Count);
    }

    // ── GetCompatible: CC BY-SA (sticky) ────────────────────────────

    [Theory]
    [InlineData("CC-BY-SA-4.0")]
    [InlineData("CC BY-SA 4.0")]
    public void GetCompatible_CcBySa_ReturnsOnlySaAndArr(string source)
    {
        var result = LicenseCatalog.GetCompatible(source, CorpusKind.Open);
        Assert.Contains(result, o => o.Id == "CC-BY-SA-4.0");
        Assert.Contains(result, o => o.Id == "all-rights-reserved");
        Assert.DoesNotContain(result, o => o.Id == "CC-BY-4.0");
        Assert.DoesNotContain(result, o => o.Id == "CC0-1.0");
    }

    // ── GetCompatible: CC BY-NC-SA (fully sticky) ───────────────────

    [Fact]
    public void GetCompatible_CcByNcSa_ReturnsOnlyNcSaAndArr()
    {
        var result = LicenseCatalog.GetCompatible("CC-BY-NC-SA-4.0", CorpusKind.Open);
        Assert.Contains(result, o => o.Id == "CC-BY-NC-SA-4.0");
        Assert.Contains(result, o => o.Id == "all-rights-reserved");
        Assert.Equal(2, result.Count);
    }

    // ── GetCompatible: CC BY-NC ─────────────────────────────────────

    [Theory]
    [InlineData("CC-BY-NC-4.0")]
    [InlineData("CC BY-NC 4.0")]
    public void GetCompatible_CcByNc_ReturnsNcVariantsOnly(string source)
    {
        var result = LicenseCatalog.GetCompatible(source, CorpusKind.Open);
        Assert.Contains(result, o => o.Id == "CC-BY-NC-4.0");
        Assert.Contains(result, o => o.Id == "CC-BY-NC-SA-4.0");
        Assert.Contains(result, o => o.Id == "CC-BY-NC-ND-4.0");
        Assert.Contains(result, o => o.Id == "all-rights-reserved");
        Assert.DoesNotContain(result, o => o.Id == "CC-BY-4.0");
        Assert.DoesNotContain(result, o => o.Id == "CC0-1.0");
    }

    // ── GetCompatible: CC BY-NC-ND ──────────────────────────────────

    [Fact]
    public void GetCompatible_CcByNcNd_ReturnsNcNdAndArr()
    {
        var result = LicenseCatalog.GetCompatible("CC-BY-NC-ND-4.0", CorpusKind.Open);
        Assert.Contains(result, o => o.Id == "CC-BY-NC-ND-4.0");
        Assert.Contains(result, o => o.Id == "all-rights-reserved");
        Assert.Equal(2, result.Count);
    }

    // ── GetCompatible: ordering correctness ─────────────────────────

    [Fact]
    public void GetCompatible_CcByNcSa_DoesNotMatchCcBySaBranch()
    {
        // CC-BY-NC-SA contains "CC-BY-SA" — must not match the SA-only branch
        var result = LicenseCatalog.GetCompatible("CC-BY-NC-SA-4.0", CorpusKind.Open);
        Assert.DoesNotContain(result, o => o.Id == "CC-BY-SA-4.0");
    }

    [Fact]
    public void GetCompatible_CcByNc_DoesNotMatchCcByBranch()
    {
        // CC-BY-NC contains "CC-BY-" — must not match the BY-only branch
        var result = LicenseCatalog.GetCompatible("CC-BY-NC-4.0", CorpusKind.Open);
        Assert.DoesNotContain(result, o => o.Id == "CC-BY-4.0");
    }

    // ── GetCompatible: unknown source ───────────────────────────────

    [Fact]
    public void GetCompatible_UnknownSource_ReturnsOnlyArr()
    {
        var result = LicenseCatalog.GetCompatible("some-custom-license", CorpusKind.Open);
        Assert.Single(result);
        Assert.Equal("all-rights-reserved", result[0].Id);
    }

    [Fact]
    public void GetCompatible_NullSource_ReturnsOnlyArr()
    {
        var result = LicenseCatalog.GetCompatible(null, CorpusKind.Open);
        Assert.Single(result);
        Assert.Equal("all-rights-reserved", result[0].Id);
    }

    // ── GetDefault ──────────────────────────────────────────────────

    [Fact]
    public void GetDefault_Cbeta_ReturnsCbetaNc()
    {
        var result = LicenseCatalog.GetDefault("anything", CorpusKind.Cbeta);
        Assert.NotNull(result);
        Assert.Equal("cbeta-nc", result!.Id);
    }

    [Fact]
    public void GetDefault_Cc0_ReturnsCc0()
    {
        var result = LicenseCatalog.GetDefault("CC0-1.0", CorpusKind.Open);
        Assert.NotNull(result);
        Assert.Equal("CC0-1.0", result!.Id);
    }

    [Fact]
    public void GetDefault_CcBySa_ReturnsCcBySa()
    {
        var result = LicenseCatalog.GetDefault("CC-BY-SA-4.0", CorpusKind.Open);
        Assert.NotNull(result);
        Assert.Equal("CC-BY-SA-4.0", result!.Id);
    }

    [Fact]
    public void GetDefault_CcByNcSa_ReturnsCcByNcSa()
    {
        var result = LicenseCatalog.GetDefault("CC-BY-NC-SA-4.0", CorpusKind.Open);
        Assert.NotNull(result);
        Assert.Equal("CC-BY-NC-SA-4.0", result!.Id);
    }

    [Fact]
    public void GetDefault_Unknown_ReturnsNull()
    {
        var result = LicenseCatalog.GetDefault("custom-license", CorpusKind.Open);
        Assert.Null(result);
    }

    // ── Find ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("CC-BY-4.0")]
    [InlineData("CC0-1.0")]
    [InlineData("cbeta-nc")]
    [InlineData("all-rights-reserved")]
    public void Find_KnownId_ReturnsOption(string id)
    {
        var result = LicenseCatalog.Find(id);
        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
    }

    [Fact]
    public void Find_UnknownId_ReturnsNull()
    {
        Assert.Null(LicenseCatalog.Find("not-a-license"));
    }
}
