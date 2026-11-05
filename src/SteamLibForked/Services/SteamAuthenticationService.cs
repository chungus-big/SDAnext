using SteamLib.Abstractions;
using SteamLib.Api.Mobile;
using SteamLib.Api.Services;
using SteamLib.Authentication;
using SteamLib.Authentication.LoginV2;
using SteamLib.ProtoCore.Services;
using SteamLib.Web;

namespace SteamLib.Services;

/// <summary>
/// Default implementation of <see cref="ISteamAuthenticationService"/> for Steam Login, status polling, and token refreshing.
/// </summary>
public class SteamAuthenticationService : ISteamAuthenticationService
{
    public Task<ISessionData> DoLoginAsync(LoginV2ExecutorOptions options, string username, string password,
        CancellationToken cancellationToken = default)
    {
        return LoginV2Executor.DoLogin(options, username, password, cancellationToken);
    }

    public Task<AuthResult> AuthenticateAsync(LoginV2ExecutorOptions options, string username, string password,
        CancellationToken cancellationToken = default)
    {
        return LoginV2Executor.AuthenticateAsync(options, username, password, cancellationToken);
    }

    public Task<string> RefreshJwtAsync(HttpClient client, string refreshToken, SteamId steamId,
        CancellationToken cancellationToken = default)
    {
        return SteamMobileApi.RefreshJwt(client, refreshToken, steamId, cancellationToken);
    }

    public async Task<string> RefreshJwtAsync(SteamSessionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.RefreshToken == null)
            throw new InvalidOperationException("RefreshToken is missing in SteamSessionContext");
        if (context.SteamId == null)
            throw new InvalidOperationException("SteamId is required in SteamSessionContext");

        var client = context.GetOrCreateClient();
        var refreshed = await RefreshJwtAsync(client, context.RefreshToken.Value.Token, context.SteamId.Value, cancellationToken)
            .ConfigureAwait(false);
        context.AccessToken = refreshed;
        return refreshed;
    }

    public Task<GetPasswordRSAPublicKey_Response> GetPasswordRSAPublicKeyAsync(HttpClient client,
        GetPasswordRSAPublicKey_Request request, CancellationToken cancellationToken = default)
    {
        return AuthenticationServiceApi.GetPasswordRSAPublicKey(client, request, cancellationToken);
    }

    public Task<BeginAuthSessionViaCredentials_Response> BeginAuthSessionViaCredentialsAsync(HttpClient client,
        BeginAuthSessionViaCredentials_Request request, CancellationToken cancellationToken = default)
    {
        return AuthenticationServiceApi.BeginAuthSessionViaCredentials(client, request, cancellationToken);
    }

    public Task<PollAuthSessionStatus_Response> PollAuthSessionStatusAsync(HttpClient client,
        PollAuthSessionStatus_Request request, CancellationToken cancellationToken = default)
    {
        return AuthenticationServiceApi.PollAuthSessionStatus(client, request, cancellationToken);
    }

    public Task<GetAuthSessionsForAccount_Response> GetAuthSessionsForAccountAsync(HttpClient client,
        string accessToken, CancellationToken cancellationToken = default)
    {
        return AuthenticationServiceApi.GetAuthSessionsForAccount(client, accessToken, cancellationToken);
    }

    public Task<GetAuthSessionInfo_Response> GetAuthSessionInfoAsync(HttpClient client,
        string accessToken, ulong clientId, CancellationToken cancellationToken = default)
    {
        return AuthenticationServiceApi.GetAuthSessionInfo(client, accessToken, clientId, cancellationToken);
    }

    public Task UpdateAuthSessionWithSteamGuardCodeAsync(HttpClient client,
        UpdateAuthSessionWithSteamGuardCode_Request request, CancellationToken cancellationToken = default)
    {
        return AuthenticationServiceApi.UpdateAuthSessionWithSteamGuardCode(client, request, cancellationToken);
    }

    public Task UpdateAuthSessionWithMobileConfirmationAsync(HttpClient client, string accessToken,
        UpdateAuthSessionWithMobileConfirmation_Request request, CancellationToken cancellationToken = default)
    {
        return AuthenticationServiceApi.UpdateAuthSessionWithMobileConfirmation(client, accessToken, request, cancellationToken);
    }
}
