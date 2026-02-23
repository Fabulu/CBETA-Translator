// Views/SearchTabView.axaml.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;

namespace CbetaTranslator.App.Views;

public partial class SearchTabView : UserControl
{
    private TextBox? _txtQuery;
    private Button? _btnSearch;
    private Button? _btnCancel;
    private Button? _btnBuildIndex;
    private TextBlock? _txtProgress;

    private CheckBox? _chkOriginal;
    private CheckBox? _chkTranslated;
    private ComboBox? _cmbStatus;
    private ComboBox? _cmbContext;

    // ✅ Zen-only filter
    private CheckBox? _chkZenOnly;

    private TextBlock? _txtSummary;
    private Button? _btnExportTsv;

    private TreeView? _resultsTree;

    // Co-occurrences panel controls
    private TextBlock? _txtCoocSummary;
    private ListBox? _lstCoocChars;
    private ListBox? _lstCoocNgrams;
    private TextBlock? _txtZipf;
    private ComboBox? _cmbCoocMetric;
    private TextBlock? _txtLeftTitle;
    private TextBlock? _txtRightTitle;
    private Control? _gridMetricView;
    private ScrollViewer? _scrollMetricGuide;
    private TextBlock? _txtMetricGuide;

    private readonly SearchIndexService _svc = new();

    private string? _root;
    private string? _originalDir;
    private string? _translatedDir;
    private bool _forceRebuildNextClick;

    private List<FileNavItem> _fileIndex = new();
    private Func<string, (string display, string tooltip, TranslationStatus? status)>? _meta;

    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _autoRerunCts;
    private readonly List<SearchResultGroup> _groups = new();

    // Incremental UI backing collection (append-only during a search run)
    private readonly ObservableCollection<SearchResultGroup> _groupsView = new();

    // remember last search so dropdown recompute works
    private string _lastQuery = "";
    private int _lastContextWidth = 40;

    // avoid stale async metric recomputes racing each other
    private int _metricComputeVersion = 0;

    // avoid stale search UI updates racing each other
    private int _searchRunVersion = 0;

    // Zen flag lookup (provided by MainWindow via SetZenResolver)
    private Func<string, bool>? _isZen;

