namespace SteamLib.Web;

/// <summary>
/// Encapsulates an <see cref="HttpClient"/> and its underlying <see cref="HttpClientHandler"/>.
/// </summary>
public readonly record struct HttpClientHandlerPair(HttpClient Client, HttpClientHandler Handler) : IDisposable
{
    public void Dispose()
    {
        Client?.Dispose();
        Handler?.Dispose();
    }
}
