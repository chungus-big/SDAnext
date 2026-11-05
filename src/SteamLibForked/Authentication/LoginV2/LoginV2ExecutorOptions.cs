using SteamLib.Abstractions.Auth;
using SteamLib.ProtoCore.Enums;
using SteamLib.ProtoCore.Services;

namespace SteamLib.Authentication.LoginV2;

public class LoginV2ExecutorOptions(ILoginConsumer consumer, HttpClient httpClient)
{
    public ILoginConsumer Consumer { get; } = consumer;
    public HttpClient HttpClient { get; } = httpClient;
    public IReadOnlyList<IAuthProvider> AuthProviders { get; init; } = [];
    public List<EAuthSessionGuardType> PreferredGuardTypes { get; init; } = [];
    public DeviceDetails? DeviceDetails { get; init; }

    /// <summary>
    ///     Gets or sets the website id that the login will be performed for.
    ///     Known values are "Unknown", "Client", "Mobile", "Website", "Store", "Community", "Partner", "SteamStats".
    /// </summary>
    /// <value>The website id.</value>
    public string? WebsiteId { get; init; }
}