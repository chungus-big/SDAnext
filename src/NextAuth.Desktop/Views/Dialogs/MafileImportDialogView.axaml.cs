using NextAuth.Desktop.Services;
using Avalonia.Controls;
using DialogHostAvalonia;
using NextAuth.Core.ViewModel.Other;

namespace NextAuth.Desktop.Views.Dialogs;

public partial class MafileImportDialogView : UserControl
{
    public MafileImportDialogView()
    {
        InitializeComponent();
        OkButton.Click += (_, _) =>
        {
            if (DataContext is MafileImportDialogVM vm)
            {
                var result = new MafileImportDialogResult(vm.OverwriteConflicts);
                DialogHost.Close(AvaloniaDialogService.MAIN_HOST_ID, result);
            }
            else
            {
                DialogHost.Close(AvaloniaDialogService.MAIN_HOST_ID, null);
            }
        };
        CancelButton.Click += (_, _) => DialogHost.Close(AvaloniaDialogService.MAIN_HOST_ID, null);
    }
}
