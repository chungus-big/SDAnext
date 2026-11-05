using SteamLib.ProtoCore.Enums;
using SteamLib.ProtoCore.Services;

namespace SteamLib.Authentication;

public static class AuthRequestHelper
{
    #region MobileGuard

    public static UpdateAuthSessionWithSteamGuardCode_Request CreateMobileCodeRequest(string code, ulong clientId,
        SteamId steamId)
    {
        return CreateMobileCodeRequest(code, clientId, steamId.Steam64.ToUlong());
    }

    public static UpdateAuthSessionWithSteamGuardCode_Request CreateMobileCodeRequest(string code, ulong clientId,
        ulong steamId)
    {
        return new UpdateAuthSessionWithSteamGuardCode_Request
        {
            Code = code,
            ClientId = clientId,
            Steamid = steamId,
            CodeType = EAuthSessionGuardType.DeviceCode
        };
    }

    #endregion

    #region EmailGuard

    public static UpdateAuthSessionWithSteamGuardCode_Request CreateEmailCodeRequest(string code, ulong clientId,
        SteamId steamId)
    {
        return CreateEmailCodeRequest(code, clientId, steamId.Steam64.ToUlong());
    }

    public static UpdateAuthSessionWithSteamGuardCode_Request CreateEmailCodeRequest(string code, ulong clientId,
        ulong steamId)
    {
        return new UpdateAuthSessionWithSteamGuardCode_Request
        {
            Code = code,
            ClientId = clientId,
            Steamid = steamId,
            CodeType = EAuthSessionGuardType.EmailCode
        };
    }

    #endregion

    #region MobileConf

    public static UpdateAuthSessionWithMobileConfirmation_Request CreateMobileConfirmationRequest(int version,
        ulong clientId, SteamId steamId, string sharedSecret)
    {
        var req = new UpdateAuthSessionWithMobileConfirmation_Request
        {
            Version = version,
            ClientId = clientId,
            Steamid = steamId.Steam64.ToUlong(),
            Confirm = true,
            Persistence = 1
        };
        req.ComputeSignature(sharedSecret);
        return req;
    }

    public static void EnrichMobileConfirmationWithSignature(UpdateAuthSessionWithMobileConfirmation_Request request,
        string sharedSecret)
    {
        request.ComputeSignature(sharedSecret);
    }

    public static byte[] ComputeConfirmationSignature(int version, ulong clientId, SteamId steamId, string sharedSecret)
    {
        return ComputeConfirmationSignature(version, clientId, steamId.Steam64.ToUlong(), sharedSecret);
    }

    public static byte[] ComputeConfirmationSignature(int version, ulong clientId, ulong steamId, string sharedSecret)
    {
        var req = new UpdateAuthSessionWithMobileConfirmation_Request
        {
            Version = version,
            ClientId = clientId,
            Steamid = steamId
        };
        req.ComputeSignature(sharedSecret);
        return req.Signature;
    }

    #endregion
}