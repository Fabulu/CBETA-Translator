using Avalonia.Controls;
using Avalonia.Input;
using ReadZen.App.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReadZen.App.Views;

public partial class CollectionPickerDialog : Window
{
    private readonly List<ScholarCollection> _allCollections;
    private List<ScholarCollection> _filtered;
    public ScholarCollection? Result { get; private set; }
    public string? PassageSummary { get; set; }

    public CollectionPickerDialog(IEnumerable<ScholarCollection> collections)
    {
        InitializeComponent();
        _allCollections = collections.OrderBy(c => c.Name).ToList();
        _filtered = new List<ScholarCollection>(_allCollections);

        var lst = this.FindControl<ListBox>("LstCollections");
        var txt = this.FindControl<TextBox>("TxtSearch");
        var btnOk = this.FindControl<Button>("BtnOk");
        var btnCancel = this.FindControl<Button>("BtnCancel");
        var btnNew = this.FindControl<Button>("BtnNewCollection");

        UpdateListSource(lst);

        if (txt != null)
        {
            txt.TextChanged += (_, _) =>
            {
                var q = (txt.Text ?? "").Trim();
                _filtered = string.IsNullOrEmpty(q)
                    ? new List<ScholarCollection>(_allCollections)
                    : _allCollections.Where(c => c.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
                UpdateListSource(lst);
            };
        }

        if (btnNew != null)
        {
            btnNew.Click += (_, _) =>
            {
                var baseName = "New Collection";
                var name = baseName;
                int counter = 2;
                while (_allCollections.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    name = $"{baseName} {counter++}";
                }

                var newCol = new ScholarCollection
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = name,
                    CreatedUtc = DateTimeOffset.UtcNow
                };
                _allCollections.Add(newCol);
                _allCollections.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

                // Reset filter
                if (txt != null) txt.Text = "";
                _filtered = new List<ScholarCollection>(_allCollections);
                UpdateListSource(lst);

                // Select the new one
                if (lst != null)
                {
                    var idx = _filtered.IndexOf(newCol);
                    if (idx >= 0) lst.SelectedIndex = idx;
                }
            };
        }

        if (btnOk != null) btnOk.Click += (_, _) => TryConfirm(lst);
        if (btnCancel != null) btnCancel.Click += (_, _) => { Result = null; Close(null as object); };

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { Result = null; Close(null as object); e.Handled = true; }
            if (e.Key == Key.Return) { TryConfirm(lst); e.Handled = true; }
        };

        var txtSummary = this.FindControl<TextBox>("TxtSummary");
        Opened += (_, _) =>
        {
            if (txtSummary != null)
                txtSummary.Text = PassageSummary ?? "";
            txt?.Focus();
        };

        // Pre-select first item if available
        if (lst != null && _filtered.Count > 0) lst.SelectedIndex = 0;
    }

    private void UpdateListSource(ListBox? lst)
    {
        if (lst == null) return;
        lst.ItemsSource = _filtered.Select(c => c.Name).ToList();
    }

    private void TryConfirm(ListBox? lst)
    {
        if (lst?.SelectedIndex >= 0 && lst.SelectedIndex < _filtered.Count)
        {
            var txtSummary = this.FindControl<TextBox>("TxtSummary");
            PassageSummary = string.IsNullOrWhiteSpace(txtSummary?.Text) ? null : txtSummary.Text.Trim();
            Result = _filtered[lst.SelectedIndex];
            Close(Result);
        }
    }
}
