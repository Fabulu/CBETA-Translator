using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CbetaTranslator.App.Views;

public partial class TagEditorWindow : Window
{
    private static readonly DataFormat<string> TagIdDataFormat = DataFormat.CreateStringApplicationFormat("TagId");
    private static readonly string[] Palette =
    {
        "#E63946", "#2A9D8F", "#457B9D", "#E9C46A", "#8338EC",
        "#F77F00", "#06D6A0", "#EF476F", "#118AB2", "#073B4C",
        "#FFB703", "#7209B7", "#3A86FF", "#FB5607", "#8AC926"
    };

    private static readonly Regex HexColorRegex = new(@"^#[0-9A-Fa-f]{6}$");

    private readonly IDocumentTagService? _tagService;
    private readonly string? _root;
    private readonly string? _username;

    private TagVocabulary _vocabulary = new();
    private ObservableCollection<TagTreeNode> _tagRoots = new();
    private bool _suppressSelectionEvents;

    // UI controls (found by name)
    private TextBox? _txtFilter;
    private TreeView? _tagTree;
    private TextBox? _txtName;
    private TextBox? _txtColor;
    private Border? _swatchPreview;
    private ComboBox? _cmbParent;
    private TextBox? _txtDescription;
    private ComboBox? _cmbPage;
    private ComboBox? _cmbSlot;
    private StackPanel? _slotPreview;
    private TextBlock? _txtStatus;
    private WrapPanel? _colorPalette;

    // Drag-and-drop state
    private Point? _dragStartPoint;
    private TagTreeNode? _dragCandidate;

    /// <summary>
    /// Fired after a successful save. MainWindow subscribes to reload the vocabulary.
    /// </summary>
    public event EventHandler? VocabularySaved;

    /// <summary>
    /// Parameterless constructor required by Avalonia XAML loader.
    /// </summary>
    public TagEditorWindow()
    {
        InitializeComponent();
    }

