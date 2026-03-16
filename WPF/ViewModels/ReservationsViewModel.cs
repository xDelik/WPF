using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPF.Models;

namespace WPF.ViewModels;

public partial class ReservationsViewModel : ObservableObject
{
    public ObservableCollection<Reservation> Reservations { get; } = [];

    public ObservableCollection<Room> Rooms { get; }
    public ObservableCollection<Guest> Guests { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveReservationCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteReservationCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddReservationCommand))]
    private Reservation? _selectedReservation;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddReservationCommand))]
    private Room? _reservationRoom;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddReservationCommand))]
    private Guest? _reservationGuest;

    [ObservableProperty] private DateTimeOffset _reservationCheckIn = DateTimeOffset.Now.Date;
    [ObservableProperty] private DateTimeOffset _reservationCheckOut = DateTimeOffset.Now.Date.AddDays(1);
    [ObservableProperty] private ReservationStatus _reservationStatus;
    [ObservableProperty] private string _reservationNotes = string.Empty;

    public static ReservationStatus[] ReservationStatuses => Enum.GetValues<ReservationStatus>();

    private int _nextId = 1;

    public ReservationsViewModel(ObservableCollection<Room> rooms, ObservableCollection<Guest> guests)
    {
        Rooms = rooms;
        Guests = guests;
    }

    [RelayCommand(CanExecute = nameof(CanAddReservation))]
    private void AddReservation()
    {
        if (ReservationRoom is null || ReservationGuest is null) return;
        var reservation = new Reservation
        {
            Id = _nextId++,
            Room = ReservationRoom,
            Guest = ReservationGuest,
            CheckInDate = ReservationCheckIn,
            CheckOutDate = ReservationCheckOut,
            Status = ReservationStatus,
            Notes = ReservationNotes
        };
        Reservations.Add(reservation);
        ClearForm();
    }

    [RelayCommand(CanExecute = nameof(CanModifyReservation))]
    private void DeleteReservation()
    {
        if (SelectedReservation is not null)
            Reservations.Remove(SelectedReservation);
    }

    [RelayCommand(CanExecute = nameof(CanModifyReservation))]
    private void SaveReservation()
    {
        if (SelectedReservation is null) return;
        if (ReservationRoom is not null) SelectedReservation.Room = ReservationRoom;
        if (ReservationGuest is not null) SelectedReservation.Guest = ReservationGuest;
        SelectedReservation.CheckInDate = ReservationCheckIn;
        SelectedReservation.CheckOutDate = ReservationCheckOut;
        SelectedReservation.Status = ReservationStatus;
        SelectedReservation.Notes = ReservationNotes;
        ClearForm();
    }

    [RelayCommand]
    private void Clear() => ClearForm();

    private bool CanAddReservation() => ReservationRoom is not null
                                        && ReservationGuest is not null
                                        && SelectedReservation is null;
    private bool CanModifyReservation() => SelectedReservation is not null;

    partial void OnSelectedReservationChanged(Reservation? value)
    {
        if (value is null) return;
        ReservationRoom = value.Room;
        ReservationGuest = value.Guest;
        ReservationCheckIn = value.CheckInDate;
        ReservationCheckOut = value.CheckOutDate;
        ReservationStatus = value.Status;
        ReservationNotes = value.Notes;
    }

    private void ClearForm()
    {
        SelectedReservation = null;
        ReservationRoom = null;
        ReservationGuest = null;
        ReservationCheckIn = DateTimeOffset.Now.Date;
        ReservationCheckOut = DateTimeOffset.Now.Date.AddDays(1);
        ReservationStatus = ReservationStatus.Confirmed;
        ReservationNotes = string.Empty;
    }
}
