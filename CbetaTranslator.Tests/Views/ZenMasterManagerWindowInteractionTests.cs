using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.ViewModels;
using CbetaTranslator.App.Views;
using CbetaTranslator.Tests.Stubs;
using Xunit;

namespace CbetaTranslator.Tests.Views;

public class ZenMasterManagerWindowInteractionTests
{
    private sealed class ZenMasterManagerWindowProbe : ZenMasterManagerWindow
    {
        public (Window owner, string baseFilePath, string? repoRoot, string? landingName)? EditorLaunch { get; private set; }
        public bool SaveResult { get; set; }

        protected override Task<bool> ShowMasterDatesEditorDialogAsync(Window owner, string baseFilePath, string? repoRoot, string? landingName)
        {
            EditorLaunch = (owner, baseFilePath, repoRoot, landingName);
            return Task.FromResult(SaveResult);
        }
    }

    private static ZenMasterManagerWindowProbe CreateWindowShell(out ZenMasterManagerWindowViewModel vm)
    {
        var window = (ZenMasterManagerWindowProbe)RuntimeHelpers.GetUninitializedObject(typeof(ZenMasterManagerWindowProbe));
        vm = new ZenMasterManagerWindowViewModel(
            new ZenMasterManagerService(new StubMasterDatesService()),
            "/repo-root",
            "/base/master-dates.json");
        vm.SelectedMaster = new ZenMasterRecord { CanonicalName = "Bodhidharma" };
        SetField(typeof(ZenMasterManagerWindow), window, "<ViewModel>k__BackingField", vm);
        SetField(typeof(ZenMasterManagerWindow), window, "_repoRoot", "/repo-root");
        SetField(typeof(ZenMasterManagerWindow), window, "_baseFilePath", "/base/master-dates.json");
        return window;
    }

    private static void SetField(Type type, object target, string name, object? value)
    {
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing field {name} on {type.Name}");
        field.SetValue(target, value);
    }

    private static async Task InvokePrivateAsync(Type type, object target, string name)
    {
        var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing method {name} on {type.Name}");
        var result = method.Invoke(target, Array.Empty<object>());
        switch (result)
        {
            case Task task:
                await task;
                return;
            case null:
                return;
            default:
                throw new InvalidOperationException($"Unexpected return type from {name}: {result.GetType().FullName}");
        }
    }

    [Fact]
    public async Task OpenEditorAsync_DelegatesToExistingMasterDatesEditorDialog()
    {
        var window = CreateWindowShell(out _);
        window.SaveResult = false;

        await InvokePrivateAsync(typeof(ZenMasterManagerWindow), window, "OpenEditorAsync");

        Assert.NotNull(window.EditorLaunch);
        Assert.Same(window, window.EditorLaunch!.Value.owner);
        Assert.Equal("/base/master-dates.json", window.EditorLaunch.Value.baseFilePath);
        Assert.Equal("/repo-root", window.EditorLaunch.Value.repoRoot);
        Assert.Equal("Bodhidharma", window.EditorLaunch.Value.landingName);
    }
}
