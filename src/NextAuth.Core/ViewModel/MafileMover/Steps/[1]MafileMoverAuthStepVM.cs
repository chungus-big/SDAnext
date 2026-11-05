using CommunityToolkit.Mvvm.ComponentModel;

namespace NextAuth.Core.ViewModel.MafileMover;

public partial class MafileMoverAuthStepVM : MafileMoverStepVM
{
    public Func<Task> Callback { get; set; } = () => Task.CompletedTask;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private string _login = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private string _password = string.Empty;

    public MafileMoverAuthStepVM()
    {
    }

    public override Task Next()
    {
        return Callback.Invoke();
    }

    public override bool CanExecute()
    {
        return !string.IsNullOrWhiteSpace(Login) && !string.IsNullOrWhiteSpace(Password);
    }

    public (string, string) GetState()
    {
        return (Login, Password);
    }

    public override void Cancel()
    {
        Login = string.Empty;
        Password = string.Empty;
    }
}