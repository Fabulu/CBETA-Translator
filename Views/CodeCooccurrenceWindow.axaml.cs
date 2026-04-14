using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.App.Views;

/// <summary>
/// Shows co-occurrence matrix info and provides Open in Browser + Export CSV buttons.
/// </summary>
public partial class CodeCooccurrenceWindow : Window
{
    private CodeCooccurrenceMatrix? _matrix;

    public CodeCooccurrenceWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Loads the co-occurrence data.
    /// </summary>
    public void LoadData(List<DocumentTag> tags, TagVocabulary vocab)
    {
        _matrix = CodeCooccurrenceService.Compute(tags, vocab);

        var txtInfo = this.FindControl<TextBlock>("TxtInfo");
        if (txtInfo != null)
            txtInfo.Text = $"Co-occurrence matrix: {_matrix.CodeIds.Count} codes across your tags.";

        var btnBrowser = this.FindControl<Button>("BtnOpenBrowser");
        if (btnBrowser != null)
            btnBrowser.Click += (_, _) => OpenInBrowser();

        var btnCsv = this.FindControl<Button>("BtnExportCsv");
        if (btnCsv != null)
            btnCsv.Click += async (_, _) => await ExportCsvAsync();

        var btnClose = this.FindControl<Button>("BtnClose");
        if (btnClose != null)
            btnClose.Click += (_, _) => Close();
    }

    private void OpenInBrowser()
    {
        if (_matrix == null) return;

        var html = CodeCooccurrenceService.BuildHtml(_matrix);
        var tmpPath = Path.Combine(Path.GetTempPath(), $"readzen-cooccurrence-{Guid.NewGuid():N}.html");
        File.WriteAllText(tmpPath, html);

        try
        {
            Process.Start(new ProcessStartInfo(tmpPath) { UseShellExecute = true });
        }
        catch
        {
            // Browser launch failed — silently ignore
        }
    }

    private async System.Threading.Tasks.Task ExportCsvAsync()
    {
        if (_matrix == null) return;

        var sp = GetTopLevel(this)?.StorageProvider;
        if (sp == null) return;

        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Co-occurrence CSV",
            DefaultExtension = "csv",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } }
            }
        });

        if (file == null) return;

        var csv = CodeCooccurrenceService.ExportCsv(_matrix);
        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(csv);
    }
}