    public TagEditorWindow(string root, string? username)
    {
        InitializeComponent();

        _root = root;
        _username = username;
        _tagService = App.Services.GetRequiredService<IDocumentTagService>();

        WireControls();

        Opened += async (_, _) => await LoadAsync();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    // ── Control wiring ──────────────────────────────────────────────────

    private void WireControls()
    {
        _txtFilter = this.FindControl<TextBox>("TxtFilter");
        _tagTree = this.FindControl<TreeView>("TagTree");
        _txtName = this.FindControl<TextBox>("TxtName");
        _txtColor = this.FindControl<TextBox>("TxtColor");
        _swatchPreview = this.FindControl<Border>("SwatchPreview");
        _cmbParent = this.FindControl<ComboBox>("CmbParent");
        _txtDescription = this.FindControl<TextBox>("TxtDescription");
        _cmbPage = this.FindControl<ComboBox>("CmbPage");
        _cmbSlot = this.FindControl<ComboBox>("CmbSlot");
        _slotPreview = this.FindControl<StackPanel>("SlotPreview");
        _txtStatus = this.FindControl<TextBlock>("TxtStatus");
        _colorPalette = this.FindControl<WrapPanel>("ColorPalette");

        BuildColorSwatches();

        // Page combo: 1-18
        if (_cmbPage != null)
        {
            _cmbPage.ItemsSource = Enumerable.Range(1, 18).ToList();
            _cmbPage.SelectedIndex = 0;
            _cmbPage.SelectionChanged += (_, _) => RefreshSlotPreview();
        }

        // Slot combo: 1-9
        if (_cmbSlot != null)
        {
            _cmbSlot.ItemsSource = Enumerable.Range(1, 9).ToList();
            _cmbSlot.SelectedIndex = 0;
        }

        // Tree selection
        if (_tagTree != null)
        {
            _tagTree.SelectionChanged += (_, _) => OnSelectionChanged();
            _tagTree.PointerPressed += OnTreePointerPressed;
            _tagTree.PointerMoved += OnTreePointerMoved;
            _tagTree.PointerReleased += (_, _) => { _dragStartPoint = null; _dragCandidate = null; };
        }

        // Filter
        if (_txtFilter != null)
            _txtFilter.TextChanged += (_, _) => ApplyFilter();

        // Name changed
        if (_txtName != null)
            _txtName.TextChanged += (_, _) => OnNameChanged();

        // Color changed
        if (_txtColor != null)
            _txtColor.TextChanged += (_, _) => OnColorChanged();

        // Description changed
        if (_txtDescription != null)
            _txtDescription.TextChanged += (_, _) => OnDescriptionChanged();

        // Parent changed
        if (_cmbParent != null)
            _cmbParent.SelectionChanged += (_, _) => OnParentChanged();

        // Buttons
        var btnNew = this.FindControl<Button>("BtnNewTag");
        if (btnNew != null) btnNew.Click += (_, _) => OnNewTag();

        var btnChild = this.FindControl<Button>("BtnNewChild");
        if (btnChild != null) btnChild.Click += (_, _) => OnNewChildTag();

        var btnDel = this.FindControl<Button>("BtnDeleteTag");
        if (btnDel != null) btnDel.Click += (_, _) => OnDeleteTag();

        var btnDelBottom = this.FindControl<Button>("BtnDeleteBottom");
        if (btnDelBottom != null) btnDelBottom.Click += (_, _) => OnDeleteTag();

        var btnAssign = this.FindControl<Button>("BtnAssignSlot");
        if (btnAssign != null) btnAssign.Click += (_, _) => OnAssignToSlot();

        var btnRemove = this.FindControl<Button>("BtnRemoveSlot");
        if (btnRemove != null) btnRemove.Click += (_, _) => OnRemoveFromSlot();

        var btnClose = this.FindControl<Button>("BtnClose");
        if (btnClose != null) btnClose.Click += (_, _) => Close();

        var btnSave = this.FindControl<Button>("BtnSave");
        if (btnSave != null) btnSave.Click += async (_, _) => await OnSaveAsync();
    }

    // ── Load ────────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        if (_tagService == null || _root == null || string.IsNullOrWhiteSpace(_username))
            return;

        try
        {
            _vocabulary = await _tagService.LoadVocabularyAsync(_root, _username);
            BuildTree();
            RefreshSlotPreview();
        }
        catch (Exception ex)
        {
            SetStatus("Load failed: " + ex.Message);
        }
    }

    // ── Tree building ───────────────────────────────────────────────────

