using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReadZen.App.Views;

public partial class MasterPickerDialog : Window
{
    private readonly List<string> _allMasters;
    private List<string> _filtered;
    public string? Result { get; private set; }

    public MasterPickerDialog(IEnumerable<string> masterNames)
    {
        InitializeComponent();
        _allMasters = masterNames.OrderBy(n => n).ToList();
        _filtered = _allMasters;

        var lst = this.FindControl<ListBox>("LstMasters");
        var txt = this.FindControl<TextBox>("TxtSearch");
        var btnOk = this.FindControl<Button>("BtnOk");
        var btnCancel = this.FindControl<Button>("BtnCancel");

        if (lst != null) lst.ItemsSource = _filtered;

        if (txt != null)
        {
            txt.TextChanged += (_, _) =>
            {
                var q = (txt.Text ?? "").Trim();
                _filtered = string.IsNullOrEmpty(q) ? _allMasters
                    : _allMasters.Where(m => m.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
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
