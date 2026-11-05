using NextAuth.Core.Services;
using NextAuth.Desktop.Controls;

namespace NextAuth.Desktop.Services;

public class AvaloniaSnackbarService(SnackbarView snackbarView) : ISnackbarService
{
    public void SendSnackbar(string text, TimeSpan? duration = null)
    {
        snackbarView.Show(text, SnackbarType.Info, duration: duration);
    }

    public void SendSnackbar(string text, SnackbarType type, TimeSpan? duration = null)
    {
        snackbarView.Show(text, type, duration: duration);
    }

    public void SendSnackbarWithButton(string text, string actionText = "OK", Action? action = null,
        TimeSpan? duration = null)
    {
        snackbarView.Show(text, SnackbarType.Info, actionText, action, duration);
    }

    public void SendSnackbarWithButton(string text, SnackbarType type, string actionText = "OK", Action? action = null,
        TimeSpan? duration = null)
    {
        snackbarView.Show(text, type, actionText, action, duration);
    }
}
