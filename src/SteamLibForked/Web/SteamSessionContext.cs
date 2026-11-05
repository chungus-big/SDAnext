using System.Net;
using SteamLib.Abstractions;
using SteamLib.Api.Mobile;
using SteamLib.Authentication;
using SteamLib.Core;
using SteamLib.Exceptions.Authorization;
using SteamLib.Models;
using SteamLib.Models.Session;

namespace SteamLib.Web;

/// <summary>
/// Encapsulates authentication session state including <see cref="CookieContainer"/>,
/// <see cref="SteamId"/>, access tokens, refresh tokens, session ID, and mobile secrets
/// into a unified object passed to network operations.
/// </summary>
public class SteamSessionContext
{
    public SteamId? SteamId { get; set; }
    public string? SessionId { get; set; }
    public string? AccessToken { get; set; }
    public SteamAuthToken? RefreshToken { get; set; }
    public CookieContainer CookieContainer { get; set; }
    public string? DeviceId { get; set; }
    public string? IdentitySecret { get; set; }
    public string? SharedSecret { get; set; }
    public HttpClient? Client { get; set; }

    /// <summary>
    /// Gets the existing <see cref="Client"/> or creates and caches an <see cref="HttpClient"/>
    /// configured with this context's <see cref="CookieContainer"/> and resilience policies.
    /// </summary>
    public HttpClient GetOrCreateClient()
    {
        if (Client != null)
            return Client;

        Client = !string.IsNullOrEmpty(DeviceId) || !string.IsNullOrEmpty(IdentitySecret)
            ? SteamHttpClientFactory.CreateMobileClient(CookieContainer)
            : SteamHttpClientFactory.CreateClient(CookieContainer);

        return Client;
    }

    public SteamSessionContext()
    {
        CookieContainer = new CookieContainer();
    }

    public SteamSessionContext(CookieContainer cookieContainer)
    {
        CookieContainer = cookieContainer;
    }

    /// <summary>
    /// Creates a session context from an existing <see cref="ISessionData"/>.
    /// </summary>
    public static SteamSessionContext FromSession(ISessionData session,
        CookieContainer? cookieContainer = null, HttpClient? client = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        var container = cookieContainer ?? new CookieContainer();
        container.SetSteamCookies(session);

        var context = new SteamSessionContext(container)
        {
            SteamId = session.SteamId,
            SessionId = session.SessionId,
            RefreshToken = session.RefreshToken,
            AccessToken = session.GetToken(SteamDomain.Community)?.Token,
            Client = client
        };

        if (session is IMobileSessionData mobile)
        {
            var mobileToken = mobile.GetMobileToken();
            if (mobileToken != null)
            {
                context.AccessToken = mobileToken.Value.Token;
            }
            container.SetSteamMobileCookies(session);
        }

        return context;
    }

    /// <summary>
    /// Creates a session context from <see cref="MobileData"/> or <see cref="MobileDataExtended"/>.
    /// </summary>
    public static SteamSessionContext FromMobile(MobileData data, SteamId? steamId = null,
        CookieContainer? cookieContainer = null, HttpClient? client = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        var container = cookieContainer ?? new CookieContainer();
        var context = new SteamSessionContext(container)
        {
            SteamId = steamId,
            DeviceId = data.DeviceId,
            IdentitySecret = data.IdentitySecret,
            SharedSecret = data.SharedSecret,
            Client = client
        };

        if (data is MobileDataExtended ext)
        {
            if (ext.SteamId.Steam64 != 0)
            {
                context.SteamId = ext.SteamId;
            }
            if (ext.SessionData != null)
            {
                if (context.SteamId == null || context.SteamId.Value.Steam64 == 0)
                {
                    context.SteamId = ext.SessionData.SteamId;
                }
                context.SessionId = ext.SessionData.SessionId;
                context.RefreshToken = ext.SessionData.RefreshToken;
                context.AccessToken = ext.SessionData.GetMobileToken()?.Token
                                      ?? ext.SessionData.GetToken(SteamDomain.Community)?.Token;
                container.SetSteamMobileCookies(ext.SessionData);
            }
        }

        return context;
    }

