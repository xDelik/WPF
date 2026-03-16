using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPF.Models;

namespace WPF.ViewModels;

public partial class GuestsViewModel : ObservableObject
{
    public ObservableCollection<Guest> Guests { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveGuestCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteGuestCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddGuestCommand))]
    private Guest? _selectedGuest;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddGuestCommand))]
    private string _guestFirstName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddGuestCommand))]
    private string _guestLastName = string.Empty;

    [ObservableProperty] private string _guestPhone = string.Empty;
    [ObservableProperty] private string _guestEmail = string.Empty;
    [ObservableProperty] private string _guestDocumentNumber = string.Empty;

    private int _nextId = 1;

    [RelayCommand(CanExecute = nameof(CanAddGuest))]
    private void AddGuest()
    {
        var guest = new Guest
        {
            Id = _nextId++,
            FirstName = GuestFirstName,
            LastName = GuestLastName,
            Phone = GuestPhone,
            Email = GuestEmail,
            DocumentNumber = GuestDocumentNumber
        };
        Guests.Add(guest);
        ClearForm();
    }

    [RelayCommand(CanExecute = nameof(CanModifyGuest))]
    private void DeleteGuest()
    {
        if (SelectedGuest is not null)
            Guests.Remove(SelectedGuest);
    }

    [RelayCommand(CanExecute = nameof(CanModifyGuest))]
    private void SaveGuest()
    {
        if (SelectedGuest is null) return;
        SelectedGuest.FirstName = GuestFirstName;
        SelectedGuest.LastName = GuestLastName;
        SelectedGuest.Phone = GuestPhone;
        SelectedGuest.Email = GuestEmail;
        SelectedGuest.DocumentNumber = GuestDocumentNumber;
        ClearForm();
    }

    [RelayCommand]
    private void Clear() => ClearForm();

    private bool CanAddGuest() => !string.IsNullOrWhiteSpace(GuestFirstName)
                                  && !string.IsNullOrWhiteSpace(GuestLastName)
                                  && SelectedGuest is null;
    private bool CanModifyGuest() => SelectedGuest is not null;

    partial void OnSelectedGuestChanged(Guest? value)
    {
        if (value is null) return;
        GuestFirstName = value.FirstName;
        GuestLastName = value.LastName;
        GuestPhone = value.Phone;
        GuestEmail = value.Email;
        GuestDocumentNumber = value.DocumentNumber;
    }

    private void ClearForm()
    {
        SelectedGuest = null;
        GuestFirstName = string.Empty;
        GuestLastName = string.Empty;
        GuestPhone = string.Empty;
        GuestEmail = string.Empty;
        GuestDocumentNumber = string.Empty;
    }
}
