using Avalonia.Controls;
using Avalonia.Input;
using ReadZen.App.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReadZen.App.Views;

public partial class PassagePickerDialog : Window
{
    private readonly List<ScholarPassage> _allPassages;
    private List<ScholarPassage> _filtered;

    public ScholarPassage? Result { get; private set; }

    public PassagePickerDialog(IReadOnlyList<ScholarPassage> passages)
    {
        InitializeComponent();
        _allPassages = passages.ToList();
        _filtered = _allPassages;

        var lst = this.FindControl<ListBox>("LstPassages");
        var txt = this.FindControl<TextBox>("TxtSearch");
        var btnOk = this.FindControl<Button>("BtnOk");
        var btnCancel = this.FindControl<Button>("BtnCancel");

        if (lst != null)
        {
            lst.ItemsSource = _filtered.Select(p => p.DisplayTitle).ToList();
        }

        if (txt != null)
        {
            txt.TextChanged += (_, _) =>
            {
                var query = (txt.Text ?? "").ToLowerInvariant().Trim();
                _filtered = string.IsNullOrEmpty(query)
                    ? _allPassages
                    : _allPassages.Where(p =>
                        (p.DisplayTitle?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (p.ZhText?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (p.EnText?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        p.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                if (lst != null)
                    lst.ItemsSource = _filtered.Select(p => p.DisplayTitle).ToList();
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
    }

    private void TryConfirm(ListBox? lst)
    {
        if (lst?.SelectedIndex >= 0 && lst.SelectedIndex < _filtered.Count)
        {
            Result = _filtered[lst.SelectedIndex];
            Close(Result);
        }
    }
}