    public event EventHandler<string>? Status;
    public event EventHandler<string>? OpenFileRequested;

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    public SearchTabView()
    {
        InitializeComponent();
        FindControls();
        WireEvents();
        InitCombos();

        if (_resultsTree != null)
            _resultsTree.ItemsSource = _groupsView;

        SetProgress("Index not loaded.");
        SetSummary("Ready.");
        ClearCoocUi();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void FindControls()
    {
        _txtQuery = this.FindControl<TextBox>("TxtQuery");
        _btnSearch = this.FindControl<Button>("BtnSearch");
        _btnCancel = this.FindControl<Button>("BtnCancel");
        _btnBuildIndex = this.FindControl<Button>("BtnBuildIndex");
        _txtProgress = this.FindControl<TextBlock>("TxtProgress");

        _chkOriginal = this.FindControl<CheckBox>("ChkOriginal");
        _chkTranslated = this.FindControl<CheckBox>("ChkTranslated");
        _cmbStatus = this.FindControl<ComboBox>("CmbStatus");
        _cmbContext = this.FindControl<ComboBox>("CmbContext");

        _chkZenOnly = this.FindControl<CheckBox>("ChkZenOnly");

        _txtSummary = this.FindControl<TextBlock>("TxtSummary");
        _btnExportTsv = this.FindControl<Button>("BtnExportTsv");

        _resultsTree = this.FindControl<TreeView>("ResultsTree");

        // Co-occurrence controls
        _txtCoocSummary = this.FindControl<TextBlock>("TxtCoocSummary");
        _lstCoocChars = this.FindControl<ListBox>("LstCoocChars");
        _lstCoocNgrams = this.FindControl<ListBox>("LstCoocNgrams");
        _txtZipf = this.FindControl<TextBlock>("TxtZipf");

        _cmbCoocMetric = this.FindControl<ComboBox>("CmbCoocMetric");
        _txtLeftTitle = this.FindControl<TextBlock>("TxtLeftTitle");
        _txtRightTitle = this.FindControl<TextBlock>("TxtRightTitle");

        _gridMetricView = this.FindControl<Control>("GridMetricView");
        _scrollMetricGuide = this.FindControl<ScrollViewer>("ScrollMetricGuide");
        _txtMetricGuide = this.FindControl<TextBlock>("TxtMetricGuide");
    }

    private void WireEvents()
    {
        if (_btnSearch != null) _btnSearch.Click += async (_, _) => await StartSearchAsync();
        if (_btnCancel != null) _btnCancel.Click += (_, _) => Cancel();

        if (_btnBuildIndex != null)
        {
            _btnBuildIndex.PointerPressed += (_, e) =>
            {
                _forceRebuildNextClick = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            };

            _btnBuildIndex.Click += async (_, _) => await BuildIndexAsync();
        }

        if (_btnExportTsv != null) _btnExportTsv.Click += async (_, _) => await ExportTsvAsync();

        if (_txtQuery != null)
        {
            _txtQuery.KeyDown += async (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    await StartSearchAsync();
                    e.Handled = true;
                }
            };
        }

        if (_resultsTree != null)
        {
            _resultsTree.DoubleTapped += (_, _) =>
            {
                var sel = _resultsTree.SelectedItem;
                if (sel is SearchResultGroup g)
                {
                    if (!string.IsNullOrWhiteSpace(g.RelPath))
                        OpenFileRequested?.Invoke(this, g.RelPath);
                }
                else if (sel is SearchResultChild c)
                {
                    if (!string.IsNullOrWhiteSpace(c.RelPath))
                        OpenFileRequested?.Invoke(this, c.RelPath);
                }
            };
        }

        if (_cmbCoocMetric != null)
        {
            // prevent expander header click-toggle stealing pointer events while selecting
            _cmbCoocMetric.PointerPressed += (_, e) => e.Handled = true;

            _cmbCoocMetric.SelectionChanged += async (_, _) =>
            {
                await RefreshCoocUiFromCurrentStateAsync();
            };
        }

        if (_chkZenOnly != null)
        {
            _chkZenOnly.IsCheckedChanged += async (_, _) =>
            {
                await TriggerAutoRerunAsync();
            };
        }

        if (_cmbStatus != null)
        {
            _cmbStatus.SelectionChanged += async (_, _) =>
            {
                await TriggerAutoRerunAsync();
            };
        }

        if (_cmbContext != null)
        {
            _cmbContext.SelectionChanged += async (_, _) =>
            {
                await TriggerAutoRerunAsync();
            };
        }

        if (_chkOriginal != null)
        {
            _chkOriginal.IsCheckedChanged += async (_, _) =>
            {
                await TriggerAutoRerunAsync();
            };
        }

        if (_chkTranslated != null)
        {
            _chkTranslated.IsCheckedChanged += async (_, _) =>
            {
                await TriggerAutoRerunAsync();
            };
        }
    }

    private void InitCombos()
    {
        if (_cmbStatus != null)
        {
            _cmbStatus.ItemsSource = new[]
            {
                "All",
                "Red (untranslated)",
                "Yellow (WIP)",
                "Green (done)"
            };
            _cmbStatus.SelectedIndex = 0;
        }

        if (_cmbContext != null)
        {
            _cmbContext.ItemsSource = new[]
            {
                "20 chars",
                "40 chars",
                "80 chars"
            };
            _cmbContext.SelectedIndex = 1; // 40
        }

        if (_cmbCoocMetric != null)
        {
            _cmbCoocMetric.ItemsSource = new[]
            {
                "Top co-occurrences (overview)",
                "Dispersion score (stable)",
                "Frequency (raw)",
                "Range (dispersion proxy)",
                "Dominance (top-file share)",
                "PMI (window-based)",
                "logDice (lexicography)",
                "t-score (frequency-biased)",
                "Metric guide (how to read these)"
            };
            _cmbCoocMetric.SelectedIndex = 0;
        }
    }

