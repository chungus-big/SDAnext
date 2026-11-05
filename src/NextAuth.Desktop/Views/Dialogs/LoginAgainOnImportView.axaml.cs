using NextAuth.Desktop.Services;
using Avalonia.Controls;
using DialogHostAvalonia;

namespace NextAuth.Desktop.Views.Dialogs;

public partial class LoginAgainOnImportView : UserControl
{
    public LoginAgainOnImportView()
    {
        InitializeComponent();
        OkButton.Click += (_, _) => DialogHost.Close(AvaloniaDialogService.MAIN_HOST_ID, true);
        CancelButton.Click += (_, _) => DialogHost.Close(AvaloniaDialogService.MAIN_HOST_ID, false);
    }
}
