using NextAuth.Desktop.Services;
using Avalonia.Controls;
using DialogHostAvalonia;

namespace NextAuth.Desktop.Views.Dialogs;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        CloseButton.Click += (_, _) => DialogHost.Close(AvaloniaDialogService.MAIN_HOST_ID);
    }
}
