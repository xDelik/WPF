using Avalonia.Controls;
using WPF.Models;
using WPF.ViewModels;

namespace WPF.Views;

public partial class RoomsView : UserControl
{
    public RoomsView() => InitializeComponent();

    private RoomsViewModel? Vm => DataContext as RoomsViewModel;

    private void OnFilterAll(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm!.StatusFilter = null;
    private void OnFilterAvailable(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm!.StatusFilter = RoomStatus.Available;
    private void OnFilterOccupied(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm!.StatusFilter = RoomStatus.Occupied;
    private void OnFilterNeedsCleaning(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm!.StatusFilter = RoomStatus.NeedsCleaning;
}
