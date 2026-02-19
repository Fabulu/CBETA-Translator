using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CbetaTranslator.App.Models;
using System;
using System.Globalization;

namespace CbetaTranslator.App.Views;

public sealed class NavStatusToBrushConverter : IValueConverter
{
    public static NavStatusToBrushConverter Instance { get; } = new();

    private static IBrush Brush(string key, IBrush fallback)
    {
        var app = Application.Current;
        if (app is null) return fallback;

        if (app.TryFindResource(key, theme: null, out var res) && res is IBrush b)
            return b;

        return fallback;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = value is TranslationStatus ts ? ts : TranslationStatus.Red;

        // Pull *active* brushes (your runtime swapped tokens)
        return s switch
        {
            TranslationStatus.Green => Brush("NavStatusGreenBg", Brushes.Transparent),
            TranslationStatus.Yellow => Brush("NavStatusYellowBg", Brushes.Transparent),
            _ => Brush("NavStatusRedBg", Brushes.Transparent),
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}