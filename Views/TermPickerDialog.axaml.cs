using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReadZen.App.Views;

public sealed class TermDisplayItem
{
    /// <summary>Deterministic dictionary entry id (DictionaryStore.ComputeId of SourceTerm); may be empty.</summary>
    public string Id { get; set; } = "";
    public string SourceTerm { get; set; } = "";
    public string PreferredTarget { get; set; } = "";
    public List<string> AlternateTargets { get; set; } = new();
    public string Display => $"{SourceTerm} — {PreferredTarget}";
}

public partial class TermPickerDialog : Window
{
    private readonly List<TermDisplayItem> _allTerms;
    private List<TermDisplayItem> _filtered;
    public TermDisplayItem? Result { get; private set; }

    public TermPickerDialog(IEnumerable<TermDisplayItem> terms)
    {
        InitializeComponent();
        _allTerms = terms.OrderBy(t => t.SourceTerm).ToList();
        _filtered = _allTerms;

        var lst = this.FindControl<ListBox>("LstTerms");
        var txt = this.FindControl<TextBox>("TxtSearch");
        var btnOk = this.FindControl<Button>("BtnOk");
        var btnCancel = this.FindControl<Button>("BtnCancel");

        if (lst != null) lst.ItemsSource = _filtered.Select(t => t.Display).ToList();

        if (txt != null)
        {
            txt.TextChanged += (_, _) =>
            {
                var q = (txt.Text ?? "").Trim();
                _filtered = string.IsNullOrEmpty(q) ? _allTerms
                    : _allTerms.Where(t =>
                        t.SourceTerm.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        t.PreferredTarget.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        t.AlternateTargets.Any(a => a.Contains(q, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                if (lst != null) lst.ItemsSource = _filtered.Select(t => t.Display).ToList();
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
