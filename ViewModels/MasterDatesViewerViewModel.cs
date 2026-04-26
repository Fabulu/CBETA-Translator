using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using ReadZen.App.Models;

namespace ReadZen.App.ViewModels;

public partial class MasterDatesViewerViewModel : ViewModelBase
{
    // School colors
    private static readonly Dictionary<string, SKColor> SchoolColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Linji"] = new SKColor(0xE5, 0x39, 0x35),
        ["Caodong"] = new SKColor(0x1E, 0x88, 0xE5),
        ["Fayan"] = new SKColor(0x43, 0xA0, 0x47),
        ["Yunmen"] = new SKColor(0xFB, 0x8C, 0x00),
        ["Guiyang"] = new SKColor(0x8E, 0x24, 0xAA),
        ["Early Chan"] = new SKColor(0x78, 0x90, 0x9C),
    };
    private static readonly SKColor OtherColor = new(0x75, 0x75, 0x75);

    private readonly List<MasterDateEntry> _allMasters;
    private List<MasterDateEntry> _filteredMasters;

    [ObservableProperty]
    private string _filterText = "";

    [ObservableProperty]
    private ISeries[] _timelineSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _timelineYAxes = { new Axis() };

    [ObservableProperty]
    private Axis[] _timelineXAxes = { new Axis() };

    [ObservableProperty]
    private ISeries[] _schoolPieSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _centuryHistogramSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _centuryXAxes = { new Axis() };

    [ObservableProperty]
    private Axis[] _centuryYAxes = { new Axis() };

    [ObservableProperty]
    private ISeries[] _regionBarSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _regionYAxes = { new Axis() };

    [ObservableProperty]
    private Axis[] _regionXAxes = { new Axis() };

    // Selected master card
    [ObservableProperty]
    private bool _hasSelectedMaster;

    [ObservableProperty]
    private string _selectedMasterName = "";

    [ObservableProperty]
    private string _selectedMasterDates = "";

    [ObservableProperty]
    private string _selectedMasterSchool = "";

    [ObservableProperty]
    private string _selectedMasterRegion = "";

    [ObservableProperty]
    private string _selectedMasterTeacher = "";

    [ObservableProperty]
    private string _selectedMasterStudents = "";

    [ObservableProperty]
    private string _selectedMasterNotes = "";

    public int FilteredCount => _filteredMasters.Count;

    public MasterDatesViewerViewModel(List<MasterDateEntry> masters)
    {
        _allMasters = masters ?? new List<MasterDateEntry>();
        _filteredMasters = _allMasters;
        BuildAllCharts();
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter(value);
    }

    private void ApplyFilter(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            _filteredMasters = _allMasters;
        }
        else
        {
            var terms = filter.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            _filteredMasters = _allMasters.Where(m =>
                terms.All(t =>
                    m.Names.Any(n => n.Contains(t, StringComparison.OrdinalIgnoreCase)) ||
                    (m.School?.Contains(t, StringComparison.OrdinalIgnoreCase) == true) ||
                    (m.Region?.Contains(t, StringComparison.OrdinalIgnoreCase) == true) ||
                    (m.Teacher?.Contains(t, StringComparison.OrdinalIgnoreCase) == true)
                )).ToList();
        }
        OnPropertyChanged(nameof(FilteredCount));
        BuildAllCharts();
    }

    private void BuildAllCharts()
    {
        RebuildTimeline(30);
        BuildSchoolPie();
        BuildCenturyHistogram();
        BuildRegionBars();
    }

    public void RebuildTimeline(int count)
    {
        var masters = _filteredMasters
            .Where(m => m.Floruit > 0)
            .OrderBy(m => m.Floruit)
            .Take(count)
            .Reverse()
            .ToList();

        if (masters.Count == 0)
        {
            TimelineSeries = Array.Empty<ISeries>();
            TimelineYAxes = new[] { new Axis() };
            TimelineXAxes = new[] { new Axis() };
            return;
        }

        // Group by school for colored bars
        var schoolGroups = masters
            .GroupBy(m => NormalizeSchool(m.School))
            .ToList();

        var seriesList = new List<ISeries>();
        var labels = masters.Select(m => m.Names.FirstOrDefault() ?? "?").ToArray();

        // Build a stacked row series per school
        foreach (var group in schoolGroups)
        {
            var color = GetSchoolColor(group.Key);
            var values = new double[masters.Count];
            foreach (var m in group)
            {
                var idx = masters.IndexOf(m);
                if (idx >= 0)
                {
                    var start = m.Floruit;
                    var end = m.Death > 0 ? m.Death : m.Floruit + 40; // estimate if no death
                    values[idx] = end - start;
                }
            }

            seriesList.Add(new RowSeries<double>
            {
                Values = values,
                Name = group.Key,
                Fill = new SolidColorPaint(color),
                MaxBarWidth = 16,
                Padding = 1,
                DataLabelsSize = 10,
            });
        }

        TimelineSeries = seriesList.ToArray();
        TimelineYAxes = new[]
        {
            new Axis
            {
                Labels = labels,
                TextSize = 11,
                MinStep = 1,
                ForceStepToMin = true,
            }
        };
        TimelineXAxes = new[]
        {
            new Axis
            {
                Name = "Lifespan (years)",
                TextSize = 11,
                MinLimit = 0,
            }
        };

        // Select first master for the card
        if (masters.Count > 0)
            SelectMaster(masters[0]);
    }

    private void BuildSchoolPie()
    {
        var groups = _filteredMasters
            .GroupBy(m => NormalizeSchool(m.School))
            .OrderByDescending(g => g.Count())
            .ToList();

        var series = groups.Select(g => new PieSeries<int>
        {
            Values = new[] { g.Count() },
            Name = $"{g.Key} ({g.Count()})",
            Fill = new SolidColorPaint(GetSchoolColor(g.Key)),
            DataLabelsPaint = new SolidColorPaint(SKColors.White),
            DataLabelsSize = 12,
            DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
            DataLabelsFormatter = p => g.Key,
        } as ISeries).ToArray();

        SchoolPieSeries = series;
    }

    private void BuildCenturyHistogram()
    {
        var byFlor = _filteredMasters.Where(m => m.Floruit > 0).ToList();
        if (byFlor.Count == 0)
        {
            CenturyHistogramSeries = Array.Empty<ISeries>();
            return;
        }

        var minCentury = (byFlor.Min(m => m.Floruit) / 100) * 100;
        var maxCentury = (byFlor.Max(m => m.Floruit) / 100) * 100;

        var centuries = new List<string>();
        var counts = new List<double>();

        for (int c = minCentury; c <= maxCentury; c += 100)
        {
            var label = $"{c}-{c + 99}";
            var count = byFlor.Count(m => m.Floruit >= c && m.Floruit < c + 100);
            centuries.Add(label);
            counts.Add(count);
        }

        CenturyHistogramSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = counts.ToArray(),
                Name = "Masters active",
                Fill = new SolidColorPaint(new SKColor(0x42, 0xA5, 0xF5)),
                MaxBarWidth = 40,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsSize = 11,
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
                DataLabelsFormatter = p => p.Coordinate.PrimaryValue > 0
                    ? ((int)p.Coordinate.PrimaryValue).ToString()
                    : "",
            }
        };

        CenturyXAxes = new[]
        {
            new Axis
            {
                Labels = centuries.ToArray(),
                TextSize = 11,
                LabelsRotation = -30,
                Name = "Century",
            }
        };
        CenturyYAxes = new[]
        {
            new Axis
            {
                Name = "Count",
                TextSize = 11,
                MinLimit = 0,
            }
        };
    }

    private void BuildRegionBars()
    {
        var groups = _filteredMasters
            .Where(m => !string.IsNullOrWhiteSpace(m.Region))
            .GroupBy(m => m.Region!.Trim())
            .OrderByDescending(g => g.Count())
            .Take(20)
            .Reverse()
            .ToList();

        if (groups.Count == 0)
        {
            RegionBarSeries = Array.Empty<ISeries>();
            return;
        }

        var labels = groups.Select(g => g.Key).ToArray();
        var values = groups.Select(g => (double)g.Count()).ToArray();

        RegionBarSeries = new ISeries[]
        {
            new RowSeries<double>
            {
                Values = values,
                Name = "Masters per region",
                Fill = new SolidColorPaint(new SKColor(0x66, 0xBB, 0x6A)),
                MaxBarWidth = 20,
                Padding = 2,
                DataLabelsSize = 11,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End,
                DataLabelsFormatter = p => ((int)p.Coordinate.PrimaryValue).ToString(),
            }
        };

        RegionYAxes = new[]
        {
            new Axis
            {
                Labels = labels,
                TextSize = 11,
                MinStep = 1,
                ForceStepToMin = true,
            }
        };
        RegionXAxes = new[]
        {
            new Axis
            {
                Name = "Count",
                TextSize = 11,
                MinLimit = 0,
            }
        };
    }

    private void SelectMaster(MasterDateEntry m)
    {
        HasSelectedMaster = true;
        SelectedMasterName = m.Names.FirstOrDefault() ?? "(unnamed)";
        SelectedMasterDates = m.Floruit > 0 && m.Death > 0
            ? $"{m.Floruit} - {m.Death} CE"
            : m.Floruit > 0
                ? $"fl. {m.Floruit} CE"
                : m.Death > 0 ? $"d. {m.Death} CE" : "dates unknown";
        SelectedMasterSchool = !string.IsNullOrWhiteSpace(m.School) ? $"School: {m.School}" : "";
        SelectedMasterRegion = !string.IsNullOrWhiteSpace(m.Region) ? $"Region: {m.Region}" : "";
        SelectedMasterTeacher = !string.IsNullOrWhiteSpace(m.Teacher) ? $"Teacher: {m.Teacher}" : "";
        SelectedMasterStudents = m.Students is { Count: > 0 }
            ? $"Students: {string.Join(", ", m.Students)}"
            : "";
        SelectedMasterNotes = m.Notes ?? "";
    }

    private static string NormalizeSchool(string? school)
    {
        if (string.IsNullOrWhiteSpace(school)) return "Other";
        var s = school.Trim();
        if (SchoolColors.ContainsKey(s)) return s;
        // Try partial match
        foreach (var key in SchoolColors.Keys)
        {
            if (s.Contains(key, StringComparison.OrdinalIgnoreCase))
                return key;
        }
        return "Other";
    }

    private static SKColor GetSchoolColor(string school)
    {
        return SchoolColors.TryGetValue(school, out var c) ? c : OtherColor;
    }
}
