using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextAuth.Core.Services;
using NextAuth.Core.Model.Entities;
using SteamLib.SteamMobile;

namespace NextAuth.Core.ViewModel;

public partial class MainVM
{
    [ObservableProperty] private string _code = "Code";
    [ObservableProperty] private double _codeProgress;
    private Timer _codeTimer;
    private long _lastCodePeriod = -1;
    private int _isUpdatingCode;

    [MemberNotNull(nameof(_codeTimer))]
    private void CreateCodeTimer()
    {
        _codeTimer = new Timer(UpdateCode, null, 0, 1000);
    }

    private async void UpdateCode(object? state = null)
    {
        if (Interlocked.CompareExchange(ref _isUpdatingCode, 1, 0) != 0)
        {
            return;
        }

        try
        {
            await UpdateCodeInternalAsync().ConfigureAwait(false);
        }
        catch
        {
        }
        finally
        {
            Interlocked.Exchange(ref _isUpdatingCode, 0);
        }
    }

    private async Task UpdateCodeInternalAsync()
    {
        var currentTime = await TimeAligner.GetSteamTimeAsync().ConfigureAwait(false);
        var currentPeriod = currentTime / 30;
        var progress = CalculateCodeProgress(currentTime);

        if (AppServices.App == null) return;
        AppServices.App.PostToUI(() =>
        {
            CodeProgress = progress;

            if (currentPeriod != _lastCodePeriod)
            {
                _lastCodePeriod = currentPeriod;
                if (SelectedMafile != null)
                {
                    Code = SteamGuardCodeGenerator.GenerateCode(SelectedMafile.SharedSecret, currentTime);
                }
            }
        });
    }

    private async Task UpdateCodeForMafileAsync(Mafile mafile)
    {
        try
        {
            var currentTime = await TimeAligner.GetSteamTimeAsync().ConfigureAwait(false);
            if (SelectedMafile != mafile) return;

            var code = SteamGuardCodeGenerator.GenerateCode(mafile.SharedSecret, currentTime);
            var progress = CalculateCodeProgress(currentTime);

            AppServices.App?.PostToUI(() =>
            {
                if (SelectedMafile == mafile)
                {
                    Code = code;
                    CodeProgress = progress;
                }
            });
        }
        catch
        {
        }
    }

    private static long CalculateCodeProgress(long currentTime)
    {
        var elapsed = currentTime % 30;
        return 30 - elapsed;
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task CopyCode()
    {
        var selectedMafile = SelectedMafile;
        if (selectedMafile == null) return;
        try
        {
            await AppServices.Clipboard.SetTextAsync(Code);
            Code = LocManager.GetOrDefault("CodeCopied", "MainWindow", "CodeCopied");
        }
        catch
        {
        }
        finally
        {
            await Task.Delay(200);
            selectedMafile = SelectedMafile;
            if (selectedMafile != null)
            {
                try
                {
                    var time = await TimeAligner.GetSteamTimeAsync().ConfigureAwait(false);
                    Code = SteamGuardCodeGenerator.GenerateCode(selectedMafile.SharedSecret, time);
                }
                catch
                {
                }
            }
        }
    }
}