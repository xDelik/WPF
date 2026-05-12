using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using WPF.Models;
using WPF.Services;

namespace WPF.ViewModels;

public partial class ReservationsViewModel : FormViewModel
{
    public ObservableCollection<Reservation> Reservations { get; }

    public ObservableCollection<Room> Rooms { get; }
    public ObservableCollection<Guest> Guests { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveReservationCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteReservationCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddReservationCommand))]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    private Reservation? _selectedReservation;

    public bool IsEditMode => IsDrawerOpen && !IsWizardMode && SelectedReservation is not null;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddReservationCommand))]
    [NotifyPropertyChangedFor(nameof(FormTotal))]
    private Room? _reservationRoom;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddReservationCommand))]
    private Guest? _reservationGuest;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormNights))]
    [NotifyPropertyChangedFor(nameof(FormTotal))]
    private DateTimeOffset _reservationCheckIn = DateTimeOffset.Now.Date;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(ReservationsViewModel), nameof(ValidateCheckOut))]
    [NotifyPropertyChangedFor(nameof(FormNights))]
    [NotifyPropertyChangedFor(nameof(FormTotal))]
    private DateTimeOffset _reservationCheckOut = DateTimeOffset.Now.Date.AddDays(1);

    public int FormNights => Math.Max(0, (ReservationCheckOut - ReservationCheckIn).Days);
    public decimal FormTotal => ReservationRoom is null ? 0m : FormNights * ReservationRoom.PricePerNight;

    [ObservableProperty] private ReservationStatus _reservationStatus;
    [ObservableProperty] private string _reservationNotes = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    private bool _isDrawerOpen;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private ReservationStatus? _statusFilter;

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredReservations));
        OnPropertyChanged(nameof(IsListEmpty));
    }

    partial void OnStatusFilterChanged(ReservationStatus? value)
    {
        OnPropertyChanged(nameof(FilteredReservations));
        OnPropertyChanged(nameof(IsListEmpty));
    }

    public IEnumerable<Reservation> FilteredReservations
    {
        get
        {
            IEnumerable<Reservation> q = Reservations;
            if (!string.IsNullOrWhiteSpace(SearchText))
                q = q.Where(r => r.Guest.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                                 || r.Room.Number.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            if (StatusFilter is { } s)
                q = q.Where(r => r.Status == s);
            return q;
        }
    }

    public bool IsListEmpty => !FilteredReservations.Any();

    public static ReservationStatus[] ReservationStatuses => Enum.GetValues<ReservationStatus>();

    private readonly Action _gotoGuestsPage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWizardMode))]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    private ReservationWizardViewModel? _wizard;

    public bool IsWizardMode => Wizard is not null;

    public ReservationsViewModel() : this([], [], [], () => { }, new NullNotificationService(), () => { }) { }

    public ReservationsViewModel(
        ObservableCollection<Room> rooms,
        ObservableCollection<Guest> guests,
        ObservableCollection<Reservation> reservations,
        Action save,
        INotificationService notifier,
        Action gotoGuestsPage) : base(save, notifier)
    {
        Rooms = rooms;
        Guests = guests;
        Reservations = reservations;
        _gotoGuestsPage = gotoGuestsPage;
        Reservations.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(FilteredReservations));
            OnPropertyChanged(nameof(IsListEmpty));
        };
    }

    [RelayCommand]
    private void OpenAddDrawer()
    {
        SelectedReservation = null;
        ClearForm();
        Wizard = new ReservationWizardViewModel(
            Rooms,
            Guests,
            Reservations,
            AddFromWizard,
            () => { Wizard = null; IsDrawerOpen = false; },
            _gotoGuestsPage,
            () => Save(),
            GetNotifier());
        IsDrawerOpen = true;
    }

    [RelayCommand]
    private void CloseDrawer()
    {
        Wizard = null;
        IsDrawerOpen = false;
        ClearForm();
    }

    public void AddFromWizard(Reservation r)
    {
        Reservations.Add(r);
        Save();
        Notify($"Reservation for {r.Guest.FullName} created.");
        Wizard = null;
        IsDrawerOpen = false;
    }

    public void OpenWizardFor(Room room, DateTimeOffset checkIn)
    {
        SelectedReservation = null;
        var wizard = new ReservationWizardViewModel(
            Rooms, Guests, Reservations,
            AddFromWizard,
            () => { Wizard = null; IsDrawerOpen = false; },
            _gotoGuestsPage,
            () => Save(),
            GetNotifier());
        wizard.CheckIn = checkIn;
        wizard.CheckOut = checkIn.AddDays(1);
        wizard.SelectedRoomOption = wizard.AvailableRooms
            .FirstOrDefault(o => o.Room.Id == room.Id);
        wizard.Step = WizardStep.GuestAndReview;
        Wizard = wizard;
        IsDrawerOpen = true;
    }

    [RelayCommand(CanExecute = nameof(CanAddReservation))]
    private void AddReservation() => TryAddReservation();

    private bool TryAddReservation()
    {
        if (ReservationRoom is null || ReservationGuest is null) return false;
        ErrorMessage = null;
        ValidateAllProperties();
        if (HasErrors) return Fail("Please correct the highlighted fields.");

        Reservations.Add(new Reservation
        {
            Id = NextId(),
            Room = ReservationRoom,
            Guest = ReservationGuest,
            CheckInDate = ReservationCheckIn,
            CheckOutDate = ReservationCheckOut,
            Status = ReservationStatus,
            Notes = ReservationNotes
        });
        Save();
        Notify($"Reservation for {ReservationGuest!.FullName} added.");
        IsDrawerOpen = false;
        ClearForm();
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanModifyReservation))]
    private void SaveReservation() => TrySaveReservation();

    private bool TrySaveReservation()
    {
        if (SelectedReservation is null) return false;
        ErrorMessage = null;
        ValidateAllProperties();
        if (HasErrors) return Fail("Please correct the highlighted fields.");

        if (ReservationRoom is not null) SelectedReservation.Room = ReservationRoom;
        if (ReservationGuest is not null) SelectedReservation.Guest = ReservationGuest;
        SelectedReservation.CheckInDate = ReservationCheckIn;
        SelectedReservation.CheckOutDate = ReservationCheckOut;
        SelectedReservation.Status = ReservationStatus;
        SelectedReservation.Notes = ReservationNotes;
        Save();
        Notify($"Reservation for {SelectedReservation.Guest.FullName} updated.");
        IsDrawerOpen = false;
        ClearForm();
        return true;
    }

    public static ValidationResult? ValidateCheckOut(DateTimeOffset checkOut, ValidationContext context)
    {
        var vm = (ReservationsViewModel)context.ObjectInstance!;

        if (checkOut <= vm.ReservationCheckIn)
            return new ValidationResult("Check-out must be after check-in.");

        var room = vm.ReservationRoom ?? vm.SelectedReservation?.Room;
        if (room is null) return ValidationResult.Success;

        if (HotelRules.HasOverlap(vm.Reservations, room,
                vm.ReservationCheckIn, checkOut,
                excludingId: vm.SelectedReservation?.Id))
            return new ValidationResult("This room is already booked for the selected dates.");

        return ValidationResult.Success;
    }

    partial void OnReservationRoomChanged(Room? value)
        => ValidateProperty(ReservationCheckOut, nameof(ReservationCheckOut));

    partial void OnReservationCheckInChanged(DateTimeOffset value)
        => ValidateProperty(ReservationCheckOut, nameof(ReservationCheckOut));

    [RelayCommand(CanExecute = nameof(CanModifyReservation))]
    private Task DeleteReservation() => TryDeleteReservationAsync();

    private async Task<bool> TryDeleteReservationAsync()
    {
        if (SelectedReservation is null) return false;
        ErrorMessage = null;

        var box = MessageBoxManager.GetMessageBoxStandard(
            "Confirm delete",
            $"Delete reservation for '{SelectedReservation.Guest.FullName}' in room {SelectedReservation.Room.Number}?",
            ButtonEnum.YesNo,
            Icon.Warning);
        if (await box.ShowAsync() != ButtonResult.Yes) return false;

        var display = $"{SelectedReservation.Guest.FullName} · {SelectedReservation.Room.Number}";
        Reservations.Remove(SelectedReservation);
        Save();
        Notify($"Reservation {display} deleted.");
        IsDrawerOpen = false;
        ClearForm();
        return true;
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
        IsDrawerOpen = true;
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
        ClearErrors();
        ErrorMessage = null;
    }

    private int NextId() => Reservations.Count == 0 ? 1 : Reservations.Max(r => r.Id) + 1;
}
