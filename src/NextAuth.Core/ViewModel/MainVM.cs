using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NextAuth.Core.Model;
using NextAuth.Core.Model.Entities;
using NextAuth.Core.Model.Mafiles;
using NextAuth.Core.Utility;
using SteamLib.Exceptions.Authorization;

namespace NextAuth.Core.ViewModel;

public partial class MainVM : ObservableObject, IDisposable
{
    public Mafile? SelectedMafile
    {
        get => _selectedMafile;
        set => SetMafile(value);
    }

    public bool IsMafileSelected => SelectedMafile != null;

    public LocalizationSource Loc => LocalizationSource.Instance;

    public ObservableCollection<Mafile> MaFiles => Storage.MaFiles;

    private Mafile? _selectedMafile;

    public MainVM()
    {
        CreateCodeTimer();
        Storage.MaFiles.CollectionChanged += MaFilesOnCollectionChanged;
        Confirmations.CollectionChanged += (_, _) => OnPropertyChanged(nameof(NoConfirmationsVisible));
        LocManager.CurrentLanguageChanged += OnCurrentLanguageChanged;
        Settings.Instance.PropertyChanged += SettingsOnPropertyChanged;
        SelectedMafile = Storage.MaFiles.FirstOrDefault();
    }

    private void OnCurrentLanguageChanged(object? sender, EventArgs e)
    {
        foreach (var conf in Confirmations)
        {
            if (conf is MarketMultiConfirmation multi)
            {
                multi.UpdateHeadline();
            }
        }
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.SeparateMarketConfirmations) && SelectedMafile != null)
        {
            _ = GetConfirmations();
        }
    }

    [RelayCommand]
    public async Task Debug()
    {
    }

    private void SetMafile(Mafile? mafile)
    {
        if (mafile != SelectedMafile)
        {
            _confirmationsCts?.Cancel();
            _selectedMafile = mafile;
            _confirmationsLoadedForMafile = null;
            Confirmations.Clear();
            OnPropertyChanged(nameof(ConfirmationsVisible));
            OnPropertyChanged(nameof(NoConfirmationsVisible));
            OnPropertyChanged(nameof(SelectedMafile));
            MaClient.SetAccount(mafile);
            if (mafile != null)
            {
                _ = UpdateCodeForMafileAsync(mafile);
                _ = GetConfirmations();
            }
            else
            {
                Code = "Code";
                IsLoadingConfirmations = false;
            }
            OnPropertyChanged(nameof(IsMafileSelected));
            OnPropertyChanged(nameof(CanGetConfirmations));
            GetConfirmationsCommand.NotifyCanExecuteChanged();
            RefreshSessionCommand.NotifyCanExecuteChanged();
            LoginAgainCommand.NotifyCanExecuteChanged();
            RemoveAuthenticatorCommand.NotifyCanExecuteChanged();
            ConfirmLoginCommand.NotifyCanExecuteChanged();
        }
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Settings.Instance.PropertyChanged -= SettingsOnPropertyChanged;
        LocManager.CurrentLanguageChanged -= OnCurrentLanguageChanged;
        Storage.MaFiles.CollectionChanged -= MaFilesOnCollectionChanged;

        _confirmationsCts?.Cancel();
        _confirmationsCts?.Dispose();
        _confirmationsCts = null;
        _codeTimer?.Dispose();
        GC.SuppressFinalize(this);
    }


    [RelayCommand(CanExecute = nameof(IsMafileSelected))]
    public async Task LoginAgain()
    {
        var selectedMafile = SelectedMafile;
        if (selectedMafile == null) return;
        var currentPassword = PHandler.DecryptPassword(selectedMafile.Password);
        var loginAgainVm = await DialogsController.ShowLoginAgainDialog(selectedMafile.AccountName, currentPassword);
        if (loginAgainVm == null)
        {
            return;
        }

        var password = loginAgainVm.Password;
        using var wait = DialogsController.ShowWaitLoginDialog();
        try
        {
            await MaClient.LoginAgain(selectedMafile, password, loginAgainVm.SavePassword);
            SnackbarController.SendSuccess(GetLocalization("SuccessfulLogin"));
        }
        catch (LoginException ex)
        {
            SnackbarController.SendError(ErrorTranslatorHelper.TranslateLoginError(ex.Error),
                TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
            when (ExceptionHandler.Handle(ex))
        {
        }
    }

    private void MaFilesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (SelectedMafile == null || !Storage.MaFiles.Contains(SelectedMafile))
        {
            SelectedMafile = Storage.MaFiles.FirstOrDefault();
        }
        OnPropertyChanged(nameof(MaFiles));
    }


    [RelayCommand(CanExecute = nameof(IsMafileSelected))]
    private async Task RefreshSession()
    {
        var selectedMafile = SelectedMafile;
        if (selectedMafile == null) return;
        try
        {
            await MaClient.RefreshSession(selectedMafile);
            SnackbarController.SendSuccess(GetLocalization("SessionRefreshed"));
        }
        catch (Exception ex) when (ExceptionHandler.Handle(ex))
        {
        }
    }

    [RelayCommand]
    public async Task MoveAccount()
    {
        await DialogsController.ShowMafileMoverDialog();
    }

    [RelayCommand(CanExecute = nameof(IsMafileSelected))]
    private async Task RemoveAuthenticator()
    {
        var selectedMafile = SelectedMafile;
        if (selectedMafile == null) return;
        if (string.IsNullOrWhiteSpace(selectedMafile.RevocationCode))
        {
            SnackbarController.SendWarning(LocManager.GetCommonOrDefault("Error", "Error") + ": " +
                                            GetLocalization("MissingRCode"));
            return;
        }

        try
        {
            if (await DialogsController.ShowConfirmCancelDialog(GetLocalization("ConfirmRemovingAuthenticator")))
            {
                var result =
                    await SessionHandler.Handle(() => MaClient.RemoveAuthenticator(selectedMafile), selectedMafile);
                if (result.Success)
                {
                    SnackbarController.SendSuccess(GetLocalization("AuthenticatorRemoved"));
                    Storage.MoveToRemoved(selectedMafile);
                }
                else
                {
                    SnackbarController.SendError(GetLocalization("AuthenticatorNotRemoved"));
                }
            }
            else
            {
                SnackbarController.SendSnackbar(LocManager.GetCommonOrDefault("Canceled", "Canceled"));
            }
        }
        catch (Exception ex)
            when (ExceptionHandler.Handle(ex, handleAllExceptions: true))
        {
        }
    }

    [RelayCommand(CanExecute = nameof(IsMafileSelected))]
    private async Task ConfirmLogin()
    {
        var selectedMafile = SelectedMafile;
        if (selectedMafile == null) return;
        try
        {
            var res = await SessionHandler.Handle(() => MaClient.ConfirmLoginRequest(selectedMafile), selectedMafile);
            if (res.Success)
            {
                SnackbarController.SendSuccess($"{GetLocalization("ConfirmLoginSuccess")} {res.IP} ({res.Country})");
            }
            else
            {
                var msg = res.Error switch
                {
                    LoginConfirmationError.NoRequests => GetLocalization("ConfirmLoginFailedNoRequests"),
                    LoginConfirmationError.MoreThanOneRequest =>
                        GetLocalization("ConfirmLoginFailedMoreThanOneRequest"), //TODO
                    _ => throw new ArgumentOutOfRangeException()
                };
                SnackbarController.SendError(msg);
            }
        }
        catch (Exception ex)
            when (ExceptionHandler.Handle(ex, handleAllExceptions: true))
        {
        }
    }

    private static string GetLocalization(string key)
    {
        const string locPath = "MainVM";
        return LocManager.GetCodeBehindOrDefault(key, locPath, key);
    }
}