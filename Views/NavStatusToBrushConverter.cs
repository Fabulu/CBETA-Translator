using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Views;

public sealed class NavStatusToBrushConverter : IValueConverter
{
    public static NavStatusToBrushConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // IMPORTANT: accept TranslationStatus, not NavStatus
        if (value is not TranslationStatus s)
            s = TranslationStatus.Red;

        // opaque backgrounds (as requested)
        return s switch
        {
            TranslationStatus.Green => new SolidColorBrush(Color.Parse("#FF1F6F2A")), // green
            TranslationStatus.Yellow => new SolidColorBrush(Color.Parse("#FF8A6E00")), // yellow
            _ => new SolidColorBrush(Color.Parse("#FF7A1E1E")), // red
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
