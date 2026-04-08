using ReadZen.App.ViewModels;
using Xunit;

namespace ReadZen.Tests.ViewModels;

public class LicensesWindowViewModelTests
{
    [Fact]
    public void Constructor_NullRoot_SetsHintWithNoRoot()
    {
        var vm = new LicensesWindowViewModel(null);

        Assert.Contains("(no text root loaded)", vm.HintText);
    }

    [Fact]
    public void Constructor_WithRoot_SetsHintWithRoot()
    {
        var vm = new LicensesWindowViewModel("/some/root");

        Assert.Contains("/some/root", vm.HintText);
    }

    [Fact]
    public void Constructor_AlwaysSetsNonEmptyLicensesText()
    {
        var vm = new LicensesWindowViewModel(null);

        Assert.False(string.IsNullOrWhiteSpace(vm.LicensesText));
        Assert.Contains("Read Zen", vm.LicensesText);
        Assert.Contains("Licenses & Attributions", vm.LicensesText);
    }

    [Fact]
    public void LicensesText_ContainsExpectedSections()
    {
        var vm = new LicensesWindowViewModel(null);

        Assert.Contains("App License", vm.LicensesText);
        Assert.Contains("Third-Party Notices", vm.LicensesText);
        Assert.Contains("CC-CEDICT", vm.LicensesText);
        Assert.Contains("Notes", vm.LicensesText);
    }

    [Fact]
    public void CloseWindow_FiresCloseRequested()
    {
        var vm = new LicensesWindowViewModel(null);

        bool closeCalled = false;
        vm.CloseRequested = () => closeCalled = true;

        vm.CloseWindowCommand.Execute(null);

        Assert.True(closeCalled);
    }

    [Fact]
    public void CloseWindow_NullHandler_DoesNotThrow()
    {
        var vm = new LicensesWindowViewModel(null);

        // Should not throw with no handler
        vm.CloseWindowCommand.Execute(null);
    }
}
