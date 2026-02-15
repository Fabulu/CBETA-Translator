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

        // Soft pastel backgrounds (requested palette)
        return s switch
        {
            TranslationStatus.Green => new SolidColorBrush(Color.Parse("#FFE0F0E0")),
            TranslationStatus.Yellow => new SolidColorBrush(Color.Parse("#FFFEF5D0")),
            _ => new SolidColorBrush(Color.Parse("#FFEDDCDC")),
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
