using System.Collections.Concurrent;

namespace StudentCourseEnrollment.Frontend.Services;

public class ToastService
{
    private readonly ConcurrentQueue<ToastMessage> _messages = new();

    public event Action? OnToastsUpdated;

    public IReadOnlyCollection<ToastMessage> Messages => _messages.ToList().AsReadOnly();

    public void ShowSuccess(string message) => Enqueue(message, ToastLevel.Success);

    public void ShowError(string message) => Enqueue(message, ToastLevel.Error);

    public void ShowInfo(string message) => Enqueue(message, ToastLevel.Info);

    public void ShowWarning(string message) => Enqueue(message, ToastLevel.Warning);

    private void Enqueue(string message, ToastLevel level)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var toast = new ToastMessage(Guid.NewGuid(), level, message);
        _messages.Enqueue(toast);
        OnToastsUpdated?.Invoke();

        if (level != ToastLevel.Error)
        {
            var toastId = toast.Id;
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(4));
                if (_messages.Any(m => m.Id == toastId))
                {
                    Remove(toastId);
                }
            });
        }
    }

    public void Remove(Guid id)
    {
        var remaining = _messages.Where(m => m.Id != id).ToList();

        while (_messages.TryDequeue(out _))
        {
        }

        foreach (var message in remaining)
        {
            _messages.Enqueue(message);
        }

        OnToastsUpdated?.Invoke();
    }
}

public enum ToastLevel
{
    Success,
    Error,
    Info,
    Warning
}

public sealed record ToastMessage(Guid Id, ToastLevel Level, string Message);

