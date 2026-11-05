using System.Buffers.Binary;
using System.Security.Cryptography;
using ProtoBuf;
using SteamLib.ProtoCore.Enums;
using SteamLib.ProtoCore.Interfaces;
using SteamLib.Utility;

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
#pragma warning disable CS8618

namespace SteamLib.ProtoCore.Services;

[ProtoContract]
public class GetPasswordRSAPublicKey_Request : IProtoMsg
{
    [ProtoMember(1)] public string AccountName { get; set; }
}

[ProtoContract]
public class GetPasswordRSAPublicKey_Response : IProtoMsg
{
    [ProtoMember(1)] public string PublicKeyMod { get; set; }
    [ProtoMember(2)] public string PublicKeyExp { get; set; }
    [ProtoMember(3)] public ulong Timestamp { get; set; }
}

[ProtoContract]
public class BeginAuthSessionViaCredentials_Request : IProtoMsg
{
    [ProtoMember(1)] public string DeviceFriendlyName { get; set; }
    [ProtoMember(2)] public string AccountName { get; set; }
    [ProtoMember(3)] public string EncryptedPassword { get; set; }
    [ProtoMember(4)] public ulong EncryptionTimestamp { get; set; }
    [ProtoMember(5)] public bool RememberLogin { get; set; }
    [ProtoMember(6)] public EAuthTokenPlatformType PlatformType { get; set; }
    [ProtoMember(7)] public int Persistence { get; set; }

    /// <summary>
    ///     Gets or sets the website id that the login will be performed for.
    ///     Known values are "Unknown", "Client", "Mobile", "Website", "Store", "Community", "Partner", "SteamStats".
    /// </summary>
    /// <value>The website id.</value>
    [ProtoMember(8)]
    public string WebsiteId { get; set; }

    [ProtoMember(9)] public DeviceDetails DeviceDetails { get; set; } = new();
    [ProtoMember(10)] public string GuardData { get; set; }
    [ProtoMember(11)] public uint Language { get; set; }

    public override string ToString() =>
        $"BeginAuthSessionViaCredentials_Request {{ AccountName = {AccountName}, PlatformType = {PlatformType}, WebsiteId = {WebsiteId}, EncryptedPassword = **** }}";
}

[ProtoContract]
public class DeviceDetails : IProtoMsg
{
    /// <summary>
    ///     User-Agent if platform is SteamCommunity
    /// </summary>
    [ProtoMember(1)]
    public string DeviceFriendlyName { get; }

    [ProtoMember(2)] public EAuthTokenPlatformType PlatformType { get; }
    [ProtoMember(3)] public int? OsType { get; }
    [ProtoMember(4)] public uint? GamingDeviceType { get; }

    #region Members

    public DeviceDetails()
    {
    }

    public DeviceDetails(string deviceFriendlyName, EAuthTokenPlatformType platformType, int? osType,
        uint? gamingDeviceType)
    {
        DeviceFriendlyName = deviceFriendlyName;
        PlatformType = platformType;
        OsType = osType;
        GamingDeviceType = gamingDeviceType;
    }

    public DeviceDetails(string deviceFriendlyName, EAuthTokenPlatformType platformType, EOSType? osType,
        uint? gamingDeviceType)
    {
        DeviceFriendlyName = deviceFriendlyName;
        PlatformType = platformType;
        OsType = (int?)osType;
        GamingDeviceType = gamingDeviceType;
    }

    public static DeviceDetails CreateDefault()
    {
        return new DeviceDetails("", EAuthTokenPlatformType.WebBrowser, (int?)null, null);
    }

    public static DeviceDetails CreateCommunityDetails(string userAgent)
    {
        return new DeviceDetails(userAgent, EAuthTokenPlatformType.WebBrowser, (int?)null, null);
    }

    public static DeviceDetails CreateMobileDetails(string deviceName)
    {
        return new DeviceDetails(deviceName, EAuthTokenPlatformType.MobileApp, EOSType.AndroidUnknown, 528);
    }

    #endregion
}

[ProtoContract]
public class BeginAuthSessionViaCredentials_Response : IProtoMsg
{
    [ProtoMember(1)] public ulong ClientId { get; set; }
    [ProtoMember(2)] public byte[] RequestId { get; set; }
    [ProtoMember(3)] public float Interval { get; set; }
    [ProtoMember(4)] public List<AllowedConfirmationMsg> AllowedConfirmations { get; } = [];
    [ProtoMember(5)] public ulong Steamid { get; set; }
    [ProtoMember(6)] public string WeakToken { get; set; }
    [ProtoMember(7)] public string AgreementSessionUrl { get; set; }
    [ProtoMember(8)] public string ExtendedErrorMessage { get; set; }
}

[ProtoContract]
public class AllowedConfirmationMsg : IProtoMsg
{
    [ProtoMember(1)] public EAuthSessionGuardType ConfirmationType { get; set; }
    [ProtoMember(2)] public string AssociatedMessage { get; set; }
}

[ProtoContract]
public class PollAuthSessionStatus_Request : IProtoMsg
{
    [ProtoMember(1)] public ulong ClientId { get; set; }
    [ProtoMember(2)] public byte[] RequestId { get; set; }
    [ProtoMember(3)] public ulong TokenToRevoke { get; set; }
}

