using CommunityToolkit.Mvvm.ComponentModel;
using NextAuth.Core.Model.Entities;

namespace NextAuth.Core.ViewModel.Other;

public partial class LoginAgainOnImportVM : ObservableObject
{
    public LocalizationSource Loc => LocalizationSource.Instance;

    public bool IsFormValid => !string.IsNullOrWhiteSpace(Password);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFormValid))]
    private string _password = null!;

    [ObservableProperty] private bool _savePassword;

    [ObservableProperty] private string _userName = null!;

    public LoginAgainOnImportVM(Mafile mafile)
    {
        UserName = mafile.AccountName;
    }

    public LoginAgainOnImportVM()
    {
    }
}