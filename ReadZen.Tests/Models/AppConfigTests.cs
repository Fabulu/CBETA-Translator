using System.Text.Json;
using ReadZen.App.Models;
using Xunit;

namespace ReadZen.Tests.Models;

public class AppConfigTests
{
    [Fact]
    public void EnableStudyPanel_DefaultsFalse()
    {
        var config = new AppConfig();
        Assert.False(config.EnableStudyPanel);
    }

    [Fact]
    public void EnableStudyPanel_CanBeSet()
    {
        var config = new AppConfig();

        config.EnableStudyPanel = true;

        Assert.True(config.EnableStudyPanel);
    }

    [Fact]
    public void EnableHoverDictionary_DefaultsTrue()
    {
        var config = new AppConfig();
        Assert.True(config.EnableHoverDictionary);
    }

    [Fact]
    public void IsDarkTheme_DefaultsTrue()
    {
        var config = new AppConfig();
        Assert.True(config.IsDarkTheme);
    }

    [Fact]
    public void Version_DefaultsTo4()
    {
        // Bumped from 3 to 4 when ActiveCorpus was added; AppConfigService
        // migrates v3 configs forward by setting ActiveCorpus = Cbeta.
        var config = new AppConfig();
        Assert.Equal(4, config.Version);
    }

    [Fact]
    public void ActiveCorpus_DefaultsToCbeta()
    {
        var config = new AppConfig();
        Assert.Equal(CorpusKind.Cbeta, config.ActiveCorpus);
    }

    [Fact]
    public void Username_DefaultsNull()
    {
        var config = new AppConfig();
        Assert.Null(config.Username);
    }

    [Fact]
    public void AllProperties_RoundTrip()
    {
        var config = new AppConfig
        {
            TextRootPath = "/some/path",
            LastSelectedRelPath = "T01/T0001.xml",
            IsDarkTheme = false,
            ZenOnly = true,
            EnableHoverDictionary = false,
            Username = "TestUser",
            GitHubAccessToken = "ghp_abc123",
            GitHubUsername = "octocat",
            HasCompletedOnboarding = true,
            HasRegisteredProtocolHandler = true,
            EnableStudyPanel = true,
            Version = 5,
        };

        Assert.Equal("/some/path", config.TextRootPath);
        Assert.Equal("T01/T0001.xml", config.LastSelectedRelPath);
        Assert.False(config.IsDarkTheme);
        Assert.True(config.ZenOnly);
        Assert.False(config.EnableHoverDictionary);
        Assert.Equal("TestUser", config.Username);
        Assert.Equal("ghp_abc123", config.GitHubAccessToken);
        Assert.Equal("octocat", config.GitHubUsername);
        Assert.True(config.HasCompletedOnboarding);
        Assert.True(config.HasRegisteredProtocolHandler);
        Assert.True(config.EnableStudyPanel);
        Assert.Equal(5, config.Version);
    }

    // ---- Window state persistence tests ----

    [Fact]
    public void WindowState_DefaultsToNull()
    {
        var config = new AppConfig();

        Assert.Null(config.WindowX);
        Assert.Null(config.WindowY);
        Assert.Null(config.WindowWidth);
        Assert.Null(config.WindowHeight);
        Assert.False(config.IsMaximized);
    }

    [Fact]
    public void WindowState_CanBeSet()
    {
        var config = new AppConfig
        {
            WindowX = 100.5,
            WindowY = 200.0,
            WindowWidth = 1280.0,
            WindowHeight = 720.0,
            IsMaximized = true,
        };

        Assert.Equal(100.5, config.WindowX);
        Assert.Equal(200.0, config.WindowY);
        Assert.Equal(1280.0, config.WindowWidth);
        Assert.Equal(720.0, config.WindowHeight);
        Assert.True(config.IsMaximized);
    }

    [Fact]
    public void WindowState_JsonRoundTrip_WithValues()
    {
        var original = new AppConfig
        {
            WindowX = 50.0,
            WindowY = 75.5,
            WindowWidth = 1920.0,
            WindowHeight = 1080.0,
            IsMaximized = false,
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<AppConfig>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(50.0, deserialized!.WindowX);
        Assert.Equal(75.5, deserialized.WindowY);
        Assert.Equal(1920.0, deserialized.WindowWidth);
        Assert.Equal(1080.0, deserialized.WindowHeight);
        Assert.False(deserialized.IsMaximized);
    }

    [Fact]
    public void WindowState_JsonRoundTrip_NullValues()
    {
        var original = new AppConfig();

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<AppConfig>(json);

        Assert.NotNull(deserialized);
        Assert.Null(deserialized!.WindowX);
        Assert.Null(deserialized.WindowY);
        Assert.Null(deserialized.WindowWidth);
        Assert.Null(deserialized.WindowHeight);
        Assert.False(deserialized.IsMaximized);
    }

    [Fact]
    public void WindowState_JsonRoundTrip_IsMaximizedTrue()
    {
        var original = new AppConfig { IsMaximized = true };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<AppConfig>(json);

        Assert.NotNull(deserialized);
        Assert.True(deserialized!.IsMaximized);
    }

    [Fact]
    public void WindowState_DeserializesFromJson_MissingWindowFields()
    {
        // Simulates loading a config.json saved before window state was added
        var json = """{"IsDarkTheme":true,"Version":4}""";
        var deserialized = JsonSerializer.Deserialize<AppConfig>(json);

        Assert.NotNull(deserialized);
        Assert.Null(deserialized!.WindowX);
        Assert.Null(deserialized.WindowY);
        Assert.Null(deserialized.WindowWidth);
        Assert.Null(deserialized.WindowHeight);
        Assert.False(deserialized.IsMaximized);
    }

    [Fact]
    public void WindowState_AllProperties_IncludedInFullRoundTrip()
    {
        // Comprehensive round-trip including window state alongside all other fields
        var config = new AppConfig
        {
            TextRootPath = "/root",
            LastSelectedRelPath = "T01/T0001.xml",
            IsDarkTheme = false,
            ZenOnly = true,
            EnableHoverDictionary = false,
            Username = "TestUser",
            GitHubAccessToken = "ghp_abc123",
            GitHubUsername = "octocat",
            HasCompletedOnboarding = true,
            HasRegisteredProtocolHandler = true,
            EnableStudyPanel = true,
            EnableProvenancePanel = true,
            WindowX = 123.0,
            WindowY = 456.0,
            WindowWidth = 1600.0,
            WindowHeight = 900.0,
            IsMaximized = true,
            EnableConcordance = true,
            TmMaxResults = 12,
            Version = 4,
        };

        var json = JsonSerializer.Serialize(config);
        var rt = JsonSerializer.Deserialize<AppConfig>(json);

        Assert.NotNull(rt);
        Assert.Equal(123.0, rt!.WindowX);
        Assert.Equal(456.0, rt.WindowY);
        Assert.Equal(1600.0, rt.WindowWidth);
        Assert.Equal(900.0, rt.WindowHeight);
        Assert.True(rt.IsMaximized);
        Assert.True(rt.EnableConcordance);
        Assert.Equal(12, rt.TmMaxResults);
        Assert.True(rt.EnableProvenancePanel);
    }

    // ---- TmMaxResults / EnableConcordance defaults ----

    [Fact]
    public void TmMaxResults_DefaultsTo8()
    {
        var config = new AppConfig();
        Assert.Equal(8, config.TmMaxResults);
    }

    [Fact]
    public void EnableConcordance_DefaultsFalse()
    {
        var config = new AppConfig();
        Assert.False(config.EnableConcordance);
    }
}
