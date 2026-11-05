namespace SteamLib.Abstractions.Auth;

public interface ILoginConsumer
{
    /// <summary>
    /// Friendly identifier of the consumer used for logging and diagnostics.
    /// </summary>
    public string FriendlyName { get; }
}