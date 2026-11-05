using NextAuth.Core.Services;

namespace NextAuth.Core;

public static class SnackbarController
{
    public static void SendSnackbar(string text, TimeSpan? duration = null)
    {
        AppServices.Snackbar?.SendSnackbar(text, duration);
    }

    public static void SendSnackbar(string text, SnackbarType type, TimeSpan? duration = null)
    {
        AppServices.Snackbar?.SendSnackbar(text, type, duration);
    }

    public static void SendSuccess(string text, TimeSpan? duration = null)
    {
        AppServices.Snackbar?.SendSnackbar(text, SnackbarType.Success, duration);
    }

    public static void SendWarning(string text, TimeSpan? duration = null)
    {
        AppServices.Snackbar?.SendSnackbar(text, SnackbarType.Warning, duration);
    }

    public static void SendError(string text, TimeSpan? duration = null)
    {
        AppServices.Snackbar?.SendSnackbar(text, SnackbarType.Error, duration);
    }

    public static void SendSnackbarWithButton(string text, string actionText = "OK", Action? action = null,
        TimeSpan? duration = null)
    {
        AppServices.Snackbar?.SendSnackbarWithButton(text, actionText, action, duration);
    }

    public static void SendSnackbarWithButton(string text, SnackbarType type, string actionText = "OK", Action? action = null,
        TimeSpan? duration = null)
    {
        AppServices.Snackbar?.SendSnackbarWithButton(text, type, actionText, action, duration);
    }
}
