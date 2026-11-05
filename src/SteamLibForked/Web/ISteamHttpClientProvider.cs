using System.Net;
using SteamLib.Abstractions;

namespace SteamLib.Web;

/// <summary>
/// Provides configured <see cref="HttpClient"/> instances and socket handlers
/// optimized for Steam protocol communication with connection pooling, DNS rotation,
/// automatic decompression, and resilience retry policies.
/// </summary>
public interface ISteamHttpClientProvider
{
    /// <summary>
    /// Gets a thread-safe singleton <see cref="HttpClient"/> backed by <see cref="SocketsHttpHandler"/>
    /// with pooled connection lifetime (15 min), DNS rotation, automatic decompression, and retry policies.
    /// Recommended for stateless operations such as <see cref="SteamMobile.TimeAligner"/> and public Steam APIs.
    /// </summary>
    HttpClient SharedClient { get; }

    /// <summary>
    /// Creates a new <see cref="SocketsHttpHandler"/> configured with optimal pooling,
    /// DNS lifetime rotation (15m), automatic decompression (All), and HTTP/2 settings.
    /// </summary>
    SocketsHttpHandler CreateSocketsHandler(CookieContainer? cookieContainer = null);

    /// <summary>
    /// Creates an <see cref="HttpClient"/> backed by <see cref="SocketsHttpHandler"/> and <see cref="SteamRetryHandler"/>.
    /// </summary>
    HttpClient CreateClient(CookieContainer? cookieContainer = null, SteamRetryOptions? retryOptions = null);

    /// <summary>
    /// Creates an <see cref="HttpClient"/> configured for Steam Mobile APIs with standard mobile headers,
    /// cookies, and retry policies.
    /// </summary>
    HttpClient CreateMobileClient(CookieContainer? cookieContainer = null, IMobileSessionData? sessionData = null, SteamRetryOptions? retryOptions = null);
}
