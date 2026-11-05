using CommunityToolkit.Mvvm.ComponentModel;

namespace NextAuth.Core.ViewModel.Other;

public partial class LoginAgainVM : ObservableObject
{
    public LocalizationSource Loc => LocalizationSource.Instance;

    public bool IsFormValid => !string.IsNullOrWhiteSpace(Password);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFormValid))]
    private string _password = null!;

    [ObservableProperty] private bool _savePassword;

    [ObservableProperty] private string _userName = null!;
}