using CommunityToolkit.Mvvm.ComponentModel;
using NextAuth.Core.Services;
using NextAuth.Core.Model;
using NextAuth.Core.Model.Entities;
using NextAuth.Core.Model.Mafiles;
using NextAuth.Core.Utility;
using SteamLib;
using SteamLib.Api.Services;
using SteamLib.Authentication;
using SteamLib.Authentication.LoginV2;
using SteamLib.Core.StatusCodes;
using SteamLib.Exceptions;
using SteamLib.Exceptions.Authorization;
using SteamLib.Exceptions.Mobile;
using SteamLib.Factory.Helpers;
using SteamLib.ProtoCore.Enums;
using SteamLib.SteamMobile.AuthenticatorLinker;
using SteamLib.SteamMobile.AuthenticatorLinker.Models;
using SteamLib.Web;
using SteamLib.Abstractions.Auth;
using SteamLib.Abstractions.Linker;
using SteamLib.Models.Session;

namespace NextAuth.Core.ViewModel.Linker;

public partial class LinkAccountVM : ObservableObject, ISmsCodeProvider, IPhoneNumberProvider, IEmailProvider,
    IDisposable
{
    public const string LOCALIZATION_KEY = "LinkVM";

    int IEmailProvider.RetryCount => 3;
    private readonly HttpClient _client;
    private readonly HttpClientHandler _handler;
    private CancellationTokenSource _cts = new();
    [ObservableProperty] private LinkAccountStepVM _currentStep;

    [ObservableProperty] private string? _error;

    [ObservableProperty] private string? _tip;

    public LocalizationSource Loc => LocalizationSource.Instance;

    public LinkAccountVM()
    {
        var pair = ClientBuilder.BuildMobileClient();
        _client = pair.Client;
        _handler = pair.Handler;

        CurrentStep = new LinkAccountAuthStepVM
        {
            Callback = Proceed
        };
    }

    public async Task Proceed()
    {
        _cts = new CancellationTokenSource();
        _cts.Token.Register(CurrentStep.Cancel);
        _handler.CookieContainer.ClearMobileSessionCookies();


        try
        {
            await ProceedImpl();
        }
        catch (OperationCanceledException)
        {
            //Ignored
        }
        catch (Exception ex)
        {
            Error = ExceptionHandler.GetExceptionString(ex);
        }
        finally
        {
            _cts.Dispose();
            if (CurrentStep is not LinkAccountAuthStepVM)
            {
                var step = new LinkAccountAuthStepVM
                {
                    Callback = Proceed
                };
                SetCurrentStep(step);
            }
        }
    }

    private async Task ProceedImpl()
    {
        // Step 1: login/pass
        Error = null;
        if (CurrentStep is not LinkAccountAuthStepVM authStep)
        {
            throw new InvalidOperationException("Current step is not an authentication step.");
        }

        var (login, pass) = authStep.GetState();
        var log = new LoginV2ExecutorOptions(StaticLoginConsumer.Instance, _client)
        {
            DeviceDetails = DeviceDetailsDefaultBuilder.GetMobileDefaultDevice(),
            WebsiteId = "Mobile",
            AuthProviders = [this]
        };

        var authResult = await LoginV2Executor.AuthenticateAsync(log, login, pass, _cts.Token);
        if (authResult is AuthResult.InvalidCredentials)
        {
            Error = GetLocalizationOrDefault("CantLogin") + ErrorTranslatorHelper.TranslateLoginError(LoginError.InvalidCredentials);
            return;
        }

        if (authResult is AuthResult.NeedDeviceCode)
        {
            Error = GetLocalizationOrDefault("CantLogin") + GetLocalizationOrDefault("UnsupportedGuardType");
            return;
        }

        if (authResult is AuthResult.RateLimited)
        {
            Error = GetLocalizationOrDefault("CantLogin") + ErrorTranslatorHelper.TranslateSteamStatusCode(SteamStatusCode.RateLimitExceeded);
            return;
        }

        if (authResult is AuthResult.Failed failed)
        {
            if (failed.Exception is SteamStatusCodeException ssc)
                Error = GetLocalizationOrDefault("CantLogin") + ErrorTranslatorHelper.TranslateSteamStatusCode(ssc.StatusCode);
            else if (failed.Exception is LoginException le)
                Error = GetLocalizationOrDefault("CantLogin") + ErrorTranslatorHelper.TranslateLoginError(le.Error);
            else
                Error = GetLocalizationOrDefault("CantLogin") + failed.Error;
            return;
        }

        if (authResult is not AuthResult.Success { Session: MobileSessionData msd })
        {
            Error = "Session data is not MobileSession.";
            return;
        }


        var linkOptions = new LinkOptions(_client, StaticLoginConsumer.Instance, this,
            this, this, Storage.WriteBackup);
        var linker = new SteamAuthenticatorLinker(linkOptions);
        MobileDataExtended result;
        try
        {
            result = await linker.LinkAccount(msd, _cts.Token);
        }
        catch (AuthenticatorLinkerException ex)
        {
            Error = $"{GetLocalizationCommon("Error")}: {ErrorTranslatorHelper.TranslateLinkerError(ex.Error)}";
            return;
        }
        catch (HttpRequestException ex)
        {
            var msg = ex.StatusCode?.ToString() ?? ex.Message;
            Error = $"{GetLocalizationCommon("RequestError")}: {msg}";
            return;
        }
        catch (SteamStatusCodeException ex)
        {
            Error = GetLocalizationOrDefault("ErrorWithCode") +
                    ErrorTranslatorHelper.TranslateSteamStatusCode(ex.StatusCode);

            return;
        }
        catch (Exception ex)
            when (ex is not OperationCanceledException)
        {
            Error = GetLocalizationOrDefault("UnknownError") + ex.Message;
            return;
        }

        var mafile = Mafile.FromMobileDataExtended(result);
        try
        {
            if (Settings.Instance.IsPasswordSet)
                mafile.Password = PHandler.Encrypt(pass);
        }
        catch
        {
        }

        await Storage.SaveMafileAsync(mafile);
        await Done(mafile.RevocationCode ?? string.Empty, mafile.SteamId.Steam64.ToString(), login);
    }

    private void SetCurrentStep(LinkAccountStepVM step)
    {
        AppServices.App.PostToUI(() =>
        {
            CurrentStep = step;
            Tip = CurrentStep.Tip;
        });
    }


    private static string GetLocalizationOrDefault(string key)
    {
        return LocManager.GetCodeBehindOrDefault(key, LOCALIZATION_KEY, key);
    }

    private static string GetLocalizationCommon(string key)
    {
        return LocManager.GetCommonOrDefault(key, key);
    }


    public void Dispose()
    {
        _client.Dispose();
        _handler.Dispose();
        try
        {
            _cts.Cancel();
        }
        catch
        {
            //Ignored, may be cancelled or disposed
        }

        _cts.Dispose();
    }

    // @formatter:off
    #region Step 2: Email Code

    public bool IsSupportedGuardType(ILoginConsumer consumer, EAuthSessionGuardType type)
    {
        return type == EAuthSessionGuardType.EmailCode;
    }

    // Step 2: Email Code
    public async Task UpdateAuthSession(HttpClient authClient, ILoginConsumer loginConsumer,
        UpdateAuthSessionModel model,
        CancellationToken cancellationToken = default)
    {
        while (true)
        {
            try
            {
                var step = new LinkAccountEmailAuthStepVM();
                SetCurrentStep(step);
                var res = await step.GetResultAsync();
                var req = AuthRequestHelper.CreateEmailCodeRequest(res, model.ClientId, model.SteamId);
                await AuthenticationServiceApi.UpdateAuthSessionWithSteamGuardCode(authClient, req, cancellationToken);
                Error = null;
                break;
            }
            catch (SteamStatusCodeException ex)
                when (ex.StatusCode.Equals(SteamStatusCode.InvalidLoginAuthCode))
            {
                Error = ExceptionHandler.GetExceptionString(ex);
            }
        }
    }

    #endregion

    #region Step 3: Phone Number

    // Step 3: Phone number
    public Task<long?> GetPhoneNumber(ILoginConsumer caller)
    {
        var step = new LinkAccountPhoneStepVM();
        SetCurrentStep(step);
        return step.GetResultAsync();
    }

    #endregion

    #region Step 4: Confirm Phone

    public Task ConfirmEmailLink(ILoginConsumer caller, EmailConfirmationType confirmationType,
        CancellationToken cancellationToken = default)
    {
        var retry = CurrentStep is LinkAccountConfirmEmailStepVM;
        var step = new LinkAccountConfirmEmailStepVM(retry);
        SetCurrentStep(step);
        return step.GetResultAsync();
    }

    #endregion

    #region Step 5: SMS Code

    public Task<int> GetSmsCode(ILoginConsumer caller, long? phoneNumber, string? phoneHint)
    {
        var step = new LinkAccountSmsStepVM(phoneHint);
        SetCurrentStep(step);
        return step.GetResultAsync();
    }

    #endregion

    #region Step 6: Email Code

    public async ValueTask<string> GetAddAuthenticatorCode(ILoginConsumer caller,
        CancellationToken cancellationToken = default)
    {
        var step = new LinkAccountEmailCodeStepVM();
        SetCurrentStep(step);
        return await step.GetResultAsync();
    }

    #endregion

    #region Step 7: Done

    public async Task Done(string rCode, string steamId, string login)
    {
        var filename = Settings.Instance.UseAccountNameAsMafileName ? login : steamId;
        var step = new LinkAccountDoneStepVM(rCode, filename);
        SetCurrentStep(step);
        await step.GetResultAsync();
    }

    #endregion
    // @formatter:on
}