using CbetaTranslator.App.Models;
using CbetaTranslator.App.ViewModels;
using CbetaTranslator.Tests.Stubs;
using Xunit;

namespace CbetaTranslator.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private static MainWindowViewModel MakeVm()
    {
        return new MainWindowViewModel(
            new StubFileService(),
            new StubAppConfigService(),
            new StubIndexCacheService(),
            new StubRenderedDocumentCacheService(),
            new StubZenTextsService(),
            new StubIndexedTranslationService(),
            new StubTranslationAssistantService(),
            new StubTranslationAssistantBuildService(),
            new StubTranslationReviewService());
    }

    // ---- Initial state ----

    [Fact]
    public void InitialState_HasDefaults()
    {
        var vm = MakeVm();

        Assert.Equal("Ready.", vm.StatusText);
        Assert.Equal("", vm.RootDisplayText);
        Assert.Equal("", vm.CurrentFileText);
        Assert.Contains("CBETA Translator", vm.WindowTitle);
        Assert.False(vm.IsDirty);
        Assert.Null(vm.Root);
        Assert.Null(vm.CurrentRelPath);
    }

    // ---- SetStatus ----

    [Fact]
    public void SetStatus_UpdatesStatusText()
    {
        var vm = MakeVm();

        vm.SetStatus("Loading...");

        Assert.Equal("Loading...", vm.StatusText);
    }

    // ---- UpdateWindowTitle ----

    [Fact]
    public void UpdateWindowTitle_NoFile_ShowsBaseTitle()
    {
        var vm = MakeVm();

        vm.UpdateWindowTitle();

        Assert.Equal("CBETA Translator", vm.WindowTitle);
        Assert.Equal("", vm.CurrentFileText);
    }

    [Fact]
    public void UpdateWindowTitle_BridgeInvoked()
    {
        var vm = MakeVm();
        string? received = null;
        vm.SetWindowTitle = t => received = t;

        vm.UpdateWindowTitle();

        Assert.NotNull(received);
        Assert.Contains("CBETA Translator", received!);
    }

    // ---- NormalizeRel ----

    [Fact]
    public void NormalizeRel_ConvertsBackslashesAndTrimsLeadingSlash()
    {
        Assert.Equal("a/b/c.xml", MainWindowViewModel.NormalizeRel("\\a\\b\\c.xml"));
        Assert.Equal("a/b.xml", MainWindowViewModel.NormalizeRel("/a/b.xml"));
        Assert.Equal("a/b.xml", MainWindowViewModel.NormalizeRel("a/b.xml"));
    }

    [Fact]
    public void NormalizeRel_Null_ReturnsEmpty()
    {
        Assert.Equal("", MainWindowViewModel.NormalizeRel(null!));
    }

    // ---- PropertyChanged ----

    [Fact]
    public void PropertyChanged_FiredForStatusText()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.StatusText = "Hello";

        Assert.Contains("StatusText", changed);
    }

    [Fact]
    public void PropertyChanged_FiredForWindowTitle()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.WindowTitle = "New Title";

        Assert.Contains("WindowTitle", changed);
    }

    [Fact]
    public void PropertyChanged_FiredForRootDisplayText()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.RootDisplayText = "/new/root";

        Assert.Contains("RootDisplayText", changed);
    }

    // ---- SetStatus with severity ----

    [Fact]
    public void SetStatus_DefaultSeverity_SetsStatusSeverityToInfo()
    {
        var vm = MakeVm();

        vm.SetStatus("Something happened");

        Assert.Equal(StatusSeverity.Info, vm.StatusSeverity);
        Assert.Equal("Something happened", vm.StatusText);
    }

    [Fact]
    public void SetStatus_ErrorSeverity_SetsStatusSeverityToError()
    {
        var vm = MakeVm();

        vm.SetStatus("Failed!", StatusSeverity.Error);

        Assert.Equal(StatusSeverity.Error, vm.StatusSeverity);
        Assert.Equal("Failed!", vm.StatusText);
    }

    [Fact]
    public void SetStatus_SuccessSeverity_SetsStatusSeverityToSuccess()
    {
        var vm = MakeVm();

        vm.SetStatus("Done!", StatusSeverity.Success);

        Assert.Equal(StatusSeverity.Success, vm.StatusSeverity);
        Assert.Equal("Done!", vm.StatusText);
    }

    [Fact]
    public void SetStatus_WarningSeverity_SetsStatusSeverityToWarning()
    {
        var vm = MakeVm();

        vm.SetStatus("Caution", StatusSeverity.Warning);

        Assert.Equal(StatusSeverity.Warning, vm.StatusSeverity);
    }

    [Fact]
    public void SetStatus_FiresPropertyChangedForBothStatusTextAndStatusSeverity()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.SetStatus("test", StatusSeverity.Error);

        Assert.Contains("StatusText", changed);
        Assert.Contains("StatusSeverity", changed);
    }

    [Theory]
    [InlineData(StatusSeverity.Info)]
    [InlineData(StatusSeverity.Success)]
    [InlineData(StatusSeverity.Warning)]
    [InlineData(StatusSeverity.Error)]
    public void SetStatus_AllSeverities_SetCorrectly(StatusSeverity severity)
    {
        var vm = MakeVm();

        vm.SetStatus("msg", severity);

        Assert.Equal(severity, vm.StatusSeverity);
    }

    // ---- Config ----

    [Fact]
    public void Config_HasDefaultDarkTheme()
    {
        var vm = MakeVm();
        Assert.True(vm.Config.IsDarkTheme);
    }

    // ---- FilteredItems ----

    [Fact]
    public void FilteredItems_InitiallyEmpty()
    {
        var vm = MakeVm();
        Assert.Empty(vm.FilteredItems);
    }

    [Fact]
    public void AllItemsByRel_InitiallyEmpty()
    {
        var vm = MakeVm();
        Assert.Empty(vm.AllItemsByRel);
    }
}
