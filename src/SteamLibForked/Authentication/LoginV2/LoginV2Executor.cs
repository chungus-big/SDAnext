using System.Text.Json;
using SteamLib.Web;
using SteamLib.Abstractions;
using SteamLib.Abstractions.Auth;
using SteamLib.Web.Serialization;
using SteamLib.Api;
using SteamLib.Api.Services;
using SteamLib.Core;
using SteamLib.Core.StatusCodes;
using SteamLib.Exceptions;
using SteamLib.Exceptions.Authorization;
using SteamLib.Models;
using SteamLib.Models.Session;
using SteamLib.ProtoCore.Enums;
using SteamLib.ProtoCore.Services;
using SteamLib.Utility;

namespace SteamLib.Authentication.LoginV2;

/// <summary>
/// Executes Steam Login V2 authentication flow including credentials submission,
/// 2FA / Steam Guard challenges, and session finalization.
/// </summary>
public class LoginV2Executor
{
    private ILoginConsumer Caller { get; }
    private HttpClient HttpClient { get; }
    private string WebsiteId { get; }
    private DeviceDetails DeviceDetails { get; }

    private LoginV2Executor(LoginV2ExecutorOptions options)
    {
        Caller = options.Consumer;
        HttpClient = options.HttpClient;
        WebsiteId = options.WebsiteId ?? throw new NullReferenceException("WebsiteId was null");
        DeviceDetails = options.DeviceDetails ?? throw new NullReferenceException("DeviceDetails was null");
    }


    /// <summary>
    ///     Do log in on <see href="https://steamcommunity.com/">SteamCommunity</see>.<br />
    ///     Some functions require proper SessionId. But <see cref="SessionData" /> contains only SteamCommunity related
    ///     SessionId and some functions on other services may not work
    /// </summary>
    /// <param name="options"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="cancellationToken"></param>
    /// <summary>
    ///     Do log in on <see href="https://steamcommunity.com/">SteamCommunity</see>.<br />
    ///     Some functions require proper SessionId. But <see cref="SessionData" /> contains only SteamCommunity related
    ///     SessionId and some functions on other services may not work
    /// </summary>
    /// <param name="options"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="SessionData" /> or <see cref="MobileSessionData" /> depending on which token type is returned</returns>
    /// <exception cref="LoginException"></exception>
    /// <exception cref="SteamStatusCodeException"></exception>
    /// <exception cref="UnsupportedAuthTypeException"></exception>
    public static async Task<ISessionData> DoLogin(LoginV2ExecutorOptions options, string username, string password,
        CancellationToken cancellationToken = default)
    {
        var authResult = await AuthenticateAsync(options, username, password, cancellationToken).ConfigureAwait(false);
        return authResult switch
        {
            AuthResult.Success success => success.Session,
            AuthResult.InvalidCredentials => throw new LoginException(LoginError.InvalidCredentials),
            AuthResult.NeedDeviceCode => throw new UnsupportedAuthTypeException([EAuthSessionGuardType.DeviceCode]),
            AuthResult.NeedEmailCode => throw new UnsupportedAuthTypeException([EAuthSessionGuardType.EmailCode]),
            AuthResult.RateLimited => throw new SteamStatusCodeException(SteamStatusCode.RateLimitExceeded),
            AuthResult.Failed failed when failed.Exception is LoginException le => throw le,
            AuthResult.Failed failed when failed.Exception is SteamStatusCodeException sse => throw sse,
            AuthResult.Failed failed when failed.Exception is UnsupportedAuthTypeException uae => throw uae,
            AuthResult.Failed failed when failed.Exception != null => throw failed.Exception,
            AuthResult.Failed failed => throw new LoginException(failed.Error),
            _ => throw new LoginException("Login failed")
        };
    }

