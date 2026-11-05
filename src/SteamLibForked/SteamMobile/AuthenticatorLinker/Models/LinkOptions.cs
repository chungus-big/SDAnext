using SteamLib.Abstractions.Auth;
using SteamLib.Abstractions.Linker;

namespace SteamLib.SteamMobile.AuthenticatorLinker.Models;

public class LinkOptions(
    HttpClient httpClient,
    ILoginConsumer loginConsumer,
    ISmsCodeProvider? smsCodeProvider,
    IEmailProvider? emailProvider,
    IPhoneNumberProvider? phoneNumberProvider,
    Action<MobileDataExtended>? backupHandler = null)
{
    public ILoginConsumer LoginConsumer { get; } = loginConsumer;
    public HttpClient HttpClient { get; } = httpClient;
    public IEmailProvider? EmailProvider { get; } = emailProvider;
    public ISmsCodeProvider? SmsCodeProvider { get; } = smsCodeProvider;
    public IPhoneNumberProvider? PhoneNumberProvider { get; } = phoneNumberProvider;
    public Action<MobileDataExtended>? BackupHandler { get; } = backupHandler;
}
