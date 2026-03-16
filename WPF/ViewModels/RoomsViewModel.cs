using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPF.Models;

namespace WPF.ViewModels;

public partial class RoomsViewModel : ObservableObject
{
    public ObservableCollection<Room> Rooms { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteRoomCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddRoomCommand))]
    private Room? _selectedRoom;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRoomCommand))]
    private string _roomNumber = string.Empty;

    [ObservableProperty] private RoomType _roomType;
    [ObservableProperty] private int _roomFloor;
    [ObservableProperty] private decimal _roomPrice;
    [ObservableProperty] private RoomStatus _roomStatus;

    public static RoomType[] RoomTypes => Enum.GetValues<RoomType>();
    public static RoomStatus[] RoomStatuses => Enum.GetValues<RoomStatus>();

    private int _nextId = 1;

    [RelayCommand(CanExecute = nameof(CanAddRoom))]
    private void AddRoom()
    {
        var room = new Room
        {
            Id = _nextId++,
            Number = RoomNumber,
            Type = RoomType,
            Floor = RoomFloor,
            PricePerNight = RoomPrice,
            Status = RoomStatus
        };
        Rooms.Add(room);
        ClearForm();
    }

    [RelayCommand(CanExecute = nameof(CanModifyRoom))]
    private void DeleteRoom()
    {
        if (SelectedRoom is not null)
            Rooms.Remove(SelectedRoom);
    }

    [RelayCommand(CanExecute = nameof(CanModifyRoom))]
    private void SaveRoom()
    {
        if (SelectedRoom is null) return;
        SelectedRoom.Number = RoomNumber;
        SelectedRoom.Type = RoomType;
        SelectedRoom.Floor = RoomFloor;
        SelectedRoom.PricePerNight = RoomPrice;
        SelectedRoom.Status = RoomStatus;
        ClearForm();
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
    }
}
