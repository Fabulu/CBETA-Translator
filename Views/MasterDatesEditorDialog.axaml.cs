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
using Avalonia.Media;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;

namespace CbetaTranslator.App.Views;

public partial class MasterDatesEditorDialog : Window
{
    private readonly string _filePath;
    private readonly string? _repoRoot;
    private List<MasterEntry> _masters = new();
    private MasterEntry? _selected;

    // Base name set for detecting base vs custom entries
    private HashSet<string> _baseNames = new(StringComparer.Ordinal);

    // Community entries and conflicts
    private List<MasterEntry> _communityMasters = new();
    private List<MasterDateConflict> _conflicts = new();

    public bool Saved { get; private set; }

    public MasterDatesEditorDialog() : this(
        Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "master-dates.json"), null)
    {
    }

    public MasterDatesEditorDialog(string filePath, string? repoRoot = null)
    {
        _filePath = filePath;
        _repoRoot = repoRoot;
        InitializeComponent();

        LoadFromFile();
        LoadCommunityEntries();
        RefreshList();
        UpdateConflictDisplay();

        var txtFilter = this.FindControl<TextBox>("TxtFilter");
        if (txtFilter != null)
            txtFilter.TextChanged += OnFilterChanged;

        var masterList = this.FindControl<ListBox>("MasterList");
        if (masterList != null)
        {
            masterList.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<MasterEntry>(
                (entry, _) =>
                {
                    if (entry == null)
                        return new TextBlock { Text = "(missing master entry)", Opacity = 0.6 };

                    var sp = new StackPanel { Spacing = 1 };

                    var nameBlock = new TextBlock
                    {
                        Text = string.IsNullOrWhiteSpace(entry.PrimaryName) ? "(unnamed)" : entry.PrimaryName,
                        FontWeight = FontWeight.SemiBold
                    };

                    // Community entries get a distinct color
                    if (entry.IsBase)
                    {
                        // Base entries: normal text
                    }
                    else if (entry.IsCommunity)
                    {
                        nameBlock.Foreground = new SolidColorBrush(Color.FromRgb(80, 160, 220));
                    }
                    else
                    {
                        // Custom (user-added) entries: green tint
                        nameBlock.Foreground = new SolidColorBrush(Color.FromRgb(100, 200, 100));
                    }

                    sp.Children.Add(nameBlock);

                    var detailParts = new List<string>();
                    detailParts.Add(entry.DatesSummary);

                    if (entry.IsBase) detailParts.Add("[canonical]");
                    else if (entry.IsCommunity) detailParts.Add($"[community: {entry.CreatedBy ?? "?"}]");
                    else detailParts.Add("[custom]");

                    if (entry.HasConflict) detailParts.Add("[conflict]");

                    sp.Children.Add(new TextBlock
                    {
                        Text = string.Join("  ", detailParts.Where(s => s.Length > 0)),
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

        // Build base name set from the canonical file
        _baseNames = MasterDatesService.LoadBaseNameSet();

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

                var entry = new MasterEntry { Names = names, Floruit = floruit, Death = death };

                // Tag as base if any name overlaps with base names
                entry.IsBase = IsBaseEntry(names);

                _masters.Add(entry);
            }
        }
        catch
        {
            // If the file is corrupt, start empty
        }
    }

    private void LoadCommunityEntries()
    {
        _communityMasters.Clear();
        _conflicts.Clear();

        if (string.IsNullOrWhiteSpace(_repoRoot))
            return;

        try
        {
            var communityDir = IMasterDatesService.GetCommunityMasterDatesDir(_repoRoot);
            if (!Directory.Exists(communityDir))
                return;

            var readOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var allUserEntries = new List<(string Username, MasterDateEntry Entry)>();

            foreach (var file in Directory.GetFiles(communityDir, "*.jsonl"))
            {
                var username = Path.GetFileNameWithoutExtension(file);
                var lines = File.ReadAllLines(file, Encoding.UTF8);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var e = JsonSerializer.Deserialize<MasterDateEntry>(line, readOpts);
                        if (e != null)
                            allUserEntries.Add((username, e));
                    }
                    catch { }
                }
            }

            // Group by master identity (shared names), detect conflicts
            var addedNames = new HashSet<string>(StringComparer.Ordinal);
            var conflictMap = new Dictionary<string, MasterDateConflict>(StringComparer.Ordinal);

            // Process alphabetically by username
            foreach (var group in allUserEntries
                .GroupBy(x => x.Username, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
            {
                foreach (var (username, entry) in group)
                {
                    // Skip entries that overlap with base
                    var mdEntry = new MasterDateEntry
                    {
                        Names = entry.Names,
                        Floruit = entry.Floruit,
                        Death = entry.Death,
                        CreatedBy = username,
                        WrittenUtc = entry.WrittenUtc
                    };
                    if (MasterDatesService.OverlapsWithBase(mdEntry, _baseNames))
                        continue;

                    // Check if this master was already seen from another user
                    string? matchKey = null;
                    foreach (var name in entry.Names)
                    {
                        var trimmed = name.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && addedNames.Contains(trimmed))
                        {
                            matchKey = trimmed;
                            break;
                        }
                    }

                    if (matchKey != null)
                    {
                        // Potential conflict: check if dates differ
                        var existingCommunity = _communityMasters.FirstOrDefault(cm =>
                            cm.Names.Any(n => string.Equals(n.Trim(), matchKey, StringComparison.Ordinal)));

                        if (existingCommunity != null &&
                            (existingCommunity.Floruit != entry.Floruit || existingCommunity.Death != entry.Death))
                        {
                            if (!conflictMap.TryGetValue(matchKey, out var conflict))
                            {
                                conflict = new MasterDateConflict
                                {
                                    MasterName = existingCommunity.PrimaryName
                                };
                                conflict.Entries.Add((existingCommunity.CreatedBy ?? "?",
                                    existingCommunity.Floruit, existingCommunity.Death));
                                conflictMap[matchKey] = conflict;
                            }
                            conflict.Entries.Add((username, entry.Floruit, entry.Death));
                            existingCommunity.HasConflict = true;
                        }
                        continue; // First user wins
                    }

                    // New community master
                    var communityEntry = new MasterEntry
                    {
                        Names = entry.Names.Select(n => n.Trim()).Where(n => n.Length > 0).ToList(),
                        Floruit = entry.Floruit,
                        Death = entry.Death,
                        IsCommunity = true,
                        CreatedBy = username
                    };
                    _communityMasters.Add(communityEntry);

                    foreach (var name in communityEntry.Names)
                        addedNames.Add(name);
                }
            }

            _conflicts = conflictMap.Values.ToList();
        }
        catch
        {
            // Community load failure is non-fatal
        }
    }

    private bool IsBaseEntry(List<string> names)
    {
        return names.Any(n => _baseNames.Contains(n.Trim()));
    }

    private void SaveToFile()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Only save base + custom entries to the master file (not community)
            var toSave = _masters.Where(m => !m.IsCommunity).ToList();
            var wrapper = new MasterDatesRoot { Masters = toSave };
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var json = JsonSerializer.Serialize(wrapper, options);

            // UTF-8 no BOM
            File.WriteAllText(_filePath, json, new UTF8Encoding(false));
            Saved = true;

            SetStatus($"Saved {toSave.Count} master(s) (excluding {_communityMasters.Count} community).");
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

        // Combine base/custom masters with community masters
        IEnumerable<MasterEntry> source = _masters.Concat(_communityMasters);

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
        var txtSource = this.FindControl<TextBlock>("TxtSource");

        if (_selected == null)
        {
            if (txtNames != null) txtNames.Text = "";
            if (txtFloruit != null) txtFloruit.Text = "";
            if (txtDeath != null) txtDeath.Text = "";
            if (txtSource != null) txtSource.Text = "";
            SetEditorReadOnly(false);
            return;
        }

        if (txtNames != null) txtNames.Text = string.Join("\n", _selected.Names);
        if (txtFloruit != null) txtFloruit.Text = _selected.Floruit > 0 ? _selected.Floruit.ToString() : "";
        if (txtDeath != null) txtDeath.Text = _selected.Death > 0 ? _selected.Death.ToString() : "";

        // Source label
        if (txtSource != null)
        {
            if (_selected.IsBase)
                txtSource.Text = "Source: Canonical (base file)";
            else if (_selected.IsCommunity)
                txtSource.Text = $"Source: Community ({_selected.CreatedBy ?? "unknown"}) — read-only";
            else
                txtSource.Text = "Source: Custom (your addition)";
        }

        // Base and community entries are read-only
        bool readOnly = _selected.IsBase || _selected.IsCommunity;
        SetEditorReadOnly(readOnly);
    }

    private void SetEditorReadOnly(bool readOnly)
    {
        var txtNames = this.FindControl<TextBox>("TxtNames");
        var txtFloruit = this.FindControl<TextBox>("TxtFloruit");
        var txtDeath = this.FindControl<TextBox>("TxtDeath");

        if (txtNames != null) txtNames.IsReadOnly = readOnly;
        if (txtFloruit != null) txtFloruit.IsReadOnly = readOnly;
        if (txtDeath != null) txtDeath.IsReadOnly = readOnly;
    }

    private void SyncFromEditor()
    {
        if (_selected == null) return;
        // Don't edit base or community entries
        if (_selected.IsBase || _selected.IsCommunity) return;

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

    private void UpdateConflictDisplay()
    {
        var txtConflicts = this.FindControl<TextBlock>("TxtConflicts");
        var conflictBorder = this.FindControl<Border>("ConflictBorder");

        if (_conflicts.Count == 0)
        {
            if (txtConflicts != null) txtConflicts.Text = "";
            if (conflictBorder != null) conflictBorder.IsVisible = false;
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"⚠ {_conflicts.Count} conflict(s) between community users:");
        foreach (var c in _conflicts)
        {
            var parts = c.Entries
                .Select(e => $"{e.Username}: fl.{e.Floruit} d.{e.Death}")
                .ToList();
            sb.AppendLine($"  {c.MasterName} — {string.Join(" vs ", parts)}");
        }

        if (txtConflicts != null) txtConflicts.Text = sb.ToString().TrimEnd();
        if (conflictBorder != null) conflictBorder.IsVisible = true;
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
            Death = 860,
            IsBase = false,
            IsCommunity = false
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

        if (_selected.IsBase)
        {
            SetStatus("Cannot delete canonical (base) entries.");
            return;
        }

        if (_selected.IsCommunity)
        {
            SetStatus("Cannot delete community entries.");
            return;
        }

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

    // ----- Public helpers -----

    /// <summary>
    /// Returns only custom (non-base, non-community) master entries for sharing.
    /// </summary>
    public List<MasterDateEntry> GetCustomEntries()
    {
        return _masters
            .Where(m => !m.IsBase && !m.IsCommunity)
            .Select(m => new MasterDateEntry
            {
                Names = new List<string>(m.Names),
                Floruit = m.Floruit,
                Death = m.Death
            })
            .ToList();
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
        public bool IsBase { get; set; }

        [JsonIgnore]
        public bool IsCommunity { get; set; }

        [JsonIgnore]
        public string? CreatedBy { get; set; }

        [JsonIgnore]
        public bool HasConflict { get; set; }

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

