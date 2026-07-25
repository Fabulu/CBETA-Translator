using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReadZen.App.Models;

namespace ReadZen.App.Views;

/// <summary>
/// Pop-out host for the Zen Dictionary browse view. The actual UI + logic live in the
/// reusable <see cref="DictionaryEditorView"/>, which is also hosted by the top-level
/// Dictionary tab in MainWindow. This window is opened from context-menu term lookups
/// and deep links. Editing goes through the rich editor (<see cref="EditRequested"/>).
/// </summary>
public partial class TermbaseEditorWindow : Window
{
    // Parameterless ctor for the XAML/designer loader only; real opens go through the
    // (root, origDir, transDir, …) overload from MainWindow.
    public TermbaseEditorWindow() : this(string.Empty, string.Empty, string.Empty)
    {
    }

    private readonly DictionaryEditorView _editor;

    /// <summary>
    /// Fired when user wants to navigate to a source occurrence in the reader.
    /// </summary>
    public event EventHandler<NavigationRequest>? CorpusNavigationRequested;

    /// <summary>
    /// Fired when the user asks to edit the dictionary; MainWindow opens the rich editor.
    /// </summary>
    public event EventHandler? EditRequested;

    public TermbaseEditorWindow(string root, string origDir, string transDir, string? username = null, string? landingTerm = null, string? landingCommunityUser = null)
    {
        InitializeComponent();

        _editor = this.FindControl<DictionaryEditorView>("Editor")!;
        _editor.CloseRequested = () => Close();
        _editor.CorpusNavigationRequested += (_, req) => CorpusNavigationRequested?.Invoke(this, req);
        _editor.EditRequested += (_, e) => EditRequested?.Invoke(this, e);

        _editor.Load(root, origDir, transDir, username, landingTerm, landingCommunityUser);
    }

    public void ApplyLanding(string? term, string? communityUser = null)
        => _editor.ApplyLanding(term, communityUser);

    /// <summary>
    /// Lands the browse view on the given source term (opened via the "Create Termbase
    /// Entry" context menu); authoring itself happens in the rich editor.
    /// </summary>
    public void PreFillNewEntry(string sourceTerm) => _editor.PreFillNewEntry(sourceTerm);

    /// <summary>Reloads the dictionary from disk (e.g. after the rich editor saved).</summary>
    public void Reload() => _editor.Reload();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
