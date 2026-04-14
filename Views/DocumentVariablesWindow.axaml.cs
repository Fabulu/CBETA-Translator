using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.App.Views;

/// <summary>
/// List of document variables, plus cross-tabulation with tags.
/// </summary>
public partial class DocumentVariablesWindow : Window
{
    private string _root = "";
    private readonly IDocumentVariableService _varService;
    private List<DocumentTag> _tags = new();
    private TagVocabulary _vocab = new();
    private ObservableCollection<DocumentVariable> _variables = new();

    public DocumentVariablesWindow()
    {
        InitializeComponent();
        _varService = new DocumentVariableService();
    }

    /// <summary>
    /// Loads variables for the given root and wires up the list.
    /// </summary>
    public async System.Threading.Tasks.Task LoadDataAsync(
        string root, List<DocumentTag> tags, TagVocabulary vocab)
    {
        _root = root;
        _tags = tags;
        _vocab = vocab;

        var loaded = await _varService.LoadAsync(root);
        _variables = new ObservableCollection<DocumentVariable>(loaded);

        var list = this.FindControl<ListBox>("VariableList");
        if (list != null)
            list.ItemsSource = _variables;

        // Populate variable name dropdown for cross-tab
        var varNames = loaded.Select(v => v.VariableName)
                             .Distinct(StringComparer.OrdinalIgnoreCase)
                             .OrderBy(n => n)
                             .ToList();

        var cmb = this.FindControl<ComboBox>("CmbCrossTabVar");
        if (cmb != null)
            cmb.ItemsSource = varNames;

        // Wire buttons
        var btnSave = this.FindControl<Button>("BtnSave");
        if (btnSave != null)
            btnSave.Click += async (_, _) =>
            {
                await _varService.SaveAsync(_root, _variables.ToList());
                btnSave.Content = "Saved!";
            };

        var btnClose = this.FindControl<Button>("BtnClose");
        if (btnClose != null)
            btnClose.Click += (_, _) => Close();

        var btnAdd = this.FindControl<Button>("BtnAddVariable");
        var txtName = this.FindControl<TextBox>("TxtVariableName");
        if (btnAdd != null && txtName != null)
        {
            btnAdd.Click += (_, _) =>
            {
                var name = txtName.Text?.Trim();
                if (string.IsNullOrEmpty(name)) return;

                var tagPaths = _tags.Select(t => t.RelPath)
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var rp in tagPaths)
                {
                    if (!_variables.Any(v =>
                        string.Equals(v.RelPath, rp, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(v.VariableName, name, StringComparison.OrdinalIgnoreCase)))
                    {
                        _variables.Add(new DocumentVariable
                        {
                            RelPath = rp,
                            VariableName = name,
                            VariableValue = ""
                        });
                    }
                }
            };
        }

        var btnCrossTab = this.FindControl<Button>("BtnCrossTab");
        if (btnCrossTab != null && cmb != null)
        {
            btnCrossTab.Click += (_, _) =>
            {
                var varName = cmb.SelectedItem as string;
                if (string.IsNullOrEmpty(varName)) return;

                var crossTab = _varService.CrossTabulate(_tags, _variables.ToList(), _vocab, varName);
                this.Title = $"Document Variables - Cross-tab by '{varName}' ({crossTab.Count} groups)";
            };
        }
    }
}
