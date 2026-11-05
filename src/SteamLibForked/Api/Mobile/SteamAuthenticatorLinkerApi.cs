using System.Text.Json;
using SteamLib.Authentication;
using SteamLib.Core;
using SteamLib.Web.Serialization;
using SteamLib.Core.StatusCodes;
using SteamLib.Exceptions.Authorization;
using SteamLib.Exceptions.Mobile;
using SteamLib.ProtoCore;
using SteamLib.ProtoCore.Enums;
using SteamLib.ProtoCore.Services;
using SteamLib.SteamMobile;
using SteamLib.SteamMobile.AuthenticatorLinker;
using SteamLib.SteamMobile.AuthenticatorLinker.Models;
using SteamLib.Abstractions;
using SteamLib.Models;
using SteamLib.Web;

namespace SteamLib.Api.Mobile;

public static class SteamAuthenticatorLinkerApi
{
    private static ISteamLinkerService _default = new SteamLib.Services.SteamLinkerService();

    /// <summary>
    /// Gets or sets the default <see cref="ISteamLinkerService"/> instance.
    /// </summary>
    public static ISteamLinkerService Default
    {
        get => _default;
        set => _default = value ?? throw new ArgumentNullException(nameof(value));
    }

    private static string GetAccessToken(this ISessionData s)
    {
        return s.GetToken(SteamDomain.Community)?.Token ??
               throw new SessionInvalidException("Access token was null. MobileEndpoints requires valid AccessToken");
    }

    public static class Routes
    {
        public const string HAS_PRONE = SteamConstants.STEAM_API + "/IPhoneService/AccountPhoneStatus/v1";
        public const string LINK_REQUEST = SteamConstants.STEAM_API + "/ITwoFactorService/AddAuthenticator/v1";
        public const string FINALIZE_LINK = SteamConstants.STEAM_API + "/ITwoFactorService/FinalizeAddAuthenticator/v1";

        public const string CHECK_EMAIL_CONFIRMATION =
            SteamConstants.STEAM_API + "/IPhoneService/IsAccountWaitingForEmailConfirmation/v1";

        public const string SEND_SMS_CODE = SteamConstants.STEAM_API + "/IPhoneService/SendPhoneVerificationCode/v1";
        public const string ATTACH_PHONE = SteamConstants.STEAM_API + "/IPhoneService/SetAccountPhoneNumber/v1";
        public const string VALIDATE_PHONE_NUMBER = SteamConstants.STEAM_STORE + "/phone/validate";
        public const string PRONE_AJAX = SteamConstants.STEAM_COMMUNITY + "/steamguard/phoneajax";

        public const string REMOVE_AUTHENTICATOR_VIA_CHALLENGE =
            SteamConstants.STEAM_API + "/ITwoFactorService/RemoveAuthenticatorViaChallengeStart/v1";

        public const string REMOVE_AUTHENTICATOR_VIA_CHALLENGE_CONTINUE = SteamConstants.STEAM_API + "/ITwoFactorService/RemoveAuthenticatorViaChallengeContinue/v1";
    }

    #region Global

    public static Task<AccountPhoneStatus_Response> HasPhone(HttpClient client, string accessToken,
        CancellationToken cancellationToken = default)
    {
        var reqUri = AddAccessToken(Routes.HAS_PRONE, accessToken);
        return client.PostProto<AccountPhoneStatus_Response>(reqUri, null, cancellationToken);
    }

