using Avalonia.Controls;
using DialogHostAvalonia;
using NextAuth.Desktop.Services;

namespace NextAuth.Desktop.Views.Dialogs;

public partial class LinkerView : UserControl
{
    public LinkerView()
    {
        InitializeComponent();
        CloseButton.Click += (_, _) => DialogHost.Close(AvaloniaDialogService.MAIN_HOST_ID);
    }
}