    // ------------------------------------------------------------
    // Public wiring
    // ------------------------------------------------------------

    public void SetRootContext(string root, string originalDir, string translatedDir)
    {
        _root = root;
        _originalDir = originalDir;
        _translatedDir = translatedDir;
    }

    public void SetFileIndex(List<FileNavItem> items)
    {
        _fileIndex = items ?? new List<FileNavItem>();
    }

    public void SetContext(
        string root,
        string originalDir,
        string translatedDir,
        Func<string, (string display, string tooltip, TranslationStatus? status)> fileMeta)
    {
        _root = root;
        _originalDir = originalDir;
        _translatedDir = translatedDir;
        _meta = fileMeta;

        SetProgress("Ready. (Index will load automatically on first search if present.)");
        SetSummary("Ready.");
        ClearCoocUi();

        // Tiny warm-up (best effort)
        _ = Task.Run(async () =>
        {
            try { await _svc.TryLoadAsync(root); }
            catch { /* ignore */ }
        });
    }

    /// <summary>
    /// Provide a resolver that tells whether a relPath is marked as Zen text.
    /// In MainWindow, call: _searchView.SetZenResolver(rel => _zenTexts.IsZen(rel));
    /// </summary>
    public void SetZenResolver(Func<string, bool> isZenResolver)
    {
        _isZen = isZenResolver;
    }

    public void Clear()
    {
        Cancel();

        _root = null;
        _originalDir = null;
        _translatedDir = null;
        _fileIndex.Clear();
        _meta = null;
        _isZen = null;

        _groups.Clear();
        _groupsView.Clear();

        SetProgress("No root loaded.");
        SetSummary("Ready.");
        if (_btnExportTsv != null) _btnExportTsv.IsEnabled = false;

        _lastQuery = "";
        _lastContextWidth = 40;

        if (_chkZenOnly != null) _chkZenOnly.IsChecked = false;

        ClearCoocUi();
    }

    // ------------------------------------------------------------
    // Filters
    // ------------------------------------------------------------

    private int GetContextWidth()
    {
        var s = _cmbContext?.SelectedItem as string ?? "40 chars";
        if (s.StartsWith("20")) return 20;
        if (s.StartsWith("80")) return 80;
        return 40;
    }

    private TranslationStatus? GetStatusFilter()
    {
        int i = _cmbStatus?.SelectedIndex ?? 0;
        return i switch
        {
            1 => TranslationStatus.Red,
            2 => TranslationStatus.Yellow,
            3 => TranslationStatus.Green,
            _ => null
        };
    }

    private bool ZenOnly()
        => _chkZenOnly?.IsChecked == true;

    private Func<string, bool>? BuildRelPathFilter(bool zenOnly, TranslationStatus? statusFilter)
    {
        if (!zenOnly && !statusFilter.HasValue)
            return null;

        return rel =>
        {
            if (string.IsNullOrWhiteSpace(rel))
                return false;

            rel = rel.Replace('\\', '/').TrimStart('/');

            if (zenOnly)
            {
                if (_isZen == null || !_isZen(rel))
                    return false;
            }

            if (statusFilter.HasValue)
            {
                if (_meta == null) return false;
                var meta = _meta(rel);
                if (!meta.status.HasValue || meta.status.Value != statusFilter.Value)
                    return false;
            }

            return true;
        };
    }

    // ------------------------------------------------------------
    // Cooc helpers
    // ------------------------------------------------------------

    private bool IsGuideSelected()
        => (_cmbCoocMetric?.SelectedIndex ?? 0) == 8;

