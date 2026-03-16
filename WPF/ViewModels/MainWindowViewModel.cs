using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPF.Models;

namespace WPF.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public ObservableCollection<Room> Rooms { get; } = [];
    public ObservableCollection<Guest> Guests { get; } = [];
    public ObservableCollection<Reservation> Reservations { get; } = [];

    [ObservableProperty] private Room? _selectedRoom;
    [ObservableProperty] private Guest? _selectedGuest;
    [ObservableProperty] private Reservation? _selectedReservation;

    [ObservableProperty] private string _roomNumber = string.Empty;
    [ObservableProperty] private RoomType _roomType;
    [ObservableProperty] private int _roomFloor;
    [ObservableProperty] private decimal _roomPrice;
    [ObservableProperty] private RoomStatus _roomStatus;

    [ObservableProperty] private string _guestFirstName = string.Empty;
    [ObservableProperty] private string _guestLastName = string.Empty;
    [ObservableProperty] private string _guestPhone = string.Empty;
    [ObservableProperty] private string _guestEmail = string.Empty;
    [ObservableProperty] private string _guestDocumentNumber = string.Empty;

    [ObservableProperty] private Room? _reservationRoom;
    [ObservableProperty] private Guest? _reservationGuest;
    [ObservableProperty] private DateTimeOffset _reservationCheckIn = DateTimeOffset.Now.Date;
    [ObservableProperty] private DateTimeOffset _reservationCheckOut = DateTimeOffset.Now.Date.AddDays(1);
    [ObservableProperty] private ReservationStatus _reservationStatus;
    [ObservableProperty] private string _reservationNotes = string.Empty;

    public static RoomType[] RoomTypes => Enum.GetValues<RoomType>();
    public static RoomStatus[] RoomStatuses => Enum.GetValues<RoomStatus>();
    public static ReservationStatus[] ReservationStatuses => Enum.GetValues<ReservationStatus>();

    private int _nextRoomId = 1;
    private int _nextGuestId = 1;
    private int _nextReservationId = 1;

    public MainWindowViewModel()
    {
        LoadTestData();
    }

    [RelayCommand]
    private void AddRoom()
    {
        var room = new Room
        {
            Id = _nextRoomId++,
            Number = RoomNumber,
            Type = RoomType,
            Floor = RoomFloor,
            PricePerNight = RoomPrice,
            Status = RoomStatus
        };
        Rooms.Add(room);
        ClearRoomForm();
    }

    [RelayCommand]
    private void DeleteRoom()
    {
        if (SelectedRoom is not null)
            Rooms.Remove(SelectedRoom);
    }

    [RelayCommand]
    private void EditRoom()
    {
        if (SelectedRoom is null) return;
        RoomNumber = SelectedRoom.Number;
        RoomType = SelectedRoom.Type;
        RoomFloor = SelectedRoom.Floor;
        RoomPrice = SelectedRoom.PricePerNight;
        RoomStatus = SelectedRoom.Status;
    }

    [RelayCommand]
    private void SaveRoom()
    {
        if (SelectedRoom is null) return;
        SelectedRoom.Number = RoomNumber;
        SelectedRoom.Type = RoomType;
        SelectedRoom.Floor = RoomFloor;
        SelectedRoom.PricePerNight = RoomPrice;
        SelectedRoom.Status = RoomStatus;
        ClearRoomForm();
    }

    private void ClearRoomForm()
    {
        RoomNumber = string.Empty;
        RoomType = RoomType.Single;
        RoomFloor = 0;
        RoomPrice = 0;
        RoomStatus = RoomStatus.Available;
    }

    [RelayCommand]
    private void AddGuest()
    {
        var guest = new Guest
        {
            Id = _nextGuestId++,
            FirstName = GuestFirstName,
            LastName = GuestLastName,
            Phone = GuestPhone,
            Email = GuestEmail,
            DocumentNumber = GuestDocumentNumber
        };
        Guests.Add(guest);
        ClearGuestForm();
    }

    [RelayCommand]
    private void DeleteGuest()
    {
        if (SelectedGuest is not null)
            Guests.Remove(SelectedGuest);
    }

    [RelayCommand]
    private void EditGuest()
    {
        if (SelectedGuest is null) return;
        GuestFirstName = SelectedGuest.FirstName;
        GuestLastName = SelectedGuest.LastName;
        GuestPhone = SelectedGuest.Phone;
        GuestEmail = SelectedGuest.Email;
        GuestDocumentNumber = SelectedGuest.DocumentNumber;
    }

    [RelayCommand]
    private void SaveGuest()
    {
        if (SelectedGuest is null) return;
        SelectedGuest.FirstName = GuestFirstName;
        SelectedGuest.LastName = GuestLastName;
        SelectedGuest.Phone = GuestPhone;
        SelectedGuest.Email = GuestEmail;
        SelectedGuest.DocumentNumber = GuestDocumentNumber;
        ClearGuestForm();
    }

    private void ClearGuestForm()
    {
        GuestFirstName = string.Empty;
        GuestLastName = string.Empty;
        GuestPhone = string.Empty;
        GuestEmail = string.Empty;
        GuestDocumentNumber = string.Empty;
    }

    [RelayCommand]
    private void AddReservation()
    {
        if (ReservationRoom is null || ReservationGuest is null) return;
        var reservation = new Reservation
        {
            Id = _nextReservationId++,
            Room = ReservationRoom,
            Guest = ReservationGuest,
            CheckInDate = ReservationCheckIn,
            CheckOutDate = ReservationCheckOut,
            Status = ReservationStatus,
            Notes = ReservationNotes
        };
        Reservations.Add(reservation);
        ClearReservationForm();
    }

    [RelayCommand]
    private void DeleteReservation()
    {
        if (SelectedReservation is not null)
            Reservations.Remove(SelectedReservation);
    }

    [RelayCommand]
    private void EditReservation()
    {
        if (SelectedReservation is null) return;
        ReservationRoom = SelectedReservation.Room;
        ReservationGuest = SelectedReservation.Guest;
        ReservationCheckIn = SelectedReservation.CheckInDate;
        ReservationCheckOut = SelectedReservation.CheckOutDate;
        ReservationStatus = SelectedReservation.Status;
        ReservationNotes = SelectedReservation.Notes;
    }

    [RelayCommand]
    private void SaveReservation()
    {
        if (SelectedReservation is null) return;
        if (ReservationRoom is not null) SelectedReservation.Room = ReservationRoom;
        if (ReservationGuest is not null) SelectedReservation.Guest = ReservationGuest;
        SelectedReservation.CheckInDate = ReservationCheckIn;
        SelectedReservation.CheckOutDate = ReservationCheckOut;
        SelectedReservation.Status = ReservationStatus;
        SelectedReservation.Notes = ReservationNotes;
        ClearReservationForm();
    }

    private void ClearReservationForm()
    {
        ReservationRoom = null;
        ReservationGuest = null;
        ReservationCheckIn = DateTimeOffset.Now.Date;
        ReservationCheckOut = DateTimeOffset.Now.Date.AddDays(1);
        ReservationStatus = ReservationStatus.Confirmed;
        ReservationNotes = string.Empty;
    }

    private void LoadTestData()
    {
        var r101 = new Room { Id = _nextRoomId++, Number = "101", Type = RoomType.Single, Floor = 1, PricePerNight = 150, Status = RoomStatus.Available };
        var r102 = new Room { Id = _nextRoomId++, Number = "102", Type = RoomType.Double, Floor = 1, PricePerNight = 250, Status = RoomStatus.Occupied };
        var r201 = new Room { Id = _nextRoomId++, Number = "201", Type = RoomType.Double, Floor = 2, PricePerNight = 280, Status = RoomStatus.Available };
        var r301 = new Room { Id = _nextRoomId++, Number = "301", Type = RoomType.Suite, Floor = 3, PricePerNight = 500, Status = RoomStatus.NeedsCleaning };
        var r202 = new Room { Id = _nextRoomId++, Number = "202", Type = RoomType.Single, Floor = 2, PricePerNight = 170, Status = RoomStatus.Available };

        Rooms.Add(r101);
        Rooms.Add(r102);
        Rooms.Add(r201);
        Rooms.Add(r301);
        Rooms.Add(r202);

        var g1 = new Guest { Id = _nextGuestId++, FirstName = "Jan", LastName = "Kowalski", Phone = "+48 600 100 200", Email = "jan.kowalski@email.com", DocumentNumber = "ABC123456" };
        var g2 = new Guest { Id = _nextGuestId++, FirstName = "Anna", LastName = "Nowak", Phone = "+48 601 200 300", Email = "anna.nowak@email.com", DocumentNumber = "DEF789012" };
        var g3 = new Guest { Id = _nextGuestId++, FirstName = "Piotr", LastName = "Wisniewski", Phone = "+48 602 300 400", Email = "piotr.w@email.com", DocumentNumber = "GHI345678" };

        Guests.Add(g1);
        Guests.Add(g2);
        Guests.Add(g3);

        Reservations.Add(new Reservation
        {
            Id = _nextReservationId++, Room = r102, Guest = g1,
            CheckInDate = DateTimeOffset.Now.Date.AddDays(-2), CheckOutDate = DateTimeOffset.Now.Date.AddDays(3),
            Status = ReservationStatus.Confirmed, Notes = "Late check-in requested"
        });
        Reservations.Add(new Reservation
        {
            Id = _nextReservationId++, Room = r201, Guest = g2,
            CheckInDate = DateTimeOffset.Now.Date.AddDays(5), CheckOutDate = DateTimeOffset.Now.Date.AddDays(10),
            Status = ReservationStatus.Confirmed, Notes = "Extra pillows"
        });
        Reservations.Add(new Reservation
        {
            Id = _nextReservationId++, Room = r301, Guest = g3,
            CheckInDate = DateTimeOffset.Now.Date.AddDays(-7), CheckOutDate = DateTimeOffset.Now.Date.AddDays(-1),
            Status = ReservationStatus.Completed, Notes = string.Empty
        });
    }
}
