using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WPF.ViewModels;

public class AppPageConverter : IValueConverter
{
    public AppPage Page { get; }
    public AppPageConverter(AppPage page) { Page = page; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is AppPage p && p == Page;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Page : (object)Avalonia.Data.BindingOperations.DoNothing;

    public static readonly AppPageConverter Rooms = new(AppPage.Rooms);
    public static readonly AppPageConverter Guests = new(AppPage.Guests);
    public static readonly AppPageConverter Reservations = new(AppPage.Reservations);
    public static readonly AppPageConverter Dashboard = new(AppPage.Dashboard);
    public static readonly AppPageConverter Calendar = new(AppPage.Calendar);
}
