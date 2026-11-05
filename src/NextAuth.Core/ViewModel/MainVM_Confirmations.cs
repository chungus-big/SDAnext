using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextAuth.Core.Model;
using NextAuth.Core.Model.Entities;
using NextAuth.Core.Utility;
using SteamLib.SteamMobile.Confirmations;
using SteamLib.SteamMobile.Confirmations.Concrete;

namespace NextAuth.Core.ViewModel;

public partial class MainVM //Confirmations
{
    public ObservableCollection<Confirmation> Confirmations { get; } = [];
    public bool ConfirmationsVisible => SelectedMafile != null && SelectedMafile == _confirmationsLoadedForMafile;
    public bool NoConfirmationsVisible => ConfirmationsVisible && Confirmations.Count == 0 && !IsLoadingConfirmations;
    private Mafile? _confirmationsLoadedForMafile;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GetConfirmationsCommand))]
    [NotifyPropertyChangedFor(nameof(CanGetConfirmations))]
    [NotifyPropertyChangedFor(nameof(NoConfirmationsVisible))]
    private bool _isLoadingConfirmations;

    public bool CanGetConfirmations => IsMafileSelected && !IsLoadingConfirmations;

    private CancellationTokenSource? _confirmationsCts;
    private int _confirmationRequestId;

    [RelayCommand(CanExecute = nameof(CanGetConfirmations))]
    private async Task GetConfirmations()
    {
        if (SelectedMafile == null) return;
        var maf = SelectedMafile;

        _confirmationsCts?.Cancel();
        _confirmationsCts?.Dispose();
        var cts = new CancellationTokenSource();
        _confirmationsCts = cts;
        var requestId = Interlocked.Increment(ref _confirmationRequestId);

        IsLoadingConfirmations = true;
        try
        {
            List<Confirmation> conf;
            try
            {
                var confI = await SessionHandler.Handle(() => MaClient.GetConfirmations(maf, cts.Token), maf);
                conf = [.. confI];
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
                when (ExceptionHandler.Handle(ex))
            {
                return;
            }

            if (cts.IsCancellationRequested || requestId != _confirmationRequestId || maf != SelectedMafile)
                return;

            _confirmationsLoadedForMafile = maf;
            OnPropertyChanged(nameof(ConfirmationsVisible));
            Confirmations.Clear();
            var marketConfirmations = conf
                .Where(c => c.ConfType == ConfirmationType.MarketSellTransaction)
                .Cast<MarketConfirmation>()
                .ToList();

            if (!Settings.Instance.SeparateMarketConfirmations && marketConfirmations.Count > 1)
            {
                var indexOfLast = conf.IndexOf(marketConfirmations.First());
                foreach (var mCon in marketConfirmations)
                {
                    conf.Remove(mCon);
                }

                var mConf = new MarketMultiConfirmation(marketConfirmations);
                conf.Insert(indexOfLast, mConf);
            }

            foreach (var con in conf)
            {
                Confirmations.Add(con);
            }

            OnPropertyChanged(nameof(NoConfirmationsVisible));
        }
        finally
        {
            if (requestId == _confirmationRequestId)
            {
                IsLoadingConfirmations = false;
            }
        }
    }

    [RelayCommand]
    private Task Confirm(Confirmation? confirmation)
    {
        if (SelectedMafile == null || confirmation == null) return Task.CompletedTask;
        return SendConfirmation(SelectedMafile, confirmation, true);
    }

    [RelayCommand]
    private Task Cancel(Confirmation? confirmation)
    {
        if (SelectedMafile == null || confirmation == null) return Task.CompletedTask;
        return SendConfirmation(SelectedMafile, confirmation, false);
    }


    private async Task SendConfirmation(Mafile mafile, Confirmation confirmation, bool confirm)
    {
        bool result;

        try
        {
            result = confirmation switch
            {
                MarketMultiConfirmation multi => await MaClient.SendMultipleConfirmation(mafile, multi.Confirmations, confirm),
                MarketConfirmation market => await MaClient.SendConfirmation(mafile, market, confirm),
                _ => await MaClient.SendConfirmation(mafile, confirmation, confirm)
            };
        }
        catch (Exception ex)
            when (ExceptionHandler.Handle(ex))
        {
            return;
        }

        if (!result)
        {
            SnackbarController.SendError(GetLocalization("ConfirmationError"));
            return;
        }

        UpdateConfirmationState(confirmation);
    }

    private void UpdateConfirmationState(Confirmation confirmation)
    {
        if (confirmation is MarketConfirmation item)
        {
            var parent = Confirmations
                .OfType<MarketMultiConfirmation>()
                .FirstOrDefault(p => p.Confirmations.Contains(item));

            if (parent == null)
            {
                Confirmations.Remove(item);
                return;
            }

            parent.Confirmations.Remove(item);

            if (parent.Confirmations.Count == 0)
            {
                Confirmations.Remove(parent);
                return;
            }

            if (parent.Confirmations.Count == 1)
            {
                var remaining = parent.Confirmations[0];
                var idx = Confirmations.IndexOf(parent);
                Confirmations[idx] = remaining;
                return;
            }

            parent.Time = parent.Confirmations.FirstOrDefault()?.Time ?? parent.Time;
            parent.UpdateHeadline();
            return;
        }

        Confirmations.Remove(confirmation);
    }
}