    private CoocMetric GetSelectedMetric()
    {
        int i = _cmbCoocMetric?.SelectedIndex ?? 0;
        return i switch
        {
            0 => CoocMetric.TopCooccurrences,
            1 => CoocMetric.DispersionScore,
            2 => CoocMetric.Frequency,
            3 => CoocMetric.Range,
            4 => CoocMetric.Dominance,
            5 => CoocMetric.PMI,
            6 => CoocMetric.LogDice,
            7 => CoocMetric.TScore,
            _ => CoocMetric.TopCooccurrences
        };
    }

    private async Task TriggerAutoRerunAsync()
    {
        if (string.IsNullOrWhiteSpace(_lastQuery) || _root == null || _meta == null)
            return;

        try { _autoRerunCts?.Cancel(); } catch { }
        _autoRerunCts = new CancellationTokenSource();
        var token = _autoRerunCts.Token;

        try
        {
            await Task.Delay(120, token);
            await StartSearchAsync();
        }
        catch (OperationCanceledException)
        {
            // newer UI change won
        }
    }

    private void Cancel()
    {
        try { _autoRerunCts?.Cancel(); } catch { }
        _autoRerunCts = null;

        try { _cts?.Cancel(); } catch { }
        _cts = null;

        if (_btnCancel != null) _btnCancel.IsEnabled = false;
    }

    private void ClearCoocUi()
    {
        if (_lstCoocChars != null) _lstCoocChars.ItemsSource = null;
        if (_lstCoocNgrams != null) _lstCoocNgrams.ItemsSource = null;
        if (_txtCoocSummary != null) _txtCoocSummary.Text = "No data yet.";
        if (_txtZipf != null) _txtZipf.Text = "";
        if (_txtLeftTitle != null) _txtLeftTitle.Text = "Top characters";
        if (_txtRightTitle != null) _txtRightTitle.Text = "Top bigrams / trigrams";
    }

