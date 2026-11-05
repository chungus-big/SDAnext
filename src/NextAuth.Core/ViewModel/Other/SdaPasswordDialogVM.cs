using CommunityToolkit.Mvvm.ComponentModel;

namespace NextAuth.Core.ViewModel.Other;

public partial class SdaPasswordDialogVM : ObservableObject
{
    public bool IsFormValid => !string.IsNullOrWhiteSpace(Password);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFormValid))]
    private string _password = string.Empty;
}