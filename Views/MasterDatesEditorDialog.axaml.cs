using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace CbetaTranslator.App.Views;

public partial class MasterDatesEditorDialog : Window
{
    private readonly string _filePath;
    private List<MasterEntry> _masters = new();
    private MasterEntry? _selected;

    public bool Saved { get; private set; }

    public MasterDatesEditorDialog() : this(
        Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "master-dates.json"))
    {
    }

    public MasterDatesEditorDialog(string filePath)
    {
        _filePath = filePath;
        InitializeComponent();

        LoadFromFile();
        RefreshList();

        var txtFilter = this.FindControl<TextBox>("TxtFilter");
        if (txtFilter != null)
            txtFilter.TextChanged += OnFilterChanged;

        var masterList = this.FindControl<ListBox>("MasterList");
        if (masterList != null)
        {
            masterList.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<MasterEntry>(
                (entry, _) =>
                {
                    var sp = new StackPanel { Spacing = 1 };
                    sp.Children.Add(new TextBlock
                    {
                        Text = entry.PrimaryName,
                        FontWeight = Avalonia.Media.FontWeight.SemiBold
                    });
                    sp.Children.Add(new TextBlock
                    {
                        Text = entry.DatesSummary,
                        FontSize = 11,
                        Opacity = 0.7
                    });
                    return sp;
                });
            masterList.SelectionChanged += OnSelectionChanged;
        }
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    // ----- Data -----

    private void LoadFromFile()
    {
        _masters.Clear();
        if (!File.Exists(_filePath)) return;

        try
        {
            var json = File.ReadAllText(_filePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("masters", out var mastersEl)) return;

            foreach (var m in mastersEl.EnumerateArray())
            {
                var names = new List<string>();
                if (m.TryGetProperty("names", out var namesEl))
                {
                    foreach (var n in namesEl.EnumerateArray())
                    {
                        var s = n.GetString();
                        if (!string.IsNullOrEmpty(s)) names.Add(s);
                    }
                }

                int floruit = m.TryGetProperty("floruit", out var f) ? f.GetInt32() : 0;
                int death = m.TryGetProperty("death", out var d) ? d.GetInt32() : 0;

                _masters.Add(new MasterEntry { Names = names, Floruit = floruit, Death = death });
            }
        }
        catch
        {
            // If the file is corrupt, start empty
        }
    }

    private void SaveToFile()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var wrapper = new MasterDatesRoot { Masters = _masters };
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var json = JsonSerializer.Serialize(wrapper, options);

            // UTF-8 no BOM
            File.WriteAllText(_filePath, json, new UTF8Encoding(false));
            Saved = true;

            SetStatus($"Saved {_masters.Count} master(s).");
        }
        catch (Exception ex)
        {
            SetStatus("Save failed: " + ex.Message);
        }
    }

    // ----- UI sync -----

    private void RefreshList(string? filter = null)
    {
        var list = this.FindControl<ListBox>("MasterList");
        if (list == null) return;

        IEnumerable<MasterEntry> source = _masters;

        if (!string.IsNullOrWhiteSpace(filter))
        {
            source = source.Where(m =>
                m.Names.Any(n => n.Contains(filter, StringComparison.OrdinalIgnoreCase)));
        }

        var sorted = source.OrderBy(m => m.Floruit).ToList();
        list.ItemsSource = sorted;

        if (_selected != null && sorted.Contains(_selected))
            list.SelectedItem = _selected;
        else if (sorted.Count > 0)
            list.SelectedItem = sorted[0];
    }

    private void SyncToEditor()
    {
        var txtNames = this.FindControl<TextBox>("TxtNames");
        var txtFloruit = this.FindControl<TextBox>("TxtFloruit");
        var txtDeath = this.FindControl<TextBox>("TxtDeath");

        if (_selected == null)
        {
            if (txtNames != null) txtNames.Text = "";
            if (txtFloruit != null) txtFloruit.Text = "";
            if (txtDeath != null) txtDeath.Text = "";
            return;
        }

        if (txtNames != null) txtNames.Text = string.Join("\n", _selected.Names);
        if (txtFloruit != null) txtFloruit.Text = _selected.Floruit > 0 ? _selected.Floruit.ToString() : "";
        if (txtDeath != null) txtDeath.Text = _selected.Death > 0 ? _selected.Death.ToString() : "";
    }

    private void SyncFromEditor()
    {
        if (_selected == null) return;

        var txtNames = this.FindControl<TextBox>("TxtNames");
        var txtFloruit = this.FindControl<TextBox>("TxtFloruit");
        var txtDeath = this.FindControl<TextBox>("TxtDeath");

        if (txtNames != null)
        {
            _selected.Names = (txtNames.Text ?? "")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => s.Length > 0)
                .ToList();
        }

        if (txtFloruit != null)
            _selected.Floruit = int.TryParse(txtFloruit.Text, out var fv) ? fv : 0;

        if (txtDeath != null)
            _selected.Death = int.TryParse(txtDeath.Text, out var dv) ? dv : 0;
    }

    private void SetStatus(string msg)
    {
        var txt = this.FindControl<TextBlock>("TxtStatus");
        if (txt != null) txt.Text = msg;
    }

    // ----- Event handlers -----

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Save edits from previous selection
        SyncFromEditor();

        var list = this.FindControl<ListBox>("MasterList");
        _selected = list?.SelectedItem as MasterEntry;
        SyncToEditor();
    }

    private void OnFilterChanged(object? sender, TextChangedEventArgs e)
    {
        SyncFromEditor();
        var filter = (sender as TextBox)?.Text?.Trim() ?? "";
        RefreshList(string.IsNullOrEmpty(filter) ? null : filter);
    }

    private void OnNewClick(object? sender, RoutedEventArgs e)
    {
        SyncFromEditor();
        var entry = new MasterEntry
        {
            Names = new List<string> { "New Master" },
            Floruit = 800,
            Death = 860
        };
        _masters.Add(entry);
        _selected = entry;

        var filter = this.FindControl<TextBox>("TxtFilter")?.Text?.Trim();
        RefreshList(string.IsNullOrEmpty(filter) ? null : filter);

        var list = this.FindControl<ListBox>("MasterList");
        if (list != null) list.SelectedItem = entry;
        SyncToEditor();

        // Focus the names field
        this.FindControl<TextBox>("TxtNames")?.Focus();
        SetStatus("New master added. Edit names and dates, then Save.");
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (_selected == null) return;
        _masters.Remove(_selected);
        _selected = null;

        var filter = this.FindControl<TextBox>("TxtFilter")?.Text?.Trim();
        RefreshList(string.IsNullOrEmpty(filter) ? null : filter);
        SetStatus("Master deleted. Click Save to persist.");
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        SyncFromEditor();
        SaveToFile();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    // ----- Inner types -----

    internal sealed class MasterEntry
    {
        [JsonPropertyName("names")]
        public List<string> Names { get; set; } = new();

        [JsonPropertyName("floruit")]
        public int Floruit { get; set; }

        [JsonPropertyName("death")]
        public int Death { get; set; }

        [JsonIgnore]
        public string PrimaryName => Names.Count > 0 ? Names[0] : "(unnamed)";

        [JsonIgnore]
        public string DatesSummary
        {
            get
            {
                if (Floruit > 0 && Death > 0) return $"fl. {Floruit}, d. {Death}";
                if (Floruit > 0) return $"fl. {Floruit}";
                if (Death > 0) return $"d. {Death}";
                return "";
            }
        }
    }

    private sealed class MasterDatesRoot
    {
        [JsonPropertyName("masters")]
        public List<MasterEntry> Masters { get; set; } = new();
    }
}
