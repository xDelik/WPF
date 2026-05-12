using Avalonia.Controls;
using WPF.Models;
using WPF.ViewModels;

namespace WPF.Views;

public partial class ReservationsView : UserControl
{
    public ReservationsView() => InitializeComponent();

    private ReservationsViewModel? Vm => DataContext as ReservationsViewModel;

    private void OnFilterAll(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm!.StatusFilter = null;
    private void OnFilterConfirmed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm!.StatusFilter = ReservationStatus.Confirmed;
    private void OnFilterCancelled(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm!.StatusFilter = ReservationStatus.Cancelled;
    private void OnFilterCompleted(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm!.StatusFilter = ReservationStatus.Completed;
}
