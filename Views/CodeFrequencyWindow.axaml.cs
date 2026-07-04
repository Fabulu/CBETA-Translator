using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.Infrastructure;

namespace ReadZen.App.Views;

/// <summary>
/// Shows a list of code frequency: tag name, color swatch, segment count, file count.
/// Supports CSV export.
/// </summary>
public partial class CodeFrequencyWindow : Window
{
    private CodeFrequencyReport? _report;

    public CodeFrequencyWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Loads the frequency data and populates the list.
    /// </summary>
    public void LoadData(List<DocumentTag> tags, TagVocabulary vocab)
    {
        _report = CodeFrequencyService.Compute(tags, vocab);

        var list = this.FindControl<ListBox>("FrequencyList");
        if (list != null)
            list.ItemsSource = _report.Rows.Select(r => new FrequencyRowVm(r)).ToList();

        var btnExport = this.FindControl<Button>("BtnExportCsv");
        if (btnExport != null)
            btnExport.Click += (_, _) => AsyncGuard.Run(async () => await ExportCsvAsync(), "CodeFrequencyWindow.btnExport.Click");

        var btnClose = this.FindControl<Button>("BtnClose");
        if (btnClose != null)
            btnClose.Click += (_, _) => Close();
    }

    private async System.Threading.Tasks.Task ExportCsvAsync()
    {
        if (_report == null) return;

        var sp = GetTopLevel(this)?.StorageProvider;
        if (sp == null) return;

        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Code Frequency CSV",
            DefaultExtension = "csv",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } }
            }
        });

        if (file == null) return;

        var csv = CodeFrequencyService.ExportCsv(_report);
        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(csv);
    }

    /// <summary>View model wrapper for ListBox binding.</summary>
    public sealed class FrequencyRowVm
    {
        private readonly CodeFrequencyRow _row;

        public FrequencyRowVm(CodeFrequencyRow row) => _row = row;

        public string TagName => _row.TagName;
        public int SegmentCount => _row.SegmentCount;
        public int FileCount => _row.FileCount;

        public IBrush ColorBrush
        {
            get
            {
                try { return new SolidColorBrush(Color.Parse(_row.Color)); }
                catch { return Brushes.Gray; }
            }
        }
    }
}
