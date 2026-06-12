using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPF.Models;
using WPF.Services;

namespace WPF.ViewModels;

public record CalendarBar(Reservation Reservation, int StartColumn, int SpanColumns, bool LeftClipped, bool RightClipped);

public record CalendarCell(Room Room, DateTimeOffset Date);

public record CalendarRow(Room Room, IReadOnlyList<CalendarBar> Bars, IReadOnlyList<CalendarCell> Cells);

public partial class CalendarViewModel : ObservableObject
{
    public const int WindowDays = 14;

    private readonly ObservableCollection<Room> _rooms;
    private readonly ObservableCollection<Reservation> _reservations;
    private readonly Action<Reservation> _onBarClick;
    private readonly Action<Room, DateTimeOffset, DateTimeOffset> _onRangeSelect;
    private readonly Action _save;
    private readonly INotificationService _notifier;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowLabel))]
    [NotifyPropertyChangedFor(nameof(DayHeaders))]
    [NotifyPropertyChangedFor(nameof(Rows))]
    private DateTimeOffset _windowStart = DateTimeOffset.Now.Date;

    public CalendarViewModel(
        ObservableCollection<Room> rooms,
        ObservableCollection<Reservation> reservations,
        Action<Reservation> onBarClick,
        Action<Room, DateTimeOffset, DateTimeOffset> onRangeSelect,
        Action save,
        INotificationService notifier)
    {
        _rooms = rooms;
        _reservations = reservations;
        _onBarClick = onBarClick;
        _onRangeSelect = onRangeSelect;
        _save = save;
        _notifier = notifier;
        _rooms.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Rows));
        _reservations.CollectionChanged += (_, e) =>
        {
            if (e.OldItems is not null)
                foreach (Reservation r in e.OldItems) r.PropertyChanged -= OnReservationPropertyChanged;
            if (e.NewItems is not null)
                foreach (Reservation r in e.NewItems) r.PropertyChanged += OnReservationPropertyChanged;
            OnPropertyChanged(nameof(Rows));
        };
    }

    public CalendarViewModel() : this(new(), new(), _ => { }, (_, _, _) => { }, () => { }, new NullNotificationService()) { }

    private void OnReservationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Reservation.CheckInDate) or nameof(Reservation.CheckOutDate)
            or nameof(Reservation.Status) or nameof(Reservation.Room))
            OnPropertyChanged(nameof(Rows));
    }

    public bool TryReschedule(Reservation reservation, DateTimeOffset newCheckIn, DateTimeOffset newCheckOut)
    {
        if (newCheckOut <= newCheckIn) return false;

        if (HotelRules.HasOverlap(_reservations, reservation.Room, newCheckIn, newCheckOut, excludingId: reservation.Id))
        {
            _notifier.Push($"Room {reservation.Room.Number} is already booked for those dates.");
            return false;
        }

        if (newCheckIn >= reservation.CheckOutDate)
        {
            reservation.CheckOutDate = newCheckOut;
            reservation.CheckInDate = newCheckIn;
        }
        else
        {
            reservation.CheckInDate = newCheckIn;
            reservation.CheckOutDate = newCheckOut;
        }

        HotelRules.RefreshStatuses(_rooms, _reservations, DateTime.Today);
        _save();
        _notifier.Push($"Reservation for {reservation.Guest.FullName} moved to {newCheckIn:dd MMM} → {newCheckOut:dd MMM}.");
        return true;
    }

    public string WindowLabel
    {
        get
        {
            var end = WindowStart.AddDays(WindowDays - 1);
            return $"{WindowStart:dd MMM} → {end:dd MMM yyyy}";
        }
    }

    public IEnumerable<DateTimeOffset> DayHeaders =>
        Enumerable.Range(0, WindowDays).Select(i => WindowStart.AddDays(i));

    public IEnumerable<CalendarRow> Rows
    {
        get
        {
            var windowEnd = WindowStart.AddDays(WindowDays);
            foreach (var room in _rooms)
            {
                var bars = _reservations
                    .Where(r => r.Status != ReservationStatus.Cancelled
                                && r.Room.Id == room.Id
                                && r.CheckInDate < windowEnd
                                && r.CheckOutDate > WindowStart)
                    .Select(r =>
                    {
                        var startOffset = (int)(r.CheckInDate.Date - WindowStart.Date).TotalDays;
                        var startCol = Math.Max(0, startOffset);
                        var endOffset = (int)(r.CheckOutDate.Date - WindowStart.Date).TotalDays;
                        var endCol = Math.Min(WindowDays, endOffset);
                        var span = Math.Max(1, endCol - startCol);
                        return new CalendarBar(r, startCol, span, startOffset < 0, endOffset > WindowDays);
                    })
                    .ToList();

                var cells = Enumerable.Range(0, WindowDays)
                    .Select(i => new CalendarCell(room, WindowStart.AddDays(i)))
                    .ToList();

                yield return new CalendarRow(room, bars, cells);
            }
        }
    }

    [RelayCommand] private void Prev() => WindowStart = WindowStart.AddDays(-WindowDays);
    [RelayCommand] private void Next() => WindowStart = WindowStart.AddDays(WindowDays);
    [RelayCommand] private void Today() => WindowStart = DateTimeOffset.Now.Date;

    [RelayCommand]
    private void OpenReservation(Reservation r) => _onBarClick(r);

    public void SelectRange(Room room, DateTimeOffset checkIn, DateTimeOffset checkOut)
        => _onRangeSelect(room, checkIn, checkOut);
}
