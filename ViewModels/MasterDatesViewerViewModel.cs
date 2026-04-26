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
    // School colors — lighter variants for dark background contrast
    private static readonly Dictionary<string, SKColor> SchoolColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Linji"] = new SKColor(255, 107, 107),      // #FF6B6B
        ["Caodong"] = new SKColor(89, 179, 255),     // #59B3FF
        ["Fayan"] = new SKColor(81, 217, 150),       // #51D996
        ["Yunmen"] = new SKColor(255, 179, 71),      // #FFB347
        ["Guiyang"] = new SKColor(200, 84, 217),     // #C854D9
        ["Early Chan"] = new SKColor(126, 207, 255), // #7ECFFF
        ["Korean Seon"] = new SKColor(255, 167, 196),// #FFA7C4
        ["Hongzhou"] = new SKColor(178, 223, 138),   // #B2DF8A
    };
    private static readonly SKColor OtherColor = new(0xAA, 0xAA, 0xAA);

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
    private ISeries[] _lineageBarSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _lineageYAxes = { new Axis() };

    [ObservableProperty]
    private Axis[] _lineageXAxes = { new Axis() };

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
        BuildCenturyChart();
        BuildLineageChart();
    }

    public void RebuildTimeline(int count)
    {
        // Include masters with either floruit or death year
        var masters = _filteredMasters
            .Where(m => m.Floruit > 0 || m.Death > 0)
            .OrderBy(m => m.Floruit > 0 ? m.Floruit : (m.Death > 0 ? m.Death - 30 : 0))
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

        // Build a row series per school
        foreach (var group in schoolGroups)
        {
            var color = GetSchoolColor(group.Key);
            var values = new double[masters.Count];
            foreach (var m in group)
            {
                var idx = masters.IndexOf(m);
                if (idx >= 0)
                {
                    int start = m.Floruit > 0 ? m.Floruit : (m.Death > 0 ? m.Death - 30 : 0);
                    int end = m.Death > 0 ? m.Death : (m.Floruit > 0 ? m.Floruit + 20 : 0);
                    // Validate: skip if start >= end (data error)
                    if (start >= end) end = start + 5;
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

        var total = (double)_filteredMasters.Count;
        var series = groups.Select(g => new PieSeries<int>
        {
            Values = new[] { g.Count() },
            Name = $"{g.Key} ({g.Count()})",
            Fill = new SolidColorPaint(GetSchoolColor(g.Key)),
            DataLabelsPaint = new SolidColorPaint(SKColors.White),
            DataLabelsSize = 12,
            DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
            DataLabelsFormatter = p => $"{g.Key} {(g.Count() / total):P0}",
        } as ISeries).ToArray();

        SchoolPieSeries = series;
    }

    private void BuildCenturyChart()
    {
        var byFlor = _filteredMasters.Where(m => m.Floruit > 0).ToList();
        if (byFlor.Count == 0)
        {
            CenturyHistogramSeries = Array.Empty<ISeries>();
            return;
        }

        var minC = (byFlor.Min(m => m.Floruit) / 100) * 100;
        var maxC = (byFlor.Max(m => m.Floruit) / 100) * 100;
        var centuries = new List<string>();
        for (int c = minC; c <= maxC; c += 100)
            centuries.Add($"{c}s");

        var schoolGroups = byFlor.GroupBy(m => NormalizeSchool(m.School)).ToList();
        var series = new List<ISeries>();

        foreach (var group in schoolGroups.OrderByDescending(g => g.Count()))
        {
            var values = new List<double>();
            for (int c = minC; c <= maxC; c += 100)
                values.Add(group.Count(m => m.Floruit >= c && m.Floruit < c + 100));

            var color = GetSchoolColor(group.Key);
            series.Add(new StackedColumnSeries<double>
            {
                Values = values.ToArray(),
                Fill = new SolidColorPaint(color),
                Name = group.Key,
                MaxBarWidth = 40,
            });
        }

        CenturyHistogramSeries = series.ToArray();
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
                Name = "Masters",
                TextSize = 11,
                MinLimit = 0,
            }
        };
    }

    private void BuildLineageChart()
    {
        var teachers = _filteredMasters
            .Where(m => m.Students != null && m.Students.Count > 0)
            .OrderByDescending(m => m.Students!.Count)
            .Take(20)
            .Reverse()
            .ToList();

        if (teachers.Count == 0)
        {
            LineageBarSeries = Array.Empty<ISeries>();
            return;
        }

        var labels = teachers.Select(m => m.Names.FirstOrDefault() ?? "?").ToArray();
        var values = teachers.Select(m => (double)m.Students!.Count).ToArray();

        // Color each bar by the teacher's school
        var points = new List<ISeries>();
        var schoolGrouped = teachers.GroupBy(m => NormalizeSchool(m.School)).ToList();

        foreach (var group in schoolGrouped)
        {
            var color = GetSchoolColor(group.Key);
            var rowValues = new double[teachers.Count];
            foreach (var m in group)
            {
                var idx = teachers.IndexOf(m);
                if (idx >= 0)
                    rowValues[idx] = m.Students!.Count;
            }

            points.Add(new RowSeries<double>
            {
                Values = rowValues,
                Fill = new SolidColorPaint(color),
                Name = group.Key,
                MaxBarWidth = 18,
                Padding = 1,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsSize = 11,
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End,
                DataLabelsFormatter = p => p.Coordinate.PrimaryValue > 0
                    ? ((int)p.Coordinate.PrimaryValue).ToString()
                    : "",
            });
        }

        LineageBarSeries = points.ToArray();
        LineageYAxes = new[]
        {
            new Axis
            {
                Labels = labels,
                TextSize = 11,
                MinStep = 1,
                ForceStepToMin = true,
            }
        };
        LineageXAxes = new[]
        {
            new Axis
            {
                Name = "Number of Students",
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
