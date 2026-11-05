using SteamLib.Abstractions.Auth;

namespace SteamLib.Abstractions.Linker;

public interface IPhoneNumberProvider
{
    public Task<long?> GetPhoneNumber(ILoginConsumer caller);
}