    private async Task RefreshCoocUiFromCurrentStateAsync()
    {
        if (_groups.Count == 0)
        {
            ClearCoocUi();
            if (_gridMetricView != null) _gridMetricView.IsVisible = true;
            if (_scrollMetricGuide != null) _scrollMetricGuide.IsVisible = false;
            return;
        }

        if (IsGuideSelected())
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_gridMetricView != null) _gridMetricView.IsVisible = false;
                if (_scrollMetricGuide != null) _scrollMetricGuide.IsVisible = true;
                if (_txtMetricGuide != null) _txtMetricGuide.Text = MetricGuideText();
                if (_txtCoocSummary != null) _txtCoocSummary.Text = $"Guide (query='{_lastQuery}', context={_lastContextWidth} chars)";
                if (_txtZipf != null) _txtZipf.Text = "";
            });
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (_gridMetricView != null) _gridMetricView.IsVisible = true;
            if (_scrollMetricGuide != null) _scrollMetricGuide.IsVisible = false;
            if (_txtCoocSummary != null) _txtCoocSummary.Text = "Computing…";
        });

        int myVer = Interlocked.Increment(ref _metricComputeVersion);
        var metric = GetSelectedMetric();

        var snapshotGroups = _groups.ToList();
        string q = _lastQuery;
        int cw = _lastContextWidth;

        var result = await Task.Run(() =>
            SearchIndexService.ComputeCooccurrences(snapshotGroups, q, cw, metric, topK: 30));

        if (myVer != Volatile.Read(ref _metricComputeVersion))
            return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_txtCoocSummary != null) _txtCoocSummary.Text = result.Summary;

            if (_txtLeftTitle != null) _txtLeftTitle.Text = result.LeftTitle;
            if (_txtRightTitle != null) _txtRightTitle.Text = result.RightTitle;

            if (_lstCoocChars != null) _lstCoocChars.ItemsSource = result.Left;
            if (_lstCoocNgrams != null) _lstCoocNgrams.ItemsSource = result.Right;

            if (_txtZipf != null) _txtZipf.Text = result.ExtraLine ?? "";
        });
    }

    // ------------------------------------------------------------
    // Incremental tree append helpers
    // ------------------------------------------------------------

    private void ResetResultsView()
    {
        _groups.Clear();
        _groupsView.Clear();
        if (_btnExportTsv != null) _btnExportTsv.IsEnabled = false;
    }

    private void AppendGroupsToView(IReadOnlyList<SearchResultGroup> batch)
    {
        // UI-thread only
        for (int i = 0; i < batch.Count; i++)
            _groupsView.Add(batch[i]);
    }

    // ------------------------------------------------------------
    // Index build
    // ------------------------------------------------------------

    private async Task BuildIndexAsync()
    {
        if (_root == null || _originalDir == null || _translatedDir == null)
        {
            Status?.Invoke(this, "Search tab has no root context yet.");
            return;
        }

        bool force = _forceRebuildNextClick;
        _forceRebuildNextClick = false;

        Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            if (_btnCancel != null) _btnCancel.IsEnabled = true;

            SetProgress(force ? "Rebuilding index..." : "Updating index...");
            SetSummary(force ? "Rebuilding index... (full rebuild)" : "Updating index... (incremental)");

            var prog = new Progress<(int done, int total, string phase)>(p =>
            {
                SetProgress($"{p.phase} {p.done:n0}/{p.total:n0}");
            });

            await _svc.BuildOrUpdateAsync(_root, _originalDir, _translatedDir, forceRebuild: force, progress: prog, ct: ct);

            SetProgress(force ? "Index rebuilt." : "Index updated.");
            SetSummary("Index ready. Search will be fast.");
            Status?.Invoke(this, force ? "Search index rebuilt." : "Search index updated.");
        }
        catch (OperationCanceledException)
        {
            SetProgress("Canceled.");
            SetSummary("Canceled.");
        }
        catch (Exception ex)
        {
            SetProgress("Index build failed: " + ex.Message);
            SetSummary("Index build failed.");
            Status?.Invoke(this, "Index build failed: " + ex.Message);
        }
        finally
        {
            if (_btnCancel != null) _btnCancel.IsEnabled = false;
        }
    }

    // ------------------------------------------------------------
    // Search
    // ------------------------------------------------------------

    private async Task StartSearchAsync()
    {
        if (_root == null || _originalDir == null || _translatedDir == null || _meta == null)
        {
            Status?.Invoke(this, "Search tab has no root context yet.");
            return;
        }

        string q = (_txtQuery?.Text ?? "").Trim();
        if (q.Length == 0)
        {
            Status?.Invoke(this, "Enter a search query.");
            return;
        }

        bool includeO = _chkOriginal?.IsChecked == true;
        bool includeT = _chkTranslated?.IsChecked == true;
        if (!includeO && !includeT)
        {
            Status?.Invoke(this, "Select Original and/or Translated.");
            return;
        }

        bool zenOnly = ZenOnly();
        if (zenOnly && _isZen == null)
        {
            Status?.Invoke(this, "Zen filter is enabled but no Zen resolver was provided.");
            zenOnly = false;
        }

        var statusFilter = GetStatusFilter();
        var relFilter = BuildRelPathFilter(zenOnly, statusFilter);

        Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        int mySearchVer = Interlocked.Increment(ref _searchRunVersion);

        ResetResultsView();
        ClearCoocUi();

        try
        {
            if (_btnCancel != null) _btnCancel.IsEnabled = true;
            SetSummary($"Searching for: {q}");
            SetProgress("Loading index...");

            var manifest = await _svc.TryLoadAsync(_root);
            if (manifest == null)
            {
                SetProgress("No index found. Build it first.");
                Status?.Invoke(this, "No search index found. Click 'Build/Update Index' first.");
                return;
            }

            int contextWidth = GetContextWidth();

            _lastQuery = q;
            _lastContextWidth = contextWidth;

            int totalHits = 0;
            int totalGroups = 0;

            var prog = new Progress<SearchIndexService.SearchProgress>(p =>
            {
                if (mySearchVer != Volatile.Read(ref _searchRunVersion))
                    return;

                SetProgress($"{p.Phase}  verified {p.VerifiedDocs:n0}/{p.TotalDocsToVerify:n0}  groups={p.Groups:n0}  hits={p.TotalHits:n0}");
            });

            // Batch pending UI appends so we avoid resetting the tree and reduce dispatcher churn.
            var pendingUiBatch = new List<SearchResultGroup>(32);

            async Task FlushPendingUiBatchAsync(bool forceSummary)
            {
                if (pendingUiBatch.Count == 0 && !forceSummary) return;

                var batch = pendingUiBatch.Count > 0 ? pendingUiBatch.ToArray() : Array.Empty<SearchResultGroup>();
                pendingUiBatch.Clear();

                int snapshotGroups = totalGroups;
                int snapshotHits = totalHits;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (mySearchVer != Volatile.Read(ref _searchRunVersion))
                        return;

                    if (batch.Length > 0)
                        AppendGroupsToView(batch);

                    if (forceSummary || batch.Length > 0)
                        SetSummary($"Results: files={snapshotGroups:n0}, hits={snapshotHits:n0}");
                });
            }

            await foreach (var g in _svc.SearchAllAsync(
                               _root,
                               _originalDir,
                               _translatedDir,
                               manifest,
                               q,
                               includeO,
                               includeT,
                               fileMeta: rel => _meta(rel),
                               contextWidth: contextWidth,
                               progress: prog,
                               relPathFilter: relFilter,
                               ct: ct))
            {
                ct.ThrowIfCancellationRequested();

                if (mySearchVer != Volatile.Read(ref _searchRunVersion))
                    return;

                _groups.Add(g);
                pendingUiBatch.Add(g);

                totalGroups++;
                totalHits += g.Children.Count;

                // Flush early for first results, then in chunks
                if (totalGroups <= 10 || pendingUiBatch.Count >= 20)
                    await FlushPendingUiBatchAsync(forceSummary: false);
            }

            // Final flush
            await FlushPendingUiBatchAsync(forceSummary: true);

            int finalGroups = _groups.Count;
            int finalHits = totalHits;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (mySearchVer != Volatile.Read(ref _searchRunVersion))
                    return;

                SetSummary($"Done. files={finalGroups:n0}, hits={finalHits:n0}");
                if (_btnExportTsv != null) _btnExportTsv.IsEnabled = finalGroups > 0;
            });

            await RefreshCoocUiFromCurrentStateAsync();
        }
        catch (OperationCanceledException)
        {
            if (mySearchVer == Volatile.Read(ref _searchRunVersion))
            {
                SetProgress("Canceled.");
                SetSummary("Canceled.");
            }
        }
        catch (Exception ex)
        {
            if (mySearchVer == Volatile.Read(ref _searchRunVersion))
            {
                SetProgress("Search failed: " + ex.Message);
                SetSummary("Search failed.");
                Status?.Invoke(this, "Search failed: " + ex.Message);
            }
        }
        finally
        {
            if (mySearchVer == Volatile.Read(ref _searchRunVersion))
            {
                if (_btnCancel != null) _btnCancel.IsEnabled = false;
            }
        }
    }

    // ------------------------------------------------------------
    // Export
    // ------------------------------------------------------------

    private async Task ExportTsvAsync()
    {
        try
        {
            if (_groups.Count == 0)
            {
                Status?.Invoke(this, "No results to export.");
                return;
            }

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner?.StorageProvider == null)
            {
                Status?.Invoke(this, "Storage provider not available.");
                return;
            }

            var file = await owner.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Export search results (TSV)",
                SuggestedFileName = "search-results.tsv"
            });

            if (file == null) return;

            var sb = new StringBuilder(1024 * 16);
            sb.AppendLine("relPath\tside\tmatchIndex\tleft\tmatch\tright");

            foreach (var g in _groups)
            {
                foreach (var c in g.Children)
                {
                    string side = c.Side == SearchSide.Original ? "O" : "T";
                    sb.Append(g.RelPath).Append('\t')
                      .Append(side).Append('\t')
                      .Append(c.Hit.Index).Append('\t')
                      .Append(EscapeTsv(c.Hit.Left)).Append('\t')
                      .Append(EscapeTsv(c.Hit.Match)).Append('\t')
                      .Append(EscapeTsv(c.Hit.Right)).AppendLine();
                }
            }

            await using var s = await file.OpenWriteAsync();
            var bytes = Utf8NoBom.GetBytes(sb.ToString());
            await s.WriteAsync(bytes, 0, bytes.Length);

            Status?.Invoke(this, "Exported TSV.");
        }
        catch (Exception ex)
        {
            Status?.Invoke(this, "Export failed: " + ex.Message);
        }
    }

    private static string EscapeTsv(string s)
    {
        s ??= "";
        s = s.Replace("\t", " ").Replace("\r", "").Replace("\n", " ");
        return s;
    }

    // ------------------------------------------------------------
    // UI helpers
    // ------------------------------------------------------------

    private void SetProgress(string msg)
    {
        if (_txtProgress != null) _txtProgress.Text = msg;
    }

    private void SetSummary(string msg)
    {
        if (_txtSummary != null) _txtSummary.Text = msg;
    }

    private static string MetricGuideText()
    {
        return
@"Metric guide (using KWIC windows)

All metrics are computed from the SAME evidence:
each hit contributes a window string = Left + Match + Right using your Context width.

Two lists are shown:
- Left panel: single characters (fast signal; useful for names, particles, punctuation patterns)
- Right panel: bigrams + trigrams (phrase fragments; stronger collocation signal)

Fields in each row:
- freq  = total occurrences inside all windows (how often it appears near your query)
- range = number of distinct files where it appears at least once (dispersion proxy)
- score = depends on selected metric
- bar   = tiny visual scale of freq

Metrics:
1) Top co-occurrences (overview)
   Ranking = your stable dispersion-aware score:
   score = (freq / sqrt(1 + totalWindows)) * log(1 + range)
   What it tells you: 'what repeatedly shows up near this query, across many files'
   Best default.

