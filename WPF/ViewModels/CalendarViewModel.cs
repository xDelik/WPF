using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPF.Models;

namespace WPF.ViewModels;

public record CalendarBar(Reservation Reservation, int StartColumn, int SpanColumns);

public record CalendarCell(Room Room, DateTimeOffset Date);

public record CalendarRow(Room Room, IReadOnlyList<CalendarBar> Bars, IReadOnlyList<CalendarCell> Cells);

public partial class CalendarViewModel : ObservableObject
{
    public const int WindowDays = 14;

    private readonly ObservableCollection<Room> _rooms;
    private readonly ObservableCollection<Reservation> _reservations;
    private readonly Action<Reservation> _onBarClick;
    private readonly Action<Room, DateTimeOffset> _onEmptyCellClick;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowLabel))]
    [NotifyPropertyChangedFor(nameof(DayHeaders))]
    [NotifyPropertyChangedFor(nameof(Rows))]
    private DateTimeOffset _windowStart = DateTimeOffset.Now.Date;

    public CalendarViewModel(
        ObservableCollection<Room> rooms,
        ObservableCollection<Reservation> reservations,
        Action<Reservation> onBarClick,
        Action<Room, DateTimeOffset> onEmptyCellClick)
    {
        _rooms = rooms;
        _reservations = reservations;
        _onBarClick = onBarClick;
        _onEmptyCellClick = onEmptyCellClick;
        _rooms.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Rows));
        _reservations.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Rows));
    }

    public CalendarViewModel() : this(new(), new(), _ => { }, (_, _) => { }) { }

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
                        return new CalendarBar(r, startCol, span);
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

    [RelayCommand]
    private void OpenEmptyCell(CalendarCell cell) => _onEmptyCellClick(cell.Room, cell.Date);
}
