namespace NextAuth.Core.Services;

public enum SnackbarType
{
    Info,
    Success,
    Warning,
    Error
}

public interface ISnackbarService
{
    void SendSnackbar(string text, TimeSpan? duration = null);
    void SendSnackbar(string text, SnackbarType type, TimeSpan? duration = null);
    void SendSnackbarWithButton(string text, string actionText = "OK", Action? action = null, TimeSpan? duration = null);
    void SendSnackbarWithButton(string text, SnackbarType type, string actionText = "OK", Action? action = null, TimeSpan? duration = null);
}
