using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF.Models;

public partial class Room : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _number = string.Empty;
    [ObservableProperty] private RoomType _type;
    [ObservableProperty] private int _floor;
    [ObservableProperty] private decimal _pricePerNight;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NeedsCleaning))]
    private RoomStatus _status;

    public bool NeedsCleaning => Status == RoomStatus.NeedsCleaning;

    public override string ToString() => $"{Number} ({Type})";
}
