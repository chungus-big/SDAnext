using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NextAuth.Desktop.Views;
using NextAuth.Core.Model;
using Avalonia.Controls;
using Avalonia.Input;

namespace NextAuth.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        InputElement.PointerEnteredEvent.AddClassHandler<ComboBoxItem>((item, _) =>
        {
            if (item.IsEnabled && item.IsVisible)
            {
                item.Focus();
            }
        });
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            mainWindow.InitializeServices();
            desktop.MainWindow = mainWindow;

            await Shell.Initialize();

            mainWindow.BindViewModel();
            mainWindow.Show();
        }
    }
}