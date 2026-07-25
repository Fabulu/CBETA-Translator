using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using ReadZen.App.Views;
using ReadZen.Tests.Stubs;
using Xunit;

namespace ReadZen.Tests.Views;

// After the lineage-embed refactor the Edit-Dates flow moved off the window and onto
// the reusable ZenMasterManagerView (the window is now a thin host of that control).
// This test follows the method to its new home.
public class ZenMasterManagerWindowInteractionTests
{
    private sealed class ZenMasterManagerViewProbe : ZenMasterManagerView
    {
        public (Window? owner, string baseFilePath, string? repoRoot, string? landingName)? EditorLaunch { get; private set; }
        public bool SaveResult { get; set; }

        protected override Task<bool> ShowMasterDatesEditorDialogAsync(Window? owner, string baseFilePath, string? repoRoot, string? landingName)
        {
            EditorLaunch = (owner, baseFilePath, repoRoot, landingName);
            return Task.FromResult(SaveResult);
        }
    }

    private static ZenMasterManagerViewProbe CreateViewShell(out ZenMasterManagerWindowViewModel vm)
    {
        var view = (ZenMasterManagerViewProbe)RuntimeHelpers.GetUninitializedObject(typeof(ZenMasterManagerViewProbe));
        vm = new ZenMasterManagerWindowViewModel(
            new ZenMasterManagerService(new StubMasterDatesService()),
            "/repo-root",
            "/base/master-dates.json");
        vm.SelectedMaster = new ZenMasterRecord { CanonicalName = "Bodhidharma" };
        SetField(typeof(ZenMasterManagerView), view, "<ViewModel>k__BackingField", vm);
        SetField(typeof(ZenMasterManagerView), view, "_repoRoot", "/repo-root");
        SetField(typeof(ZenMasterManagerView), view, "_baseFilePath", "/base/master-dates.json");
        return view;
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
        var view = CreateViewShell(out _);
        view.SaveResult = false;

        await InvokePrivateAsync(typeof(ZenMasterManagerView), view, "OpenEditorAsync");

        Assert.NotNull(view.EditorLaunch);
        // The uninitialized probe is not attached to a visual tree, so the host window
        // (HostWindow) is null — the dialog would fall back to a non-owned Show().
        Assert.Null(view.EditorLaunch!.Value.owner);
        Assert.Equal("/base/master-dates.json", view.EditorLaunch.Value.baseFilePath);
        Assert.Equal("/repo-root", view.EditorLaunch.Value.repoRoot);
        Assert.Equal("Bodhidharma", view.EditorLaunch.Value.landingName);
    }
}
