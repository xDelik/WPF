using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using WPF.Models;

namespace WPF.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ObservableCollection<Room> _rooms;
    private readonly ObservableCollection<Guest> _guests;
    private readonly ObservableCollection<Reservation> _reservations;

    public DashboardViewModel(
        ObservableCollection<Room> rooms,
        ObservableCollection<Guest> guests,
        ObservableCollection<Reservation> reservations)
    {
        _rooms = rooms;
        _guests = guests;
        _reservations = reservations;

        HookCollection(_rooms);
        HookCollection(_guests);
        HookCollection(_reservations);
    }

    public DashboardViewModel() : this(new(), new(), new()) { }

    public string OccupancyText
    {
        get
        {
            var total = _rooms.Count;
            if (total == 0) return "—";
            var occupied = _rooms.Count(r => r.Status == RoomStatus.Occupied);
            return $"{(double)occupied / total:P0}";
        }
    }

    public int AvailableCount => _rooms.Count(r => r.Status == RoomStatus.Available);
    public int RoomsTotal => _rooms.Count;

    public int ArrivalsToday => _reservations.Count(r =>
        r.Status == ReservationStatus.Confirmed &&
        r.CheckInDate.Date == DateTimeOffset.Now.Date);

    public int GuestsCount => _guests.Count;

    public IEnumerable<Reservation> RecentReservations =>
        _reservations.OrderByDescending(r => r.Id).Take(5);

    public string TodayLabel => DateTimeOffset.Now.ToString("dddd, dd MMM");

    private void HookCollection(INotifyCollectionChanged source)
    {
        source.CollectionChanged += (_, e) =>
        {
            if (e.OldItems is not null)
                foreach (INotifyPropertyChanged i in e.OldItems) i.PropertyChanged -= OnItemChanged;
            if (e.NewItems is not null)
                foreach (INotifyPropertyChanged i in e.NewItems) i.PropertyChanged += OnItemChanged;
            RaiseAll();
        };
        foreach (INotifyPropertyChanged i in (System.Collections.IEnumerable)source)
            i.PropertyChanged += OnItemChanged;
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e) => RaiseAll();

    private void RaiseAll()
    {
        OnPropertyChanged(nameof(OccupancyText));
        OnPropertyChanged(nameof(AvailableCount));
        OnPropertyChanged(nameof(RoomsTotal));
        OnPropertyChanged(nameof(ArrivalsToday));
        OnPropertyChanged(nameof(GuestsCount));
        OnPropertyChanged(nameof(RecentReservations));
    }
}
