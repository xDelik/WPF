using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF.ViewModels;

public abstract partial class FormViewModel : ObservableValidator
{
    [ObservableProperty] private string? _errorMessage;

    protected bool Fail(string message) { ErrorMessage = message; return false; }
}
