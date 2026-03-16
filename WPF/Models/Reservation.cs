using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF.Models;

public partial class Reservation : ObservableObject
{
    [ObservableProperty] private int _id;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    private Room _room = null!;

    [ObservableProperty] private Guest _guest = null!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Nights))]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    private DateTimeOffset _checkInDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Nights))]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    private DateTimeOffset _checkOutDate;

    [ObservableProperty] private ReservationStatus _status;
    [ObservableProperty] private string _notes = string.Empty;

    public int Nights => (CheckOutDate - CheckInDate).Days;
    public decimal TotalPrice => Nights * Room.PricePerNight;
}
