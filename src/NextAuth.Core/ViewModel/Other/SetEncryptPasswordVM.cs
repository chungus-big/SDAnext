using CommunityToolkit.Mvvm.ComponentModel;

namespace NextAuth.Core.ViewModel.Other;

public partial class SetEncryptPasswordVM : ObservableObject
{
    [ObservableProperty] private string? _password;
}