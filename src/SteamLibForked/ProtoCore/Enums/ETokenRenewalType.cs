using ProtoBuf;

namespace SteamLib.ProtoCore.Enums;

/// <summary>
/// Protobuf enum corresponding to ETokenRenewalType for access token renewal.
/// </summary>
[ProtoContract]
public enum ETokenRenewalType
{
    [ProtoEnum(Name = "k_ETokenRenewalType_None")]
    None = 0,

    [ProtoEnum(Name = "k_ETokenRenewalType_Allow")]
    Allow = 1
}
