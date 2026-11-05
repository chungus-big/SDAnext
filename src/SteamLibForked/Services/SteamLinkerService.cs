using SteamLib.Abstractions;
using SteamLib.Api.Mobile;
using SteamLib.Models.Session;
using SteamLib.ProtoCore.Enums;
using SteamLib.ProtoCore.Services;
using SteamLib.SteamMobile.AuthenticatorLinker;
using SteamLib.SteamMobile.AuthenticatorLinker.Models;
using SteamLib.Web;

namespace SteamLib.Services;

/// <summary>
/// Default implementation of <see cref="ISteamLinkerService"/> for linking/unlinking mobile authenticators.
/// </summary>
public class SteamLinkerService : ISteamLinkerService
{
    public Task<MobileDataExtended> LinkAccountAsync(LinkOptions options, MobileSessionData data,
        CancellationToken cancellationToken = default)
    {
        return new SteamAuthenticatorLinker(options).LinkAccount(data, cancellationToken);
    }

    public Task<AccountPhoneStatus_Response> HasPhoneAsync(HttpClient client, string accessToken,
        CancellationToken cancellationToken = default)
    {
        return SteamAuthenticatorLinkerApi.HasPhone(client, accessToken, cancellationToken);
    }

    public Task<AccountPhoneStatus_Response> HasPhoneAsync(HttpClient client, SteamSessionContext context,
        CancellationToken cancellationToken = default)
    {
        return SteamAuthenticatorLinkerApi.HasPhone(client, context, cancellationToken);
    }

    public Task<AddAuthenticator_Response> LinkRequestAsync(HttpClient client, ISessionData data, string deviceId,
        CancellationToken cancellationToken = default)
    {
        return SteamAuthenticatorLinkerApi.LinkRequest(client, data, deviceId, cancellationToken);
    }

    public Task<LinkResult> FinalizeLinkAsync(HttpClient client, string confirmationCode, ISessionData data,
        byte[] sharedSecret, bool validateSmsCode, CancellationToken cancellationToken = default)
    {
        return SteamAuthenticatorLinkerApi.FinalizeLink(client, confirmationCode, data, sharedSecret, validateSmsCode, cancellationToken);
    }

    public Task<bool> CheckEmailConfirmationAsync(HttpClient client, string accessToken,
        CancellationToken cancellationToken = default)
    {
        return SteamAuthenticatorLinkerApi.CheckEmailConfirmation(client, accessToken, cancellationToken);
    }

    public Task<bool> CheckEmailConfirmationAsync(HttpClient client, SteamSessionContext context,
        CancellationToken cancellationToken = default)
    {
        return SteamAuthenticatorLinkerApi.CheckEmailConfirmation(client, context, cancellationToken);
    }

    public Task<EResult> SendSmsCodeAsync(HttpClient client, string accessToken,
        CancellationToken cancellationToken = default)
    {
        return SteamAuthenticatorLinkerApi.SendSmsCode(client, accessToken, cancellationToken);
    }

    public Task<EResult> SendSmsCodeAsync(HttpClient client, SteamSessionContext context,
        CancellationToken cancellationToken = default)
    {
        return SteamAuthenticatorLinkerApi.SendSmsCode(client, context, cancellationToken);
    }

    public Task<bool> CheckSmsCodeAsync(HttpClient client, int smsCode, string sessionId,
        CancellationToken cancellationToken = default)
    {
        return SteamAuthenticatorLinkerApi.CheckSmsCode(client, smsCode, sessionId, cancellationToken);
    }

    public Task<bool> CheckSmsCodeAsync(HttpClient client, SteamSessionContext context, int smsCode,
        CancellationToken cancellationToken = default)
    {
        return SteamAuthenticatorLinkerApi.CheckSmsCode(client, context, smsCode, cancellationToken);
    }

    public Task<bool> AttachPhoneAsync(HttpClient client, long phoneNumber, string accessToken,
        CancellationToken cancellationToken = default)
    {
        return SteamAuthenticatorLinkerApi.AttachPhone(client, phoneNumber, accessToken, cancellationToken);
    }

    public Task RemoveAuthenticatorViaChallengeStartAsync(HttpClient client, string accessToken,
        CancellationToken cancellationToken = default)
    {
        return SteamAuthenticatorLinkerApi.RemoveAuthenticatorViaChallengeStart(client, accessToken, cancellationToken);
    }

    public Task RemoveAuthenticatorViaChallengeStartAsync(HttpClient client, SteamSessionContext context,
        CancellationToken cancellationToken = default)
    {
        return SteamAuthenticatorLinkerApi.RemoveAuthenticatorViaChallengeStart(client, context, cancellationToken);
    }

    public Task<RemoveAuthenticatorViaChallengeContinue_Response> RemoveAuthenticatorViaChallengeContinueAsync(
        HttpClient client, string accessToken, string smsCode, CancellationToken cancellationToken = default)
    {
        return SteamAuthenticatorLinkerApi.RemoveAuthenticatorViaChallengeContinue(client, accessToken, smsCode, cancellationToken);
    }

    public Task<RemoveAuthenticatorViaChallengeContinue_Response> RemoveAuthenticatorViaChallengeContinueAsync(
        HttpClient client, SteamSessionContext context, string smsCode, CancellationToken cancellationToken = default)
    {
        return SteamAuthenticatorLinkerApi.RemoveAuthenticatorViaChallengeContinue(client, context, smsCode, cancellationToken);
    }

    public Task<RemoveAuthenticator_Response> RemoveAuthenticatorAsync(HttpClient client, string accessToken,
        string revocationCode, CancellationToken cancellationToken = default)
    {
        return SteamMobileApi.RemoveAuthenticator(client, accessToken, revocationCode, cancellationToken);
    }
}
