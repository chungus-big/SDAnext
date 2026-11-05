using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NextAuth.Core.ViewModel;

public partial class MainVM : ObservableObject
{
    [RelayCommand]
    private Task OpenSetPasswordsDialog()
    {
        return DialogsController.ShowSetAccountsPasswordDialog();
    }

    [RelayCommand]
    private Task OpenExporterDialog()
    {
        return DialogsController.ShowExportDialog();
    }

    [RelayCommand]
    public Task LinkAccount()
    {
        return DialogsController.ShowLinkerDialog();
    }

    [RelayCommand]
    private Task OpenLinksView()
    {
        return DialogsController.ShowLinksDialog();
    }
}