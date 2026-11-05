namespace SteamLib.Abstractions.Auth;

public interface ISteamGuardProvider : IAuthProvider
{
    public ValueTask<string> GetSteamGuardCode();
}