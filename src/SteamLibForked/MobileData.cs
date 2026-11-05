using System.Text.Json.Serialization;
using SteamLib.Models.Session;
using SteamLib.Web.Converters;

namespace SteamLib;

//WARNING: Any changes here should be reflected in MafileSerializer.cs

public class MobileData
{
    [JsonRequired] public string SharedSecret { get; set; } = null!;
    [JsonRequired] public string IdentitySecret { get; set; } = null!;
    public string DeviceId { get; set; } = null!;

    public override string ToString() =>
        $"MobileData {{ DeviceId = {DeviceId}, SharedSecret = ****, IdentitySecret = **** }}";
}

public class MobileDataExtended : MobileData
{
    public string? RevocationCode { get; set; }
    public string AccountName { get; set; } = null!;

    public MobileSessionData? SessionData
    {
        get => _sessionData;
        set
        {
            _sessionData = value;
            if (value != null)
            {
                SteamId = value.SteamId;
            }
        }
    }

    [JsonConverter(typeof(SteamIdToSteam64Converter))]
    public SteamId SteamId { get; set; }

    private MobileSessionData? _sessionData;


    #region Unused

    public long ServerTime { get; set; } //Unused
    public ulong SerialNumber { get; set; } //Unused //fixed64 greater than long must be ulong or string
    public string Uri { get; set; } = null!; //Unused
    public string TokenGid { get; set; } = null!; //Unused
    public string Secret1 { get; set; } = null!; //Unused

    #endregion

    public override string ToString() =>
        $"MobileDataExtended {{ AccountName = {AccountName}, SteamId = {SteamId}, DeviceId = {DeviceId}, RevocationCode = ****, SharedSecret = ****, IdentitySecret = **** }}";
}