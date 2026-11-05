using CommunityToolkit.Mvvm.Input;
using NextAuth.Core.Utility;

namespace NextAuth.Core.ViewModel.MafileMover;

public partial class MafileMoverDoneStepVM : MafileMoverStepVM
{
    public string? InnerTip { get; }
    private readonly TaskCompletionSource _doneTcs = new();
    private readonly string _rCode;

    public MafileMoverDoneStepVM(string rCode, string steamId)
    {
        var tipStr = LocManager.GetCodeBehindOrDefault("DoneStepTip", MafileMoverVM.LOCALIZATION_KEY, "DoneStepTip");
        InnerTip = string.Format(tipStr, rCode, steamId);
        _rCode = rCode;
    }

    public Task GetResultAsync()
    {
        return _doneTcs.Task;
    }

    public override Task Next()
    {
        _doneTcs.TrySetResult();
        return Task.CompletedTask;
    }

    public override bool CanExecute()
    {
        return true;
    }

    public override void Cancel()
    {
        _doneTcs.TrySetResult();
    }

    [RelayCommand]
    private async Task CopyCode()
    {
        try
        {
            await ClipboardHelper.Set(_rCode);
        }
        catch
        {
        }
    }
}