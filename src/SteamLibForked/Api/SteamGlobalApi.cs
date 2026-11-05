using System.Net;
using System.Web;
using SteamLib.Core;
using SteamLib.Models;
using SteamLib.Web;
using SteamLib.Web.Scrappers.HTML;

namespace SteamLib.Api;

public static class SteamGlobalApi
{
    /// <summary>
    /// Fetches the login page and parses session credentials from JavaScript global variables.
    /// </summary>
    public static async Task<ScriptHeaderModel> GetSessionIdFromLoginPage(HttpClient client,
        CancellationToken cancellationToken = default)
    {
        var resp = await client.GetStringAsync("https://steamcommunity.com/login/home", cancellationToken);
        return SteamLibErrorMonitor.HandleResponse(resp, ScriptHeaderScrapper.Scrap);
    }

    public static async Task<ScriptHeaderModel> GetSessionIdFromPage(HttpClient client, string url,
        CancellationToken cancellationToken = default)
    {
        var resp = await client.GetStringAsync(url, cancellationToken);
        return SteamLibErrorMonitor.HandleResponse(resp, ScriptHeaderScrapper.Scrap);
    }


    [Obsolete(
        "Not recommended. Creates HttpClientHandler's copy with AllowAutoRedirect = false. Rapid usage of this method can exhaust socket pool")]
    public static async Task<string> EligibilityCheck(HttpClientHandler handler,
        CancellationToken cancellationToken = default)
    {
        var oneTimeHandler = new HttpClientHandler
        {
            CookieContainer = handler.CookieContainer,
            AllowAutoRedirect = false,
            AutomaticDecompression = handler.AutomaticDecompression
        };
        var client = new HttpClient(oneTimeHandler, true);
        var req = new HttpRequestMessage(HttpMethod.Get, SteamConstants.STEAM_COMMUNITY + "/market/eligibilitycheck/");
        var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var setCookieHeaders =
            resp.Headers.FirstOrDefault(h => h.Key == "Set-Cookie").Value
            ?? throw new NullReferenceException("Can't find 'Set-Cookie' header in EligibilityCheck response");

        var webTradeEligibility = setCookieHeaders.FirstOrDefault(s => s.StartsWith("webTradeEligibility"))
            ?? throw new NullReferenceException(
                "Can't find webTradeEligibility header in EligibilityCheck response");

        var eligibilityStr = webTradeEligibility
            .ToCharArray()
            .SkipWhile(ch => !ch.Equals('='))
            .TakeWhile(ch => !ch.Equals(';'))
            .ToArray();

        var result = new string(eligibilityStr[1..]);
        client.Dispose();
        return HttpUtility.UrlDecode(result);
    }

    /// <summary>
    /// Executes market eligibility check using a preconfigured <see cref="HttpClient"/>
    /// (must be configured with <c>AllowAutoRedirect = false</c>).
    /// </summary>
    public static async Task<string> EligibilityCheck(HttpClient client,
        CancellationToken cancellationToken = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, SteamConstants.STEAM_COMMUNITY + "/market/eligibilitycheck/");
        var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        var setCookieHeaders =
            resp.Headers.FirstOrDefault(h => h.Key == "Set-Cookie").Value
            ?? throw new NullReferenceException("Can't find 'Set-Cookie' header in EligibilityCheck response");

        var webTradeEligibility = setCookieHeaders.FirstOrDefault(s => s.StartsWith("webTradeEligibility"))
            ?? throw new NullReferenceException(
                "Can't find webTradeEligibility header in EligibilityCheck response");

        var eligibilityStr = webTradeEligibility
            .ToCharArray()
            .SkipWhile(ch => !ch.Equals('='))
            .TakeWhile(ch => !ch.Equals(';'))
            .ToArray();

        var result = new string(eligibilityStr[1..]);
        return HttpUtility.UrlDecode(result);
    }

    [Obsolete(
        "Creates a one-time SocketsHttpHandler with AllowAutoRedirect = false. Rapid usage can exhaust socket pool. Prefer SetEligibilityCheckCookies or pass a preconfigured HttpClient.")]
    public static async Task<string> EligibilityCheck(CookieContainer container,
        CancellationToken cancellationToken = default)
    {
        var socketsHandler = new SocketsHttpHandler
        {
            CookieContainer = container,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(15)
        };
        using var client = new HttpClient(socketsHandler, true);
        return await EligibilityCheck(client, cancellationToken).ConfigureAwait(false);
    }

    [Obsolete(
        "Creates a one-time SocketsHttpHandler with AllowAutoRedirect = false. Rapid usage can exhaust socket pool. Prefer SetEligibilityCheckCookies or pass a preconfigured HttpClient.")]
    public static Task<string> EligibilityCheck(SteamSessionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return EligibilityCheck(context.CookieContainer, cancellationToken);
    }

    public static Task SetEligibilityCheckCookies(HttpClient client, CancellationToken cancellationToken = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, SteamConstants.STEAM_COMMUNITY + "/market/eligibilitycheck/");
        return client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }
}