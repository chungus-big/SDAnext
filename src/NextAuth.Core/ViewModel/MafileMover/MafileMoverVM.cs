using CommunityToolkit.Mvvm.ComponentModel;
using NextAuth.Core.Services;
using NextAuth.Core.Model;
using System.Text.Json;
using NextAuth.Core.Model.Entities;
using NextAuth.Core.Model.Mafiles;
using NextAuth.Core.Utility;
using NextAuth.Core.ViewModel.Linker;
using SteamLib.Api.Mobile;
using SteamLib.Api.Services;
using SteamLib.Authentication;
using SteamLib.Authentication.LoginV2;
using SteamLib.Core.StatusCodes;
using SteamLib.Exceptions;
using SteamLib.Exceptions.Authorization;
using SteamLib.Factory.Helpers;
using SteamLib.ProtoCore.Enums;
using SteamLib.ProtoCore.Services;
using SteamLib.Web;
using SteamLib.Abstractions.Auth;
using SteamLib.Models;
using SteamLib.Models.Session;

namespace NextAuth.Core.ViewModel.MafileMover;

public partial class MafileMoverVM : ObservableObject, IAuthProvider, IDisposable
{
    public const string LOCALIZATION_KEY = "MafileMoverVM";

    private readonly HttpClient _client;
    private readonly HttpClientHandler _handler;
    private CancellationTokenSource _cts = new();
    [ObservableProperty] private MafileMoverStepVM _currentStep;

    [ObservableProperty] private string? _error;

    [ObservableProperty] private string? _tip;

    public LocalizationSource Loc => LocalizationSource.Instance;

    public MafileMoverVM()
    {
        var pair = ClientBuilder.BuildMobileClient();
        _client = pair.Client;
        _handler = pair.Handler;

        CurrentStep = new MafileMoverAuthStepVM
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
            if (CurrentStep is not MafileMoverAuthStepVM)
            {
                var step = new MafileMoverAuthStepVM
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
        if (CurrentStep is not MafileMoverAuthStepVM authStep)
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
            Error = GetLinkerLocalizationOrDefault("CantLogin") + ErrorTranslatorHelper.TranslateLoginError(LoginError.InvalidCredentials);
            return;
        }

        if (authResult is AuthResult.NeedEmailCode)
        {
            Error = GetLinkerLocalizationOrDefault("CantLogin") + GetLocalizationOrDefault("GuardIsNotActive");
            return;
        }

        if (authResult is AuthResult.RateLimited)
        {
            Error = GetLinkerLocalizationOrDefault("CantLogin") + ErrorTranslatorHelper.TranslateSteamStatusCode(SteamStatusCode.RateLimitExceeded);
            return;
        }

        if (authResult is AuthResult.Failed failed)
        {
            if (failed.Exception is SteamStatusCodeException ssc)
                Error = GetLinkerLocalizationOrDefault("CantLogin") + ErrorTranslatorHelper.TranslateSteamStatusCode(ssc.StatusCode);
            else if (failed.Exception is LoginException le)
                Error = GetLinkerLocalizationOrDefault("CantLogin") + ErrorTranslatorHelper.TranslateLoginError(le.Error);
            else
                Error = GetLinkerLocalizationOrDefault("CantLogin") + failed.Error;
            return;
        }

        if (authResult is not AuthResult.Success { Session: MobileSessionData msd })
        {
            Error = "Session data is not MobileSession.";
            return;
        }

        if (CurrentStep is not MafileMoverGuardCodeStepVM)
        {
            //No guard was requested
            Error = GetLocalizationOrDefault("GuardIsNotActive");
            return;
        }

        var token = msd.GetToken(SteamDomain.Community)!.Value.Token;
        try
        {
            await SteamAuthenticatorLinkerApi.RemoveAuthenticatorViaChallengeStart(_client, token, _cts.Token);
        }
        catch (SteamStatusCodeException ex)
            when (ex.StatusCode.Equals(SteamStatusCode.Fail))
        {
            Error = GetLocalizationOrDefault("SeemsNoPhoneNumber");
            return;
        }
        catch (Exception ex)
            when (ex is not OperationCanceledException)
        {
            Error = ExceptionHandler.GetExceptionString(ex);
            return;
        }


        RemoveAuthenticatorViaChallengeContinue_Response res;
        try
        {
            var i = 0;
            while (true)
            {
                try
                {
                    var sms = await GetSms();
                    res = await SteamAuthenticatorLinkerApi
                        .RemoveAuthenticatorViaChallengeContinue(_client, token, sms.ToString("D5"), _cts.Token);
                    Error = null;
                    break;
                }
                catch (SteamStatusCodeException ex)
                    when (ex.StatusCode.Equals(SteamStatusCode.SmsCodeFailed))
                {
                    Error = GetLocalizationOrDefault("SmsCodeFailed");
                    if (i == 2) throw;
                }

                i++;
            }
        }
        catch (Exception ex)
            when (ex is not OperationCanceledException)
        {
            Error = ExceptionHandler.GetExceptionString(ex);
            return;
        }

        var t = res.ReplacementToken;
        var j = JsonSerializer.Serialize(t);
        Storage.WriteBackup(login, j);
        var mobileData = t.ToMobileDataExtended(SteamAuthenticatorLinkerApi.GenerateDeviceId(), msd);


        var mafile = Mafile.FromMobileDataExtended(mobileData);
        try
        {
            if (Settings.Instance.IsPasswordSet)
                mafile.Password = PHandler.Encrypt(pass);
        }
        catch
        {
        }

        await Storage.SaveMafileAsync(mafile);
        await Done(mafile.RevocationCode ?? string.Empty, mafile.SteamId.Steam64.ToString());
    }

    private void SetCurrentStep(MafileMoverStepVM step)
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

    private static string GetLinkerLocalizationOrDefault(string key)
    {
        return LocManager.GetCodeBehindOrDefault(key, LinkAccountVM.LOCALIZATION_KEY, key);
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


    public bool IsSupportedGuardType(ILoginConsumer consumer, EAuthSessionGuardType type)
    {
        return type == EAuthSessionGuardType.DeviceCode;
    }

    // @formatter:off

    #region Step 2: Guard Code
    // Step 2: Guard Code
    public async Task UpdateAuthSession(HttpClient authClient, ILoginConsumer loginConsumer,
        UpdateAuthSessionModel model,
        CancellationToken cancellationToken = default)
    {
        var isRetrying = CurrentStep is MafileMoverGuardCodeStepVM;
        while (true)
        {
            try
            {
                var step = new MafileMoverGuardCodeStepVM(isRetrying);
                SetCurrentStep(step);
                var res = await step.GetResultAsync();
                if (res == null)
                {
                    return;
                }

                var req = AuthRequestHelper.CreateMobileCodeRequest(res, model.ClientId, model.SteamId);
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

    #region Step 3: Sms code

    // Step 3: Sms code
    public Task<int> GetSms()
    {
        var step = new MafileMoverSmsStepVM();
        SetCurrentStep(step);
        return step.GetResultAsync();
    }

    #endregion

    #region Step 4: Done

    public async Task Done(string rCode, string steamId)
    {
        var step = new MafileMoverDoneStepVM(rCode, steamId);
        SetCurrentStep(step);
        await step.GetResultAsync();
    }

    #endregion

    // @formatter:on
}