using System.Security.Cryptography;
using System.Text;
using SteamLib.Models;
using SteamLib.Models.Session;
using SteamLib.ProtoCore.Enums;
using SteamLib.ProtoCore.Services;
using SteamLib.SteamMobile;
using SteamLib.Utility;
using SteamLib.Utility.MafileSerialization;
using SteamLib.Web;
using Xunit;

namespace SteamLib.Tests;

public class MemorySafetyAndMaskingTests
{
    [Fact]
    public void GenerateCode_SpanOverloadMatchesStringAndByteArray()
    {
        var secretBytes = Encoding.UTF8.GetBytes("SecretBytesSample123");
        var secretBase64 = Convert.ToBase64String(secretBytes);
        var time = 1712345678L;

        var fromString = SteamGuardCodeGenerator.GenerateCode(secretBase64, time);
        var fromByteArray = SteamGuardCodeGenerator.GenerateCode(secretBytes, time);
        var fromSpan = SteamGuardCodeGenerator.GenerateCode((ReadOnlySpan<byte>)secretBytes, time);

        Assert.Equal(fromString, fromByteArray);
        Assert.Equal(fromString, fromSpan);
        Assert.Equal(5, fromSpan.Length);
    }

    [Fact]
    public void GenerateConfirmationHash_SpanMatchesString()
    {
        var secretBytes = Encoding.UTF8.GetBytes("IdentitySecret123456");
        var secretBase64 = Convert.ToBase64String(secretBytes);
        var time = 1712345678L;
        var tag = "conf";

        var hashFromString = EncryptionHelper.GenerateConfirmationHash(time, secretBase64, tag);
        var hashFromSpan = EncryptionHelper.GenerateConfirmationHash(time, (ReadOnlySpan<byte>)secretBytes, tag);

        Assert.Equal(hashFromString, hashFromSpan);

        Span<byte> destination = stackalloc byte[20];
        EncryptionHelper.GenerateConfirmationHashBytes(time, (ReadOnlySpan<byte>)secretBytes, tag, destination);
        Assert.Equal(hashFromString, Convert.ToBase64String(destination));
    }

    [Fact]
    public void MobileData_ToString_MasksSecrets()
    {
        var mobileData = new MobileData
        {
            DeviceId = "android:12345678-abcd",
            SharedSecret = "SuperSecretSharedKey123==",
            IdentitySecret = "SuperSecretIdentityKey456=="
        };

        var str = mobileData.ToString();

        Assert.Contains("android:12345678-abcd", str);
        Assert.Contains("SharedSecret = ****", str);
        Assert.Contains("IdentitySecret = ****", str);
        Assert.DoesNotContain("SuperSecretSharedKey123==", str);
        Assert.DoesNotContain("SuperSecretIdentityKey456==", str);
    }

    [Fact]
    public void MobileDataExtended_ToString_MasksAllSensitiveFields()
    {
        var ext = new MobileDataExtended
        {
            AccountName = "test_steam_user",
            SteamId = SteamId.FromSteam64(76561198000000001),
            DeviceId = "android:87654321-dcba",
            SharedSecret = "SecretSharedABC==",
            IdentitySecret = "SecretIdentityDEF==",
            RevocationCode = "R12345"
        };

        var str = ext.ToString();

        Assert.Contains("test_steam_user", str);
        Assert.Contains("RevocationCode = ****", str);
        Assert.Contains("SharedSecret = ****", str);
        Assert.Contains("IdentitySecret = ****", str);
        Assert.DoesNotContain("SecretSharedABC==", str);
        Assert.DoesNotContain("SecretIdentityDEF==", str);
        Assert.DoesNotContain("R12345", str);
    }

    [Fact]
    public void SessionData_ToString_MasksTokensAndSessionId()
    {
        var steamId = SteamId.FromSteam64(76561198000000002);
        var secretToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.dummy_sensitive_jwt";
        var refreshToken = new SteamAuthToken(secretToken, steamId, UnixTimeStamp.FromDateTime(DateTime.UtcNow.AddDays(1)),
            SteamDomain.Community, SteamAccessTokenType.Refresh);

        var session = new SessionData("SECRET_SESSION_ID_XYZ", steamId, refreshToken, []);

        var str = session.ToString();

        Assert.Contains("SessionId = ****", str);
        Assert.Contains("RefreshToken = ****", str);
        Assert.DoesNotContain("SECRET_SESSION_ID_XYZ", str);
        Assert.DoesNotContain("dummy_sensitive_jwt", str);
    }

    [Fact]
    public void MobileSessionData_ToString_MasksMobileToken()
    {
        var steamId = SteamId.FromSteam64(76561198000000003);
        var refToken = new SteamAuthToken("sensitive_refresh", steamId, UnixTimeStamp.FromDateTime(DateTime.UtcNow.AddDays(1)),
            SteamDomain.Community, SteamAccessTokenType.Refresh);
        var mobToken = new SteamAuthToken("sensitive_mobile_access", steamId, UnixTimeStamp.FromDateTime(DateTime.UtcNow.AddDays(1)),
            SteamDomain.Community, SteamAccessTokenType.Mobile);

        var mobSession = new MobileSessionData("SECRET_SESS_456", steamId, refToken, mobToken, []);

        var str = mobSession.ToString();

        Assert.Contains("MobileToken = ****", str);
        Assert.Contains("SessionId = ****", str);
        Assert.DoesNotContain("sensitive_mobile_access", str);
        Assert.DoesNotContain("SECRET_SESS_456", str);
    }