    /// <summary>
    /// Creates a session context from an <see cref="IMobileSessionData"/> and associated <see cref="MobileData"/>.
    /// </summary>
    public static SteamSessionContext FromMobileSession(IMobileSessionData session,
        MobileData? mobileData = null, CookieContainer? cookieContainer = null, HttpClient? client = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        var container = cookieContainer ?? new CookieContainer();
        container.SetSteamMobileCookies(session);

        var context = new SteamSessionContext(container)
        {
            SteamId = session.SteamId,
            SessionId = session.SessionId,
            RefreshToken = session.RefreshToken,
            AccessToken = session.GetMobileToken()?.Token ?? session.GetToken(SteamDomain.Community)?.Token,
            Client = client
        };

        if (mobileData != null)
        {
            context.DeviceId = mobileData.DeviceId;
            context.IdentitySecret = mobileData.IdentitySecret;
            context.SharedSecret = mobileData.SharedSecret;
        }

        return context;
    }

    /// <summary>
    /// Ensures that the required cookies for this session are populated in the <see cref="CookieContainer"/>.
    /// </summary>
    public void EnsureCookies(string language = "english")
    {
        if (SessionId != null && SteamId != null)
        {
            var community = SteamDomains.GetDomainUri(SteamDomain.Community);
            CookieContainer.Add(community, new Cookie(AdmissionHelper.SESSION_ID_COOKIE_NAME, SessionId, "/"));
            CookieContainer.Add(community, new Cookie(AdmissionHelper.LANGUAGE_COOKIE_NAME, language, "/"));
            CookieContainer.Add(community, new Cookie("steamid", SteamId.Value.Steam64.ToString(), "/"));
            AdmissionHelper.TransferCommunityCookies(CookieContainer);
        }
    }

    /// <summary>
    /// Generates the query parameters required for Steam Mobile confirmation endpoints.
    /// </summary>
    public ValueTask<IEnumerable<KeyValuePair<string, string>>> GetConfirmationKvpAsync(
        string tag, ISteamTimeAligner? timeAligner, CancellationToken cancellationToken = default)
    {
        if (SteamId == null)
            throw new InvalidOperationException("SteamId is required in SteamSessionContext");
        if (string.IsNullOrEmpty(DeviceId))
            throw new InvalidOperationException("DeviceId is required in SteamSessionContext");
        if (string.IsNullOrEmpty(IdentitySecret))
            throw new InvalidOperationException("IdentitySecret is required in SteamSessionContext");

        return SteamMobileConfirmationsApi.GetConfirmationKvpAsync(
            SteamId.Value, DeviceId, IdentitySecret, tag, timeAligner, cancellationToken);
    }

    /// <summary>
    /// Generates the query parameters required for Steam Mobile confirmation endpoints.
    /// </summary>
    public ValueTask<IEnumerable<KeyValuePair<string, string>>> GetConfirmationKvpAsync(
        string tag = "conf", CancellationToken cancellationToken = default)
        => GetConfirmationKvpAsync(tag, null, cancellationToken);

    /// <summary>
    /// Returns the session ID or retrieves it from cookies; throws <see cref="SessionInvalidException"/> if absent.
    /// </summary>
    public string EnsureSessionId()
    {
        if (!string.IsNullOrEmpty(SessionId))
            return SessionId;

        var fromCookie = CookieContainer.GetSessionId();
        if (!string.IsNullOrEmpty(fromCookie))
        {
            SessionId = fromCookie;
            return fromCookie;
        }

        throw new SessionInvalidException("SessionId is missing in SteamSessionContext");
    }

    /// <summary>
    /// Returns the access token; throws <see cref="SessionInvalidException"/> if absent.
    /// </summary>
    public string EnsureAccessToken()
    {
        if (!string.IsNullOrEmpty(AccessToken))
            return AccessToken;

        throw new SessionInvalidException("AccessToken is missing in SteamSessionContext");
    }

    public override string ToString() =>
        $"SteamSessionContext {{ SteamId = {SteamId}, DeviceId = {DeviceId}, SessionId = ****, AccessToken = ****, RefreshToken = ****, SharedSecret = ****, IdentitySecret = **** }}";
}