    /// <summary>
    /// Executes the Steam Login V2 authentication flow without throwing exceptions for expected auth outcomes,
    /// returning a typed <see cref="AuthResult"/>.
    /// </summary>
    public static async Task<AuthResult> AuthenticateAsync(LoginV2ExecutorOptions options, string username, string password,
        CancellationToken cancellationToken = default)
    {
        var executor = new LoginV2Executor(options);
        var client = executor.HttpClient;

        string sessionId;
        try
        {
            var header = await SteamGlobalApi.GetSessionIdFromLoginPage(client, cancellationToken).ConfigureAwait(false);
            sessionId = header.SessionId;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new AuthResult.Failed("Failed to get session ID from Steam login page", ex);
        }

        GetPasswordRSAPublicKey_Response rsaResp;
        try
        {
            var rsgMsg = new GetPasswordRSAPublicKey_Request
            {
                AccountName = username
            };
            rsaResp = await AuthenticationServiceApi.GetPasswordRSAPublicKey(client, rsgMsg, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            return new AuthResult.RateLimited();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new AuthResult.Failed("Failed to get RSA public key", ex);
        }

        var encodedPassword =
            EncryptionHelper.ToBase64EncryptedPassword(rsaResp.PublicKeyExp, rsaResp.PublicKeyMod, password);

        var beginAuthMsg = new BeginAuthSessionViaCredentials_Request
        {
            DeviceFriendlyName = string.Empty,
            AccountName = username,
            EncryptedPassword = encodedPassword,
            EncryptionTimestamp = rsaResp.Timestamp,
            RememberLogin = true,
            PlatformType = executor.DeviceDetails.PlatformType,
            Persistence = 1,
            WebsiteId = executor.WebsiteId,
            DeviceDetails = executor.DeviceDetails
        };

        BeginAuthSessionViaCredentials_Response beginAuthResp;
        try
        {
            beginAuthResp =
                await AuthenticationServiceApi.BeginAuthSessionViaCredentials(client, beginAuthMsg, cancellationToken).ConfigureAwait(false);
        }
        catch (SteamStatusCodeException ex)
            when (ex.StatusCode == SteamStatusCode.InvalidPassword)
        {
            return new AuthResult.InvalidCredentials();
        }
        catch (SteamStatusCodeException ex)
            when (ex.StatusCode == SteamStatusCode.RateLimitExceeded)
        {
            return new AuthResult.RateLimited();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            return new AuthResult.RateLimited();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new AuthResult.Failed(ex.Message, ex);
        }

        var clientId = beginAuthResp.ClientId;
        var steamId = beginAuthResp.Steamid;

        PollAuthSessionStatus_Response? pollResp = null;
        for (var i = 0; i < 3; i++)
        {
            if (beginAuthResp.AllowedConfirmations.Count > 0 &&
                beginAuthResp.AllowedConfirmations.All(a => a.ConfirmationType != EAuthSessionGuardType.None))
            {
                var t = SelectProvider(options, beginAuthResp.AllowedConfirmations);
                if (!t.HasValue)
                {
                    var devGuard = beginAuthResp.AllowedConfirmations.FirstOrDefault(a => a.ConfirmationType == EAuthSessionGuardType.DeviceCode);
                    if (devGuard != null)
                    {
                        return new AuthResult.NeedDeviceCode(clientId, SteamId.FromSteam64(steamId));
                    }

                    var emailGuard = beginAuthResp.AllowedConfirmations.FirstOrDefault(a => a.ConfirmationType == EAuthSessionGuardType.EmailCode);
                    if (emailGuard != null)
                    {
                        return new AuthResult.NeedEmailCode(clientId, SteamId.FromSteam64(steamId), emailGuard.AssociatedMessage ?? string.Empty);
                    }

                    return new AuthResult.Failed(
                        $"Unsupported confirmation type: {string.Join(", ", beginAuthResp.AllowedConfirmations.Select(a => a.ConfirmationType))}");
                }

                var provider = t.Value.Item1;
                var guardType = t.Value.Item2;
                var model = new UpdateAuthSessionModel(guardType, clientId, steamId);
                try
                {
                    await UpdateSessionAndMapException(client, executor.Caller, provider, model, cancellationToken).ConfigureAwait(false);
                }
                catch (LoginException ex)
                {
                    return new AuthResult.Failed(ex.Message, ex);
                }
            }

            var pollSessionMsg = new PollAuthSessionStatus_Request
            {
                ClientId = clientId,
                RequestId = beginAuthResp.RequestId
            };

            try
            {
                pollResp = await AuthenticationServiceApi.PollAuthSessionStatus(client, pollSessionMsg, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(pollResp.AccessToken) && !string.IsNullOrEmpty(pollResp.RefreshToken))
                {
                    break;
                }
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
            }
        }

        if (pollResp == null || string.IsNullOrEmpty(pollResp.RefreshToken) || string.IsNullOrEmpty(pollResp.AccessToken))
        {
            return new AuthResult.Failed("Session polling failed: missing refresh or access token");
        }

        SteamAuthToken refreshToken;
        try
        {
            refreshToken = SteamTokenHelper.Parse(pollResp.RefreshToken);
            if (refreshToken.Type is not (SteamAccessTokenType.Refresh or SteamAccessTokenType.MobileRefresh))
                return new AuthResult.Failed("Refresh token must be of type Refresh or MobileRefresh. No 'renew' audience found in JWT.");
        }
        catch (ArgumentException ex)
        {
            return new AuthResult.Failed("Steam returned invalid refresh token or it's schema was unsupported.", ex);
        }

        var data = new Dictionary<string, string>
        {
            {"nonce", pollResp.RefreshToken},
            {"sessionid", sessionId}
        };

        try
        {
            var finalize = await client.PostAsync("https://login.steampowered.com/jwt/finalizelogin",
                new FormUrlEncodedContent(data), cancellationToken).ConfigureAwait(false);
            var finalizeContent = await finalize.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            var finalizeResp =
                JsonSerializer.Deserialize(finalizeContent, SteamJsonSerializerContext.Default.FinalizeLoginJson)!;

            List<SteamAuthToken> tokens = [];
            foreach (var transferInfo in finalizeResp.TransferInfo)
            {
                var transferData = new Dictionary<string, string>
                {
                    {"nonce", transferInfo.TransferInfoParams.Nonce},
                    {"auth", transferInfo.TransferInfoParams.Auth},
                    {"steamID", steamId.ToString()}
                };

                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Post, transferInfo.Url)
                    {
                        Content = new FormUrlEncodedContent(transferData),
                        Headers = { Referrer = SteamDomains.GetDomainUri(SteamDomain.Store) }
                    };
                    var transferResp = await client.SendAsync(req, cancellationToken).ConfigureAwait(false);
                    var transferContent = await transferResp.ReadAsStringEnsureSuccessAsync(cancellationToken).ConfigureAwait(false);
                    var status = JsonSerializer.Deserialize(transferContent, SteamJsonSerializerContext.Default.TransferResult);
                    var result = status?.Result;
                    if (result != null)
                        SteamStatusCode.ValidateSuccessOrThrow(result.Value);

                    var tokenStr = SteamTokenHelper.ExtractJwtFromSetCookiesHeader(transferResp.Headers);
                    var token = SteamTokenHelper.Parse(tokenStr);
                    tokens.Add(token);
                }
                catch
                {
                }
            }

            var accessToken = SteamTokenHelper.Parse(pollResp.AccessToken);

            if (accessToken.Type == SteamAccessTokenType.Mobile)
            {
                return new AuthResult.Success(new MobileSessionData(sessionId, SteamId.FromSteam64(steamId), refreshToken, accessToken, tokens));
            }

            return new AuthResult.Success(new SessionData(sessionId, SteamId.FromSteam64(steamId), refreshToken, tokens));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new AuthResult.Failed($"Error while finalizing login: {ex.Message}", ex);
        }
    }

    private static async Task UpdateSessionAndMapException(HttpClient client, ILoginConsumer consumer,
        IAuthProvider provider, UpdateAuthSessionModel model, CancellationToken cancellationToken = default)
    {
        //Also DuplicateRequest means invalid steamId/clientId in MobileConf
        try
        {
            await provider.UpdateAuthSession(client, consumer, model, cancellationToken);
        }
        catch (SteamStatusCodeException ex)
            when (IsSupported(ex.StatusCode))
        {
            var loginError = Map(ex.StatusCode);
            throw new LoginException(loginError, ex);
        }

        return;

        bool IsSupported(SteamStatusCode code)
        {
            return code == SteamStatusCode.InvalidLoginAuthCode ||
                   code == SteamStatusCode.InvalidSignature ||
                   code == SteamStatusCode.TwoFactorCodeMismatch;
        }

        LoginError Map(SteamStatusCode code)
        {
            if (code == SteamStatusCode.InvalidLoginAuthCode)
            {
                return LoginError.InvalidEmailAuthCode;
            }

            if (code == SteamStatusCode.InvalidSignature)
            {
                return LoginError.InvalidSharedSecret;
            }

            if (code == SteamStatusCode.TwoFactorCodeMismatch)
            {
                return LoginError.InvalidTwoFactorCode;
            }

            return LoginError.UndefinedError;
        }
    }

    private static (IAuthProvider, EAuthSessionGuardType)? SelectProvider(LoginV2ExecutorOptions options,
        List<AllowedConfirmationMsg> allowed)
    {
        foreach (var guardType in options.PreferredGuardTypes)
        {
            var availableProvider =
                options.AuthProviders.FirstOrDefault(a => a.IsSupportedGuardType(options.Consumer, guardType));
            if (availableProvider != null)
            {
                return (availableProvider, guardType);
            }
        }

        foreach (var allowedConfirmation in allowed)
        {
            var availableProvider = options.AuthProviders.FirstOrDefault(a =>
                a.IsSupportedGuardType(options.Consumer, allowedConfirmation.ConfirmationType));
            if (availableProvider != null)
            {
                return (availableProvider, allowedConfirmation.ConfirmationType);
            }
        }

        return null;
    }
}