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

public partial class RoomsViewModel : FormViewModel
{
    private readonly IReadOnlyCollection<Reservation> _reservations;

    public ObservableCollection<Room> Rooms { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddRoomCommand))]
    private Room? _selectedRoom;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(AddRoomCommand))]
    [Required(ErrorMessage = "Room number is required")]
    [CustomValidation(typeof(RoomsViewModel), nameof(ValidateUniqueNumber))]
    private string _roomNumber = string.Empty;

    [ObservableProperty] private RoomType _roomType;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, 100, ErrorMessage = "Floor must be between 0 and 100")]
    private int _roomFloor;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Price must be greater than 0")]
    private decimal _roomPrice;

    [ObservableProperty] private RoomStatus _roomStatus;

    public static RoomType[] RoomTypes => Enum.GetValues<RoomType>();
    public static RoomStatus[] RoomStatuses => Enum.GetValues<RoomStatus>();

    public RoomsViewModel() : this([], () => { }) { }

    public RoomsViewModel(IReadOnlyCollection<Reservation> reservations, Action save) : base(save)
    {
        _reservations = reservations;
    }

    [RelayCommand(CanExecute = nameof(CanAddRoom))]
    private void AddRoom() => TryAddRoom();

    private bool TryAddRoom()
    {
        ErrorMessage = null;
        ValidateAllProperties();
        if (HasErrors) return Fail("Please correct the highlighted fields.");

        Rooms.Add(new Room
        {
            Id = NextId(),
            Number = RoomNumber,
            Type = RoomType,
            Floor = RoomFloor,
            PricePerNight = RoomPrice,
            Status = RoomStatus
        });
        Save();
        ClearForm();
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanModifyRoom))]
    private void SaveRoom() => TrySaveRoom();

    private bool TrySaveRoom()
    {
        if (SelectedRoom is null) return false;
        ErrorMessage = null;
        ValidateAllProperties();
        if (HasErrors) return Fail("Please correct the highlighted fields.");

        SelectedRoom.Number = RoomNumber;
        SelectedRoom.Type = RoomType;
        SelectedRoom.Floor = RoomFloor;
        SelectedRoom.PricePerNight = RoomPrice;
        SelectedRoom.Status = RoomStatus;
        Save();
        ClearForm();
        return true;
    }

    public static ValidationResult? ValidateUniqueNumber(string? number, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(number)) return ValidationResult.Success;

        var vm = (RoomsViewModel)context.ObjectInstance!;
        return HotelRules.IsRoomNumberUnique(vm.Rooms, number, excludingId: vm.SelectedRoom?.Id)
            ? ValidationResult.Success
            : new ValidationResult($"Room number '{number}' already exists.");
    }

    [RelayCommand(CanExecute = nameof(CanModifyRoom))]
    private Task DeleteRoom() => TryDeleteRoomAsync();

    private async Task<bool> TryDeleteRoomAsync()
    {
        if (SelectedRoom is null) return false;
        ErrorMessage = null;

        if (HotelRules.IsRoomInUse(_reservations, SelectedRoom))
            return Fail("Cannot delete a room that is referenced by active or completed reservations.");

        var box = MessageBoxManager.GetMessageBoxStandard(
            "Confirm delete",
            $"Delete room '{SelectedRoom.Number}'?",
            ButtonEnum.YesNo,
            Icon.Warning);
        if (await box.ShowAsync() != ButtonResult.Yes) return false;

        Rooms.Remove(SelectedRoom);
        Save();
        ClearForm();
        return true;
    }

    [RelayCommand]
    private void Clear() => ClearForm();

    private bool CanAddRoom() => !string.IsNullOrWhiteSpace(RoomNumber) && SelectedRoom is null;
    private bool CanModifyRoom() => SelectedRoom is not null;

    partial void OnSelectedRoomChanged(Room? value)
    {
        if (value is null) return;
        RoomNumber = value.Number;
        RoomType = value.Type;
        RoomFloor = value.Floor;
        RoomPrice = value.PricePerNight;
        RoomStatus = value.Status;
    }

    private void ClearForm()
    {
        SelectedRoom = null;
        RoomNumber = string.Empty;
        RoomType = RoomType.Single;
        RoomFloor = 0;
        RoomPrice = 0;
        RoomStatus = RoomStatus.Available;
        ClearErrors();
        ErrorMessage = null;
    }

    private int NextId() => Rooms.Count == 0 ? 1 : Rooms.Max(r => r.Id) + 1;
}
