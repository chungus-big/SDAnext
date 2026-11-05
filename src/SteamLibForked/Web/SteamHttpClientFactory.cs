using System.Net;
using System.Net.Http.Headers;
using SteamLib.Abstractions;
using SteamLib.Authentication;

namespace SteamLib.Web;

/// <summary>
/// Default implementation of <see cref="ISteamHttpClientProvider"/> using <see cref="SocketsHttpHandler"/>
/// and resilience retry handlers.
/// </summary>
public class DefaultSteamHttpClientProvider : ISteamHttpClientProvider
{
    private readonly Lazy<HttpClient> _sharedClient;

    public DefaultSteamHttpClientProvider()
    {
        _sharedClient = new Lazy<HttpClient>(() =>
        {
            var handler = CreateSocketsHandler(null);
            var retryHandler = new SteamRetryHandler(handler);
            var client = new HttpClient(retryHandler, disposeHandler: true)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            ConfigureClientHeaders(client);
            return client;
        });
    }

    public HttpClient SharedClient => _sharedClient.Value;

    public SocketsHttpHandler CreateSocketsHandler(CookieContainer? cookieContainer = null)
    {
        return new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            AutomaticDecompression = DecompressionMethods.All,
            EnableMultipleHttp2Connections = true,
            ConnectTimeout = TimeSpan.FromSeconds(15),
            CookieContainer = cookieContainer ?? new CookieContainer()
        };
    }

    public HttpClient CreateClient(CookieContainer? cookieContainer = null, SteamRetryOptions? retryOptions = null)
    {
        var socketsHandler = CreateSocketsHandler(cookieContainer);
        var retryHandler = new SteamRetryHandler(socketsHandler, retryOptions);
        var client = new HttpClient(retryHandler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        ConfigureClientHeaders(client);
        return client;
    }

    public HttpClient CreateMobileClient(CookieContainer? cookieContainer = null, IMobileSessionData? sessionData = null,
        SteamRetryOptions? retryOptions = null)
    {
        var container = cookieContainer ?? new CookieContainer();
        if (sessionData != null)
        {
            sessionData.EnsureValidated();
            container.SetSteamMobileCookies(sessionData);
        }
        else
        {
            container.AddMinimalMobileCookies();
        }

        container.InjectWebTradeEligibilityCookie();

        var socketsHandler = CreateSocketsHandler(container);
        var retryHandler = new SteamRetryHandler(socketsHandler, retryOptions);
        var client = new HttpClient(retryHandler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(50)
        };

        ConfigureMobileClientHeaders(client);
        return client;
    }

    private static void ConfigureClientHeaders(HttpClient client)
    {
        client.DefaultRequestHeaders.Accept.ParseAdd(
            "application/json, text/javascript, text/html, application/xml, text/xml, */*");
        client.DefaultRequestHeaders.Referrer = new Uri("https://steamcommunity.com");
        client.DefaultRequestHeaders.Add("Origin", "https://steamcommunity.com");
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));
    }

    private static void ConfigureMobileClientHeaders(HttpClient client)
    {
        ConfigureClientHeaders(client);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("okhttp/3.12.12");
    }
}

/// <summary>
/// Static factory providing access to the current <see cref="ISteamHttpClientProvider"/>
/// and convenience methods for creating optimized Steam <see cref="HttpClient"/> instances.
/// </summary>
public static class SteamHttpClientFactory
{
    private static ISteamHttpClientProvider _provider = new DefaultSteamHttpClientProvider();

    public static ISteamHttpClientProvider Provider
    {
        get => _provider;
        set => _provider = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Singleton shared <see cref="HttpClient"/> configured with <see cref="SocketsHttpHandler"/>,
    /// 15-minute connection pooling, DNS rotation, and resilience retry policy.
    /// Reusable for stateless operations (e.g. <see cref="SteamMobile.TimeAligner"/>).
    /// </summary>
    public static HttpClient SharedClient => Provider.SharedClient;

    /// <summary>
    /// Creates a new <see cref="SocketsHttpHandler"/> configured with optimal pooling and DNS rotation.
    /// </summary>
    public static SocketsHttpHandler CreateSocketsHandler(CookieContainer? cookieContainer = null)
        => Provider.CreateSocketsHandler(cookieContainer);

    /// <summary>
    /// Creates a new <see cref="HttpClient"/> backed by <see cref="SocketsHttpHandler"/> and <see cref="SteamRetryHandler"/>.
    /// </summary>
    public static HttpClient CreateClient(CookieContainer? cookieContainer = null, SteamRetryOptions? retryOptions = null)
        => Provider.CreateClient(cookieContainer, retryOptions);

    /// <summary>
    /// Creates a new <see cref="HttpClient"/> configured for Steam Mobile APIs with standard mobile headers.
    /// </summary>
    public static HttpClient CreateMobileClient(CookieContainer? cookieContainer = null, IMobileSessionData? sessionData = null,
        SteamRetryOptions? retryOptions = null)
        => Provider.CreateMobileClient(cookieContainer, sessionData, retryOptions);
}