    /// <summary>
    /// Sends a request to Steam to link a new authenticator to the account.
    /// </summary>
    public static async Task<AddAuthenticator_Response> LinkRequest(HttpClient client, ISessionData data,
        string deviceId, CancellationToken cancellationToken = default)
    {
        data.EnsureValidated();
        var reqUri = AddAccessToken(Routes.LINK_REQUEST, data.GetAccessToken());
        var req = new AddAuthenticator_Request
        {
            SteamId = (ulong)data.SteamId.Steam64.Id,
            AuthenticatorType = 1,
            DeviceIdentifier = deviceId,
            Version = 2
        };

        var resp = await client.PostProtoMsg<AddAuthenticator_Response>(reqUri, req, cancellationToken).ConfigureAwait(false);
        if (resp is { Result: EResult.InvalidState, ResponseMsg.Status: 2 })
        {
            throw new AuthenticatorLinkerException(AuthenticatorLinkerError.InvalidStateWithStatus2);
        }

        return resp.GetResponseEnsureSuccess();
    }

    public static async Task<LinkResult> FinalizeLink(HttpClient client, string confirmationCode, ISessionData data,
        byte[] sharedSecret, bool validateSmsCode, CancellationToken cancellationToken = default)
    {
        data.EnsureValidated();
        var reqUri = AddAccessToken(Routes.FINALIZE_LINK, data.GetAccessToken());
        var tries = 0;


        while (tries < 30)
        {
            cancellationToken.ThrowIfCancellationRequested();
            tries++;
            var time = await TimeAligner.GetSteamTimeAsync(cancellationToken).ConfigureAwait(false);
            var code = SteamGuardCodeGenerator.GenerateCode(sharedSecret, time);

            var req = new FinalizeAddAuthenticator_Request
            {
                SteamId = data.SteamId.Steam64.ToUlong(),
                AuthenticatorCode = code,
                AuthenticatorTime = (ulong)time,
                ConfirmationCode = confirmationCode,
                ValidateConfirmationCode = validateSmsCode
            };


            var resp = await client.PostProto<FinalizeAddAuthenticator_Response>(reqUri, req, cancellationToken).ConfigureAwait(false);

            if (resp is { Success: true, WantMore: false })
            {
                return new LinkResult();
            }

            if (resp.Status == 89)
            {
                return new LinkResult(LinkError.BadConfirmationCode);
            }


            if (resp.Status == 88 && tries >= 30)
            {
                return new LinkResult(LinkError.UnableToGenerateCorrectCodes);
            }
        }

        return new LinkResult(LinkError.GeneralFailure);
    }

    public static async Task<bool> IsValidPhoneNumber(HttpClient client, long phoneNumber, string sessionId,
        CancellationToken cancellationToken = default)
    {
        return await CheckPhoneNumber(client, phoneNumber, sessionId, cancellationToken).ConfigureAwait(false) == CheckPhoneResult.Valid;
    }

    public static async Task<CheckPhoneResult> CheckPhoneNumber(HttpClient client, long phoneNumber, string sessionId,
        CancellationToken cancellationToken = default)
    {
        var phone = '+' + phoneNumber.ToString();
        var data = new Dictionary<string, string>
        {
            {"phoneNumber", phone},
            {"sessionID", sessionId}
        };

        var reqContent = new FormUrlEncodedContent(data);
        var req = new HttpRequestMessage(HttpMethod.Post, Routes.VALIDATE_PHONE_NUMBER)
        {
            Content = reqContent
        };
        req.Headers.Referrer = new Uri("https://store.steampowered.com/phone/add");
        var resp = await client.SendAsync(req, cancellationToken).ConfigureAwait(false);
        var content = await resp.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var phoneResp = JsonSerializer.Deserialize(content, SteamJsonSerializerContext.Default.PhoneValidationResponse);

        if (phoneResp == null || !phoneResp.Success)
            return CheckPhoneResult.GeneralFailure;

        if (phoneResp.IsVoip)
            return CheckPhoneResult.VoIp;

        return phoneResp.IsValid ? CheckPhoneResult.Valid : CheckPhoneResult.NotValid;
    }

