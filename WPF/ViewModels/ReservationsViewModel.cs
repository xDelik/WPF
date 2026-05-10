using System;
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
    private Reservation? _selectedReservation;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddReservationCommand))]
    private Room? _reservationRoom;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddReservationCommand))]
    private Guest? _reservationGuest;

    [ObservableProperty] private DateTimeOffset _reservationCheckIn = DateTimeOffset.Now.Date;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(ReservationsViewModel), nameof(ValidateCheckOut))]
    private DateTimeOffset _reservationCheckOut = DateTimeOffset.Now.Date.AddDays(1);

    [ObservableProperty] private ReservationStatus _reservationStatus;
    [ObservableProperty] private string _reservationNotes = string.Empty;

    public static ReservationStatus[] ReservationStatuses => Enum.GetValues<ReservationStatus>();

    public ReservationsViewModel() : this([], [], [], () => { }) { }

    public ReservationsViewModel(
        ObservableCollection<Room> rooms,
        ObservableCollection<Guest> guests,
        ObservableCollection<Reservation> reservations,
        Action save) : base(save)
    {
        Rooms = rooms;
        Guests = guests;
        Reservations = reservations;
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

        Reservations.Remove(SelectedReservation);
        Save();
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
