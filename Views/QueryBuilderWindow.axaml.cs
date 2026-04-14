using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.App.Views;

/// <summary>
/// Boolean query builder for tags: select two codes + an operator, run, see results.
/// Supports saving/loading queries.
/// </summary>
public partial class QueryBuilderWindow : Window
{
    private string _root = "";
    private List<DocumentTag> _tags = new();
    private List<CodeItem> _codeItems = new();

    public QueryBuilderWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the query builder with tag data.
    /// </summary>
    public void LoadData(string root, List<DocumentTag> tags, TagVocabulary vocab)
    {
        _root = root;
        _tags = tags;

        // Build code list for dropdowns
        var tagLookup = new Dictionary<string, string>(StringComparer.Ordinal);
        if (vocab?.Tags != null)
        {
            foreach (var td in vocab.Tags)
                tagLookup.TryAdd(td.Id, td.DisplayName);
        }

        var codeIds = tags.Select(t => t.TagId)
                          .Distinct(StringComparer.Ordinal)
                          .OrderBy(id => id)
                          .ToList();

        _codeItems = codeIds.Select(id => new CodeItem
        {
            Id = id,
            Name = tagLookup.TryGetValue(id, out var n) ? n : id
        }).ToList();

        var cmbA = this.FindControl<ComboBox>("CmbCodeA");
        var cmbB = this.FindControl<ComboBox>("CmbCodeB");
        if (cmbA != null) cmbA.ItemsSource = _codeItems;
        if (cmbB != null) cmbB.ItemsSource = _codeItems;

        // Wire buttons
        var btnRun = this.FindControl<Button>("BtnRun");
        if (btnRun != null) btnRun.Click += (_, _) => RunQuery();

        var btnSave = this.FindControl<Button>("BtnSave");
        if (btnSave != null) btnSave.Click += (_, _) => SaveQuery();

        var btnLoad = this.FindControl<Button>("BtnLoad");
        if (btnLoad != null) btnLoad.Click += (_, _) => LoadQueries();
    }

    private void RunQuery()
    {
        var cmbA = this.FindControl<ComboBox>("CmbCodeA");
        var cmbB = this.FindControl<ComboBox>("CmbCodeB");
        var cmbOp = this.FindControl<ComboBox>("CmbOperator");
        var txtStatus = this.FindControl<TextBlock>("TxtStatus");
        var resultsList = this.FindControl<ListBox>("ResultsList");

        if (cmbA?.SelectedItem is not CodeItem codeA ||
            cmbB?.SelectedItem is not CodeItem codeB)
        {
            if (txtStatus != null) txtStatus.Text = "Please select both codes.";
            return;
        }

        var op = (cmbOp?.SelectedIndex ?? 0) switch
        {
            1 => TagQueryOperator.Or,
            2 => TagQueryOperator.Not,
            _ => TagQueryOperator.And
        };

        var query = new TagQuery
        {
            Name = this.FindControl<TextBox>("TxtQueryName")?.Text ?? "",
            CodeA = codeA.Id,
            CodeB = codeB.Id,
            Operator = op
        };

        var results = TagQueryService.Execute(query, _tags);

        if (resultsList != null)
            resultsList.ItemsSource = results.Select(r => new ResultRowVm(r)).ToList();

        if (txtStatus != null)
            txtStatus.Text = $"Found {results.Count} matching files.";
    }

    private void SaveQuery()
    {
        var cmbA = this.FindControl<ComboBox>("CmbCodeA");
        var cmbB = this.FindControl<ComboBox>("CmbCodeB");
        var cmbOp = this.FindControl<ComboBox>("CmbOperator");
        var txtName = this.FindControl<TextBox>("TxtQueryName");

        if (cmbA?.SelectedItem is not CodeItem codeA ||
            cmbB?.SelectedItem is not CodeItem codeB)
            return;

        var op = (cmbOp?.SelectedIndex ?? 0) switch
        {
            1 => TagQueryOperator.Or,
            2 => TagQueryOperator.Not,
            _ => TagQueryOperator.And
        };

        var query = new TagQuery
        {
            Name = txtName?.Text ?? "Unnamed",
            CodeA = codeA.Id,
            CodeB = codeB.Id,
            Operator = op
        };

        var path = Path.Combine(_root, "saved-queries.json");
        List<TagQuery> existing = new();
        if (File.Exists(path))
        {
            try { existing = TagQueryService.DeserializeQueries(File.ReadAllText(path)); }
            catch { /* ignore */ }
        }

        existing.Add(query);
        File.WriteAllText(path, TagQueryService.SerializeQueries(existing));

        var txtStatus = this.FindControl<TextBlock>("TxtStatus");
        if (txtStatus != null)
            txtStatus.Text = $"Saved query '{query.Name}'.";
    }

    private void LoadQueries()
    {
        var path = Path.Combine(_root, "saved-queries.json");
        if (!File.Exists(path)) return;

        var queries = TagQueryService.DeserializeQueries(File.ReadAllText(path));
        if (queries.Count == 0) return;

        var q = queries[^1];
        var cmbA = this.FindControl<ComboBox>("CmbCodeA");
        var cmbB = this.FindControl<ComboBox>("CmbCodeB");
        var cmbOp = this.FindControl<ComboBox>("CmbOperator");
        var txtName = this.FindControl<TextBox>("TxtQueryName");

        if (cmbA != null) cmbA.SelectedItem = _codeItems.FirstOrDefault(c => c.Id == q.CodeA);
        if (cmbB != null) cmbB.SelectedItem = _codeItems.FirstOrDefault(c => c.Id == q.CodeB);
        if (cmbOp != null) cmbOp.SelectedIndex = q.Operator switch
        {
            TagQueryOperator.Or => 1,
            TagQueryOperator.Not => 2,
            _ => 0
        };
        if (txtName != null) txtName.Text = q.Name;

        var txtStatus = this.FindControl<TextBlock>("TxtStatus");
        if (txtStatus != null)
            txtStatus.Text = $"Loaded query '{q.Name}' ({queries.Count} saved).";
    }

    public sealed class CodeItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public override string ToString() => Name;
    }

    public sealed class ResultRowVm
    {
        private readonly TagQueryMatch _match;
        public ResultRowVm(TagQueryMatch match) => _match = match;

        public string RelPath => _match.RelPath;
        public string FromLb => _match.FromLb;
        public string ToLb => _match.ToLb;
        public string MatchedCodesText => string.Join(", ", _match.MatchedTagIds);
    }
}
