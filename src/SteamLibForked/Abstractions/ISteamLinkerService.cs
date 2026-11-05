using SteamLib.Models.Session;
using SteamLib.ProtoCore.Enums;
using SteamLib.ProtoCore.Services;
using SteamLib.SteamMobile.AuthenticatorLinker.Models;
using SteamLib.Web;

namespace SteamLib.Abstractions;

/// <summary>
/// Service for linking/unlinking Steam Guard Mobile Authenticator, phone attachment, and SMS verification.
/// </summary>
public interface ISteamLinkerService
{
    /// <summary>
    /// Executes the full multi-step authenticator linking flow.
    /// </summary>
    Task<MobileDataExtended> LinkAccountAsync(LinkOptions options, MobileSessionData data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the account has a verified phone number attached.
    /// </summary>
    Task<AccountPhoneStatus_Response> HasPhoneAsync(HttpClient client, string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the account has a verified phone number attached using session context.
    /// </summary>
    Task<AccountPhoneStatus_Response> HasPhoneAsync(HttpClient client, SteamSessionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the initial authenticator linking request to Steam.
    /// </summary>
    Task<AddAuthenticator_Response> LinkRequestAsync(HttpClient client, ISessionData data, string deviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalizes the authenticator linking process by verifying the SMS code and generated TOTP code.
    /// </summary>
    Task<LinkResult> FinalizeLinkAsync(HttpClient client, string confirmationCode, ISessionData data,
        byte[] sharedSecret, bool validateSmsCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the user has confirmed the email link confirmation.
    /// </summary>
    Task<bool> CheckEmailConfirmationAsync(HttpClient client, string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the user has confirmed the email link confirmation using session context.
    /// </summary>
    Task<bool> CheckEmailConfirmationAsync(HttpClient client, SteamSessionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests Steam to send an SMS verification code to the attached phone number.
    /// </summary>
    Task<EResult> SendSmsCodeAsync(HttpClient client, string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests Steam to send an SMS verification code to the attached phone number using session context.
    /// </summary>
    Task<EResult> SendSmsCodeAsync(HttpClient client, SteamSessionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the received SMS verification code.
    /// </summary>
    Task<bool> CheckSmsCodeAsync(HttpClient client, int smsCode, string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the received SMS verification code using session context.
    /// </summary>
    Task<bool> CheckSmsCodeAsync(HttpClient client, SteamSessionContext context, int smsCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attaches a new phone number to the account.
    /// </summary>
    Task<bool> AttachPhoneAsync(HttpClient client, long phoneNumber, string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates authenticator removal via SMS challenge.
    /// </summary>
    Task RemoveAuthenticatorViaChallengeStartAsync(HttpClient client, string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates authenticator removal via SMS challenge using session context.
    /// </summary>
    Task RemoveAuthenticatorViaChallengeStartAsync(HttpClient client, SteamSessionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes authenticator removal via SMS challenge code.
    /// </summary>
    Task<RemoveAuthenticatorViaChallengeContinue_Response> RemoveAuthenticatorViaChallengeContinueAsync(
        HttpClient client, string accessToken, string smsCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes authenticator removal via SMS challenge code using session context.
    /// </summary>
    Task<RemoveAuthenticatorViaChallengeContinue_Response> RemoveAuthenticatorViaChallengeContinueAsync(
        HttpClient client, SteamSessionContext context, string smsCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the authenticator using revocation code (R-code).
    /// </summary>
    Task<RemoveAuthenticator_Response> RemoveAuthenticatorAsync(HttpClient client, string accessToken,
        string revocationCode, CancellationToken cancellationToken = default);
}
