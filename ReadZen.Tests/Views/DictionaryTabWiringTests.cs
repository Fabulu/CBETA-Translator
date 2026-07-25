using System;
using System.Reflection;
using Avalonia.Controls;
using ReadZen.App.Models;
using ReadZen.App.Views;
using Xunit;

namespace ReadZen.Tests.Views;

/// <summary>
/// Pins the dict-as-tab fix at the seam that is testable without a windowing platform or
/// a live App.Services container: the reusable <see cref="DictionaryEditorView"/> that the
/// new top-level Dictionary tab hosts must expose the exact contract MainWindow wires to
/// (Load/Reload for corpus (re)loads, SetCloseButtonVisible + CloseRequested to repurpose
/// the pop-out Close button as "return to Read", and the CorpusNavigationRequested /
/// EditRequested events). If any of these are renamed/removed, the tab wiring silently
/// breaks — this test fails loudly instead.
///
/// The MainWindow-side XAML (the TabDictionaryItem strip item + the hosted
/// DictionaryEditorView) and the "selecting the Dictionary tab loads it" handler live in
/// MainWindow, which cannot be constructed in a unit test (it needs App.Services and a
/// desktop windowing platform); those are covered by the app's manual/integration surface.
/// </summary>
[Trait("Domain", "Dictionary")]
public class DictionaryTabWiringTests
{
    private static readonly Type ViewType = typeof(DictionaryEditorView);

    [Fact]
    public void DictionaryEditorView_IsAUserControl()
    {
        // The Dictionary tab hosts it as a child control (not a Window).
        Assert.True(typeof(UserControl).IsAssignableFrom(ViewType));
    }

    [Theory]
    [InlineData("Load")]
    [InlineData("Reload")]
    [InlineData("SetCloseButtonVisible")]
    public void DictionaryEditorView_ExposesTabHostMethods(string methodName)
    {
        var method = ViewType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
    }

    [Fact]
    public void DictionaryEditorView_Load_AcceptsCorpusSignature()
    {
        // MainWindow calls Load(root, origDir, transDir, username).
        var method = ViewType.GetMethod("Load", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var p = method!.GetParameters();
        Assert.True(p.Length >= 4, "Load must accept at least (root, origDir, transDir, username)");
        Assert.Equal(typeof(string), p[0].ParameterType);
        Assert.Equal(typeof(string), p[1].ParameterType);
        Assert.Equal(typeof(string), p[2].ParameterType);
    }

    [Fact]
    public void DictionaryEditorView_ExposesCloseRequestedHook()
    {
        var prop = ViewType.GetProperty("CloseRequested", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(prop);
        Assert.Equal(typeof(Action), prop!.PropertyType);
        Assert.True(prop.CanWrite, "host assigns CloseRequested = () => ForceTab(0)");
    }

    [Theory]
    [InlineData("CorpusNavigationRequested")]
    [InlineData("EditRequested")]
    public void DictionaryEditorView_ExposesHostEvents(string eventName)
    {
        Assert.NotNull(ViewType.GetEvent(eventName, BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void DictionaryEditorView_CorpusNavigation_CarriesNavigationRequest()
    {
        var evt = ViewType.GetEvent("CorpusNavigationRequested", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(evt);
        Assert.Equal(typeof(EventHandler<NavigationRequest>), evt!.EventHandlerType);
    }
}
