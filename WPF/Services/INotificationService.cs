using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;

namespace WPF.Services;

public enum NotificationKind { Success, Warning }

public interface INotificationService
{
    ObservableCollection<Toast> Toasts { get; }
    void Push(string message, NotificationKind kind = NotificationKind.Success);
}

public class Toast
{
    public string Message { get; init; } = string.Empty;
    public NotificationKind Kind { get; init; }
}

public class NotificationService : INotificationService
{
    public ObservableCollection<Toast> Toasts { get; } = new();

    public void Push(string message, NotificationKind kind = NotificationKind.Success)
    {
        var t = new Toast { Message = message, Kind = kind };
        Toasts.Add(t);
        DispatcherTimer.RunOnce(() => Toasts.Remove(t), TimeSpan.FromSeconds(3));
    }
}

public class NullNotificationService : INotificationService
{
    public ObservableCollection<Toast> Toasts { get; } = new();
    public void Push(string message, NotificationKind kind = NotificationKind.Success) { }
}
