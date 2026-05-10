using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF.Models;

public partial class Guest : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    private string _firstName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    private string _lastName = string.Empty;

    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _documentNumber = string.Empty;

    [JsonIgnore]
    public string FullName => $"{FirstName} {LastName}";

    public override string ToString() => FullName;
}