    private void BuildTree()
    {
        var lookup = new Dictionary<string, TagTreeNode>();
        foreach (var tag in _vocabulary.Tags.OrderBy(t => t.SortOrder))
        {
            lookup[tag.Id] = new TagTreeNode { Tag = tag, IsExpanded = true };
        }

        var roots = new ObservableCollection<TagTreeNode>();
        foreach (var tag in _vocabulary.Tags.OrderBy(t => t.SortOrder))
        {
            var node = lookup[tag.Id];
            // Guard against self-referencing ParentId (cycle to self)
            if (!string.IsNullOrEmpty(tag.ParentId)
                && tag.ParentId != tag.Id
                && lookup.TryGetValue(tag.ParentId, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        _tagRoots = roots;
        if (_tagTree != null)
            _tagTree.ItemsSource = _tagRoots;
    }

    private void RefreshTree(string? selectId = null)
    {
        BuildTree();
        ApplyFilter();

        if (selectId != null)
        {
            var node = FindNodeById(selectId);
            if (node != null && _tagTree != null)
            {
                _suppressSelectionEvents = true;
                _tagTree.SelectedItem = node;
                _suppressSelectionEvents = false;
                PopulatePropertiesPanel(node);
            }
        }
    }

    private TagTreeNode? FindNodeById(string id)
    {
        return FindInCollection(_tagRoots, id);
    }

    private static TagTreeNode? FindInCollection(IEnumerable<TagTreeNode> nodes, string id)
    {
        foreach (var node in nodes)
        {
            if (node.Tag.Id == id) return node;
            var child = FindInCollection(node.Children, id);
            if (child != null) return child;
        }
        return null;
    }

    // ── Filter ──────────────────────────────────────────────────────────

    private void ApplyFilter()
    {
        var filter = _txtFilter?.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(filter))
        {
            if (_tagTree != null)
                _tagTree.ItemsSource = _tagRoots;
            return;
        }

        var filtered = FilterNodes(_tagRoots, filter);
        if (_tagTree != null)
            _tagTree.ItemsSource = filtered;
    }

    private static ObservableCollection<TagTreeNode> FilterNodes(
        IEnumerable<TagTreeNode> nodes, string filter)
    {
        var result = new ObservableCollection<TagTreeNode>();
        foreach (var node in nodes)
        {
            var childMatches = FilterNodes(node.Children, filter);
            bool selfMatch = node.Tag.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                          || (node.Tag.Description?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false);

            if (selfMatch || childMatches.Count > 0)
            {
                // When parent matches, show all its children (use a shallow copy to avoid
                // sharing the mutable ObservableCollection reference with the original tree).
                var clonedChildren = childMatches.Count > 0
                    ? childMatches
                    : new ObservableCollection<TagTreeNode>(node.Children);
                var clone = new TagTreeNode
                {
                    Tag = node.Tag,
                    Children = clonedChildren,
                    IsExpanded = true
                };
                result.Add(clone);
            }
        }
        return result;
    }

    // ── Selection ───────────────────────────────────────────────────────

    private void OnSelectionChanged()
    {
        if (_suppressSelectionEvents) return;
        var node = _tagTree?.SelectedItem as TagTreeNode;
        PopulatePropertiesPanel(node);
    }

    private void PopulatePropertiesPanel(TagTreeNode? node)
    {
        _suppressSelectionEvents = true;
        try
        {
            if (node == null)
            {
                if (_txtName != null) _txtName.Text = "";
                if (_txtColor != null) _txtColor.Text = "";
                if (_txtDescription != null) _txtDescription.Text = "";
                if (_cmbParent != null) _cmbParent.SelectedIndex = -1;
                return;
            }

            var tag = node.Tag;
            if (_txtName != null) _txtName.Text = tag.Name;
            if (_txtColor != null) _txtColor.Text = tag.Color;
            if (_txtDescription != null) _txtDescription.Text = tag.Description ?? "";

            UpdateSwatchPreview(tag.Color);
            HighlightSelectedSwatch(tag.Color);
            PopulateParentCombo(tag);
        }
        finally
        {
            _suppressSelectionEvents = false;
        }
    }

    private void PopulateParentCombo(TagDefinition tag)
    {
        if (_cmbParent == null) return;

        var descendants = GetDescendantIds(tag.Id);
        var items = new List<ParentComboItem>
        {
            new() { Display = "(none)", TagId = null }
        };

        foreach (var t in _vocabulary.Tags.OrderBy(t => t.SortOrder))
        {
            if (t.Id == tag.Id) continue;
            if (descendants.Contains(t.Id)) continue;
            items.Add(new ParentComboItem { Display = t.DisplayName, TagId = t.Id });
        }

        _cmbParent.ItemsSource = items;

        // Select current parent
        int idx = 0;
        if (!string.IsNullOrEmpty(tag.ParentId))
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].TagId == tag.ParentId) { idx = i; break; }
            }
        }
        _cmbParent.SelectedIndex = idx;
    }

    private HashSet<string> GetDescendantIds(string tagId)
    {
        var result = new HashSet<string>();
        CollectDescendants(tagId, result);
        return result;
    }

    private void CollectDescendants(string parentId, HashSet<string> result)
    {
        foreach (var t in _vocabulary.Tags)
        {
            if (t.ParentId == parentId && result.Add(t.Id))
            {
                CollectDescendants(t.Id, result);
            }
        }
    }

    // ── Property changes ────────────────────────────────────────────────

    private void OnNameChanged()
    {
        if (_suppressSelectionEvents) return;
        var node = _tagTree?.SelectedItem as TagTreeNode;
        if (node == null || _txtName == null) return;
        node.Tag.Name = _txtName.Text ?? "";
        // Notify the tree binding to update the displayed name without full rebuild
        node.RaiseTagChanged();
    }

    private void OnColorChanged()
    {
        if (_suppressSelectionEvents) return;
        var node = _tagTree?.SelectedItem as TagTreeNode;
        if (node == null || _txtColor == null) return;

        var hex = _txtColor.Text ?? "";
        if (HexColorRegex.IsMatch(hex))
        {
            node.Tag.Color = hex;
            UpdateSwatchPreview(hex);
            SetStatus("");
            // Notify the tree binding to update the swatch without full rebuild
            node.RaiseTagChanged();
        }
        else if (hex.Length > 0)
        {
            SetStatus("Invalid color. Use #RRGGBB format.");
        }
    }

    private void OnDescriptionChanged()
    {
        if (_suppressSelectionEvents) return;
        var node = _tagTree?.SelectedItem as TagTreeNode;
        if (node == null || _txtDescription == null) return;
        node.Tag.Description = string.IsNullOrWhiteSpace(_txtDescription.Text) ? null : _txtDescription.Text;
    }

    private void OnParentChanged()
    {
        if (_suppressSelectionEvents) return;
        var node = _tagTree?.SelectedItem as TagTreeNode;
        if (node == null || _cmbParent == null) return;

        var selected = _cmbParent.SelectedItem as ParentComboItem;
        if (selected == null) return;

        node.Tag.ParentId = selected.TagId;
        RefreshTree(node.Tag.Id);
    }

    private void UpdateSwatchPreview(string hex)
    {
        if (_swatchPreview == null) return;
        try
        {
            _swatchPreview.Background = new SolidColorBrush(Color.Parse(hex));
        }
        catch
        {
            _swatchPreview.Background = new SolidColorBrush(Colors.Gray);
        }
    }

    // ── CRUD ────────────────────────────────────────────────────────────

    private void OnNewTag()
    {
        var tag = CreateNewTag(null);
        _vocabulary.Tags.Add(tag);
        RefreshTree(tag.Id);
    }

    private void OnNewChildTag()
    {
        var node = _tagTree?.SelectedItem as TagTreeNode;
        var parentId = node?.Tag.Id;
        var tag = CreateNewTag(parentId);
        _vocabulary.Tags.Add(tag);
        RefreshTree(tag.Id);
    }

    private TagDefinition CreateNewTag(string? parentId)
    {
        int maxSort = _vocabulary.Tags.Count > 0
            ? _vocabulary.Tags.Max(t => t.SortOrder)
            : 0;

        string color = Palette[_vocabulary.Tags.Count % Palette.Length];

        return new TagDefinition
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "New Tag",
            ParentId = parentId,
            Color = color,
            SortOrder = maxSort + 1,
            CreatedUtc = DateTimeOffset.UtcNow
        };
    }

    private void OnDeleteTag()
    {
        var node = _tagTree?.SelectedItem as TagTreeNode;
        if (node == null) return;

        var tag = node.Tag;

        // Reparent children to the deleted tag's parent
        foreach (var child in _vocabulary.Tags.Where(t => t.ParentId == tag.Id))
        {
            child.ParentId = tag.ParentId;
        }

        _vocabulary.Tags.Remove(tag);

        // Also remove from any code bar page slots
        var emptyPages = new List<int>();
        foreach (var kvp in _vocabulary.Pages)
        {
            var slots = kvp.Value;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == tag.Id)
                    slots[i] = null;
            }

            // Track pages that are now fully empty (all null slots)
            if (slots.All(s => s == null))
                emptyPages.Add(kvp.Key);
        }

        // Remove fully-empty pages to keep the page count accurate
        foreach (var p in emptyPages)
            _vocabulary.Pages.Remove(p);

        RefreshTree();
        SetStatus($"Deleted tag \"{tag.DisplayName}\".");
    }

    // ── Code bar assignment ─────────────────────────────────────────────

    private void OnAssignToSlot()
    {
        var node = _tagTree?.SelectedItem as TagTreeNode;
        if (node == null)
        {
            SetStatus("Select a tag first.");
            return;
        }

        int page = (_cmbPage?.SelectedItem as int?) ?? 1;
        int slot = (_cmbSlot?.SelectedItem as int?) ?? 1;

        if (!_vocabulary.Pages.TryGetValue(page, out var slots))
        {
            slots = new string?[9];
            _vocabulary.Pages[page] = slots;
        }

        // Ensure slot array is at least length 9
        if (slots.Length < 9)
        {
            var newSlots = new string?[9];
            Array.Copy(slots, newSlots, slots.Length);
            slots = newSlots;
            _vocabulary.Pages[page] = slots;
        }

        slots[slot - 1] = node.Tag.Id;
        RefreshSlotPreview();
        SetStatus($"Assigned \"{node.Tag.DisplayName}\" to Page {page}, Slot {slot}.");
    }

    private void OnRemoveFromSlot()
    {
        int page = (_cmbPage?.SelectedItem as int?) ?? 1;
        int slot = (_cmbSlot?.SelectedItem as int?) ?? 1;

        if (_vocabulary.Pages.TryGetValue(page, out var slots) && slots.Length >= slot)
        {
            slots[slot - 1] = null;
            RefreshSlotPreview();
            SetStatus($"Cleared Page {page}, Slot {slot}.");
        }
    }

    private void RefreshSlotPreview()
    {
        if (_slotPreview == null) return;
        _slotPreview.Children.Clear();

        int page = (_cmbPage?.SelectedItem as int?) ?? 1;

        if (!_vocabulary.Pages.TryGetValue(page, out var slots))
            slots = new string?[9];

        var tagLookup = _vocabulary.Tags.ToDictionary(t => t.Id);

        for (int i = 0; i < 9; i++)
        {
            var tagId = i < slots.Length ? slots[i] : null;
            TagDefinition? tag = null;
            if (tagId != null)
                tagLookup.TryGetValue(tagId, out tag);

            var bgColor = tag != null ? ParseColorSafe(tag.Color) : Color.Parse("#333333");
            // Use dark text on light backgrounds for readability
            var fgColor = (0.299 * bgColor.R + 0.587 * bgColor.G + 0.114 * bgColor.B) > 160
                ? Colors.Black : Colors.White;

            var chip = new Border
            {
                Width = 28,
                Height = 20,
                CornerRadius = new CornerRadius(3),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)),
                Background = new SolidColorBrush(bgColor),
                Child = new TextBlock
                {
                    Text = (i + 1).ToString(),
                    FontSize = 10,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(fgColor)
                }
            };

            if (tag != null)
                ToolTip.SetTip(chip, tag.DisplayName);

            DragDrop.SetAllowDrop(chip, true);
            int capturedSlot = i;
            chip.AddHandler(DragDrop.DropEvent, (_, args) =>
            {
                if (args.DataTransfer.Contains(TagIdDataFormat))
                {
                    var dragTagId = args.DataTransfer.TryGetValue(TagIdDataFormat);
                    if (!string.IsNullOrEmpty(dragTagId))
                    {
                        int dropPage = (_cmbPage?.SelectedItem as int?) ?? 1;
                        if (!_vocabulary.Pages.TryGetValue(dropPage, out var dropSlots))
                        {
                            dropSlots = new string?[9];
                            _vocabulary.Pages[dropPage] = dropSlots;
                        }
                        if (dropSlots.Length < 9)
                        {
                            var newSlots = new string?[9];
                            Array.Copy(dropSlots, newSlots, dropSlots.Length);
                            dropSlots = newSlots;
                            _vocabulary.Pages[dropPage] = dropSlots;
                        }
                        dropSlots[capturedSlot] = dragTagId;
                        RefreshSlotPreview();
                    }
                }
            });

            _slotPreview.Children.Add(chip);
        }
    }

    private static Color ParseColorSafe(string hex)
    {
        try { return Color.Parse(hex); }
        catch { return Colors.Gray; }
    }

    // ── Save ────────────────────────────────────────────────────────────

    private async Task OnSaveAsync()
    {
        if (_tagService == null || _root == null || string.IsNullOrWhiteSpace(_username))
        {
            SetStatus("Cannot save: missing root or username.");
            return;
        }

        try
        {
            await _tagService.SaveVocabularyAsync(_root, _username, _vocabulary);
            VocabularySaved?.Invoke(this, EventArgs.Empty);
            SetStatus("Vocabulary saved.");
        }
        catch (Exception ex)
        {
            SetStatus("Save failed: " + ex.Message);
        }
    }

    // ── Color palette swatches ─────────────────────────────────────────

    private void BuildColorSwatches()
    {
        if (_colorPalette == null) return;
        _colorPalette.Children.Clear();

        foreach (var hex in Palette)
        {
            var color = Color.Parse(hex);
            var swatch = new Border
            {
                Width = 24, Height = 24,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(color),
                BorderThickness = new Thickness(2),
                BorderBrush = Brushes.Transparent,
                Margin = new Thickness(2),
                Cursor = new Cursor(StandardCursorType.Hand),
            };
            ToolTip.SetTip(swatch, hex);

            var capturedHex = hex;
            swatch.PointerPressed += (_, _) =>
            {
                if (_txtColor != null) _txtColor.Text = capturedHex;
                HighlightSelectedSwatch(capturedHex);
            };

            _colorPalette.Children.Add(swatch);
        }
    }

    private void HighlightSelectedSwatch(string hex)
    {
        if (_colorPalette == null) return;
        foreach (var child in _colorPalette.Children)
        {
            if (child is Border b)
            {
                var tip = ToolTip.GetTip(b) as string;
                b.BorderBrush = string.Equals(tip, hex, StringComparison.OrdinalIgnoreCase)
                    ? Brushes.White
                    : Brushes.Transparent;
            }
        }
    }

    // ── Drag-and-drop tag assignment ────────────────────────────────────

    private void OnTreePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _dragStartPoint = e.GetPosition(_tagTree);
        _dragCandidate = _tagTree?.SelectedItem as TagTreeNode;
    }

    private async void OnTreePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragStartPoint == null || _dragCandidate == null) return;

        var pos = e.GetPosition(_tagTree);
        var delta = pos - _dragStartPoint.Value;
        if (Math.Abs(delta.X) < 5 && Math.Abs(delta.Y) < 5) return;

        _dragStartPoint = null;
        var data = new DataTransfer();
        data.Add(DataTransferItem.Create(TagIdDataFormat, _dragCandidate.Tag.Id));

        await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Copy);
        _dragCandidate = null;
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private void SetStatus(string msg)
    {
        if (_txtStatus != null) _txtStatus.Text = msg;
    }

    private class ParentComboItem
    {
        public string Display { get; set; } = "";
        public string? TagId { get; set; }
        public override string ToString() => Display;
    }
}

public class TagTreeNode : INotifyPropertyChanged
{
    private TagDefinition _tag = new();
    public TagDefinition Tag
    {
        get => _tag;
        set { _tag = value; OnPropertyChanged(nameof(Tag)); OnPropertyChanged(nameof(ColorBrush)); }
    }

    public ObservableCollection<TagTreeNode> Children { get; set; } = new();
    public bool IsExpanded { get; set; } = true;

    public IBrush ColorBrush
    {
        get
        {
            try { return new SolidColorBrush(Color.Parse(_tag.Color)); }
            catch { return new SolidColorBrush(Colors.Gray); }
        }
    }

    /// <summary>
    /// Notify bindings that the underlying Tag properties (Name, Color) changed
    /// without replacing the Tag object itself.
    /// </summary>
    public void RaiseTagChanged()
    {
        OnPropertyChanged(nameof(Tag));
        OnPropertyChanged(nameof(ColorBrush));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}



