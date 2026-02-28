using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Views;

public partial class TermbaseEditorWindow : Window
{
    private readonly string _root;
    private readonly TermbaseStorageService _storage = new();

    private TextBox? _txtSearch;
    private ListBox? _lstTerms;

    private TextBox? _txtSourceTerm;
    private TextBox? _txtPreferredTarget;
    private ComboBox? _cmbStatus;
    private TextBox? _txtAlternates;
    private TextBox? _txtNote;
    private TextBlock? _txtEditorStatus;

    private Button? _btnNew;
    private Button? _btnDelete;
    private Button? _btnDuplicate;
    private Button? _btnCancel;
    private Button? _btnSave;

    private readonly ObservableCollection<TermbaseEntry> _allEntries = new();
    private readonly ObservableCollection<TermbaseEntry> _visibleEntries = new();

    private bool _suppressSelectionChanged;
    private bool _suppressFieldEvents;
    private TermbaseEntry? _currentEntry;

    public bool Saved { get; private set; }

    public TermbaseEditorWindow(string root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));

        InitializeComponent();
        FindControls();
        WireEvents();

        if (_lstTerms != null)
            _lstTerms.ItemsSource = _visibleEntries;

        Opened += async (_, _) => await LoadAsync();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private T? Find<T>(string name) where T : Control => this.FindControl<T>(name);

    private void FindControls()
    {
        _txtSearch = Find<TextBox>("TxtSearch");
        _lstTerms = Find<ListBox>("LstTerms");

        _txtSourceTerm = Find<TextBox>("TxtSourceTerm");
        _txtPreferredTarget = Find<TextBox>("TxtPreferredTarget");
        _cmbStatus = Find<ComboBox>("CmbStatus");
        _txtAlternates = Find<TextBox>("TxtAlternates");
        _txtNote = Find<TextBox>("TxtNote");
        _txtEditorStatus = Find<TextBlock>("TxtEditorStatus");

        _btnNew = Find<Button>("BtnNew");
        _btnDelete = Find<Button>("BtnDelete");
        _btnDuplicate = Find<Button>("BtnDuplicate");
        _btnCancel = Find<Button>("BtnCancel");
        _btnSave = Find<Button>("BtnSave");
    }

    private void WireEvents()
    {
        if (_txtSearch != null)
            _txtSearch.TextChanged += (_, _) => ApplyFilter();

        if (_lstTerms != null)
            _lstTerms.SelectionChanged += LstTerms_SelectionChanged;

        if (_txtSourceTerm != null)
            _txtSourceTerm.TextChanged += (_, _) => PushFieldsIntoCurrentEntry();

        if (_txtPreferredTarget != null)
            _txtPreferredTarget.TextChanged += (_, _) => PushFieldsIntoCurrentEntry();

        if (_cmbStatus != null)
            _cmbStatus.SelectionChanged += (_, _) => PushFieldsIntoCurrentEntry();

        if (_txtAlternates != null)
            _txtAlternates.TextChanged += (_, _) => PushFieldsIntoCurrentEntry();

        if (_txtNote != null)
            _txtNote.TextChanged += (_, _) => PushFieldsIntoCurrentEntry();

        if (_btnNew != null)
            _btnNew.Click += BtnNew_Click;

        if (_btnDelete != null)
            _btnDelete.Click += BtnDelete_Click;

        if (_btnDuplicate != null)
            _btnDuplicate.Click += BtnDuplicate_Click;

        if (_btnCancel != null)
            _btnCancel.Click += (_, _) => Close(false);

        if (_btnSave != null)
            _btnSave.Click += async (_, _) => await SaveAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var entries = await _storage.LoadAsync(_root);

            _allEntries.Clear();
            foreach (var entry in entries ?? new List<TermbaseEntry>())
                _allEntries.Add(NormalizeEntry(entry));

            ApplyFilter();

            if (_visibleEntries.Count > 0)
            {
                _currentEntry = _visibleEntries[0];

                if (_lstTerms != null)
                    _lstTerms.SelectedItem = _currentEntry;
            }
            else
            {
                _currentEntry = null;
            }

            LoadCurrentEntryIntoFields();
            SetEditorStatus($"Loaded {_allEntries.Count:n0} term(s).");
        }
        catch (Exception ex)
        {
            SetEditorStatus("Load failed: " + ex.Message);
        }
    }

    private void ApplyFilter()
    {
        string q = (_txtSearch?.Text ?? "").Trim();

        IEnumerable<TermbaseEntry> seq = _allEntries;

        if (q.Length > 0)
        {
            seq = seq.Where(x =>
                (x.SourceTerm?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.PreferredTarget?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.Note?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.AlternateTargets?.Any(a => a.Contains(q, StringComparison.OrdinalIgnoreCase)) ?? false));
        }

        var filtered = seq
            .OrderBy(x => x.SourceTerm ?? "", StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.PreferredTarget ?? "", StringComparer.OrdinalIgnoreCase)
            .ToList();

        _visibleEntries.Clear();
        foreach (var entry in filtered)
            _visibleEntries.Add(entry);

        if (_lstTerms == null)
            return;

        try
        {
            _suppressSelectionChanged = true;

            if (_currentEntry != null && _visibleEntries.Contains(_currentEntry))
            {
                _lstTerms.SelectedItem = _currentEntry;
            }
            else if (_visibleEntries.Count > 0)
            {
                _currentEntry = _visibleEntries[0];
                _lstTerms.SelectedItem = _currentEntry;
            }
            else
            {
                _currentEntry = null;
                _lstTerms.SelectedItem = null;
            }
        }
        finally
        {
            _suppressSelectionChanged = false;
        }

        LoadCurrentEntryIntoFields();
    }

    private void LstTerms_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionChanged)
            return;

        _currentEntry = _lstTerms?.SelectedItem as TermbaseEntry;
        LoadCurrentEntryIntoFields();
    }

    private void LoadCurrentEntryIntoFields()
    {
        try
        {
            _suppressFieldEvents = true;

            if (_currentEntry == null)
            {
                if (_txtSourceTerm != null) _txtSourceTerm.Text = "";
                if (_txtPreferredTarget != null) _txtPreferredTarget.Text = "";
                if (_txtAlternates != null) _txtAlternates.Text = "";
                if (_txtNote != null) _txtNote.Text = "";
                if (_cmbStatus != null) _cmbStatus.SelectedIndex = 0;
                return;
            }

            if (_txtSourceTerm != null) _txtSourceTerm.Text = _currentEntry.SourceTerm ?? "";
            if (_txtPreferredTarget != null) _txtPreferredTarget.Text = _currentEntry.PreferredTarget ?? "";
            if (_txtAlternates != null) _txtAlternates.Text = string.Join(Environment.NewLine, _currentEntry.AlternateTargets ?? new List<string>());
            if (_txtNote != null) _txtNote.Text = _currentEntry.Note ?? "";

            if (_cmbStatus != null)
            {
                string status = (_currentEntry.Status ?? "preferred").Trim().ToLowerInvariant();
                _cmbStatus.SelectedIndex = status switch
                {
                    "preferred" => 0,
                    "allowed" => 1,
                    "deprecated" => 2,
                    "forbidden" => 3,
                    _ => 0
                };
            }
        }
        finally
        {
            _suppressFieldEvents = false;
        }
    }

    private void PushFieldsIntoCurrentEntry()
    {
        if (_suppressFieldEvents || _currentEntry == null)
            return;

        _currentEntry.SourceTerm = _txtSourceTerm?.Text?.Trim() ?? "";
        _currentEntry.PreferredTarget = _txtPreferredTarget?.Text?.Trim() ?? "";
        _currentEntry.Note = _txtNote?.Text?.Trim() ?? "";
        _currentEntry.Status = GetSelectedStatus();

        _currentEntry.AlternateTargets = (_txtAlternates?.Text ?? "")
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        RefreshCurrentListItemTextOnly();
    }

    private void RefreshCurrentListItemTextOnly()
    {
        if (_currentEntry == null || _lstTerms == null)
            return;

        var selected = _lstTerms.SelectedItem;
        var items = _lstTerms.ItemsSource;

        try
        {
            _suppressSelectionChanged = true;
            _lstTerms.ItemsSource = null;
            _lstTerms.ItemsSource = items;
            _lstTerms.SelectedItem = selected ?? _currentEntry;
        }
        finally
        {
            _suppressSelectionChanged = false;
        }
    }

    private void BtnNew_Click(object? sender, RoutedEventArgs e)
    {
        var entry = new TermbaseEntry
        {
            SourceTerm = "新語",
            PreferredTarget = "",
            Status = "preferred",
            Note = "",
            AlternateTargets = new List<string>()
        };

        _allEntries.Add(entry);
        _currentEntry = entry;

        ApplyFilter();

        if (_lstTerms != null)
            _lstTerms.SelectedItem = entry;

        LoadCurrentEntryIntoFields();
        _txtSourceTerm?.Focus();
        SetEditorStatus("New term created.");
    }

    private void BtnDelete_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentEntry == null)
            return;

        var entry = _currentEntry;
        _currentEntry = null;

        _allEntries.Remove(entry);

        ApplyFilter();
        SetEditorStatus("Term deleted.");
    }

    private void BtnDuplicate_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentEntry == null)
            return;

        var copy = new TermbaseEntry
        {
            SourceTerm = _currentEntry.SourceTerm,
            PreferredTarget = _currentEntry.PreferredTarget,
            Status = _currentEntry.Status,
            Note = _currentEntry.Note,
            AlternateTargets = (_currentEntry.AlternateTargets ?? new List<string>()).ToList()
        };

        _allEntries.Add(copy);
        _currentEntry = copy;

        ApplyFilter();

        if (_lstTerms != null)
            _lstTerms.SelectedItem = copy;

        LoadCurrentEntryIntoFields();
        SetEditorStatus("Term duplicated.");
    }

    private async Task SaveAsync()
    {
        try
        {
            PushFieldsIntoCurrentEntry();

            var cleaned = _allEntries
                .Select(NormalizeEntry)
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.SourceTerm) ||
                    !string.IsNullOrWhiteSpace(x.PreferredTarget) ||
                    !string.IsNullOrWhiteSpace(x.Note) ||
                    (x.AlternateTargets?.Count ?? 0) > 0)
                .ToList();

            var bad = cleaned.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.SourceTerm));
            if (bad != null)
            {
                SetEditorStatus("Save blocked: every non-empty term needs a source term.");
                return;
            }

            await _storage.SaveAsync(_root, cleaned);

            _allEntries.Clear();
            foreach (var entry in cleaned)
                _allEntries.Add(entry);

            Saved = true;
            SetEditorStatus($"Saved {cleaned.Count:n0} terms.");
            Close(true);
        }
        catch (Exception ex)
        {
            SetEditorStatus("Save failed: " + ex.Message);
        }
    }

    private string GetSelectedStatus()
    {
        if (_cmbStatus?.SelectedItem is ComboBoxItem cbi)
            return cbi.Content?.ToString()?.Trim() ?? "preferred";

        return _cmbStatus?.SelectedItem?.ToString()?.Trim() ?? "preferred";
    }

    private void SetEditorStatus(string message)
    {
        if (_txtEditorStatus != null)
            _txtEditorStatus.Text = message;
    }

    private static TermbaseEntry NormalizeEntry(TermbaseEntry? entry)
    {
        entry ??= new TermbaseEntry();

        entry.SourceTerm = (entry.SourceTerm ?? "").Trim();
        entry.PreferredTarget = (entry.PreferredTarget ?? "").Trim();
        entry.Note = (entry.Note ?? "").Trim();
        entry.Status = string.IsNullOrWhiteSpace(entry.Status) ? "preferred" : entry.Status.Trim();

        entry.AlternateTargets = (entry.AlternateTargets ?? new List<string>())
            .Select(x => (x ?? "").Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return entry;
    }
}