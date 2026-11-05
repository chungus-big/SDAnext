using SteamLib.Authentication.LoginV2;
using SteamLib.ProtoCore.Enums;

namespace SteamLib.Abstractions.Auth;

public interface IAuthProvider
{
    bool IsSupportedGuardType(ILoginConsumer consumer, EAuthSessionGuardType type);

    /// <summary>
    /// </summary>
    /// <param name="authClient">Client which initiated auth request (typically <see cref="LoginV2Executor" />'s client)</param>
    /// <param name="loginConsumer"></param>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task UpdateAuthSession(HttpClient authClient, ILoginConsumer loginConsumer, UpdateAuthSessionModel model,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Data model required to update an authentication session with a guard code or confirmation.
/// </summary>
public class UpdateAuthSessionModel(EAuthSessionGuardType type, ulong clientId, ulong steamId)
{
    public EAuthSessionGuardType Type { get; } = type;
    public ulong ClientId { get; } = clientId;
    public ulong SteamId { get; } = steamId;
}