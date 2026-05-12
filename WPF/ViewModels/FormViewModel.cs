using System;
using CommunityToolkit.Mvvm.ComponentModel;
using WPF.Services;

namespace WPF.ViewModels;

public abstract partial class FormViewModel : ObservableValidator
{
    private readonly Action _save;
    private readonly INotificationService _notifier;

    protected FormViewModel(Action save, INotificationService notifier)
    {
        _save = save;
        _notifier = notifier;
    }

    [ObservableProperty] private string? _errorMessage;

    protected void Save() => _save();
    protected void Notify(string message) => _notifier.Push(message);
    protected bool Fail(string message) { ErrorMessage = message; return false; }
    protected INotificationService GetNotifier() => _notifier;
}
