using Avalonia.Controls;
using DialogHostAvalonia;
using NextAuth.Desktop.Services;

namespace NextAuth.Desktop.Views.Dialogs;

public partial class MafileExporterView : UserControl
{
    public MafileExporterView()
    {
        InitializeComponent();
        CloseButton.Click += (_, _) => DialogHost.Close(AvaloniaDialogService.MAIN_HOST_ID);
    }
}
