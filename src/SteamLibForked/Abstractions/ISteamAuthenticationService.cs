using SteamLib.Authentication;
using SteamLib.Authentication.LoginV2;
using SteamLib.ProtoCore.Services;
using SteamLib.Web;

namespace SteamLib.Abstractions;

/// <summary>
/// Service for Steam authentication, login flow execution, status polling, and token refresh.
/// </summary>
public interface ISteamAuthenticationService
{
    /// <summary>
    /// Executes the Steam Login V2 flow for the specified credentials.
    /// </summary>
    Task<ISessionData> DoLoginAsync(LoginV2ExecutorOptions options, string username, string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the Steam Login V2 flow returning a typed <see cref="AuthResult"/>.
    /// </summary>
    Task<AuthResult> AuthenticateAsync(LoginV2ExecutorOptions options, string username, string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the mobile JWT access token using the refresh token.
    /// </summary>
    Task<string> RefreshJwtAsync(HttpClient client, string refreshToken, SteamId steamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the mobile JWT access token using the session context.
    /// </summary>
    Task<string> RefreshJwtAsync(SteamSessionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the RSA public key and timestamp for encrypting password.
    /// </summary>
    Task<GetPasswordRSAPublicKey_Response> GetPasswordRSAPublicKeyAsync(HttpClient client,
        GetPasswordRSAPublicKey_Request request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates an authentication session using encrypted credentials.
    /// </summary>
    Task<BeginAuthSessionViaCredentials_Response> BeginAuthSessionViaCredentialsAsync(HttpClient client,
        BeginAuthSessionViaCredentials_Request request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Polls the status of an ongoing authentication session.
    /// </summary>
    Task<PollAuthSessionStatus_Response> PollAuthSessionStatusAsync(HttpClient client,
        PollAuthSessionStatus_Request request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the list of active authentication sessions for the account.
    /// </summary>
    Task<GetAuthSessionsForAccount_Response> GetAuthSessionsForAccountAsync(HttpClient client,
        string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves details for a specific authentication session.
    /// </summary>
    Task<GetAuthSessionInfo_Response> GetAuthSessionInfoAsync(HttpClient client,
        string accessToken, ulong clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an auth session by submitting a Steam Guard (TOTP/email) code.
    /// </summary>
    Task UpdateAuthSessionWithSteamGuardCodeAsync(HttpClient client,
        UpdateAuthSessionWithSteamGuardCode_Request request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an auth session by approving mobile confirmation.
    /// </summary>
    Task UpdateAuthSessionWithMobileConfirmationAsync(HttpClient client, string accessToken,
        UpdateAuthSessionWithMobileConfirmation_Request request, CancellationToken cancellationToken = default);
}
