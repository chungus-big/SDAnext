using SteamLib.Abstractions.Auth;

namespace SteamLib.Abstractions.Linker;

public interface ISmsCodeProvider
{
    public Task<int> GetSmsCode(ILoginConsumer caller, long? phoneNumber, string? phoneHint);
}