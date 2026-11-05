using System.Diagnostics;
using Avalonia.Threading;
using NextAuth.Core.Services;

namespace NextAuth.Desktop.Services;

public class AvaloniaAppService(Func<Avalonia.Controls.Window?> mainWindowAccessor) : IAppService
{
    public void OpenFolder(string path)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{path}\"",
                    UseShellExecute = true
                });
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"\"{path}\"",
                    UseShellExecute = false
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = $"\"{path}\"",
                    UseShellExecute = false
                });
            }
        }
        catch
        {
        }
    }

    public void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
        }
    }

    public void Shutdown()
    {
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        lifetime?.Shutdown();
    }

    public bool IsMainWindowMinimized()
    {
        var window = mainWindowAccessor();
        return window is { WindowState: Avalonia.Controls.WindowState.Minimized };
    }

    public void PostToUI(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess()) action();
        else Dispatcher.UIThread.Post(action);
    }
}