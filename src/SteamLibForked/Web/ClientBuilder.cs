using System.Net;
using System.Net.Http.Headers;
using SteamLib.Authentication;
using SteamLib.Abstractions;

namespace SteamLib.Web;

public static class ClientBuilder
{
    public static HttpClientHandlerPair BuildMobileClient(IMobileSessionData? sessionData = null,
        bool disposeHandler = true)
    {
        sessionData?.EnsureValidated();
        var handler = new HttpClientHandler();
        var client = new HttpClient(handler, disposeHandler);

        client.DefaultRequestHeaders.Accept.ParseAdd(
            "application/json, text/javascript, text/html, application/xml, text/xml, */*");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("okhttp/3.12.12");

        var container = handler.CookieContainer;
        if (sessionData == null)
        {
            container.ClearMobileSessionCookies();
        }
        else
        {
            container.SetSteamMobileCookies(sessionData);
        }

        handler.CookieContainer.InjectWebTradeEligibilityCookie();
        ConfigureCommon(handler, client);
        return new HttpClientHandlerPair(client, handler);
    }

    /// <summary>
    /// Builds a mobile HttpClient backed by SocketsHttpHandler, connection pooling, and resilience retry policy.
    /// </summary>
    public static HttpClient BuildMobileClientWithSockets(IMobileSessionData? sessionData = null,
        CookieContainer? cookieContainer = null)
    {
        return SteamHttpClientFactory.CreateMobileClient(cookieContainer, sessionData);
    }

    private static void ConfigureCommon(HttpClientHandler handler, HttpClient client)
    {
        ConfigureCommonClient(client);
        handler.AutomaticDecompression = DecompressionMethods.All;
    }

    private static void ConfigureCommonClient(HttpClient client)
    {
        client.Timeout = TimeSpan.FromSeconds(50);
        client.DefaultRequestHeaders.Referrer = new Uri("https://steamcommunity.com");
        client.DefaultRequestHeaders.Add("Origin", "https://steamcommunity.com");
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));
    }
}