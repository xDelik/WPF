using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF.ViewModels;

public abstract partial class FormViewModel : ObservableValidator
{
    private readonly Action _save;

    protected FormViewModel(Action save) { _save = save; }

    [ObservableProperty] private string? _errorMessage;

    protected void Save() => _save();
    protected bool Fail(string message) { ErrorMessage = message; return false; }
}
