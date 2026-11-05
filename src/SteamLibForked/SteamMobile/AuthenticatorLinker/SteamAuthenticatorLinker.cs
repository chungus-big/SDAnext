using SteamLib.Abstractions;
using SteamLib.Abstractions.Auth;
using SteamLib.Api.Mobile;
using SteamLib.Authentication;
using SteamLib.Exceptions.Authorization;
using SteamLib.Exceptions.Mobile;
using SteamLib.Models.Session;
using SteamLib.ProtoCore.Enums;
using SteamLib.SteamMobile.AuthenticatorLinker.Models;

namespace SteamLib.SteamMobile.AuthenticatorLinker;

public class SteamAuthenticatorLinker(LinkOptions options)
{
    public ILoginConsumer Consumer => Options.LoginConsumer;
    public LinkOptions Options { get; } = options;
    public ISessionData? SessionData { get; private set; }
    internal HttpClient Client => Options.HttpClient;
    internal string? DeviceId { get; set; }

    public async Task<MobileDataExtended> LinkAccount(MobileSessionData data, CancellationToken cancellationToken = default)
    {
        SessionData = data;

        data.EnsureValidated();
        if (data.IsExpired)
        {
            throw new SessionPermanentlyExpiredException(SessionPermanentlyExpiredException.SESSION_EXPIRED_MSG);
        }

        var accessToken = data.GetMobileToken()
            ?? throw new SessionInvalidException("SessionData must provide valid AccessToken to access mobile endpoints");

        if (accessToken.IsExpired || accessToken.Type != SteamAccessTokenType.Mobile)
        {
            var refreshed = await SteamMobileApi.RefreshJwt(Options.HttpClient, data.RefreshToken.Token,
                data.RefreshToken.SteamId, cancellationToken).ConfigureAwait(false);
            accessToken = SteamTokenHelper.Parse(refreshed);
            data.SetMobileToken(accessToken);
        }

        if (accessToken.Type != SteamAccessTokenType.Mobile)
            throw new SessionInvalidException(
                "SessionData must provide AccessToken of type Mobile to access mobile endpoints");


        var hasPhoneResp = await this.HasPhone(cancellationToken).ConfigureAwait(false);
        var hasPhone = hasPhoneResp.HasPhone;


        if (hasPhone && Options.SmsCodeProvider == null)
        {
            throw new AuthenticatorLinkerException(AuthenticatorLinkerError.PhoneAlreadyAttached);
        }

        long? phoneNumber = null;

        if (!hasPhone && Options.PhoneNumberProvider != null)
        {
            phoneNumber = await Options.PhoneNumberProvider.GetPhoneNumber(Consumer);
        }

        if (!hasPhone && phoneNumber != null)
        {
            if (!await this.AttachPhone(phoneNumber.Value, cancellationToken).ConfigureAwait(false))
                throw new AuthenticatorLinkerException(AuthenticatorLinkerError.CantAttachPhone);


            var emailValid = false;
            for (var i = 0; i < Options.EmailProvider!.RetryCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Options.EmailProvider!.ConfirmEmailLink(Consumer, EmailConfirmationType.AttachPhoneAuthenticator, cancellationToken).ConfigureAwait(false);
                emailValid = await this.CheckEmailConfirmation(cancellationToken).ConfigureAwait(false);
                if (emailValid) break;
            }


            if (!emailValid)
                throw new AuthenticatorLinkerException(AuthenticatorLinkerError.CantConfirmAttachingEmail);

            var sendSms = await this.SendSmsCode(cancellationToken).ConfigureAwait(false);
            if (sendSms != EResult.OK)
                throw new AuthenticatorLinkerException($"Can't send SMS code: {sendSms} ({(int)sendSms})");
        }

        var resp = await this.LinkRequest(cancellationToken).ConfigureAwait(false);

        var mobileData = resp.ToMobileDataExtended(DeviceId!, data.Clone());
        Options.BackupHandler?.Invoke(mobileData);

        if (resp.Status == 29)
        {
            throw new AuthenticatorLinkerException(AuthenticatorLinkerError.AuthenticatorPresent);
        }

        if (resp.Status != 1)
        {
            throw new AuthenticatorLinkerException(resp.Status);
        }

        string code;
        var isPhone = resp.ConfirmType is 1 or 2;
        if (isPhone)
        {
            if (Options.SmsCodeProvider == null)
                throw new AuthenticatorLinkerException(AuthenticatorLinkerError.NoSmsCodeProvider);

            var smsCode = await Options.SmsCodeProvider.GetSmsCode(Consumer, phoneNumber, resp.PhoneNumberHint).ConfigureAwait(false);
            code = smsCode.ToString("D5");
        }
        else if (resp.ConfirmType == 3)
        {
            if (Options.EmailProvider == null)
                throw new AuthenticatorLinkerException(AuthenticatorLinkerError.NoEmailProvider);

            code = await Options.EmailProvider.GetAddAuthenticatorCode(Consumer, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(resp.ConfirmType), resp.ConfirmType,
                $"ConfirmType {resp.ConfirmType} not supported");
        }

        var result = await this.FinalizeLink(code, resp.SharedSecret, isPhone, cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            var error = result.Error switch
            {
                LinkError.GeneralFailure => throw new AuthenticatorLinkerException(result.Code!.Value)
                { OnFinalization = true },
                LinkError.BadConfirmationCode => AuthenticatorLinkerError.BadConfirmationCode,
                LinkError.UnableToGenerateCorrectCodes => AuthenticatorLinkerError.UnableToGenerateCorrectCodes,
                _ => throw new ArgumentOutOfRangeException(nameof(result.Error), result.Error,
                    $"LinkError {result.Error} not supported")
            };

            throw new AuthenticatorLinkerException(error) { OnFinalization = true };
        }

        return mobileData;
    }
}
