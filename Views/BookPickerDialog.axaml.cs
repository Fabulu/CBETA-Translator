using Avalonia.Controls;
using Avalonia.Input;
using ReadZen.App.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReadZen.App.Views;

public partial class BookPickerDialog : Window
{
    public sealed class BookEntry
    {
        public string RelPath { get; init; } = "";
        public string DisplayShort { get; init; } = "";
        public string Subtitle { get; init; } = "";
        public string Tooltip { get; init; } = "";
    }

    private readonly List<BookEntry> _allBooks;
    private List<BookEntry> _filtered;
    public BookEntry? Result { get; private set; }

    public BookPickerDialog(IEnumerable<FileNavItem> items)
    {
        InitializeComponent();

        _allBooks = items
            .Where(f => !string.IsNullOrWhiteSpace(f.DisplayShort))
            .Select(f =>
            {
                var tooltip = f.Tooltip ?? "";
                var parts = tooltip.Split('\n', 2);
                var subtitle = parts.Length > 1 ? parts[1] : f.RelPath;
                return new BookEntry
                {
                    RelPath = f.RelPath,
                    DisplayShort = f.DisplayShort,
                    Subtitle = subtitle,
                    Tooltip = tooltip
                };
            })
            .OrderBy(b => b.DisplayShort)
            .ToList();
        _filtered = new List<BookEntry>(_allBooks);

        var lst = this.FindControl<ListBox>("LstBooks");
        var txt = this.FindControl<TextBox>("TxtSearch");
        var btnOk = this.FindControl<Button>("BtnOk");
        var btnCancel = this.FindControl<Button>("BtnCancel");

        if (lst != null) lst.ItemsSource = _filtered;

        if (txt != null)
        {
            txt.TextChanged += (_, _) =>
            {
                var q = (txt.Text ?? "").Trim();
                _filtered = string.IsNullOrEmpty(q)
                    ? new List<BookEntry>(_allBooks)
                    : _allBooks.Where(b =>
                        b.DisplayShort.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        b.Subtitle.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        b.Tooltip.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        b.RelPath.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (lst != null) lst.ItemsSource = _filtered;
            };
        }

        if (btnOk != null) btnOk.Click += (_, _) => TryConfirm(lst);
        if (btnCancel != null) btnCancel.Click += (_, _) => { Result = null; Close(null as object); };

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { Result = null; Close(null as object); e.Handled = true; }
            if (e.Key == Key.Return) { TryConfirm(lst); e.Handled = true; }
        };

        Opened += (_, _) => txt?.Focus();

        if (lst != null && _filtered.Count > 0) lst.SelectedIndex = 0;
    }

    private void TryConfirm(ListBox? lst)
    {
        if (lst?.SelectedItem is BookEntry entry)
        {
            Result = entry;
            Close(Result);
        }
    }
}
