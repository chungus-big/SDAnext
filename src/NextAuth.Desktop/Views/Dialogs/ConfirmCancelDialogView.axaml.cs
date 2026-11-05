using NextAuth.Desktop.Services;
using Avalonia.Controls;
using DialogHostAvalonia;

namespace NextAuth.Desktop.Views.Dialogs;

public partial class ConfirmCancelDialogView : UserControl
{
    public string? Title
    {
        get => TitleText.Text;
        set => TitleText.Text = value;
    }

    public string? Message
    {
        get => MessageText.Text;
        set => MessageText.Text = value;
    }

    public string? ConfirmText
    {
        get => ConfirmButton.Content as string;
        set => ConfirmButton.Content = value;
    }

    public string? CancelText
    {
        get => CancelButton.Content as string;
        set => CancelButton.Content = value;
    }

    public ConfirmCancelDialogView()
    {
        InitializeComponent();
        ConfirmButton.Click += (_, _) => DialogHost.Close(AvaloniaDialogService.MAIN_HOST_ID, true);
        CancelButton.Click += (_, _) => DialogHost.Close(AvaloniaDialogService.MAIN_HOST_ID, false);
    }
}