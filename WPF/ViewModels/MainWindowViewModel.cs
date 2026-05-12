using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPF.Models;
using WPF.Persistence;
using WPF.Services;

namespace WPF.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly HotelStore _store = new();

    public INotificationService Notifier { get; } = new NotificationService();

    public RoomsViewModel RoomsVm { get; }
    public GuestsViewModel GuestsVm { get; }
    public ReservationsViewModel ReservationsVm { get; }
    public DashboardViewModel DashboardVm { get; }
    public CalendarViewModel CalendarVm { get; }

    [ObservableProperty] private bool _isSidebarOpen = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentPage))]
    private AppPage _selectedPage = AppPage.Dashboard;

    public object CurrentPage => SelectedPage switch
    {
        AppPage.Dashboard => DashboardVm,
        AppPage.Rooms => RoomsVm,
        AppPage.Guests => GuestsVm,
        AppPage.Reservations => ReservationsVm,
        AppPage.Calendar => CalendarVm,
        _ => DashboardVm
    };

    [RelayCommand]
    private void SelectPage(AppPage page) => SelectedPage = page;

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarOpen = !IsSidebarOpen;

    public MainWindowViewModel()
    {
        Console.WriteLine($"[Persistence] {_store.Path}");

        var reservations = new ObservableCollection<Reservation>();
        RoomsVm = new RoomsViewModel(reservations, Save, Notifier);
        GuestsVm = new GuestsViewModel(reservations, Save, Notifier);
        ReservationsVm = new ReservationsViewModel(
            RoomsVm.Rooms,
            GuestsVm.Guests,
            reservations,
            Save,
            Notifier,
            gotoGuestsPage: () => SelectedPage = AppPage.Guests);

        DashboardVm = new DashboardViewModel(RoomsVm.Rooms, GuestsVm.Guests, ReservationsVm.Reservations);
        CalendarVm = new CalendarViewModel(
            RoomsVm.Rooms,
            ReservationsVm.Reservations,
            OpenReservationFromCalendar,
            OpenEmptyCellFromCalendar);

        LoadOrSeed();
    }

    private void OpenReservationFromCalendar(Reservation r)
    {
        SelectedPage = AppPage.Reservations;
        ReservationsVm.SelectedReservation = r;
    }

    private void OpenEmptyCellFromCalendar(Room room, DateTimeOffset date)
    {
        SelectedPage = AppPage.Reservations;
        ReservationsVm.OpenWizardFor(room, date);
    }

    private void LoadOrSeed()
    {
        var data = _store.Load();
        if (data is null)
        {
            SeedDefaultData();
            Save();
            return;
        }

        foreach (var r in data.Rooms) RoomsVm.Rooms.Add(r);
        foreach (var g in data.Guests) GuestsVm.Guests.Add(g);
        foreach (var r in data.Reservations) ReservationsVm.Reservations.Add(r);
    }

    private void Save()
    {
        try
        {
            _store.Save(RoomsVm.Rooms, GuestsVm.Guests, ReservationsVm.Reservations);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Persistence] Save failed: {ex}");
        }
    }

    private void SeedDefaultData()
    {
        var r101 = new Room { Id = 1, Number = "101", Type = RoomType.Single, Floor = 1, PricePerNight = 150, Status = RoomStatus.Available };
        var r102 = new Room { Id = 2, Number = "102", Type = RoomType.Double, Floor = 1, PricePerNight = 250, Status = RoomStatus.Occupied };
        var r201 = new Room { Id = 3, Number = "201", Type = RoomType.Double, Floor = 2, PricePerNight = 280, Status = RoomStatus.Available };
        var r301 = new Room { Id = 4, Number = "301", Type = RoomType.Suite, Floor = 3, PricePerNight = 500, Status = RoomStatus.NeedsCleaning };
        var r202 = new Room { Id = 5, Number = "202", Type = RoomType.Single, Floor = 2, PricePerNight = 170, Status = RoomStatus.Available };

        RoomsVm.Rooms.Add(r101);
        RoomsVm.Rooms.Add(r102);
        RoomsVm.Rooms.Add(r201);
        RoomsVm.Rooms.Add(r301);
        RoomsVm.Rooms.Add(r202);

        var g1 = new Guest { Id = 1, FirstName = "Jan", LastName = "Kowalski", Phone = "+48 600 100 200", Email = "jan.kowalski@email.com", DocumentNumber = "ABC123456" };
        var g2 = new Guest { Id = 2, FirstName = "Anna", LastName = "Nowak", Phone = "+48 601 200 300", Email = "anna.nowak@email.com", DocumentNumber = "DEF789012" };
        var g3 = new Guest { Id = 3, FirstName = "Piotr", LastName = "Wisniewski", Phone = "+48 602 300 400", Email = "piotr.w@email.com", DocumentNumber = "GHI345678" };

        GuestsVm.Guests.Add(g1);
        GuestsVm.Guests.Add(g2);
        GuestsVm.Guests.Add(g3);

        ReservationsVm.Reservations.Add(new Reservation
        {
            Id = 1, Room = r102, Guest = g1,
            CheckInDate = DateTimeOffset.Now.Date.AddDays(-2), CheckOutDate = DateTimeOffset.Now.Date.AddDays(3),
            Status = ReservationStatus.Confirmed, Notes = "Late check-in requested"
        });
        ReservationsVm.Reservations.Add(new Reservation
        {
            Id = 2, Room = r201, Guest = g2,
            CheckInDate = DateTimeOffset.Now.Date.AddDays(5), CheckOutDate = DateTimeOffset.Now.Date.AddDays(10),
            Status = ReservationStatus.Confirmed, Notes = "Extra pillows"
        });
        ReservationsVm.Reservations.Add(new Reservation
        {
            Id = 3, Room = r301, Guest = g3,
            CheckInDate = DateTimeOffset.Now.Date.AddDays(-7), CheckOutDate = DateTimeOffset.Now.Date.AddDays(-1),
            Status = ReservationStatus.Completed, Notes = string.Empty
        });
    }
}

public enum AppPage { Dashboard, Rooms, Guests, Reservations, Calendar }
