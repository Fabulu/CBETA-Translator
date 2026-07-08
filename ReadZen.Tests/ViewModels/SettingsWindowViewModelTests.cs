using System.ComponentModel;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;
using Xunit;

namespace ReadZen.Tests.ViewModels;

public class SettingsWindowViewModelTests
{
    private static AppConfig MakeConfig(
        string username = "TestUser",
        bool isDark = true,
        bool hoverDict = true)
    {
        return new AppConfig
        {
            Username = username,
            IsDarkTheme = isDark,
            EnableHoverDictionary = hoverDict,
            TextRootPath = "/some/root",
            LastSelectedRelPath = "T2076/T2076_.xml",
            ZenOnly = false,
            Version = 3
        };
    }

    [Fact]
    public void Constructor_CopiesConfigValues()
    {
        var cfg = MakeConfig("Alice", isDark: false, hoverDict: false);
        var vm = new SettingsWindowViewModel(cfg);

        Assert.Equal("Alice", vm.Username);
        Assert.False(vm.IsDarkTheme);
        Assert.False(vm.EnableHoverDictionary);
        Assert.Null(vm.Result);
    }

    [Fact]
    public void Constructor_NullUsername_DefaultsToEmpty()
    {
        var cfg = MakeConfig();
        cfg.Username = null;
        var vm = new SettingsWindowViewModel(cfg);

        Assert.Equal("", vm.Username);
    }

    [Fact]
    public void Apply_ValidUsername_SetsResultAndFiresCloseRequested()
    {
        var cfg = MakeConfig("Bob");
        var vm = new SettingsWindowViewModel(cfg);
        vm.Username = "  NewName  ";
        vm.IsDarkTheme = false;
        vm.EnableHoverDictionary = false;

        AppConfig? closedWith = null;
        vm.CloseRequested = result => closedWith = result;

        vm.ApplyCommand.Execute(null);

        Assert.NotNull(vm.Result);
        Assert.Equal("NewName", vm.Result!.Username);
        Assert.False(vm.Result.IsDarkTheme);
        Assert.False(vm.Result.EnableHoverDictionary);
        Assert.Equal(cfg.TextRootPath, vm.Result.TextRootPath);
        Assert.Equal(cfg.LastSelectedRelPath, vm.Result.LastSelectedRelPath);
        Assert.Equal(cfg.Version, vm.Result.Version);

        Assert.NotNull(closedWith);
        Assert.Same(vm.Result, closedWith);
    }

    [Fact]
    public void Apply_EmptyUsername_ShowsError_DoesNotClose()
    {
        var cfg = MakeConfig("Bob");
        var vm = new SettingsWindowViewModel(cfg);
        vm.Username = "   ";

        bool closeCalled = false;
        vm.CloseRequested = _ => closeCalled = true;

        vm.ApplyCommand.Execute(null);

        Assert.True(vm.ShowUsernameError);
        Assert.Null(vm.Result);
        Assert.False(closeCalled);
    }

    [Fact]
    public void Cancel_FiresCloseRequestedWithNull()
    {
        var cfg = MakeConfig();
        var vm = new SettingsWindowViewModel(cfg);

        AppConfig? closedWith = new AppConfig(); // sentinel
        vm.CloseRequested = result => closedWith = result;

        vm.CancelCommand.Execute(null);

        Assert.Null(closedWith);
        Assert.Null(vm.Result);
    }

    [Fact]
    public void PropertyChanged_FiredForUsername()
    {
        var vm = new SettingsWindowViewModel(MakeConfig());
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.Username = "Changed";

        Assert.Contains("Username", changed);
    }

    [Fact]
    public void PropertyChanged_FiredForIsDarkTheme()
    {
        var vm = new SettingsWindowViewModel(MakeConfig());
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.IsDarkTheme = false;

        Assert.Contains("IsDarkTheme", changed);
    }

    [Fact]
    public void PropertyChanged_FiredForShowUsernameError()
    {
        var vm = new SettingsWindowViewModel(MakeConfig());
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.ShowUsernameError = true;

        Assert.Contains("ShowUsernameError", changed);
    }

    // ---------------------------------------------------------------
    // Regression: Apply() must PRESERVE fields the dialog does not edit.
    // The old hand-written copy list silently reset citation style, panels,
    // window geometry, search history, corpus, ... on every settings save.
    // ---------------------------------------------------------------

    [Fact]
    public void Apply_PreservesFieldsTheDialogDoesNotEdit()
    {
        var cfg = MakeConfig();
        cfg.EnableStudyPanel = true;
        cfg.EnableProvenancePanel = true;
        cfg.PreferredCitationStyle = CitationStyle.Apa;
        cfg.PreferredCitationStyleIndex = 2;
        cfg.EditorFontSize = 18.5;
        cfg.HasRegisteredProtocolHandler = true;
        cfg.SearchHistory.Add("previous search");
        cfg.EnableBilingualScrollSync = false;

        var vm = new SettingsWindowViewModel(cfg);
        vm.ApplyCommand.Execute(null);

        var r = vm.Result;
        Assert.NotNull(r);
        Assert.True(r!.EnableStudyPanel);
        Assert.True(r.EnableProvenancePanel);
        Assert.Equal(CitationStyle.Apa, r.PreferredCitationStyle);
        Assert.Equal(2, r.PreferredCitationStyleIndex);
        Assert.Equal(18.5, r.EditorFontSize);
        Assert.True(r.HasRegisteredProtocolHandler);
        Assert.Contains("previous search", r.SearchHistory);
        // Dialog-edited field round-trips from the constructor-loaded value:
        Assert.False(r.EnableBilingualScrollSync);
    }

    [Fact]
    public void Apply_EditedFieldsStillWin()
    {
        var cfg = MakeConfig(isDark: true, hoverDict: true);
        cfg.EnableBilingualScrollSync = true;

        var vm = new SettingsWindowViewModel(cfg);
        vm.IsDarkTheme = false;
        vm.EnableHoverDictionary = false;
        vm.EnableBilingualScrollSync = false;
        vm.ApplyCommand.Execute(null);

        var r = vm.Result;
        Assert.NotNull(r);
        Assert.False(r!.IsDarkTheme);
        Assert.False(r.EnableHoverDictionary);
        Assert.False(r.EnableBilingualScrollSync);
    }
}
