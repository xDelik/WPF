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

public partial class GuestsViewModel : FormViewModel
{
    private readonly IReadOnlyCollection<Reservation> _reservations;

    public ObservableCollection<Guest> Guests { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveGuestCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteGuestCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddGuestCommand))]
    [NotifyPropertyChangedFor(nameof(IsAddMode))]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    private Guest? _selectedGuest;

    public bool IsAddMode => IsDrawerOpen && SelectedGuest is null;
    public bool IsEditMode => IsDrawerOpen && SelectedGuest is not null;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(AddGuestCommand))]
    [Required(ErrorMessage = "First name is required")]
    private string _guestFirstName = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(AddGuestCommand))]
    [Required(ErrorMessage = "Last name is required")]
    private string _guestLastName = string.Empty;

    [ObservableProperty] private string _guestPhone = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    private string _guestEmail = string.Empty;

    [ObservableProperty] private string _guestDocumentNumber = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAddMode))]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    private bool _isDrawerOpen;
    [ObservableProperty] private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredGuests));
        OnPropertyChanged(nameof(IsListEmpty));
    }

    public IEnumerable<Guest> FilteredGuests
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return Guests;
            var s = SearchText;
            return Guests.Where(g =>
                (g.FirstName + " " + g.LastName).Contains(s, StringComparison.OrdinalIgnoreCase) ||
                g.Email.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                g.DocumentNumber.Contains(s, StringComparison.OrdinalIgnoreCase));
        }
    }

    public bool IsListEmpty => !FilteredGuests.Any();

    public GuestsViewModel() : this([], () => { }, new NullNotificationService()) { }

    public GuestsViewModel(IReadOnlyCollection<Reservation> reservations, Action save, INotificationService notifier)
        : base(save, notifier)
    {
        _reservations = reservations;
        Guests.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(FilteredGuests));
            OnPropertyChanged(nameof(IsListEmpty));
        };
    }

    [RelayCommand]
    private void OpenAddDrawer()
    {
        SelectedGuest = null;
        ClearForm();
        IsDrawerOpen = true;
    }

    [RelayCommand]
    private void CloseDrawer()
    {
        IsDrawerOpen = false;
        ClearForm();
    }

    [RelayCommand(CanExecute = nameof(CanAddGuest))]
    private void AddGuest() => TryAddGuest();

    private bool TryAddGuest()
    {
        ErrorMessage = null;
        ValidateAllProperties();
        if (HasErrors) return Fail("Please correct the highlighted fields.");

        Guests.Add(new Guest
        {
            Id = NextId(),
            FirstName = GuestFirstName,
            LastName = GuestLastName,
            Phone = GuestPhone,
            Email = GuestEmail,
            DocumentNumber = GuestDocumentNumber
        });
        Save();
        Notify($"Guest {GuestFirstName} {GuestLastName} added.");
        IsDrawerOpen = false;
        ClearForm();
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanModifyGuest))]
    private void SaveGuest() => TrySaveGuest();

    private bool TrySaveGuest()
    {
        if (SelectedGuest is null) return false;
        ErrorMessage = null;
        ValidateAllProperties();
        if (HasErrors) return Fail("Please correct the highlighted fields.");

        SelectedGuest.FirstName = GuestFirstName;
        SelectedGuest.LastName = GuestLastName;
        SelectedGuest.Phone = GuestPhone;
        SelectedGuest.Email = GuestEmail;
        SelectedGuest.DocumentNumber = GuestDocumentNumber;
        Save();
        Notify($"Guest {GuestFirstName} {GuestLastName} updated.");
        IsDrawerOpen = false;
        ClearForm();
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanModifyGuest))]
    private Task DeleteGuest() => TryDeleteGuestAsync();

    private async Task<bool> TryDeleteGuestAsync()
    {
        if (SelectedGuest is null) return false;
        ErrorMessage = null;

        if (HotelRules.IsGuestInUse(_reservations, SelectedGuest))
            return Fail("Cannot delete a guest with active or completed reservations.");

        var box = MessageBoxManager.GetMessageBoxStandard(
            "Confirm delete",
            $"Delete guest '{SelectedGuest.FullName}'?",
            ButtonEnum.YesNo,
            Icon.Warning);
        if (await box.ShowAsync() != ButtonResult.Yes) return false;

        var name = SelectedGuest.FullName;
        Guests.Remove(SelectedGuest);
        Save();
        Notify($"Guest {name} deleted.");
        IsDrawerOpen = false;
        ClearForm();
        return true;
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
        IsDrawerOpen = true;
    }

    private void ClearForm()
    {
        SelectedGuest = null;
        GuestFirstName = string.Empty;
        GuestLastName = string.Empty;
        GuestPhone = string.Empty;
        GuestEmail = string.Empty;
        GuestDocumentNumber = string.Empty;
        ClearErrors();
        ErrorMessage = null;
    }

    private int NextId() => Guests.Count == 0 ? 1 : Guests.Max(g => g.Id) + 1;
}
