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
    public void Version_DefaultsTo3()
    {
        var config = new AppConfig();
        Assert.Equal(3, config.Version);
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
}
