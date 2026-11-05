using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NextAuth.Core.ViewModel.Linker;

public abstract partial class LinkAccountStepVM : ObservableObject
{
    public virtual string? Tip => null;

    [RelayCommand(CanExecute = nameof(CanExecute))]
    public abstract Task Next();

    public abstract bool CanExecute();

    [RelayCommand]
    public abstract void Cancel();
}