    [Fact]
    public void SteamAuthToken_ToString_MasksJwtToken()
    {
        var steamId = SteamId.FromSteam64(76561198000000004);
        var token = new SteamAuthToken("very_secret_jwt_payload_here", steamId,
            UnixTimeStamp.FromDateTime(DateTime.UtcNow.AddDays(1)), SteamDomain.Community, SteamAccessTokenType.Web);

        var str = token.ToString();

        Assert.Contains("Token = ****", str);
        Assert.DoesNotContain("very_secret_jwt_payload_here", str);
    }

    [Fact]
    public void SteamSessionContext_ToString_MasksAllSensitiveFields()
    {
        var ctx = new SteamSessionContext
        {
            SteamId = SteamId.FromSteam64(76561198000000005),
            DeviceId = "device_123",
            SessionId = "sensitive_session_id",
            AccessToken = "sensitive_access_token",
            SharedSecret = "sensitive_shared_secret",
            IdentitySecret = "sensitive_identity_secret"
        };

        var str = ctx.ToString();

        Assert.Contains("device_123", str);
        Assert.Contains("SessionId = ****", str);
        Assert.Contains("AccessToken = ****", str);
        Assert.Contains("SharedSecret = ****", str);
        Assert.Contains("IdentitySecret = ****", str);
        Assert.DoesNotContain("sensitive_session_id", str);
        Assert.DoesNotContain("sensitive_access_token", str);
        Assert.DoesNotContain("sensitive_shared_secret", str);
        Assert.DoesNotContain("sensitive_identity_secret", str);
    }

    [Fact]
    public void LegacyMafile_ToString_MasksSecrets()
    {
        var legacy = new LegacyMafile
        {
            AccountName = "legacy_user",
            DeviceId = "device_legacy",
            SharedSecret = "top_secret_shared",
            IdentitySecret = "top_secret_identity",
            RevocationCode = "R99999"
        };

        var str = legacy.ToString();

        Assert.Contains("legacy_user", str);
        Assert.Contains("SharedSecret = ****", str);
        Assert.Contains("IdentitySecret = ****", str);
        Assert.Contains("RevocationCode = ****", str);
        Assert.DoesNotContain("top_secret_shared", str);
        Assert.DoesNotContain("top_secret_identity", str);
        Assert.DoesNotContain("R99999", str);
    }

    [Fact]
    public void BeginAuthSessionViaCredentials_Request_ToString_MasksEncryptedPassword()
    {
        var req = new BeginAuthSessionViaCredentials_Request
        {
            AccountName = "user123",
            EncryptedPassword = "SuperSensitiveEncryptedPasswordBase64==",
            PlatformType = EAuthTokenPlatformType.MobileApp,
            WebsiteId = "Mobile"
        };

        var str = req.ToString();

        Assert.Contains("user123", str);
        Assert.Contains("EncryptedPassword = ****", str);
        Assert.DoesNotContain("SuperSensitiveEncryptedPasswordBase64==", str);
    }

    [Fact]
    public void RemoveAuthenticatorViaChallengeContinue_Replacement_Token_ToString_MasksSecrets()
    {
        var token = new RemoveAuthenticatorViaChallengeContinue_Replacement_Token
        {
            AccountName = "test_user",
            SteamId = 76561198000000001,
            Status = 1,
            RevocationCode = "R12345",
            IdentitySecret = [1, 2, 3, 4]
        };

        var str = token.ToString();

        Assert.StartsWith("RemoveAuthenticatorViaChallengeContinue_Replacement_Token", str);
        Assert.Contains("test_user", str);
        Assert.Contains("RevocationCode = ****", str);
        Assert.Contains("IdentitySecret = ****", str);
        Assert.DoesNotContain("R12345", str);
    }

    [Fact]
    public void FinalizeAddAuthenticator_Response_ToString_ContainsStatus()
    {
        var resp = new FinalizeAddAuthenticator_Response
        {
            Success = true,
            WantMore = false,
            ServerTime = 1712345678UL,
            Status = 1
        };

        var str = resp.ToString();

        Assert.StartsWith("FinalizeAddAuthenticator_Response", str);
        Assert.Contains("Success = True", str);
        Assert.Contains("Status = 1", str);
    }

    [Fact]
    public void ToBase64EncryptedPassword_CorrectlyEncryptsPassword()
    {
        using var rsa = RSA.Create(2048);
        var parameters = rsa.ExportParameters(false);
        var expHex = Convert.ToHexString(parameters.Exponent!);
        var modHex = Convert.ToHexString(parameters.Modulus!);

        var password = "MySuperSecretPassword!@#123";
        var encryptedBase64 = EncryptionHelper.ToBase64EncryptedPassword(expHex, modHex, password);
        var encryptedBytes = Convert.FromBase64String(encryptedBase64);

        var decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.Pkcs1);
        var decryptedPassword = Encoding.ASCII.GetString(decryptedBytes);

        Assert.Equal(password, decryptedPassword);
    }

    [Fact]
    public void GenerateConfirmationHash_NonAsciiAndLongTag_DoesNotThrow()
    {
        var secretBytes = Encoding.UTF8.GetBytes("IdentitySecret123456");
        var secretBase64 = Convert.ToBase64String(secretBytes);
        var time = 1712345678L;

        var hash = EncryptionHelper.GenerateConfirmationHash(time, secretBase64, "тест_юникод_очень_длинный_тег_1234567890");
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }
}
