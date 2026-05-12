using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WPF.ViewModels;

public class BoolToBrushConverter : IValueConverter
{
    public IBrush TrueBrush { get; set; } = Brushes.White;
    public IBrush FalseBrush { get; set; } = Brushes.Transparent;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TrueBrush : FalseBrush;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindingOperations.DoNothing;

    public static readonly BoolToBrushConverter AccentOrFaint = new()
    {
        TrueBrush = new SolidColorBrush(Color.Parse("#6C95EB")),
        FalseBrush = new SolidColorBrush(Color.Parse("#393B40"))
    };
}

public class BoolToOpacity : IValueConverter
{
    public double TrueValue { get; set; } = 1.0;
    public double FalseValue { get; set; } = 0.4;
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TrueValue : FalseValue;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindingOperations.DoNothing;

    public static readonly BoolToOpacity Visible = new();
}
