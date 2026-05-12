using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPF.Models;
using WPF.Services;

namespace WPF.ViewModels;

public enum WizardStep { Dates, Room, GuestAndReview }

public record RoomOption(Room Room, bool IsAvailable);

public partial class ReservationWizardViewModel : FormViewModel
{
    private readonly ObservableCollection<Room> _rooms;
    private readonly ObservableCollection<Guest> _guests;
    private readonly ObservableCollection<Reservation> _reservations;
    private readonly Action<Reservation> _onCreate;
    private readonly Action _onCancel;
    private readonly Action _gotoGuestsPage;

    public ReservationWizardViewModel(
        ObservableCollection<Room> rooms,
        ObservableCollection<Guest> guests,
        ObservableCollection<Reservation> reservations,
        Action<Reservation> onCreate,
        Action onCancel,
        Action gotoGuestsPage,
        Action save,
        INotificationService notifier) : base(save, notifier)
    {
        _rooms = rooms;
        _guests = guests;
        _reservations = reservations;
        _onCreate = onCreate;
        _onCancel = onCancel;
        _gotoGuestsPage = gotoGuestsPage;
    }

    public ReservationWizardViewModel() : this(
        new(), new(), new(),
        _ => { }, () => { }, () => { },
        () => { }, new NullNotificationService()) { }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDates))]
    [NotifyPropertyChangedFor(nameof(IsRoom))]
    [NotifyPropertyChangedFor(nameof(IsGuestAndReview))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    [NotifyCanExecuteChangedFor(nameof(FinishCommand))]
    private WizardStep _step = WizardStep.Dates;

    public bool IsDates => Step == WizardStep.Dates;
    public bool IsRoom => Step == WizardStep.Room;
    public bool IsGuestAndReview => Step == WizardStep.GuestAndReview;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Nights))]
    [NotifyPropertyChangedFor(nameof(Total))]
    [NotifyPropertyChangedFor(nameof(AvailableRooms))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private DateTimeOffset _checkIn = DateTimeOffset.Now.Date;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Nights))]
    [NotifyPropertyChangedFor(nameof(Total))]
    [NotifyPropertyChangedFor(nameof(AvailableRooms))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private DateTimeOffset _checkOut = DateTimeOffset.Now.Date.AddDays(1);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedRoom))]
    private RoomOption? _selectedRoomOption;

    partial void OnSelectedRoomOptionChanged(RoomOption? value)
    {
        SelectedRoom = value?.Room;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private Room? _selectedRoom;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FinishCommand))]
    private Guest? _selectedGuest;

    [ObservableProperty] private string _notes = string.Empty;

    public int Nights => Math.Max(0, (CheckOut - CheckIn).Days);
    public decimal Total => SelectedRoom is null ? 0m : Nights * SelectedRoom.PricePerNight;

    public IEnumerable<RoomOption> AvailableRooms =>
        _rooms.Select(r => new RoomOption(
            r,
            !HotelRules.HasOverlap(_reservations, r, CheckIn, CheckOut)));

    public ObservableCollection<Guest> Guests => _guests;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next()
    {
        if (Step == WizardStep.Dates) Step = WizardStep.Room;
        else if (Step == WizardStep.Room) Step = WizardStep.GuestAndReview;
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back()
    {
        if (Step == WizardStep.Room) Step = WizardStep.Dates;
        else if (Step == WizardStep.GuestAndReview) Step = WizardStep.Room;
    }

    [RelayCommand(CanExecute = nameof(CanFinish))]
    private void Finish()
    {
        if (SelectedRoom is null || SelectedGuest is null) return;
        var r = new Reservation
        {
            Room = SelectedRoom,
            Guest = SelectedGuest,
            CheckInDate = CheckIn,
            CheckOutDate = CheckOut,
            Status = ReservationStatus.Confirmed,
            Notes = Notes
        };
        _onCreate(r);
    }

    [RelayCommand]
    private void Cancel() => _onCancel();

    [RelayCommand]
    private void AddNewGuest() => _gotoGuestsPage();

    private bool CanGoNext() => Step switch
    {
        WizardStep.Dates => CheckOut > CheckIn,
        WizardStep.Room  => SelectedRoom is not null,
        _ => false
    };

    private bool CanGoBack() => Step != WizardStep.Dates;
    private bool CanFinish() => Step == WizardStep.GuestAndReview && SelectedGuest is not null;
}
