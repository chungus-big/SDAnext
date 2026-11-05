using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SteamLib.Abstractions;
using SteamLib.SteamMobile;
using SteamLib.Web;

namespace SteamLib.Services;

/// <summary>
/// Extension methods for setting up SteamLib services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class SteamLibServiceCollectionExtensions
{
    /// <summary>
    /// Registers SteamLib services (HTTP provider, TimeAligner, Authentication, Confirmations, Linker) in the service container.
    /// </summary>
    public static IServiceCollection AddSteamLib(this IServiceCollection services)
    {
        services.TryAddSingleton<ISteamHttpClientProvider, DefaultSteamHttpClientProvider>();
        services.TryAddSingleton<ISteamTimeAligner, SteamTimeAligner>();
        services.TryAddSingleton<ISteamAuthenticationService, SteamAuthenticationService>();
        services.TryAddSingleton<ISteamConfirmationsService, SteamConfirmationsService>();
        services.TryAddSingleton<ISteamLinkerService, SteamLinkerService>();
        return services;
    }
}