[ProtoContract]
public class PollAuthSessionStatus_Response : IProtoMsg
{
    [ProtoMember(1)] public ulong NewClientId { get; set; }
    [ProtoMember(2)] public string NewChallengeUrl { get; set; }
    [ProtoMember(3)] public string RefreshToken { get; set; }
    [ProtoMember(4)] public string AccessToken { get; set; }
    [ProtoMember(5)] public bool HadRemoteInteraction { get; set; }
    [ProtoMember(6)] public string AccountName { get; set; }
    [ProtoMember(7)] public string NewGuardData { get; set; }
    [ProtoMember(8)] public string AgreementSessionUrl { get; set; }
}

[ProtoContract]
public class UpdateAuthSessionWithSteamGuardCode_Request : IProtoMsg
{
    [ProtoMember(1)] public ulong ClientId { get; set; }

    [ProtoMember(2, DataFormat = DataFormat.FixedSize)]
    public ulong Steamid { get; set; }

    [ProtoMember(3)] public string Code { get; set; }
    [ProtoMember(4)] public EAuthSessionGuardType CodeType { get; set; }
}

[ProtoContract]
public class GetAuthSessionsForAccount_Response : IProtoMsg
{
    [ProtoMember(1)] public List<ulong> ClientIds { get; set; } = [];
}

[ProtoContract]
public class GetAuthSessionInfo_Request : IProtoMsg
{
    [ProtoMember(1)] public ulong ClientId { get; set; }
}

[ProtoContract]
public class GetAuthSessionInfo_Response : IProtoMsg
{
    [ProtoMember(1)] public string IP { get; set; }
    [ProtoMember(2)] public string GeoLoc { get; set; }
    [ProtoMember(3)] public string City { get; set; }
    [ProtoMember(4)] public string State { get; set; }
    [ProtoMember(5)] public string Country { get; set; }
    [ProtoMember(6)] public EAuthTokenPlatformType PlatformType { get; set; }
    [ProtoMember(7)] public string DeviceFriendlyName { get; set; }
    [ProtoMember(8)] public int Version { get; set; }
    [ProtoMember(9)] public bool RequestorLocationMismatch { get; set; }
    [ProtoMember(10)] public bool LoginHistory { get; set; }
    [ProtoMember(11)] public bool HighUsageLogin { get; set; }
    [ProtoMember(12)] public int RequestedPersistence { get; set; }
}

[ProtoContract]
public class UpdateAuthSessionWithMobileConfirmation_Request : IProtoMsg
{
    [ProtoMember(1)] public int Version { get; set; }
    [ProtoMember(2)] public ulong ClientId { get; set; }

    [ProtoMember(3, DataFormat = DataFormat.FixedSize)]
    public ulong Steamid { get; set; }

    [ProtoMember(4)] public byte[] Signature { get; set; }
    [ProtoMember(5)] public bool Confirm { get; set; }
    [ProtoMember(6)] public int Persistence { get; set; }


    public void ComputeSignature(string privateKey)
    {
        Span<byte> signatureData = stackalloc byte[2 + 8 + 8];
        BinaryPrimitives.WriteInt16LittleEndian(signatureData[..2], (short)Version);
        BinaryPrimitives.WriteUInt64LittleEndian(signatureData.Slice(2, 8), ClientId);
        BinaryPrimitives.WriteUInt64LittleEndian(signatureData.Slice(10, 8), Steamid);

        Span<byte> keyBytes = stackalloc byte[64];
        if (Convert.TryFromBase64String(privateKey, keyBytes, out var bytesWritten))
        {
            var key = keyBytes[..bytesWritten];
            Signature = HMACSHA256.HashData(key, signatureData);
            CryptographicOperations.ZeroMemory(key);
        }
        else
        {
            var key = Convert.FromBase64String(privateKey);
            Signature = HMACSHA256.HashData(key, signatureData);
            CryptographicOperations.ZeroMemory(key);
        }
    }

    public override string ToString() =>
        $"UpdateAuthSessionWithMobileConfirmation_Request {{ ClientId = {ClientId}, SteamId = {Steamid}, Signature = **** }}";
}

[ProtoContract]
public class GenerateAccessTokenForApp_Request : IProtoMsg
{
    [ProtoMember(1)] public string RefreshToken { get; set; }

    [ProtoMember(2, DataFormat = DataFormat.FixedSize)]
    public long SteamId { get; set; }

    // Corresponds to protobuf enum ETokenRenewalType (k_ETokenRenewalType_Allow = 1)
    [ProtoMember(3)] public ETokenRenewalType TokenRenewalType { get; set; } = ETokenRenewalType.Allow;

    public override string ToString() =>
        $"GenerateAccessTokenForApp_Request {{ SteamId = {SteamId}, RefreshToken = **** }}";
}

[ProtoContract]
public class GenerateAccessTokenForApp_Response : IProtoMsg
{
    [ProtoMember(1)] public string AccessToken { get; set; }

    public override string ToString() =>
        "GenerateAccessTokenForApp_Response { AccessToken = **** }";
}