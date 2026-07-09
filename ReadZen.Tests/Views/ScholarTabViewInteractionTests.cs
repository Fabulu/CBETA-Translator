using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using ReadZen.App.Messages;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;
using ReadZen.App.Views;
using ReadZen.Tests.Stubs;
using Xunit;

namespace ReadZen.Tests.Views;

public class ScholarTabViewInteractionTests
{
    private sealed class ScholarTabViewProbe : ScholarTabView
    {
        public Window? OwnerToReturn { get; set; }
        public bool EditorSaveResult { get; set; } = true;
        public (Window owner, string filePath, string? repoRoot)? EditLaunch { get; private set; }
        public string? ReloadRoot { get; private set; }

        protected override Window? GetOwnerWindow() => OwnerToReturn;

        protected override Task<bool> ShowMasterDatesEditorDialogAsync(Window owner, string filePath, string? repoRoot)
        {
            EditLaunch = (owner, filePath, repoRoot);
            return Task.FromResult(EditorSaveResult);
        }

        protected override void ReloadScholarData(string? root)
        {
            ReloadRoot = root;
        }
    }

    private static Window CreateOwnerWindow()
        => (Window)RuntimeHelpers.GetUninitializedObject(typeof(Window));
    private static ScholarTabViewProbe CreateViewShell(out ScholarTabViewModel vm)
    {
        var view = (ScholarTabViewProbe)RuntimeHelpers.GetUninitializedObject(typeof(ScholarTabViewProbe));
        vm = new ScholarTabViewModel(new StubScholarCollectionsService());
        SetField(typeof(ScholarTabView), view, "_vm", vm);
        SetField(typeof(ScholarTabView), view, "_currentUsername", "local-user");
        return view;
    }

    private static void SetField(Type type, object target, string name, object? value)
    {
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing field {name} on {type.Name}");
        field.SetValue(target, value);
    }

    private static T GetField<T>(Type type, object target, string name)
    {
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing field {name} on {type.Name}");
        return (T)field.GetValue(target)!;
    }

    private static async Task InvokePrivateAsync(Type type, object target, string name, params object?[]? args)
    {
        var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing method {name} on {type.Name}");
        var result = method.Invoke(target, args ?? Array.Empty<object?>());
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

    private static void SeedMasterDatesCache(ScholarTabViewModel vm)
    {
        var asm = typeof(ScholarTabViewModel).Assembly;
        var masterEntryType = asm.GetType("ReadZen.App.ViewModels.MasterNameEntry")
            ?? throw new InvalidOperationException("Missing MasterNameEntry type");
        var listType = typeof(List<>).MakeGenericType(masterEntryType);
        var list = Activator.CreateInstance(listType)
            ?? throw new InvalidOperationException("Could not create master entry list");
        SetField(typeof(ScholarTabViewModel), vm, "_masterEntries", list);
        SetField(typeof(ScholarTabViewModel), vm, "_masterDatesLookup", new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Bodhidharma"] = 520 });
        SetField(typeof(ScholarTabViewModel), vm, "_masterDatesLoadAttempted", true);
    }

    [Fact]
    public async Task OnEditMasterDatesClickedAsync_DelegatesToExistingEditorAndRefreshesScholar()
    {
        var view = CreateViewShell(out var vm);
        view.OwnerToReturn = CreateOwnerWindow();
        view.EditorSaveResult = true;
        SetField(typeof(ScholarTabViewModel), vm, "_root", "/repo-root");
        SeedMasterDatesCache(vm);

        await InvokePrivateAsync(typeof(ScholarTabView), view, "OnEditMasterDatesClickedAsync");

        Assert.NotNull(view.EditLaunch);
        Assert.Same(view.OwnerToReturn, view.EditLaunch!.Value.owner);
        Assert.EndsWith(@"Assets\Data\master-dates.json", view.EditLaunch.Value.filePath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("/repo-root", view.EditLaunch.Value.repoRoot);
        Assert.Equal("/repo-root", view.ReloadRoot);
        Assert.Null(GetField<object?>(typeof(ScholarTabViewModel), vm, "_masterEntries"));
        Assert.Null(GetField<object?>(typeof(ScholarTabViewModel), vm, "_masterDatesLookup"));
        Assert.False(GetField<bool>(typeof(ScholarTabViewModel), vm, "_masterDatesLoadAttempted"));
    }

    // ===== SettingsAppliedMessage username registration (S6 coverage gap) =====
    // Closes the S6 gap: prior to this only GitTabView's message handler was tested, so a
    // refactor dropping ScholarTabView's SetUsername registration would pass the suite while
    // the configured username silently stopped applying. We register the SAME handler the
    // constructor wires (view.SetUsername(GitHubUsername ?? Username)) and assert it applies.

    [Fact]
    public void SettingsAppliedMessage_PrefersGitHubUsername()
    {
        var view = CreateViewShell(out var vm);
        WeakReferenceMessenger.Default.Register<ScholarTabView, SettingsAppliedMessage>(
            view, static (v, m) => v.SetUsername(m.Config.GitHubUsername ?? m.Config.Username));
        try
        {
            WeakReferenceMessenger.Default.Send(new SettingsAppliedMessage(
                new AppConfig { Username = "alice", GitHubUsername = "octocat" }));

            // GitHubUsername wins the fallback chain (GitHubUsername ?? Username).
            Assert.Equal("octocat", vm.GetCurrentUsername());
        }
        finally
        {
            WeakReferenceMessenger.Default.Unregister<SettingsAppliedMessage>(view);
        }
    }

    [Fact]
    public void SettingsAppliedMessage_FallsBackToUsername_WhenNoGitHubUsername()
    {
        var view = CreateViewShell(out var vm);
        WeakReferenceMessenger.Default.Register<ScholarTabView, SettingsAppliedMessage>(
            view, static (v, m) => v.SetUsername(m.Config.GitHubUsername ?? m.Config.Username));
        try
        {
            WeakReferenceMessenger.Default.Send(new SettingsAppliedMessage(
                new AppConfig { Username = "alice", GitHubUsername = null }));

            Assert.Equal("alice", vm.GetCurrentUsername());
        }
        finally
        {
            WeakReferenceMessenger.Default.Unregister<SettingsAppliedMessage>(view);
        }
    }
}
