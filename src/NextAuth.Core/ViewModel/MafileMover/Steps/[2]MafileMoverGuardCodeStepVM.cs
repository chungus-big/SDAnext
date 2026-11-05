using CommunityToolkit.Mvvm.ComponentModel;

namespace NextAuth.Core.ViewModel.MafileMover;

public partial class MafileMoverGuardCodeStepVM(bool isRetrying) : MafileMoverStepVM
{
    public override string? Tip { get; } = isRetrying
        ? LocManager.GetCodeBehindOrDefault("EnterGuardCodeRetrying", MafileMoverVM.LOCALIZATION_KEY,
            "EnterGuardCodeRetrying")
        : LocManager.GetCodeBehindOrDefault("EnterGuardCode", MafileMoverVM.LOCALIZATION_KEY, "EnterGuardCode");

    private readonly TaskCompletionSource<string?> _tcs = new();
    private bool _executing;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private string _guardCode = string.Empty;


    public Task<string?> GetResultAsync()
    {
        return _tcs.Task;
    }

    public override Task Next()
    {
        _executing = true;
        NextCommand.NotifyCanExecuteChanged();
        if (string.IsNullOrWhiteSpace(GuardCode))
        {
            _tcs.TrySetResult(null);
            return Task.CompletedTask;
        }

        _tcs.TrySetResult(GuardCode);
        return Task.CompletedTask;
    }

    public override bool CanExecute()
    {
        return (string.IsNullOrWhiteSpace(GuardCode) || GuardCode.Length == 5) && !_executing;
    }

    public override void Cancel()
    {
        _tcs.TrySetCanceled();
    }
}