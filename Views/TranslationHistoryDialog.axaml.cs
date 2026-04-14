// Views/TranslationHistoryDialog.axaml.cs
// Shows git history for a translation file. User can preview and restore old versions.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.App.Views;

public partial class TranslationHistoryDialog : Window
{
    private IGitRepoService? _git;
    private string? _repoDir;
    private string? _relPath;
    private string? _selectedContent;
    private GitCommitEntry? _selectedCommit;
    private readonly ListBox _lstCommits;
    private readonly TextBlock _txtPreview;
    private readonly TextBlock _txtPreviewHeader;
    private readonly Button _btnRestore;

    /// <summary>
    /// The content of the selected historical version, ready to write to disk.
    /// Null if the user cancelled or no version was selected.
    /// </summary>
    public string? RestoredContent { get; private set; }

    public TranslationHistoryDialog()
    {
        InitializeComponent();

        _lstCommits = this.FindControl<ListBox>("LstCommits")!;
        _txtPreview = this.FindControl<TextBlock>("TxtPreview")!;
        _txtPreviewHeader = this.FindControl<TextBlock>("TxtPreviewHeader")!;
        _btnRestore = this.FindControl<Button>("BtnRestore")!;

        _lstCommits.SelectionChanged += OnCommitSelected;
        _btnRestore.Click += OnRestoreClick;

        var btnClose = this.FindControl<Button>("BtnClose")!;
        btnClose.Click += (_, _) => Close();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Loads the commit history for the given file and populates the dialog.
    /// </summary>
    public async void LoadHistory(
        IGitRepoService git,
        string repoDir,
        string relPath,
        string? displayFileName = null)
    {
        try
        {
            await LoadHistoryCore(git, repoDir, relPath, displayFileName);
        }
        catch (Exception ex)
        {
            _txtPreviewHeader.Text = $"Error: {ex.Message}";
        }
    }

    private async Task LoadHistoryCore(
        IGitRepoService git,
        string repoDir,
        string relPath,
        string? displayFileName)
    {
        _git = git;
        _repoDir = repoDir;
        _relPath = relPath;

        var txtFileName = this.FindControl<TextBlock>("TxtFileName");
        var txtFileInfo = this.FindControl<TextBlock>("TxtFileInfo");
        if (txtFileName != null) txtFileName.Text = displayFileName ?? System.IO.Path.GetFileName(relPath);
        if (txtFileInfo != null) txtFileInfo.Text = relPath;

        List<GitCommitEntry> commits;
        try
        {
            commits = await git.GetFileLogAsync(repoDir, relPath, 50, CancellationToken.None);
        }
        catch
        {
            _txtPreviewHeader.Text = "Could not load git history.";
            return;
        }

        if (commits.Count == 0)
        {
            _txtPreviewHeader.Text = "No commits found for this file.";
            return;
        }

        var items = new List<ListBoxItem>();
        for (int i = 0; i < commits.Count; i++)
        {
            var c = commits[i];
            var label = i == 0 ? "(latest) " : "";
            items.Add(new ListBoxItem
            {
                Content = $"{label}{c.DateDisplay}  {c.Author}\n{c.Subject}",
                Tag = c,
                FontSize = 11,
                Padding = new Avalonia.Thickness(6, 4),
            });
        }

        _lstCommits.ItemsSource = items;
    }

    private async void OnCommitSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_lstCommits.SelectedItem is not ListBoxItem item || item.Tag is not GitCommitEntry commit)
            return;

        if (_git == null || _repoDir == null || _relPath == null)
            return;

        _selectedCommit = commit;
        _txtPreviewHeader.Text = $"Version from {commit.DateDisplay} by {commit.Author}";
        _txtPreview.Text = "Loading...";
        _btnRestore.IsEnabled = false;

        try
        {
            var content = await _git.GetFileAtCommitAsync(_repoDir, commit.Hash, _relPath, CancellationToken.None);
            if (content == null)
            {
                _txtPreview.Text = "(file did not exist at this commit)";
                _selectedContent = null;
                return;
            }

            _selectedContent = content;
            // Show a truncated preview (first 3000 chars) to keep UI responsive
            _txtPreview.Text = content.Length > 3000
                ? content[..3000] + "\n\n... (truncated)"
                : content;
            _btnRestore.IsEnabled = true;
        }
        catch (Exception ex)
        {
            _txtPreview.Text = $"Error loading version: {ex.Message}";
            _selectedContent = null;
        }
    }

    private void OnRestoreClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_selectedContent == null) return;
        RestoredContent = _selectedContent;
        Close();
    }
}