    public static async Task<bool> AttachPhone(HttpClient client, long phoneNumber, string accessToken,
        CancellationToken cancellationToken = default)
    {
        var phone = '+' + phoneNumber.ToString();
        var req = new SetAccountPhoneNumber_Request
        {
            PhoneNumber = phone
        };

        var reqUri = AddAccessToken(Routes.ATTACH_PHONE, accessToken);
        var resp =
            await client.PostProtoMsg<SetAccountPhoneNumber_Response>(reqUri, req, cancellationToken).ConfigureAwait(false);

        return resp.Result == EResult.Pending;
    }

    public static async Task<bool> CheckEmailConfirmation(HttpClient client, string accessToken,
        CancellationToken cancellationToken = default)
    {
        var reqUri = AddAccessToken(Routes.CHECK_EMAIL_CONFIRMATION, accessToken);

        var i = 0;
        while (i < 5)
        {
            cancellationToken.ThrowIfCancellationRequested();
            i++;
            var resp = await client.PostProto<IsAccountWaitingForEmailConfirmation_Response>(reqUri,
                new EmptyMessage(), cancellationToken).ConfigureAwait(false);

            if (!resp.IsWaiting) return true;

            await Task.Delay(resp.SecondsToWait * 1000, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    public static Task<EResult> SendSmsCode(HttpClient client, string accessToken,
        CancellationToken cancellationToken = default)
    {
        var reqUri = AddAccessToken(Routes.SEND_SMS_CODE, accessToken);
        return client.PostProto(reqUri, new SendPhoneVerificationCode_Request(), cancellationToken);
    }

    public static async Task<bool> CheckSmsCode(HttpClient client, int smsCode, string sessionId,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, string>
        {
            {"op", "check_sms_code"},
            {"arg", smsCode.ToString()},
            {"checkfortos", "0"},
            {"skipvoip", "1"},
            {"sessionid", sessionId}
        };

        var content = new FormUrlEncodedContent(data);
        var resp = await client.PostAsync(Routes.PRONE_AJAX, content, cancellationToken).ConfigureAwait(false);
        var respContent = await resp.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#pragma warning disable CS0618
        var success = SteamStatusCode.Translate(respContent).Equals(SteamStatusCode.Ok);
#pragma warning restore CS0618

        if (success) return true;

        await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
        var hasPhone = await HasPhone(client, sessionId, cancellationToken).ConfigureAwait(false);
        return hasPhone.HasPhone;
    }

    public static Task RemoveAuthenticatorViaChallengeStart(HttpClient client, string accessToken,
        CancellationToken cancellationToken = default)
    {
        var reqUri = AddAccessToken(Routes.REMOVE_AUTHENTICATOR_VIA_CHALLENGE, accessToken);
        return client.PostProtoEnsureSuccess(reqUri, EmptyMessage.Instance, cancellationToken);
    }

    public static Task<RemoveAuthenticatorViaChallengeContinue_Response> RemoveAuthenticatorViaChallengeContinue(
        HttpClient client, string accessToken, string smsCode, CancellationToken cancellationToken = default)
    {
        var req = new RemoveAuthenticatorViaChallengeContinue_Request
        {
            SmsCode = smsCode,
            GenerateNewToken = true,
            Version = 2
        };
        var reqUri = AddAccessToken(Routes.REMOVE_AUTHENTICATOR_VIA_CHALLENGE_CONTINUE, accessToken);
        return client.PostProto<RemoveAuthenticatorViaChallengeContinue_Response>(reqUri, req, cancellationToken);
    }

    public static Task<AccountPhoneStatus_Response> HasPhone(HttpClient client, SteamSessionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return HasPhone(client, context.EnsureAccessToken(), cancellationToken);
    }

    public static Task<bool> CheckEmailConfirmation(HttpClient client, SteamSessionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return CheckEmailConfirmation(client, context.EnsureAccessToken(), cancellationToken);
    }

    public static Task<EResult> SendSmsCode(HttpClient client, SteamSessionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return SendSmsCode(client, context.EnsureAccessToken(), cancellationToken);
    }

    public static Task<bool> CheckSmsCode(HttpClient client, SteamSessionContext context, int smsCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return CheckSmsCode(client, smsCode, context.EnsureSessionId(), cancellationToken);
    }

    public static Task RemoveAuthenticatorViaChallengeStart(HttpClient client, SteamSessionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return RemoveAuthenticatorViaChallengeStart(client, context.EnsureAccessToken(), cancellationToken);
    }

    public static Task<RemoveAuthenticatorViaChallengeContinue_Response> RemoveAuthenticatorViaChallengeContinue(
        HttpClient client, SteamSessionContext context, string smsCode, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return RemoveAuthenticatorViaChallengeContinue(client, context.EnsureAccessToken(), smsCode, cancellationToken);
    }

    public static string GenerateDeviceId()
    {
        return "android:" + Guid.NewGuid();
    }

    private static string AddAccessToken(string uri, string accessToken)
    {
        return uri + "?access_token=" + accessToken;
    }

    #endregion

    #region ForLinker

    internal static Task<AddAuthenticator_Response> LinkRequest(this SteamAuthenticatorLinker linker,
        CancellationToken cancellationToken = default)
    {
        if (linker.SessionData == null)
            throw new InvalidOperationException("SessionData is null");

        return LinkRequest(linker.Client, linker.SessionData, linker.DeviceId ??= GenerateDeviceId(), cancellationToken);
    }

    internal static Task<bool> IsValidPhoneNumber(this SteamAuthenticatorLinker linker, long phoneNumber,
        CancellationToken cancellationToken = default)
    {
        if (linker.SessionData == null)
            throw new InvalidOperationException("SessionData is null");

        return IsValidPhoneNumber(linker.Client, phoneNumber, linker.SessionData.SessionId, cancellationToken);
    }

    internal static Task<AccountPhoneStatus_Response> HasPhone(this SteamAuthenticatorLinker linker,
        CancellationToken cancellationToken = default)
    {
        if (linker.SessionData == null)
            throw new InvalidOperationException("SessionData is null");

        return HasPhone(linker.Client, linker.SessionData.GetAccessToken(), cancellationToken);
    }

    internal static Task<bool> AttachPhone(this SteamAuthenticatorLinker linker, long phoneNumber,
        CancellationToken cancellationToken = default)
    {
        if (linker.SessionData == null)
            throw new InvalidOperationException("SessionData is null");

        return AttachPhone(linker.Client, phoneNumber, linker.SessionData.GetAccessToken(), cancellationToken);
    }

    internal static Task<bool> CheckEmailConfirmation(this SteamAuthenticatorLinker linker,
        CancellationToken cancellationToken = default)
    {
        if (linker.SessionData == null)
            throw new InvalidOperationException("SessionData is null");

        return CheckEmailConfirmation(linker.Client, linker.SessionData.GetAccessToken(), cancellationToken);
    }

    internal static Task<EResult> SendSmsCode(this SteamAuthenticatorLinker linker,
        CancellationToken cancellationToken = default)
    {
        if (linker.SessionData == null)
            throw new InvalidOperationException("SessionData is null");

        return SendSmsCode(linker.Client, linker.SessionData.GetAccessToken(), cancellationToken);
    }

    internal static Task<bool> CheckSmsCode(this SteamAuthenticatorLinker linker, int smsCode,
        CancellationToken cancellationToken = default)
    {
        if (linker.SessionData == null)
            throw new InvalidOperationException("SessionData is null");

        return CheckSmsCode(linker.Client, smsCode, linker.SessionData.SessionId, cancellationToken);
    }

    internal static Task<LinkResult> FinalizeLink(this SteamAuthenticatorLinker linker, string confirmationCode,
        byte[] sharedSecret, bool validateSmsCode, CancellationToken cancellationToken = default)
    {
        if (linker.SessionData == null)
            throw new InvalidOperationException("SessionData is null");

        return FinalizeLink(linker.Client, confirmationCode, linker.SessionData, sharedSecret, validateSmsCode, cancellationToken);
    }

    #endregion
}