using NextAuth.Desktop.Services;
using Avalonia.Controls;
using DialogHostAvalonia;
using NextAuth.Core;
using NextAuth.Core.Services;

namespace NextAuth.Desktop.Views.Dialogs;

public partial class LinksView : UserControl
{
    public LocalizationSource Loc => LocalizationSource.Instance;

    public LinksView()
    {
        InitializeComponent();
        DataContext = this;

        GitHubButton.Click += (_, _) => AppServices.App.OpenUrl("https://github.com/chungus-big");
        CloseButton.Click += (_, _) => DialogHost.Close(AvaloniaDialogService.MAIN_HOST_ID);
    }
}
