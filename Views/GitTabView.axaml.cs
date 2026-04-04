using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CbetaTranslator.App.Views;

public partial class GitTabView : UserControl
{
    private readonly GitTabViewModel _vm;
    private DeviceCodeDialog? _activeDeviceCodeDialog;

    public event EventHandler<string>? Status;
    public event EventHandler<string>? RootCloned;
    public event EventHandler? CommunityDataFetched;
    public event Func<Task>? PrepareCommunityShareRequested;
    public event Func<string, Task<bool>>? EnsureTranslatedForSelectedRequested;

    public GitTabView()
    {
        InitializeComponent();

        _vm = new GitTabViewModel(
            App.Services.GetRequiredService<IGitRepoService>(),
            App.Services.GetRequiredService<IGitHubAuthService>(),
            App.Services.GetRequiredService<IGitHubApiService>(),
            App.Services.GetRequiredService<ICommunityDataService>(),
            App.Services.GetRequiredService<IScholarCollectionsService>(),
            App.Services.GetRequiredService<ITermbaseStorageService>(),
            App.Services.GetRequiredService<ITranslationReviewService>(),
            App.Services.GetRequiredService<IMasterDatesService>(),
            App.Services.GetRequiredService<IDocumentTagService>());

        DataContext = _vm;

        // Wire bridge delegates
        _vm.PickFolderAsync = PickFolderBridgeAsync;
        _vm.ConfirmAsync = ConfirmBridgeAsync;
        _vm.ScrollLogToEnd = ScrollLogToEnd;
        _vm.ShowDeviceCodeAsync = ShowDeviceCodeBridgeAsync;
        _vm.DeviceFlowCompleted += (_, _) => Dispatcher.UIThread.Post(() => _activeDeviceCodeDialog?.DismissIfOpen());

        // Forward VM events to code-behind events (for MainWindow)
        _vm.StatusChanged += (_, msg) => Status?.Invoke(this, msg);
        _vm.RootCloned += (_, root) => RootCloned?.Invoke(this, root);
        _vm.CommunityDataFetched += (_, _) => CommunityDataFetched?.Invoke(this, EventArgs.Empty);
        _vm.PrepareCommunityShareRequested += () =>
            PrepareCommunityShareRequested?.Invoke() ?? Task.CompletedTask;
        _vm.EnsureTranslatedForSelectedRequested += relPath =>
            EnsureTranslatedForSelectedRequested?.Invoke(relPath) ?? Task.FromResult(true);

        AttachedToVisualTree += (_, _) => _vm.OnAttachedToVisualTree();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    /// <summary>Programmatically triggers the full Sync command.</summary>
    public void TriggerSync()
    {
        if (_vm.SyncCommand.CanExecute(null))
            _vm.SyncCommand.Execute(null);
    }

    public Task TriggerInitialDownloadAsync() => _vm.StartInitialDownloadAsync();

    public void SetCurrentRepoRoot(string? rootPath) => _vm.SetCurrentRepoRoot(rootPath);
    public void SetSelectedRelPath(string? relPath) => _vm.SetSelectedRelPath(relPath);
    public void SetUsername(string? username) => _vm.SetUsername(username);
    public void LoadPersistedAuth(string? token, string? login) => _vm.LoadPersistedAuth(token, login);
    public event EventHandler<(string Token, string Login)>? GitHubAuthCompleted
    {
        add => _vm.GitHubAuthCompleted += value;
        remove => _vm.GitHubAuthCompleted -= value;
    }

    // ----- Bridge implementations (UI concerns) -----

    private async Task<string?> PickFolderBridgeAsync()
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner?.StorageProvider == null)
            return null;

        var picked = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select a folder where the repo will be stored"
        });

        var folder = picked.Count > 0 ? picked[0] : null;
        return folder?.Path.LocalPath;
    }

    private async Task<bool> ConfirmBridgeAsync(string title, string message, string yesText, string noText)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null) return false;

        var dlg = new ConfirmDialog(title, message, yesText, noText)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        return await dlg.ShowDialog<bool>(owner);
    }

    private void ScrollLogToEnd()
    {
        var txtLog = this.FindControl<TextBox>("TxtLog");
        if (txtLog?.Text != null)
        {
            try { txtLog.CaretIndex = txtLog.Text.Length; } catch { }
        }
    }

    private async Task ShowDeviceCodeBridgeAsync(string userCode, string verificationUri)
    {
        // Copy to clipboard
        bool copiedOk = false;
        var top = TopLevel.GetTopLevel(this);
        if (top?.Clipboard != null)
        {
            try
            {
                await top.Clipboard.SetTextAsync(userCode);
                copiedOk = true;
            }
            catch { }
        }

        // Show non-modal dialog
        var owner = top as Window;
        if (owner == null) return;
        _activeDeviceCodeDialog?.DismissIfOpen();
        _activeDeviceCodeDialog = new DeviceCodeDialog(userCode, copiedOk);
        _activeDeviceCodeDialog.Show(owner);
    }

    // ----- Confirm dialog (kept in code-behind as it's pure UI) -----

    private sealed class ConfirmDialog : Window
    {
        public ConfirmDialog(string title, string message, string yesText, string noText)
        {
            Title = title;
            Width = 600;
            Height = 340;
            CanResize = false;
            Topmost = false;

            var root = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto"),
                Margin = new Thickness(16),
                RowSpacing = 12
            };

            var header = new TextBlock
            {
                Text = title,
                FontSize = 20,
                FontWeight = FontWeight.SemiBold
            };

            var bodyBorder = new Border
            {
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                Background = Brushes.Transparent,
                Padding = new Thickness(12),
                Child = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 14
                    }
                }
            };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8
            };

            var btnNo = new Button { Content = noText, MinWidth = 170 };
            btnNo.Click += (_, _) => Close(false);

            var btnYes = new Button { Content = yesText, MinWidth = 250 };
            btnYes.Click += (_, _) => Close(true);

            buttons.Children.Add(btnNo);
            buttons.Children.Add(btnYes);

            root.Children.Add(header);
            Grid.SetRow(header, 0);

            root.Children.Add(bodyBorder);
            Grid.SetRow(bodyBorder, 1);

            root.Children.Add(buttons);
            Grid.SetRow(buttons, 2);

            Content = root;
        }
    }
}