2) Dispersion score (stable)
   Same as overview, but explicitly framed as 'reliable evidence over many documents'.
   Use when you want to avoid one-file artifacts.

3) Frequency (raw)
   score = freq
   Use when you only care about 'what shows up most', even if it’s dominated by one text.
   Good for: spotting formulaic refrains in a single long discourse.

4) Range (dispersion proxy)
   score = range
   Use when you want 'breadth' rather than 'intensity'.
   Good for: whether a phrase is widespread across the corpus.

5) Dominance (top-file share)
   score = topFileShare = maxCountInSingleFile / freq
   Interpretation:
   - 80–100% = probably a single-document artifact
   - 20–40%  = fairly dispersed
   Use when results look suspiciously 'too specific'.

6) PMI (window-based)
   PMI is association strength. It rewards exclusivity.
   High PMI often surfaces rare but very 'tight' collocations.
   Warning: PMI loves low-frequency one-offs. Always sanity-check freq + range.

7) logDice
   A lexicography-friendly association measure (more stable than PMI).
   Good for: 'dictionary-like' collocation candidates.

8) t-score
   Frequency-biased association: prefers collocations with lots of evidence.
   Good for: robust collocations that occur often.

How a researcher uses this:
Example query: 洞山 (Dongshan)
- Start with Top co-occurrences (overview): find names/titles that reliably surround Dongshan.
- Switch to Range: see which collocates appear across many files (tradition-wide usage).
- Switch to Dominance: verify you're not just seeing one famous text dominating.
- Switch to PMI/logDice: hunt for tighter phrase fragments (nicknames, technical terms).
- Use t-score to prioritize collocations with real volume.

Rule of thumb:
PMI/logDice = 'interesting and specific'
t-score/dispersion = 'reliable and common'
dominance = 'artifact detector'";
    }
}