using System.Text.Json.Serialization;
using SteamLib.Authentication.LoginV2;
using SteamLib.Models;
using SteamLib.Models.Account;
using SteamLib.SteamMobile;
using SteamLib.SteamMobile.AuthenticatorLinker.Models;
using SteamLib.Web.Models.Account;
using SteamLib.Web.Models.Mobile;
using SteamLib.Web.Serialization.Converters;

namespace SteamLib.Web.Serialization;

/// <summary>
/// Source-generated JSON serializer context for high-performance, reflection-free serialization of SteamLib DTOs.
/// </summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = [typeof(UnixTimeStampJsonConverter), typeof(SteamIdJsonConverter)])]
[JsonSerializable(typeof(ConfirmationsJson))]
[JsonSerializable(typeof(ConfirmationJson))]
[JsonSerializable(typeof(List<ConfirmationJson>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(ScriptHeaderModel))]
[JsonSerializable(typeof(FinalizeLoginJson))]
[JsonSerializable(typeof(TransferInfo))]
[JsonSerializable(typeof(List<TransferInfo>))]
[JsonSerializable(typeof(TransferInfoParams))]
[JsonSerializable(typeof(TransferResult))]
[JsonSerializable(typeof(WalletInfoSchema))]
[JsonSerializable(typeof(UserWalletSchema))]
[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(WebTradeEligibility))]
[JsonSerializable(typeof(TimeQueryResponse))]
[JsonSerializable(typeof(TimeQueryInnerResponse))]
[JsonSerializable(typeof(PhoneValidationResponse))]
[JsonSerializable(typeof(HasPhoneResponse))]
[JsonSerializable(typeof(UnixTimeStamp))]
[JsonSerializable(typeof(SteamId))]
[JsonSerializable(typeof(SteamId?))]
public partial class SteamJsonSerializerContext : JsonSerializerContext
{
}
