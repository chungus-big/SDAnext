using SteamLib.Core;
using SteamLib.Web;

namespace SteamLib.Api.Trade;

public static class SteamTradeApi
{
    public static async Task<bool> Acknowledge(HttpClient client, string sessionId,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, string>
        {
            {"sessionid", sessionId},
            {"message", "1"}
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, Routes.ACKNOWLEDGE)
        {
            Content = new FormUrlEncodedContent(data)
        };
        var resp = await client.SendAsync(req, cancellationToken);
        var cont = await resp.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancellationToken);
        // Steam returns "[]" on success; unauthenticated / expired requests return "{\"success\":false}"
        return cont == "[]";
    }

    public static Task<bool> Acknowledge(HttpClient client, SteamSessionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var sessionId = context.EnsureSessionId();
        return Acknowledge(client, sessionId, cancellationToken);
    }

    public static Task<bool> Acknowledge(SteamSessionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var client = context.GetOrCreateClient();
        return Acknowledge(client, context, cancellationToken);
    }

    public static class Routes
    {
        public const string ACKNOWLEDGE = SteamConstants.STEAM_COMMUNITY + "/trade/new/acknowledge";
    }
}