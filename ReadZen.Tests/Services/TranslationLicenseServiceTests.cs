using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

public class TranslationLicenseServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly TranslationLicenseService _svc;

    public TranslationLicenseServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "readzen_license_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _svc = new TranslationLicenseService();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip()
    {
        var license = new TranslationLicenseInfo
        {
            RelPath = "T/T48/T48n2005.xml",
            License = "CC-BY-4.0",
            CopyrightHolder = "TestUser",
            CommercialUseAllowed = true,
            AttributionRequired = true,
        };

        await _svc.SaveLicenseAsync(_tempDir, "testuser", license);
        await _svc.LoadUserLicensesAsync(_tempDir, "testuser");

        var loaded = _svc.GetLicense("T/T48/T48n2005.xml");
        Assert.NotNull(loaded);
        Assert.Equal("CC-BY-4.0", loaded!.License);
        Assert.Equal("TestUser", loaded.CopyrightHolder);
        Assert.True(loaded.CommercialUseAllowed);
    }

    [Fact]
    public async Task GetLicense_NotFound_ReturnsNull()
    {
        await _svc.LoadUserLicensesAsync(_tempDir, "testuser");
        Assert.Null(_svc.GetLicense("nonexistent.xml"));
    }

    [Fact]
    public async Task SaveLicense_CreatesDirectory()
    {
        var license = new TranslationLicenseInfo
        {
            RelPath = "test.xml",
            License = "CC0-1.0",
        };

        var subDir = Path.Combine(_tempDir, "sub");
        await _svc.SaveLicenseAsync(subDir, "testuser", license);

        var licenseDir = TranslationLicenseService.GetLicenseDir(subDir);
        Assert.True(Directory.Exists(licenseDir));
    }

    [Fact]
    public async Task SaveLicense_OverwritesSameRelPath()
    {
        var license1 = new TranslationLicenseInfo { RelPath = "test.xml", License = "CC-BY-4.0" };
        var license2 = new TranslationLicenseInfo { RelPath = "test.xml", License = "CC0-1.0" };

        await _svc.SaveLicenseAsync(_tempDir, "testuser", license1);
        await _svc.SaveLicenseAsync(_tempDir, "testuser", license2);

        await _svc.LoadUserLicensesAsync(_tempDir, "testuser");
        var loaded = _svc.GetLicense("test.xml");
        Assert.Equal("CC0-1.0", loaded?.License);
    }

    [Fact]
    public async Task GetEffectiveLicense_WithChosenLicense_ReturnsChosen()
    {
        var license = new TranslationLicenseInfo { RelPath = "test.xml", License = "MIT" };
        await _svc.SaveLicenseAsync(_tempDir, "testuser", license);
        await _svc.LoadUserLicensesAsync(_tempDir, "testuser");

        var effective = _svc.GetEffectiveLicense("test.xml", "CC0-1.0", CorpusKind.Open);
        Assert.Equal("MIT", effective.License);
    }

    [Fact]
    public void GetEffectiveLicense_NoChoice_Cbeta_ReturnsCbetaNc()
    {
        var effective = _svc.GetEffectiveLicense("test.xml", null, CorpusKind.Cbeta);
        Assert.Equal("cbeta-nc", effective.License);
    }

    [Fact]
    public void GetEffectiveLicense_NoChoice_NoSource_ReturnsEmpty()
    {
        var effective = _svc.GetEffectiveLicense("test.xml", null, CorpusKind.Open);
        Assert.Null(effective.License);
    }

    [Fact]
    public async Task LoadUserLicenses_MalformedLines_Skipped()
    {
        var licenseDir = TranslationLicenseService.GetLicenseDir(_tempDir);
        Directory.CreateDirectory(licenseDir);
        var path = Path.Combine(licenseDir, "testuser.jsonl");
        await File.WriteAllTextAsync(path,
            "{\"rel_path\":\"good.xml\",\"license\":\"CC0-1.0\"}\n" +
            "THIS IS NOT JSON\n" +
            "{\"rel_path\":\"also_good.xml\",\"license\":\"MIT\"}\n",
            new UTF8Encoding(false));

        await _svc.LoadUserLicensesAsync(_tempDir, "testuser");
        Assert.NotNull(_svc.GetLicense("good.xml"));
        Assert.NotNull(_svc.GetLicense("also_good.xml"));
    }

    [Fact]
    public async Task LoadAllCommunityLicenses_MultipleUsers()
    {
        var license1 = new TranslationLicenseInfo { RelPath = "test.xml", License = "CC-BY-4.0" };
        var license2 = new TranslationLicenseInfo { RelPath = "test.xml", License = "CC0-1.0" };

        await _svc.SaveLicenseAsync(_tempDir, "user1", license1);

        var svc2 = new TranslationLicenseService();
        await svc2.SaveLicenseAsync(_tempDir, "user2", license2);

        var all = await _svc.LoadAllCommunityLicensesAsync(_tempDir);
        Assert.True(all.Count >= 2);
        Assert.Equal("CC-BY-4.0", all[("test.xml", "user1")].License);
        Assert.Equal("CC0-1.0", all[("test.xml", "user2")].License);
    }

    [Fact]
    public async Task SaveLicense_NullRelPath_NoOp()
    {
        var license = new TranslationLicenseInfo { RelPath = null, License = "CC0-1.0" };
        await _svc.SaveLicenseAsync(_tempDir, "testuser", license);
        // Should not create a file with null key
        await _svc.LoadUserLicensesAsync(_tempDir, "testuser");
        Assert.Null(_svc.GetLicense(""));
    }
}
